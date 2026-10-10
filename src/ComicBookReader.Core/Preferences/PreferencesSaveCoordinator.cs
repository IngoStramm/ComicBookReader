namespace ComicBookReader.Core.Preferences;

/// <summary>Owns one persistence worker. Flush before disposal when closing normally.</summary>
public sealed class PreferencesSaveCoordinator : IAsyncDisposable
{
    private static readonly TimeSpan DebounceDuration = TimeSpan.FromMilliseconds(300);
    private readonly object sync = new();
    private readonly IReadingPreferencesStore store;
    private readonly Func<TimeSpan, CancellationToken, Task> delay;
    private readonly SemaphoreSlim wake = new(0, 1);
    private readonly CancellationTokenSource lifetime = new();
    private readonly Task worker;
    private ReaderPreferences latestSnapshot;
    private long latestVersion;
    private long savedVersion;
    private CancellationTokenSource? debounceCancellation;
    private TaskCompletionSource? flushCompletion;
    private Exception? lastError;
    private bool disposed;

    public PreferencesSaveCoordinator(IReadingPreferencesStore store, ReaderPreferences? initial = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        this.store = store;
        this.delay = delay ?? Task.Delay;
        latestSnapshot = PreferencesResolver.Validate(initial ?? new ReaderPreferences());
        worker = Task.Run(RunAsync);
    }

    public ReaderPreferences CurrentSnapshot
    {
        get { lock (sync) return PreferencesResolver.Validate(latestSnapshot); }
    }

    public Exception? LastError
    {
        get { lock (sync) return lastError; }
    }

    /// <summary>Raised on the worker thread when a failure appears or a retry succeeds.</summary>
    public event EventHandler? SaveStatusChanged;

    public void Update(ReaderPreferences preferences)
    {
        var snapshot = PreferencesResolver.Validate(preferences);
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            latestSnapshot = snapshot;
            latestVersion++;
            debounceCancellation?.Cancel();
            WakeLocked();
        }
    }

    /// <summary>Waits for the latest snapshot. Storage failures are exposed through LastError.</summary>
    /// <remarks>Cancellation stops this caller's wait; the worker retains ownership of writes.</remarks>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Task completion;
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (latestVersion == savedVersion) return;
            flushCompletion ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
            completion = flushCompletion.Task;
            debounceCancellation?.Cancel();
            WakeLocked();
        }
        await completion.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Cancels and awaits the worker; does not implicitly save dirty state.</summary>
    public ValueTask DisposeAsync()
    {
        lock (sync)
        {
            if (!disposed)
            {
                disposed = true;
                lifetime.Cancel();
            }
        }
        return new ValueTask(worker);
    }

    private void WakeLocked()
    {
        if (wake.CurrentCount == 0) wake.Release();
    }

    private async Task RunAsync()
    {
        try
        {
            while (true)
            {
                await wake.WaitAsync(lifetime.Token).ConfigureAwait(false);
                await SavePendingAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        finally
        {
            lock (sync)
            {
                flushCompletion?.TrySetCanceled(lifetime.Token);
                flushCompletion = null;
            }
            lifetime.Dispose();
            wake.Dispose();
        }
    }

    private async Task SavePendingAsync()
    {
        while (true)
        {
            CancellationTokenSource? pendingDelay = null;
            long delayedVersion;
            lock (sync)
            {
                if (disposed) return;
                if (latestVersion == savedVersion)
                {
                    flushCompletion?.TrySetResult();
                    flushCompletion = null;
                    return;
                }
                delayedVersion = latestVersion;
                if (flushCompletion is null)
                {
                    pendingDelay = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                    debounceCancellation = pendingDelay;
                }
            }

            if (pendingDelay is not null)
            {
                try
                {
                    await delay(DebounceDuration, pendingDelay.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (pendingDelay.IsCancellationRequested)
                {
                    lifetime.Token.ThrowIfCancellationRequested();
                    continue;
                }
                catch (Exception error)
                {
                    ReportFailure(error);
                    return;
                }
                finally
                {
                    lock (sync)
                    {
                        debounceCancellation = null;
                        pendingDelay.Dispose();
                    }
                }
            }

            ReaderPreferences snapshot;
            long version;
            lock (sync)
            {
                if (disposed) return;
                if (pendingDelay is not null && latestVersion != delayedVersion && flushCompletion is null)
                    continue;
                snapshot = PreferencesResolver.Validate(latestSnapshot);
                version = latestVersion;
            }
            try
            {
                await store.SaveAsync(snapshot, lifetime.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { throw; }
            catch (Exception error)
            {
                ReportFailure(error);
                return;
            }

            bool recovered;
            lock (sync)
            {
                savedVersion = version;
                recovered = lastError is not null;
                lastError = null;
            }
            if (recovered) NotifyStatusChanged();
        }
    }

    private void ReportFailure(Exception error)
    {
        lock (sync)
        {
            lastError = error;
            flushCompletion?.TrySetResult();
            flushCompletion = null;
        }
        NotifyStatusChanged();
    }

    private void NotifyStatusChanged()
    {
        var handlers = SaveStatusChanged;
        if (handlers is null) return;
        foreach (EventHandler handler in handlers.GetInvocationList())
        {
            // An observer must not stop the owned persistence worker.
            try { handler(this, EventArgs.Empty); }
            catch { }
        }
    }
}
