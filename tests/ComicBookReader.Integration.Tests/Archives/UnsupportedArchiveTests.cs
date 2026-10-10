using System.Buffers.Binary;
using ComicBookReader.Core.Contracts;
using ComicBookReader.Infrastructure.Archives;
using ComicBookReader.Integration.Tests.Fixtures;
using SkiaSharp;
using Xunit;

namespace ComicBookReader.Integration.Tests.Archives;

public sealed class UnsupportedArchiveTests
{
    [Theory]
    [InlineData("protected.cbr")]
    [InlineData("protected-entries.cbr")]
    [InlineData("multivolume.part01.cbr")]
    public async Task UnsupportedRarIsRejectedBeforeImageDecodeAndReleasesFile(string name)
    {
        using var fixture = new RarComicSessionTests.FixtureCopy(name);
        var path = fixture.FilePath;
        var before = RarComicSessionTests.HashFile(path);
        var error = await Assert.ThrowsAsync<ReaderException>(() =>
            new ComicOpener().OpenAsync(path, TestContext.Current.CancellationToken));
        Assert.Equal(ReaderErrorCode.UnsupportedArchive, error.Code);
        RarComicSessionTests.AssertReleasedAndUnchanged(path, before);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ZipProtectedOrSplitDirectoryReturnsUnsupported(bool encrypted)
    {
        using var fixture = new SyntheticComicFactory();
        var path = fixture.CreateZip(("1.png", SyntheticComicFactory.Image(SKEncodedImageFormat.Png)));
        var bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        var central = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(bytes.Length - 6)));
        if (encrypted)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(6), 1);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(central + 8), 1);
        }
        else BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(central + 34), 1);
        await File.WriteAllBytesAsync(path, bytes, TestContext.Current.CancellationToken);
        var before = RarComicSessionTests.HashFile(path);
        var error = await Assert.ThrowsAsync<ReaderException>(() =>
            new ComicOpener().OpenAsync(path, TestContext.Current.CancellationToken));
        Assert.Equal(ReaderErrorCode.UnsupportedArchive, error.Code);
        RarComicSessionTests.AssertReleasedAndUnchanged(path, before);
    }

    [Fact]
    public async Task UnknownContentHasInvalidArchiveCodeDespiteComicExtension()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cbr");
        await File.WriteAllBytesAsync(path, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, TestContext.Current.CancellationToken);
        try
        {
            var error = await Assert.ThrowsAsync<ReaderException>(() =>
                new ComicOpener().OpenAsync(path, TestContext.Current.CancellationToken));
            Assert.Equal(ReaderErrorCode.InvalidArchive, error.Code);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
