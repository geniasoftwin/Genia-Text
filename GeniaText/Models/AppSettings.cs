namespace GeniaText.Models;

public sealed class AppSettings
{
    // MOD_CONTROL = 0x0002, VK_SPACE = 0x20 — defaults recovered from 0.6.2.
    public uint HotkeyModifiers { get; set; } = 0x0002;
    public uint HotkeyVirtualKey { get; set; } = 0x20;

    public double PickerLeft { get; set; } = double.NaN;
    public double PickerTop { get; set; } = double.NaN;
    public double PickerWidth { get; set; } = 680;
    public double PickerHeight { get; set; } = 520;

    /// <summary>
    /// Optional modules. Every flag is off by default so an upgraded installation
    /// keeps the same small, predictable Ctrl+Space -> choose -> Enter/Space flow.
    /// </summary>
    public FeatureSettings Features { get; set; } = new();
}

public sealed class FeatureSettings
{
    public bool EditorProEnabled { get; set; }
    public bool SmartTemplatesEnabled { get; set; }
    public bool AdvancedSearchEnabled { get; set; }
    public bool UsageHistoryEnabled { get; set; }
    public bool AppProfilesEnabled { get; set; }
    public bool CompatibilityModeEnabled { get; set; }
    public bool AdvancedBackupsEnabled { get; set; }
    public bool RecycleBinEnabled { get; set; }
    public bool BackupBrowserEnabled { get; set; }
    public bool ImportPreviewEnabled { get; set; }

    /// <summary>
    /// One rule per line: process=category. Process names may be entered with or
    /// without .exe. Rules are evaluated locally; nothing is sent anywhere.
    /// </summary>
    public string AppProfileRules { get; set; } = string.Empty;
}
