using ComicBookReader.Core.Contracts;
using ComicBookReader.Core.Preferences;
using ComicBookReader.Infrastructure.Storage;
using Xunit;

namespace ComicBookReader.Integration.Tests.Storage;

public sealed class PreferencesRecoveryTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SuccessfulReplacementKeepsLastValidVersionAsBackup()
    {
        using var directory = new TemporaryPreferencesDirectory();
        var store = new JsonReadingPreferencesStore(directory.Path);
        await store.SaveAsync(new ReaderPreferences { ScrollSpeed = .7 }, Token);
        var first = await File.ReadAllBytesAsync(directory.PreferencesPath, Token);
        await store.SaveAsync(new ReaderPreferences { ScrollSpeed = .9 }, Token);
        Assert.Equal(first, await File.ReadAllBytesAsync(directory.BackupPath, Token));
        Assert.Equal(.9, (await store.LoadAsync(Token)).ScrollSpeed);
    }

    [Fact]
    public async Task CorruptMainIsPreservedAndRecoveryKeepsValidBackupOnNextSave()
    {
        using var directory = new TemporaryPreferencesDirectory();
        var initial = new JsonReadingPreferencesStore(directory.Path);
        await initial.SaveAsync(new ReaderPreferences { ScrollSpeed = .7 }, Token);
        await initial.SaveAsync(new ReaderPreferences { ScrollSpeed = .9 }, Token);
        var backup = await File.ReadAllBytesAsync(directory.BackupPath, Token);
        await File.WriteAllTextAsync(directory.PreferencesPath, "broken-json", Token);
        var store = new JsonReadingPreferencesStore(directory.Path);
        var recovered = await store.LoadAsync(Token);
        Assert.Equal(.7, recovered.ScrollSpeed);
        Assert.NotNull(store.LastWarning);
        Assert.Equal("broken-json", await File.ReadAllTextAsync(directory.PreferencesPath, Token));
        var preserved = Assert.Single(Directory.GetFiles(directory.Path, "*.corrupt-*.json"));
        Assert.Equal("broken-json", await File.ReadAllTextAsync(preserved, Token));
        await store.SaveAsync(recovered with { ScrollSpeed = 1 }, Token);
        Assert.Equal(backup, await File.ReadAllBytesAsync(directory.BackupPath, Token));
        Assert.Equal(1, (await new JsonReadingPreferencesStore(directory.Path).LoadAsync(Token)).ScrollSpeed);
        Assert.Equal("broken-json", await File.ReadAllTextAsync(preserved, Token));
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("null")]
    [InlineData("{\"ScrollSpeed\":0}")]
    [InlineData("{\"KeyBindings\":{\"NextPage\":\"Down\"}}")]
    [InlineData("{\"SchemaVersion\":1,\"SchemaVersion\":1}")]
    public async Task InvalidMainAndInvalidBackupUseDefaultsWithWarning(string invalid)
    {
        using var directory = new TemporaryPreferencesDirectory();
        await File.WriteAllTextAsync(directory.PreferencesPath, invalid, Token);
        await File.WriteAllTextAsync(directory.BackupPath, "invalid-backup", Token);
        var store = new JsonReadingPreferencesStore(directory.Path);
        Assert.Equal(.6, (await store.LoadAsync(Token)).ScrollSpeed);
        Assert.NotNull(store.LastWarning);
        Assert.Equal(invalid, await File.ReadAllTextAsync(directory.PreferencesPath, Token));
        Assert.Equal("invalid-backup", await File.ReadAllTextAsync(directory.BackupPath, Token));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FutureSchemaRemainsUntouchedAndRefusesSaveEvenBeforeLoad(bool loadFirst)
    {
        using var directory = new TemporaryPreferencesDirectory();
        const string future = "{\"SchemaVersion\":99,\"ScrollSpeed\":\"future-format\"}";
        await File.WriteAllTextAsync(directory.PreferencesPath, future, Token);
        var store = new JsonReadingPreferencesStore(directory.Path);
        if (loadFirst)
        {
            Assert.Equal(.6, (await store.LoadAsync(Token)).ScrollSpeed);
            Assert.True(store.IsReadOnly);
            Assert.NotNull(store.LastWarning);
        }
        var error = await Assert.ThrowsAsync<ReaderException>(() => store.SaveAsync(new ReaderPreferences(), Token));
        Assert.Equal(ReaderErrorCode.StorageUnavailable, error.Code);
        Assert.True(store.IsReadOnly);
        Assert.Equal(future, await File.ReadAllTextAsync(directory.PreferencesPath, Token));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.corrupt-*.json"));
        Assert.False(File.Exists(directory.BackupPath));
    }

    [Fact]
    public async Task MissingMainCanRecoverBackup()
    {
        using var directory = new TemporaryPreferencesDirectory();
        await File.WriteAllTextAsync(directory.BackupPath, "{\"ScrollSpeed\":1.2}", Token);
        var store = new JsonReadingPreferencesStore(directory.Path);
        Assert.Equal(1.2, (await store.LoadAsync(Token)).ScrollSpeed);
        Assert.NotNull(store.LastWarning);
        Assert.False(File.Exists(directory.PreferencesPath));
    }

    [Fact]
    public async Task ReplacementFailureKeepsOriginalAndBackupAndCleansTemporaryFile()
    {
        using var directory = new TemporaryPreferencesDirectory();
        var store = new JsonReadingPreferencesStore(directory.Path);
        await store.SaveAsync(new ReaderPreferences { ScrollSpeed = .7 }, Token);
        await store.SaveAsync(new ReaderPreferences { ScrollSpeed = .9 }, Token);
        var original = await File.ReadAllBytesAsync(directory.PreferencesPath, Token);
        var backup = await File.ReadAllBytesAsync(directory.BackupPath, Token);
        using (var locked = new FileStream(directory.PreferencesPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var error = await Assert.ThrowsAsync<ReaderException>(() => store.SaveAsync(new ReaderPreferences(), Token));
            Assert.Equal(ReaderErrorCode.StorageUnavailable, error.Code);
        }
        Assert.Equal(original, await File.ReadAllBytesAsync(directory.PreferencesPath, Token));
        Assert.Equal(backup, await File.ReadAllBytesAsync(directory.BackupPath, Token));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public async Task OversizedProfileUsesDefaultsAndPreservesOriginal()
    {
        using var directory = new TemporaryPreferencesDirectory();
        await using (var file = new FileStream(directory.PreferencesPath, FileMode.CreateNew, FileAccess.Write))
            file.SetLength(8L * 1024 * 1024 + 1);
        var store = new JsonReadingPreferencesStore(directory.Path);
        Assert.Equal(.6, (await store.LoadAsync(Token)).ScrollSpeed);
        Assert.NotNull(store.LastWarning);
        Assert.Equal(8L * 1024 * 1024 + 1, new FileInfo(directory.PreferencesPath).Length);
        Assert.Single(Directory.GetFiles(directory.Path, "*.corrupt-*.json"));
    }
}
