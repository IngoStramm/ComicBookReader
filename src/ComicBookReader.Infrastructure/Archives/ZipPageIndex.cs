using System.IO.Compression;
using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Contracts;

namespace ComicBookReader.Infrastructure.Archives;

public static class ZipPageIndex
{
    public static IReadOnlyList<IndexedComicPage> Read(ZipArchive archive)
    {
        var pages = ComicEntryIndex.Create(archive.Entries.Select((entry, index) => (entry.FullName, index)));
        if (pages.Count == 0) throw new ReaderException(ReaderErrorCode.NoImages, "O arquivo não contém páginas JPEG, PNG ou WebP.");
        return pages;
    }
}
