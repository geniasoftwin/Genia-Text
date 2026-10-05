namespace GeniaText.Services;

public sealed class ApplicationPaths
{
    private readonly bool _startupPortableMode;

    public ApplicationPaths()
    {
        string? processPath = Environment.ProcessPath;
        ExecutableDirectory = !string.IsNullOrWhiteSpace(processPath)
            ? Path.GetDirectoryName(processPath) ?? AppContext.BaseDirectory
            : AppContext.BaseDirectory;

        StandardDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GeniaText");

        PortableFlagPath = Path.Combine(ExecutableDirectory, "GeniaText.portable");
        _startupPortableMode = File.Exists(PortableFlagPath);

        // A true portable start must not create anything in %APPDATA%.
        // In standard mode DataDirectory is StandardDataDirectory; in portable
        // mode the executable directory already exists and will hold all data.
        if (!IsPortable)
            Directory.CreateDirectory(StandardDataDirectory);
    }

    public string ExecutableDirectory { get; }
    public string StandardDataDirectory { get; }
    public string PortableFlagPath { get; }

    /// <summary>The storage mode used by this running process.</summary>
    public bool IsPortable => _startupPortableMode;

    /// <summary>The mode configured for the next process start.</summary>
    public bool ConfiguredPortableMode => File.Exists(PortableFlagPath);

    public string DataDirectory => IsPortable ? ExecutableDirectory : StandardDataDirectory;
    public string PhraseFilePath => Path.Combine(DataDirectory, "phrases.json");
    public string SettingsFilePath => Path.Combine(DataDirectory, "settings.json");
    public string BackupDirectory => Path.Combine(DataDirectory, "Backups");

    /// <summary>
    /// Configures storage mode for the next process start. Current data is copied first;
    /// only after successful copies is the marker changed, avoiding half-switched state.
    /// The running process keeps using its startup data directory until restart.
    /// </summary>
    public void PreparePortableMode(bool portableMode)
    {
        if (portableMode == ConfiguredPortableMode) return;

        string sourceDirectory = DataDirectory;
        string targetDirectory = portableMode ? ExecutableDirectory : StandardDataDirectory;
        Directory.CreateDirectory(targetDirectory);

        ValidateWritable(targetDirectory);
        CopyIfExists(Path.Combine(sourceDirectory, "phrases.json"), Path.Combine(targetDirectory, "phrases.json"));
        CopyIfExists(Path.Combine(sourceDirectory, "settings.json"), Path.Combine(targetDirectory, "settings.json"));

        if (portableMode)
            AtomicFile.WriteAllText(PortableFlagPath, "GeniaText portable mode");
        else if (File.Exists(PortableFlagPath))
            File.Delete(PortableFlagPath);
    }

    /// <summary>
    /// While a mode switch is pending until restart, mirror newly saved data to the
    /// configured target so an unexpected exit cannot leave the target stale.
    /// </summary>
    public void MirrorIfModeSwitchPending(string sourcePath, string fileName)
    {
        if (ConfiguredPortableMode == IsPortable || !File.Exists(sourcePath)) return;
        string targetDirectory = ConfiguredPortableMode ? ExecutableDirectory : StandardDataDirectory;
        string targetPath = Path.Combine(targetDirectory, fileName);
        if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
            return;
        AtomicFile.CopyReplace(sourcePath, targetPath);
    }

    private static void ValidateWritable(string directory)
    {
        string probe = Path.Combine(directory, $".geniatext-write-{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(probe, string.Empty);
        }
        finally
        {
            try { if (File.Exists(probe)) File.Delete(probe); } catch { }
        }
    }

    private static void CopyIfExists(string sourcePath, string targetPath)
    {
        if (!File.Exists(sourcePath)) return;
        AtomicFile.CopyReplace(sourcePath, targetPath);
    }
}
