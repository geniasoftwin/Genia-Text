using GeniaText.Models;
using System.Collections.Specialized;

namespace GeniaText.Services;

public sealed class PhraseStore
{
    private const long MaxImportBytes = 20 * 1024 * 1024;
    private const long MaxPhraseDatabaseBytes = 64 * 1024 * 1024;
    private const int MaxImportedPhrases = 10_000;
    private const int MaxTitleLength = 512;
    private const int MaxCategoryLength = 256;
    private const int MaxPhraseTextLength = 1_000_000;
    private const int CoreBackupCount = 10;
    private const int AdvancedBackupCount = 30;
    private static readonly TimeSpan AdvancedBackupInterval = TimeSpan.FromMinutes(10);

    private readonly ApplicationPaths _paths;
    private readonly AppSettingsStore _settingsStore;
    private readonly string _filePath;
    private readonly string _backupDirectory;
    private readonly DispatcherTimer _saveTimer;
    private readonly HashSet<PhraseEntry> _subscribedPhrases = [];
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private bool _isLoading;
    private bool _isSaving;
    private bool _writesBlocked;
    private DateTimeOffset _lastAdvancedBackupAt = DateTimeOffset.MinValue;

    public PhraseStore(ApplicationPaths paths, AppSettingsStore settingsStore)
    {
        _paths = paths;
        _settingsStore = settingsStore;
        _filePath = paths.PhraseFilePath;
        _backupDirectory = paths.BackupDirectory;
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            TrySaveFromTimer();
        };
        Phrases.CollectionChanged += Phrases_CollectionChanged;
        MigrateLegacyPhraseFile();
    }

    public ObservableCollection<PhraseEntry> Phrases { get; } = [];

    public event EventHandler<string>? SaveFailed;

    public void Load()
    {
        _isLoading = true;
        _writesBlocked = false;
        bool shouldSave = false;
        try
        {
            Phrases.Clear();
            if (!File.Exists(_filePath))
            {
                AddDefaultPhrases();
                shouldSave = true;
                return;
            }

            var databaseInfo = new FileInfo(_filePath);
            if (databaseInfo.Length > MaxPhraseDatabaseBytes)
            {
                _writesBlocked = true;
                throw new InvalidDataException(
                    $"phrases.json слишком большой ({databaseInfo.Length:N0} байт). Максимум: {MaxPhraseDatabaseBytes / 1024 / 1024} МБ. Исходный файл не изменён, запись заблокирована.");
            }

            CreateBackup("startup");
            try
            {
                string json = File.ReadAllText(_filePath);
                List<PhraseEntry?> loaded = JsonSerializer.Deserialize<List<PhraseEntry?>>(json, _jsonOptions) ?? [];
                foreach (PhraseEntry? candidate in loaded)
                {
                    if (candidate is null) continue;
                    PhraseEntry phrase = candidate;
                    NormalizePhrase(phrase);
                    Phrases.Add(phrase);
                }
                NormalizeSortOrders();
            }
            catch (JsonException ex)
            {
                // Only malformed JSON is quarantined. 0.6.2 used a catch-all here,
                // so a transient I/O/ACL failure could move a valid user file away.
                AppLog.Error("Поврежден phrases.json", ex);
                QuarantineBrokenPhraseFile();
                Phrases.Clear();
                AddDefaultPhrases();
                shouldSave = true;
            }
            catch (IOException ex)
            {
                _writesBlocked = true;
                AppLog.Error("Не удалось прочитать phrases.json", ex);
                throw new IOException("Не удалось прочитать файл фраз. Исходный файл не изменён, а запись заблокирована до следующего успешного запуска.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                _writesBlocked = true;
                AppLog.Error("Нет доступа к phrases.json", ex);
                throw new UnauthorizedAccessException("Нет доступа к файлу фраз. Исходный файл не изменён, а запись заблокирована до следующего успешного запуска.", ex);
            }
        }
        finally
        {
            _isLoading = false;
            if (shouldSave)
                Save();
        }
    }

    public void Save()
    {
        if (_writesBlocked)
            throw new IOException("Сохранение заблокировано, потому что исходный phrases.json не удалось безопасно загрузить.");
        if (_isLoading || _isSaving) return;
        _isSaving = true;
        try
        {
            NormalizeSortOrders();
            MaybeCreateAdvancedBackup();
            List<PhraseEntry> phrasesToSave = Phrases
                .Where(p => !string.IsNullOrWhiteSpace(p.Text) ||
                            !string.IsNullOrWhiteSpace(p.Title) ||
                            !string.IsNullOrWhiteSpace(p.Category))
                .ToList();

            foreach (PhraseEntry phrase in phrasesToSave)
                NormalizePhrase(phrase);

            string json = JsonSerializer.Serialize(phrasesToSave, _jsonOptions);
            AtomicFile.WriteAllText(_filePath, json);
            _paths.MirrorIfModeSwitchPending(_filePath, "phrases.json");
        }
        finally
        {
            _isSaving = false;
        }
    }

    public void ScheduleSave()
    {
        if (_writesBlocked || _isLoading || _isSaving) return;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    public void Flush()
    {
        _saveTimer.Stop();
        Save();
    }

    public void DiscardEmptyPhrases()
    {
        for (int i = Phrases.Count - 1; i >= 0; i--)
        {
            PhraseEntry phrase = Phrases[i];
            if (string.IsNullOrWhiteSpace(phrase.Text) &&
                string.IsNullOrWhiteSpace(phrase.Title) &&
                string.IsNullOrWhiteSpace(phrase.Category))
            {
                Phrases.RemoveAt(i);
            }
        }
        NormalizeSortOrders();
    }

    public PhraseEntry Duplicate(PhraseEntry source)
    {
        int sourceIndex = Phrases.IndexOf(source);
        var copy = new PhraseEntry
        {
            Title = string.IsNullOrWhiteSpace(source.Title) ? string.Empty : source.Title + " — копия",
            Text = source.Text,
            Category = source.Category,
            IsFavorite = source.IsFavorite
        };
        int index = sourceIndex < 0 ? Phrases.Count : sourceIndex + 1;
        Phrases.Insert(index, copy);
        NormalizeSortOrders();
        ScheduleSave();
        return copy;
    }

    public void Move(PhraseEntry phrase, int newIndex)
    {
        int oldIndex = Phrases.IndexOf(phrase);
        if (oldIndex < 0 || Phrases.Count == 0) return;

        newIndex = Math.Clamp(newIndex, 0, Phrases.Count - 1);
        if (oldIndex == newIndex) return;
        Phrases.Move(oldIndex, newIndex);
        NormalizeSortOrders();
        ScheduleSave();
    }

    public void MarkUsed(PhraseEntry phrase)
    {
        if (phrase.UseCount < int.MaxValue)
            phrase.UseCount++;
        phrase.LastUsedAt = DateTimeOffset.Now;
        ScheduleSave();
    }

    public IReadOnlyList<string> GetCategories() => Phrases
        .Select(p => p.Category.Trim())
        .Where(c => !string.IsNullOrWhiteSpace(c))
        .Distinct(StringComparer.CurrentCultureIgnoreCase)
        .OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    public void Export(string path)
    {
        Flush();
        if (!File.Exists(_filePath))
            throw new FileNotFoundException("Файл фраз ещё не создан.", _filePath);
        AtomicFile.CopyReplace(_filePath, path);
    }

    /// <summary>Imports phrases. append=true adds to current phrases; false replaces them.</summary>
    public int Import(string path, bool append)
    {
        var info = new FileInfo(path);
        if (!info.Exists)
            throw new FileNotFoundException("Файл импорта не найден.", path);
        if (info.Length > MaxImportBytes)
            throw new InvalidDataException($"Файл импорта слишком большой. Максимум: {MaxImportBytes / 1024 / 1024} МБ.");

        string json = File.ReadAllText(path);
        List<PhraseEntry?>? imported;
        try
        {
            imported = JsonSerializer.Deserialize<List<PhraseEntry?>>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Файл содержит некорректный JSON.", ex);
        }

        if (imported is null)
            throw new InvalidDataException("Файл не содержит фраз.");
        if (imported.Count > MaxImportedPhrases)
            throw new InvalidDataException($"Слишком много фраз в одном файле. Максимум: {MaxImportedPhrases:N0}.");

        List<PhraseEntry> validPhrases = [];
        foreach (PhraseEntry? candidate in imported)
        {
            if (candidate is null) continue;
            PhraseEntry phrase = candidate;
            ValidateImportedPhrase(phrase);
            NormalizePhrase(phrase);
            if (!string.IsNullOrWhiteSpace(phrase.Text))
                validPhrases.Add(phrase);
        }

        if (validPhrases.Count == 0)
            throw new InvalidDataException("В выбранном файле нет фраз с текстом.");

        CreateBackup("before-import");
        _isLoading = true;
        try
        {
            if (!append)
                Phrases.Clear();

            HashSet<Guid> existingIds = Phrases.Select(p => p.Id).ToHashSet();
            foreach (PhraseEntry phrase in validPhrases)
            {
                if (phrase.Id == Guid.Empty || !existingIds.Add(phrase.Id))
                {
                    phrase.Id = Guid.NewGuid();
                    existingIds.Add(phrase.Id);
                }
                Phrases.Add(phrase);
            }
            NormalizeSortOrders();
        }
        finally
        {
            _isLoading = false;
        }

        Save();
        return validPhrases.Count;
    }

    private void Phrases_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (PhraseEntry phrase in e.OldItems)
                Unsubscribe(phrase);
        if (e.NewItems is not null)
            foreach (PhraseEntry phrase in e.NewItems)
                Subscribe(phrase);

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (PhraseEntry phrase in _subscribedPhrases.ToArray())
                Unsubscribe(phrase);
            foreach (PhraseEntry phrase in Phrases)
                Subscribe(phrase);
        }

        ScheduleSave();
    }

    private void Phrase_PropertyChanged(object? sender, PropertyChangedEventArgs e) => ScheduleSave();

    private void Subscribe(PhraseEntry phrase)
    {
        if (_subscribedPhrases.Add(phrase))
            phrase.PropertyChanged += Phrase_PropertyChanged;
    }

    private void Unsubscribe(PhraseEntry phrase)
    {
        if (_subscribedPhrases.Remove(phrase))
            phrase.PropertyChanged -= Phrase_PropertyChanged;
    }

    private void NormalizeSortOrders()
    {
        for (int i = 0; i < Phrases.Count; i++)
            Phrases[i].SortOrder = i;
    }

    private static void NormalizePhrase(PhraseEntry phrase)
    {
        if (phrase.Id == Guid.Empty) phrase.Id = Guid.NewGuid();
        phrase.Title = (phrase.Title ?? string.Empty).Trim();
        phrase.Category = (phrase.Category ?? string.Empty).Trim();
        phrase.Text ??= string.Empty;
        if (phrase.UseCount < 0) phrase.UseCount = 0;
    }

    private static void ValidateImportedPhrase(PhraseEntry phrase)
    {
        if ((phrase.Title?.Length ?? 0) > MaxTitleLength)
            throw new InvalidDataException($"Название фразы длиннее {MaxTitleLength} символов.");
        if ((phrase.Category?.Length ?? 0) > MaxCategoryLength)
            throw new InvalidDataException($"Категория длиннее {MaxCategoryLength} символов.");
        if ((phrase.Text?.Length ?? 0) > MaxPhraseTextLength)
            throw new InvalidDataException($"Текст одной фразы длиннее {MaxPhraseTextLength:N0} символов.");
    }

    private void CreateBackup(string reason)
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            Directory.CreateDirectory(_backupDirectory);
            string backupPath = Path.Combine(_backupDirectory,
                $"phrases-{DateTime.Now:yyyyMMdd-HHmmss-fff}-{SanitizeReason(reason)}.json");
            File.Copy(_filePath, backupPath, overwrite: false);

            int backupCount = _settingsStore.Current.Features.AdvancedBackupsEnabled
                ? AdvancedBackupCount
                : CoreBackupCount;
            foreach (string old in Directory.EnumerateFiles(_backupDirectory, "phrases-*.json")
                         .OrderByDescending(File.GetLastWriteTimeUtc)
                         .Skip(backupCount))
            {
                try { File.Delete(old); } catch { }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Error("Не удалось создать резервную копию", ex);
        }
    }


    private void MaybeCreateAdvancedBackup()
    {
        if (!_settingsStore.Current.Features.AdvancedBackupsEnabled || !File.Exists(_filePath))
            return;

        DateTimeOffset now = DateTimeOffset.Now;
        if (now - _lastAdvancedBackupAt < AdvancedBackupInterval)
            return;

        _lastAdvancedBackupAt = now;
        CreateBackup("periodic");
    }

    private static string SanitizeReason(string reason) =>
        new(reason.Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_').Take(32).ToArray());

    private void QuarantineBrokenPhraseFile()
    {
        try
        {
            if (File.Exists(_filePath))
                File.Move(_filePath, _filePath + $".broken-{DateTime.Now:yyyyMMdd-HHmmss}", true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Error("Не удалось сохранить поврежденный phrases.json", ex);
        }
    }

    private void MigrateLegacyPhraseFile()
    {
        if (File.Exists(_filePath)) return;
        string legacyFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuickPhrase", "phrases.json");
        if (!File.Exists(legacyFilePath)) return;

        try
        {
            AtomicFile.CopyReplace(legacyFilePath, _filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Error("Не удалось перенести QuickPhrase", ex);
        }
    }

    private void AddDefaultPhrases()
    {
        Phrases.Add(new PhraseEntry { Title = "Приветствие", Text = "Здравствуйте! Чем я могу вам помочь?" });
        Phrases.Add(new PhraseEntry { Title = "Уточнение", Text = "Уточните, пожалуйста, ваш вопрос, чтобы я мог дать точный ответ." });
        Phrases.Add(new PhraseEntry { Title = "Завершение разговора", Text = "Спасибо за обращение! Хорошего дня!" });
        NormalizeSortOrders();
    }

    private void TrySaveFromTimer()
    {
        try
        {
            Save();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Error("Автосохранение фраз не удалось", ex);
            SaveFailed?.Invoke(this, ex.Message);
        }
    }
}
