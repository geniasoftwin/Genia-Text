using GeniaText.Features;
using GeniaText.Models;
using GeniaText.Services;

namespace GeniaText;

public partial class EditorProWindow : Window
{
    private readonly PhraseStore _phraseStore;
    private readonly EditorProService _editorPro = new();

    public EditorProWindow(PhraseStore phraseStore)
    {
        InitializeComponent();
        _phraseStore = phraseStore;
        PhraseList.ItemsSource = _phraseStore.Phrases;
        UpdateSelectionState();
    }

    private IReadOnlyList<PhraseEntry> SelectedPhrases =>
        PhraseList.SelectedItems.Cast<PhraseEntry>().ToList();

    private void PhraseList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateSelectionState();

    private void UpdateSelectionState()
    {
        int count = PhraseList.SelectedItems.Count;
        SelectionSummaryText.Text = count == 0 ? "Фразы не выбраны" : $"Выбрано фраз: {count}";
        ApplyCategoryButton.IsEnabled = count > 0;
        FavoriteButton.IsEnabled = count > 0;
        UnfavoriteButton.IsEnabled = count > 0;
    }

    private void ApplyCategory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int changed = _editorPro.ApplyCategory(SelectedPhrases, CategoryBox.Text);
            if (changed > 0) _phraseStore.ScheduleSave();
            ActionStatusText.Text = changed == 0
                ? "Категория уже установлена у выбранных фраз."
                : $"Категория изменена у фраз: {changed}.";
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "GeniaText", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void AddFavorite_Click(object sender, RoutedEventArgs e) => SetFavorite(true);

    private void RemoveFavorite_Click(object sender, RoutedEventArgs e) => SetFavorite(false);

    private void SetFavorite(bool isFavorite)
    {
        int changed = _editorPro.SetFavorite(SelectedPhrases, isFavorite);
        if (changed > 0) _phraseStore.ScheduleSave();
        ActionStatusText.Text = changed == 0
            ? "Состояние избранного уже совпадает."
            : isFavorite
                ? $"Добавлено в избранное: {changed}."
                : $"Удалено из избранного: {changed}.";
    }

    private void FindDuplicates_Click(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<DuplicatePhraseGroup> groups = _editorPro.FindDuplicateGroups(_phraseStore.Phrases);
        PhraseList.SelectedItems.Clear();

        List<PhraseEntry> duplicates = groups
            .SelectMany(group => group.Phrases)
            .Distinct()
            .ToList();
        foreach (PhraseEntry phrase in duplicates)
            PhraseList.SelectedItems.Add(phrase);

        if (duplicates.Count > 0)
            PhraseList.ScrollIntoView(duplicates[0]);

        DuplicateSummaryText.Text = groups.Count == 0
            ? "Повторяющиеся фразы не найдены."
            : $"Найдено групп: {groups.Count}; выделено фраз: {duplicates.Count}. Ничего не удалено.";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
