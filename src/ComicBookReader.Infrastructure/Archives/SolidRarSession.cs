using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Contracts;
using ComicBookReader.Infrastructure.Imaging;

namespace ComicBookReader.Infrastructure.Archives;

public sealed class SolidRarSession : IComicSession
{
    private readonly SolidRarCache cache;
    private readonly IReadOnlyList<string> pageFiles;
    private readonly SkiaPageDecoder decoder;
    private readonly SemaphoreSlim reads = new(1, 1);
    private readonly object disposeLock = new();
    private volatile bool closing;
    private Task? disposeTask;

    public BookInfo Book { get; }
    public IReadOnlyList<PageInfo> Pages { get; }

    internal SolidRarSession(BookInfo book, SolidRarCache cache,
        IReadOnlyList<(string Name, int Ordinal, string File)> entries, SkiaPageDecoder decoder)
    {
        Book = book;
        this.cache = cache;
        this.decoder = decoder;
        var index = ComicEntryIndex.Create(entries.Select(entry => (entry.Name, entry.Ordinal)));
        if (index.Count == 0)
            throw new ReaderException(ReaderErrorCode.NoImages, "O arquivo não contém páginas JPEG, PNG ou WebP.");
        var filesByOrdinal = entries.ToDictionary(entry => entry.Ordinal, entry => entry.File);
        pageFiles = Array.AsReadOnly(index.Select(page => filesByOrdinal[page.EntryOrdinal]).ToArray());
        Pages = Array.AsReadOnly(index.Select(page => page.Page).ToArray());
    }

    public async ValueTask<PageImage> ReadPageAsync(int pageIndex, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(closing, this);
        if ((uint)pageIndex >= (uint)Pages.Count) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        await reads.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(closing, this);
            cancellationToken.ThrowIfCancellationRequested();
            using var stream = cache.OpenPage(pageFiles[pageIndex]);
            var image = await decoder.DecodeAsync(stream, cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested)
            {
                image.Dispose();
                cancellationToken.ThrowIfCancellationRequested();
            }
            return image;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new ReaderException(ReaderErrorCode.PageDecodeFailed,
                "Não foi possível ler a página materializada do arquivo RAR.", exception);
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
            cache.Dispose();
        }
        finally
        {
            reads.Release();
        }
    }
}
