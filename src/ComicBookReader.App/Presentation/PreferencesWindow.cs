using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ComicBookReader.Core.Preferences;

namespace ComicBookReader.App.Presentation;

public sealed class PreferencesWindow : Window
{
    private readonly ReaderPreferences original;
    private readonly TextBox width;
    private readonly TextBox speed;
    private readonly Dictionary<string, TextBox> keys = [];
    private readonly TextBlock error;
    public ReaderPreferences? Result { get; private set; }

    public PreferencesWindow(ReaderPreferences preferences)
    {
        original = preferences;
        Title = "Preferências · ComicBookReader";
        Width = 480; Height = 640; MinWidth = 380; MinHeight = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(36, 40, 44));
        Foreground = Brushes.WhiteSmoke;
        var panel = new StackPanel { Margin = new Thickness(24) };
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        panel.Children.Add(new TextBlock { Text = "Preferências de leitura", FontSize = 24, Margin = new Thickness(0, 0, 0, 16) });
        width = AddField(panel, "Largura padrão de uma página (%) · 25 a 200", (preferences.PreferredPageWidthRatio * 100).ToString(CultureInfo.CurrentCulture));
        speed = AddField(panel, "Velocidade (alturas da tela por segundo) · 0,1 a 2", preferences.ScrollSpeed.ToString(CultureInfo.CurrentCulture));
        panel.Children.Add(new TextBlock { Text = "Atalhos · setas ficam reservadas para rolagem", Margin = new Thickness(0, 14, 0, 8) });
        Dictionary<string, string> labels = new()
        {
            ["NextPage"] = "Próxima página", ["PreviousPage"] = "Página anterior", ["Overview"] = "Página inteira / retornar",
            ["Fullscreen"] = "Tela cheia", ["Open"] = "Abrir HQ", ["ZoomIn"] = "Aumentar largura", ["ZoomOut"] = "Diminuir largura"
        };
        foreach (var binding in preferences.KeyBindings) keys[binding.Key] = AddField(panel, labels[binding.Key], binding.Value);
        error = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightSalmon, Margin = new Thickness(0, 10, 0, 10) };
        System.Windows.Automation.AutomationProperties.SetLiveSetting(error, System.Windows.Automation.AutomationLiveSetting.Polite);
        panel.Children.Add(error);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Cancelar", IsCancel = true, Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(4) };
        var apply = new Button { Content = "Aplicar", IsDefault = true, Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(4) };
        apply.Click += Apply;
        buttons.Children.Add(cancel); buttons.Children.Add(apply); panel.Children.Add(buttons);
    }

    private static TextBox AddField(Panel panel, string label, string value)
    {
        panel.Children.Add(new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 4) });
        var field = new TextBox { Text = value, Padding = new Thickness(7), MinHeight = 32 };
        System.Windows.Automation.AutomationProperties.SetName(field, label);
        panel.Children.Add(field);
        return field;
    }

    private void Apply(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(width.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double ratio) ||
            !double.TryParse(speed.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double velocity))
        { error.Text = "Digite números válidos para a largura e a velocidade."; return; }
        try
        {
            Result = PreferencesResolver.Validate(original with
            {
                PreferredPageWidthRatio = ratio / 100, ScrollSpeed = velocity,
                KeyBindings = keys.ToDictionary(x => x.Key, x => x.Value.Text)
            });
            DialogResult = true;
        }
        catch (ArgumentException ex)
        {
            error.Text = ex.ParamName == "ScrollSpeed" ? "A velocidade deve ficar entre 0,1 e 2." :
                ex.ParamName == "PreferredPageWidthRatio" ? "A largura deve ficar entre 25% e 200%." :
                "Verifique os atalhos: use teclas válidas, sem duplicações ou setas. Exemplo: Ctrl+O.";
        }
    }
}
