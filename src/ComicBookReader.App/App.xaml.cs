using System.Windows;
using System.IO;
using ComicBookReader.App.Presentation;

namespace ComicBookReader.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        bool smoke = e.Args.Length == 3 && e.Args[0] == "--smoke-test";
        var window = new MainWindow(!smoke);
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
