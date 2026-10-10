using System.Text;
using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Contracts;
using ComicBookReader.Infrastructure.Imaging;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using SharpCompress.Common.Rar;
using SharpCompress.Readers;
using SharpCompress.Readers.Rar;

namespace ComicBookReader.Infrastructure.Archives;

public sealed class SolidRarMaterializer
{
    public const long MaximumCacheBytes = 2L * 1024 * 1024 * 1024;
    private readonly string cacheRoot;
    private readonly long maximumEntryBytes;
    private readonly long maximumCacheBytes;

    public SolidRarMaterializer(string? cacheRoot = null,
        long maximumEntryBytes = SkiaPageDecoder.MaximumEncodedBytes, long maximumCacheBytes = MaximumCacheBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumEntryBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumEntryBytes, SkiaPageDecoder.MaximumEncodedBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCacheBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumCacheBytes, MaximumCacheBytes);
        this.cacheRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(cacheRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ComicBookReader", "solid-cache")));
        this.maximumEntryBytes = maximumEntryBytes;
        this.maximumCacheBytes = maximumCacheBytes;
        RecoverAbandonedCaches();
    }

    // Metadata scan uses only the supplied stream, so no other archive volume can be discovered.
    internal static bool InspectArchive(FileStream file, CancellationToken cancellationToken)
    {
        try
        {
            using var archive = RarArchive.OpenArchive(file, ReaderOptions.ForExternalStream);
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (entry.IsEncrypted || !entry.IsComplete) throw UnsupportedRar();
            }
            foreach (var volume in archive.Volumes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (volume is RarVolume rarVolume && rarVolume.IsMultiVolume) throw UnsupportedRar();
            }
            return archive.IsSolid;
        }
        catch (SharpCompress.Common.CryptographicException exception) { throw UnsupportedRar(exception); }
        catch (Exception exception) when (exception is IncompleteArchiveException or MultipartStreamRequiredException or MultiVolumeExtractionException)
        {
            throw UnsupportedRar(exception);
        }
        catch (SharpCompressException exception)
        {
            throw InvalidRar(exception);
        }
        finally
        {
            file.Position = 0;
        }
    }

    internal SolidRarSession Materialize(FileStream file, SkiaPageDecoder decoder,
        CancellationToken cancellationToken, IProgress<ComicOpenProgress>? progress)
    {
        var book = BookInfo.Create(file.Name, file.Length, File.GetLastWriteTimeUtc(file.Name));
        using var reader = RarReader.OpenReader(file, ReaderOptions.ForExternalStream);
        var cache = SolidRarCache.Create(cacheRoot);
        try
        {
            var pages = new List<(string Name, int Ordinal, string File)>();
            var buffer = new byte[64 * 1024];
            long materialized = 0;
            var ordinal = 0;
            progress?.Report(new ComicOpenProgress(0, 0));
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!reader.MoveToNextEntry()) break;
                var entry = reader.Entry;
                if (entry.IsEncrypted) throw UnsupportedRar();
                var name = (entry.Key ?? string.Empty).Replace('\\', '/');
                if (!entry.IsDirectory)
                {
                    var isImage = ComicEntryIndex.IsImage(name);
                    if (entry.Size > maximumEntryBytes || isImage && entry.Size > maximumCacheBytes - materialized)
                        throw LimitExceeded();
                    string? pageFile = isImage ? cache.NewPageFile() : null;
                    using var output = pageFile is null ? null : cache.CreatePage(pageFile);
                    using var input = reader.OpenEntryStream();
                    try
                    {
                        long entryBytes = 0;
                        while (true)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var count = input.Read(buffer, 0, buffer.Length);
                            if (count == 0) break;
                            cancellationToken.ThrowIfCancellationRequested();
                            if (count > maximumEntryBytes - entryBytes) throw LimitExceeded();
                            entryBytes += count;
                            if (output is not null)
                            {
                                if (count > maximumCacheBytes - materialized)
                                    throw LimitExceeded();
                                output.Write(buffer, 0, count);
                                materialized += count;
                                progress?.Report(new ComicOpenProgress(ordinal, materialized));
                            }
                        }
                    }
                    catch
                    {
                        // EntryStream.Dispose otherwise decompresses to its end despite cancellation/limits.
                        reader.Cancel();
                        throw;
                    }
                    if (pageFile is not null) pages.Add((name, ordinal, pageFile));
                }
                ordinal++;
                progress?.Report(new ComicOpenProgress(ordinal, materialized));
            }
            cancellationToken.ThrowIfCancellationRequested();
            return new SolidRarSession(book, cache, pages.AsReadOnly(), decoder);
        }
        catch (SharpCompress.Common.CryptographicException exception)
        {
            reader.Cancel();
            cache.Dispose();
            throw UnsupportedRar(exception);
        }
        catch (Exception exception) when (exception is IncompleteArchiveException or MultipartStreamRequiredException or MultiVolumeExtractionException)
        {
            reader.Cancel();
            cache.Dispose();
            throw UnsupportedRar(exception);
        }
        catch (Exception exception) when (exception is SharpCompressException or InvalidDataException or EndOfStreamException)
        {
            reader.Cancel();
            cache.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
            throw InvalidRar(exception);
        }
        catch
        {
            reader.Cancel();
            cache.Dispose();
            throw;
        }
    }

    public void RecoverAbandonedCaches() => SolidRarCache.Recover(cacheRoot);

    private static ReaderException UnsupportedRar(Exception? inner = null) => new(ReaderErrorCode.UnsupportedArchive,
        "Arquivos RAR com senha ou múltiplos volumes não são suportados.", inner);
    private static ReaderException InvalidRar(Exception inner) => new(ReaderErrorCode.InvalidArchive,
        "O arquivo RAR está danificado ou incompleto.", inner);
    private static ReaderException LimitExceeded() => new(ReaderErrorCode.ResourceLimit,
        "As páginas excedem o limite de 128 MiB por entrada ou 2 GiB de cache da sessão.");
}

