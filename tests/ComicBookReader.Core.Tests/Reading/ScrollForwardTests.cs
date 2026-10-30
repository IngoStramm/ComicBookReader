using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Layout;
using ComicBookReader.Core.Preferences;
using ComicBookReader.Core.Reading;
using Xunit;

namespace ComicBookReader.Core.Tests.Reading;

public sealed class ScrollForwardTests
{
    [Theory]
    [InlineData(10, 100)]
    [InlineData(45, 450)]
    [InlineData(100, 1000)]
    public async Task ForwardScrollUsesTheConfiguredViewportPercentage(int percent, double distance)
    {
        await using var reader = await CreateReader();
        reader.ApplyPreferences(reader.CapturePreferences() with { SpaceScrollPercent = percent });
        await reader.ScrollForwardAsync(TestContext.Current.CancellationToken);
        Assert.Equal(distance, reader.State.OffsetYDip);
        Assert.Equal(0, reader.State.PresentedIndex);
    }
    [Fact]
    public async Task ForwardScrollFinishesTheRowBeforeDescendingAndTheBottomBeforeChangingPage()
    {
        await using var reader = await CreateReader();
        reader.SetWidthRatio(1.5);
        reader.ScrollBy(120, -double.MaxValue);
        double x = reader.State.OffsetXDip;
        reader.PressDirection(ScrollDirection.Right);
        await reader.ScrollForwardAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, reader.State.OffsetYDip);
        Assert.Equal(reader.State.Layout!.MaxOffsetXDip, reader.State.OffsetXDip);
        Assert.True(reader.State.OffsetXDip > x);
        await reader.ScrollForwardAsync(TestContext.Current.CancellationToken);
        Assert.Equal(900, reader.State.OffsetYDip);
        Assert.Equal(0, reader.State.OffsetXDip);
        Assert.Equal(0, reader.State.PresentedIndex);
        Assert.False(reader.HasActiveDirections);

