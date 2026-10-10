using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Contracts;
using ComicBookReader.Infrastructure.Imaging;

namespace ComicBookReader.Infrastructure.Archives;

/// <summary>Owns a session, prioritizing requested pages over cancellable adjacent prefetch.</summary>
public sealed class CachedComicSession : IComicSession
{
    private readonly IComicSession inner;
    private readonly PageImageCache cache;
    private readonly bool prefetchEnabled;
    private readonly object sync = new();
    private readonly SemaphoreSlim reads = new(1, 1);
    private CancellationTokenSource? requestedCancellation;
    private CancellationTokenSource? prefetchCancellation;
    private Task prefetchTask = Task.CompletedTask;
    private TaskCompletionSource foregroundIdle = CompletedSignal();
    private Task? disposeTask;
    private PageImage? currentImage;
    private int currentIndex = -1;
    private int pendingReads;
    private long generation;
    private long publishedGeneration = -1;
    private bool closing;

    public BookInfo Book => inner.Book;
    public IReadOnlyList<PageInfo> Pages => inner.Pages;
    public long CacheBytes => cache.ByteCount;

    /// <param name="inner">Session whose lifetime is transferred to this wrapper.</param>
    /// <param name="prefetchEnabled">Disable speculative reads for diagnostics.</param>
    /// <param name="cacheBudgetBytes">Additional-image budget; the retained current image is separate.</param>
    public CachedComicSession(IComicSession inner, bool prefetchEnabled = true,
        long cacheBudgetBytes = PageImageCache.DefaultBudgetBytes)
    {
        ArgumentNullException.ThrowIfNull(inner);
        cache = new PageImageCache(cacheBudgetBytes);
        this.inner = inner;
        this.prefetchEnabled = prefetchEnabled;
    }

    public async ValueTask<PageImage> ReadPageAsync(int pageIndex, CancellationToken cancellationToken)
    {
        CancellationTokenSource requested;
        Task previousPrefetch;
        long requestGeneration;
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            if ((uint)pageIndex >= (uint)Pages.Count) throw new ArgumentOutOfRangeException(nameof(pageIndex));
            cancellationToken.ThrowIfCancellationRequested();
            requestedCancellation?.Cancel();
            prefetchCancellation?.Cancel();
            requested = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestedCancellation = requested;
            previousPrefetch = prefetchTask;
            requestGeneration = ++generation;
            if (pendingReads++ == 0) foregroundIdle = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        var acquired = false;
        PageImage? image = null;
        try
        {
            // Join the one speculative worker before acquiring the underlying read slot.
            await previousPrefetch.WaitAsync(requested.Token).ConfigureAwait(false);
            await reads.WaitAsync(requested.Token).ConfigureAwait(false);
            acquired = true;
            lock (sync)
            {
                requested.Token.ThrowIfCancellationRequested();
                if (currentIndex == pageIndex) image = currentImage!.Retain();
                else cache.TryGet(pageIndex, out image);
            }
            image ??= await inner.ReadPageAsync(pageIndex, requested.Token).ConfigureAwait(false);
            lock (sync)
            {
                requested.Token.ThrowIfCancellationRequested();
                if (closing || requestGeneration != generation) throw new OperationCanceledException(requested.Token);
                cache.KeepAdjacentTo(pageIndex);
                if (currentImage is not null)
                {
                    if (Math.Abs((long)currentIndex - pageIndex) == 1) cache.Store(currentIndex, currentImage);
                    currentImage.Dispose();
                }
                currentImage = image.Retain();
                currentIndex = pageIndex;
                publishedGeneration = requestGeneration;
            }
            var result = image;
            image = null;
            return result;
        }
        finally
        {
            image?.Dispose();
            if (acquired) reads.Release();
            lock (sync)
            {
                if (requestedCancellation == requested) requestedCancellation = null;
                requested.Dispose();
                if (--pendingReads == 0)
                {
                    foregroundIdle.TrySetResult();
                    if (!closing && prefetchEnabled && publishedGeneration == generation)
                    {
                        prefetchCancellation = new CancellationTokenSource();
                        var cancellation = prefetchCancellation;
                        var index = currentIndex;
                        var version = generation;
                        prefetchTask = Task.Run(() => PrefetchAsync(index, version, cancellation));
                    }
                }
            }
        }
    }

    /// <summary>Joins the current speculative worker without scheduling further work.</summary>
    public Task WaitForPrefetchAsync() { lock (sync) return prefetchTask; }

    private async Task PrefetchAsync(int pageIndex, long version, CancellationTokenSource cancellation)
    {
        try
        {
            foreach (var neighbor in new[] { pageIndex - 1, pageIndex + 1 })
            {
                if ((uint)neighbor >= (uint)Pages.Count) continue;
                await reads.WaitAsync(cancellation.Token).ConfigureAwait(false);
                try
                {
                    lock (sync)
                    {
                        cancellation.Token.ThrowIfCancellationRequested();
                        if (closing || generation != version || pendingReads != 0) return;
                        if (cache.TryGet(neighbor, out var cached))
                        {
                            cached.Dispose();
                            continue;
                        }
                    }
                    try
                    {
                        using var image = await inner.ReadPageAsync(neighbor, cancellation.Token).ConfigureAwait(false);
                        lock (sync)
                            if (!closing && generation == version && pendingReads == 0 && !cancellation.IsCancellationRequested)
                                cache.Store(neighbor, image);
                    }
                    catch (ReaderException) { /* A damaged neighbor must not fail the requested page. */ }
                }
                finally { reads.Release(); }
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        finally
        {
            lock (sync)
            {
                if (prefetchCancellation == cancellation) prefetchCancellation = null;
                cancellation.Dispose();
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (sync)
        {
            if (disposeTask is not null) return new ValueTask(disposeTask);
            closing = true;
            generation++;
            requestedCancellation?.Cancel();
            prefetchCancellation?.Cancel();
            disposeTask = DisposeCoreAsync(prefetchTask, foregroundIdle.Task);
            return new ValueTask(disposeTask);
        }
    }

    private async Task DisposeCoreAsync(Task background, Task foreground)
    {
        try { await Task.WhenAll(background, foreground).ConfigureAwait(false); }
        finally
        {
            lock (sync)
            {
                currentImage?.Dispose();
                currentImage = null;
                cache.Dispose();
            }
            await inner.DisposeAsync().ConfigureAwait(false);
            reads.Dispose();
        }
    }

    private static TaskCompletionSource CompletedSignal()
    {
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        signal.SetResult();
        return signal;
    }
}
