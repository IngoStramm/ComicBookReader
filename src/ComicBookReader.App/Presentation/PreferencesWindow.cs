using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using ComicBookReader.Core.Preferences;

namespace ComicBookReader.App.Presentation;

public sealed class PreferencesWindow : Window
{
    private readonly ReaderPreferences original;
    private readonly UiText text;
    private readonly Dictionary<string, TextBox> keys = [];
    private readonly TextBlock error;
    internal IntegerInput WidthInput { get; }
    internal IntegerInput SpeedInput { get; }
    internal IntegerInput SpaceInput { get; }
    internal ComboBox LanguageChoice { get; }
    internal ComboBox ClassificationChoice { get; }
    internal Button ApplyButton { get; }
    public ReaderPreferences? Result { get; private set; }

    public PreferencesWindow(ReaderPreferences preferences)
    {
        original = PreferencesResolver.Validate(preferences);
        text = new UiText(original.Language);
        Title = text["PreferencesTitle"];
        Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/comic-reader.ico"));
        Width = 520; Height = 760; MinWidth = 420; MinHeight = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(36, 40, 44));
        Foreground = Brushes.WhiteSmoke;
        var root = new DockPanel { Margin = new Thickness(24) };
        Content = root;
        var heading = new TextBlock { Text = text["PreferencesHeading"], FontSize = 24, Margin = new Thickness(0, 0, 0, 16) };
        DockPanel.SetDock(heading, Dock.Top); root.Children.Add(heading);
        var footer = new StackPanel();
        DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
        error = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightSalmon, Margin = new Thickness(0, 10, 0, 10) };
        AutomationProperties.SetLiveSetting(error, AutomationLiveSetting.Polite);
        footer.Children.Add(error);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = text["Cancel"], IsCancel = true, Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(4) };
        ApplyButton = new Button { Content = text["Apply"], IsDefault = true, Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(4) };
        ApplyButton.Click += Apply;
        buttons.Children.Add(cancel); buttons.Children.Add(ApplyButton); footer.Children.Add(buttons);

        var panel = new StackPanel();
        root.Children.Add(new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        LanguageChoice = AddChoice(panel, text["Language"], ["Português (Brasil)", "English"], original.Language == "en" ? 1 : 0);
        WidthInput = AddNumber(panel, "DefaultWidth", (int)Math.Round(original.PreferredPageWidthRatio * 100), 25, 200);
        SpeedInput = AddNumber(panel, "ScrollSpeed", (int)Math.Round(original.ScrollSpeed * 100), 10, 200);
        SpaceInput = AddNumber(panel, "SpaceDistance", original.SpaceScrollPercent, 10, 100);
        panel.Children.Add(new TextBlock { Text = text["SpaceHint"], TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });
        ClassificationChoice = AddChoice(panel, text["Classification"], [text["Auto"], text["Single"], text["Spread"]], (int)original.PageClassification);
        panel.Children.Add(new TextBlock { Text = text["Shortcuts"], TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 20, 0, 8) });
        foreach (var binding in original.KeyBindings)
        {
            var field = new TextBox { Text = binding.Value, Padding = new Thickness(7), MinHeight = 32 };
            AddField(panel, text[binding.Key], field);
            keys[binding.Key] = field;
        }
    }

    private IntegerInput AddNumber(Panel panel, string labelKey, int value, int min, int max)
    {
        var field = new IntegerInput(value, min, max);
        field.SetSpinnerLabels(text["Increase"], text["Decrease"]);
        AddField(panel, text[labelKey], field);
        AutomationProperties.SetName(field.Editor, text[labelKey]);
        return field;
    }

    private static ComboBox AddChoice(Panel panel, string label, string[] choices, int selected)
    {
        var field = new ComboBox { ItemsSource = choices, SelectedIndex = selected, MinHeight = 32, Padding = new Thickness(7), Foreground = Brushes.Black };
        AddField(panel, label, field);
        return field;
    }

    private static void AddField(Panel panel, string label, Control field)
    {
        var caption = new Label
        {
            Content = new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap },
            Target = field,
            Foreground = Brushes.WhiteSmoke,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 12, 0, 4)
        };
        AutomationProperties.SetName(field, label);
        AutomationProperties.SetLabeledBy(field, caption);
        panel.Children.Add(caption); panel.Children.Add(field);
    }

    private void Apply(object sender, RoutedEventArgs e)
    {
        try
        {
            Result = PreferencesResolver.Validate(original with
            {
                PreferredPageWidthRatio = WidthInput.Value / 100d,
                ScrollSpeed = SpeedInput.Value / 100d,
                SpaceScrollPercent = SpaceInput.Value,
                Language = LanguageChoice.SelectedIndex == 1 ? "en" : "pt-BR",
                PageClassification = (PageClassificationOverride)ClassificationChoice.SelectedIndex,
                KeyBindings = keys.ToDictionary(x => x.Key, x => x.Value.Text)
            });
            DialogResult = true;
        }
        catch (ArgumentException) { error.Text = text["ShortcutError"]; }
    }
}
