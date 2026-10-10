namespace ComicBookReader.Core.Preferences;

public sealed record ReaderPreferences
{
    public int SchemaVersion { get; init; } = 1;
    public double PreferredPageWidthRatio { get; init; } = 1;
    public double ScrollSpeed { get; init; } = .6;
    public bool LastFullscreen { get; init; } = true;
    public Dictionary<string, string> KeyBindings { get; init; } = new(StringComparer.Ordinal)
    {
        ["NextPage"] = "PageDown",
        ["PreviousPage"] = "PageUp",
        ["Overview"] = "V",
        ["Fullscreen"] = "F11",
        ["Open"] = "Ctrl+O",
        ["ZoomIn"] = "OemPlus",
        ["ZoomOut"] = "OemMinus"
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
