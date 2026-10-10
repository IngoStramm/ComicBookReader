namespace ComicBookReader.Core.ComicFiles;

public sealed class NaturalEntryComparer : IComparer<string>
{
    public static NaturalEntryComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (x is null || y is null) return StringComparer.Ordinal.Compare(x, y);
        int a = 0, b = 0;
        while (a < x.Length && b < y.Length)
        {
            if (char.IsAsciiDigit(x[a]) && char.IsAsciiDigit(y[b]))
            {
                int startA = a, startB = b;
                while (a < x.Length && char.IsAsciiDigit(x[a])) a++;
                while (b < y.Length && char.IsAsciiDigit(y[b])) b++;
                while (startA < a && x[startA] == '0') startA++;
                while (startB < b && y[startB] == '0') startB++;
                int comparison = (a - startA).CompareTo(b - startB);
                if (comparison == 0)
                    comparison = x.AsSpan(startA, a - startA).SequenceCompareTo(y.AsSpan(startB, b - startB));
                if (comparison != 0) return comparison;
            }
            else
            {
                int comparison = char.ToUpperInvariant(x[a++]).CompareTo(char.ToUpperInvariant(y[b++]));
                if (comparison != 0) return comparison;
            }
        }
        int remaining = (x.Length - a).CompareTo(y.Length - b);
        return remaining != 0 ? remaining : StringComparer.Ordinal.Compare(x, y);
    }
}

public sealed record IndexedComicPage(PageInfo Page, int EntryOrdinal);

public static class ComicEntryIndex
{
    public static bool IsImage(string name)
    {
        var normalized = name.Replace('\\', '/');
        if (normalized.Split('/').Any(segment => segment == "__MACOSX" || segment.StartsWith("._", StringComparison.Ordinal)))
            return false;
        return Path.GetExtension(name).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".webp";
    }

    public static IReadOnlyList<IndexedComicPage> Create(IEnumerable<(string Name, int Ordinal)> entries)
    {
        var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
        return entries.Where(e => IsImage(e.Name))
            .Select(e =>
            {
                var normalized = e.Name.Replace('\\', '/');
                int duplicate = occurrences.GetValueOrDefault(normalized);
                occurrences[normalized] = duplicate + 1;
                return (e.Name, e.Ordinal, Key: normalized + "#" + duplicate);
            })
            .OrderBy(e => e.Name, NaturalEntryComparer.Instance).ThenBy(e => e.Ordinal)
            .Select((e, index) => new IndexedComicPage(new PageInfo(index, e.Key, e.Name), e.Ordinal))
            .ToArray();
    }
}
