using GeniaText.Models;

namespace GeniaText.Features;

public enum ImportPreviewStatus
{
    New,
    ExactDuplicate,
    IdConflict
}

public sealed class ImportPreviewItem
{
    public ImportPreviewItem(PhraseEntry phrase, ImportPreviewStatus status)
    {
        Phrase = phrase;
        Status = status;
    }

    public PhraseEntry Phrase { get; }
    public ImportPreviewStatus Status { get; }
    public string DisplayTitle => Phrase.DisplayTitle;
    public string Category => Phrase.Category;
    public string StatusLabel => Status switch
    {
        ImportPreviewStatus.New => "Новая",
        ImportPreviewStatus.ExactDuplicate => "Точный повтор",
        ImportPreviewStatus.IdConflict => "Конфликт ID",
        _ => "Неизвестно"
    };
}

public sealed class ImportPreviewResult
{
    public ImportPreviewResult(IReadOnlyList<ImportPreviewItem> items, int sourceCount, int skippedEmptyCount)
    {
        Items = items;
        SourceCount = sourceCount;
        SkippedEmptyCount = skippedEmptyCount;
    }

    public IReadOnlyList<ImportPreviewItem> Items { get; }
    public int SourceCount { get; }
    public int SkippedEmptyCount { get; }
    public int NewCount => Items.Count(item => item.Status == ImportPreviewStatus.New);
    public int DuplicateCount => Items.Count(item => item.Status == ImportPreviewStatus.ExactDuplicate);
    public int IdConflictCount => Items.Count(item => item.Status == ImportPreviewStatus.IdConflict);
    public int ImportableCount => Items.Count;
}

/// <summary>
/// Pure helper for optional recovery modules. It does not read, write or mutate
/// the live phrase database and is deliberately independent from Picker/Core.
/// </summary>
public sealed class SafetyPackService
{
    public const int MaxTitleLength = 512;
    public const int MaxCategoryLength = 256;
    public const int MaxPhraseTextLength = 1_000_000;

    public ImportPreviewResult AnalyzeImport(
        IEnumerable<PhraseEntry?> imported,
        IEnumerable<PhraseEntry> existing)
    {
        ArgumentNullException.ThrowIfNull(imported);
        ArgumentNullException.ThrowIfNull(existing);

        List<PhraseEntry?> candidates = imported.ToList();
        HashSet<Guid> knownIds = existing
            .Where(phrase => phrase.Id != Guid.Empty)
            .Select(phrase => phrase.Id)
            .ToHashSet();
        HashSet<string> knownTexts = existing
            .Select(phrase => NormalizeText(phrase.Text))
            .Where(text => text.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

        List<ImportPreviewItem> items = [];
        int skippedEmpty = 0;
        foreach (PhraseEntry? candidate in candidates)
        {
            if (candidate is null)
            {
                skippedEmpty++;
                continue;
            }

            ValidatePhrase(candidate);
            string normalizedText = NormalizeText(candidate.Text);
            if (normalizedText.Length == 0)
            {
                skippedEmpty++;
                continue;
            }

            bool duplicate = !knownTexts.Add(normalizedText);
            bool idConflict = candidate.Id != Guid.Empty && !knownIds.Add(candidate.Id);
            ImportPreviewStatus status = duplicate
                ? ImportPreviewStatus.ExactDuplicate
                : idConflict
                    ? ImportPreviewStatus.IdConflict
                    : ImportPreviewStatus.New;
            items.Add(new ImportPreviewItem(ClonePhrase(candidate), status));
        }

        return new ImportPreviewResult(items, candidates.Count, skippedEmpty);
    }

    public PhraseEntry CloneForInsert(PhraseEntry source, IEnumerable<Guid> existingIds, int sortOrder)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(existingIds);

        PhraseEntry clone = ClonePhrase(source);
        HashSet<Guid> usedIds = existingIds.ToHashSet();
        while (clone.Id == Guid.Empty || usedIds.Contains(clone.Id))
            clone.Id = Guid.NewGuid();
        clone.SortOrder = Math.Max(0, sortOrder);
        return clone;
    }

    public static PhraseEntry ClonePhrase(PhraseEntry source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new PhraseEntry
        {
            Id = source.Id,
            Title = source.Title,
            Text = source.Text,
            Category = source.Category,
            IsFavorite = source.IsFavorite,
            SortOrder = source.SortOrder,
            UseCount = Math.Max(0, source.UseCount),
            LastUsedAt = source.LastUsedAt
        };
    }

    public static void ValidatePhrase(PhraseEntry phrase)
    {
        ArgumentNullException.ThrowIfNull(phrase);
        if (phrase.Title.Length > MaxTitleLength)
            throw new InvalidDataException($"Название фразы длиннее {MaxTitleLength} символов.");
        if (phrase.Category.Length > MaxCategoryLength)
            throw new InvalidDataException($"Категория длиннее {MaxCategoryLength} символов.");
        if (phrase.Text.Length > MaxPhraseTextLength)
            throw new InvalidDataException($"Текст одной фразы длиннее {MaxPhraseTextLength:N0} символов.");
    }

    private static string NormalizeText(string text) =>
        (text ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
}
