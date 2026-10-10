namespace ComicBookReader.Core.Reading;

public sealed partial class ReaderController
{
    private (double X, double Y)? dragPoint;
    public bool IsDragging => dragPoint is not null;

    public bool BeginDrag(double pointerXDip, double pointerYDip)
    {
        ValidatePointer(pointerXDip, pointerYDip);
        Deactivate();
        if (State.Mode != ReaderMode.Reading || State.Layout is not { } layout ||
            layout.MaxOffsetXDip == 0 && layout.MaxOffsetYDip == 0) return false;
        dragPoint = (pointerXDip, pointerYDip);
        return true;
    }

    public void DragTo(double pointerXDip, double pointerYDip)
    {
        ValidatePointer(pointerXDip, pointerYDip);
        if (dragPoint is not { } previous) return;
        dragPoint = (pointerXDip, pointerYDip);
        ScrollBy(previous.X - pointerXDip, previous.Y - pointerYDip);
    }

    public void EndDrag() => dragPoint = null;

    private static void ValidatePointer(double x, double y)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(x));
    }
}
