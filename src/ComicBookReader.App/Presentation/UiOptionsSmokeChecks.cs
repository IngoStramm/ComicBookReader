using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ComicBookReader.Core.Preferences;

namespace ComicBookReader.App.Presentation;

internal static class UiOptionsSmokeChecks
{
    public static async Task RunAsync(MainWindow window, string output)
    {
        var profile = window.Reader.CapturePreferences();
        string expectedFile = Path.Combine(output, "expected-options.json");
        if (File.Exists(expectedFile))
        {
            var expected = JsonSerializer.Deserialize<ReaderPreferences>(await File.ReadAllTextAsync(expectedFile))!;
            Require(profile.Language == expected.Language && profile.SpaceScrollPercent == expected.SpaceScrollPercent &&
                profile.PageClassification == expected.PageClassification, "Restart lost global reader options.");
        }
        window.ApplyUserPreferences(profile with { Language = "pt-BR", SpaceScrollPercent = 90, PageClassification = PageClassificationOverride.Auto });
        if (window.Toolbar.Visibility != Visibility.Visible) ReadingSmokeChecks.RaiseKey(window, Key.F12, true);
        ReadingSmokeChecks.RaiseKey(window.ClassificationChoice, Key.F12, true);
        Require(window.Toolbar.Visibility == Visibility.Collapsed, "Toolbar shortcut failed while the selector had focus.");
        Require(window.ToolbarRevealButton.Visibility == Visibility.Visible, "Hidden toolbar has no mouse-accessible toggle.");
        window.ToolbarRevealButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(window.Toolbar.Visibility == Visibility.Visible, "Mouse toolbar toggle did not show the toolbar.");
        window.Reader.SetWidthRatio(1.3);
        FocusManager.SetFocusedElement(window, window.DefaultWidthButton);
        window.DefaultWidthButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WaitForAsync(() => FocusManager.GetFocusedElement(window) == window.Viewport,
            "Default width button did not restore reading focus.");
        Require(window.Reader.WidthRatio == profile.PreferredPageWidthRatio, "Default width did not remove the book override.");
        ReadingSmokeChecks.RaiseKey((UIElement)FocusManager.GetFocusedElement(window), Key.V, true);
        Require(window.Reader.State.Mode == Core.Reading.ReaderMode.Overview, "Keyboard shortcut failed after default width.");
        ReadingSmokeChecks.RaiseKey(window, Key.V, true);
        var activation = ReadingSmokeChecks.RaiseKey(window.DefaultWidthButton, Key.Space, true);
        Require(!activation.Handled, "Space did not remain available to activate a focused button.");

        var bindings = new Dictionary<string, string>(profile.KeyBindings) { ["Preferences"] = "F10" };
        window.ApplyUserPreferences(window.Reader.CapturePreferences() with { KeyBindings = bindings });
        Exception? failure = null;
        bool opened = false;
        var editing = window.Dispatcher.BeginInvoke(() =>
        {
            var dialog = Application.Current.Windows.OfType<PreferencesWindow>().SingleOrDefault();
            if (dialog is null) { failure = new InvalidOperationException("Preferences shortcut did not open a dialog."); return; }
            try
            {
                opened = true;
                Require(dialog.Title.StartsWith("Preferências", StringComparison.Ordinal), "Portuguese preferences title was not localized.");
                dialog.WidthInput.Editor.SelectAll();
                Require(RejectsText(dialog.WidthInput.Editor, "abc") && RejectsText(dialog.WidthInput.Editor, "1.5") &&
                    RejectsText(dialog.WidthInput.Editor, "201"), "Integer field accepted invalid input.");
                var paste = new DataObjectPastingEventArgs(new DataObject(DataFormats.UnicodeText, "bad paste"), false, DataFormats.UnicodeText)
                { RoutedEvent = DataObject.PastingEvent };
                dialog.WidthInput.Editor.RaiseEvent(paste);
                Require(paste.CommandCancelled, "Integer field accepted invalid pasted text.");
                dialog.WidthInput.Editor.Text = "";
                Require(dialog.WidthInput.Value is >= 25 and <= 200, "Empty integer field did not restore a valid value.");
                dialog.SpaceInput.Value = 45;
                dialog.LanguageChoice.SelectedIndex = 1;
                dialog.ClassificationChoice.SelectedIndex = (int)PageClassificationOverride.Single;
                Capture(dialog, Path.Combine(output, "preferences-pt.png"));
                dialog.ApplyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            catch (Exception error) { failure = error; dialog.Close(); }
        });
        ReadingSmokeChecks.RaiseKey(window, Key.F10, true);
        await editing;
        if (failure is not null) throw failure;
        Require(opened, "Preferences shortcut was not routed.");
        await WaitForAsync(() => FocusManager.GetFocusedElement(window) == window.Viewport, "Preferences did not restore reading focus.");
        var saved = window.Reader.CapturePreferences();
        Require(saved.Language == "en" && saved.SpaceScrollPercent == 45 && saved.PageClassification == PageClassificationOverride.Single,
            "Applying preferences lost a reader option.");
        Require((string)window.Resources["PreferencesButton"] == "_Preferences…" &&
            (string)((ComboBoxItem)window.ClassificationChoice.Items[1]).Content == "Single" && window.WidthLabel.Text.StartsWith("Width", StringComparison.Ordinal),
            "Applying English did not translate the reading window.");
        Capture(window, Path.Combine(output, "toolbar-en.png"));
        var english = new PreferencesWindow(saved) { Left = -10000, Top = -10000, WindowStartupLocation = WindowStartupLocation.Manual, ShowActivated = false };
        english.Show(); english.UpdateLayout();
        Require(english.Title.StartsWith("Preferences", StringComparison.Ordinal), "English preferences title was not translated.");
        Capture(english, Path.Combine(output, "preferences-en.png"));
        english.Close();
        window.ApplyUserPreferences(saved with { KeyBindings = profile.KeyBindings });
        // ReadingSmokeChecks runs next and records its final global classification for the following process.
    }

    public static async Task SaveExpectedAsync(MainWindow window, string output) =>
        await File.WriteAllTextAsync(Path.Combine(output, "expected-options.json"), JsonSerializer.Serialize(window.Reader.CapturePreferences()));

    private static bool RejectsText(TextBox field, string value)
    {
        var composition = new TextComposition(InputManager.Current, field, value);
        var args = new TextCompositionEventArgs(Keyboard.PrimaryDevice, composition) { RoutedEvent = TextCompositionManager.PreviewTextInputEvent };
        field.RaiseEvent(args);
        return args.Handled;
    }

    private static void Capture(Window window, string path)
    {
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var file = File.Create(path); encoder.Save(file);
    }

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

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
