using GeniaText.Models;

namespace GeniaText.Features;

/// <summary>
/// Pure, optional editor operations. This service never participates in the
/// hotkey, picker or paste path.
/// </summary>
public sealed class EditorProService
{
    public const int MaxCategoryLength = 256;

    public IReadOnlyList<DuplicatePhraseGroup> FindDuplicateGroups(IEnumerable<PhraseEntry> phrases)
    {
        ArgumentNullException.ThrowIfNull(phrases);

        return phrases
            .Where(phrase => !string.IsNullOrWhiteSpace(phrase.Text))
            .GroupBy(phrase => NormalizeText(phrase.Text), StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => new DuplicatePhraseGroup(
                group.OrderBy(phrase => phrase.SortOrder).ToList()))
            .OrderBy(group => group.Phrases[0].SortOrder)
            .ToList();
    }

    public int ApplyCategory(IEnumerable<PhraseEntry> phrases, string? category)
    {
        ArgumentNullException.ThrowIfNull(phrases);
        string normalized = (category ?? string.Empty).Trim();
        if (normalized.Length > MaxCategoryLength)
            throw new ArgumentException($"Категория длиннее {MaxCategoryLength} символов.", nameof(category));

        int changed = 0;
        foreach (PhraseEntry phrase in phrases.Distinct())
        {
            if (string.Equals(phrase.Category, normalized, StringComparison.Ordinal)) continue;
            phrase.Category = normalized;
            changed++;
        }
        return changed;
    }

    public int SetFavorite(IEnumerable<PhraseEntry> phrases, bool isFavorite)
    {
        ArgumentNullException.ThrowIfNull(phrases);

        int changed = 0;
        foreach (PhraseEntry phrase in phrases.Distinct())
        {
            if (phrase.IsFavorite == isFavorite) continue;
            phrase.IsFavorite = isFavorite;
            changed++;
        }
        return changed;
    }

    private static string NormalizeText(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();
}

public sealed record DuplicatePhraseGroup(IReadOnlyList<PhraseEntry> Phrases);
