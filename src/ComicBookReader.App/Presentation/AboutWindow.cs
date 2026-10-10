using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ComicBookReader.App.Presentation;

internal sealed class AboutWindow : Window
{
    internal string LicensesText { get; }

    public AboutWindow(string language)
    {
        var text = new UiText(language);
        using var stream = typeof(AboutWindow).Assembly.GetManifestResourceStream("ComicBookReader.ThirdPartyLicenses")
            ?? throw new InvalidOperationException("Missing embedded third-party notices.");
        using var reader = new StreamReader(stream);
        LicensesText = reader.ReadToEnd();
        Title = text["AboutTitle"];
        Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/comic-reader.ico"));
        Width = 700; Height = 560; MinWidth = 420; MinHeight = 360;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(36, 40, 44)); Foreground = Brushes.WhiteSmoke;
        var root = new DockPanel { Margin = new Thickness(24) };
        Content = root;
        var heading = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        var version = typeof(AboutWindow).Assembly.GetName().Version!;
        heading.Children.Add(new TextBlock { Text = $"ComicBookReader {version.Major}.{version.Minor}.{version.Build}", FontSize = 24 });
        heading.Children.Add(new TextBlock { Text = text["ThirdPartyLicenses"], Margin = new Thickness(0, 8, 0, 0) });
        DockPanel.SetDock(heading, Dock.Top); root.Children.Add(heading);
        var close = new Button
        {
            Content = text["Close"],
            IsCancel = true,
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(16, 8, 16, 8),
            Margin = new Thickness(0, 16, 0, 0)
        };
        close.Click += (_, _) => Close();
        DockPanel.SetDock(close, Dock.Bottom); root.Children.Add(close);
        var licenses = new TextBox
        {
            Text = LicensesText,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(12),
            Background = new SolidColorBrush(Color.FromRgb(21, 23, 25)),
            Foreground = Brushes.WhiteSmoke,
            BorderBrush = Brushes.DimGray,
            FontSize = 12
        };
        System.Windows.Automation.AutomationProperties.SetName(licenses, text["ThirdPartyLicenses"]);
        root.Children.Add(licenses);
    }
}
