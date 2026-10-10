namespace ComicBookReader.Infrastructure.Storage;

public class AtomicFileWriter
{
    public virtual async Task WriteAsync(string path, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string fullPath = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);
        var existing = await PreferencesDto.ReadAsync(fullPath, cancellationToken).ConfigureAwait(false);
        if (existing.Status == PreferencesFileStatus.Future)
            throw new IOException("A future preferences version cannot be replaced.");
        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var temporary = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await temporary.WriteAsync(content, cancellationToken).ConfigureAwait(false);
                await temporary.FlushAsync(cancellationToken).ConfigureAwait(false);
                temporary.Flush(flushToDisk: true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (existing.Status == PreferencesFileStatus.Missing)
                File.Move(temporaryPath, fullPath);
            else
                File.Replace(temporaryPath, fullPath,
                    existing.Status == PreferencesFileStatus.Valid ? fullPath + ".bak" : null);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
