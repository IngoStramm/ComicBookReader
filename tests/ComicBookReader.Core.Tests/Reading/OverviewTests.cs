using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Layout;
using ComicBookReader.Core.Preferences;
using ComicBookReader.Core.Reading;
using Xunit;

namespace ComicBookReader.Core.Tests.Reading;

public sealed class OverviewTests
{
    [Fact]
    public async Task OverviewRestoresExactReadingPointAndKeepsItAvailableForPersistence()
    {
        await using var reader = await CreateReader();
        reader.ScrollBy(300, 500);
        var before = reader.State;
        reader.PressDirection(ScrollDirection.Down);
        reader.ToggleOverview();
        Assert.Equal(ReaderMode.Overview, reader.State.Mode);
        Assert.False(reader.HasActiveDirections);
        Assert.True(reader.State.Layout!.DisplayWidthDip <= 900);
        Assert.True(reader.State.Layout.DisplayHeightDip <= 700);
        Assert.Equal(before.OffsetXDip, reader.ReadingPosition.RelativeX * before.Layout!.MaxOffsetXDip, 6);
        Assert.Equal(before.OffsetYDip, reader.ReadingPosition.RelativeY * before.Layout.MaxOffsetYDip, 6);
        reader.ToggleOverview();
        Assert.Equal(before.Layout, reader.State.Layout);
        Assert.Equal(before.OffsetXDip, reader.State.OffsetXDip);
        Assert.Equal(before.OffsetYDip, reader.State.OffsetYDip);
    }

    [Fact]
    public async Task ResizeAndNavigationInvalidateOldGeometryWithoutLosingTheAnchor()
    {
        await using var reader = await CreateReader();
        reader.ScrollBy(300, 300);
        reader.ToggleOverview();
        reader.SetViewport(new ViewportSize(1200, 900));
        reader.ToggleOverview();
        Assert.InRange(reader.State.OffsetXDip, 399.99, 400.01);
        Assert.InRange(reader.State.OffsetYDip, 416.66, 416.67);
        reader.ToggleOverview();
        await reader.NavigateAsync(1, TestContext.Current.CancellationToken);
        Assert.Equal(ReaderMode.Reading, reader.State.Mode);
        Assert.Equal(0, reader.State.OffsetXDip);
        Assert.Equal(0, reader.State.OffsetYDip);
    }

    [Fact]
    public async Task WidthChangePreservesCenterAndEndsOverviewAndManualClassificationWins()
    {
        await using var reader = await CreateReader();
        reader.ScrollBy(300, 300);
        reader.ToggleOverview();
        reader.SetWidthRatio(1.2);
        Assert.Equal(ReaderMode.Reading, reader.State.Mode);
        Assert.InRange(reader.State.OffsetXDip, 449.99, 450.01);
        Assert.InRange(reader.State.OffsetYDip, 429.99, 430.01);
        Assert.Equal(2160, reader.State.Layout!.DisplayWidthDip);
        reader.SetClassification(PageClassificationOverride.Single);
        Assert.Equal(1080, reader.State.Layout!.DisplayWidthDip);
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.SetWidthRatio(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.SetWidthRatio(2.1));
    }

    internal static async Task<ReaderController> CreateReader()
    {
        var reader = new ReaderController(new Opener());
        reader.SetViewport(new ViewportSize(900, 700));
        await reader.OpenAsync("spread", TestContext.Current.CancellationToken);
        return reader;
    }
    private sealed class Opener : IComicOpener
    {
        public Task<IComicSession> OpenAsync(string path, CancellationToken cancellationToken,
            IProgress<ComicOpenProgress>? progress = null) => Task.FromResult<IComicSession>(new Session());
    }
    private sealed class Session : IComicSession
    {
        public BookInfo Book { get; } = new("spread", "spread", 1, DateTime.UnixEpoch);
        public IReadOnlyList<PageInfo> Pages { get; } = [new(0, "0", "0.png"), new(1, "1", "1.png")];
        public ValueTask<PageImage> ReadPageAsync(int index, CancellationToken cancellationToken)
            => ValueTask.FromResult(new PageImage(new PixelSize(200, 150), 800, new byte[120000]));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
