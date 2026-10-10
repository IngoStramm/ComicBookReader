using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Layout;
using ComicBookReader.Core.Preferences;
using Xunit;

namespace ComicBookReader.Core.Tests.Layout;

public sealed class PageLayoutCalculatorTests
{
    private readonly IPageLayoutCalculator calculator = new PageLayoutCalculator();

    [Theory]
    [InlineData(1000, 1500, PageClassificationOverride.Auto, PageKind.Single)]
    [InlineData(1000, 1000, PageClassificationOverride.Auto, PageKind.Single)]
    [InlineData(2000, 1500, PageClassificationOverride.Auto, PageKind.Spread)]
    [InlineData(2000, 1500, PageClassificationOverride.Single, PageKind.Single)]
    [InlineData(1000, 1500, PageClassificationOverride.Spread, PageKind.Spread)]
    [InlineData(1000, 1000, PageClassificationOverride.Spread, PageKind.Spread)]
    public void ClassificationUsesOrientationUnlessManuallyOverridden(
        int width, int height, PageClassificationOverride manualOverride, PageKind expected)
    {
        Assert.Equal(expected, calculator.Classify(new PixelSize(width, height), manualOverride));
    }

    [Theory]
    [InlineData(1000, 1500, PageKind.Single, 1, 0.9, 900, 1350, 0, 550, 0, 0)]
    [InlineData(2000, 1500, PageKind.Spread, 1, 0.9, 1800, 1350, 900, 550, 0, 0)]
    [InlineData(4000, 3000, PageKind.Spread, 1, 0.45, 1800, 1350, 900, 550, 0, 0)]
    [InlineData(2000, 1500, PageKind.Single, 1, 0.45, 900, 675, 0, 0, 0, 62.5)]
    [InlineData(1000, 1500, PageKind.Single, 0.8, 0.72, 720, 1080, 0, 280, 90, 0)]
    public void ReadingMatchesIndependentSpecExamples(
        int width, int height, PageKind kind, double ratio, double scale,
        double displayWidth, double displayHeight, double maxOffsetX, double maxOffsetY,
        double originX, double originY)
    {
        var result = calculator.CalculateReading(new PixelSize(width, height), new ViewportSize(900, 800), kind, ratio);

        AssertLayout(result, new PageLayout(scale, displayWidth, displayHeight,
            maxOffsetX, maxOffsetY, originX, originY));
    }

    [Fact]
    public void DoubledPortraitResolutionPreservesVisualSizeWithoutFittingHeight()
    {
        var viewport = new ViewportSize(900, 200);
        var original = calculator.CalculateReading(new PixelSize(1000, 1500), viewport, PageKind.Single, 1);
        var doubled = calculator.CalculateReading(new PixelSize(2000, 3000), viewport, PageKind.Single, 1);

        AssertLayout(original, new PageLayout(0.9, 900, 1350, 0, 1150, 0, 0));
        AssertLayout(doubled, new PageLayout(0.45, 900, 1350, 0, 1150, 0, 0));
    }

    [Fact]
    public void SpreadWithOddPixelWidthUsesFractionalReferenceWidth()
    {
        var result = calculator.CalculateReading(new PixelSize(3, 2), new ViewportSize(9, 6), PageKind.Spread, 1);

        AssertLayout(result, new PageLayout(6, 18, 12, 9, 6, 0, 0));
    }

    [Theory]
    [InlineData(1000, 500, PageKind.Single, 0.5, 0.5, 500, 250, 0, 0, 250, 375)]
    [InlineData(2000, 500, PageKind.Spread, 1, 1, 2000, 500, 1000, 0, 0, 250)]
    [InlineData(1000, 2000, PageKind.Single, 0.75, 0.75, 750, 1500, 0, 500, 125, 0)]
    [InlineData(2000, 1500, PageKind.Spread, 1, 1, 2000, 1500, 1000, 500, 0, 0)]
    public void ReadingCentersOnlyAxesWithoutOverflow(
        int width, int height, PageKind kind, double ratio, double scale,
        double displayWidth, double displayHeight, double maxOffsetX, double maxOffsetY,
        double originX, double originY)
    {
        var result = calculator.CalculateReading(new PixelSize(width, height), new ViewportSize(1000, 1000), kind, ratio);

        AssertLayout(result, new PageLayout(scale, displayWidth, displayHeight,
            maxOffsetX, maxOffsetY, originX, originY));
    }

    [Theory]
    [InlineData(1000, 1500, 1200, 600, 0.4, 400, 600, 400, 0)]
    [InlineData(2000, 1500, 900, 1800, 0.45, 900, 675, 0, 562.5)]
    public void OverviewContainsAndCentersTheWholeImage(
        int width, int height, double viewportWidth, double viewportHeight,
        double scale, double displayWidth, double displayHeight, double originX, double originY)
    {
        var viewport = new ViewportSize(viewportWidth, viewportHeight);
        var result = calculator.CalculateOverview(new PixelSize(width, height), viewport);

        AssertLayout(result, new PageLayout(scale, displayWidth, displayHeight, 0, 0, originX, originY));
        Assert.True(result.DisplayWidthDip <= viewport.WidthDip);
        Assert.True(result.DisplayHeightDip <= viewport.HeightDip);
    }

