using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ComicBookReader.Core.ComicFiles;

public readonly record struct PixelSize(int Width, int Height);
public sealed record PageInfo(int Index, string PageKey, string EntryName);
public sealed record ComicOpenProgress(int EntriesProcessed, long BytesMaterialized);

public sealed record BookInfo(string BookId, string CanonicalPath, long Length, DateTime LastWriteUtc)
{
    public static BookInfo Create(string path, long length, DateTime lastWriteUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        var fullPath = Path.GetFullPath(path);
        var canonical = fullPath.ToUpperInvariant();
        var identity = JsonSerializer.Serialize(new { Path = canonical, Length = length, Ticks = lastWriteUtc.ToUniversalTime().Ticks });
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        return new BookInfo(id, fullPath, length, lastWriteUtc.ToUniversalTime());
    }
}

public interface IComicSession : IAsyncDisposable
{
    BookInfo Book { get; }
    IReadOnlyList<PageInfo> Pages { get; }
    ValueTask<PageImage> ReadPageAsync(int pageIndex, CancellationToken cancellationToken);
}

public interface IComicOpener
{
    Task<IComicSession> OpenAsync(string path, CancellationToken cancellationToken,
        IProgress<ComicOpenProgress>? progress = null);
}
