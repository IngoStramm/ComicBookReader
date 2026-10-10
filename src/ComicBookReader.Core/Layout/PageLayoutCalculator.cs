using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Preferences;

namespace ComicBookReader.Core.Layout;

public sealed class PageLayoutCalculator : IPageLayoutCalculator
{
    public PageKind Classify(PixelSize image, PageClassificationOverride manualOverride)
    {
        ValidateImage(image);
        return manualOverride switch
        {
            PageClassificationOverride.Auto => image.Width > image.Height ? PageKind.Spread : PageKind.Single,
            PageClassificationOverride.Single => PageKind.Single,
            PageClassificationOverride.Spread => PageKind.Spread,
            _ => throw new ArgumentOutOfRangeException(nameof(manualOverride))
        };
    }

    public PageLayout CalculateReading(PixelSize image, ViewportSize viewport,
        PageKind kind, double preferredPageWidthRatio)
    {
        ValidateImage(image);
        ValidateViewport(viewport);
        if (!double.IsFinite(preferredPageWidthRatio) || preferredPageWidthRatio is < 0.25 or > 2)
            throw new ArgumentOutOfRangeException(nameof(preferredPageWidthRatio));

        double pageCount = kind switch
        {
            PageKind.Single => 1,
            PageKind.Spread => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        double referenceWidthPixels = image.Width / pageCount;
        double scale = preferredPageWidthRatio * (viewport.WidthDip / referenceWidthPixels);
        return CreateLayout(image, viewport, scale);
    }

    public PageLayout CalculateOverview(PixelSize image, ViewportSize viewport)
    {
        ValidateImage(image);
        ValidateViewport(viewport);
        double scale = Math.Min(viewport.WidthDip / image.Width, viewport.HeightDip / image.Height);
        return CreateLayout(image, viewport, scale);
    }

    private static PageLayout CreateLayout(PixelSize image, ViewportSize viewport, double scale)
    {
        double displayWidthDip = image.Width * scale;
        double displayHeightDip = image.Height * scale;
        if (!IsFinitePositive(scale) || !IsFinitePositive(displayWidthDip) || !IsFinitePositive(displayHeightDip))
            throw new ArgumentException("Image and viewport dimensions cannot produce finite positive layout geometry.");

        return new PageLayout(
            scale,
            displayWidthDip,
            displayHeightDip,
            Math.Max(0, displayWidthDip - viewport.WidthDip),
            Math.Max(0, displayHeightDip - viewport.HeightDip),
            Math.Max(0, (viewport.WidthDip - displayWidthDip) / 2),
            Math.Max(0, (viewport.HeightDip - displayHeightDip) / 2));
    }

    private static void ValidateImage(PixelSize image)
    {
        if (image.Width <= 0 || image.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(image));
    }

    private static void ValidateViewport(ViewportSize viewport)
    {
        if (!IsFinitePositive(viewport.WidthDip) || !IsFinitePositive(viewport.HeightDip))
            throw new ArgumentOutOfRangeException(nameof(viewport));
    }

    private static bool IsFinitePositive(double value) => double.IsFinite(value) && value > 0;
}
