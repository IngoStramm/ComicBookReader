using System.Windows;
using System.Windows.Controls;
using ComicBookReader.App.Presentation;
using ComicBookReader.Core.Layout;
using ComicBookReader.Core.Preferences;
using ComicBookReader.Core.Reading;

namespace ComicBookReader.App;

public partial class MainWindow
{
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
        ReadingDirectionChoice.SelectedIndex = (int)Reader.ReadingDirection;
        ReadingDirectionChoice.ToolTip = $"{text["ReadingDirection"]} ({input.Bindings["ToggleReadingDirection"]})";
        WidthLabel.Text = $"{text["Width"]}: {Reader.WidthRatio.ToString("P0", text.Culture)}";
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
        PageLabel.Text = state.Book is null ? text["NoComic"] : $"{state.PresentedIndex + 1} / {state.Pages.Count}";
        Status.Text = state.Error is { } error ? text.Error(error) : storageWarning is { } warning ? text.Warning(warning) :
            state.Mode == ReaderMode.Loading ? text["Loading"] : state.Book is null ? (string)Resources["ToolbarHint"] : System.IO.Path.GetFileName(state.Book.CanonicalPath);
        if (state.Mode == ReaderMode.Loading && state.OpenProgress is { } progress)
            Status.Text = $"{text["PreparingRar"]}: {progress.EntriesProcessed} {text["Entries"]} · {(progress.BytesMaterialized / 1048576d).ToString("F1", text.Culture)} MiB";
        overlay.Update();
    }
    private void ApplyLanguage(string language)
    {
        text = new UiText(language);
        Language = System.Windows.Markup.XmlLanguage.GetLanguage(language);
        foreach (string key in text.Keys) Resources[key] = text[key];
        foreach (var binding in input.Bindings)
        {
            string shortcut = binding.Value switch { "OemPlus" => "+", "OemMinus" => "−", _ => binding.Value };
            Resources[binding.Key + "Tip"] = $"{text[binding.Key]} ({shortcut})";
        }
        Resources["ToolbarHint"] = $"{input.Bindings["ToggleToolbar"]} · {text["ToggleToolbar"]}    {input.Bindings["Preferences"]} · {text["Preferences"]}";
    }
}
