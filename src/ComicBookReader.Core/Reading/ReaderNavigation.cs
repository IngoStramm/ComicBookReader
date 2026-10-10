using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Contracts;

namespace ComicBookReader.Core.Reading;

public sealed partial class ReaderController
{
    public async Task NavigateAsync(int delta, CancellationToken cancellationToken = default)
    {
        Deactivate();
        CapturePreferences();
        IComicSession currentSession;
        CancellationTokenSource request;
        int target, current;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (session is null || delta == 0) return;
            target = (int)Math.Clamp((long)State.TargetIndex + delta, 0, session.Pages.Count - 1);
            if (target == State.TargetIndex && State.Mode != ReaderMode.PageError) return;
            currentSession = session;
            activeRequest?.Cancel();
            activeRequest = request = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            current = ++generation;
            State = State with { Mode = ReaderMode.Loading, TargetIndex = target, Error = null, Generation = current };
        }
        Changed?.Invoke();
        PageImage? image = null;
        try
        {
            image = await currentSession.ReadPageAsync(target, request.Token);
            request.Token.ThrowIfCancellationRequested();
            PageImage? previous;
            lock (gate)
            {
                if (current != generation || disposed) return;
                previous = State.Image;
                overview = null;
                ResolvePageSettings(currentSession, target);
                State = State with { Mode = ReaderMode.Reading, Image = image, Layout = ReadingLayout(image),
                    PresentedIndex = target, OffsetXDip = 0, OffsetYDip = 0 };
                image = null;
            }
            Changed?.Invoke();
            previous?.Dispose();
        }
        catch (OperationCanceledException)
        {
            lock (gate)
            {
                if (current != generation || disposed) return;
                State = State with { Mode = overview is null ? ReaderMode.Reading : ReaderMode.Overview,
                    TargetIndex = State.PresentedIndex };
            }
            Changed?.Invoke();
        }
        catch (ReaderException ex)
        {
            lock (gate)
            {
                if (current != generation || disposed) return;
                State = State with { Mode = ReaderMode.PageError, Error = ex };
            }
            Changed?.Invoke();
        }
        finally
        {
            image?.Dispose();
            lock (gate) { if (ReferenceEquals(activeRequest, request)) activeRequest = null; }
            request.Dispose();
        }
    }
}
