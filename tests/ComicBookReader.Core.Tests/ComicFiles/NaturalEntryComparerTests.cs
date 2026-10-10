using ComicBookReader.Core.ComicFiles;
using Xunit;

namespace ComicBookReader.Core.Tests.ComicFiles;

public sealed class NaturalEntryComparerTests
{
    [Fact]
    public void OrdersPathsNaturallyWithoutParsingAnInteger()
    {
        string[] names = ["10.jpg", "2.jpg", "1.jpg", "999999999999999999999999.jpg", "1000000000000000000000000.jpg"];
        Assert.Equal(["1.jpg", "2.jpg", "10.jpg", "999999999999999999999999.jpg", "1000000000000000000000000.jpg"],
            names.Order(NaturalEntryComparer.Instance));
    }

    [Fact]
    public void IndexFiltersAuxiliariesAndPreservesDuplicateIdentity()
    {
        var entries = new[] { ("folder/10.png", 0), ("__MACOSX/1.jpg", 1), ("folder/2.jpg", 2),
            ("ComicInfo.xml", 3), ("folder/2.jpg", 4), ("folder/._3.jpg", 5) };
        var pages = ComicEntryIndex.Create(entries);
        Assert.Equal(["folder/2.jpg", "folder/2.jpg", "folder/10.png"], pages.Select(p => p.Page.EntryName));
        Assert.NotEqual(pages[0].Page.PageKey, pages[1].Page.PageKey);
        Assert.Equal([0, 1, 2], pages.Select(p => p.Page.Index));
    }
}
