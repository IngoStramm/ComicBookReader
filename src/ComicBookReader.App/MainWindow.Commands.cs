using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ComicBookReader.App.Presentation;
using ComicBookReader.Core.Preferences;
using Microsoft.Win32;

namespace ComicBookReader.App;

public partial class MainWindow
{
    private async void OpenClicked(object sender, RoutedEventArgs e)
    {
        input.Stop();
        var dialog = new OpenFileDialog { Filter = text["FileFilter"], Title = text["Open"] };
        if (dialog.ShowDialog(this) == true) await OpenPathAsync(dialog.FileName);
        ReturnToReading();
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: 1 } paths)
            await OpenPathAsync(paths[0]);
    }

    private async void RunCommand(string command)
    {
        switch (command)
        {
            case "NextPage": await NavigateAsync(1); break;
            case "PreviousPage": await NavigateAsync(-1); break;
            case "ScrollForward":
                PersistProgress();
                await saves.FlushAsync();
                await Reader.ScrollForwardAsync();
                break;
            case "Fullscreen": ToggleFullscreen(); break;
            case "Escape": if (fullscreen) SetFullscreen(false); break;
            case "Open": OpenClicked(this, new RoutedEventArgs()); break;
            case "Overview": Reader.ToggleOverview(); break;
            case "ZoomIn": ChangeWidth(.05); break;
            case "ZoomOut": ChangeWidth(-.05); break;
            case "ResetWidth": Reader.ResetBookWidth(); break;
            case "ToggleReadingDirection":
                SetReadingDirection(Reader.ReadingDirection == ReadingDirection.LeftToRight
                    ? ReadingDirection.RightToLeft
                    : ReadingDirection.LeftToRight);
                break;
            case "ToggleToolbar": overlay.Toggle(); ReturnToReading(); break;
            case "Preferences": PreferencesClicked(this, new RoutedEventArgs()); break;
        }
    }

    private async Task NavigateAsync(int delta) { PersistProgress(); await saves.FlushAsync(); await Reader.NavigateAsync(delta); }
    private async void PreviousClicked(object sender, RoutedEventArgs e) { await NavigateAsync(-1); ReturnToReading(); }
    private async void NextClicked(object sender, RoutedEventArgs e) { await NavigateAsync(1); ReturnToReading(); }
    private void OverviewClicked(object sender, RoutedEventArgs e) { Reader.ToggleOverview(); Viewport.Focus(); }
    private void SmallerClicked(object sender, RoutedEventArgs e) => ChangeWidth(-.05);
    private void LargerClicked(object sender, RoutedEventArgs e) => ChangeWidth(.05);
    private void ChangeWidth(double delta, Point? anchor = null) => Reader.SetWidthRatio(
        Math.Clamp(Reader.WidthRatio + delta, .25, 2), anchor?.X, anchor?.Y);
    private void ClassificationChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!updatingControls && ClassificationChoice is not null)
            Reader.SetClassification((PageClassificationOverride)ClassificationChoice.SelectedIndex);
    }
    private void ReadingSelectorClosed(object? sender, EventArgs e) => ReturnToReading();
    private void ReadingDirectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || updatingControls || ReadingDirectionChoice.SelectedIndex < 0 ||
            ReadingDirectionChoice.SelectedIndex == (int)Reader.ReadingDirection) return;
        SetReadingDirection((ReadingDirection)ReadingDirectionChoice.SelectedIndex);
    }
    private void SetReadingDirection(ReadingDirection direction)
    {
        if (Reader.ReadingDirection == direction) return;
        input.Stop();
        Reader.ApplyPreferences(Reader.CapturePreferences() with { ReadingDirection = direction });
        UpdateView();
        PersistProgress();
    }
    private void ResetWidthClicked(object sender, RoutedEventArgs e) { Reader.ResetBookWidth(); Viewport.Focus(); }
    private void PreferencesClicked(object sender, RoutedEventArgs e)
    {
        input.Stop();
        var dialog = new PreferencesWindow(Reader.CapturePreferences()) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.Result is { } result)
        {
            ApplyUserPreferences(result);
        }
        ReturnToReading();
    }
    internal void ApplyUserPreferences(ReaderPreferences result)
    {
        Reader.ApplyPreferences(result);
        input.Bindings = result.KeyBindings;
        ApplyLanguage(result.Language);
        UpdateView();
        PersistProgress();
    }
    private void AboutClicked(object sender, RoutedEventArgs e)
    {
        input.Stop();
        new AboutWindow(Reader.CapturePreferences().Language) { Owner = this }.ShowDialog();
        ReturnToReading();
    }
    private void HideToolbarClicked(object sender, RoutedEventArgs e) { overlay.Toggle(); ReturnToReading(); }
    private void ToolbarButtonClicked(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is Button) ReturnToReading();
    }
    private void ReturnToReading()
    {
        input.Stop();
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, () =>
        {
            if (closing || ClassificationChoice.IsDropDownOpen || ReadingDirectionChoice.IsDropDownOpen ||
                OwnedWindows.Cast<Window>().Any(owned => owned.IsVisible)) return;
            FocusManager.SetFocusedElement(this, Viewport);
            Keyboard.Focus(Viewport);
        });
    }
    private void MouseWheelChanged(object sender, MouseWheelEventArgs e)
    {
        if (!Viewport.IsMouseOver) return;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            ChangeWidth(Math.Sign(e.Delta) * .05, e.GetPosition(Viewport));
        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) Reader.ScrollBy(-e.Delta, 0);
        else Reader.ScrollBy(0, -e.Delta);
        e.Handled = true;
    }

    private void FullscreenClicked(object sender, RoutedEventArgs e) => ToggleFullscreen();
    public void ToggleFullscreen() => SetFullscreen(!fullscreen);
    private void SetFullscreen(bool enabled)
    {
        fullscreen = enabled;
        WindowState = WindowState.Normal;
        WindowStyle = enabled ? WindowStyle.None : WindowStyle.SingleBorderWindow;
        ResizeMode = enabled ? ResizeMode.NoResize : ResizeMode.CanResize;
        if (enabled) WindowState = WindowState.Maximized;
        PersistProgress();
    }

}