        reader.ScrollBy(double.MaxValue, reader.State.Layout!.MaxOffsetYDip - reader.State.OffsetYDip - 100);
        await reader.ScrollForwardAsync(TestContext.Current.CancellationToken);
        Assert.Equal(reader.State.Layout.MaxOffsetYDip, reader.State.OffsetYDip);
        Assert.Equal(0, reader.State.PresentedIndex);
        Assert.Equal(0, reader.State.OffsetXDip);
        await reader.ScrollForwardAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, reader.State.PresentedIndex);
        Assert.Equal(reader.State.Layout.MaxOffsetXDip, reader.State.OffsetXDip);
        await reader.ScrollForwardAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, reader.State.PresentedIndex);
        Assert.Equal(0, reader.State.OffsetXDip);
        Assert.Equal(0, reader.State.OffsetYDip);
    }

    [Fact]
    public async Task WidePageIsReadAcrossEachRowBeforeDescendingOrAdvancing()
    {
        await using var reader = new ReaderController(new WideOpener(), preferences: new ReaderPreferences { PageClassification = PageClassificationOverride.Spread });
        reader.SetViewport(new ViewportSize(100, 80));
        await reader.OpenAsync("wide", TestContext.Current.CancellationToken);
        Assert.Equal(100, reader.State.Layout!.MaxOffsetXDip);
        Assert.Equal(120, reader.State.Layout.MaxOffsetYDip);
        (double X, double Y)[] expected = [(90, 0), (100, 0), (0, 72), (90, 72), (100, 72), (0, 120), (90, 120), (100, 120)];
        foreach (var point in expected)
        {
            await reader.ScrollForwardAsync(TestContext.Current.CancellationToken);
            Assert.Equal(point.X, reader.State.OffsetXDip);
            Assert.Equal(point.Y, reader.State.OffsetYDip);
            Assert.Equal(0, reader.State.PresentedIndex);
        }
        await reader.ScrollForwardAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, reader.State.PresentedIndex);
        Assert.Equal(0, reader.State.OffsetXDip);
        Assert.Equal(0, reader.State.OffsetYDip);
    }

    [Fact]
    public async Task HorizontalOnlyOverflowMustBeReadBeforeAdvancing()
    {
        await using var reader = new ReaderController(new WideOpener(), preferences: new ReaderPreferences { PageClassification = PageClassificationOverride.Spread });
        reader.SetViewport(new ViewportSize(100, 300));
        await reader.OpenAsync("wide", TestContext.Current.CancellationToken);
        await reader.ScrollForwardAsync(TestContext.Current.CancellationToken);
        Assert.Equal(90, reader.State.OffsetXDip);
        Assert.Equal(0, reader.State.PresentedIndex);
        await reader.ScrollForwardAsync(TestContext.Current.CancellationToken);
        Assert.Equal(100, reader.State.OffsetXDip);
        await reader.ScrollForwardAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, reader.State.PresentedIndex);
    }

    [Fact]
    public async Task PageThatFitsAdvancesAndTheLastPageRemainsAtTheBottom()
    {
        await using var reader = await CreateReader();
        reader.SetWidthRatio(.25);
        Assert.Equal(0, reader.State.Layout!.MaxOffsetYDip);
        await reader.ScrollForwardAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, reader.State.PresentedIndex);
        reader.SetWidthRatio(1);
        reader.ScrollBy(0, double.MaxValue);
        var last = reader.State;
        await reader.ScrollForwardAsync(TestContext.Current.CancellationToken);
        Assert.Equal(last, reader.State);
    }

    [Fact]
    public async Task OverviewAdvancesToReadingAndAnEmptyReaderIgnoresForwardScroll()
    {
        await using var reader = new ReaderController(new Opener());
        await reader.ScrollForwardAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ReaderMode.Empty, reader.State.Mode);
        await reader.OpenAsync("book", TestContext.Current.CancellationToken);
        reader.ToggleOverview();
        await reader.ScrollForwardAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ReaderMode.Reading, reader.State.Mode);
        Assert.Equal(1, reader.State.PresentedIndex);
    }

    private static async Task<ReaderController> CreateReader()
    {
        var reader = new ReaderController(new Opener());
        reader.SetViewport(new ViewportSize(900, 1000));
        await reader.OpenAsync("book", TestContext.Current.CancellationToken);
        return reader;
    }

    private sealed class Opener : IComicOpener
    {
        public Task<IComicSession> OpenAsync(string path, CancellationToken cancellationToken,
            IProgress<ComicOpenProgress>? progress = null) => Task.FromResult<IComicSession>(new Session());
    }

    private sealed class Session : IComicSession
    {
        public BookInfo Book { get; } = new("book", "book", 1, DateTime.UnixEpoch);
        public IReadOnlyList<PageInfo> Pages { get; } = [new(0, "0", "0.png"), new(1, "1", "1.png")];
        public ValueTask<PageImage> ReadPageAsync(int index, CancellationToken cancellationToken)
            => ValueTask.FromResult(new PageImage(new PixelSize(10, 40), 40, new byte[1600]));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class WideOpener : IComicOpener
    {
        public Task<IComicSession> OpenAsync(string path, CancellationToken cancellationToken,
            IProgress<ComicOpenProgress>? progress = null) => Task.FromResult<IComicSession>(new WideSession());
    }
    private sealed class WideSession : IComicSession
    {
        public BookInfo Book { get; } = new("wide", "wide", 1, DateTime.UnixEpoch);
        public IReadOnlyList<PageInfo> Pages { get; } = [new(0, "0", "0.png"), new(1, "1", "1.png")];
        public ValueTask<PageImage> ReadPageAsync(int index, CancellationToken cancellationToken)
            => ValueTask.FromResult(new PageImage(new PixelSize(200, 200), 800, new byte[160000]));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
