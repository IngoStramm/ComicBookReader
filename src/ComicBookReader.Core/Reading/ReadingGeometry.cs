using ComicBookReader.Core.Layout;
using ComicBookReader.Core.Preferences;

namespace ComicBookReader.Core.Reading;

public sealed partial class ReaderController
{
    private ReadingSnapshot? overview;
    private sealed record ReadingSnapshot(PageLayout Layout, double X, double Y, ViewportSize Viewport);

    public LastReadingPosition ReadingPosition
    {
        get
        {
            var page = State.Pages[State.PresentedIndex];
            var layout = overview?.Layout ?? State.Layout!;
            return PreferencesResolver.CapturePosition(page.PageKey, State.PresentedIndex,
                overview?.X ?? State.OffsetXDip, overview?.Y ?? State.OffsetYDip,
                layout.MaxOffsetXDip, layout.MaxOffsetYDip);
        }
    }

    public void ToggleOverview()
    {
        Deactivate();
        if (State.Image is null || State.Mode is not (ReaderMode.Reading or ReaderMode.Overview)) return;
        if (overview is { } saved)
        {
            overview = null;
            State = State with { Mode = ReaderMode.Reading, Layout = saved.Layout, OffsetXDip = saved.X, OffsetYDip = saved.Y };
            if (saved.Viewport != viewport) Reframe(saved.Layout, saved.X, saved.Y, saved.Viewport, null, null);
        }
        else
        {
            overview = new(State.Layout!, State.OffsetXDip, State.OffsetYDip, viewport);
            State = State with { Mode = ReaderMode.Overview,
                Layout = calculator.CalculateOverview(State.Image.Size, viewport), OffsetXDip = 0, OffsetYDip = 0 };
        }
        Changed?.Invoke();
    }

    public void SetWidthRatio(double ratio, double? anchorXDip = null, double? anchorYDip = null)
    {
        if (!double.IsFinite(ratio) || ratio is < .25 or > 2) throw new ArgumentOutOfRangeException(nameof(ratio));
        ChangeGeometry(() => { WidthRatio = ratio; SaveWidthOverride(); }, anchorXDip, anchorYDip);
    }

    public void SetClassification(PageClassificationOverride classification)
    {
        if (!Enum.IsDefined(classification)) throw new ArgumentOutOfRangeException(nameof(classification));
        ChangeGeometry(() => { Classification = classification; SaveClassificationOverride(); }, null, null);
    }

    private void ChangeGeometry(Action change, double? anchorX, double? anchorY)
    {
        if (anchorX is { } x && !double.IsFinite(x) || anchorY is { } y && !double.IsFinite(y))
            throw new ArgumentOutOfRangeException(nameof(anchorX));
        Deactivate();
        var saved = overview ?? (State.Layout is { } layout ? new ReadingSnapshot(layout, State.OffsetXDip, State.OffsetYDip, viewport) : null);
        change();
        overview = null;
        if (saved is null || State.Image is null) return;
        State = State with { Mode = ReaderMode.Reading };
        Reframe(saved.Layout, saved.X, saved.Y, saved.Viewport, anchorX, anchorY);
        Changed?.Invoke();
    }

    private void Reframe(PageLayout old, double x, double y, ViewportSize oldViewport, double? anchorX, double? anchorY)
    {
        if (State.Image is null) return;
        double oldX = anchorX ?? oldViewport.WidthDip / 2, oldY = anchorY ?? oldViewport.HeightDip / 2;
        double imageX = (x + oldX - old.OriginXDip) / old.Scale;
        double imageY = (y + oldY - old.OriginYDip) / old.Scale;
        var layout = ReadingLayout(State.Image);
        double newX = anchorX ?? viewport.WidthDip / 2, newY = anchorY ?? viewport.HeightDip / 2;
        State = State with { Layout = layout,
            OffsetXDip = Math.Clamp(imageX * layout.Scale + layout.OriginXDip - newX, 0, layout.MaxOffsetXDip),
            OffsetYDip = Math.Clamp(imageY * layout.Scale + layout.OriginYDip - newY, 0, layout.MaxOffsetYDip) };
    }
}
