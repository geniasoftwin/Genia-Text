namespace GeniaText.Features;

public sealed class SmartTemplateService
{
    public string Expand(string text) => Expand(text, DateTimeOffset.Now);

    internal string Expand(string text, DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Intentionally small and deterministic for the first modular release.
        // Unknown tokens are left untouched so existing phrase text is never lost.
        return text
            .Replace("{date}", now.ToString("dd.MM.yyyy"), StringComparison.OrdinalIgnoreCase)
            .Replace("{time}", now.ToString("HH:mm"), StringComparison.OrdinalIgnoreCase)
            .Replace("{datetime}", now.ToString("dd.MM.yyyy HH:mm"), StringComparison.OrdinalIgnoreCase);
    }
}
