using System.Windows;
using System.Windows.Input;
using ComicBookReader.Core.Reading;

namespace ComicBookReader.App.Presentation;

internal sealed class ReaderDrag : IDisposable
{
    private readonly Window window;
    private readonly FrameworkElement viewport;
    private readonly ReaderController reader;
    private readonly Action stopKeyboard;

    public ReaderDrag(Window window, FrameworkElement viewport, ReaderController reader, Action stopKeyboard)
    {
        this.window = window; this.viewport = viewport; this.reader = reader; this.stopKeyboard = stopKeyboard;
        viewport.MouseLeftButtonDown += MouseDown;
        viewport.MouseLeftButtonUp += MouseUp;
        viewport.MouseMove += MouseMoved;
        viewport.LostMouseCapture += LostCapture;
        window.Deactivated += Deactivated;
        window.LostKeyboardFocus += LostFocus;
        window.PreviewKeyDown += KeyDown;
        window.PreviewMouseWheel += MouseWheel;
        reader.Changed += ReaderChanged;
    }

    private void MouseDown(object sender, MouseButtonEventArgs e)
    {
        stopKeyboard();
        viewport.Focus();
        Point point = e.GetPosition(viewport);
        if (!viewport.CaptureMouse()) return;
        if (!reader.BeginDrag(point.X, point.Y)) { viewport.ReleaseMouseCapture(); return; }
        viewport.Cursor = Cursors.SizeAll;
        e.Handled = true;
    }

    private void MouseMoved(object sender, MouseEventArgs e)
    {
        if (!reader.IsDragging) return;
        if (e.LeftButton != MouseButtonState.Pressed || !viewport.IsMouseCaptured) { Stop(); return; }
        MoveTo(e.GetPosition(viewport));
        e.Handled = true;
    }

    internal void MoveTo(Point point) => reader.DragTo(point.X, point.Y);

    private void MouseUp(object sender, MouseButtonEventArgs e)
    {
        bool dragging = reader.IsDragging;
        Stop();
        e.Handled = dragging;
    }

    private void Stop()
    {
        reader.EndDrag();
        viewport.Cursor = null;
        if (viewport.IsMouseCaptured) viewport.ReleaseMouseCapture();
    }

    private void ReaderChanged()
    {
        if (!window.Dispatcher.CheckAccess()) { window.Dispatcher.Invoke(ReaderChanged); return; }
        if (!reader.IsDragging) Stop();
    }
    private void LostCapture(object sender, MouseEventArgs e) => Stop();
    private void Deactivated(object? sender, EventArgs e) => Stop();
    private void LostFocus(object sender, KeyboardFocusChangedEventArgs e) => Stop();
    private void KeyDown(object sender, KeyEventArgs e) => Stop();
    private void MouseWheel(object sender, MouseWheelEventArgs e) => Stop();

    public void Dispose()
    {
        Stop();
        viewport.MouseLeftButtonDown -= MouseDown;
        viewport.MouseLeftButtonUp -= MouseUp;
        viewport.MouseMove -= MouseMoved;
        viewport.LostMouseCapture -= LostCapture;
        window.Deactivated -= Deactivated;
        window.LostKeyboardFocus -= LostFocus;
        window.PreviewKeyDown -= KeyDown;
        window.PreviewMouseWheel -= MouseWheel;
        reader.Changed -= ReaderChanged;
    }
}
