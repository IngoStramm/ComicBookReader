using System.Diagnostics.CodeAnalysis;
using ComicBookReader.Core.ComicFiles;

namespace ComicBookReader.Infrastructure.Imaging;

/// <summary>Owns retained image leases within an additional-image byte budget.</summary>
public sealed class PageImageCache : IDisposable
{
    public const long DefaultBudgetBytes = 256L * 1024 * 1024;
    private readonly object sync = new();
    private readonly Dictionary<int, PageImage> images = [];
    private readonly LinkedList<int> order = [];
    private readonly long budgetBytes;
    private long byteCount;
    private bool disposed;

    public PageImageCache(long budgetBytes = DefaultBudgetBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(budgetBytes);
        this.budgetBytes = budgetBytes;
    }

    public long ByteCount { get { lock (sync) return byteCount; } }

    public bool Store(int pageIndex, PageImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (image.ByteLength > budgetBytes) return false;
            var retained = image.Retain();
            RemoveCore(pageIndex);
            while (byteCount > budgetBytes - retained.ByteLength) RemoveCore(order.First!.Value);
            images.Add(pageIndex, retained);
            order.AddLast(pageIndex);
            byteCount += retained.ByteLength;
            return true;
        }
    }

    public bool TryGet(int pageIndex, [NotNullWhen(true)] out PageImage? image)
    {
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            image = images.TryGetValue(pageIndex, out var owner) ? owner.Retain() : null;
            if (image is null) return false;
            order.Remove(pageIndex);
            order.AddLast(pageIndex);
            return true;
        }
    }

    public void Remove(int pageIndex) { lock (sync) RemoveCore(pageIndex); }

    public void KeepAdjacentTo(int pageIndex)
    {
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            foreach (var index in images.Keys.ToArray())
                if (index != pageIndex - 1 && index != pageIndex + 1) RemoveCore(index);
        }
    }

    private void RemoveCore(int pageIndex)
    {
        if (!images.Remove(pageIndex, out var image)) return;
        order.Remove(pageIndex);
        byteCount -= image.ByteLength;
        image.Dispose();
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed) return;
            disposed = true;
            foreach (var image in images.Values) image.Dispose();
            images.Clear();
            order.Clear();
            byteCount = 0;
        }
    }
}
