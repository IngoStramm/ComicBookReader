namespace ComicBookReader.Core.Reading;

public enum ScrollDirection { Up, Down, Left, Right }

public sealed partial class ReaderController
{
    private readonly HashSet<ScrollDirection> directions = [];
    public bool HasActiveDirections => directions.Count > 0;
    public double ScrollSpeed { get; private set; } = .6;

    public void SetScrollSpeed(double speed)
    {
        if (!double.IsFinite(speed) || speed is < .1 or > 2)
            throw new ArgumentOutOfRangeException(nameof(speed));
        ScrollSpeed = speed;
    }

    public void PressDirection(ScrollDirection direction)
    {
        if (State.Mode == ReaderMode.Reading && Enum.IsDefined(direction)) directions.Add(direction);
    }

    public void ReleaseDirection(ScrollDirection direction) => directions.Remove(direction);
    public void Deactivate() => directions.Clear();

    public Task ScrollForwardAsync(CancellationToken cancellationToken = default)
    {
        Deactivate();
        if (State.Mode == ReaderMode.Overview) return NavigateAsync(1, cancellationToken);
        if (State.Mode != ReaderMode.Reading || State.Layout is not { } layout) return Task.CompletedTask;
        if (State.OffsetYDip >= layout.MaxOffsetYDip) return NavigateAsync(1, cancellationToken);
        ScrollBy(0, viewport.HeightDip * profile.SpaceScrollPercent / 100);
        return Task.CompletedTask;
    }

    public void AdvanceFrame(double elapsedSeconds)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        if (State.Mode != ReaderMode.Reading || !HasActiveDirections) return;
        double x = (directions.Contains(ScrollDirection.Right) ? 1 : 0) -
            (directions.Contains(ScrollDirection.Left) ? 1 : 0);
        double y = (directions.Contains(ScrollDirection.Down) ? 1 : 0) -
            (directions.Contains(ScrollDirection.Up) ? 1 : 0);
        double magnitude = Math.Sqrt(x * x + y * y);
        if (magnitude == 0) return;
        double distance = ScrollSpeed * viewport.HeightDip * Math.Min(elapsedSeconds, .05);
        ScrollBy(x / magnitude * distance, y / magnitude * distance);
    }

    public void ScrollBy(double xDip, double yDip)
    {
        if (!double.IsFinite(xDip) || !double.IsFinite(yDip)) throw new ArgumentOutOfRangeException(nameof(xDip));
        if (State.Mode != ReaderMode.Reading || State.Layout is not { } layout) return;
        double x = Math.Clamp(State.OffsetXDip + xDip, 0, layout.MaxOffsetXDip);
        double y = Math.Clamp(State.OffsetYDip + yDip, 0, layout.MaxOffsetYDip);
        if (x == State.OffsetXDip && y == State.OffsetYDip) return;
        State = State with { OffsetXDip = x, OffsetYDip = y };
        Changed?.Invoke();
    }
}
