using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Preferences;
using ComicBookReader.Core.Reading;
using Xunit;

namespace ComicBookReader.Core.Tests.Reading;

public sealed class DragPanTests
{
    [Fact]
    public async Task PageFollowsPointerInBothAxesAndReleaseStopsMovement()
    {
        await using var reader = new ReaderController(new Opener());
        await reader.OpenAsync("A", TestContext.Current.CancellationToken);
        reader.SetWidthRatio(1.5);
        reader.ScrollBy(-double.MaxValue, -double.MaxValue);
        reader.ScrollBy(200, 300);
        reader.PressDirection(ScrollDirection.Down);
        Assert.True(reader.BeginDrag(400, 400));
        Assert.False(reader.HasActiveDirections);
        reader.DragTo(450, 475);
        Assert.Equal(150, reader.State.OffsetXDip);
        Assert.Equal(225, reader.State.OffsetYDip);
        reader.DragTo(350, 350);
        Assert.Equal(250, reader.State.OffsetXDip);
        Assert.Equal(350, reader.State.OffsetYDip);
        reader.EndDrag();
        reader.DragTo(0, 0);
        Assert.Equal(250, reader.State.OffsetXDip);
        Assert.Equal(350, reader.State.OffsetYDip);
        Assert.Equal(0, reader.State.PresentedIndex);
    }

    [Fact]
    public async Task EdgesClampAndReversingPointerMovesImmediatelyWithoutChangingPage()
    {
        await using var reader = new ReaderController(new Opener());
        await reader.OpenAsync("A", TestContext.Current.CancellationToken);
        reader.SetWidthRatio(1.5);
        Assert.True(reader.BeginDrag(300, 300));
        reader.DragTo(10000, 10000);
        Assert.Equal(0, reader.State.OffsetXDip);
        Assert.Equal(0, reader.State.OffsetYDip);
        reader.DragTo(9990, 9980);
        Assert.Equal(10, reader.State.OffsetXDip);
        Assert.Equal(20, reader.State.OffsetYDip);
        reader.DragTo(-10000, -10000);
        Assert.Equal(reader.State.Layout!.MaxOffsetXDip, reader.State.OffsetXDip);
        Assert.Equal(reader.State.Layout.MaxOffsetYDip, reader.State.OffsetYDip);
        reader.DragTo(-9990, -9980);
        Assert.Equal(reader.State.Layout.MaxOffsetXDip - 10, reader.State.OffsetXDip);
        Assert.Equal(reader.State.Layout.MaxOffsetYDip - 20, reader.State.OffsetYDip);
        Assert.Equal(0, reader.State.PresentedIndex);
    }

    [Fact]
    public async Task DragOnlyMovesAxesWithOverflowAndPositionIsSaved()
    {
        await using var reader = new ReaderController(new Opener());
        await reader.OpenAsync("A", TestContext.Current.CancellationToken);
        Assert.True(reader.BeginDrag(100, 100));
        reader.DragTo(20, 40);
        Assert.Equal(0, reader.State.OffsetXDip);
        Assert.Equal(60, reader.State.OffsetYDip);
        Assert.Equal(reader.ReadingPosition, reader.CapturePreferences().Books["A"].Position);
        reader.SetWidthRatio(.25);
        Assert.False(reader.BeginDrag(0, 0));
        reader.ToggleOverview();
        Assert.False(reader.BeginDrag(0, 0));
    }

    [Theory]
    [InlineData("deactivate")]
    [InlineData("overview")]
    [InlineData("width")]
    [InlineData("resize")]
    [InlineData("navigate")]
    [InlineData("open")]
    public async Task ReaderTransitionsCancelTheGesture(string transition)
    {
        await using var reader = new ReaderController(new Opener());
        Assert.False(reader.BeginDrag(0, 0));
        await reader.OpenAsync("A", TestContext.Current.CancellationToken);
        Assert.True(reader.BeginDrag(100, 100));
        switch (transition)
        {
            case "deactivate": reader.Deactivate(); break;
            case "overview": reader.ToggleOverview(); break;
            case "width": reader.ResetBookWidth(); break;
            case "resize": reader.SetViewport(new(800, 600)); break;
            case "navigate": await reader.NavigateAsync(1, TestContext.Current.CancellationToken); break;
            case "open": await reader.OpenAsync("B", TestContext.Current.CancellationToken); break;
        }
        Assert.False(reader.IsDragging);
        var before = reader.State;
        reader.DragTo(0, 0);
        Assert.Equal(before, reader.State);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void InvalidPointerCoordinatesAreRejected(double coordinate)
    {
        var reader = new ReaderController(new Opener());
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.BeginDrag(coordinate, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.BeginDrag(0, coordinate));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.DragTo(coordinate, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.DragTo(0, coordinate));
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
