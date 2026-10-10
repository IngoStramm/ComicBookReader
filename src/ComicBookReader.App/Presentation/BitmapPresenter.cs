using System.Windows.Media;
using System.Windows.Media.Imaging;
using ComicBookReader.Core.ComicFiles;

namespace ComicBookReader.App.Presentation;

public static class BitmapPresenter
{
    public static BitmapSource Create(PageImage image)
    {
        var bitmap = BitmapSource.Create(image.Size.Width, image.Size.Height, 96, 96,
            PixelFormats.Pbgra32, null, image.Pixels.ToArray(), image.Stride);
        bitmap.Freeze();
        return bitmap;
    }
}
