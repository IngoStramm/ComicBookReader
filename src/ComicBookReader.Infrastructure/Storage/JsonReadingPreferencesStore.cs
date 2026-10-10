using ComicBookReader.Core.Contracts;
using ComicBookReader.Core.Preferences;

namespace ComicBookReader.Infrastructure.Storage;

public sealed class JsonReadingPreferencesStore : IReadingPreferencesStore
{
    private readonly string directory;
    private readonly string path;
    private readonly AtomicFileWriter writer;
    private readonly SemaphoreSlim access = new(1, 1);

    public JsonReadingPreferencesStore(string? directory = null, AtomicFileWriter? writer = null)
    {
        this.directory = Path.GetFullPath(directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ComicBookReader"));
        path = Path.Combine(this.directory, "preferences.json");
        this.writer = writer ?? new AtomicFileWriter();
    }

    public string? LastWarning { get; private set; }
    public bool IsReadOnly { get; private set; }

    public async Task<ReaderPreferences> LoadAsync(CancellationToken cancellationToken)
    {
        await access.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            LastWarning = null;
            IsReadOnly = false;
            if (File.Exists(directory)) throw new IOException("Preferences directory is a file.");
            var main = await PreferencesDto.ReadAsync(path, cancellationToken).ConfigureAwait(false);
            if (main.Status == PreferencesFileStatus.Valid) return main.Snapshot!;
            if (main.Status == PreferencesFileStatus.Future)
            {
                MarkFutureVersion();
                return new ReaderPreferences();
            }
            if (main.Status == PreferencesFileStatus.Invalid)
                await PreserveCorruptAsync(path, cancellationToken).ConfigureAwait(false);

            var backup = await PreferencesDto.ReadAsync(path + ".bak", cancellationToken).ConfigureAwait(false);
            if (backup.Status == PreferencesFileStatus.Future)
                MarkFutureVersion();
            else if (backup.Status == PreferencesFileStatus.Valid)
            {
                LastWarning = "Preferências recuperadas do backup; o arquivo anterior foi preservado.";
                return backup.Snapshot!;
            }
            else
            {
                if (backup.Status == PreferencesFileStatus.Invalid)
                    await PreserveCorruptAsync(path + ".bak", cancellationToken).ConfigureAwait(false);
                if (main.Status == PreferencesFileStatus.Invalid || backup.Status == PreferencesFileStatus.Invalid)
                    LastWarning = "Não foi possível recuperar as preferências; os arquivos foram preservados e os padrões serão usados.";
            }
            return new ReaderPreferences();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw StorageError(error);
        }
        finally { access.Release(); }
    }

    public async Task SaveAsync(ReaderPreferences preferences, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = PreferencesResolver.Validate(preferences);
        await access.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsReadOnly) throw new ReaderException(ReaderErrorCode.StorageUnavailable, LastWarning!);
            var main = await PreferencesDto.ReadAsync(path, cancellationToken).ConfigureAwait(false);
            var backup = await PreferencesDto.ReadAsync(path + ".bak", cancellationToken).ConfigureAwait(false);
            if (main.Status == PreferencesFileStatus.Future || backup.Status == PreferencesFileStatus.Future)
            {
                MarkFutureVersion();
                throw new ReaderException(ReaderErrorCode.StorageUnavailable, LastWarning!);
            }
            if (main.Status == PreferencesFileStatus.Invalid)
                await PreserveCorruptAsync(path, cancellationToken).ConfigureAwait(false);
            byte[] content = PreferencesDto.Encode(snapshot);
            if (content.Length > PreferencesDto.MaximumFileBytes)
                throw new ReaderException(ReaderErrorCode.StorageUnavailable, "As preferências excedem o limite de 8 MiB.");
            await writer.WriteAsync(path, content, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw StorageError(error);
        }
        finally { access.Release(); }
    }

    private void MarkFutureVersion()
    {
        IsReadOnly = true;
        LastWarning = "As preferências usam uma versão futura; alterações não serão salvas.";
    }

    private static ReaderException StorageError(Exception error) => new(
        ReaderErrorCode.StorageUnavailable, "Não foi possível acessar as preferências. A leitura pode continuar sem salvar alterações.", error);

    private static async Task PreserveCorruptAsync(string sourcePath, CancellationToken cancellationToken)
    {
        string preservedPath = sourcePath + $".corrupt-{Guid.NewGuid():N}.json";
        try
        {
            await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var preserved = new FileStream(preservedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.Asynchronous);
            await source.CopyToAsync(preserved, cancellationToken).ConfigureAwait(false);
            await preserved.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            File.Delete(preservedPath);
            throw;
        }
    }
}
