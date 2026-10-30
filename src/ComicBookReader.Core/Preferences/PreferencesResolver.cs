namespace ComicBookReader.Core.Preferences;

public static class PreferencesResolver
{
    private static readonly Dictionary<string, string> KnownKeys = CreateKnownKeys();

    public static double ResolveWidth(ReaderPreferences preferences, string bookId)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ValidateIdentity(bookId, nameof(bookId));
        double width = preferences.Books.TryGetValue(bookId, out var book)
            ? book.WidthRatio ?? preferences.PreferredPageWidthRatio
            : preferences.PreferredPageWidthRatio;
        ValidateRange(width, .25, 2, nameof(ReaderPreferences.PreferredPageWidthRatio));
        return width;
    }

    public static PageClassificationOverride ResolveClassification(
        ReaderPreferences preferences, string bookId, string pageKey)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ValidateIdentity(bookId, nameof(bookId));
        ValidateIdentity(pageKey, nameof(pageKey));
        var classification = preferences.PageClassification;
        ValidateClassification(classification);
        return classification;
    }

    public static ReaderPreferences Validate(ReaderPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        if (preferences.SchemaVersion != 1)
        {
            string reason = preferences.SchemaVersion > 1 ? "future" : "invalid";
            throw new ArgumentException($"SchemaVersion is {reason}: {preferences.SchemaVersion}.",
                nameof(ReaderPreferences.SchemaVersion));
        }
        ValidateRange(preferences.PreferredPageWidthRatio, .25, 2,
            nameof(ReaderPreferences.PreferredPageWidthRatio));
        ValidateRange(preferences.ScrollSpeed, .1, 2, nameof(ReaderPreferences.ScrollSpeed));
        ValidateRange(preferences.SpaceScrollPercent, 10, 100, nameof(ReaderPreferences.SpaceScrollPercent));
        if (!Enum.IsDefined(preferences.ReadingDirection))
            throw new ArgumentException("Unknown reading direction.", nameof(ReaderPreferences.ReadingDirection));
        ValidateClassification(preferences.PageClassification);
        if (preferences.Language is not ("pt-BR" or "en"))
            throw new ArgumentException("Unsupported language.", nameof(ReaderPreferences.Language));
        if (preferences.Books is null)
            throw new ArgumentException("Books cannot be null.", nameof(preferences));

        var books = new Dictionary<string, BookPreferences>(StringComparer.Ordinal);
        foreach (var (bookId, book) in preferences.Books)
        {
            ValidateIdentity(bookId, nameof(bookId));
            if (book is null || book.Classifications is null)
                throw new ArgumentException("Book preferences and classifications cannot be null.", nameof(preferences));
            if (book.WidthRatio is double width)
                ValidateRange(width, .25, 2, nameof(BookPreferences.WidthRatio));
            if (book.Position is not null)
                ValidatePosition(book.Position);

            var classifications = new Dictionary<string, PageClassificationOverride>(StringComparer.Ordinal);
            foreach (var (pageKey, classification) in book.Classifications)
            {
                ValidateIdentity(pageKey, nameof(pageKey));
                ValidateClassification(classification);
                classifications.Add(pageKey, classification);
            }
            books.Add(bookId, book with { Classifications = classifications });
        }
        return preferences with { KeyBindings = ValidateBindings(preferences.KeyBindings), Books = books };
    }

    public static LastReadingPosition CapturePosition(
        string pageKey, int pageIndex, double offsetX, double offsetY, double maxOffsetX, double maxOffsetY)
    {
        ValidateIdentity(pageKey, nameof(pageKey));
        if (pageIndex < 0)
            throw new ArgumentException("PageIndex must be nonnegative.", nameof(pageIndex));
        ValidateLimit(maxOffsetX, nameof(maxOffsetX));
        ValidateLimit(maxOffsetY, nameof(maxOffsetY));
        if (!double.IsFinite(offsetX) || !double.IsFinite(offsetY))
            throw new ArgumentException("Reading offsets must be finite.");
        double relativeX = maxOffsetX == 0 ? 0 : Math.Clamp(offsetX / maxOffsetX, 0, 1);
        double relativeY = maxOffsetY == 0 ? 0 : Math.Clamp(offsetY / maxOffsetY, 0, 1);
        return new LastReadingPosition(pageKey, pageIndex, relativeX, relativeY);
    }

    public static (double X, double Y) RestorePosition(
        LastReadingPosition position, double maxOffsetX, double maxOffsetY)
    {
        ArgumentNullException.ThrowIfNull(position);
        ValidatePosition(position);
        ValidateLimit(maxOffsetX, nameof(maxOffsetX));
        ValidateLimit(maxOffsetY, nameof(maxOffsetY));
        return (position.RelativeX * maxOffsetX, position.RelativeY * maxOffsetY);
    }

    private static Dictionary<string, string> ValidateBindings(Dictionary<string, string> bindings)
    {
        if (bindings is null)
            throw new ArgumentException("KeyBindings cannot be null.", nameof(bindings));
        var normalized = new ReaderPreferences().KeyBindings;
        var commands = normalized.Keys.ToDictionary(command => command, StringComparer.OrdinalIgnoreCase);
        var suppliedCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (command, binding) in bindings)
        {
            if (!commands.TryGetValue(command, out var canonicalCommand) || !suppliedCommands.Add(command))
                throw new ArgumentException($"Unknown or duplicate key binding command: {command}.", nameof(bindings));
            normalized[canonicalCommand] = NormalizeBinding(binding);
        }
        // Add new commands without taking an existing user's shortcut or invalidating an older profile.
        foreach (string addedCommand in new[] { "ToggleToolbar", "Preferences", "ResetWidth", "ToggleReadingDirection" })
        {
            if (suppliedCommands.Contains(addedCommand)) continue;
            if (!normalized.Any(pair => pair.Key != addedCommand && pair.Value == normalized[addedCommand])) continue;
            normalized[addedCommand] = Enumerable.Range(1, 24).Select(number => $"Ctrl+Alt+Shift+F{number}")
                .First(candidate => !normalized.Values.Contains(candidate, StringComparer.OrdinalIgnoreCase));
        }
        var usedBindings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in normalized.Values)
        {
            if (!usedBindings.Add(binding))
                throw new ArgumentException($"Conflicting key binding: {binding}.", nameof(bindings));
        }
        return normalized;
    }

    private static string NormalizeBinding(string binding)
    {
        if (string.IsNullOrWhiteSpace(binding))
            throw new ArgumentException("Key bindings cannot be empty.", nameof(binding));
        var parts = binding.Split('+', StringSplitOptions.TrimEntries);
        var modifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var modifier in parts[..^1])
        {
            if (!(modifier.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)
                || modifier.Equals("Shift", StringComparison.OrdinalIgnoreCase)
                || modifier.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                || !modifiers.Add(modifier))
                throw new ArgumentException($"Invalid key binding modifier: {modifier}.", nameof(binding));
        }
        if (!KnownKeys.TryGetValue(parts[^1], out var key))
            throw new ArgumentException($"Unknown key in binding: {parts[^1]}.", nameof(binding));
        if (key is "Up" or "Down" or "Left" or "Right" or "Escape")
            throw new ArgumentException($"Reserved key in binding: {key}.", nameof(binding));
        var canonicalParts = new List<string>();
        foreach (var modifier in new[] { "Ctrl", "Shift", "Alt" })
        {
            if (modifiers.Contains(modifier)) canonicalParts.Add(modifier);
        }
        canonicalParts.Add(key);
        return string.Join('+', canonicalParts);
    }

    private static Dictionary<string, string> CreateKnownKeys()
    {
        var keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (char letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ") keys.Add(letter.ToString(), letter.ToString());
        for (int digit = 0; digit <= 9; digit++)
        {
            keys.Add($"D{digit}", $"D{digit}");
            keys.Add($"{digit}", $"D{digit}");
            keys.Add($"NumPad{digit}", $"NumPad{digit}");
        }
        for (int function = 1; function <= 24; function++) keys.Add($"F{function}", $"F{function}");
        foreach (var key in new[]
        {
            "PageUp", "PageDown", "Home", "End", "Insert", "Delete", "Back", "Tab", "Space", "Enter",
            "Escape", "Up", "Down", "Left", "Right", "OemPlus", "OemMinus", "Add", "Subtract",
            "Multiply", "Divide", "Decimal"
        }) keys.Add(key, key);
        return keys;
    }

    private static void ValidatePosition(LastReadingPosition position)
    {
        ValidateIdentity(position.PageKey, nameof(LastReadingPosition.PageKey));
        if (position.PageIndex < 0)
            throw new ArgumentException("PageIndex must be nonnegative.", nameof(LastReadingPosition.PageIndex));
        ValidateRange(position.RelativeX, 0, 1, nameof(LastReadingPosition.RelativeX));
        ValidateRange(position.RelativeY, 0, 1, nameof(LastReadingPosition.RelativeY));
    }

    private static void ValidateClassification(PageClassificationOverride classification)
    {
        if (!Enum.IsDefined(classification))
            throw new ArgumentException("Unknown page classification.", nameof(classification));
    }

    private static void ValidateRange(double value, double min, double max, string name)
    {
        if (!double.IsFinite(value) || value < min || value > max)
            throw new ArgumentException($"{name} must be finite and between {min} and {max}.", name);
    }

    private static void ValidateLimit(double limit, string name)
    {
        if (!double.IsFinite(limit) || limit < 0)
            throw new ArgumentException($"{name} must be finite and nonnegative.", name);
    }

    private static void ValidateIdentity(string identity, string name)
    {
        if (string.IsNullOrEmpty(identity))
            throw new ArgumentException($"{name} cannot be empty.", name);
    }
}
