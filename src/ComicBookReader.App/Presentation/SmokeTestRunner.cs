using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ComicBookReader.Core.Reading;

namespace ComicBookReader.App.Presentation;

internal static class SmokeTestRunner
{
    public static async Task RunAsync(MainWindow window, string path, string output)
    {
        Directory.CreateDirectory(output);
        await window.OpenPathAsync(path);
        await Task.Delay(200);
        window.UpdateLayout();
        var ready = window.Reader.State;
        if (ready.Mode != ReaderMode.Reading || ready.Image is null)
            throw new InvalidOperationException(ready.Error?.Message ?? "Page did not become ready.");
        var target = new RenderTargetBitmap((int)window.Root.ActualWidth, (int)window.Root.ActualHeight,
            96, 96, PixelFormats.Pbgra32);
        target.Render(window.Root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(target));
        using (var file = File.Create(Path.Combine(output, "reader.png"))) encoder.Save(file);
        await window.OpenPathAsync(path + ".missing");
        if (window.Reader.State.Book?.BookId != ready.Book!.BookId || window.Reader.State.Image is null)
            throw new InvalidOperationException("Failed open lost the previous book.");
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await window.Reader.OpenAsync(path, canceled.Token);
        if (window.Reader.State.Book?.BookId != ready.Book.BookId)
            throw new InvalidOperationException("Canceled open lost the previous book.");
        window.Hide();
        var fullscreenKey = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), 0, Key.F11)
        { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        window.RaiseEvent(fullscreenKey);
        bool fullscreen = window.WindowStyle == WindowStyle.None;
        var escapeKey = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), 0, Key.Escape)
        { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        window.RaiseEvent(escapeKey);
        if (!fullscreen || window.WindowStyle != WindowStyle.SingleBorderWindow)
            throw new InvalidOperationException("Fullscreen keys did not change the window mode.");
        var evidence = new
        {
            ready.Book.CanonicalPath, Pages = ready.Pages.Count, ready.Image.Size,
            ready.Layout, window.ActualWidth, window.ActualHeight,
            FailedOpenPreservedBook = true, CanceledOpenPreservedBook = true,
            FullscreenRoutedKeysPassed = true,
            Scope = "WPF runtime and captured visual; perceptual scrolling awaits validation"
        };
        await File.WriteAllTextAsync(Path.Combine(output, "runtime.json"), JsonSerializer.Serialize(evidence,
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
