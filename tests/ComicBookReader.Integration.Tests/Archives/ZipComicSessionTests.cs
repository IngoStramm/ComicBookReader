using System.Security.Cryptography;
using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Contracts;
using ComicBookReader.Infrastructure.Archives;
using ComicBookReader.Infrastructure.Imaging;
using ComicBookReader.Integration.Tests.Fixtures;
using SkiaSharp;
using Xunit;

namespace ComicBookReader.Integration.Tests.Archives;

public sealed class ZipComicSessionTests
{
    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg, "jpg")]
    [InlineData(SKEncodedImageFormat.Png, "png")]
    [InlineData(SKEncodedImageFormat.Webp, "webp")]
    public async Task ReadsActualFormatsByArchiveSignatureAndCanReturnToPreviousPage(SKEncodedImageFormat format, string extension)
    {
        using var fixture = new SyntheticComicFactory();
        var path = fixture.CreateZip(($"2.{extension}", SyntheticComicFactory.Image(format)),
            ($"1.{extension}", SyntheticComicFactory.Image(format)), ("ComicInfo.xml", [1, 2]));
        await using var session = await new ComicOpener().OpenAsync(path, CancellationToken.None);
        Assert.Equal(2, session.Pages.Count);
        Assert.Equal($"1.{extension}", session.Pages[0].EntryName);
        using var first = await session.ReadPageAsync(0, CancellationToken.None);
        using var second = await session.ReadPageAsync(1, CancellationToken.None);
        using var back = await session.ReadPageAsync(0, CancellationToken.None);
        Assert.Equal(new PixelSize(40, 20), first.Size);
        Assert.Equal(160, first.Stride);
        Assert.Equal(first.Pixels.ToArray(), back.Pixels.ToArray());
        AssertPixel(first, 5, 5, SKColors.Red, 12);
        AssertPixel(first, 35, 15, SKColors.Yellow, 12);
    }

    [Theory]
    [InlineData(1, 40, 20, 0, 1, 2, 3)]
    [InlineData(2, 40, 20, 1, 0, 3, 2)]
    [InlineData(3, 40, 20, 3, 2, 1, 0)]
    [InlineData(4, 40, 20, 2, 3, 0, 1)]
    [InlineData(5, 20, 40, 0, 2, 1, 3)]
    [InlineData(6, 20, 40, 2, 0, 3, 1)]
    [InlineData(7, 20, 40, 3, 1, 2, 0)]
    [InlineData(8, 20, 40, 1, 3, 0, 2)]
    public async Task AppliesAllExifOrientationsToDimensionsAndReferencePixels(int orientation, int width, int height,
        int topLeft, int topRight, int bottomLeft, int bottomRight)
    {
        using var stream = new MemoryStream(SyntheticComicFactory.OrientedJpeg((byte)orientation));
        using var image = await new SkiaPageDecoder().DecodeAsync(stream, CancellationToken.None);
        Assert.Equal(new PixelSize(width, height), image.Size);
        var colors = new[] { SKColors.Red, SKColors.Green, SKColors.Blue, SKColors.Yellow };
        AssertPixel(image, width / 4, height / 4, colors[topLeft], 12);
        AssertPixel(image, width * 3 / 4, height / 4, colors[topRight], 12);
        AssertPixel(image, width / 4, height * 3 / 4, colors[bottomLeft], 12);
        AssertPixel(image, width * 3 / 4, height * 3 / 4, colors[bottomRight], 12);
    }

    [Fact]
    public async Task ReturnsPremultipliedBgraPixels()
    {
        using var stream = new MemoryStream(SyntheticComicFactory.Image(SKEncodedImageFormat.Png, alpha: true));
        using var image = await new SkiaPageDecoder().DecodeAsync(stream, CancellationToken.None);
        Assert.Equal(new byte[] { 25, 50, 100, 128 }, image.Pixels.Span[..4].ToArray());
    }

    [Fact]
    public async Task InvalidPageKeepsItsIndexAndHealthyFollowingPageRemainsReadable()
    {
        using var fixture = new SyntheticComicFactory();
        var path = fixture.CreateZip(("1.png", [1, 2, 3]), ("2.png", SyntheticComicFactory.Image(SKEncodedImageFormat.Png)));
        await using var session = await new ComicOpener().OpenAsync(path, CancellationToken.None);
        var error = await Assert.ThrowsAsync<ReaderException>(async () => await session.ReadPageAsync(0, CancellationToken.None));
        Assert.Equal(ReaderErrorCode.PageDecodeFailed, error.Code);
        Assert.Equal(new[] { 0, 1 }, session.Pages.Select(page => page.Index));
        using var healthy = await session.ReadPageAsync(1, CancellationToken.None);
        Assert.Equal(new PixelSize(40, 20), healthy.Size);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await session.ReadPageAsync(2, CancellationToken.None));
    }

    [Fact]
    public async Task CancellationAndCloseReleaseArchiveWithoutChangingItsHash()
    {
        using var fixture = new SyntheticComicFactory();
        var path = fixture.CreateZip(("../../1.png", SyntheticComicFactory.Image(SKEncodedImageFormat.Png)));
        var before = SHA256.HashData(await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
        var session = await new ComicOpener().OpenAsync(path, CancellationToken.None);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await session.ReadPageAsync(0, canceled.Token));
        using (var image = await session.ReadPageAsync(0, CancellationToken.None)) Assert.Equal(40, image.Size.Width);
        await session.DisposeAsync();
        await session.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await session.ReadPageAsync(0, CancellationToken.None));
        using var exclusive = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        Assert.Equal(before, SHA256.HashData(exclusive));
    }

    [Fact]
    public async Task CloseWaitsForActiveReadAndRejectsQueuedReads()
    {
        using var fixture = new SyntheticComicFactory();
        var path = fixture.CreateZip(("1.png", SyntheticComicFactory.Image(SKEncodedImageFormat.Png)));
        var decoder = new GateDecoder();
        var session = await new ComicOpener(decoder).OpenAsync(path, CancellationToken.None);
        var read = session.ReadPageAsync(0, CancellationToken.None).AsTask();
        await decoder.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var queued = session.ReadPageAsync(0, CancellationToken.None).AsTask();
        var close = session.DisposeAsync().AsTask();
        Assert.False(close.IsCompleted);
        decoder.Release.SetResult();
        using var image = await read;
        await close;
        await Assert.ThrowsAsync<ObjectDisposedException>(() => queued);
        using var exclusive = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
    }

    [Fact]
    public async Task RejectsLargeEncodedInputAndPixelDimensionsBeforeDecodingPixels()
    {
        using var oversized = new DeclaredLargeStream();
        var encodedError = await Assert.ThrowsAsync<ReaderException>(() => new SkiaPageDecoder().DecodeAsync(oversized, CancellationToken.None));
        Assert.Equal(ReaderErrorCode.ResourceLimit, encodedError.Code);
        Assert.Equal(0, oversized.ReadCount);
        using var png = new MemoryStream(SyntheticComicFactory.OversizedPng());
        var dimensionError = await Assert.ThrowsAsync<ReaderException>(() => new SkiaPageDecoder().DecodeAsync(png, CancellationToken.None));
        Assert.Equal(ReaderErrorCode.ResourceLimit, dimensionError.Code);
    }

    [Fact]
    public async Task InvalidArchiveAndMissingFileHaveStableCodes()
    {
        using var fixture = new SyntheticComicFactory();
        var empty = fixture.CreateZip(("info.xml", [1]));
        var noImages = await Assert.ThrowsAsync<ReaderException>(() => new ComicOpener().OpenAsync(empty, CancellationToken.None));
        Assert.Equal(ReaderErrorCode.NoImages, noImages.Code);
        var missing = await Assert.ThrowsAsync<ReaderException>(() => new ComicOpener().OpenAsync(empty + ".missing", CancellationToken.None));
        Assert.Equal(ReaderErrorCode.FileUnavailable, missing.Code);
    }

    private static void AssertPixel(PageImage image, int x, int y, SKColor expected, int tolerance)
    {
        var pixel = image.Pixels.Span.Slice(y * image.Stride + x * 4, 4);
        Assert.InRange((int)pixel[0], Math.Max(0, expected.Blue - tolerance), Math.Min(255, expected.Blue + tolerance));
        Assert.InRange((int)pixel[1], Math.Max(0, expected.Green - tolerance), Math.Min(255, expected.Green + tolerance));
        Assert.InRange((int)pixel[2], Math.Max(0, expected.Red - tolerance), Math.Min(255, expected.Red + tolerance));
        Assert.Equal(expected.Alpha, pixel[3]);
    }

    private sealed class GateDecoder : SkiaPageDecoder
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async Task<PageImage> DecodeAsync(Stream stream, CancellationToken cancellationToken)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return await base.DecodeAsync(stream, cancellationToken);
        }
    }

    private sealed class DeclaredLargeStream : MemoryStream
    {
        public int ReadCount { get; private set; }
        public override long Length => 128L * 1024 * 1024 + 1;
        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCount++;
            return base.Read(buffer, offset, count);
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return base.ReadAsync(buffer, cancellationToken);
        }
    }
}
