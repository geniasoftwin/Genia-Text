using GeniaText.Features;
using GeniaText.Models;

namespace GeniaText.Services;

public sealed class ImportPreviewService
{
    private const long MaxImportBytes = 20 * 1024 * 1024;
    private const int MaxImportedPhrases = 10_000;
    private readonly SafetyPackService _safetyPack = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ImportPreviewResult Load(string filePath, IEnumerable<PhraseEntry> existing)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(existing);

        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("Файл импорта не найден.", filePath);
        if (fileInfo.Length > MaxImportBytes)
            throw new InvalidDataException(
                $"Файл импорта слишком большой ({fileInfo.Length:N0} байт). Максимум: {MaxImportBytes / 1024 / 1024} МБ.");

        string json = File.ReadAllText(filePath);
        List<PhraseEntry?> imported = JsonSerializer.Deserialize<List<PhraseEntry?>>(json, _jsonOptions) ?? [];
        if (imported.Count > MaxImportedPhrases)
            throw new InvalidDataException($"Слишком много фраз в одном файле. Максимум: {MaxImportedPhrases:N0}.");

        ImportPreviewResult result = _safetyPack.AnalyzeImport(imported, existing);
        if (result.ImportableCount == 0)
            throw new InvalidDataException("В выбранном файле нет фраз с текстом.");
        return result;
    }
}
