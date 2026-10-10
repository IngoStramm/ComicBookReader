using ComicBookReader.Core.Contracts;
using ComicBookReader.Core.Preferences;
using ComicBookReader.Infrastructure.Storage;
using Xunit;

namespace ComicBookReader.Integration.Tests.Storage;

public sealed class PreferencesStoreTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task MissingProfileUsesDefaultsWithoutCreatingFiles()
    {
        using var directory = new TemporaryPreferencesDirectory();
        var store = new JsonReadingPreferencesStore(directory.Path);
        var preferences = await store.LoadAsync(Token);
        Assert.Equal(1, preferences.PreferredPageWidthRatio);
        Assert.Equal(.6, preferences.ScrollSpeed);
        Assert.True(preferences.LastFullscreen);
        Assert.Null(store.LastWarning);
        Assert.False(store.IsReadOnly);
        Assert.Empty(Directory.GetFiles(directory.Path));
    }

    [Fact]
    public async Task RestartRestoresAllReadingDataAndOverrides()
    {
        using var directory = new TemporaryPreferencesDirectory();
        var expected = new ReaderPreferences
        {
            PreferredPageWidthRatio = 1.3,
            ScrollSpeed = .8,
            LastFullscreen = false,
            KeyBindings = new() { ["NextPage"] = "Ctrl+N" },
            Books = new()
            {
                ["book"] = new BookPreferences
                {
                    WidthRatio = .9,
                    Classifications = new() { ["page"] = PageClassificationOverride.Spread },
                    Position = new LastReadingPosition("page", 7, .2, .8)
                }
            }
        };
        await new JsonReadingPreferencesStore(directory.Path).SaveAsync(expected, Token);
        var actual = await new JsonReadingPreferencesStore(directory.Path).LoadAsync(Token);
        Assert.Equal(1.3, actual.PreferredPageWidthRatio);
        Assert.Equal(.8, actual.ScrollSpeed);
        Assert.False(actual.LastFullscreen);
        Assert.Equal("Ctrl+N", actual.KeyBindings["NextPage"]);
        Assert.Equal("PageUp", actual.KeyBindings["PreviousPage"]);
        Assert.Equal(.9, actual.Books["book"].WidthRatio);
        Assert.Equal(PageClassificationOverride.Spread, actual.Books["book"].Classifications["page"]);
        Assert.Equal(expected.Books["book"].Position, actual.Books["book"].Position);
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public async Task MissingKnownFieldsGetDefaults()
    {
        using var directory = new TemporaryPreferencesDirectory();
        await File.WriteAllTextAsync(directory.PreferencesPath,
            "{\"SchemaVersion\":1,\"Books\":{\"book\":{}}}", Token);
        var preferences = await new JsonReadingPreferencesStore(directory.Path).LoadAsync(Token);
        Assert.Equal(1, preferences.PreferredPageWidthRatio);
        Assert.Equal(.6, preferences.ScrollSpeed);
        Assert.True(preferences.LastFullscreen);
        Assert.Equal(7, preferences.KeyBindings.Count);
        Assert.Null(preferences.Books["book"].WidthRatio);
        Assert.Empty(preferences.Books["book"].Classifications);
    }

    [Fact]
    public async Task InvalidSnapshotCannotReplacePreviousFile()
    {
        using var directory = new TemporaryPreferencesDirectory();
        var store = new JsonReadingPreferencesStore(directory.Path);
        await store.SaveAsync(new ReaderPreferences(), Token);
        var before = await File.ReadAllBytesAsync(directory.PreferencesPath, Token);
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(
            new ReaderPreferences { ScrollSpeed = double.NaN }, Token));
        Assert.Equal(before, await File.ReadAllBytesAsync(directory.PreferencesPath, Token));
    }

    [Fact]
    public async Task CanceledLoadAndSaveRemainCancellation()
    {
        using var directory = new TemporaryPreferencesDirectory();
        var store = new JsonReadingPreferencesStore(directory.Path);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.LoadAsync(canceled.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(new ReaderPreferences(), canceled.Token));
        Assert.Empty(Directory.GetFiles(directory.Path));
    }

    [Fact]
    public async Task UnusableDirectoryReportsStorageUnavailable()
    {
        using var directory = new TemporaryPreferencesDirectory();
        string blocked = System.IO.Path.Combine(directory.Path, "file-instead-of-directory");
        await File.WriteAllTextAsync(blocked, "blocked", Token);
        var store = new JsonReadingPreferencesStore(blocked);
        var error = await Assert.ThrowsAsync<ReaderException>(() => store.SaveAsync(new ReaderPreferences(), Token));
        Assert.Equal(ReaderErrorCode.StorageUnavailable, error.Code);
        Assert.Equal("blocked", await File.ReadAllTextAsync(blocked, Token));
    }

    [Fact]
    public async Task SavesAreSerializedAndUseDetachedSnapshots()
    {
        using var directory = new TemporaryPreferencesDirectory();
        var writer = new GateWriter();
        var store = new JsonReadingPreferencesStore(directory.Path, writer);
        var first = store.SaveAsync(new ReaderPreferences { ScrollSpeed = .7 }, Token);
        await writer.Entered.Task.WaitAsync(Token);
        var latest = new ReaderPreferences
        {
            Books = new() { ["book"] = new BookPreferences { WidthRatio = .9 } }
        };
        var second = store.SaveAsync(latest, Token);
        latest.Books.Clear();
        Assert.Equal(1, writer.Calls);
        writer.Release.TrySetResult();
        await Task.WhenAll(first, second);
        var restored = await new JsonReadingPreferencesStore(directory.Path).LoadAsync(Token);
        Assert.Equal(.9, restored.Books["book"].WidthRatio);
        Assert.Equal(1, writer.MaximumActive);
        Assert.Equal(2, writer.Calls);
    }

    private sealed class GateWriter : AtomicFileWriter
    {
        private int active;
        public int Calls { get; private set; }
        public int MaximumActive { get; private set; }
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async Task WriteAsync(string path, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
        {
            Calls++;
            MaximumActive = Math.Max(MaximumActive, Interlocked.Increment(ref active));
            try
            {
                Entered.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
                await base.WriteAsync(path, content, cancellationToken);
            }
            finally { Interlocked.Decrement(ref active); }
        }
    }
}

internal sealed class TemporaryPreferencesDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ComicBookReader-preferences-tests", Guid.NewGuid().ToString("N"));
    public string PreferencesPath => System.IO.Path.Combine(Path, "preferences.json");
    public string BackupPath => PreferencesPath + ".bak";
    public TemporaryPreferencesDirectory() => Directory.CreateDirectory(Path);
    public void Dispose() => Directory.Delete(Path, recursive: true);
}
