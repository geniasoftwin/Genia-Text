using GeniaText.Features;

namespace GeniaText;

public enum ImportApplyMode
{
    Append,
    Replace
}

public partial class ImportPreviewWindow : Window
{
    public ImportPreviewWindow(ImportPreviewResult preview)
    {
        InitializeComponent();
        PreviewList.ItemsSource = preview.Items;
        SummaryText.Text =
            $"Всего в файле: {preview.SourceCount:N0}    Новых: {preview.NewCount:N0}    " +
            $"Точных повторов: {preview.DuplicateCount:N0}    Конфликтов ID: {preview.IdConflictCount:N0}" +
            (preview.SkippedEmptyCount > 0 ? $"    Пустых пропущено: {preview.SkippedEmptyCount:N0}" : string.Empty);
    }

    public ImportApplyMode? SelectedMode { get; private set; }

    private void Append_Click(object sender, RoutedEventArgs e)
    {
        SelectedMode = ImportApplyMode.Append;
        DialogResult = true;
    }

    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult confirmation = MessageBox.Show(
            "Заменить все текущие фразы содержимым проверенного файла?\n\nПеред изменением GeniaText создаст резервную копию.",
            "Импорт GeniaText", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmation != MessageBoxResult.Yes) return;
        SelectedMode = ImportApplyMode.Replace;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
