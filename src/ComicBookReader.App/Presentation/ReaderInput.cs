using System.Diagnostics;
using System.Windows;
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
        if (e.OriginalSource is TextBoxBase or Selector) { Stop(); return; }
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (Direction(key) is { } direction)
        {
            reader.PressDirection(direction);
            e.Handled = true;
            return;
        }
        if (key == Key.Escape) { Stop(); command("Escape"); e.Handled = true; return; }
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
        foreach (var binding in Bindings)
        {
            if (!string.Equals(binding.Value, chord, StringComparison.OrdinalIgnoreCase)) continue;
            e.Handled = true;
            if (!e.IsRepeat) { Stop(); command(binding.Key); }
            break;
        }
    }

    private void KeyUp(object sender, KeyEventArgs e)
    {
        if (Direction(e.Key) is not { } direction) return;
        reader.ReleaseDirection(direction);
        if (!reader.HasActiveDirections) previousTimestamp = 0;
        e.Handled = true;
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
