using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Reading;
using Xunit;

namespace ComicBookReader.Core.Tests.Reading;

public sealed class ReaderLoadingTests
{
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

    private sealed class FakeSession(string path) : IComicSession
    {
        public bool IsDisposed { get; private set; }
        public BookInfo Book { get; } = new(path, path, 1, DateTime.UnixEpoch);
        public IReadOnlyList<PageInfo> Pages { get; } = [new(0, "0", "0.png"), new(1, "1", "1.png")];
        public ValueTask<PageImage> ReadPageAsync(int pageIndex, CancellationToken cancellationToken)
            => ValueTask.FromResult(new PageImage(new PixelSize(2, 3), 8, new byte[24]));
        public ValueTask DisposeAsync() { IsDisposed = true; return ValueTask.CompletedTask; }
    }
}
