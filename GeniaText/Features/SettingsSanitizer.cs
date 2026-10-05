using GeniaText.Models;

namespace GeniaText.Features;

internal static class SettingsSanitizer
{
    internal const int MaxAppProfileRulesLength = 32_768;

    internal static void Sanitize(AppSettings settings)
    {
        settings.Features ??= new FeatureSettings();
        settings.Features.AppProfileRules ??= string.Empty;
        if (settings.Features.AppProfileRules.Length > MaxAppProfileRulesLength)
            settings.Features.AppProfileRules = settings.Features.AppProfileRules[..MaxAppProfileRulesLength];

        if (settings.HotkeyModifiers == 0) settings.HotkeyModifiers = 0x0002;
        if (settings.HotkeyVirtualKey == 0) settings.HotkeyVirtualKey = 0x20;
        if (!double.IsFinite(settings.PickerWidth) || settings.PickerWidth < 420 || settings.PickerWidth > 2000)
            settings.PickerWidth = 680;
        if (!double.IsFinite(settings.PickerHeight) || settings.PickerHeight < 320 || settings.PickerHeight > 1600)
            settings.PickerHeight = 520;
        if (!double.IsFinite(settings.PickerLeft)) settings.PickerLeft = double.NaN;
        if (!double.IsFinite(settings.PickerTop)) settings.PickerTop = double.NaN;
    }
}
