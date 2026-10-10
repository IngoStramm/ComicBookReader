using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ComicBookReader.Core.Reading;

namespace ComicBookReader.App.Presentation;

internal sealed class ReadingOverlay : IDisposable
{
    private readonly MainWindow window;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private bool visible = true;

    public ReadingOverlay(MainWindow window)
    {
        this.window = window;
        window.PreviewMouseMove += MouseMoved;
        window.PreviewKeyDown += KeyDown;
        timer.Tick += HideWhenIdle;
        timer.Start();
    }

    public void Update()
    {
        bool reading = window.Reader.State.Image is not null;
        window.Toolbar.Visibility = !reading || visible ? Visibility.Visible : Visibility.Collapsed;
        bool message = window.Reader.State.Mode is ReaderMode.Loading or ReaderMode.PageError ||
            window.Reader.State.Error is not null || window.HasStorageWarning;
        window.StatusPanel.Visibility = !reading || visible || message ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Show()
    {
        visible = true;
        timer.Stop();
        timer.Start();
        Update();
    }

    private void MouseMoved(object sender, MouseEventArgs e) => Show();
    private void KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Tab or Key.LeftAlt or Key.RightAlt) Show();
    }
    private void HideWhenIdle(object? sender, EventArgs e)
    {
        if (window.Toolbar.IsKeyboardFocusWithin || window.Toolbar.IsMouseOver) return;
        visible = false;
        Update();
    }

    public void Dispose()
    {
        timer.Stop();
        timer.Tick -= HideWhenIdle;
        window.PreviewMouseMove -= MouseMoved;
        window.PreviewKeyDown -= KeyDown;
    }
}
