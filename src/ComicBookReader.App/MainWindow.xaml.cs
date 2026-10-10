using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using ComicBookReader.App.Presentation;
using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Layout;
using ComicBookReader.Core.Reading;
using ComicBookReader.Core.Preferences;
using ComicBookReader.Infrastructure.Archives;
using Microsoft.Win32;

namespace ComicBookReader.App;

public partial class MainWindow : Window
{
    public ReaderController Reader { get; } = new(new ComicOpener());
    private PageImage? presented;
    private bool fullscreen;
    private readonly ReaderInput input;
    private bool updatingControls;

    public MainWindow(bool startFullscreen = true)
    {
        InitializeComponent();
        Reader.Changed += UpdateView;
        Viewport.SizeChanged += (_, _) => UpdateViewport();
        input = new ReaderInput(this, Reader, RunCommand);
        PreviewMouseWheel += MouseWheelChanged;
        Drop += OnDrop;
        Loaded += (_, _) => { SetFullscreen(startFullscreen); UpdateViewport(); Viewport.Focus(); };
        Closed += async (_, _) => { input.Dispose(); Reader.Changed -= UpdateView; await Reader.DisposeAsync(); };
    }

    public async Task OpenPathAsync(string path)
    {
        await Reader.OpenAsync(path);
        Viewport.Focus();
    }

    private async void OpenClicked(object sender, RoutedEventArgs e)
    {
        input.Stop();
        var dialog = new OpenFileDialog { Filter = "Histórias em quadrinhos|*.cbz;*.cbr|Todos os arquivos|*.*" };
        if (dialog.ShowDialog(this) == true) await OpenPathAsync(dialog.FileName);
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: 1 } paths)
            await OpenPathAsync(paths[0]);
    }

    private async void RunCommand(string command)
    {
        switch (command)
        {
            case "NextPage": await Reader.NavigateAsync(1); break;
            case "PreviousPage": await Reader.NavigateAsync(-1); break;
            case "Fullscreen": ToggleFullscreen(); break;
            case "Escape": if (fullscreen) SetFullscreen(false); break;
            case "Open": OpenClicked(this, new RoutedEventArgs()); break;
            case "Overview": Reader.ToggleOverview(); break;
            case "ZoomIn": ChangeWidth(.05); break;
            case "ZoomOut": ChangeWidth(-.05); break;
        }
    }

    private async void PreviousClicked(object sender, RoutedEventArgs e) => await Reader.NavigateAsync(-1);
    private async void NextClicked(object sender, RoutedEventArgs e) => await Reader.NavigateAsync(1);
    private void OverviewClicked(object sender, RoutedEventArgs e) { Reader.ToggleOverview(); Viewport.Focus(); }
    private void SmallerClicked(object sender, RoutedEventArgs e) => ChangeWidth(-.05);
    private void LargerClicked(object sender, RoutedEventArgs e) => ChangeWidth(.05);
    private void ChangeWidth(double delta, Point? anchor = null) => Reader.SetWidthRatio(
        Math.Clamp(Reader.WidthRatio + delta, .25, 2), anchor?.X, anchor?.Y);
    private void ClassificationChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!updatingControls && ClassificationChoice is not null)
            Reader.SetClassification((PageClassificationOverride)ClassificationChoice.SelectedIndex);
    }
    private void MouseWheelChanged(object sender, MouseWheelEventArgs e)
    {
        if (!Viewport.IsMouseOver) return;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            ChangeWidth(Math.Sign(e.Delta) * .05, e.GetPosition(Viewport));
        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) Reader.ScrollBy(-e.Delta, 0);
        else Reader.ScrollBy(0, -e.Delta);
        e.Handled = true;
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
        updatingControls = true;
        ClassificationChoice.SelectedIndex = (int)Reader.Classification;
        WidthLabel.Text = $"Largura: {Reader.WidthRatio:P0}";
        updatingControls = false;
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
