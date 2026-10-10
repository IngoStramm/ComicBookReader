namespace ComicBookReader.Core.ComicFiles;

// Constructor transfers ownership of the pixel array; consumers receive read-only leases.
public sealed class PageImage : IDisposable
{
    private sealed class SharedBuffer(byte[] pixels)
    {
        public byte[]? Pixels = pixels;
        public int References = 1;
    }

    private readonly SharedBuffer buffer;
    private bool disposed;
    public PixelSize Size { get; }
    public int Stride { get; }
    public long ByteLength => (long)Stride * Size.Height;

    public PageImage(PixelSize size, int stride, byte[] pixels)
    {
        if (size.Width <= 0 || size.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(size));
        if (stride < (long)size.Width * 4 || pixels.LongLength < (long)stride * size.Height)
            throw new ArgumentException("Pixel buffer or stride is too small.");
        Size = size;
        Stride = stride;
        buffer = new SharedBuffer(pixels);
    }

    private PageImage(PageImage owner)
    {
        buffer = owner.buffer;
        Size = owner.Size;
        Stride = owner.Stride;
        buffer.References++;
    }

    public ReadOnlyMemory<byte> Pixels
    {
        get
        {
            lock (buffer)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                return buffer.Pixels!;
            }
        }
    }

    public PageImage Retain()
    {
        lock (buffer)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return new PageImage(this);
        }
    }

    public void Dispose()
    {
        lock (buffer)
        {
            if (disposed) return;
            disposed = true;
            if (--buffer.References == 0) buffer.Pixels = null;
        }
    }
}
