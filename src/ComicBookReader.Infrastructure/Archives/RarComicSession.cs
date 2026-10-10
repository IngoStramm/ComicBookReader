using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Contracts;
using ComicBookReader.Infrastructure.Imaging;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using SharpCompress.Common.Rar;
using SharpCompress.Readers;

namespace ComicBookReader.Infrastructure.Archives;

public sealed class RarComicSession : IComicSession
{
    private readonly FileStream file;
    private readonly IRarArchive archive;
    private readonly IReadOnlyList<IArchiveEntry> entries;
    private readonly IReadOnlyList<IndexedComicPage> index;
    private readonly SkiaPageDecoder decoder;
    private readonly SemaphoreSlim reads = new(1, 1);
    private readonly object disposeLock = new();
    private volatile bool closing;
    private Task? disposeTask;

    public BookInfo Book { get; }
    public IReadOnlyList<PageInfo> Pages { get; }

    internal RarComicSession(FileStream file, SkiaPageDecoder decoder, CancellationToken cancellationToken)
    {
        this.file = file;
        this.decoder = decoder;
        Book = BookInfo.Create(file.Name, file.Length, File.GetLastWriteTimeUtc(file.Name));
        // Stream overload prevents discovery of other volume files. The session owns the supplied file.
        archive = RarArchive.OpenArchive(file, ReaderOptions.ForExternalStream);
        try
        {
            var collected = new List<IArchiveEntry>();
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Auxiliary entries also carry encryption/split metadata; never inspect their Size first.
                if (entry.IsEncrypted || !entry.IsComplete) throw UnsupportedRar();
                collected.Add(entry);
            }
            foreach (var volume in archive.Volumes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (volume is RarVolume rarVolume && rarVolume.IsMultiVolume) throw UnsupportedRar();
            }
            if (archive.IsSolid)
                throw new ReaderException(ReaderErrorCode.UnsupportedArchive,
                    "A leitura de arquivos RAR sólidos ainda não está disponível.");
            entries = collected.AsReadOnly();
            index = ComicEntryIndex.Create(entries.Select((entry, ordinal) => (entry, ordinal))
                .Where(item => !item.entry.IsDirectory)
                .Select(item => ((item.entry.Key ?? string.Empty).Replace('\\', '/'), item.ordinal)));
            if (index.Count == 0)
                throw new ReaderException(ReaderErrorCode.NoImages, "O arquivo não contém páginas JPEG, PNG ou WebP.");
            Pages = Array.AsReadOnly(index.Select(page => page.Page).ToArray());
        }
        catch (SharpCompress.Common.CryptographicException exception)
        {
            archive.Dispose();
            throw UnsupportedRar(exception);
        }
        catch (Exception exception) when (exception is IncompleteArchiveException or MultipartStreamRequiredException or MultiVolumeExtractionException)
        {
            archive.Dispose();
            throw UnsupportedRar(exception);
        }
        catch (SharpCompressException exception)
        {
            archive.Dispose();
            throw new ReaderException(ReaderErrorCode.InvalidArchive, "O arquivo RAR está danificado ou incompleto.", exception);
        }
        catch
        {
            archive.Dispose();
            throw;
        }
    }

    public async ValueTask<PageImage> ReadPageAsync(int pageIndex, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(closing, this);
        if ((uint)pageIndex >= (uint)Pages.Count) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        await reads.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(closing, this);
            return await Task.Run(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var entry = entries[index[pageIndex].EntryOrdinal];
                    if (entry.Size > SkiaPageDecoder.MaximumEncodedBytes)
                        throw new ReaderException(ReaderErrorCode.ResourceLimit, "A página excede o limite de 128 MiB.");
                    using var stream = entry.OpenEntryStream();
                    var image = await decoder.DecodeAsync(stream, cancellationToken).ConfigureAwait(false);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        image.Dispose();
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    return image;
                }
                catch (SharpCompress.Common.CryptographicException exception)
                {
                    throw UnsupportedRar(exception);
                }
                catch (Exception exception) when (exception is SharpCompressException or IOException or InvalidDataException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new ReaderException(ReaderErrorCode.PageDecodeFailed, "Não foi possível ler a página do arquivo RAR.", exception);
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            reads.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (disposeLock)
        {
            closing = true;
            disposeTask ??= DisposeCoreAsync();
            return new ValueTask(disposeTask);
        }
    }

    private async Task DisposeCoreAsync()
    {
        await reads.WaitAsync().ConfigureAwait(false);
        try
        {
            archive.Dispose();
        }
        finally
        {
            file.Dispose();
            reads.Release();
        }
    }

    private static ReaderException UnsupportedRar(Exception? inner = null) => new(ReaderErrorCode.UnsupportedArchive,
        "Arquivos RAR com senha ou múltiplos volumes não são suportados.", inner);
}
