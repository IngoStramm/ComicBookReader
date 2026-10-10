using System.Windows;
using System.Windows.Input;

namespace ComicBookReader.App.Presentation;

internal static class DragSmokeChecks
{
    public static async Task<object> RunAsync(MainWindow window)
    {
        var reader = window.Reader;
        await reader.NavigateAsync(-100);
        reader.SetWidthRatio(1.5);
        reader.ScrollBy(-double.MaxValue, -double.MaxValue);
        reader.ScrollBy(200, 400);
        Point origin = Press(window);
        Require(window.Viewport.Cursor == Cursors.SizeAll, "Dragging did not show a movement cursor.");
        window.PageDrag.MoveTo(new Point(origin.X + 50, origin.Y + 75));
        Require(reader.State.OffsetXDip == 150 && reader.State.OffsetYDip == 325 && reader.State.PresentedIndex == 0,
            "Page did not follow the pointer in both axes.");
        window.Viewport.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        { RoutedEvent = UIElement.MouseLeftButtonUpEvent });
        RequireStopped(window, "Releasing the button did not release page dragging.");
        var released = reader.State;
        window.PageDrag.MoveTo(new Point(0, 0));
        Require(reader.State == released, "Page moved after mouse release.");

        Press(window);
        window.Viewport.ReleaseMouseCapture();
        RequireStopped(window, "Losing mouse capture did not stop dragging.");
        Press(window);
        window.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, window.Viewport, window.Toolbar)
        { RoutedEvent = Keyboard.LostKeyboardFocusEvent });
        RequireStopped(window, "Losing focus did not release dragging.");
        Press(window);
        ReadingSmokeChecks.RaiseKey(window, Key.R, true);
        RequireStopped(window, "A width shortcut did not release dragging.");
        reader.SetWidthRatio(1.5);
        Press(window);
        await reader.NavigateAsync(1);
        RequireStopped(window, "Changing the page did not release dragging.");
        await reader.NavigateAsync(-1);
        reader.SetWidthRatio(1);
        reader.ScrollBy(-double.MaxValue, -double.MaxValue);
        origin = Press(window);
        window.PageDrag.MoveTo(new Point(origin.X - 80, origin.Y - 60));
        Require(reader.State.OffsetXDip == 0 && reader.State.OffsetYDip == 60,
            "Dragging a narrow page did not preserve the non-overflow axis.");
        window.Viewport.ReleaseMouseCapture();
        reader.ResetBookWidth();
        return new
        {
            RoutedMousePressAndRelease = true,
            BothAxesFollowPointer = true,
            CaptureAndFocusLossStopDrag = true,
            NavigationAndShortcutStopDrag = true,
            NarrowPagePreservesHorizontalPosition = true,
            Scope = "Native WPF capture and routed button/focus events with deterministic pointer coordinates; physical mouse movement awaits human validation."
        };
    }

    private static Point Press(MainWindow window)
    {
        Point origin = Mouse.GetPosition(window.Viewport);
        var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        { RoutedEvent = UIElement.MouseLeftButtonDownEvent };
        window.Viewport.RaiseEvent(args);
        Require(window.Reader.IsDragging && window.Viewport.IsMouseCaptured && args.Handled,
            $"Mouse press did not start dragging (handled={args.Handled}, captured={window.Viewport.IsMouseCaptured}, mode={window.Reader.State.Mode}).");
        return origin;
    }

    private static void RequireStopped(MainWindow window, string message) =>
        Require(!window.Reader.IsDragging && !window.Viewport.IsMouseCaptured && window.Viewport.Cursor is null, message);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
