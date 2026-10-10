using System.Text.Json;
using ComicBookReader.Core.Preferences;

namespace ComicBookReader.Infrastructure.Storage;

internal sealed class PreferencesDto
{
    // Profiles are limited to 8 MiB on read and write; recovery copies stream without this allocation.
    internal const int MaximumFileBytes = 8 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowDuplicateProperties = false
    };

    public int SchemaVersion { get; set; } = 1;
    public double PreferredPageWidthRatio { get; set; } = 1;
    public double ScrollSpeed { get; set; } = .6;
    public bool LastFullscreen { get; set; } = true;
    public Dictionary<string, string> KeyBindings { get; set; } = new ReaderPreferences().KeyBindings;
    public Dictionary<string, BookPreferencesDto> Books { get; set; } = new(StringComparer.Ordinal);

    internal static byte[] Encode(ReaderPreferences preferences)
    {
        var dto = new PreferencesDto
        {
            SchemaVersion = preferences.SchemaVersion,
            PreferredPageWidthRatio = preferences.PreferredPageWidthRatio,
            ScrollSpeed = preferences.ScrollSpeed,
            LastFullscreen = preferences.LastFullscreen,
            KeyBindings = preferences.KeyBindings,
            Books = preferences.Books.ToDictionary(pair => pair.Key, pair => BookPreferencesDto.FromSnapshot(pair.Value),
                StringComparer.Ordinal)
        };
        return JsonSerializer.SerializeToUtf8Bytes(dto, JsonOptions);
    }

    internal static async Task<PreferencesFile> ReadAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (file.Length > MaximumFileBytes) return new(PreferencesFileStatus.Invalid);
            var bytes = new byte[(int)file.Length];
            await file.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
            return Decode(bytes);
        }
        catch (FileNotFoundException) { return new(PreferencesFileStatus.Missing); }
        catch (DirectoryNotFoundException) { return new(PreferencesFileStatus.Missing); }
    }

    private static PreferencesFile Decode(byte[] bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { AllowDuplicateProperties = false });
            if (document.RootElement.ValueKind != JsonValueKind.Object) return new(PreferencesFileStatus.Invalid);
            bool hasSchema = false;
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!property.Name.Equals(nameof(SchemaVersion), StringComparison.OrdinalIgnoreCase)) continue;
                if (hasSchema || property.Value.ValueKind != JsonValueKind.Number)
                    return new(PreferencesFileStatus.Invalid);
                hasSchema = true;
                // Inspect the schema before deserializing fields that a future version may redefine.
                if (property.Value.TryGetInt64(out long version) && version > 1)
                    return new(PreferencesFileStatus.Future);
            }
            var dto = JsonSerializer.Deserialize<PreferencesDto>(bytes, JsonOptions);
            if (dto is null || dto.Books is null || dto.KeyBindings is null) return new(PreferencesFileStatus.Invalid);
            var books = new Dictionary<string, BookPreferences>(StringComparer.Ordinal);
            foreach (var (bookId, book) in dto.Books)
            {
                if (book is null || book.Classifications is null) return new(PreferencesFileStatus.Invalid);
                books.Add(bookId, book.ToSnapshot());
            }
            return new(PreferencesFileStatus.Valid, PreferencesResolver.Validate(new ReaderPreferences
            {
                SchemaVersion = dto.SchemaVersion,
                PreferredPageWidthRatio = dto.PreferredPageWidthRatio,
                ScrollSpeed = dto.ScrollSpeed,
                LastFullscreen = dto.LastFullscreen,
                KeyBindings = dto.KeyBindings,
                Books = books
            }));
        }
        catch (JsonException) { return new(PreferencesFileStatus.Invalid); }
        catch (ArgumentException) { return new(PreferencesFileStatus.Invalid); }
    }
}

internal sealed class BookPreferencesDto
{
    public double? WidthRatio { get; set; }
    public Dictionary<string, PageClassificationOverride> Classifications { get; set; } = new(StringComparer.Ordinal);
    public ReadingPositionDto? Position { get; set; }
    internal BookPreferences ToSnapshot() => new()
    {
        WidthRatio = WidthRatio,
        Classifications = Classifications,
        Position = Position is null ? null : new LastReadingPosition(Position.PageKey, Position.PageIndex, Position.RelativeX, Position.RelativeY)
    };
    internal static BookPreferencesDto FromSnapshot(BookPreferences book) => new()
    {
        WidthRatio = book.WidthRatio,
        Classifications = book.Classifications,
        Position = book.Position is null ? null : new ReadingPositionDto
        {
            PageKey = book.Position.PageKey,
            PageIndex = book.Position.PageIndex,
            RelativeX = book.Position.RelativeX,
            RelativeY = book.Position.RelativeY
        }
    };
}

internal sealed class ReadingPositionDto
{
    public string PageKey { get; set; } = string.Empty;
    public int PageIndex { get; set; }
    public double RelativeX { get; set; }
    public double RelativeY { get; set; }
}

internal enum PreferencesFileStatus { Missing, Valid, Invalid, Future }
internal readonly record struct PreferencesFile(PreferencesFileStatus Status, ReaderPreferences? Snapshot = null);
