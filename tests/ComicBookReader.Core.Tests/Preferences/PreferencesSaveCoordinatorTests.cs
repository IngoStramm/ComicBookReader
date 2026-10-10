using System.Threading.Channels;
using ComicBookReader.Core.Contracts;
using ComicBookReader.Core.Preferences;
using Xunit;

namespace ComicBookReader.Core.Tests.Preferences;

public sealed class PreferencesSaveCoordinatorTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task UpdateAtDelayCompletionStartsAnotherDebounceBeforeWriting()
    {
        var delay = new ControlledDelay();
        var store = new ControlledStore();
        PreferencesSaveCoordinator? coordinator = null;
        int delayCalls = 0;
        Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken)
        {
            if (++delayCalls == 1)
            {
                coordinator!.Update(new ReaderPreferences { ScrollSpeed = .9 });
                return Task.CompletedTask;
            }
            return delay.WaitAsync(duration, cancellationToken);
        }
        await using var ownedCoordinator = new PreferencesSaveCoordinator(store, delay: WaitAsync);
        coordinator = ownedCoordinator;
        coordinator.Update(new ReaderPreferences { ScrollSpeed = .7 });

        var latestDelay = await delay.NextAsync();
        Assert.Empty(store.Writes);
        latestDelay.Complete();
        var write = await store.NextAsync();
        Assert.Equal(.9, write.Snapshot.ScrollSpeed);
        write.Complete();
        await coordinator.FlushAsync(Token);
        Assert.Single(store.Writes);
    }

    [Fact]
    public async Task RapidUpdatesRestartThreeHundredMillisecondDelayAndSaveDetachedLatestSnapshot()
    {
        var delay = new ControlledDelay();
        var store = new ControlledStore();
        await using var coordinator = new PreferencesSaveCoordinator(store, delay: delay.WaitAsync);
        coordinator.Update(new ReaderPreferences { ScrollSpeed = .7 });
        var firstDelay = await delay.NextAsync();
        var latest = new ReaderPreferences
        {
            ScrollSpeed = .9,
            Books = new() { ["book"] = new BookPreferences { WidthRatio = .8 } }
        };
        coordinator.Update(latest);
        var latestDelay = await delay.NextAsync();
        latest.Books.Clear();
        coordinator.CurrentSnapshot.Books.Clear();

        Assert.Equal(TimeSpan.FromMilliseconds(300), latestDelay.Duration);
        Assert.True(firstDelay.Token.IsCancellationRequested);
        Assert.Empty(store.Writes);
        latestDelay.Complete();
        var write = await store.NextAsync();
        Assert.Equal(.9, write.Snapshot.ScrollSpeed);
        Assert.Equal(.8, write.Snapshot.Books["book"].WidthRatio);
        write.Complete();
        await coordinator.FlushAsync(Token);
        Assert.Single(store.Writes);
        Assert.Equal(.8, coordinator.CurrentSnapshot.Books["book"].WidthRatio);
    }

    [Fact]
    public async Task FlushBypassesPendingDelayAndCleanFlushDoesNotWriteAgain()
    {
        var delay = new ControlledDelay();
        var store = new ControlledStore();
        await using var coordinator = new PreferencesSaveCoordinator(store, delay: delay.WaitAsync);
        coordinator.Update(new ReaderPreferences { ScrollSpeed = .8 });
        var pendingDelay = await delay.NextAsync();

        var flush = coordinator.FlushAsync(Token);
        var write = await store.NextAsync();
        Assert.True(pendingDelay.Token.IsCancellationRequested);
        Assert.False(flush.IsCompleted);
        write.Complete();
        await flush;
        await coordinator.FlushAsync(Token);
        Assert.Single(store.Writes);
        Assert.Equal(.8, store.Persisted!.ScrollSpeed);
    }

    [Fact]
    public async Task OverlappingUpdatesAndFlushesSerializeWritesAndPersistNewestSnapshot()
    {
        var delay = new ControlledDelay();
        var store = new ControlledStore();
        await using var coordinator = new PreferencesSaveCoordinator(store, delay: delay.WaitAsync);
        coordinator.Update(new ReaderPreferences { ScrollSpeed = .7 });
        (await delay.NextAsync()).Complete();
        var firstWrite = await store.NextAsync();
        coordinator.Update(new ReaderPreferences { ScrollSpeed = .8 });
        var firstFlush = coordinator.FlushAsync(Token);
        coordinator.Update(new ReaderPreferences { ScrollSpeed = .9 });
        var secondFlush = coordinator.FlushAsync(Token);
        Assert.Single(store.Writes);
        Assert.Equal(1, store.ActiveWrites);

        firstWrite.Complete();
        var latestWrite = await store.NextAsync();
        Assert.Equal(.9, latestWrite.Snapshot.ScrollSpeed);
        Assert.False(firstFlush.IsCompleted);
        Assert.False(secondFlush.IsCompleted);
        latestWrite.Complete();
        await Task.WhenAll(firstFlush, secondFlush);
        Assert.Equal(1, store.MaximumActiveWrites);
        Assert.Equal(new[] { .7, .9 }, store.Writes.Select(write => write.Snapshot.ScrollSpeed));
        Assert.Equal(.9, store.Persisted!.ScrollSpeed);
    }

    [Fact]
    public async Task CancelingFlushWaitDoesNotCancelOwnedWriteOrLoseLatestSnapshot()
    {
        var delay = new ControlledDelay();
        var store = new ControlledStore();
        await using var coordinator = new PreferencesSaveCoordinator(store, delay: delay.WaitAsync);
        coordinator.Update(new ReaderPreferences { ScrollSpeed = .8 });
        await delay.NextAsync();
        using var canceledWait = new CancellationTokenSource();
        var abandonedFlush = coordinator.FlushAsync(canceledWait.Token);
        var write = await store.NextAsync();

        canceledWait.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => abandonedFlush);
        Assert.False(write.Token.IsCancellationRequested);
        Assert.Equal(.8, coordinator.CurrentSnapshot.ScrollSpeed);
        var completingFlush = coordinator.FlushAsync(Token);
        write.Complete();
        await completingFlush;
        Assert.Single(store.Writes);
        Assert.Equal(.8, store.Persisted!.ScrollSpeed);
        Assert.Null(coordinator.LastError);
    }

    [Fact]
    public async Task AlreadyCanceledFlushLeavesDebounceAndSnapshotAvailable()
    {
        var delay = new ControlledDelay();
        var store = new ControlledStore();
        await using var coordinator = new PreferencesSaveCoordinator(store, delay: delay.WaitAsync);
        coordinator.Update(new ReaderPreferences { ScrollSpeed = .8 });
        var pendingDelay = await delay.NextAsync();
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => coordinator.FlushAsync(canceled.Token));
        Assert.False(pendingDelay.Token.IsCancellationRequested);
        Assert.Empty(store.Writes);
        pendingDelay.Complete();
        var write = await store.NextAsync();
        write.Complete();
        await coordinator.FlushAsync(Token);
        Assert.Equal(.8, store.Persisted!.ScrollSpeed);
    }

    [Fact]
    public async Task StorageFailureIsReportedWithoutLosingMemoryAndFlushCanRetry()
    {
        var delay = new ControlledDelay();
        var store = new ControlledStore();
        await using var coordinator = new PreferencesSaveCoordinator(store, delay: delay.WaitAsync);
        var failed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recovered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        coordinator.SaveStatusChanged += (_, _) =>
        {
            if (coordinator.LastError is not null) failed.TrySetResult();
            else recovered.TrySetResult();
        };
        coordinator.Update(new ReaderPreferences { ScrollSpeed = .8 });
        (await delay.NextAsync()).Complete();
        var write = await store.NextAsync();
        var error = new ReaderException(ReaderErrorCode.StorageUnavailable, "Storage unavailable.");

        write.Fail(error);
        await failed.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.Same(error, coordinator.LastError);
        Assert.Equal(.8, coordinator.CurrentSnapshot.ScrollSpeed);
        Assert.Null(store.Persisted);
        var retry = coordinator.FlushAsync(Token);
        var retriedWrite = await store.NextAsync();
        Assert.Equal(.8, retriedWrite.Snapshot.ScrollSpeed);
        retriedWrite.Complete();
        await retry;
        await recovered.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.Null(coordinator.LastError);
        Assert.Equal(.8, store.Persisted!.ScrollSpeed);
    }

    [Fact]
    public async Task DisposeCancelsPendingAutosaveAndRejectsFurtherRequests()
    {
        var delay = new ControlledDelay();
        var store = new ControlledStore();
        var coordinator = new PreferencesSaveCoordinator(store, delay: delay.WaitAsync);
        coordinator.Update(new ReaderPreferences { ScrollSpeed = .8 });
        var pendingDelay = await delay.NextAsync();

        await coordinator.DisposeAsync();
        await coordinator.DisposeAsync();
        Assert.True(pendingDelay.Token.IsCancellationRequested);
        Assert.Empty(store.Writes);
        Assert.Throws<ObjectDisposedException>(() => { coordinator.Update(new ReaderPreferences()); });
        await Assert.ThrowsAsync<ObjectDisposedException>(() => coordinator.FlushAsync(Token));
        Assert.Equal(.8, coordinator.CurrentSnapshot.ScrollSpeed);
    }

    [Fact]
    public async Task DisposeCancelsAndAwaitsInFlightWriteAndPendingFlush()
    {
        var delay = new ControlledDelay();
        var store = new ControlledStore();
        var coordinator = new PreferencesSaveCoordinator(store, delay: delay.WaitAsync);
        coordinator.Update(new ReaderPreferences { ScrollSpeed = .8 });
        (await delay.NextAsync()).Complete();
        var write = await store.NextAsync();
        var flush = coordinator.FlushAsync(Token);

        await coordinator.DisposeAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => flush);
        Assert.True(write.Token.IsCancellationRequested);
        Assert.Equal(0, store.ActiveWrites);
        Assert.Null(store.Persisted);
        Assert.Null(coordinator.LastError);
        Assert.Equal(.8, coordinator.CurrentSnapshot.ScrollSpeed);
    }

    [Fact]
    public async Task InvalidUpdateCannotReplaceValidatedInitialState()
    {
        var delay = new ControlledDelay();
        var store = new ControlledStore();
        await using var coordinator = new PreferencesSaveCoordinator(store,
            new ReaderPreferences { ScrollSpeed = .7 }, delay.WaitAsync);

        Assert.Throws<ArgumentException>(() => { coordinator.Update(new ReaderPreferences { ScrollSpeed = double.NaN }); });
        await coordinator.FlushAsync(Token);
        Assert.Empty(store.Writes);
        Assert.Equal(.7, coordinator.CurrentSnapshot.ScrollSpeed);
    }

    private sealed class ControlledDelay
    {
        private readonly Channel<DelayRequest> requests = Channel.CreateUnbounded<DelayRequest>();

        public Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken)
        {
            var request = new DelayRequest(duration, cancellationToken);
            requests.Writer.TryWrite(request);
            return request.Completion.Task.WaitAsync(cancellationToken);
        }

        public Task<DelayRequest> NextAsync() => requests.Reader.ReadAsync(Token).AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5), Token);
    }

    private sealed class DelayRequest(TimeSpan duration, CancellationToken token)
    {
        public TimeSpan Duration { get; } = duration;
        public CancellationToken Token { get; } = token;
        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Complete() => Completion.TrySetResult();
    }

    private sealed class ControlledStore : IReadingPreferencesStore
    {
        private readonly object sync = new();
        private readonly Channel<SaveRequest> requests = Channel.CreateUnbounded<SaveRequest>();
        private readonly List<SaveRequest> writes = [];
        private int activeWrites;
        public int ActiveWrites { get { lock (sync) return activeWrites; } }
        public int MaximumActiveWrites { get; private set; }
        public ReaderPreferences? Persisted { get; private set; }
        public SaveRequest[] Writes { get { lock (sync) return writes.ToArray(); } }

        public Task<ReaderPreferences> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new ReaderPreferences());

        public async Task SaveAsync(ReaderPreferences preferences, CancellationToken cancellationToken)
        {
            var request = new SaveRequest(preferences, cancellationToken);
            lock (sync)
            {
                writes.Add(request);
                activeWrites++;
                MaximumActiveWrites = Math.Max(MaximumActiveWrites, activeWrites);
            }
            requests.Writer.TryWrite(request);
            try
            {
                await request.Completion.Task.WaitAsync(cancellationToken);
                Persisted = preferences;
            }
            finally { lock (sync) activeWrites--; }
        }

        public Task<SaveRequest> NextAsync() => requests.Reader.ReadAsync(Token).AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5), Token);
    }

    private sealed class SaveRequest(ReaderPreferences snapshot, CancellationToken token)
    {
        public ReaderPreferences Snapshot { get; } = snapshot;
        public CancellationToken Token { get; } = token;
        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Complete() => Completion.TrySetResult();
        public void Fail(Exception error) => Completion.TrySetException(error);
    }
}
