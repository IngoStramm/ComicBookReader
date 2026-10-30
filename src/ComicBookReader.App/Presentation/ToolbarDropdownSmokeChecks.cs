using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ComicBookReader.App.Presentation;

internal static class ToolbarDropdownSmokeChecks
{
    public static async Task RunAsync(MainWindow window, string output)
    {
        var profile = window.Reader.CapturePreferences();
        foreach (var selector in new[] { window.ClassificationChoice, window.ReadingDirectionChoice })
        {
            window.UpdateLayout();
            var toggle = FindToggle(selector) ?? throw new InvalidOperationException("Selector toggle was not found.");
            // A pointer click toggles the control and then raises the bubbling ButtonBase.Click event.
            ((IToggleProvider)new ToggleButtonAutomationPeer(toggle)).Toggle();
            toggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            Require(selector.IsDropDownOpen, $"{selector.Name} closed immediately after its toggle click.");
            var popup = (Popup)selector.Template.FindName("PART_Popup", selector);
            Require(popup.IsOpen && popup.Child.IsVisible &&
                PresentationSource.FromVisual(popup.Child) != PresentationSource.FromVisual(window),
                "Selector options were not presented in their own visible popup.");
            Capture(popup.Child, Path.Combine(output, selector.Name + "-options.png"));
            int next = (selector.SelectedIndex + 1) % selector.Items.Count;
            var item = (ComboBoxItem)selector.ItemContainerGenerator.ContainerFromIndex(next);
            item.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            { RoutedEvent = UIElement.MouseLeftButtonDownEvent });
            item.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            { RoutedEvent = UIElement.MouseLeftButtonUpEvent });
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            Require(selector.SelectedIndex == next && !selector.IsDropDownOpen,
                "Clicking a selector item did not apply the choice and close the popup.");
            Require(FocusManager.GetFocusedElement(window) == window.Viewport, "Selector did not return focus after choosing an option.");
            window.ApplyUserPreferences(profile);
            window.DefaultWidthButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            ((IToggleProvider)new ToggleButtonAutomationPeer(toggle)).Toggle();
            toggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            Require(selector.IsDropDownOpen, "A pending toolbar action stole focus from an open selector.");
            selector.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(selector), 0, Key.Escape)
            { RoutedEvent = Keyboard.KeyDownEvent });
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            Require(!selector.IsDropDownOpen && FocusManager.GetFocusedElement(window) == window.Viewport,
                "Escape did not close the selector and return focus to reading.");
            window.ApplyUserPreferences(profile);
        }
    }

    private static ToggleButton? FindToggle(DependencyObject parent)
    {
        if (parent is ToggleButton toggle) return toggle;
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            if (FindToggle(VisualTreeHelper.GetChild(parent, index)) is { } found) return found;
        return null;
    }

    private static void Capture(UIElement content, string path)
    {
        content.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(content.RenderSize.Width), (int)Math.Ceiling(content.RenderSize.Height),
            96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path); encoder.Save(file);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
