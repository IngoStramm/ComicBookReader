using System.Security.Cryptography;
using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Infrastructure.Archives;
using ComicBookReader.Infrastructure.Imaging;
using Xunit;

namespace ComicBookReader.Integration.Tests.Archives;

public sealed class RarComicSessionTests
{
    [Theory]
    [InlineData("rar4.cbr")]
    [InlineData("rar5.cbr")]
    public async Task NonSolidRarReadsReferencePixelsTwiceAndPreservesArchive(string name)
    {
        using var fixture = new FixtureCopy(name);
        var path = fixture.FilePath;
        var before = HashFile(path);
        var session = await new ComicOpener().OpenAsync(path, TestContext.Current.CancellationToken);
        try
        {
            var page = Assert.Single(session.Pages);
            Assert.Equal(0, page.Index);
            Assert.Equal("jpg/test.jpg", page.EntryName);
            using var first = await session.ReadPageAsync(0, TestContext.Current.CancellationToken);
            using var back = await session.ReadPageAsync(0, TestContext.Current.CancellationToken);
            Assert.Equal(new PixelSize(384, 512), first.Size);
            Assert.Equal(1536, first.Stride);
            Assert.Equal(first.Pixels.ToArray(), back.Pixels.ToArray());
            AssertPixel(first, 0, 0, 0, 108, 94);
            AssertPixel(first, 192, 256, 126, 127, 118);
            AssertPixel(first, 383, 511, 27, 71, 42);
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
                await session.ReadPageAsync(-1, TestContext.Current.CancellationToken));
            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await session.ReadPageAsync(0, canceled.Token));
        }
        finally
        {
            await session.DisposeAsync();
        }
        await session.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await session.ReadPageAsync(0, TestContext.Current.CancellationToken));
        AssertReleasedAndUnchanged(path, before);
    }

    [Theory]
    [InlineData("rar4.cbr")]
    [InlineData("rar5.cbr")]
    public async Task RarContentWithCbzExtensionStillOpens(string name)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cbz");
        File.Copy(FixturePath(name), path);
        try
        {
            await using var session = await new ComicOpener().OpenAsync(path, TestContext.Current.CancellationToken);
            using var image = await session.ReadPageAsync(0, TestContext.Current.CancellationToken);
            Assert.Equal(new PixelSize(384, 512), image.Size);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task RarCloseWaitsForActiveReadAndRejectsQueuedRead()
    {
        using var fixture = new FixtureCopy("rar5.cbr");
        var path = fixture.FilePath;
        var before = HashFile(path);
        var decoder = new GateDecoder();
        var session = await new ComicOpener(decoder, useCache: false).OpenAsync(path, TestContext.Current.CancellationToken);
        var read = session.ReadPageAsync(0, TestContext.Current.CancellationToken).AsTask();
        await decoder.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var queued = session.ReadPageAsync(0, TestContext.Current.CancellationToken).AsTask();
        var close = session.DisposeAsync().AsTask();
        Assert.False(close.IsCompleted);
        decoder.Release.TrySetResult();
        using var image = await read;
        await close;
        await Assert.ThrowsAsync<ObjectDisposedException>(() => queued);
        AssertReleasedAndUnchanged(path, before);
    }

    internal static string FixturePath(string name)
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "ComicBookReader.slnx")))
                return Path.Combine(directory.FullName, "tests", "fixtures", name);
        throw new DirectoryNotFoundException("Could not find solution root for fixtures.");
    }

    internal sealed class FixtureCopy : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "ComicBookReader-tests", Guid.NewGuid().ToString("N"));
        public string FilePath { get; }

        public FixtureCopy(string name)
        {
            Directory.CreateDirectory(directory);
            FilePath = Path.Combine(directory, name);
            File.Copy(FixturePath(name), FilePath);
        }

        public void Dispose() => Directory.Delete(directory, true);
    }

    internal static string HashFile(string path)
    {
        using var file = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(file));
    }

    internal static void AssertReleasedAndUnchanged(string path, string expected)
    {
        using var exclusive = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        Assert.Equal(expected, Convert.ToHexString(SHA256.HashData(exclusive)));
    }

    private static void AssertPixel(PageImage image, int x, int y, int blue, int green, int red)
    {
        var pixel = image.Pixels.Span.Slice(y * image.Stride + x * 4, 4);
        Assert.InRange((int)pixel[0], Math.Max(0, blue - 3), Math.Min(255, blue + 3));
        Assert.InRange((int)pixel[1], Math.Max(0, green - 3), Math.Min(255, green + 3));
        Assert.InRange((int)pixel[2], Math.Max(0, red - 3), Math.Min(255, red + 3));
        Assert.Equal((byte)255, pixel[3]);
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
}
