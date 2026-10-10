using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Preferences;

namespace ComicBookReader.Core.Layout;

public enum PageKind { Single, Spread }

public readonly record struct ViewportSize(double WidthDip, double HeightDip);

public sealed record PageLayout(
    double Scale,
    double DisplayWidthDip,
    double DisplayHeightDip,
    double MaxOffsetXDip,
    double MaxOffsetYDip,
    double OriginXDip,
    double OriginYDip);

public interface IPageLayoutCalculator
{
    PageKind Classify(PixelSize image, PageClassificationOverride manualOverride);
    PageLayout CalculateReading(PixelSize image, ViewportSize viewport,
        PageKind kind, double preferredPageWidthRatio);
    PageLayout CalculateOverview(PixelSize image, ViewportSize viewport);
}
