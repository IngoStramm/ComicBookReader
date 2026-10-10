using System.Collections.Concurrent;
using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Contracts;
using ComicBookReader.Infrastructure.Archives;
using Xunit;

namespace ComicBookReader.Integration.Tests.Reading;

public sealed class ReadPriorityTests
{
    [Fact]
    public async Task PrefetchesOnlyNeighborsAndServesRetainedCachedPixels()
    {
        var source = new InstrumentedSession(5);
        await using var session = new CachedComicSession(source);
        using var current = await session.ReadPageAsync(2, CancellationToken.None);
        await session.WaitForPrefetchAsync();
        Assert.Equal(new[] { 2, 1, 3 }, source.Reads.ToArray());
        Assert.Equal(8, session.CacheBytes);
        using var previous = await session.ReadPageAsync(1, CancellationToken.None);
        Assert.Equal(1, previous.Pixels.Span[0]);
        await session.WaitForPrefetchAsync();
        Assert.Equal(1, source.Reads.Count(index => index == 1));
        Assert.Equal(1, source.MaximumConcurrentReads);
    }

    [Fact]
    public async Task CurrentImageIsSeparateFromAdditionalBudgetAndPrefetchCanBeDisabled()
    {
        var source = new InstrumentedSession(5);
        await using var session = new CachedComicSession(source, prefetchEnabled: false, cacheBudgetBytes: 0);
        using var current = await session.ReadPageAsync(2, CancellationToken.None);
        using var repeated = await session.ReadPageAsync(2, CancellationToken.None);
        await session.WaitForPrefetchAsync();
        Assert.Equal(new[] { 2 }, source.Reads.ToArray());
        Assert.Equal(0, session.CacheBytes);
        Assert.Equal(2, repeated.Pixels.Span[0]);
    }

    [Fact]
    public async Task RequestedReadCancelsPrefetchAndObsoletePixelsNeverEnterCache()
    {
        var entered = Signal();
        var canceled = Signal();
        var release = Signal();
        var source = new InstrumentedSession(6)
        {
            BeforeRead = async (index, token) =>
            {
                if (index != 1) return;
                using var registration = token.Register(() => canceled.TrySetResult());
                entered.TrySetResult();
                // Model a decoder that can only acknowledge cancellation after decoding.
                await release.Task;
            }
        };
        await using var session = new CachedComicSession(source, cacheBudgetBytes: 8);
        using var first = await session.ReadPageAsync(2, CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var requested = session.ReadPageAsync(4, CancellationToken.None).AsTask();
        await canceled.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        release.TrySetResult();
        using var replacement = await requested.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await session.WaitForPrefetchAsync();
        Assert.Equal(new[] { 2, 1, 4, 3, 5 }, source.Reads.ToArray());
        Assert.Equal(4, replacement.Pixels.Span[0]);
        Assert.Equal(8, session.CacheBytes);
        Assert.Equal(1, source.MaximumConcurrentReads);
        Assert.Throws<ObjectDisposedException>(() => source.Images[1][0].Pixels);
    }

    [Fact]
    public async Task NewRequestedReadCancelsOlderRequestBeforeItCanPublish()
    {
        var entered = Signal();
        var release = Signal();
        var source = new InstrumentedSession(5)
        {
            BeforeRead = async (index, _) =>
            {
                if (index != 1) return;
                entered.TrySetResult();
                await release.Task;
            }
        };
        await using var session = new CachedComicSession(source, prefetchEnabled: false);
        var old = session.ReadPageAsync(1, CancellationToken.None).AsTask();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var latest = session.ReadPageAsync(3, CancellationToken.None).AsTask();
        release.TrySetResult();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => old);
        using var replacement = await latest;
        Assert.Equal(3, replacement.Pixels.Span[0]);
        Assert.Throws<ObjectDisposedException>(() => source.Images[1][0].Pixels);
    }

    [Fact]
    public async Task CloseCancelsAndJoinsPrefetchDisposesSourceAndPreservesCallerLease()
    {
        var entered = Signal();
        var canceled = Signal();
        var release = Signal();
        var source = new InstrumentedSession(3)
        {
            BeforeRead = async (index, token) =>
            {
                if (index != 0) return;
                using var registration = token.Register(() => canceled.TrySetResult());
                entered.TrySetResult();
                await release.Task;
            }
        };
        var session = new CachedComicSession(source);
        using var current = await session.ReadPageAsync(1, CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var close = session.DisposeAsync().AsTask();
        await canceled.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.False(close.IsCompleted);
        release.TrySetResult();
        await close;
        await session.DisposeAsync();
        Assert.Equal(1, source.DisposeCount);
        Assert.Equal(0, session.CacheBytes);
        Assert.Equal(1, current.Pixels.Span[0]);
        Assert.Throws<ObjectDisposedException>(() => source.Images[0][0].Pixels);
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await session.ReadPageAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task NeighborDecodeFailureDoesNotFailCurrentOrFollowingRequestedPage()
    {
        var source = new InstrumentedSession(3)
        {
            BeforeRead = (index, _) => index == 0
                ? Task.FromException(new ReaderException(ReaderErrorCode.PageDecodeFailed, "Bad neighbor"))
                : Task.CompletedTask
        };
        await using var session = new CachedComicSession(source);
        using var current = await session.ReadPageAsync(1, CancellationToken.None);
        await session.WaitForPrefetchAsync();
        using var next = await session.ReadPageAsync(2, CancellationToken.None);
        Assert.Equal(2, next.Pixels.Span[0]);
    }

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class InstrumentedSession(int pageCount) : IComicSession
    {
        private int concurrent;
        public BookInfo Book { get; } = BookInfo.Create("test.cbz", 0, DateTime.UnixEpoch);
        public IReadOnlyList<PageInfo> Pages { get; } = Enumerable.Range(0, pageCount)
            .Select(index => new PageInfo(index, index.ToString(), $"{index}.png")).ToArray();
        public ConcurrentQueue<int> Reads { get; } = new();
        public ConcurrentDictionary<int, List<PageImage>> Images { get; } = new();
        public Func<int, CancellationToken, Task>? BeforeRead { get; init; }
        public int MaximumConcurrentReads { get; private set; }
        public int DisposeCount { get; private set; }

        public async ValueTask<PageImage> ReadPageAsync(int pageIndex, CancellationToken cancellationToken)
        {
            Reads.Enqueue(pageIndex);
            MaximumConcurrentReads = Math.Max(MaximumConcurrentReads, Interlocked.Increment(ref concurrent));
            try
            {
                if (BeforeRead is not null) await BeforeRead(pageIndex, cancellationToken);
                var image = new PageImage(new PixelSize(1, 1), 4, [(byte)pageIndex, 0, 0, 255]);
                Images.GetOrAdd(pageIndex, _ => []).Add(image);
                return image;
            }
            finally { Interlocked.Decrement(ref concurrent); }
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }
}
