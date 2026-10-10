using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Preferences;
using ComicBookReader.Core.Reading;
using Xunit;

namespace ComicBookReader.Core.Tests.Reading;

public sealed class ReadingPreferencesTests
{
    [Fact]
    public async Task ReopenRestoresBookWidthPageClassificationAndReadingPosition()
    {
        await using var first = new ReaderController(new Opener());
        await first.OpenAsync("A", TestContext.Current.CancellationToken);
        await first.NavigateAsync(1, TestContext.Current.CancellationToken);
        first.SetWidthRatio(1.2);
        first.SetClassification(PageClassificationOverride.Spread);
        first.ScrollBy(300, 500);
        var point = first.ReadingPosition;
        first.ToggleOverview();
        var saved = first.CapturePreferences();
        await using var restored = new ReaderController(new Opener(), preferences: saved);
        await restored.OpenAsync("A", TestContext.Current.CancellationToken);
        Assert.Equal(1, restored.State.PresentedIndex);
        Assert.Equal(1.2, restored.WidthRatio);
        Assert.Equal(PageClassificationOverride.Spread, restored.Classification);
        Assert.Equal(point, restored.ReadingPosition);
        Assert.Equal(ReaderMode.Reading, restored.State.Mode);
        await restored.OpenAsync("B", TestContext.Current.CancellationToken);
        Assert.Equal(1, restored.WidthRatio);
        Assert.Equal(PageClassificationOverride.Spread, restored.Classification);
        Assert.Equal(0, restored.State.PresentedIndex);
        await restored.NavigateAsync(1, TestContext.Current.CancellationToken);
        Assert.Equal(PageClassificationOverride.Spread, restored.Classification);
        restored.SetClassification(PageClassificationOverride.Single);
        Assert.Equal(PageClassificationOverride.Single, restored.CapturePreferences().PageClassification);
    }

    [Fact]
    public async Task PageKeyWinsOverOldIndexAndMissingPageUsesClampedFallback()
    {
        var prefs = new ReaderPreferences
        {
            Books = new()
            {
                ["A"] = new BookPreferences { Position = new("second", 0, 0, .5) }
            }
        };
        await using var reader = new ReaderController(new Opener(), preferences: prefs);
        await reader.OpenAsync("A", TestContext.Current.CancellationToken);
        Assert.Equal(1, reader.State.PresentedIndex);
        var snapshot = reader.CapturePreferences();
        snapshot.Books["A"] = snapshot.Books["A"] with { Position = new("missing", 100, 0, 1) };
        await using var fallback = new ReaderController(new Opener(), preferences: snapshot);
        await fallback.OpenAsync("A", TestContext.Current.CancellationToken);
        Assert.Equal(1, fallback.State.PresentedIndex);
        Assert.Equal(fallback.State.Layout!.MaxOffsetYDip, fallback.State.OffsetYDip);
    }

    private sealed class Opener : IComicOpener
    {
        public Task<IComicSession> OpenAsync(string path, CancellationToken cancellationToken,
            IProgress<ComicOpenProgress>? progress = null) => Task.FromResult<IComicSession>(new Session(path));
    }
    private sealed class Session(string path) : IComicSession
    {
        public BookInfo Book { get; } = new(path, path, 1, DateTime.UnixEpoch);
        public IReadOnlyList<PageInfo> Pages { get; } = [new(0, "first", "1.png"), new(1, "second", "2.png")];
        public ValueTask<PageImage> ReadPageAsync(int index, CancellationToken cancellationToken)
            => ValueTask.FromResult(new PageImage(new PixelSize(10, 30), 40, new byte[1200]));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
