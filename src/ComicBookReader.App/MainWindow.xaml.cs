using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using ComicBookReader.App.Presentation;
using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Layout;
using ComicBookReader.Core.Reading;
using ComicBookReader.Infrastructure.Archives;
using Microsoft.Win32;

namespace ComicBookReader.App;

public partial class MainWindow : Window
{
    public ReaderController Reader { get; } = new(new ComicOpener());
    private PageImage? presented;
    private bool fullscreen;

    public MainWindow(bool startFullscreen = true)
    {
        InitializeComponent();
        Reader.Changed += UpdateView;
        Viewport.SizeChanged += (_, _) => UpdateViewport();
        PreviewKeyDown += OnKeyDown;
        Drop += OnDrop;
        Loaded += (_, _) => { SetFullscreen(startFullscreen); UpdateViewport(); Viewport.Focus(); };
        Closed += async (_, _) => { Reader.Changed -= UpdateView; await Reader.DisposeAsync(); };
    }

    public async Task OpenPathAsync(string path)
    {
        await Reader.OpenAsync(path);
        Viewport.Focus();
    }

    private async void OpenClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Histórias em quadrinhos|*.cbz;*.cbr|Todos os arquivos|*.*" };
        if (dialog.ShowDialog(this) == true) await OpenPathAsync(dialog.FileName);
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: 1 } paths)
            await OpenPathAsync(paths[0]);
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11 && !e.IsRepeat) { ToggleFullscreen(); e.Handled = true; }
        else if (e.Key == Key.Escape && fullscreen) { SetFullscreen(false); e.Handled = true; }
        else if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control && !e.IsRepeat)
        {
            OpenClicked(sender, e);
            e.Handled = true;
        }
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
    }

    private void UpdateViewport()
    {
        if (Viewport.ActualWidth > 0 && Viewport.ActualHeight > 0)
            Reader.SetViewport(new ViewportSize(Viewport.ActualWidth, Viewport.ActualHeight));
    }

    private void UpdateView()
    {
        if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(UpdateView); return; }
        var state = Reader.State;
        if (!ReferenceEquals(presented, state.Image))
        {
            presented = state.Image;
            Page.Source = presented is null ? null : BitmapPresenter.Create(presented);
        }
        if (state.Layout is { } layout)
        {
            Page.Width = layout.DisplayWidthDip;
            Page.Height = layout.DisplayHeightDip;
            Canvas.SetLeft(Page, layout.OriginXDip - state.OffsetXDip);
            Canvas.SetTop(Page, layout.OriginYDip - state.OffsetYDip);
        }
        EmptyHint.Visibility = state.Image is null ? Visibility.Visible : Visibility.Collapsed;
        PageLabel.Text = state.Book is null ? "Nenhuma HQ aberta" : $"{state.PresentedIndex + 1} / {state.Pages.Count}";
        Status.Text = state.Error?.Message ?? (state.Mode == ReaderMode.Loading ? "Carregando…" :
            state.Book is null ? "Ctrl+O · Abrir HQ    F11 · Tela cheia" : System.IO.Path.GetFileName(state.Book.CanonicalPath));
    }
}
