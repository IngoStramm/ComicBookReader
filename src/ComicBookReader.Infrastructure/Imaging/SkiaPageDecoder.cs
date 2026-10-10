using System.Runtime.InteropServices;
using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Contracts;
using SkiaSharp;

namespace ComicBookReader.Infrastructure.Imaging;

public class SkiaPageDecoder
{
    public const long MaximumEncodedBytes = 128L * 1024 * 1024;
    public const long MaximumPixels = 96_000_000;

    public virtual Task<PageImage> DecodeAsync(Stream stream, CancellationToken cancellationToken) =>
        Task.Run(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (stream.CanSeek && stream.Length - stream.Position > MaximumEncodedBytes) throw LimitExceeded();
                using var encoded = new MemoryStream();
                var chunk = new byte[64 * 1024];
                while (true)
                {
                    var count = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false);
                    if (count == 0) break;
                    if (encoded.Length + count > MaximumEncodedBytes) throw LimitExceeded();
                    encoded.Write(chunk, 0, count);
                }
                cancellationToken.ThrowIfCancellationRequested();
                encoded.Position = 0;
                using var codec = SKCodec.Create(encoded);
                if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
                    throw DecodeFailed();
                var sourceInfo = codec.Info;
                if (sourceInfo.Width <= 0 || sourceInfo.Height <= 0) throw DecodeFailed();
                if ((long)sourceInfo.Width * sourceInfo.Height > MaximumPixels) throw LimitExceeded();
                var rawInfo = new SKImageInfo(sourceInfo.Width, sourceInfo.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
                using var raw = new SKBitmap(rawInfo);
                if (raw.GetPixels() == IntPtr.Zero) throw LimitExceeded();
                if (codec.GetPixels(rawInfo, raw.GetPixels(), raw.RowBytes, SKCodecOptions.Default) != SKCodecResult.Success)
                    throw DecodeFailed();
                cancellationToken.ThrowIfCancellationRequested();
                var origin = codec.EncodedOrigin;
                var swapped = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
                var width = swapped ? sourceInfo.Height : sourceInfo.Width;
                var height = swapped ? sourceInfo.Width : sourceInfo.Height;
                if (origin == SKEncodedOrigin.TopLeft) return CopyPixels(raw, cancellationToken);
                using var normalized = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
                if (normalized.GetPixels() == IntPtr.Zero) throw LimitExceeded();
                using (var canvas = new SKCanvas(normalized))
                {
                    canvas.SetMatrix(OrientationMatrix(origin, sourceInfo.Width, sourceInfo.Height));
                    using var paint = new SKPaint { BlendMode = SKBlendMode.Src, IsAntialias = false };
                    canvas.DrawBitmap(raw, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest), paint);
                    canvas.Flush();
                }
                return CopyPixels(normalized, cancellationToken);
            }
            catch (OutOfMemoryException exception)
            {
                throw new ReaderException(ReaderErrorCode.ResourceLimit, "A imagem excede os recursos disponíveis.", exception);
            }
            catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException)
            {
                throw new ReaderException(ReaderErrorCode.PageDecodeFailed, "Não foi possível ler a imagem desta página.", exception);
            }
        }, cancellationToken);

    private static PageImage CopyPixels(SKBitmap bitmap, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var pixels = new byte[checked(bitmap.RowBytes * bitmap.Height)];
        Marshal.Copy(bitmap.GetPixels(), pixels, 0, pixels.Length);
        cancellationToken.ThrowIfCancellationRequested();
        return new PageImage(new PixelSize(bitmap.Width, bitmap.Height), bitmap.RowBytes, pixels);
    }

    // Maps source pixel boundaries into upright output boundaries; dimensions were validated first.
    private static SKMatrix OrientationMatrix(SKEncodedOrigin origin, int width, int height) => origin switch
    {
        SKEncodedOrigin.TopRight => new(-1, 0, width, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new(-1, 0, width, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new(1, 0, 0, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new(0, -1, height, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new(0, -1, height, -1, 0, width, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new(0, 1, 0, -1, 0, width, 0, 0, 1),
        _ => SKMatrix.Identity
    };

    private static ReaderException LimitExceeded() => new(ReaderErrorCode.ResourceLimit,
        "A página excede o limite de 128 MiB ou 96 milhões de pixels.");
    private static ReaderException DecodeFailed() => new(ReaderErrorCode.PageDecodeFailed,
        "Não foi possível decodificar a imagem desta página.");
}
