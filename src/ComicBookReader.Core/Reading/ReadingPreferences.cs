using ComicBookReader.Core.ComicFiles;
using ComicBookReader.Core.Preferences;

namespace ComicBookReader.Core.Reading;

public sealed partial class ReaderController
{
    private ReaderPreferences profile = PreferencesResolver.Validate(preferences ?? new ReaderPreferences());
    public ReadingDirection ReadingDirection => profile.ReadingDirection;

    public ReaderPreferences CapturePreferences()
    {
        if (State.Book is { } book && State.Image is not null && State.Mode != ReaderMode.Loading)
        {
            var settings = GetBookSettings(book.BookId);
            profile.Books[book.BookId] = settings with { Position = ReadingPosition };
        }
        return PreferencesResolver.Validate(profile);
    }

    public void ApplyPreferences(ReaderPreferences value)
    {
        var validated = PreferencesResolver.Validate(value);
        bool directionChanged = validated.ReadingDirection != profile.ReadingDirection;
        profile = validated;
        SetScrollSpeed(validated.ScrollSpeed);
        ChangeGeometry(() =>
        {
            WidthRatio = State.Book is { } book ? PreferencesResolver.ResolveWidth(profile, book.BookId) : profile.PreferredPageWidthRatio;
            Classification = profile.PageClassification;
        }, null, null, directionChanged);
    }

    private BookPreferences GetBookSettings(string bookId) => profile.Books.GetValueOrDefault(bookId) ?? new BookPreferences();
    public void ResetBookWidth()
    {
        ChangeGeometry(() =>
        {
            if (State.Book is { } book) profile.Books[book.BookId] = GetBookSettings(book.BookId) with { WidthRatio = null };
            WidthRatio = profile.PreferredPageWidthRatio;
        }, null, null);
    }
    private int RestoredPageIndex(IComicSession candidate)
    {
        if (GetBookSettings(candidate.Book.BookId).Position is not { } position) return 0;
        for (int i = 0; i < candidate.Pages.Count; i++)
            if (candidate.Pages[i].PageKey == position.PageKey) return i;
        return Math.Clamp(position.PageIndex, 0, candidate.Pages.Count - 1);
    }

    private void ResolvePageSettings(IComicSession candidate, int index)
    {
        WidthRatio = PreferencesResolver.ResolveWidth(profile, candidate.Book.BookId);
        Classification = PreferencesResolver.ResolveClassification(profile, candidate.Book.BookId, candidate.Pages[index].PageKey);
        ScrollSpeed = profile.ScrollSpeed;
    }

    private void SaveWidthOverride()
    {
        if (State.Book is { } book) profile.Books[book.BookId] = GetBookSettings(book.BookId) with { WidthRatio = WidthRatio };
    }

    private void SaveClassificationOverride()
    {
        profile = profile with { PageClassification = Classification };
    }
}
