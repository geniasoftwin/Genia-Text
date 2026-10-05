namespace GeniaText.Services;

/// <summary>
/// Minimal local diagnostics. Never logs phrase text or clipboard contents.
/// Logging failures are intentionally ignored so diagnostics cannot break the app.
/// </summary>
internal static class AppLog
{
    private const long MaxLogBytes = 1_000_000;
    private static readonly object Gate = new();
    private static string? _logPath;

    public static void Initialize(ApplicationPaths paths)
    {
        // Respect storage mode: portable builds leave diagnostics next to the EXE
        // instead of creating a hidden footprint in %APPDATA%.
        _logPath = Path.Combine(paths.DataDirectory, "geniatext.log");
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string context, Exception exception) =>
        Write("ERROR", $"{context}: {exception.GetType().Name}: {exception.Message}");

    private static void Write(string level, string message)
    {
        try
        {
            string? path = _logPath;
            if (string.IsNullOrWhiteSpace(path)) return;

            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                if (File.Exists(path) && new FileInfo(path).Length > MaxLogBytes)
                    File.Move(path, path + ".old", true);

                File.AppendAllText(path,
                    $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Diagnostics must never become a failure source.
        }
    }
}
