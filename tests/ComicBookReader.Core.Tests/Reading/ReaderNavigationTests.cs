using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Reading;
using Xunit;

namespace ComicBookReader.Core.Tests.Reading;

public sealed class ReaderNavigationTests
{
    [Fact]
    public async Task ExtremeNavigationReachesFirstAndLastPages()
    {
        await using var reader = new ReaderController(new TestOpener(new TestSession(false)));
        await reader.OpenAsync("book", TestContext.Current.CancellationToken);
        await reader.NavigateAsync(int.MaxValue, TestContext.Current.CancellationToken);
        Assert.Equal(2, reader.State.PresentedIndex);
        await reader.NavigateAsync(int.MinValue, TestContext.Current.CancellationToken);
        Assert.Equal(0, reader.State.PresentedIndex);
    }

    [Fact]
    public async Task CanceledNavigationThatIgnoresTokenKeepsOverviewAndDisposesReturnedPixels()
    {
        var session = new TestSession();
        await using var reader = new ReaderController(new TestOpener(session));
        await reader.OpenAsync("book", TestContext.Current.CancellationToken);
        reader.ScrollBy(0, 100);
        var point = reader.ReadingPosition;
        reader.ToggleOverview();
        using var cancellation = new CancellationTokenSource();
        var pending = reader.NavigateAsync(1, cancellation.Token);
        cancellation.Cancel();
        var obsolete = TestSession.Image();
        session.Delayed.SetResult(obsolete);
        await pending;
        Assert.Equal(ReaderMode.Overview, reader.State.Mode);
        Assert.Equal(0, reader.State.PresentedIndex);
        Assert.Equal(point, reader.ReadingPosition);
        Assert.Throws<ObjectDisposedException>(() => obsolete.Pixels);
    }

    [Fact]
    public async Task LatePageResultCannotReplaceNewerTargetAndIsDisposed()
    {
        var session = new TestSession();
        await using var reader = new ReaderController(new TestOpener(session));
        await reader.OpenAsync("book", TestContext.Current.CancellationToken);
        var first = reader.NavigateAsync(1, TestContext.Current.CancellationToken);
        await reader.NavigateAsync(1, TestContext.Current.CancellationToken);
        var late = TestSession.Image();
        session.Delayed.SetResult(late);
        await first;
        Assert.Equal(2, reader.State.PresentedIndex);
        Assert.Equal(2, reader.State.TargetIndex);
        Assert.Throws<ObjectDisposedException>(() => late.Pixels);
    }

    [Fact]
    public async Task BookLimitsKeepSamePageAndNextPageStartsAtOrigin()
    {
        var session = new TestSession(false);
        await using var reader = new ReaderController(new TestOpener(session));
        await reader.OpenAsync("book", TestContext.Current.CancellationToken);
        await reader.NavigateAsync(-1, TestContext.Current.CancellationToken);
        Assert.Equal(0, reader.State.PresentedIndex);
        await reader.NavigateAsync(1, TestContext.Current.CancellationToken);
        Assert.Equal(1, reader.State.PresentedIndex);
        Assert.Equal(0, reader.State.OffsetXDip);
        Assert.Equal(0, reader.State.OffsetYDip);
        await reader.NavigateAsync(50, TestContext.Current.CancellationToken);
        Assert.Equal(2, reader.State.PresentedIndex);
        await reader.NavigateAsync(1, TestContext.Current.CancellationToken);
        Assert.Equal(2, reader.State.PresentedIndex);
    }

    private sealed class TestOpener(IComicSession session) : IComicOpener
    {
        public Task<IComicSession> OpenAsync(string path, CancellationToken cancellationToken,
            IProgress<ComicOpenProgress>? progress = null) => Task.FromResult(session);
    }

    private sealed class TestSession(bool delay = true) : IComicSession
    {
        public TaskCompletionSource<PageImage> Delayed { get; } = new();
        public BookInfo Book { get; } = new("book", "book", 1, DateTime.UnixEpoch);
        public IReadOnlyList<PageInfo> Pages { get; } = [new(0, "0", "0.png"), new(1, "1", "1.png"), new(2, "2", "2.png")];
        public static PageImage Image() => new(new PixelSize(100, 300), 400, new byte[120000]);
        public ValueTask<PageImage> ReadPageAsync(int index, CancellationToken cancellationToken)
            => index == 1 && delay ? new(Delayed.Task) : ValueTask.FromResult(Image());
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
