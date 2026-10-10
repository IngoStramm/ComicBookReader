namespace ComicBookReader.Core.Preferences;

public interface IReadingPreferencesStore
{
    Task<ReaderPreferences> LoadAsync(CancellationToken cancellationToken);
    Task SaveAsync(ReaderPreferences preferences, CancellationToken cancellationToken);
}
