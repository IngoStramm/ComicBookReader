using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Contracts;
using ComicBookReader.Core.Layout;

namespace ComicBookReader.Core.Reading;

public enum ReaderMode { Empty, Loading, Reading, Overview, PageError }

public sealed record ReaderState
{
    public ReaderMode Mode { get; init; } = ReaderMode.Empty;
    public BookInfo? Book { get; init; }
    public IReadOnlyList<PageInfo> Pages { get; init; } = [];
    public int TargetIndex { get; init; }
    public int PresentedIndex { get; init; }
    public PageImage? Image { get; init; }
    public PageLayout? Layout { get; init; }
    public double OffsetXDip { get; init; }
    public double OffsetYDip { get; init; }
    public ReaderException? Error { get; init; }
    public ComicOpenProgress? OpenProgress { get; init; }
    public int Generation { get; init; }
}