internal sealed class SolidRarCache : IDisposable
{
    private const string RootOwner = ".comicbookreader-solid-cache-v1";
    private const string SessionOwner = ".owner";
    private const string OwnerMagic = "ComicBookReader solid cache v1\n";
    private readonly string root;
    private readonly string directory;
    private readonly FileStream ownerLease;
    private bool disposed;

    private SolidRarCache(string root, string directory, FileStream ownerLease)
    {
        this.root = root;
        this.directory = directory;
        this.ownerLease = ownerLease;
    }

    internal static SolidRarCache Create(string root)
    {
        EnsurePlainPath(root);
        Directory.CreateDirectory(root);
        var marker = Path.Combine(root, RootOwner);
        var expectedRootMarker = OwnerMagic + root;
        try
        {
            using var created = new FileStream(marker, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            created.Write(Encoding.UTF8.GetBytes(expectedRootMarker));
            created.Flush(true);
        }
        catch (IOException) when (File.Exists(marker)) { }
        if (!RootIsOwned(root)) throw new IOException("Unowned cache root.");
        var directory = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        // Read/write is exclusive. ShareDelete allows our held lease to cover the entire deletion.
        var lease = new FileStream(Path.Combine(directory, SessionOwner), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Delete);
        lease.Write(Encoding.UTF8.GetBytes(OwnerMagic + directory));
        lease.Flush(true);
        return new SolidRarCache(root, directory, lease);
    }

    internal string NewPageFile() => Guid.NewGuid().ToString("N") + ".bin";
    internal FileStream CreatePage(string name) => new(PagePath(name), FileMode.CreateNew, FileAccess.Write, FileShare.None);
    internal FileStream OpenPage(string name) => new(PagePath(name), FileMode.Open, FileAccess.Read, FileShare.Read);

    private string PagePath(string name)
    {
        if (!IsPageName(name)) throw new IOException("Invalid generated page name.");
        EnsurePlainPath(directory);
        var path = Path.Combine(directory, name);
        EnsurePlainPath(path);
        return path;
    }

    internal static void Recover(string root)
    {
        try
        {
            if (!Directory.Exists(root) || !RootIsOwned(root)) return;
            foreach (var directory in Directory.EnumerateDirectories(root))
            {
                try
                {
                    if (!Guid.TryParseExact(Path.GetFileName(directory), "N", out _)) continue;
                    EnsurePlainPath(directory);
                    using var lease = new FileStream(Path.Combine(directory, SessionOwner), FileMode.Open, FileAccess.ReadWrite, FileShare.Delete);
                    DeleteOwned(root, directory, lease);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
    }

    private static bool RootIsOwned(string root)
    {
        EnsurePlainPath(root);
        var marker = Path.Combine(root, RootOwner);
        EnsurePlainPath(marker);
        using var stream = new FileStream(marker, FileMode.Open, FileAccess.Read, FileShare.Read);
        return MarkerMatches(stream, OwnerMagic + root);
    }

    private static bool MarkerMatches(FileStream stream, string expected)
    {
        var bytes = Encoding.UTF8.GetBytes(expected);
        if (stream.Length != bytes.Length) return false;
        stream.Position = 0;
        var actual = new byte[bytes.Length];
        stream.ReadExactly(actual);
        return actual.AsSpan().SequenceEqual(bytes);
    }

    private static void DeleteOwned(string root, string directory, FileStream lease)
    {
        EnsurePlainPath(root);
        EnsurePlainPath(directory);
        if (!string.Equals(Path.GetDirectoryName(directory), root, StringComparison.OrdinalIgnoreCase) ||
            !Guid.TryParseExact(Path.GetFileName(directory), "N", out _) || !RootIsOwned(root) ||
            !MarkerMatches(lease, OwnerMagic + directory)) throw new IOException("Unowned cache directory.");
        var files = Directory.GetFileSystemEntries(directory);
        foreach (var path in files)
        {
            EnsurePlainPath(path);
            if ((File.GetAttributes(path) & FileAttributes.Directory) != 0 ||
                (Path.GetFileName(path) != SessionOwner && !IsPageName(Path.GetFileName(path))))
                throw new IOException("Unexpected cache entry.");
        }
        // No recursive deletion: refuse unexpected children and links before removing anything.
        foreach (var path in files.Where(path => Path.GetFileName(path) != SessionOwner)) File.Delete(path);
        File.Delete(Path.Combine(directory, SessionOwner));
        Directory.Delete(directory, false);
    }

    private static bool IsPageName(string name) => name.Length == 36 && name.EndsWith(".bin", StringComparison.Ordinal) &&
        Guid.TryParseExact(name[..32], "N", out _);

    private static void EnsurePlainPath(string path)
    {
        for (var current = Path.GetFullPath(path); !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Reparse points are not permitted in the session cache.");
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            DeleteOwned(root, directory, ownerLease);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Keep the marker for a later recovery rather than widen the cleanup target.
        }
        finally
        {
            ownerLease.Dispose();
        }
    }
}
