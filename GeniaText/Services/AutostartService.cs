using Microsoft.Win32;

namespace GeniaText.Services;

public sealed class AutostartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "GeniaText";
    private const string LegacyValueName = "QuickPhrase";

    public bool IsEnabled
    {
        get
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) is string value &&
                   string.Equals(value, GetLaunchCommand(), StringComparison.OrdinalIgnoreCase);
        }
    }

    public void MigrateLegacyRegistration()
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (key.GetValue(ValueName) is null && key.GetValue(LegacyValueName) is string)
            key.SetValue(ValueName, GetLaunchCommand(), RegistryValueKind.String);

        key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
    }

    public void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (enabled)
            key.SetValue(ValueName, GetLaunchCommand(), RegistryValueKind.String);
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);

        key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
    }

    private static string GetLaunchCommand()
    {
        string? executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
            throw new InvalidOperationException("Не удалось определить путь к GeniaText.");

        // Always quote the executable path: protects paths containing spaces and avoids argument ambiguity.
        return $"\"{executablePath}\"";
    }
}
