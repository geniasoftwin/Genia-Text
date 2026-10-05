using GeniaText.Models;

namespace GeniaText.Features;

public sealed class AdvancedSearchService
{
    private const int MaxSearchableFieldLength = 4096;

    public bool Matches(PhraseEntry phrase, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;

        string boundedQuery = query.Length <= 256 ? query : query[..256];
        string[] terms = boundedQuery
            .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (terms.Length == 0) return true;
        if (terms.Length > 12) terms = terms[..12];

        string searchable = BuildSearchableText(phrase);
        string[]? words = null;

        foreach (string term in terms)
        {
            if (searchable.Contains(term, StringComparison.CurrentCultureIgnoreCase))
                continue;

            // Conservative typo tolerance: one edit for short words, two for longer.
            int allowedDistance = term.Length >= 7 ? 2 : term.Length >= 4 ? 1 : 0;
            if (allowedDistance == 0)
                return false;

            words ??= searchable.Split(
                [' ', '\t', '\r', '\n', ',', '.', ';', ':', '!', '?', '-', '_', '/', '\\', '(', ')', '[', ']', '{', '}'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!words.Any(word => IsWithinDistance(word, term, allowedDistance)))
                return false;
        }

        return true;
    }

    private static string BuildSearchableText(PhraseEntry phrase)
    {
        static string Crop(string value) => value.Length <= MaxSearchableFieldLength
            ? value
            : value[..MaxSearchableFieldLength];

        return string.Concat(Crop(phrase.Title), "\n", Crop(phrase.Category), "\n", Crop(phrase.Text));
    }

    internal static bool IsWithinDistance(string source, string target, int maxDistance)
    {
        if (Math.Abs(source.Length - target.Length) > maxDistance) return false;
        if (string.Equals(source, target, StringComparison.CurrentCultureIgnoreCase)) return true;

        source = source.ToUpperInvariant();
        target = target.ToUpperInvariant();

        int[] previous = new int[target.Length + 1];
        int[] current = new int[target.Length + 1];
        for (int j = 0; j <= target.Length; j++) previous[j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            current[0] = i;
            int rowMinimum = current[0];
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
                rowMinimum = Math.Min(rowMinimum, current[j]);
            }

            if (rowMinimum > maxDistance) return false;
            (previous, current) = (current, previous);
        }

        return previous[target.Length] <= maxDistance;
    }
}
