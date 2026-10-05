using GeniaText.Features;
using GeniaText.Models;

namespace GeniaText.Services;

public sealed class BackupSnapshotInfo
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Reason { get; init; }
    public required long SizeBytes { get; init; }
    public string CreatedAtLabel => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
    public string SizeLabel => SizeBytes < 1024
        ? $"{SizeBytes} Б"
        : $"{SizeBytes / 1024d:N1} КБ";
}

public sealed class BackupBrowserService
{
    // Keep whole-snapshot restore compatible with the frozen PhraseStore.Import limit.
    private const long MaxSnapshotBytes = 20 * 1024 * 1024;
    private const int MaxSnapshotPhrases = 10_000;
    private readonly string _backupDirectory;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BackupBrowserService(ApplicationPaths paths)
    {
        _backupDirectory = paths.BackupDirectory;
    }

    public IReadOnlyList<BackupSnapshotInfo> ListSnapshots()
    {
        if (!Directory.Exists(_backupDirectory)) return [];

        return Directory.EnumerateFiles(_backupDirectory, "phrases-*.json", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.LastWriteTimeUtc)
            .Select(info => new BackupSnapshotInfo
            {
                FilePath = info.FullName,
                FileName = info.Name,
                CreatedAt = info.LastWriteTime,
                Reason = ReadReason(info.Name),
                SizeBytes = info.Length
            })
            .ToList();
    }

    public IReadOnlyList<PhraseEntry> LoadPhrases(BackupSnapshotInfo snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();
        string filePath = ValidateSnapshotPath(snapshot.FilePath);
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("Резервная копия больше не существует.", filePath);
        if (fileInfo.Length > MaxSnapshotBytes)
            throw new InvalidDataException(
                $"Резервная копия слишком большая ({fileInfo.Length:N0} байт). Максимум: {MaxSnapshotBytes / 1024 / 1024} МБ.");

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        List<PhraseEntry?> loaded = JsonSerializer.Deserialize<List<PhraseEntry?>>(stream, _jsonOptions) ?? [];
        cancellationToken.ThrowIfCancellationRequested();
        if (loaded.Count > MaxSnapshotPhrases)
            throw new InvalidDataException($"В резервной копии больше {MaxSnapshotPhrases:N0} фраз.");

        List<PhraseEntry> phrases = [];
        foreach (PhraseEntry? phrase in loaded)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (phrase is null) continue;
            SafetyPackService.ValidatePhrase(phrase);
            if (string.IsNullOrWhiteSpace(phrase.Text) &&
                string.IsNullOrWhiteSpace(phrase.Title) &&
                string.IsNullOrWhiteSpace(phrase.Category))
                continue;
            // Preview data is detached from the live PhraseStore already. Keep the
            // deserialized object instead of cloning every phrase a second time; the
            // restore command performs its own safe clone before inserting anything.
            phrases.Add(phrase);
        }
        return phrases;
    }

    public string GetValidatedPath(BackupSnapshotInfo snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return ValidateSnapshotPath(snapshot.FilePath);
    }

    public void DeleteSnapshot(BackupSnapshotInfo snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        string filePath = ValidateSnapshotPath(snapshot.FilePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Резервная копия больше не существует.", filePath);

        string fileName = Path.GetFileName(filePath);
        if (!fileName.StartsWith("phrases-", StringComparison.OrdinalIgnoreCase) ||
            !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Можно удалить только резервную копию GeniaText.");

        File.Delete(filePath);
    }

    private string ValidateSnapshotPath(string path)
    {
        string root = Path.GetFullPath(_backupDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Выбранный файл находится вне каталога резервных копий.");
        return fullPath;
    }

    private static string ReadReason(string fileName)
    {
        string[] parts = Path.GetFileNameWithoutExtension(fileName).Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5) return "Резервная копия";
        return string.Join('-', parts.Skip(4)) switch
        {
            "startup" => "Запуск программы",
            "before-import" => "Перед импортом",
            "periodic" => "Периодическая",
            string reason => reason
        };
    }
}
