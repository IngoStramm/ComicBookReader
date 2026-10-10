using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Contracts;
using ComicBookReader.Core.Layout;
using ComicBookReader.Core.Preferences;

namespace ComicBookReader.Core.Reading;

public sealed partial class ReaderController(IComicOpener opener, IPageLayoutCalculator? layoutCalculator = null) : IAsyncDisposable
{
    private readonly IPageLayoutCalculator calculator = layoutCalculator ?? new PageLayoutCalculator();
    private readonly object gate = new();
    private IComicSession? session;
    private CancellationTokenSource? activeRequest;
    private int generation;
    private bool disposed;
    private ViewportSize viewport = new(900, 700);
    public ReaderState State { get; private set; } = new();
    public double WidthRatio { get; private set; } = 1;
    public PageClassificationOverride Classification { get; private set; }
    public event Action? Changed;

    public async Task OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        CancellationTokenSource request;
        int current;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            activeRequest?.Cancel();
            activeRequest = request = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            current = ++generation;
            State = State with { Mode = ReaderMode.Loading, Error = null, Generation = current };
        }
        Changed?.Invoke();
        IComicSession? candidate = null;
        PageImage? image = null;
        try
        {
            candidate = await opener.OpenAsync(path, request.Token);
            request.Token.ThrowIfCancellationRequested();
            image = await candidate.ReadPageAsync(0, request.Token);
            IComicSession? previous;
            PageImage? previousImage;
            lock (gate)
            {
                if (current != generation || disposed) return;
                previous = session;
                previousImage = State.Image;
                session = candidate;
                State = new ReaderState
                {
                    Mode = ReaderMode.Reading, Book = candidate.Book, Pages = candidate.Pages,
                    Image = image, Layout = ReadingLayout(image), Generation = current
                };
                candidate = null;
                image = null;
            }
            Changed?.Invoke();
            previousImage?.Dispose();
            if (previous is not null) await previous.DisposeAsync();
        }
        catch (OperationCanceledException)
        {
            RestoreAfterFailedOpen(current, null);
        }
        catch (Exception ex) when (ex is ReaderException or IOException or UnauthorizedAccessException)
        {
            RestoreAfterFailedOpen(current, ex as ReaderException ??
                new ReaderException(ReaderErrorCode.FileUnavailable, "Não foi possível abrir esta HQ.", ex));
        }
        finally
        {
            image?.Dispose();
            if (candidate is not null) await candidate.DisposeAsync();
            lock (gate) { if (ReferenceEquals(activeRequest, request)) activeRequest = null; }
            request.Dispose();
        }
    }

    private PageLayout ReadingLayout(PageImage image) => calculator.CalculateReading(image.Size, viewport,
        calculator.Classify(image.Size, Classification), WidthRatio);

    private void RestoreAfterFailedOpen(int current, ReaderException? error)
    {
        lock (gate)
        {
            if (current != generation || disposed) return;
            State = State with { Mode = State.Image is null ? ReaderMode.Empty : ReaderMode.Reading, Error = error };
        }
        Changed?.Invoke();
    }

    public void SetViewport(ViewportSize value)
    {
        // Validate even when no page is open.
        calculator.CalculateReading(new PixelSize(1, 1), value, PageKind.Single, WidthRatio);
        viewport = value;
        if (State.Image is null) return;
        var layout = ReadingLayout(State.Image);
        State = State with { Layout = layout, OffsetXDip = Math.Clamp(State.OffsetXDip, 0, layout.MaxOffsetXDip),
            OffsetYDip = Math.Clamp(State.OffsetYDip, 0, layout.MaxOffsetYDip) };
        Changed?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        IComicSession? previous;
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            ++generation;
            activeRequest?.Cancel();
            State.Image?.Dispose();
            previous = session;
            session = null;
            State = new ReaderState();
        }
        if (previous is not null) await previous.DisposeAsync();
    }
}
