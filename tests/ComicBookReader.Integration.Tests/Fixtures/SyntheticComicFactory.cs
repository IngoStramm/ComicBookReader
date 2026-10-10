using System.IO.Compression;
using SkiaSharp;

namespace ComicBookReader.Integration.Tests.Fixtures;

public sealed class SyntheticComicFactory : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "ComicBookReader-tests", Guid.NewGuid().ToString("N"));

    public SyntheticComicFactory() => Directory.CreateDirectory(directory);

    public string CreateZip(params (string Name, byte[] Content)[] entries)
    {
        var path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".bin");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            using var stream = archive.CreateEntry(name).Open();
            stream.Write(content);
        }
        return path;
    }

    public static byte[] Image(SKEncodedImageFormat format, int width = 40, int height = 20, bool alpha = false)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint();
        var colors = new[] { SKColors.Red, SKColors.Green, SKColors.Blue, SKColors.Yellow };
        for (var i = 0; i < 4; i++)
        {
            paint.Color = alpha ? new SKColor(200, 100, 50, 128) : colors[i];
            canvas.DrawRect(new SKRect(i % 2 * width / 2f, i / 2 * height / 2f,
                (i % 2 + 1) * width / 2f, (i / 2 + 1) * height / 2f), paint);
        }
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 100);
        return data.ToArray();
    }

    // Minimal little-endian TIFF IFD in a real JPEG APP1 segment: tag 0x0112 (orientation).
    public static byte[] OrientedJpeg(byte orientation)
    {
        var jpeg = Image(SKEncodedImageFormat.Jpeg);
        byte[] exif = [0xff, 0xe1, 0, 34, (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0,
            (byte)'I', (byte)'I', 42, 0, 8, 0, 0, 0, 1, 0,
            0x12, 1, 3, 0, 1, 0, 0, 0, orientation, 0, 0, 0, 0, 0, 0, 0];
        return [.. jpeg.AsSpan(0, 2), .. exif, .. jpeg.AsSpan(2)];
    }

    public static byte[] OversizedPng()
    {
        var png = Image(SKEncodedImageFormat.Png);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(16), 12_000);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(20), 8_001);
        uint crc = uint.MaxValue;
        foreach (var value in png.AsSpan(12, 17))
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320u : 0);
        }
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(29), ~crc);
        return png;
    }

    public void Dispose() => Directory.Delete(directory, true);
}
