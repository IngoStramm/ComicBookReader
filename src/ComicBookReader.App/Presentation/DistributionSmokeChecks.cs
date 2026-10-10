using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ComicBookReader.App.Presentation;

internal static class DistributionSmokeChecks
{
    public static void Run(string output)
    {
        foreach (string language in new[] { "pt-BR", "en" })
        {
            var dialog = new AboutWindow(language)
            { Left = -10000, Top = -10000, WindowStartupLocation = WindowStartupLocation.Manual, ShowActivated = false };
            Require(dialog.Title.StartsWith(language == "en" ? "About" : "Sobre", StringComparison.Ordinal),
                "About dialog was not localized.");
            Require(dialog.LicensesText.Contains("SharpCompress", StringComparison.Ordinal) &&
                dialog.LicensesText.Contains("SkiaSharp", StringComparison.Ordinal), "Third-party notices are missing.");
            if (Environment.GetEnvironmentVariable("COMICBOOKREADER_LICENSES_SHA256") is { } expected)
            {
                string actual = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dialog.LicensesText)));
                Require(actual.Equals(expected, StringComparison.OrdinalIgnoreCase), "Bundled licenses do not match the distribution texts.");
                Require(dialog.LicensesText.Contains("Permission is hereby granted", StringComparison.Ordinal), "Full license texts are missing.");
            }
            dialog.Show(); dialog.UpdateLayout();
            Require(dialog.ActualHeight > 0 && dialog.Icon is not null, "About dialog did not render.");
            var target = new RenderTargetBitmap((int)dialog.ActualWidth, (int)dialog.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            target.Render(dialog);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(target));
            using (var file = File.Create(Path.Combine(output, $"about-{language}.png"))) encoder.Save(file);
            dialog.Close();
        }
    }

    public static Dictionary<string, string> CheckNativeModules()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var process = Process.GetCurrentProcess();
        foreach (ProcessModule module in process.Modules)
        {
            if (module.ModuleName.Equals("coreclr.dll", StringComparison.OrdinalIgnoreCase) ||
                module.ModuleName.Equals("libSkiaSharp.dll", StringComparison.OrdinalIgnoreCase))
                result[module.ModuleName] = Path.GetFullPath(module.FileName);
        }
        if (!result.ContainsKey("coreclr.dll"))
        {
            // .NET 10 self-contained single-file hosts link CoreCLR statically.
            var main = process.MainModule ?? throw new InvalidOperationException("Missing process module.");
            Require(NativeLibrary.TryGetExport(main.BaseAddress, "DotNetRuntimeInfo", out _), "Embedded CoreCLR export was not found.");
            result["coreclr (embedded)"] = Path.GetFullPath(main.FileName);
        }
        Require(result.Count == 2 && result.ContainsKey("libSkiaSharp.dll"), "Runtime or native decoder was not loaded.");
        if (Environment.GetEnvironmentVariable("COMICBOOKREADER_RUNTIME_DIRECTORY") is { } expectedDirectory)
        {
            string prefix = Path.GetFullPath(expectedDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Require(result.All(module => module.Key == "coreclr (embedded)"
                ? module.Value.Equals(Path.GetFullPath(Environment.ProcessPath!), StringComparison.OrdinalIgnoreCase)
                : module.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)),
                "Native runtime or decoder came from outside the distribution.");
        }
        return result;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
