using System.IO.Compression;
using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Contracts;
using ComicBookReader.Infrastructure.Imaging;

namespace ComicBookReader.Infrastructure.Archives;

public sealed class ZipComicSession : IComicSession
{
    private readonly ZipArchive archive;
    private readonly IReadOnlyList<IndexedComicPage> index;
    private readonly SkiaPageDecoder decoder;
    private readonly SemaphoreSlim reads = new(1, 1);
    private readonly object disposeLock = new();
    private volatile bool closing;
    private Task? disposeTask;

    public BookInfo Book { get; }
    public IReadOnlyList<PageInfo> Pages { get; }

    internal ZipComicSession(FileStream file, SkiaPageDecoder decoder)
    {
        this.decoder = decoder;
        Book = BookInfo.Create(file.Name, file.Length, File.GetLastWriteTimeUtc(file.Name));
        archive = new ZipArchive(file, ZipArchiveMode.Read);
        try
        {
            index = ZipPageIndex.Read(archive);
            Pages = Array.AsReadOnly(index.Select(entry => entry.Page).ToArray());
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
                    var entry = archive.Entries[index[pageIndex].EntryOrdinal];
                    if (entry.Length > SkiaPageDecoder.MaximumEncodedBytes)
                        throw new ReaderException(ReaderErrorCode.ResourceLimit, "A página excede o limite de 128 MiB.");
                    using var stream = entry.Open();
                    var image = await decoder.DecodeAsync(stream, cancellationToken).ConfigureAwait(false);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        image.Dispose();
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    return image;
                }
                catch (Exception exception) when (exception is InvalidDataException or IOException)
                {
                    throw new ReaderException(ReaderErrorCode.PageDecodeFailed, "Não foi possível ler a página do arquivo ZIP.", exception);
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
            reads.Release();
        }
    }
}
