using GeniaText.Features;
using GeniaText.Models;
using System.Text;

namespace GeniaText.Services;

public sealed class RecycleBinService
{
    private const long MaxArchiveBytes = 64 * 1024 * 1024;
    private const int MaxArchiveItems = 10_000;
    private readonly ApplicationPaths _paths;
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    private bool _loaded;
    private bool _writesBlocked;

    public RecycleBinService(ApplicationPaths paths)
    {
        _paths = paths;
        _filePath = Path.Combine(paths.DataDirectory, "trash.json");
    }

    public ObservableCollection<RecycleBinItem> Items { get; } = [];

    public void Load()
    {
        if (_loaded) return;
        _writesBlocked = false;
        Items.Clear();

        if (!File.Exists(_filePath))
        {
            _loaded = true;
            return;
        }

        var fileInfo = new FileInfo(_filePath);
        if (fileInfo.Length > MaxArchiveBytes)
        {
            _writesBlocked = true;
            throw new InvalidDataException(
                $"trash.json слишком большой ({fileInfo.Length:N0} байт). Исходный файл не изменён.");
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            List<RecycleBinItem?> loaded = JsonSerializer.Deserialize<List<RecycleBinItem?>>(json, _jsonOptions) ?? [];
            if (loaded.Count > MaxArchiveItems)
                throw new InvalidDataException($"В корзине больше {MaxArchiveItems:N0} записей. Исходный файл не изменён.");

            foreach (RecycleBinItem? item in loaded)
            {
                if (item is null || item.Phrase is null) continue;
                SafetyPackService.ValidatePhrase(item.Phrase);
                if (item.TrashId == Guid.Empty) item.TrashId = Guid.NewGuid();
                Items.Add(item);
            }
            _loaded = true;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Items.Clear();
            _writesBlocked = true;
            throw new InvalidDataException(
                "Корзину не удалось безопасно прочитать. Исходный trash.json оставлен без изменений, запись заблокирована до перезапуска.", ex);
        }
    }

    public RecycleBinItem Archive(PhraseEntry phrase)
    {
        ArgumentNullException.ThrowIfNull(phrase);
        EnsureWritable();

        var item = new RecycleBinItem
        {
            TrashId = Guid.NewGuid(),
            DeletedAt = DateTimeOffset.Now,
            Phrase = SafetyPackService.ClonePhrase(phrase)
        };
        Items.Insert(0, item);
        try
        {
            Save();
            return item;
        }
        catch
        {
            Items.Remove(item);
            throw;
        }
    }

    public void Remove(RecycleBinItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureWritable();
        int index = Items.IndexOf(item);
        if (index < 0) return;

        Items.RemoveAt(index);
        try
        {
            Save();
        }
        catch
        {
            Items.Insert(Math.Min(index, Items.Count), item);
            throw;
        }
    }

    public void Clear()
    {
        EnsureWritable();
        List<RecycleBinItem> snapshot = Items.ToList();
        Items.Clear();
        try
        {
            Save();
        }
        catch
        {
            foreach (RecycleBinItem item in snapshot)
                Items.Add(item);
            throw;
        }
    }

    private void EnsureWritable()
    {
        Load();
        if (_writesBlocked)
            throw new IOException("Запись корзины заблокирована, потому что trash.json не удалось безопасно загрузить.");
    }

    private void Save()
    {
        if (_writesBlocked)
            throw new IOException("Запись корзины заблокирована.");
        if (Items.Count > MaxArchiveItems)
            throw new InvalidDataException($"Корзина не может содержать больше {MaxArchiveItems:N0} записей.");

        string json = JsonSerializer.Serialize(Items, _jsonOptions);
        if (Encoding.UTF8.GetByteCount(json) > MaxArchiveBytes)
            throw new InvalidDataException($"Размер корзины не может превышать {MaxArchiveBytes / 1024 / 1024} МБ.");

        AtomicFile.WriteAllText(_filePath, json);
        _paths.MirrorIfModeSwitchPending(_filePath, "trash.json");
    }
}
