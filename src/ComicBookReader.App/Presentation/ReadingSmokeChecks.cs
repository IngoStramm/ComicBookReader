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
    public static async Task<bool> ValidateRestartAsync(MainWindow window, string output)
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
        return restarted;
    }

    public static async Task<object> RunAsync(MainWindow window, string output, bool restarted)
    {
        var reader = window.Reader;
        var expectedFile = Path.Combine(output, "expected-reopen.json");
        await reader.NavigateAsync(-100);
        reader.SetClassification(PageClassificationOverride.Auto);
        await Task.Delay(2300);
        var toolbarLayout = reader.State.Layout;
        if (window.Toolbar.Visibility == Visibility.Visible) RaiseKey(window, Key.F12, true);
        Require(window.Toolbar.Visibility == Visibility.Collapsed, "F12 did not hide the toolbar before the toggle test.");
        RaiseKey(window, Key.F12, true);
        Require(window.Toolbar.Visibility == Visibility.Visible, "F12 did not show the reading toolbar.");
        RaiseKey(window, Key.F12, true);
        Require(window.Toolbar.Visibility == Visibility.Collapsed, "F12 did not hide the reading toolbar.");
        window.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.PreviewMouseMoveEvent });
        Require(window.Toolbar.Visibility == Visibility.Collapsed, "Mouse movement showed the toolbar.");
        Require(reader.State.Layout == toolbarLayout, "Toggling the toolbar changed page size.");
        var pageKey = RaiseKey(window, Key.PageDown, true);
        await WaitForPageAsync(window, 1,
            $"Page Down did not advance the page (handled={pageKey.Handled}, source={pageKey.OriginalSource?.GetType().Name}, modifiers={Keyboard.Modifiers}, mode={reader.State.Mode}).");
        RaiseKey(window, Key.PageUp, true);
        await WaitForPageAsync(window, 0, "Page Up did not return to the previous page.");
        reader.SetWidthRatio(1);
        reader.ScrollBy(0, -double.MaxValue);
        RaiseKey(window, Key.Space, true);
        await WaitForAsync(() => reader.State.OffsetYDip > 0, "Space did not scroll down.");
        Require(reader.State.PresentedIndex == 0 &&
            Math.Abs(reader.State.OffsetYDip - reader.CapturePreferences().SpaceScrollPercent / 100d * window.Viewport.ActualHeight) < .001,
            "Space skipped a page or lost viewport overlap.");
        reader.ScrollBy(0, reader.State.Layout!.MaxOffsetYDip - reader.State.OffsetYDip - 10);
        RaiseKey(window, Key.Space, true);
        await WaitForAsync(() => reader.State.OffsetYDip == reader.State.Layout!.MaxOffsetYDip,
            "Space did not show the bottom of the page.");
        Require(reader.State.PresentedIndex == 0, "Space skipped the bottom of the current page.");
        RaiseKey(window, Key.Space, true);
        await WaitForPageAsync(window, 1, "Space at the bottom did not advance the page.");
        await reader.NavigateAsync(-1);
        reader.ScrollBy(0, -double.MaxValue);

        reader.SetWidthRatio(1.5);
        reader.ScrollBy(-double.MaxValue, 0);
        window.ClassificationChoice.IsDropDownOpen = true;
        await Task.Delay(50);
        RaiseKey((UIElement)window.ClassificationChoice.Items[0], Key.Right, true);
        Require(!reader.HasActiveDirections, "An arrow inside the classification control scrolled the page.");
        window.ClassificationChoice.IsDropDownOpen = false;
        await WaitForAsync(() => FocusManager.GetFocusedElement(window) == window.Viewport,
            "Closing the classification selector did not restore reading focus.");
        RaiseKey(window.Viewport, Key.Right, true);
        await Task.Delay(150);
        RaiseKey(window.Viewport, Key.Right, false);
        Require(reader.State.OffsetXDip > 0, "Right arrow did not scroll a wide page.");
        RaiseKey(window.Viewport, Key.Left, true);
        await Task.Delay(300);
        RaiseKey(window.Viewport, Key.Left, false);
        Require(reader.State.OffsetXDip == 0 && reader.State.PresentedIndex == 0,
            "Left arrow did not return to the edge of the same page.");

        FocusManager.SetFocusedElement(window, window.ClassificationChoice);
        window.ClassificationChoice.IsDropDownOpen = true;
        window.ClassificationChoice.IsDropDownOpen = false;
        await WaitForAsync(() => FocusManager.GetFocusedElement(window) == window.Viewport,
            "Closing the classification selector did not restore reading focus.");
        RaiseKey((UIElement)FocusManager.GetFocusedElement(window), Key.Right, true);
        await Task.Delay(150);
        RaiseKey(window, Key.Right, false);
        Require(reader.State.OffsetXDip > 0, "Right arrow stopped working after closing the selector.");
        reader.ScrollBy(double.MaxValue, 0);
        RaiseKey(window.Viewport, Key.Right, true);
        await Task.Delay(100);
        RaiseKey(window.Viewport, Key.Right, false);
        Require(reader.State.PresentedIndex == 0 && reader.State.OffsetXDip == reader.State.Layout!.MaxOffsetXDip,
            "Right arrow at the page edge changed the page.");
        reader.SetWidthRatio(1);
        reader.ScrollBy(0, -double.MaxValue);
        var originalLayout = reader.State.Layout;
        if (window.Toolbar.Visibility == Visibility.Visible) RaiseKey(window, Key.F12, true);
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
        var overviewKey = RaiseKey(window, Key.V, true);
        Require(reader.State.Mode == ReaderMode.Overview,
            $"V did not show the full image (handled={overviewKey.Handled}, source={overviewKey.OriginalSource?.GetType().Name}, modifiers={Keyboard.Modifiers}, mode={reader.State.Mode}).");
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
            PageNavigationKeysPassed = true,
            SpaceScrollAndAdvancePassed = true,
            HorizontalArrowsAndSelectorFocusPassed = true,
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

    internal static KeyEventArgs RaiseKey(UIElement source, Key key, bool down)
    {
        var args = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(source), 0, key)
        { RoutedEvent = down ? Keyboard.PreviewKeyDownEvent : Keyboard.PreviewKeyUpEvent };
        source.RaiseEvent(args);
        return args;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static Task WaitForPageAsync(MainWindow window, int index, string message) =>
        WaitForAsync(() => window.Reader.State.Mode == ReaderMode.Reading && window.Reader.State.PresentedIndex == index, message);

    private static async Task WaitForAsync(Func<bool> condition, string message)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(3))
        {
            if (condition()) return;
            await Task.Delay(10);
        }
        throw new InvalidOperationException(message);
    }
}
