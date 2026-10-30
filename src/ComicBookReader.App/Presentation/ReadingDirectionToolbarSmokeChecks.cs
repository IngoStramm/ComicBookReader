using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using ComicBookReader.Core.Preferences;

namespace ComicBookReader.App.Presentation;

internal static class ReadingDirectionToolbarSmokeChecks
{
    public static async Task RunAsync(MainWindow window)
    {
        var original = window.Reader.CapturePreferences();
        Require(window.ReadingDirectionChoice.SelectedIndex == (int)original.ReadingDirection,
            "Toolbar did not display the saved reading direction.");
        var layout = window.Reader.State.Layout;
        double viewportWidth = window.Viewport.ActualWidth, viewportHeight = window.Viewport.ActualHeight;
        foreach (var direction in new[] { ReadingDirection.LeftToRight, ReadingDirection.RightToLeft })
        {
            window.ReadingDirectionChoice.SelectedIndex = (int)direction;
            Require(window.Reader.CapturePreferences().ReadingDirection == direction,
                "Toolbar selection did not change the reading preference.");
            Require(window.Reader.State.Layout == layout && window.Viewport.ActualWidth == viewportWidth &&
                window.Viewport.ActualHeight == viewportHeight, "Reading direction selector resized the page.");
            var dialog = new PreferencesWindow(window.Reader.CapturePreferences());
            try
            {
                Require(dialog.ReadingDirectionChoice.SelectedIndex == (int)direction,
                    "Toolbar and preferences disagree about reading direction.");
            }
            finally { dialog.Close(); }
        }
        window.ApplyUserPreferences(original);
        Require(window.ReadingDirectionChoice.SelectedIndex == (int)original.ReadingDirection,
            "Applying preferences did not synchronize the toolbar selector.");
        window.ReadingDirectionChoice.IsDropDownOpen = true;
        ReadingSmokeChecks.RaiseKey(window.ReadingDirectionChoice, Key.Right, true);
        Require(!window.Reader.HasActiveDirections, "An arrow in the reading direction selector scrolled the page.");
        window.ReadingDirectionChoice.IsDropDownOpen = false;
        var timer = Stopwatch.StartNew();
        while (FocusManager.GetFocusedElement(window) != window.Viewport && timer.Elapsed.TotalSeconds < 3)
            await Task.Delay(10);
        Require(FocusManager.GetFocusedElement(window) == window.Viewport,
            "Closing reading direction selector did not restore reading focus.");
        double originalWidth = window.Width;
        try
        {
            window.Width = window.MinWidth;
            window.UpdateLayout();
            var bounds = window.ReadingDirectionChoice.TransformToAncestor(window.Root).TransformBounds(
                new Rect(window.ReadingDirectionChoice.RenderSize));
            Require(bounds.Left >= 0 && bounds.Right <= window.Root.ActualWidth && bounds.Bottom <= window.Root.ActualHeight,
                "Reading direction selector was clipped at the minimum window width.");
        }
        finally { window.Width = originalWidth; window.UpdateLayout(); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
