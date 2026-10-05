namespace GeniaText.Services;

internal static class AtomicFile
{
    public static void WriteAllText(string path, string contents)
    {
        string directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Не удалось определить каталог файла.");
        Directory.CreateDirectory(directory);

        string tempPath = path + ".tmp";
        try
        {
            File.WriteAllText(tempPath, contents);
            File.Move(tempPath, path, true);
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    public static void CopyReplace(string sourcePath, string destinationPath)
    {
        string directory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException("Не удалось определить каталог назначения.");
        Directory.CreateDirectory(directory);

        string tempPath = destinationPath + ".copying";
        try
        {
            File.Copy(sourcePath, tempPath, true);
            File.Move(tempPath, destinationPath, true);
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
}
