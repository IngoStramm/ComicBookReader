using ComicBookReader.Core.ComicFiles;
using Xunit;

namespace ComicBookReader.Core.Tests.ComicFiles;

public sealed class ComicContractsTests
{
    [Fact]
    public void IdentityChangesWhenPathLengthOrTimestampChanges()
    {
        var stamp = new DateTime(2026, 10, 9, 0, 0, 0, DateTimeKind.Utc);
        var first = BookInfo.Create("C:/Books/a.cbz", 20, stamp);
        Assert.Equal(first.BookId, BookInfo.Create("c:/books/A.cbz", 20, stamp).BookId);
        Assert.NotEqual(first.BookId, BookInfo.Create("C:/Books/b.cbz", 20, stamp).BookId);
        Assert.NotEqual(first.BookId, BookInfo.Create("C:/Books/a.cbz", 21, stamp).BookId);
        Assert.NotEqual(first.BookId, BookInfo.Create("C:/Books/a.cbz", 20, stamp.AddTicks(1)).BookId);
    }

    [Fact]
    public void IndependentLeaseSurvivesDisposingOriginal()
    {
        var image = new PageImage(new PixelSize(2, 1), 8, new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 });
        using var retained = image.Retain();
        image.Dispose();
        image.Dispose();
        Assert.Throws<ObjectDisposedException>(() => image.Pixels);
        Assert.Equal(8, retained.Pixels.Length);
        Assert.Equal(1, retained.Pixels.Span[0]);
    }

    [Fact]
    public void InvalidImageGeometryIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PageImage(new PixelSize(0, 1), 4, new byte[4]));
        Assert.Throws<ArgumentException>(() => new PageImage(new PixelSize(2, 1), 4, new byte[4]));
        Assert.Throws<ArgumentException>(() => new PageImage(new PixelSize(2, 1), 8, new byte[7]));
    }
}
