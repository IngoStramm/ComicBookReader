using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Reading;
using Xunit;

namespace ComicBookReader.Core.Tests.Reading;

public sealed class ReaderLoadingTests
{
    [Fact]
    public async Task CancellationDuringFirstDecodeKeepsPreviousOverviewAndReleasesCandidate()
    {
        using var cancellation = new CancellationTokenSource();
        var candidate = new FakeSession("second", cancellation.Cancel);
        await using var reader = new ReaderController(new ControlledOpener(
            Task.FromResult<IComicSession>(new FakeSession("first")), Task.FromResult<IComicSession>(candidate)));
        await reader.OpenAsync("first", TestContext.Current.CancellationToken);
        reader.ToggleOverview();
        await reader.OpenAsync("second", cancellation.Token);
        Assert.Equal("first", reader.State.Book!.CanonicalPath);
        Assert.Equal(ReaderMode.Overview, reader.State.Mode);
        Assert.True(candidate.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => candidate.LastImage!.Pixels);
    }

    [Fact]
    public async Task OpeningProgressIsVisibleAndAnObsoleteBookCannotChangeIt()
    {
        var opener = new ProgressOpener();
        await using var reader = new ReaderController(opener);
        var first = reader.OpenAsync("first", TestContext.Current.CancellationToken);
        opener.Reports[0]?.Report(new ComicOpenProgress(2, 100));
        Assert.Equal(new ComicOpenProgress(2, 100), reader.State.OpenProgress);
        var second = reader.OpenAsync("second", TestContext.Current.CancellationToken);
        opener.Reports[1]?.Report(new ComicOpenProgress(3, 200));
        opener.Reports[0]?.Report(new ComicOpenProgress(9, 900));
        Assert.Equal(new ComicOpenProgress(3, 200), reader.State.OpenProgress);
        opener.Pending[1].SetResult(new FakeSession("second"));
        await second;
        Assert.Null(reader.State.OpenProgress);
        opener.Pending[0].SetResult(new FakeSession("first"));
        await first;
    }

    private sealed class ProgressOpener : IComicOpener
    {
        public List<IProgress<ComicOpenProgress>?> Reports { get; } = [];
        public List<TaskCompletionSource<IComicSession>> Pending { get; } = [];
        public Task<IComicSession> OpenAsync(string path, CancellationToken cancellationToken,
            IProgress<ComicOpenProgress>? progress = null)
        {
            Reports.Add(progress);
            var source = new TaskCompletionSource<IComicSession>();
            Pending.Add(source);
            return source.Task;
        }
    }

    [Fact]
    public async Task WidthAdjustmentCannotEnableScrollingWhileNewBookIsLoading()
    {
        var pending = new TaskCompletionSource<IComicSession>();
        await using var reader = new ReaderController(new ControlledOpener(
            Task.FromResult<IComicSession>(new FakeSession("first")), pending.Task));
        await reader.OpenAsync("first", TestContext.Current.CancellationToken);
        var opening = reader.OpenAsync("second", TestContext.Current.CancellationToken);
        reader.SetWidthRatio(1.2);
        Assert.Equal(ReaderMode.Loading, reader.State.Mode);
        reader.PressDirection(ScrollDirection.Down);
        Assert.False(reader.HasActiveDirections);
        await reader.ScrollForwardAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ReaderMode.Loading, reader.State.Mode);
        Assert.Equal(0, reader.State.PresentedIndex);
        pending.SetResult(new FakeSession("second"));
        await opening;
    }

    [Fact]
    public async Task LateOpenCannotReplaceNewerBook()
    {
        var first = new TaskCompletionSource<IComicSession>();
        var second = new TaskCompletionSource<IComicSession>();
        var opener = new ControlledOpener(first.Task, second.Task);
        await using var reader = new ReaderController(opener);
        var oldRequest = reader.OpenAsync("first", TestContext.Current.CancellationToken);
        var newRequest = reader.OpenAsync("second", TestContext.Current.CancellationToken);
        var newSession = new FakeSession("second");
        second.SetResult(newSession);
        await newRequest;
        var oldSession = new FakeSession("first");
        first.SetResult(oldSession);
        await oldRequest;
        Assert.Equal("second", reader.State.Book!.CanonicalPath);
        Assert.Equal(ReaderMode.Reading, reader.State.Mode);
        Assert.True(oldSession.IsDisposed);
    }

    [Fact]
    public async Task OpeningFailureLeavesCurrentBookReadable()
    {
        var opener = new ControlledOpener(Task.FromResult<IComicSession>(new FakeSession("first")),
            Task.FromException<IComicSession>(new IOException("unavailable")));
        await using var reader = new ReaderController(opener);
        await reader.OpenAsync("first", TestContext.Current.CancellationToken);
        await reader.OpenAsync("broken", TestContext.Current.CancellationToken);
        Assert.Equal("first", reader.State.Book!.CanonicalPath);
        Assert.Equal(ReaderMode.Reading, reader.State.Mode);
        Assert.NotNull(reader.State.Error);
        Assert.Equal(0, reader.State.PresentedIndex);
    }

    private sealed class ControlledOpener(params Task<IComicSession>[] results) : IComicOpener
    {
        private int next;
        public Task<IComicSession> OpenAsync(string path, CancellationToken cancellationToken,
            IProgress<ComicOpenProgress>? progress = null) => results[next++];
    }

    private sealed class FakeSession(string path, Action? onRead = null) : IComicSession
    {
        public bool IsDisposed { get; private set; }
        public BookInfo Book { get; } = new(path, path, 1, DateTime.UnixEpoch);
        public IReadOnlyList<PageInfo> Pages { get; } = [new(0, "0", "0.png"), new(1, "1", "1.png")];
        public PageImage? LastImage { get; private set; }
        public ValueTask<PageImage> ReadPageAsync(int pageIndex, CancellationToken cancellationToken)
        {
            onRead?.Invoke();
            LastImage = new PageImage(new PixelSize(2, 3), 8, new byte[24]);
            return ValueTask.FromResult(LastImage);
        }
        public ValueTask DisposeAsync() { IsDisposed = true; return ValueTask.CompletedTask; }
    }
}
