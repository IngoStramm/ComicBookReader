using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ComicBookReader.Core.Preferences;
using ComicBookReader.Core.Reading;

namespace ComicBookReader.App.Presentation;

public sealed class ReaderInput : IDisposable
{
    private readonly Window window;
    private readonly ReaderController reader;
    private readonly Action<string> command;
    private long previousTimestamp;
    public IReadOnlyDictionary<string, string> Bindings { get; set; } = new ReaderPreferences().KeyBindings;

    public ReaderInput(Window window, ReaderController reader, Action<string> command)
    {
        this.window = window;
        this.reader = reader;
        this.command = command;
        window.PreviewKeyDown += KeyDown;
        window.PreviewKeyUp += KeyUp;
        window.Deactivated += Deactivated;
        window.LostKeyboardFocus += LostFocus;
        CompositionTarget.Rendering += Render;
    }

    private static ScrollDirection? Direction(Key key) => key switch
    {
        Key.Up => ScrollDirection.Up,
        Key.Down => ScrollDirection.Down,
        Key.Left => ScrollDirection.Left,
        Key.Right => ScrollDirection.Right,
        _ => null
    };

    private void KeyDown(object sender, KeyEventArgs e)
    {
        bool editing = IsEditingControl(e.OriginalSource as DependencyObject);
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (Direction(key) is { } direction)
        {
            if (editing) { Stop(); return; }
            reader.PressDirection(direction);
            e.Handled = true;
            return;
        }
        if (key == Key.Escape)
        {
            Stop();
            if (editing) return;
            command("Escape"); e.Handled = true; return;
        }
        if (key == Key.Space && Keyboard.Modifiers == ModifierKeys.None && IsButton(e.OriginalSource as DependencyObject))
        { Stop(); return; }
        string keyName = key switch
        {
            Key.PageDown => nameof(Key.PageDown),
            Key.PageUp => nameof(Key.PageUp),
            Key.Enter => nameof(Key.Enter),
            _ => key.ToString()
        };
        string chord = (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ? "Ctrl+" : "") +
            (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "Shift+" : "") +
            (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) ? "Alt+" : "") + keyName;
        string? selectedCommand = FindCommand(chord);
        if (selectedCommand is not null)
        {
            if (editing && selectedCommand is not ("ToggleToolbar" or "Preferences" or "Open" or "Fullscreen"))
            { Stop(); return; }
            e.Handled = true;
            if (!e.IsRepeat) { Stop(); command(selectedCommand); }
            return;
        }
        if (key == Key.Space && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (editing) { Stop(); return; }
            e.Handled = true;
            if (!e.IsRepeat) { Stop(); command("ScrollForward"); }
        }
    }

    internal string? FindCommand(string chord)
    {
        string? exact = Bindings.FirstOrDefault(binding => string.Equals(binding.Value, chord, StringComparison.OrdinalIgnoreCase)).Key;
        if (exact is not null) return exact;
        string? alias = chord switch
        {
            "Add" or "Shift+OemPlus" => "ZoomIn",
            "Subtract" => "ZoomOut",
            _ => null
        };
        string defaultKey = alias == "ZoomIn" ? "OemPlus" : "OemMinus";
        return alias is not null && Bindings.TryGetValue(alias, out string? binding) && binding == defaultKey ? alias : null;
    }

    private static bool IsButton(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is ButtonBase) return true;
            source = LogicalTreeHelper.GetParent(source) ?? (source is Visual ? VisualTreeHelper.GetParent(source) : null);
        }
        return false;
    }

    private static bool IsEditingControl(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is TextBoxBase or Selector or ComboBoxItem) return true;
            source = LogicalTreeHelper.GetParent(source) ??
                (source is Visual ? VisualTreeHelper.GetParent(source) : null);
        }
        return false;
    }

    private void KeyUp(object sender, KeyEventArgs e)
    {
        if (Direction(e.Key) is not { } direction) return;
        reader.ReleaseDirection(direction);
        if (!reader.HasActiveDirections) previousTimestamp = 0;
        if (!IsEditingControl(e.OriginalSource as DependencyObject)) e.Handled = true;
    }

    private void Render(object? sender, EventArgs e)
    {
        if (!reader.HasActiveDirections) { previousTimestamp = 0; return; }
        long now = Stopwatch.GetTimestamp();
        if (previousTimestamp != 0) reader.AdvanceFrame(Stopwatch.GetElapsedTime(previousTimestamp, now).TotalSeconds);
        previousTimestamp = now;
    }

    public void Stop() { reader.Deactivate(); previousTimestamp = 0; }
    private void Deactivated(object? sender, EventArgs e) => Stop();
    private void LostFocus(object sender, KeyboardFocusChangedEventArgs e) => Stop();

    public void Dispose()
    {
        Stop();
        CompositionTarget.Rendering -= Render;
        window.PreviewKeyDown -= KeyDown;
        window.PreviewKeyUp -= KeyUp;
        window.Deactivated -= Deactivated;
        window.LostKeyboardFocus -= LostFocus;
    }
}
