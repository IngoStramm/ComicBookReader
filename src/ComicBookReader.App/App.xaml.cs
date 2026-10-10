using System.IO;
using System.Windows;
using ComicBookReader.App.Presentation;
using ComicBookReader.Core.Contracts;
using ComicBookReader.Core.Preferences;
using ComicBookReader.Infrastructure.Storage;

namespace ComicBookReader.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        bool smoke = e.Args.Length == 3 && e.Args[0] == "--smoke-test";
        var store = new JsonReadingPreferencesStore(smoke ? Path.Combine(Path.GetFullPath(e.Args[2]), "preferences") : null);
        ReaderPreferences preferences;
        string? warning;
        try { preferences = await store.LoadAsync(CancellationToken.None); warning = store.LastWarning; }
        catch (ReaderException) { preferences = new(); warning = "Não foi possível recuperar as preferências. Os padrões foram aplicados."; }
        var window = new MainWindow(!smoke && preferences.LastFullscreen, preferences, store, warning);
        if (smoke) { window.Left = -10000; window.Top = -10000; window.ShowActivated = false; }
        MainWindow = window;
        window.Show();
        if (smoke)
        {
            try { await SmokeTestRunner.RunAsync(window, e.Args[1], e.Args[2]); }
            catch (Exception ex)
            {
                Directory.CreateDirectory(e.Args[2]);
                await File.WriteAllTextAsync(Path.Combine(e.Args[2], "failure.txt"), ex.ToString());
                Shutdown(1);
                return;
            }
            window.Close();
        }
        else if (e.Args.Length == 1) await window.OpenPathAsync(e.Args[0]);
    }
}
