using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Contracts;
using ComicBookReader.Infrastructure.Archives;
using ComicBookReader.Infrastructure.Imaging;
using ComicBookReader.Integration.Tests.Fixtures;
using SkiaSharp;
using Xunit;

namespace ComicBookReader.Integration.Tests.Archives;

public sealed class SolidRarSessionTests
{
    [Fact]
    public async Task CompressedSolidRarDrainsAuxiliaryEntriesAndReadsReferencePixelsRepeatedly()
    {
        using var fixture = new RarComicSessionTests.FixtureCopy("solid.cbr");
        using var workspace = new CacheWorkspace();
        var before = RarComicSessionTests.HashFile(fixture.FilePath);
        var reports = new List<ComicOpenProgress>();
        var session = await new ComicOpener(solidRarMaterializer: new SolidRarMaterializer(workspace.CacheRoot)).OpenAsync(fixture.FilePath,
            TestContext.Current.CancellationToken, new InlineProgress(reports.Add));
        try
        {
            var page = Assert.Single(session.Pages);
            Assert.Equal("jpg/test.jpg", page.EntryName);
            using var first = await session.ReadPageAsync(0, TestContext.Current.CancellationToken);
            using var previous = await session.ReadPageAsync(0, TestContext.Current.CancellationToken);
            Assert.Equal(new PixelSize(384, 512), first.Size);
            Assert.Equal(first.Pixels.ToArray(), previous.Pixels.ToArray());
            // JPEG is the third compressed file: earlier text and EXE must retain the solid dictionary.
            Assert.Contains(reports, item => item.EntriesProcessed >= 3 && item.BytesMaterialized == 40372);
            AssertPixel(first, 0, 0, 0, 108, 94);
            AssertPixel(first, 192, 256, 126, 127, 118);
            AssertPixel(first, 383, 511, 27, 71, 42);
            var cached = Assert.Single(Directory.GetFiles(workspace.CacheRoot, "*.bin", SearchOption.AllDirectories));
            Assert.Matches("^[a-f0-9]{32}\\.bin$", Path.GetFileName(cached));
            Assert.Equal("B251C7501FB0F55DD4A92FEABE0A6F5733BC40A02679498155FAE9B30138FC53",
                Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(cached, TestContext.Current.CancellationToken))));
            Assert.Empty(Directory.GetFiles(workspace.CacheRoot, "*.exe", SearchOption.AllDirectories));
        }
        finally
        {
            await session.DisposeAsync();
        }
        Assert.Empty(Directory.GetDirectories(workspace.CacheRoot));
        RarComicSessionTests.AssertReleasedAndUnchanged(fixture.FilePath, before);
    }

    [Fact]
    public async Task StoredSolidPagesHaveNaturalOrderAndNeverUseArchiveNamesAsDestinations()
    {
        using var workspace = new CacheWorkspace();
        var image = SyntheticComicFactory.Image(SKEncodedImageFormat.Png);
        var escaped = Path.Combine(workspace.DirectoryPath, "escaped.png");
        var path = workspace.CreateStoredRar(("10.png", image), ("2.png", image),
            ("1.png", image), ("../../escaped.png", image), (escaped.Replace('\\', '/'), image),
            ("metadata.xml", Encoding.UTF8.GetBytes("metadata")));
        var before = RarComicSessionTests.HashFile(path);
        var session = await new ComicOpener(solidRarMaterializer: new SolidRarMaterializer(workspace.CacheRoot))
            .OpenAsync(path, TestContext.Current.CancellationToken);
        try
        {
            var ordered = session.Pages.Where(page => page.EntryName is "1.png" or "2.png" or "10.png")
                .Select(page => page.EntryName).ToArray();
            Assert.Equal(new[] { "1.png", "2.png", "10.png" }, ordered);
            using var next = await session.ReadPageAsync(2, TestContext.Current.CancellationToken);
            using var previous = await session.ReadPageAsync(0, TestContext.Current.CancellationToken);
            using var back = await session.ReadPageAsync(2, TestContext.Current.CancellationToken);
            Assert.Equal(next.Pixels.ToArray(), back.Pixels.ToArray());
            Assert.Equal(5, Directory.GetFiles(workspace.CacheRoot, "*.bin", SearchOption.AllDirectories).Length);
            Assert.False(File.Exists(escaped));
            Assert.False(File.Exists(Path.Combine(workspace.CacheRoot, "metadata.xml")));
        }
        finally
        {
            await session.DisposeAsync();
        }
        Assert.Empty(Directory.GetDirectories(workspace.CacheRoot));
        RarComicSessionTests.AssertReleasedAndUnchanged(path, before);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LimitsReturnResourceLimitAndRemovePartialCache(bool perEntry)
    {
        using var workspace = new CacheWorkspace();
        var image = SyntheticComicFactory.Image(SKEncodedImageFormat.Png);
        var path = workspace.CreateStoredRar(("1.png", image), ("2.png", image));
        var materializer = new SolidRarMaterializer(workspace.CacheRoot,
            maximumEntryBytes: perEntry ? image.Length - 1 : image.Length,
            maximumCacheBytes: perEntry ? image.Length * 2 : image.Length * 2 - 1);
        var error = await Assert.ThrowsAsync<ReaderException>(() =>
            new ComicOpener(solidRarMaterializer: materializer).OpenAsync(path, TestContext.Current.CancellationToken));
        Assert.Equal(ReaderErrorCode.ResourceLimit, error.Code);
        Assert.Empty(Directory.GetDirectories(workspace.CacheRoot));
        using var exclusive = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        using var fixture = new RarComicSessionTests.FixtureCopy("solid.cbr");
        await using var recovered = await new ComicOpener(solidRarMaterializer: new SolidRarMaterializer(workspace.CacheRoot))
            .OpenAsync(fixture.FilePath, TestContext.Current.CancellationToken);
        using var page = await recovered.ReadPageAsync(0, TestContext.Current.CancellationToken);
        Assert.Equal(new PixelSize(384, 512), page.Size);
    }

    [Fact]
    public void LimitsCannotBeRaisedAboveTheSpecifiedCaps()
    {
        using var workspace = new CacheWorkspace();
        Assert.Throws<ArgumentOutOfRangeException>(() => new SolidRarMaterializer(workspace.CacheRoot,
            maximumEntryBytes: SkiaPageDecoder.MaximumEncodedBytes + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SolidRarMaterializer(workspace.CacheRoot,
            maximumCacheBytes: SolidRarMaterializer.MaximumCacheBytes + 1));
        Assert.Equal(128L * 1024 * 1024, SkiaPageDecoder.MaximumEncodedBytes);
        Assert.Equal(2L * 1024 * 1024 * 1024, SolidRarMaterializer.MaximumCacheBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationDuringAuxiliaryOrImageMaterializationRemovesCache(bool duringImage)
    {
        using var workspace = new CacheWorkspace();
        using var fixture = new RarComicSessionTests.FixtureCopy("solid.cbr");
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var reports = new List<ComicOpenProgress>();
        var progress = new InlineProgress(value =>
        {
            reports.Add(value);
            if (duringImage ? value.BytesMaterialized > 0 : value.EntriesProcessed > 0) cancellation.Cancel();
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ComicOpener(solidRarMaterializer: new SolidRarMaterializer(workspace.CacheRoot))
                .OpenAsync(fixture.FilePath, cancellation.Token, progress));
        Assert.Contains(reports, value => duringImage ? value.BytesMaterialized > 0 : value.EntriesProcessed > 0);
        Assert.Empty(Directory.GetDirectories(workspace.CacheRoot));
        using var exclusive = new FileStream(fixture.FilePath, FileMode.Open, FileAccess.Read, FileShare.None);
    }

    [Fact]
    public async Task CloseWaitsForActiveDecodeBeforeRemovingCacheAndRejectsQueuedRead()
    {
        using var workspace = new CacheWorkspace();
        using var fixture = new RarComicSessionTests.FixtureCopy("solid.cbr");
        var decoder = new GateDecoder();
        var session = await new ComicOpener(decoder, new SolidRarMaterializer(workspace.CacheRoot), useCache: false)
            .OpenAsync(fixture.FilePath, TestContext.Current.CancellationToken);
        var read = session.ReadPageAsync(0, TestContext.Current.CancellationToken).AsTask();
        await decoder.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var queued = session.ReadPageAsync(0, TestContext.Current.CancellationToken).AsTask();
        var close = session.DisposeAsync().AsTask();
        Assert.False(close.IsCompleted);
        Assert.Single(Directory.GetDirectories(workspace.CacheRoot));
        decoder.Release.TrySetResult();
        using var image = await read;
        await close;
        await Assert.ThrowsAsync<ObjectDisposedException>(() => queued);
        await session.DisposeAsync();
        Assert.Empty(Directory.GetDirectories(workspace.CacheRoot));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await session.ReadPageAsync(0, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartupRecoveryRemovesOwnedAbandonedCacheAndPreservesActiveAndForeignDirectories()
    {
        using var workspace = new CacheWorkspace();
        using var fixture = new RarComicSessionTests.FixtureCopy("solid.cbr");
        await using var session = await new ComicOpener(solidRarMaterializer: new SolidRarMaterializer(workspace.CacheRoot))
            .OpenAsync(fixture.FilePath, TestContext.Current.CancellationToken);
        var active = Assert.Single(Directory.GetDirectories(workspace.CacheRoot));
        var abandoned = workspace.CreateAbandonedCache();
        var foreign = Path.Combine(workspace.CacheRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(foreign);
        await File.WriteAllTextAsync(Path.Combine(foreign, "valuable.txt"), "keep", TestContext.Current.CancellationToken);
        _ = new SolidRarMaterializer(workspace.CacheRoot);
        Assert.False(Directory.Exists(abandoned));
        Assert.True(Directory.Exists(active));
        Assert.Equal("keep", await File.ReadAllTextAsync(Path.Combine(foreign, "valuable.txt"), TestContext.Current.CancellationToken));
        using var image = await session.ReadPageAsync(0, TestContext.Current.CancellationToken);
        Assert.Equal(new PixelSize(384, 512), image.Size);
    }

    [Fact]
    public void RecoveryRefusesUnownedRootAndOwnedDirectoryWithUnexpectedChild()
    {
        using var workspace = new CacheWorkspace();
        var abandoned = workspace.CreateAbandonedCache();
        _ = new SolidRarMaterializer(workspace.CacheRoot);
        Assert.True(Directory.Exists(abandoned));
        workspace.WriteRootMarker();
        var unexpected = Path.Combine(abandoned, "subdirectory");
        Directory.CreateDirectory(unexpected);
        var valuable = Path.Combine(unexpected, "valuable.txt");
        File.WriteAllText(valuable, "keep");
        _ = new SolidRarMaterializer(workspace.CacheRoot);
        Assert.True(Directory.Exists(abandoned));
        Assert.Equal("keep", File.ReadAllText(valuable));
    }

    private static void AssertPixel(PageImage image, int x, int y, int blue, int green, int red)
    {
        var pixel = image.Pixels.Span.Slice(y * image.Stride + x * 4, 4);
        Assert.InRange((int)pixel[0], Math.Max(0, blue - 3), Math.Min(255, blue + 3));
        Assert.InRange((int)pixel[1], Math.Max(0, green - 3), Math.Min(255, green + 3));
        Assert.InRange((int)pixel[2], Math.Max(0, red - 3), Math.Min(255, red + 3));
        Assert.Equal((byte)255, pixel[3]);
    }

    private sealed class InlineProgress(Action<ComicOpenProgress> report) : IProgress<ComicOpenProgress>
    {
        public void Report(ComicOpenProgress value) => report(value);
    }

    private sealed class GateDecoder : SkiaPageDecoder
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async Task<PageImage> DecodeAsync(Stream stream, CancellationToken cancellationToken)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return await base.DecodeAsync(stream, cancellationToken);
        }
    }

    private sealed class CacheWorkspace : IDisposable
    {
        public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "ComicBookReader-tests", Guid.NewGuid().ToString("N"));
        public string CacheRoot => Path.Combine(DirectoryPath, "cache");

        public CacheWorkspace() => Directory.CreateDirectory(DirectoryPath);

        public void WriteRootMarker()
        {
            Directory.CreateDirectory(CacheRoot);
            File.WriteAllText(Path.Combine(CacheRoot, ".comicbookreader-solid-cache-v1"), "ComicBookReader solid cache v1\n" + CacheRoot);
        }

        public string CreateAbandonedCache()
        {
            var directory = Path.Combine(CacheRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, ".owner"), "ComicBookReader solid cache v1\n" + directory);
            File.WriteAllBytes(Path.Combine(directory, Guid.NewGuid().ToString("N") + ".bin"), [1, 2, 3]);
            return directory;
        }

        // RAR5 stored security cases follow RARLAB's header/CRC specification; the native fixture above
        // separately proves real compression and dictionary continuity through earlier auxiliary files.
        public string CreateStoredRar(params (string Name, byte[] Bytes)[] entries)
        {
            var path = Path.Combine(DirectoryPath, "stored-solid.cbr");
            using var file = File.Create(path);
            file.Write([0x52, 0x61, 0x72, 0x21, 0x1a, 0x07, 0x01, 0x00]);
            WriteHeader(file, [1, 0, 4]);
            Span<byte> crc = stackalloc byte[4];
            for (var ordinal = 0; ordinal < entries.Length; ordinal++)
            {
                var entry = entries[ordinal];
                using var header = new MemoryStream();
                WriteVint(header, 2); // File header.
                WriteVint(header, 2); // Data area present.
                WriteVint(header, (ulong)entry.Bytes.Length);
                WriteVint(header, 4); // Data CRC present.
                WriteVint(header, (ulong)entry.Bytes.Length);
                WriteVint(header, 0x20); // Windows archive attribute.
                BinaryPrimitives.WriteUInt32LittleEndian(crc, Crc32(entry.Bytes));
                header.Write(crc);
                WriteVint(header, ordinal == 0 ? 0UL : 0x40UL); // Stored; subsequent file retains dictionary.
                WriteVint(header, 0); // Host OS Windows.
                var name = Encoding.UTF8.GetBytes(entry.Name);
                WriteVint(header, (ulong)name.Length);
                header.Write(name);
                WriteHeader(file, header.ToArray());
                file.Write(entry.Bytes);
            }
            WriteHeader(file, [5, 0, 0]);
            return path;
        }

        private static void WriteHeader(Stream file, byte[] header)
        {
            using var data = new MemoryStream();
            WriteVint(data, (ulong)header.Length);
            data.Write(header);
            Span<byte> crc = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(crc, Crc32(data.ToArray()));
            file.Write(crc);
            data.Position = 0;
            data.CopyTo(file);
        }

        private static void WriteVint(Stream stream, ulong value)
        {
            do
            {
                var next = (byte)(value & 0x7f);
                value >>= 7;
                stream.WriteByte(value == 0 ? next : (byte)(next | 0x80));
            } while (value != 0);
        }

        private static uint Crc32(ReadOnlySpan<byte> data)
        {
            uint crc = uint.MaxValue;
            foreach (var value in data)
            {
                crc ^= value;
                for (var bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0 : 0xedb88320);
            }
            return ~crc;
        }

        public void Dispose() => Directory.Delete(DirectoryPath, true);
    }
}
