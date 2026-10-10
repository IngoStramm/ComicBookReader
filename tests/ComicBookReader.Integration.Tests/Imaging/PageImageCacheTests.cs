using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Infrastructure.Imaging;
using Xunit;

namespace ComicBookReader.Integration.Tests.Imaging;

public sealed class PageImageCacheTests
{
    [Fact]
    public void AccountsStrideTimesHeightAndEvictsOldestEntryWithinBudget()
    {
        using var cache = new PageImageCache(24);
        using var first = Image(1, 12, 2);
        using var second = Image(2, 8, 2);
        Assert.True(cache.Store(0, first));
        Assert.Equal(24, cache.ByteCount);
        Assert.True(cache.Store(1, second));
        Assert.Equal(16, cache.ByteCount);
        Assert.False(cache.TryGet(0, out _));
        Assert.True(cache.TryGet(1, out var lease));
        using (lease) Assert.Equal(2, lease!.Pixels.Span[0]);
    }

    [Fact]
    public void EvictionAndCacheDisposalPreserveOutstandingConsumerLeases()
    {
        var cache = new PageImageCache(8);
        using var first = Image(1, 8, 1);
        cache.Store(0, first);
        Assert.True(cache.TryGet(0, out var consumer));
        using var second = Image(2, 8, 1);
        cache.Store(1, second);
        first.Dispose();
        cache.Dispose();
        using (consumer) Assert.Equal(1, consumer!.Pixels.Span[0]);
    }

    [Fact]
    public void RejectsOversizedImagesWithoutEvictingUsefulEntries()
    {
        using var cache = new PageImageCache(8);
        using var small = Image(1, 8, 1);
        using var large = Image(2, 12, 1);
        cache.Store(0, small);
        Assert.False(cache.Store(1, large));
        Assert.Equal(8, cache.ByteCount);
        Assert.True(cache.TryGet(0, out var lease));
        lease!.Dispose();
    }

    [Fact]
    public void PruningKeepsOnlyPreviousAndNextAndExcludesCurrentPage()
    {
        using var cache = new PageImageCache();
        using var image = Image(1, 4, 1);
        for (var index = 0; index < 5; index++) cache.Store(index, image);
        cache.KeepAdjacentTo(2);
        Assert.Equal(8, cache.ByteCount);
        foreach (var index in new[] { 0, 2, 4 }) Assert.False(cache.TryGet(index, out _));
        foreach (var index in new[] { 1, 3 })
        {
            Assert.True(cache.TryGet(index, out var lease));
            lease!.Dispose();
        }
    }

    private static PageImage Image(byte value, int stride, int height) =>
        new(new PixelSize(1, height), stride, Enumerable.Repeat(value, stride * height).ToArray());
}
