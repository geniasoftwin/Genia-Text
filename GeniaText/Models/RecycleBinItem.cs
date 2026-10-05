namespace GeniaText.Models;

public sealed class RecycleBinItem
{
    public Guid TrashId { get; set; } = Guid.NewGuid();
    public DateTimeOffset DeletedAt { get; set; } = DateTimeOffset.Now;
    public PhraseEntry Phrase { get; set; } = new();

    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayTitle => Phrase.DisplayTitle;

    [System.Text.Json.Serialization.JsonIgnore]
    public string Category => Phrase.Category;
}
