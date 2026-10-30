namespace ComicBookReader.Core.Preferences;

public enum ReadingDirection { LeftToRight, RightToLeft }

public sealed record ReaderPreferences
{
    public int SchemaVersion { get; init; } = 1;
    public double PreferredPageWidthRatio { get; init; } = 1;
    public double ScrollSpeed { get; init; } = .6;
    public int SpaceScrollPercent { get; init; } = 90;
    public ReadingDirection ReadingDirection { get; init; } = ReadingDirection.LeftToRight;
    public string Language { get; init; } = "pt-BR";
    public PageClassificationOverride PageClassification { get; init; } = PageClassificationOverride.Auto;
    public bool LastFullscreen { get; init; } = true;
    public Dictionary<string, string> KeyBindings { get; init; } = new(StringComparer.Ordinal)
    {
        ["NextPage"] = "PageDown",
        ["PreviousPage"] = "PageUp",
        ["Overview"] = "V",
        ["Fullscreen"] = "F11",
        ["Open"] = "Ctrl+O",
        ["ZoomIn"] = "OemPlus",
        ["ZoomOut"] = "OemMinus",
        ["ToggleToolbar"] = "F12",
        ["Preferences"] = "Ctrl+P",
        ["ResetWidth"] = "R",
        ["ToggleReadingDirection"] = "M",
        ["FirstPage"] = "Ctrl+Home",
        ["LastPage"] = "Ctrl+End",
        ["Thumbnails"] = "T"
    };
    public Dictionary<string, BookPreferences> Books { get; init; } = new(StringComparer.Ordinal);
}

public sealed record BookPreferences
{
    public double? WidthRatio { get; init; }
    public Dictionary<string, PageClassificationOverride> Classifications { get; init; } = new(StringComparer.Ordinal);
    public LastReadingPosition? Position { get; init; }
}

public sealed record LastReadingPosition(string PageKey, int PageIndex, double RelativeX, double RelativeY);
