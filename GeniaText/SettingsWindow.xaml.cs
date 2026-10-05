using GeniaText.Models;
using GeniaText.Services;
using GeniaText.Features;

namespace GeniaText;

public partial class SettingsWindow : Window
{
    private readonly AppSettingsStore _settingsStore;
    private readonly ApplicationPaths _paths;
    private readonly Func<uint, uint, bool> _applyHotkey;
    private readonly Action _flushData;
    private readonly List<HotkeyOption> _modifierOptions;
    private readonly List<HotkeyOption> _keyOptions;

    public SettingsWindow(
        AppSettingsStore settingsStore,
        ApplicationPaths paths,
        Func<uint, uint, bool> applyHotkey,
        Action flushData)
    {
        InitializeComponent();
        _settingsStore = settingsStore;
        _paths = paths;
        _applyHotkey = applyHotkey;
        _flushData = flushData;

        _modifierOptions =
        [
            new("Ctrl", 0x0002),
            new("Ctrl+Alt", 0x0003),
            new("Ctrl+Shift", 0x0006),
            new("Alt", 0x0001),
            new("Alt+Shift", 0x0005)
        ];

        _keyOptions = [new("Space", 0x20)];
        for (uint vk = 0x70; vk <= 0x7B; vk++)
            _keyOptions.Add(new($"F{vk - 0x6F}", vk));
        _keyOptions.Add(new("Insert", 0x2D));

        AppSettings settings = _settingsStore.Current;
        EnsureCurrentOption(_modifierOptions, settings.HotkeyModifiers,
            GlobalHotkey.Format(settings.HotkeyModifiers, settings.HotkeyVirtualKey).Split('+')[0]);
        EnsureCurrentOption(_keyOptions, settings.HotkeyVirtualKey, $"VK 0x{settings.HotkeyVirtualKey:X2}");

        ModifierBox.ItemsSource = _modifierOptions;
        KeyBox.ItemsSource = _keyOptions;
        ModifierBox.SelectedItem = _modifierOptions.First(x => x.Value == settings.HotkeyModifiers);
        KeyBox.SelectedItem = _keyOptions.First(x => x.Value == settings.HotkeyVirtualKey);
        PortableModeCheckBox.IsChecked = _paths.ConfiguredPortableMode;

        FeatureSettings features = settings.Features;
        EditorProCheckBox.IsChecked = features.EditorProEnabled;
        SmartTemplatesCheckBox.IsChecked = features.SmartTemplatesEnabled;
        AdvancedSearchCheckBox.IsChecked = features.AdvancedSearchEnabled;
        UsageHistoryCheckBox.IsChecked = features.UsageHistoryEnabled;
        AppProfilesCheckBox.IsChecked = features.AppProfilesEnabled;
        AppProfileRulesBox.Text = features.AppProfileRules;
        CompatibilityModeCheckBox.IsChecked = features.CompatibilityModeEnabled;
        AdvancedBackupsCheckBox.IsChecked = features.AdvancedBackupsEnabled;
        RecycleBinCheckBox.IsChecked = features.RecycleBinEnabled;
        BackupBrowserCheckBox.IsChecked = features.BackupBrowserEnabled;
        ImportPreviewCheckBox.IsChecked = features.ImportPreviewEnabled;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (ModifierBox.SelectedItem is not HotkeyOption modifier || KeyBox.SelectedItem is not HotkeyOption key)
            return;

        AppSettings settings = _settingsStore.Current;
        uint oldModifiers = settings.HotkeyModifiers;
        uint oldVirtualKey = settings.HotkeyVirtualKey;

        if (!_applyHotkey(modifier.Value, key.Value))
        {
            MessageBox.Show(
                $"Не удалось зарегистрировать {GlobalHotkey.Format(modifier.Value, key.Value)}. Скорее всего, комбинация уже занята другой программой.",
                "GeniaText", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        settings.HotkeyModifiers = modifier.Value;
        settings.HotkeyVirtualKey = key.Value;

        FeatureSettings features = settings.Features;
        features.EditorProEnabled = EditorProCheckBox.IsChecked == true;
        features.SmartTemplatesEnabled = SmartTemplatesCheckBox.IsChecked == true;
        features.AdvancedSearchEnabled = AdvancedSearchCheckBox.IsChecked == true;
        features.UsageHistoryEnabled = UsageHistoryCheckBox.IsChecked == true;
        features.AppProfilesEnabled = AppProfilesCheckBox.IsChecked == true;
        string rules = AppProfileRulesBox.Text ?? string.Empty;
        features.AppProfileRules = rules.Length <= SettingsSanitizer.MaxAppProfileRulesLength
            ? rules
            : rules[..SettingsSanitizer.MaxAppProfileRulesLength];
        features.CompatibilityModeEnabled = CompatibilityModeCheckBox.IsChecked == true;
        features.AdvancedBackupsEnabled = AdvancedBackupsCheckBox.IsChecked == true;
        features.RecycleBinEnabled = RecycleBinCheckBox.IsChecked == true;
        features.BackupBrowserEnabled = BackupBrowserCheckBox.IsChecked == true;
        features.ImportPreviewEnabled = ImportPreviewCheckBox.IsChecked == true;

        try
        {
            _flushData();
            bool portableMode = PortableModeCheckBox.IsChecked == true;
            _paths.PreparePortableMode(portableMode);
            DialogResult = true;
            Close();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Error("Сохранение настроек/portable mode", ex);
            MessageBox.Show(
                $"Настройки сохранены в памяти, но режим хранения изменить не удалось.\n\n{ex.Message}\n\nЕсли GeniaText находится в Program Files, перенесите его в папку, куда у вас есть право записи.",
                "GeniaText", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch
        {
            _applyHotkey(oldModifiers, oldVirtualKey);
            settings.HotkeyModifiers = oldModifiers;
            settings.HotkeyVirtualKey = oldVirtualKey;
            throw;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static void EnsureCurrentOption(List<HotkeyOption> options, uint value, string label)
    {
        if (options.All(option => option.Value != value))
            options.Add(new HotkeyOption(label, value));
    }
}

internal sealed record HotkeyOption(string Label, uint Value);
