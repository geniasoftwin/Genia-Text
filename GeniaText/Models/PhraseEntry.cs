namespace GeniaText.Models;

public sealed class PhraseEntry : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private string _text = string.Empty;
    private string _category = string.Empty;
    private bool _isFavorite;
    private int _sortOrder;
    private int _useCount;
    private DateTimeOffset? _lastUsedAt;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title
    {
        get => _title;
        set
        {
            value ??= string.Empty;
            if (_title == value) return;
            _title = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayTitle));
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            value ??= string.Empty;
            if (_text == value) return;
            _text = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayTitle));
        }
    }

    public string Category
    {
        get => _category;
        set
        {
            value ??= string.Empty;
            if (_category == value) return;
            _category = value;
            OnPropertyChanged();
        }
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (_isFavorite == value) return;
            _isFavorite = value;
            OnPropertyChanged();
        }
    }

    public int SortOrder
    {
        get => _sortOrder;
        set
        {
            if (_sortOrder == value) return;
            _sortOrder = value;
            OnPropertyChanged();
        }
    }

    public int UseCount
    {
        get => _useCount;
        set
        {
            if (_useCount == value) return;
            _useCount = value;
            OnPropertyChanged();
        }
    }

    public DateTimeOffset? LastUsedAt
    {
        get => _lastUsedAt;
        set
        {
            if (_lastUsedAt == value) return;
            _lastUsedAt = value;
            OnPropertyChanged();
        }
    }

    public string DisplayTitle
    {
        get
        {
            string title = Title.Trim();
            if (!string.IsNullOrWhiteSpace(title))
                return title;

            string preview = Text.Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (preview.Length == 0)
                return "Без текста";

            return preview.Length <= 48 ? preview : preview[..48] + "…";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
