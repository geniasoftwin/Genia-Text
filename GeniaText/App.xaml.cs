using GeniaText.Models;
using GeniaText.Services;
using System.Drawing;
using Forms = System.Windows.Forms;

namespace GeniaText;

public partial class App : System.Windows.Application
{
    private Mutex? _singleInstanceMutex;
    private ApplicationPaths? _paths;
    private AppSettingsStore? _settingsStore;
    private PhraseStore? _phraseStore;
    private PasteService? _pasteService;
    private AutostartService? _autostartService;
    private GlobalHotkey? _globalHotkey;
    private PickerWindow? _pickerWindow;
    private PhraseEditorWindow? _editorWindow;
    private Forms.NotifyIcon? _trayIcon;
    private Forms.ToolStripMenuItem? _autostartItem;
    private bool _exiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RegisterCrashDiagnostics();

        _singleInstanceMutex = new Mutex(initiallyOwned: true, "Local\\GeniaText.SingleInstance", out bool isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show("GeniaText уже запущен.", "GeniaText", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        try
        {
            _paths = new ApplicationPaths();
            AppLog.Initialize(_paths);
            _settingsStore = new AppSettingsStore(_paths);
            _settingsStore.Load();
            if (!string.IsNullOrWhiteSpace(_settingsStore.LoadWarning))
            {
                MessageBox.Show(
                    $"Настройки не удалось безопасно загрузить. GeniaText запущен с настройками по умолчанию, но исходный settings.json не будет перезаписан.\n\n{_settingsStore.LoadWarning}",
                    "GeniaText", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            _phraseStore = new PhraseStore(_paths, _settingsStore);
            try
            {
                _phraseStore.Load();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                MessageBox.Show($"GeniaText не смог загрузить ваши фразы. Исходный файл оставлен без изменений.\n\n{ex.Message}",
                    "GeniaText", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            _phraseStore.SaveFailed += (_, message) => Dispatcher.Invoke(() =>
                MessageBox.Show($"Автосохранение не удалось.\n\n{message}", "GeniaText",
                    MessageBoxButton.OK, MessageBoxImage.Warning));

            _pasteService = new PasteService(_settingsStore);
            _autostartService = new AutostartService();
            // A portable startup should not modify the registry automatically.
            // The user can still explicitly enable Autostart from the tray menu.
            if (!_paths.IsPortable)
            {
                try { _autostartService.MigrateLegacyRegistration(); }
                catch (Exception ex) { AppLog.Error("Миграция автозапуска", ex); }
            }

            _pickerWindow = new PickerWindow(_phraseStore, _pasteService, _settingsStore);
            _pickerWindow.EditRequested += (_, _) => OpenEditor();

            _globalHotkey = new GlobalHotkey(
                _pickerWindow,
                _settingsStore.Current.HotkeyModifiers,
                _settingsStore.Current.HotkeyVirtualKey);
            _globalHotkey.Pressed += (_, _) => Dispatcher.Invoke(() =>
            {
                _pickerWindow.CaptureTargetWindow();
                _pickerWindow.ShowPicker();
            });

            if (!_globalHotkey.Register())
                RestoreDefaultHotkeyOrWarn();

            CreateTrayIcon();
        }
        catch (Exception ex)
        {
            AppLog.Error("Ошибка запуска", ex);
            MessageBox.Show($"Не удалось запустить GeniaText.\n\n{ex.Message}", "GeniaText",
                MessageBoxButton.OK, MessageBoxImage.Error);
            ExitApplication();
        }
    }

    private void RegisterCrashDiagnostics()
    {
        DispatcherUnhandledException += (_, args) =>
        {
            AppLog.Error("Необработанная ошибка UI", args.Exception);
            // Do not mark as handled: continuing after an unknown UI exception can
            // corrupt state. The log remains available for diagnosis.
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                AppLog.Error("Необработанная ошибка процесса", ex);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            AppLog.Error("Необработанная ошибка фоновой задачи", args.Exception);
            args.SetObserved();
        };
    }

    private void CreateTrayIcon()
    {
        _trayIcon = new Forms.NotifyIcon
        {
            Text = GetTrayText(),
            Icon = LoadApplicationIcon(),
            Visible = true
        };

        var menu = new Forms.ContextMenuStrip();
        var openItem = new Forms.ToolStripMenuItem("Открыть GeniaText");
        openItem.Click += (_, _) => Dispatcher.Invoke(() =>
        {
            _pickerWindow?.CaptureTargetWindow();
            _pickerWindow?.ShowPicker();
        });
        var editorItem = new Forms.ToolStripMenuItem("Редактор фраз");
        editorItem.Click += (_, _) => Dispatcher.Invoke(OpenEditor);
        var settingsItem = new Forms.ToolStripMenuItem("Настройки");
        settingsItem.Click += (_, _) => Dispatcher.Invoke(OpenSettings);
        _autostartItem = new Forms.ToolStripMenuItem("Автозапуск") { CheckOnClick = false };
        try { _autostartItem.Checked = _autostartService?.IsEnabled == true; } catch { }
        _autostartItem.Click += (_, _) => Dispatcher.Invoke(ToggleAutostart);
        var exitItem = new Forms.ToolStripMenuItem("Выход");
        exitItem.Click += (_, _) => Dispatcher.Invoke(ExitApplication);

        menu.Items.Add(openItem);
        menu.Items.Add(editorItem);
        menu.Items.Add(settingsItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_autostartItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(exitItem);
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(OpenEditor);
    }

    private void RestoreDefaultHotkeyOrWarn()
    {
        if (_globalHotkey is null || _settingsStore is null) return;
        const uint defaultModifiers = 0x0002;
        const uint defaultVirtualKey = 0x20;

        if (_globalHotkey.TryChange(defaultModifiers, defaultVirtualKey))
        {
            _settingsStore.Current.HotkeyModifiers = defaultModifiers;
            _settingsStore.Current.HotkeyVirtualKey = defaultVirtualKey;
            _settingsStore.ScheduleSave();
            MessageBox.Show("Сохранённая горячая клавиша занята другой программой. GeniaText вернулся к Ctrl+Space.",
                "GeniaText", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else
        {
            MessageBox.Show("Не удалось зарегистрировать глобальную горячую клавишу. Открывайте GeniaText через значок в трее и выберите другую комбинацию в настройках.",
                "GeniaText", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private string GetTrayText()
    {
        if (_globalHotkey is null) return "GeniaText";
        string hotkey = GlobalHotkey.Format(_globalHotkey.Modifiers, _globalHotkey.VirtualKey);
        string text = $"GeniaText — {hotkey}";
        return text.Length <= 63 ? text : text[..63];
    }

    private void UpdateTrayText()
    {
        if (_trayIcon is not null)
            _trayIcon.Text = GetTrayText();
    }

    private static Icon LoadApplicationIcon()
    {
        try
        {
            string? processPath = Environment.ProcessPath;
            Icon? icon = !string.IsNullOrWhiteSpace(processPath) ? Icon.ExtractAssociatedIcon(processPath) : null;
            return icon ?? SystemIcons.Application;
        }
        catch
        {
            return SystemIcons.Application;
        }
    }

    private void ToggleAutostart()
    {
        if (_autostartService is null || _autostartItem is null) return;
        try
        {
            bool enabled = !_autostartService.IsEnabled;
            _autostartService.SetEnabled(enabled);
            _autostartItem.Checked = enabled;
        }
        catch (Exception ex)
        {
            AppLog.Error("Автозапуск", ex);
            MessageBox.Show($"Не удалось изменить автозапуск.\n\n{ex.Message}", "GeniaText",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenEditor()
    {
        if (_phraseStore is null) return;
        _pickerWindow?.Hide();

        if (_editorWindow is not null)
        {
            if (_editorWindow.WindowState == WindowState.Minimized)
                _editorWindow.WindowState = WindowState.Normal;
            _editorWindow.Activate();
            return;
        }

        if (_settingsStore is null || _paths is null) return;
        _editorWindow = new PhraseEditorWindow(_phraseStore, _settingsStore, _paths);
        _editorWindow.SettingsRequested += (_, _) => OpenSettings();
        _editorWindow.Closed += EditorWindow_Closed;
        _editorWindow.Show();
        _editorWindow.Activate();
    }

    private void EditorWindow_Closed(object? sender, EventArgs e)
    {
        if (_editorWindow is not null)
        {
            _editorWindow.Closed -= EditorWindow_Closed;
            _editorWindow = null;
        }
    }

    private void OpenSettings()
    {
        if (_settingsStore is null || _paths is null) return;
        var window = new SettingsWindow(_settingsStore, _paths, ApplyHotkey, FlushData)
        {
            Owner = (Window?)_editorWindow ?? _pickerWindow
        };
        window.ShowDialog();
        _editorWindow?.RefreshFeatureAvailability();
        UpdateTrayText();
    }

    private bool ApplyHotkey(uint modifiers, uint virtualKey)
    {
        if (_globalHotkey is null) return false;
        bool changed = _globalHotkey.TryChange(modifiers, virtualKey);
        if (changed) UpdateTrayText();
        return changed;
    }

    private void FlushData()
    {
        _phraseStore?.Flush();
        _settingsStore?.Flush();
    }

    private void ExitApplication()
    {
        if (_exiting) return;
        _exiting = true;

        try { _phraseStore?.DiscardEmptyPhrases(); } catch { }
        try { _phraseStore?.Flush(); } catch (Exception ex) { AppLog.Error("Сохранение фраз при выходе", ex); }
        try { _settingsStore?.Flush(); } catch (Exception ex) { AppLog.Error("Сохранение настроек при выходе", ex); }

        if (_pickerWindow is not null)
        {
            _pickerWindow.AllowClose = true;
            _pickerWindow.Close();
        }
        _editorWindow?.Close();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _globalHotkey?.Dispose(); } catch { }
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        try { _singleInstanceMutex?.ReleaseMutex(); } catch { }
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
