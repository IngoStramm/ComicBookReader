using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Layout;
using ComicBookReader.Core.Reading;
using Xunit;

namespace ComicBookReader.Core.Tests.Reading;

public sealed class ContinuousScrollTests
{
    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public async Task HeldArrowMovesByTimeRegardlessOfFrameRate(int frames)
    {
        await using var reader = await CreateReader();
        reader.PressDirection(ScrollDirection.Down);
        for (int i = 0; i < frames; i++) reader.AdvanceFrame(1d / frames);
        Assert.InRange(reader.State.OffsetYDip, 599.999, 600.001);
        reader.ReleaseDirection(ScrollDirection.Down);
        reader.AdvanceFrame(.05);
        Assert.InRange(reader.State.OffsetYDip, 599.999, 600.001);
    }

    [Fact]
    public async Task RepeatedPressAndEdgesNeverNavigate()
    {
        await using var reader = await CreateReader();
        for (int i = 0; i < 100; i++) reader.PressDirection(ScrollDirection.Down);
        reader.AdvanceFrame(.05);
        Assert.Equal(30, reader.State.OffsetYDip);
        for (int i = 0; i < 1000; i++) reader.AdvanceFrame(.05);
        Assert.Equal(reader.State.Layout!.MaxOffsetYDip, reader.State.OffsetYDip);
        reader.ReleaseDirection(ScrollDirection.Down);
        reader.PressDirection(ScrollDirection.Down);
        reader.AdvanceFrame(.05);
        Assert.Equal(0, reader.State.PresentedIndex);
        reader.Deactivate();
        Assert.False(reader.HasActiveDirections);
    }

    [Fact]
    public async Task OppositeDirectionsCancelAndLargePauseIsClamped()
    {
        await using var reader = await CreateReader();
        reader.PressDirection(ScrollDirection.Down);
        reader.PressDirection(ScrollDirection.Up);
        reader.AdvanceFrame(.05);
        Assert.Equal(0, reader.State.OffsetYDip);
        reader.ReleaseDirection(ScrollDirection.Up);
        reader.AdvanceFrame(10);
        Assert.Equal(30, reader.State.OffsetYDip);
    }

    [Theory]
    [InlineData(.5, 0)]
    [InlineData(1, 0)]
    [InlineData(1.5, 30)]
    public async Task HorizontalArrowsMoveOnlyWithWidthOverflowAndNeverNavigate(double width, double movement)
    {
        await using var reader = await CreateReader();
        reader.SetWidthRatio(width);
        reader.ScrollBy(-double.MaxValue, 0);
        double y = reader.State.OffsetYDip;
        reader.PressDirection(ScrollDirection.Right);
        reader.AdvanceFrame(.05);
        Assert.Equal(movement, reader.State.OffsetXDip);
        Assert.Equal(y, reader.State.OffsetYDip);
        reader.ScrollBy(double.MaxValue, 0);
        reader.AdvanceFrame(.05);
        Assert.Equal(reader.State.Layout!.MaxOffsetXDip, reader.State.OffsetXDip);
        Assert.Equal(0, reader.State.PresentedIndex);
        reader.ReleaseDirection(ScrollDirection.Right);
        reader.ScrollBy(-double.MaxValue, 0);
        reader.PressDirection(ScrollDirection.Left);
        reader.AdvanceFrame(.05);
        Assert.Equal(0, reader.State.OffsetXDip);
        Assert.Equal(0, reader.State.PresentedIndex);
    }

    private static async Task<ReaderController> CreateReader()
    {
        var reader = new ReaderController(new Opener());
        reader.SetViewport(new ViewportSize(900, 1000));
        await reader.OpenAsync("book", TestContext.Current.CancellationToken);
        return reader;
    }

    [Fact]
    public async Task DiagonalUsesTheSameTotalSpeedAndLoadingClearsHeldKeys()
    {
        await using var reader = await CreateReader();
        reader.PressDirection(ScrollDirection.Down);
        reader.PressDirection(ScrollDirection.Right);
        reader.AdvanceFrame(.05);
        Assert.InRange(reader.State.OffsetYDip, 21.213, 21.214);
        await reader.OpenAsync("next", TestContext.Current.CancellationToken);
        Assert.False(reader.HasActiveDirections);
        reader.AdvanceFrame(.05);
        Assert.Equal(0, reader.State.OffsetYDip);
    }
    private sealed class Opener : IComicOpener
    {
        public Task<IComicSession> OpenAsync(string path, CancellationToken cancellationToken,
            IProgress<ComicOpenProgress>? progress = null) => Task.FromResult<IComicSession>(new Session(path));
    }
    private sealed class Session(string path) : IComicSession
    {
        public BookInfo Book { get; } = new(path, path, 1, DateTime.UnixEpoch);
        public IReadOnlyList<PageInfo> Pages { get; } = [new(0, "0", "0.png")];
        public ValueTask<PageImage> ReadPageAsync(int index, CancellationToken cancellationToken)
            => ValueTask.FromResult(new PageImage(new PixelSize(10, 40), 40, new byte[1600]));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
