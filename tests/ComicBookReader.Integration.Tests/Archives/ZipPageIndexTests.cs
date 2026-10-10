using System.IO.Compression;
using ComicBookReader.Core.Contracts;
using ComicBookReader.Infrastructure.Archives;
using Xunit;

namespace ComicBookReader.Integration.Tests.Archives;

public sealed class ZipPageIndexTests
{
    [Fact]
    public void ZipIndexUsesActualArchiveAndIncludesNestedImages()
    {
        using var memory = new MemoryStream();
        using (var writer = new ZipArchive(memory, ZipArchiveMode.Create, true))
        {
            writer.CreateEntry("folder/10.webp");
            writer.CreateEntry("folder/2.png");
            writer.CreateEntry("1.jpg");
            writer.CreateEntry("ComicInfo.xml");
        }
        memory.Position = 0;
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
        Assert.Equal(["1.jpg", "folder/2.png", "folder/10.webp"],
            ZipPageIndex.Read(archive).Select(p => p.Page.EntryName));
    }

    [Fact]
    public void EmptyZipHasSpecificError()
    {
        using var memory = new MemoryStream();
        using (var writer = new ZipArchive(memory, ZipArchiveMode.Create, true)) { }
        memory.Position = 0;
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
        Assert.Equal(ReaderErrorCode.NoImages, Assert.Throws<ReaderException>(() => ZipPageIndex.Read(archive)).Code);
    }
}
