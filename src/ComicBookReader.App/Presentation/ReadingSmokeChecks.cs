using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ComicBookReader.Core.Preferences;
using ComicBookReader.Core.Reading;

namespace ComicBookReader.App.Presentation;

internal static class ReadingSmokeChecks
{
    public static async Task<object> RunAsync(MainWindow window, string output)
    {
        var reader = window.Reader;
        var expectedFile = Path.Combine(output, "expected-reopen.json");
        bool restarted = File.Exists(expectedFile);
        if (restarted)
        {
            var expected = JsonSerializer.Deserialize<BookPreferences>(await File.ReadAllTextAsync(expectedFile))!;
            var restored = reader.CapturePreferences().Books[reader.State.Book!.BookId];
            Require(restored.WidthRatio == expected.WidthRatio && restored.Position == expected.Position &&
                restored.Classifications.SequenceEqual(expected.Classifications), "Restart lost reading preferences.");
        }
        await reader.NavigateAsync(-100);
        reader.SetWidthRatio(1);
        reader.ScrollBy(0, -double.MaxValue);
        var originalLayout = reader.State.Layout;
        await Task.Delay(2300);
        Require(window.Toolbar.Visibility == Visibility.Collapsed, "Reading controls did not hide.");
        Require(reader.State.Layout == originalLayout, "Hiding controls changed page size.");
        RaiseKey(window, Key.Tab, true);
        Require(window.Toolbar.Visibility == Visibility.Visible, "Tab did not reveal reading controls.");

        var intervals = new List<double>();
        long previous = 0;
        void Rendered(object? sender, EventArgs e)
        {
            long now = Stopwatch.GetTimestamp();
            if (previous != 0) intervals.Add(Stopwatch.GetElapsedTime(previous, now).TotalMilliseconds);
            previous = now;
        }
        CompositionTarget.Rendering += Rendered;
        try
        {
            RaiseKey(window, Key.Down, true);
            await Task.Delay(TimeSpan.FromSeconds(10));
            RaiseKey(window, Key.Down, false);
        }
        finally { CompositionTarget.Rendering -= Rendered; reader.Deactivate(); }
        double moved = reader.State.OffsetYDip;
        Require(moved > 0 && reader.State.PresentedIndex == 0, "Timed input did not scroll the current page.");
        await Task.Delay(150);
        Require(reader.State.OffsetYDip == moved, "Scrolling continued after key release.");
        reader.ScrollBy(0, double.MaxValue);
        RaiseKey(window, Key.Down, true);
        await Task.Delay(100);
        RaiseKey(window, Key.Down, false);
        Require(reader.State.PresentedIndex == 0, "Arrow at page edge changed the page.");
        reader.PressDirection(ScrollDirection.Up);
        window.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, window.Viewport, window.Toolbar)
        { RoutedEvent = Keyboard.LostKeyboardFocusEvent });
        Require(!reader.HasActiveDirections, "Focus loss did not stop scrolling.");

        await reader.NavigateAsync(1);
        double viewportWidth = window.Viewport.ActualWidth;
        Require(Math.Abs(reader.State.Layout!.DisplayWidthDip - 2 * viewportWidth) < .001,
            "Spread lost the width of an individual page.");
        reader.SetWidthRatio(1.1);
        reader.SetClassification(PageClassificationOverride.Spread);
        reader.ScrollBy(220, 180);
        var before = reader.State;
        RaiseKey(window, Key.V, true);
        Require(reader.State.Mode == ReaderMode.Overview, "V did not show the full image.");
        RaiseKey(window, Key.V, true);
        Require(reader.State.Layout == before.Layout && reader.State.OffsetXDip == before.OffsetXDip &&
            reader.State.OffsetYDip == before.OffsetYDip, "Overview did not restore exact reading geometry.");
        double pageWidth = reader.State.Layout!.DisplayWidthDip;
        await reader.NavigateAsync(1);
        Require(Math.Abs(reader.State.Layout!.DisplayWidthDip - pageWidth) < .001,
            "Higher resolution spread changed the reading width.");
        await Task.WhenAll(reader.NavigateAsync(-1), reader.NavigateAsync(1));
        Require(reader.State.PresentedIndex == 2, "Rapid navigation published an obsolete target.");
        window.Width = 800;
        window.Height = 600;
        window.UpdateLayout();
        reader.ScrollBy(100, 100);
        var layout = reader.State.Layout!;
        Require(reader.State.OffsetXDip <= layout.MaxOffsetXDip && reader.State.OffsetYDip <= layout.MaxOffsetYDip,
            "Resize left the offsets outside the image.");
        reader.ToggleOverview();
        var snapshot = reader.CapturePreferences().Books[reader.State.Book!.BookId];
        await File.WriteAllTextAsync(expectedFile, JsonSerializer.Serialize(snapshot));
        var dpi = VisualTreeHelper.GetDpi(window);
        return new
        {
            RestartVerified = restarted,
            OverlayHiddenWithoutReframe = true,
            RoutedArrowScrollAndRelease = true,
            FocusLossStopsScroll = true,
            EdgeKeepsPage = true,
            SpreadResolutionsMatch = true,
            OverviewRestoresExactly = true,
            RapidNavigationPassed = true,
            ResizePassed = true,
            ClosedInOverview = true,
            DpiX = dpi.PixelsPerInchX,
            DpiY = dpi.PixelsPerInchY,
            ScrollDurationSeconds = 10,
            ScrollDistanceDip = moved,
            RenderingIntervals = intervals.Count,
            MedianIntervalMs = intervals.Count == 0 ? (double?)null : intervals.Order().ElementAt(intervals.Count / 2),
            MaximumIntervalMs = intervals.Count == 0 ? (double?)null : intervals.Max(),
            Scope = "Offscreen WPF runtime with routed input. Physical keyboard repeat, alternate monitor/DPI and perceptual smoothness need human validation."
        };
    }

    internal static void RaiseKey(MainWindow window, Key key, bool down)
    {
        window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), 0, key)
        { RoutedEvent = down ? Keyboard.PreviewKeyDownEvent : Keyboard.PreviewKeyUpEvent });
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
