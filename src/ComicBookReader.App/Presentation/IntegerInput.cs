using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ComicBookReader.App.Presentation;

/// <summary>A non-negative integer input with text editing and spinner controls.</summary>
public sealed class IntegerInput : UserControl
{
    private readonly int minimum;
    private readonly int maximum;
    private readonly Button increaseButton;
    private readonly Button decreaseButton;
    private int currentValue;
    public TextBox Editor { get; }
    public int Value
    {
        get { CommitEditorValue(); return currentValue; }
        set { currentValue = Clamp(value); ShowValue(currentValue); }
    }
    public IntegerInput(int value, int minimum, int maximum)
    {
        if (minimum < 0)
            throw new ArgumentOutOfRangeException(nameof(minimum), "The minimum must be non-negative.");
        if (maximum < minimum)
            throw new ArgumentOutOfRangeException(nameof(maximum), "The maximum must not be less than the minimum.");

        this.minimum = minimum;
        this.maximum = maximum;
        currentValue = Clamp(value);

        Background = Brushes.Transparent;
        Focusable = false;
        MinWidth = 74;

        Editor = new TextBox
        {
            Text = Format(currentValue),
            MinWidth = 48,
            Padding = new Thickness(7, 3, 7, 3),
            HorizontalContentAlignment = HorizontalAlignment.Right,
            VerticalContentAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(Color.FromRgb(35, 39, 43)),
            Foreground = new SolidColorBrush(Color.FromRgb(243, 243, 243)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(92, 101, 108)),
            BorderThickness = new Thickness(1),
            SelectionBrush = new SolidColorBrush(Color.FromRgb(67, 91, 109))
        };
        Editor.PreviewTextInput += EditorPreviewTextInput;
        Editor.PreviewKeyDown += EditorPreviewKeyDown;
        Editor.LostKeyboardFocus += EditorLostKeyboardFocus;
        DataObject.AddPastingHandler(Editor, EditorPasting);

        var layout = new Grid { Height = 32 };
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(Editor, 0);
        layout.Children.Add(Editor);

        var spinner = new Grid
        {
            Width = 24,
            Height = 32,
            RowDefinitions =
            {
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            }
        };

        increaseButton = CreateSpinnerButton("▲", "Increase value");
        increaseButton.Click += (_, _) => Step(1);
        Grid.SetRow(increaseButton, 0);
        spinner.Children.Add(increaseButton);

        decreaseButton = CreateSpinnerButton("▼", "Decrease value");
        decreaseButton.Click += (_, _) => Step(-1);
        Grid.SetRow(decreaseButton, 1);
        spinner.Children.Add(decreaseButton);

        Grid.SetColumn(spinner, 1);
        layout.Children.Add(spinner);
        Content = layout;
    }
    public void SetSpinnerLabels(string increaseLabel, string decreaseLabel)
    {
        if (string.IsNullOrWhiteSpace(increaseLabel) || string.IsNullOrWhiteSpace(decreaseLabel))
            throw new ArgumentException("Both spinner labels are required.");
        increaseButton.ToolTip = increaseLabel;
        decreaseButton.ToolTip = decreaseLabel;
        AutomationProperties.SetName(increaseButton, increaseLabel);
        AutomationProperties.SetName(decreaseButton, decreaseLabel);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new IntegerInputAutomationPeer(this, minimum, maximum);

    private int Clamp(int value) => Math.Clamp(value, minimum, maximum);
    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static Button CreateSpinnerButton(string glyph, string accessibleName)
    {
        var button = new Button
        {
            Content = glyph,
            ToolTip = accessibleName,
            MinWidth = 24,
            MinHeight = 15,
            Padding = new Thickness(0),
            FontSize = 8,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(Color.FromRgb(48, 52, 56)),
            Foreground = new SolidColorBrush(Color.FromRgb(243, 243, 243)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(92, 101, 108)),
            BorderThickness = new Thickness(1),
            Focusable = true
        };
        AutomationProperties.SetName(button, accessibleName);
        return button;
    }

    private void EditorPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !CanInsertText(e.Text);
    }

    private void EditorPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Up)
        {
            Step(1);
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            Step(-1);
            e.Handled = true;
        }
    }

    private void EditorLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => CommitEditorValue();
    private void EditorPasting(object sender, DataObjectPastingEventArgs e)
    {
        string? pastedText = e.DataObject.GetDataPresent(DataFormats.UnicodeText, true)
            ? e.DataObject.GetData(DataFormats.UnicodeText) as string
            : e.DataObject.GetDataPresent(DataFormats.Text, true) ? e.DataObject.GetData(DataFormats.Text) as string : null;
        if (pastedText is null || !CanInsertText(pastedText))
            e.CancelCommand();
    }

    private bool CanInsertText(string insertedText)
    {
        if (insertedText.Length > 0 && !HasOnlyAsciiDigits(insertedText))
            return false;

        string text = Editor.Text ?? string.Empty;
        int selectionStart = Math.Clamp(Editor.SelectionStart, 0, text.Length);
        int selectionLength = Math.Clamp(Editor.SelectionLength, 0, text.Length - selectionStart);
        string candidate = text.Remove(selectionStart, selectionLength).Insert(selectionStart, insertedText);

        if (candidate.Length == 0) return true;
        if (!HasOnlyAsciiDigits(candidate)) return false;

        return int.TryParse(candidate, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) &&
            parsed <= maximum;
    }

    private static bool HasOnlyAsciiDigits(string text)
    {
        foreach (char character in text)
            if (character is < '0' or > '9') return false;
        return true;
    }
    private void CommitEditorValue()
    {
        string text = Editor.Text ?? string.Empty;
        if (HasOnlyAsciiDigits(text) && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed))
            currentValue = Clamp(parsed);
        ShowValue(currentValue);
    }

    private void ShowValue(int value)
    {
        Editor.Text = Format(value);
        Editor.CaretIndex = Editor.Text.Length;
    }

    private void Step(int direction)
    {
        int value = Value;
        if (direction > 0 && value < maximum) Value = value + 1;
        else if (direction < 0 && value > minimum) Value = value - 1;
    }
}
