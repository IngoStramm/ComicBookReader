using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Infrastructure.Archives;

namespace ComicBookReader.App.Presentation;

internal sealed class ThumbnailWindow : Window
{
    private readonly CancellationTokenSource cancellation = new();
    private readonly List<Image> images = [];
    private readonly List<TextBlock> labels = [];
    public int? SelectedPage { get; private set; }

    public ThumbnailWindow(BookInfo book, IReadOnlyList<PageInfo> pages, int selected, UiText text)
    {
        Title = text["Thumbnails"];
        Width = 850; Height = 650; MinWidth = 420; MinHeight = 300;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(36, 40, 44));
        var panel = new WrapPanel { Margin = new Thickness(12) };
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        for (int i = 0; i < pages.Count; i++)
        {
            int index = i;
            var image = new Image { Width = 120, Height = 150, Stretch = Stretch.Uniform };
            var label = new TextBlock { Text = (i + 1).ToString(), HorizontalAlignment = HorizontalAlignment.Center };
            var body = new StackPanel();
            body.Children.Add(image); body.Children.Add(label);
            var button = new Button
            {
                Content = body,
                Margin = new Thickness(5),
                Padding = new Thickness(5),
                ToolTip = pages[i].EntryName,
                BorderThickness = new Thickness(i == selected ? 3 : 1)
            };
            if (i == selected)
                button.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            button.Click += (_, _) => { SelectedPage = index; DialogResult = true; };
            images.Add(image); labels.Add(label); panel.Children.Add(button);
        }
        Loaded += (_, _) => _ = LoadThumbnailsAsync(book, text);
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { e.Handled = true; Close(); } };
        Closed += (_, _) => cancellation.Cancel();
    }

    private async Task LoadThumbnailsAsync(BookInfo book, UiText text)
    {
        var token = cancellation.Token;
        try
        {
            await Task.Run(async () =>
            {
                await using var session = await new ComicOpener(useCache: false, prefetchEnabled: false)
                    .OpenAsync(book.CanonicalPath, token);
                if (session.Book.BookId != book.BookId) return;
                for (int i = 0; i < images.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        using var page = await session.ReadPageAsync(i, token);
                        var thumbnail = CreateThumbnail(page);
                        int index = i;
                        await Dispatcher.InvokeAsync(() => { if (!token.IsCancellationRequested) images[index].Source = thumbnail; });
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception)
                    {
                        int index = i;
                        await Dispatcher.InvokeAsync(() => { if (!token.IsCancellationRequested) labels[index].Text = $"{index + 1} · {text["ThumbnailUnavailable"]}"; });
                    }
                }
            }, token);
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!token.IsCancellationRequested) Title = $"{text["Thumbnails"]} · {text["ThumbnailUnavailable"]}"; }
    }

    private static BitmapSource CreateThumbnail(PageImage page)
    {
        double scale = Math.Min(120d / page.Size.Width, 150d / page.Size.Height);
        int width = Math.Max(1, (int)(page.Size.Width * scale));
        int height = Math.Max(1, (int)(page.Size.Height * scale));
        int stride = width * 4;
        var output = new byte[stride * height];
        var input = page.Pixels.Span;
        for (int y = 0; y < height; y++)
        {
            int sourceY = (int)((long)y * page.Size.Height / height);
            for (int x = 0; x < width; x++)
            {
                int sourceX = (int)((long)x * page.Size.Width / width);
                input.Slice(sourceY * page.Stride + sourceX * 4, 4).CopyTo(output.AsSpan(y * stride + x * 4, 4));
            }
        }
        var thumbnail = BitmapSource.Create(width, height, 96, 96, PixelFormats.Pbgra32, null, output, stride);
        thumbnail.Freeze();
        return thumbnail;
    }
}