    [Fact]
    public void OverviewDoesNotChangeSubsequentReadingGeometry()
    {
        var image = new PixelSize(2000, 1500);
        var viewport = new ViewportSize(900, 800);
        var before = calculator.CalculateReading(image, viewport, PageKind.Spread, 0.8);

        calculator.CalculateOverview(image, viewport);

        Assert.Equal(before, calculator.CalculateReading(image, viewport, PageKind.Spread, 0.8));
    }

    [Fact]
    public void ViewportChangeRecalculatesScaleAndOverflowInDips()
    {
        var image = new PixelSize(1000, 1500);
        var result = calculator.CalculateReading(image, new ViewportSize(600, 800), PageKind.Single, 1);

        AssertLayout(result, new PageLayout(0.6, 600, 900, 0, 100, 0, 0));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    public void AllOperationsRejectNonpositiveImageDimensions(int width, int height)
    {
        var image = new PixelSize(width, height);
        var viewport = new ViewportSize(900, 800);

        Assert.ThrowsAny<ArgumentException>(() => calculator.Classify(image, PageClassificationOverride.Auto));
        Assert.ThrowsAny<ArgumentException>(() => calculator.CalculateReading(image, viewport, PageKind.Single, 1));
        Assert.ThrowsAny<ArgumentException>(() => calculator.CalculateOverview(image, viewport));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void BothLayoutModesRejectInvalidViewportDimensions(double invalid)
    {
        var image = new PixelSize(1000, 1500);
        var invalidWidth = new ViewportSize(invalid, 800);
        var invalidHeight = new ViewportSize(900, invalid);

        Assert.ThrowsAny<ArgumentException>(() => calculator.CalculateReading(image, invalidWidth, PageKind.Single, 1));
        Assert.ThrowsAny<ArgumentException>(() => calculator.CalculateReading(image, invalidHeight, PageKind.Single, 1));
        Assert.ThrowsAny<ArgumentException>(() => calculator.CalculateOverview(image, invalidWidth));
        Assert.ThrowsAny<ArgumentException>(() => calculator.CalculateOverview(image, invalidHeight));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.249)]
    [InlineData(2.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ReadingRejectsRatioOutsideValidatedPreferenceRange(double ratio)
    {
        Assert.ThrowsAny<ArgumentException>(() => calculator.CalculateReading(
            new PixelSize(1000, 1500), new ViewportSize(900, 800), PageKind.Single, ratio));
    }

    [Theory]
    [InlineData(0.25, 0.225, 225, 337.5, 0, 0, 337.5, 231.25)]
    [InlineData(2, 1.8, 1800, 2700, 900, 1900, 0, 0)]
    public void ReadingAcceptsBothRatioBoundaries(double ratio, double scale,
        double width, double height, double maxOffsetX, double maxOffsetY, double originX, double originY)
    {
        var result = calculator.CalculateReading(new PixelSize(1000, 1500), new ViewportSize(900, 800), PageKind.Single, ratio);

        AssertLayout(result, new PageLayout(scale, width, height, maxOffsetX, maxOffsetY, originX, originY));
    }

    [Fact]
    public void UndefinedClassificationValuesAreRejected()
    {
        var image = new PixelSize(1000, 1500);

        Assert.ThrowsAny<ArgumentException>(() => calculator.Classify(image, (PageClassificationOverride)99));
        Assert.ThrowsAny<ArgumentException>(() => calculator.CalculateReading(image, new ViewportSize(900, 800), (PageKind)99, 1));
    }

    [Fact]
    public void ReadingRejectsGeometryThatCannotBeRepresentedAsFiniteDips()
    {
        Assert.ThrowsAny<ArgumentException>(() => calculator.CalculateReading(
            new PixelSize(2, 1), new ViewportSize(double.MaxValue, 800), PageKind.Spread, 2));
    }

    private static void AssertLayout(PageLayout actual, PageLayout expected)
    {
        Assert.InRange(actual.Scale, expected.Scale - 0.000001, expected.Scale + 0.000001);
        Assert.InRange(actual.DisplayWidthDip, expected.DisplayWidthDip - 0.001, expected.DisplayWidthDip + 0.001);
        Assert.InRange(actual.DisplayHeightDip, expected.DisplayHeightDip - 0.001, expected.DisplayHeightDip + 0.001);
        Assert.InRange(actual.MaxOffsetXDip, expected.MaxOffsetXDip - 0.001, expected.MaxOffsetXDip + 0.001);
        Assert.InRange(actual.MaxOffsetYDip, expected.MaxOffsetYDip - 0.001, expected.MaxOffsetYDip + 0.001);
        Assert.InRange(actual.OriginXDip, expected.OriginXDip - 0.001, expected.OriginXDip + 0.001);
        Assert.InRange(actual.OriginYDip, expected.OriginYDip - 0.001, expected.OriginYDip + 0.001);
    }
}
