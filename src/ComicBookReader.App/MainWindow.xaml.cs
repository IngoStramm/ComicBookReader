using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using ComicBookReader.App.Presentation;
using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Layout;
using ComicBookReader.Core.Reading;
using ComicBookReader.Core.Preferences;
using ComicBookReader.Infrastructure.Archives;
using ComicBookReader.Infrastructure.Storage;
using Microsoft.Win32;

namespace ComicBookReader.App;

public partial class MainWindow : Window
{
    public ReaderController Reader { get; }
    private PageImage? presented;
    private bool fullscreen;
    private readonly ReaderInput input;
    private bool updatingControls;
    private readonly PreferencesSaveCoordinator saves;
    private bool closing;
    private bool allowClose;
    private bool progressDirty;
    private readonly System.Windows.Threading.DispatcherTimer snapshotTimer;
    private readonly ReadingOverlay overlay;
    private string? storageWarning;
    internal bool HasStorageWarning => storageWarning is not null;

    public MainWindow(bool startFullscreen = true, ReaderPreferences? preferences = null,
        IReadingPreferencesStore? store = null, string? storageWarning = null)
    {
        Reader = new ReaderController(new ComicOpener(prefetchEnabled:
            Environment.GetEnvironmentVariable("COMICBOOKREADER_DISABLE_PREFETCH") != "1"), preferences: preferences);
        saves = new PreferencesSaveCoordinator(store ?? new JsonReadingPreferencesStore(), preferences);
        this.storageWarning = storageWarning;
        InitializeComponent();
        overlay = new ReadingOverlay(this);
        snapshotTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        snapshotTimer.Tick += (_, _) => { if (progressDirty && Reader.State.Mode != ReaderMode.Loading) PersistProgress(); };
        snapshotTimer.Start();
        Reader.Changed += ReaderChanged;
        Viewport.SizeChanged += (_, _) => UpdateViewport();
        input = new ReaderInput(this, Reader, RunCommand);
        input.Bindings = (preferences ?? new ReaderPreferences()).KeyBindings;
        saves.SaveStatusChanged += SaveStatusChanged;
        PreviewMouseWheel += MouseWheelChanged;
        Drop += OnDrop;
        Loaded += (_, _) => { SetFullscreen(startFullscreen); UpdateViewport(); Viewport.Focus(); };
        Closing += ClosingWindow;
    }

    private void ReaderChanged() { UpdateView(); progressDirty = true; }
    private void PersistProgress()
    {
        saves.Update(Reader.CapturePreferences() with { LastFullscreen = fullscreen });
        progressDirty = false;
    }
    private void SaveStatusChanged(object? sender, EventArgs e) => Dispatcher.BeginInvoke(() =>
    {
        storageWarning = saves.LastError is null ? null : "As alterações desta sessão não puderam ser salvas.";
        UpdateView();
    });
    private async void ClosingWindow(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (allowClose) return;
        e.Cancel = true;
        if (closing) return;
        closing = true;
        IsEnabled = false;
        input.Stop();
        snapshotTimer.Stop();
        overlay.Dispose();
        PersistProgress();
        await saves.FlushAsync();
        saves.SaveStatusChanged -= SaveStatusChanged;
        Reader.Changed -= ReaderChanged;
        input.Dispose();
        await Reader.DisposeAsync();
        await saves.DisposeAsync();
        allowClose = true;
        Close();
    }

    public async Task OpenPathAsync(string path)
    {
        PersistProgress();
        await saves.FlushAsync();
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
            case "NextPage": await NavigateAsync(1); break;
            case "PreviousPage": await NavigateAsync(-1); break;
            case "Fullscreen": ToggleFullscreen(); break;
            case "Escape": if (fullscreen) SetFullscreen(false); break;
            case "Open": OpenClicked(this, new RoutedEventArgs()); break;
            case "Overview": Reader.ToggleOverview(); break;
            case "ZoomIn": ChangeWidth(.05); break;
            case "ZoomOut": ChangeWidth(-.05); break;
        }
    }

    private async Task NavigateAsync(int delta) { PersistProgress(); await saves.FlushAsync(); await Reader.NavigateAsync(delta); }
    private async void PreviousClicked(object sender, RoutedEventArgs e) => await NavigateAsync(-1);
    private async void NextClicked(object sender, RoutedEventArgs e) => await NavigateAsync(1);
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
    private void ResetWidthClicked(object sender, RoutedEventArgs e) { Reader.ResetBookWidth(); Viewport.Focus(); }
    private void PreferencesClicked(object sender, RoutedEventArgs e)
    {
        input.Stop();
        var dialog = new PreferencesWindow(Reader.CapturePreferences()) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.Result is { } result)
        {
            Reader.ApplyPreferences(result);
            input.Bindings = result.KeyBindings;
            PersistProgress();
        }
        Viewport.Focus();
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
        PersistProgress();
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
        Status.Text = state.Error?.Message ?? storageWarning ?? (state.Mode == ReaderMode.Loading ? "Carregando…" :
            state.Book is null ? "Ctrl+O · Abrir HQ    F11 · Tela cheia" : System.IO.Path.GetFileName(state.Book.CanonicalPath));
        if (state.Mode == ReaderMode.Loading && state.OpenProgress is { } progress)
            Status.Text = $"Preparando RAR: {progress.EntriesProcessed} entradas · {progress.BytesMaterialized / 1048576d:F1} MiB";
        overlay.Update();
    }
}
