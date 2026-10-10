using System.Windows;
using System.Windows.Input;
using ComicBookReader.Core.Reading;

namespace ComicBookReader.App.Presentation;

internal sealed class ReadingOverlay : IDisposable
{
    private readonly MainWindow window;
    private bool visible = true;
    private bool hasReadingSession;

    public ReadingOverlay(MainWindow window)
    {
        this.window = window;
        window.PreviewKeyDown += KeyDown;
    }

    public void Update()
    {
        bool reading = window.Reader.State.Image is not null;
        if (reading && !hasReadingSession) { visible = false; hasReadingSession = true; }
        window.Toolbar.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        window.ToolbarRevealButton.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
        bool message = window.Reader.State.Mode is ReaderMode.Loading or ReaderMode.PageError ||
            window.Reader.State.Error is not null || window.HasStorageWarning;
        window.StatusPanel.Visibility = !reading || visible || message ? Visibility.Visible : Visibility.Collapsed;
    }

    public void Toggle()
    {
        visible = !visible;
        Update();
    }

    private void KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Tab or Key.LeftAlt or Key.RightAlt) { visible = true; Update(); }
    }

    public void Dispose()
    {
        window.PreviewKeyDown -= KeyDown;
    }
}
