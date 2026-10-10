using System.Security.Cryptography;
using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Infrastructure.Imaging;
using SharpCompress.Archives.Rar;
using SharpCompress.Readers;
using SharpCompress.Readers.Rar;
using Xunit;

namespace ComicBookReader.Integration.Tests.Archives;

// T09 adapter/decoder evidence. Product CBR sessions and cache policies are tested in T22/T23.
public sealed class ArchiveCompatibilityTests
{
    private const string JpegHash = "b251c7501fb0f55dd4a92feabe0a6f5733bc40a02679498155fae9b30138fc53";

    [Theory]
    [InlineData("rar4.cbr", "9d9c261e50d3a84ab11a3701c7a736057fc970e59ca365885a41f60270c1875e")]
    [InlineData("rar5.cbr", "3319de3e8a91a58d08d8a83f48ebae7e6b022c542b4a58775bab64f4063988a9")]
    public async Task NonSolidArchiveIndexesAndReadsTheReferenceJpegTwice(string fixture, string expectedArchiveHash)
    {
        var path = FixturePath(fixture);
        Assert.Equal(expectedArchiveHash, HashFile(path));
        byte[] first;
        byte[] back;
        using (var file = File.OpenRead(path))
        using (var archive = RarArchive.OpenArchive(file, ReaderOptions.ForExternalStream))
        {
            Assert.False(archive.IsSolid);
            var entries = archive.Entries.Where(entry => !entry.IsDirectory).ToArray();
            Assert.Equal(3, entries.Length);
            Assert.All(entries, entry => Assert.False(entry.IsEncrypted));
            var page = Assert.Single(entries, entry => IsImage(entry.Key));
            Assert.Equal("jpg/test.jpg", NormalizeKey(page.Key));
            Assert.Equal(40372L, page.Size);
            using (var stream = page.OpenEntryStream()) first = await ReadBytesAsync(stream);
            using (var stream = page.OpenEntryStream()) back = await ReadBytesAsync(stream);
        }

        AssertReferenceEncodedBytes(first);
        Assert.Equal(first, back);
        await AssertReferencePixelsAsync(first);
        AssertReleasedAndUnchanged(path, expectedArchiveHash);
    }

    [Fact]
    public async Task SolidReaderMaterializesDependentJpegAndDecodesItAfterArchiveIsClosed()
    {
        const string archiveHash = "3043d68c5a81f46adcbb5393632d243944ead44eb383a677ac82e68afdde27cf";
        var path = FixturePath("solid.cbr");
        Assert.Equal(archiveHash, HashFile(path));
        using (var file = File.OpenRead(path))
        using (var archive = RarArchive.OpenArchive(file, ReaderOptions.ForExternalStream))
            Assert.True(archive.IsSolid);

        var consumedEntries = new List<string>();
        var materialized = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        using (var file = File.OpenRead(path))
        using (var reader = RarReader.OpenReader(file, ReaderOptions.ForExternalStream))
        {
            while (reader.MoveToNextEntry())
            {
                TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
                if (reader.Entry.IsDirectory) continue;
                Assert.False(reader.Entry.IsEncrypted);
                var key = NormalizeKey(reader.Entry.Key);
                consumedEntries.Add(key);
                using var stream = reader.OpenEntryStream();
                if (IsImage(key)) materialized.Add(key, await ReadBytesAsync(stream));
                else await stream.CopyToAsync(Stream.Null, TestContext.Current.CancellationToken);
            }
        }

        Assert.Equal(new[] { "тест.txt", "exe/test.exe", "jpg/test.jpg" }, consumedEntries);
        Assert.Equal("jpg/test.jpg", Assert.Single(materialized).Key);
        var encoded = materialized["jpg/test.jpg"];
        AssertReferenceEncodedBytes(encoded);
        AssertReleasedAndUnchanged(path, archiveHash);

        // Independent streams over owned materialized bytes; the original Reader is already disposed.
        using var firstStream = new MemoryStream(encoded, writable: false);
        using var backStream = new MemoryStream(encoded, writable: false);
        var decoder = new SkiaPageDecoder();
        using var first = await decoder.DecodeAsync(firstStream, TestContext.Current.CancellationToken);
        using var back = await decoder.DecodeAsync(backStream, TestContext.Current.CancellationToken);
        AssertReferencePixels(first);
        AssertReferencePixels(back);
        Assert.Equal(first.Pixels.ToArray(), back.Pixels.ToArray());
        AssertReleasedAndUnchanged(path, archiveHash);
    }

    private static bool IsImage(string? key) => Path.GetExtension(key ?? string.Empty).ToLowerInvariant()
        is ".jpg" or ".jpeg" or ".png" or ".webp";

    private static string NormalizeKey(string? key) => (key ?? string.Empty).Replace('\\', '/');

    private static async Task<byte[]> ReadBytesAsync(Stream stream)
    {
        using var bytes = new MemoryStream();
        await stream.CopyToAsync(bytes, TestContext.Current.CancellationToken);
        return bytes.ToArray();
    }

    private static void AssertReferenceEncodedBytes(byte[] encoded)
    {
        Assert.Equal(40372, encoded.Length);
        Assert.Equal(JpegHash, Convert.ToHexStringLower(SHA256.HashData(encoded)));
    }

    private static async Task AssertReferencePixelsAsync(byte[] encoded)
    {
        using var stream = new MemoryStream(encoded, writable: false);
        using var image = await new SkiaPageDecoder().DecodeAsync(stream, TestContext.Current.CancellationToken);
        AssertReferencePixels(image);
    }

    private static void AssertReferencePixels(PageImage image)
    {
        Assert.Equal(new PixelSize(384, 512), image.Size);
        Assert.Equal(384 * 4, image.Stride);
        Assert.Equal(image.Stride * image.Size.Height, image.Pixels.Length);
        // Independent Pillow RGB samples of the official original JPEG; tolerate JPEG decoder rounding.
        AssertBgraPixel(image, 0, 0, 0, 108, 94);
        AssertBgraPixel(image, 96, 128, 12, 52, 34);
        AssertBgraPixel(image, 192, 256, 126, 127, 118);
        AssertBgraPixel(image, 288, 384, 21, 30, 39);
        AssertBgraPixel(image, 383, 511, 27, 71, 42);
    }

    private static void AssertBgraPixel(PageImage image, int x, int y, int blue, int green, int red)
    {
        var pixel = image.Pixels.Span.Slice(y * image.Stride + x * 4, 4);
        Assert.InRange((int)pixel[0], Math.Max(0, blue - 3), Math.Min(255, blue + 3));
        Assert.InRange((int)pixel[1], Math.Max(0, green - 3), Math.Min(255, green + 3));
        Assert.InRange((int)pixel[2], Math.Max(0, red - 3), Math.Min(255, red + 3));
        Assert.Equal((byte)255, pixel[3]);
    }

    private static void AssertReleasedAndUnchanged(string path, string expectedHash)
    {
        using var exclusive = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        Assert.Equal(expectedHash, Convert.ToHexStringLower(SHA256.HashData(exclusive)));
    }

    private static string HashFile(string path)
    {
        using var file = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(file));
    }

    private static string FixturePath(string name)
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ComicBookReader.slnx")))
                return Path.Combine(directory.FullName, "tests", "fixtures", name);
        }
        throw new DirectoryNotFoundException("Could not find the ComicBookReader solution root for RAR fixtures.");
    }
}
