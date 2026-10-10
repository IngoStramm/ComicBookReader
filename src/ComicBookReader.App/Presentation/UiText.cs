using System.Globalization;
using ComicBookReader.Core.Contracts;

namespace ComicBookReader.App.Presentation;

internal sealed class UiText(string language)
{
    public CultureInfo Culture { get; } = CultureInfo.GetCultureInfo(language);
    public string Choose(string portuguese, string english) => language == "en" ? english : portuguese;
    public string this[string key]
    {
        get
        {
            var pair = Translations[key];
            return Choose(pair.Pt, pair.En);
        }
    }

    public IEnumerable<string> Keys => Translations.Keys;
    public string Error(ReaderException error) => language != "en" ? error.Message : error.Code switch
    {
        ReaderErrorCode.FileUnavailable => "The comic file could not be opened. Check its location and permissions.",
        ReaderErrorCode.InvalidArchive => "The comic archive is invalid or damaged.",
        ReaderErrorCode.UnsupportedArchive => "This archive is not supported. Passwords and multi-volume archives are not supported.",
        ReaderErrorCode.NoImages => "This archive contains no supported images (JPEG, PNG or WebP).",
        ReaderErrorCode.PageDecodeFailed => "This page could not be decoded. Try another page.",
        ReaderErrorCode.ResourceLimit => "The archive or image exceeds the reader's resource limits.",
        ReaderErrorCode.StorageUnavailable => this["SaveWarning"],
        _ => "The comic could not be read."
    };

    public string Warning(string message) => language != "en" ? message : message switch
    {
        "Preferências recuperadas do backup; o arquivo anterior foi preservado." => "Preferences recovered from backup; the previous file was preserved.",
        "As preferências usam uma versão futura; alterações não serão salvas." => "These preferences use a newer version; changes will not be saved.",
        _ => "Preferences could not be fully recovered or saved. Reading can continue; existing files were preserved."
    };

    private static readonly Dictionary<string, (string Pt, string En)> Translations = new()
    {
        ["ReadingArea"] = ("Área de leitura", "Reading area"),
        ["EmptyHeading"] = ("Abra uma HQ para começar a leitura", "Open a comic to start reading"),
        ["DropHint"] = ("Você também pode arrastar um arquivo para esta janela.", "You can also drop a file into this window."),
        ["OpenButton"] = ("_Abrir…", "_Open…"),
        ["Open"] = ("Abrir HQ", "Open comic"),
        ["NextPage"] = ("Próxima página", "Next page"),
        ["PreviousPage"] = ("Página anterior", "Previous page"),
        ["OverviewButton"] = ("Página _inteira", "_Whole page"),
        ["Overview"] = ("Página inteira / retornar", "Whole page / restore"),
        ["ZoomIn"] = ("Aumentar largura", "Increase reading width"),
        ["ZoomOut"] = ("Diminuir largura", "Decrease reading width"),
        ["Classification"] = ("Tipo de página · global", "Page type · global"),
        ["Auto"] = ("Automática", "Automatic"),
        ["Single"] = ("Simples", "Single"),
        ["Spread"] = ("Dupla", "Spread"),
        ["FullscreenButton"] = ("_Tela cheia", "_Fullscreen"),
        ["Fullscreen"] = ("Tela cheia", "Fullscreen"),
        ["PreferencesButton"] = ("_Preferências…", "_Preferences…"),
        ["Preferences"] = ("Preferências", "Preferences"),
        ["AboutButton"] = ("_Sobre…", "_About…"),
        ["AboutTitle"] = ("Sobre · ComicBookReader", "About · ComicBookReader"),
        ["ThirdPartyLicenses"] = ("Licenças e avisos de terceiros", "Third-party licenses and notices"),
        ["Close"] = ("Fechar", "Close"),
        ["ResetWidthButton"] = ("Largura _padrão", "_Default width"),
        ["ResetWidth"] = ("Usar o padrão global de largura nesta HQ", "Use the global default width for this comic"),
        ["ToggleToolbar"] = ("Mostrar / esconder barra", "Show / hide toolbar"),
        ["ToolbarButton"] = ("_Esconder barra", "_Hide toolbar"),
        ["NoComic"] = ("Nenhuma HQ aberta", "No comic open"),
        ["Width"] = ("Largura", "Width"),
        ["Loading"] = ("Carregando…", "Loading…"),
        ["PreparingRar"] = ("Preparando RAR", "Preparing RAR"),
        ["Entries"] = ("entradas", "entries"),
        ["FileFilter"] = ("Histórias em quadrinhos|*.cbz;*.cbr|Todos os arquivos|*.*", "Comic books|*.cbz;*.cbr|All files|*.*"),
        ["SaveWarning"] = ("As alterações desta sessão não puderam ser salvas.", "Changes made in this session could not be saved."),
        ["PreferencesTitle"] = ("Preferências · ComicBookReader", "Preferences · ComicBookReader"),
        ["PreferencesHeading"] = ("Preferências de leitura", "Reading preferences"),
        ["DefaultWidth"] = ("Largura padrão de uma página (%) · 25 a 200", "Default single-page width (%) · 25 to 200"),
        ["ScrollSpeed"] = ("Velocidade (% da altura da tela por segundo) · 10 a 200", "Scroll speed (% of screen height per second) · 10 to 200"),
        ["SpaceDistance"] = ("Distância do Espaço (% da altura da tela) · 10 a 100", "Space step (% of screen height) · 10 to 100"),
        ["SpaceHint"] = ("No fim da imagem, o próximo toque avança a página.", "At the bottom of the image, the next press advances the page."),
        ["Language"] = ("Idioma", "Language"),
        ["Shortcuts"] = ("Atalhos · setas ficam reservadas para rolagem", "Shortcuts · arrow keys are reserved for scrolling"),
        ["ShortcutError"] = ("Verifique os atalhos: use teclas válidas, sem duplicações ou setas. Exemplo: Ctrl+O.", "Check shortcuts: use valid keys without duplicates or arrows. Example: Ctrl+O."),
        ["Apply"] = ("Aplicar", "Apply"),
        ["Cancel"] = ("Cancelar", "Cancel"),
        ["Increase"] = ("Aumentar valor", "Increase value"),
        ["Decrease"] = ("Diminuir valor", "Decrease value")
    };
}
