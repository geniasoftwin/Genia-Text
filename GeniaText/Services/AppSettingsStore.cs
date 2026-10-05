using GeniaText.Models;
using GeniaText.Features;
using System.Text.Json.Serialization;

namespace GeniaText.Services;

public sealed class AppSettingsStore
{
    private const long MaxSettingsBytes = 1 * 1024 * 1024;
    private readonly ApplicationPaths _paths;
    private readonly string _filePath;
    private readonly DispatcherTimer _saveTimer;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    private bool _writesBlocked;

    public AppSettingsStore(ApplicationPaths paths)
    {
        _paths = paths;
        _filePath = paths.SettingsFilePath;
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            TrySave(showError: false);
        };
    }

    public AppSettings Current { get; private set; } = new();
    public string? LoadWarning { get; private set; }

    public void Load()
    {
        _writesBlocked = false;
        LoadWarning = null;

        if (!File.Exists(_filePath))
        {
            Current = new AppSettings();
            return;
        }

        try
        {
            var info = new FileInfo(_filePath);
            if (info.Length > MaxSettingsBytes)
                throw new InvalidDataException($"settings.json слишком большой ({info.Length:N0} байт). Максимум: {MaxSettingsBytes:N0} байт.");

            string json = File.ReadAllText(_filePath);
            Current = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
            SettingsSanitizer.Sanitize(Current);
        }
        catch (JsonException ex)
        {
            AppLog.Error("Некорректный settings.json", ex);
            QuarantineBrokenSettings();
            Current = new AppSettings();
        }
        catch (InvalidDataException ex)
        {
            // Oversized/untrusted settings are not loaded and are never silently
            // overwritten. Keep the original file for manual recovery.
            _writesBlocked = true;
            Current = new AppSettings();
            LoadWarning = ex.Message + " Запись settings.json заблокирована до следующего успешного запуска.";
            AppLog.Error("settings.json отклонён", ex);
        }
        catch (IOException ex)
        {
            BlockWritesAfterReadFailure("Не удалось прочитать settings.json", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            BlockWritesAfterReadFailure("Нет доступа к settings.json", ex);
        }
    }

    public void ScheduleSave()
    {
        if (_writesBlocked) return;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    public void Save()
    {
        if (_writesBlocked)
            throw new IOException("Сохранение настроек заблокировано, потому что существующий settings.json не удалось безопасно загрузить.");

        SettingsSanitizer.Sanitize(Current);
        string json = JsonSerializer.Serialize(Current, _jsonOptions);
        AtomicFile.WriteAllText(_filePath, json);
        _paths.MirrorIfModeSwitchPending(_filePath, "settings.json");
    }

    public void Flush()
    {
        _saveTimer.Stop();
        Save();
    }

    private void TrySave(bool showError)
    {
        try
        {
            Save();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Error("Не удалось сохранить настройки", ex);
            if (showError)
                MessageBox.Show($"Не удалось сохранить настройки.\n\n{ex.Message}", "GeniaText",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BlockWritesAfterReadFailure(string context, Exception ex)
    {
        _writesBlocked = true;
        Current = new AppSettings();
        LoadWarning = ex.Message + " Исходный settings.json не изменён; запись заблокирована до следующего успешного запуска.";
        AppLog.Error(context, ex);
    }

    private void QuarantineBrokenSettings()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            File.Move(_filePath, _filePath + $".broken-{DateTime.Now:yyyyMMdd-HHmmss}", true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // If quarantine itself fails, do not allow a future timer save to
            // overwrite the malformed source file.
            _writesBlocked = true;
            LoadWarning = "Повреждённый settings.json не удалось безопасно переименовать. Запись настроек заблокирована до следующего запуска.";
            AppLog.Error("Не удалось переименовать поврежденный settings.json", ex);
        }
    }

}
