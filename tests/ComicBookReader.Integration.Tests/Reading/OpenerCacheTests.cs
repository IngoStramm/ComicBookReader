using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Infrastructure.Archives;
using ComicBookReader.Infrastructure.Imaging;
using ComicBookReader.Integration.Tests.Fixtures;
using SkiaSharp;
using Xunit;

namespace ComicBookReader.Integration.Tests.Reading;

public sealed class OpenerCacheTests
{
    [Fact]
    public async Task NormalOpenRetainsCurrentPageInsteadOfDecodingItAgain()
    {
        using var fixtures = new SyntheticComicFactory();
        var path = fixtures.CreateZip(("1.png", SyntheticComicFactory.Image(SKEncodedImageFormat.Png)));
        var decoder = new CountingDecoder();
        await using var session = await new ComicOpener(decoder).OpenAsync(path, TestContext.Current.CancellationToken);
        using var first = await session.ReadPageAsync(0, TestContext.Current.CancellationToken);
        using var repeated = await session.ReadPageAsync(0, TestContext.Current.CancellationToken);
        Assert.Equal(first.Pixels.ToArray(), repeated.Pixels.ToArray());
        Assert.Equal(1, decoder.Decodes);
    }

    private sealed class CountingDecoder : SkiaPageDecoder
    {
        public int Decodes { get; private set; }
        public override Task<PageImage> DecodeAsync(Stream stream, CancellationToken cancellationToken)
        {
            Decodes++;
            return base.DecodeAsync(stream, cancellationToken);
        }
    }
}
