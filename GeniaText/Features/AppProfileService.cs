using System.Diagnostics;

namespace GeniaText.Features;

public sealed class AppProfileService
{
    public string? GetCategoryForWindow(IntPtr windowHandle, string rulesText)
    {
        if (windowHandle == IntPtr.Zero || string.IsNullOrWhiteSpace(rulesText)) return null;

        try
        {
            _ = GetWindowThreadProcessId(windowHandle, out uint processId);
            if (processId == 0) return null;
            using Process process = Process.GetProcessById(checked((int)processId));
            return MatchCategory(process.ProcessName, rulesText);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            _ = ex;
            return null;
        }
    }

    internal static string? MatchCategory(string processName, string rulesText)
    {
        string normalizedProcess = NormalizeProcessName(processName);
        foreach ((string process, string category) in ParseRules(rulesText))
        {
            if (string.Equals(process, normalizedProcess, StringComparison.OrdinalIgnoreCase))
                return category;
        }
        return null;
    }

    internal static IReadOnlyList<(string Process, string Category)> ParseRules(string rulesText)
    {
        var result = new List<(string Process, string Category)>();
        if (string.IsNullOrWhiteSpace(rulesText)) return result;

        foreach (string rawLine in rulesText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            int separator = line.IndexOf('=');
            if (separator <= 0 || separator == line.Length - 1) continue;

            string process = NormalizeProcessName(line[..separator]);
            string category = line[(separator + 1)..].Trim();
            if (process.Length == 0 || category.Length == 0) continue;
            result.Add((process, category));
        }
        return result;
    }

    private static string NormalizeProcessName(string value)
    {
        string process = value.Trim();
        if (process.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            process = process[..^4];
        return process;
    }

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
