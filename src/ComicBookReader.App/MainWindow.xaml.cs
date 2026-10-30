using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ComicBookReader.App.Presentation;
using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Layout;
using ComicBookReader.Core.Preferences;
using ComicBookReader.Core.Reading;
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
    internal ReaderDrag PageDrag { get; }
    private bool updatingControls;
    private readonly PreferencesSaveCoordinator saves;
    private bool closing;
    private bool allowClose;
    private bool progressDirty;
    private readonly System.Windows.Threading.DispatcherTimer snapshotTimer;
    private readonly ReadingOverlay overlay;
    private string? storageWarning;
    private UiText text;
    internal bool HasStorageWarning => storageWarning is not null;

    public MainWindow(bool startFullscreen = true, ReaderPreferences? preferences = null,
        IReadingPreferencesStore? store = null, string? storageWarning = null)
    {
        Reader = new ReaderController(new ComicOpener(prefetchEnabled:
            Environment.GetEnvironmentVariable("COMICBOOKREADER_DISABLE_PREFETCH") != "1"), preferences: preferences);
        saves = new PreferencesSaveCoordinator(store ?? new JsonReadingPreferencesStore(), preferences);
        this.storageWarning = storageWarning;
        text = new UiText((preferences ?? new ReaderPreferences()).Language);
        InitializeComponent();
        overlay = new ReadingOverlay(this);
        snapshotTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        snapshotTimer.Tick += (_, _) => { if (progressDirty && Reader.State.Mode != ReaderMode.Loading) PersistProgress(); };
        snapshotTimer.Start();
        Reader.Changed += ReaderChanged;
        Viewport.SizeChanged += (_, _) => UpdateViewport();
        input = new ReaderInput(this, Reader, RunCommand);
        input.Bindings = (preferences ?? new ReaderPreferences()).KeyBindings;
        PageDrag = new ReaderDrag(this, Viewport, Reader, input.Stop);
        ApplyLanguage((preferences ?? new ReaderPreferences()).Language);
        Reader.ApplyPreferences(preferences ?? new ReaderPreferences());
        UpdateView();
        Toolbar.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(ToolbarButtonClicked));
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
        storageWarning = saves.LastError is null ? null : text["SaveWarning"];
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
        PageDrag.Dispose();
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

}
