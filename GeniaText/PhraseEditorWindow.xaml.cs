using GeniaText.Features;
using GeniaText.Models;
using GeniaText.Services;
using Microsoft.Win32;
using System.Windows.Controls.Primitives;

namespace GeniaText;

public partial class PhraseEditorWindow : Window
{
    private readonly PhraseStore _phraseStore;
    private readonly AppSettingsStore _settingsStore;
    private readonly ApplicationPaths _paths;
    private RecycleBinService? _recycleBinService;
    private BackupBrowserService? _backupBrowserService;
    private ImportPreviewService? _importPreviewService;
    private WpfPoint _dragStartPoint;
    private PhraseEntry? _draggedPhrase;

    public PhraseEditorWindow(PhraseStore phraseStore, AppSettingsStore settingsStore, ApplicationPaths paths)
    {
        InitializeComponent();
        _phraseStore = phraseStore;
        _settingsStore = settingsStore;
        _paths = paths;
        PhrasesList.ItemsSource = _phraseStore.Phrases;
        PhrasesList.SelectedIndex = _phraseStore.Phrases.Count > 0 ? 0 : -1;
        RefreshFeatureAvailability();
        UpdateEditorState();
    }

    public event EventHandler? SettingsRequested;

    public void RefreshFeatureAvailability()
    {
        FeatureSettings features = _settingsStore.Current.Features;
        EditorProButton.Visibility = features.EditorProEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;
        RecycleBinButton.Visibility = features.RecycleBinEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;
        BackupBrowserButton.Visibility = features.BackupBrowserEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;

        RefreshRecycleBinButtonState(features.RecycleBinEnabled);
    }

    private void EditorPro_Click(object sender, RoutedEventArgs e)
    {
        if (!_settingsStore.Current.Features.EditorProEnabled) return;
        var window = new EditorProWindow(_phraseStore) { Owner = this };
        window.ShowDialog();
        UpdateEditorState();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var phrase = new PhraseEntry { SortOrder = _phraseStore.Phrases.Count };
        _phraseStore.Phrases.Add(phrase);
        PhrasesList.SelectedItem = phrase;
        PhrasesList.ScrollIntoView(phrase);
        _phraseStore.ScheduleSave();
    }

    private void Duplicate_Click(object sender, RoutedEventArgs e)
    {
        if (PhrasesList.SelectedItem is not PhraseEntry selected) return;
        PhraseEntry copy = _phraseStore.Duplicate(selected);
        PhrasesList.SelectedItem = copy;
        PhrasesList.ScrollIntoView(copy);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (PhrasesList.SelectedItem is not PhraseEntry selected) return;
        bool useRecycleBin = _settingsStore.Current.Features.RecycleBinEnabled;
        MessageBoxResult result = MessageBox.Show(
            useRecycleBin
                ? $"Переместить фразу «{selected.DisplayTitle}» в корзину?"
                : $"Удалить фразу «{selected.DisplayTitle}»?",
            "GeniaText", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        if (useRecycleBin)
        {
            try
            {
                RecycleBin.Archive(selected);
            }
            catch (Exception ex) when (IsExpectedModuleException(ex))
            {
                AppLog.Error("Перемещение фразы в корзину", ex);
                MessageBox.Show(
                    $"Фраза не удалена: сохранить её в корзине не удалось.\n\n{ex.Message}",
                    "GeniaText", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        int index = PhrasesList.SelectedIndex;
        _phraseStore.Phrases.Remove(selected);
        if (_phraseStore.Phrases.Count > 0)
            PhrasesList.SelectedIndex = Math.Min(index, _phraseStore.Phrases.Count - 1);
        _phraseStore.ScheduleSave();
        if (useRecycleBin)
            RefreshRecycleBinButtonState(enabled: true);
    }

    private void Save_Click(object sender, RoutedEventArgs e) => SavePhrases(showSuccess: false);

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Импорт фраз GeniaText",
            Filter = "Фразы GeniaText (*.json)|*.json|Все файлы (*.*)|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            bool append;
            if (_settingsStore.Current.Features.ImportPreviewEnabled)
            {
                ImportPreviewResult preview = ImportPreview.Load(dialog.FileName, _phraseStore.Phrases);
                var previewWindow = new ImportPreviewWindow(preview) { Owner = this };
                if (previewWindow.ShowDialog() != true || previewWindow.SelectedMode is null) return;
                append = previewWindow.SelectedMode == ImportApplyMode.Append;
            }
            else
            {
                MessageBoxResult choice = MessageBox.Show(
                    "Добавить импортируемые фразы к текущим?\n\nДа — добавить\nНет — заменить текущие\nОтмена — ничего не менять",
                    "Импорт GeniaText", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (choice == MessageBoxResult.Cancel) return;
                append = choice == MessageBoxResult.Yes;
            }

            int count = _phraseStore.Import(dialog.FileName, append);
            PhrasesList.SelectedIndex = _phraseStore.Phrases.Count > 0 ? 0 : -1;
            MessageBox.Show($"Импортировано фраз: {count}.", "GeniaText",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or JsonException)
        {
            AppLog.Error("Импорт фраз", ex);
            MessageBox.Show($"Не удалось импортировать фразы.\n\n{ex.Message}", "GeniaText",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RecycleBin_Click(object sender, RoutedEventArgs e) =>
        OpenRecovery(RecoverySection.Trash);

    private void BackupBrowser_Click(object sender, RoutedEventArgs e) =>
        OpenRecovery(RecoverySection.Backups);

    private void OpenRecovery(RecoverySection initialSection)
    {
        FeatureSettings features = _settingsStore.Current.Features;
        if (initialSection == RecoverySection.Trash &&
            (!features.RecycleBinEnabled || !RecycleBinButton.IsEnabled))
            return;
        if (initialSection == RecoverySection.Backups && !features.BackupBrowserEnabled)
            return;

        try
        {
            var window = new RecoveryCenterWindow(
                _phraseStore,
                RecycleBin,
                BackupBrowser,
                features.RecycleBinEnabled,
                features.BackupBrowserEnabled,
                initialSection)
            {
                Owner = this
            };
            window.ShowDialog();
            if (window.DataChanged)
            {
                PhrasesList.SelectedIndex = _phraseStore.Phrases.Count > 0 ? 0 : -1;
                UpdateEditorState();
            }
        }
        catch (Exception ex) when (IsRecoverableModuleException(ex))
        {
            // Recovery is optional. A constructor/show failure must be contained here
            // instead of reaching App.DispatcherUnhandledException and closing GeniaText.
            AppLog.Error("Открытие центра восстановления", ex);
            MessageBox.Show(
                $"Инструменты восстановления не удалось открыть. GeniaText продолжит работу.\n\n{ex.Message}",
                "GeniaText", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            RefreshRecycleBinButtonState(features.RecycleBinEnabled);
        }
    }

    private void RefreshRecycleBinButtonState(bool enabled)
    {
        if (!enabled)
        {
            RecycleBinButton.IsEnabled = false;
            RecycleBinButton.ToolTip = null;
            return;
        }

        try
        {
            RecycleBin.Load();
            int count = RecycleBin.Items.Count;
            RecycleBinButton.IsEnabled = count > 0;
            RecycleBinButton.ToolTip = count > 0
                ? $"Удалённых фраз: {count:N0}"
                : "Корзина пуста";
        }
        catch (Exception ex) when (IsExpectedModuleException(ex))
        {
            // Unknown is not the same as empty: keep the entry point available
            // so Recovery can show the detailed, fail-closed load error.
            RecycleBinButton.IsEnabled = true;
            RecycleBinButton.ToolTip = "Не удалось проверить корзину. Откройте для подробностей.";
            AppLog.Error("Проверка корзины в редакторе", ex);
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Экспорт фраз GeniaText",
            Filter = "Фразы GeniaText (*.json)|*.json",
            FileName = $"GeniaText-phrases-{DateTime.Now:yyyy-MM-dd}.json",
            AddExtension = true,
            DefaultExt = ".json"
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            _phraseStore.Export(dialog.FileName);
            MessageBox.Show("Фразы экспортированы.", "GeniaText", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Error("Экспорт фраз", ex);
            MessageBox.Show($"Не удалось экспортировать фразы.\n\n{ex.Message}", "GeniaText",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        AppLog.Info("Editor: Settings button activated");
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void PhrasesList_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateEditorState();

    private void UpdateEditorState()
    {
        bool hasSelection = PhrasesList.SelectedItem is PhraseEntry;
        EditorPanel.IsEnabled = hasSelection;
        MoveUpButton.IsEnabled = hasSelection && PhrasesList.SelectedIndex > 0;
        MoveDownButton.IsEnabled = hasSelection && PhrasesList.SelectedIndex >= 0 &&
                                   PhrasesList.SelectedIndex < _phraseStore.Phrases.Count - 1;
    }

    private void SavePhrases(bool showSuccess)
    {
        try
        {
            CommitSingleLineEditorFields();
            _phraseStore.DiscardEmptyPhrases();
            _phraseStore.Flush();
            if (showSuccess)
                MessageBox.Show("Фразы сохранены.", "GeniaText", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Error("Сохранение фраз", ex);
            MessageBox.Show($"Не удалось сохранить фразы.\n\n{ex.Message}", "GeniaText",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CommitSingleLineEditorFields()
    {
        // Title/category are trimmed by the frozen store during save. Updating
        // on focus loss avoids a timed save rewriting an actively edited field
        // after a trailing Space and moving its caret back to the beginning.
        TitleBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        CategoryBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    private void PhrasesList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CommitSingleLineEditorFields();
        _dragStartPoint = e.GetPosition(null);
        _draggedPhrase = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject)?.DataContext as PhraseEntry;
    }

    private void PhrasesList_MouseMove(object sender, WpfMouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedPhrase is null) return;
        WpfPoint position = e.GetPosition(null);
        if (Math.Abs(position.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        DragDrop.DoDragDrop(PhrasesList, _draggedPhrase, DragDropEffects.Move);
    }

    private void PhrasesList_DragOver(object sender, WpfDragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(PhraseEntry)))
        {
            e.Effects = DragDropEffects.None;
            return;
        }
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
        AutoScrollDuringDrag(e.GetPosition(PhrasesList));
    }

    private void AutoScrollDuringDrag(WpfPoint position)
    {
        ScrollViewer? viewer = FindDescendant<ScrollViewer>(PhrasesList);
        if (viewer is null) return;
        const double scrollMargin = 34;
        if (position.Y < scrollMargin)
            viewer.LineUp();
        else if (position.Y > PhrasesList.ActualHeight - scrollMargin)
            viewer.LineDown();
    }

    private void PhrasesList_Drop(object sender, WpfDragEventArgs e)
    {
        if (e.Data.GetData(typeof(PhraseEntry)) is not PhraseEntry draggedPhrase) return;
        int oldIndex = _phraseStore.Phrases.IndexOf(draggedPhrase);
        int targetIndex = GetDropIndex(e.GetPosition(PhrasesList));
        if (targetIndex > oldIndex) targetIndex--;
        targetIndex = Math.Clamp(targetIndex, 0, Math.Max(0, _phraseStore.Phrases.Count - 1));
        _phraseStore.Move(draggedPhrase, targetIndex);
        PhrasesList.SelectedItem = draggedPhrase;
        e.Handled = true;
    }

    private int GetDropIndex(WpfPoint position)
    {
        for (int index = 0; index < PhrasesList.Items.Count; index++)
        {
            if (PhrasesList.ItemContainerGenerator.ContainerFromIndex(index) is not ListBoxItem container)
                continue;
            WpfPoint relative = PhrasesList.TranslatePoint(position, container);
            if (relative.Y < container.ActualHeight / 2)
                return index;
        }
        return PhrasesList.Items.Count;
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e) => MoveSelectedPhrase(-1);
    private void MoveDown_Click(object sender, RoutedEventArgs e) => MoveSelectedPhrase(1);

    private void MoveSelectedPhrase(int direction)
    {
        if (PhrasesList.SelectedItem is not PhraseEntry phrase) return;
        int currentPosition = _phraseStore.Phrases.IndexOf(phrase);
        int target = currentPosition + direction;
        if (target < 0 || target >= _phraseStore.Phrases.Count) return;
        _phraseStore.Move(phrase, target);
        PhrasesList.SelectedItem = phrase;
        PhrasesList.ScrollIntoView(phrase);
        UpdateEditorState();
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T typed) return typed;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed) return typed;
            T? descendant = FindDescendant<T>(child);
            if (descendant is not null) return descendant;
        }
        return null;
    }

    private RecycleBinService RecycleBin => _recycleBinService ??= new RecycleBinService(_paths);
    private BackupBrowserService BackupBrowser => _backupBrowserService ??= new BackupBrowserService(_paths);
    private ImportPreviewService ImportPreview => _importPreviewService ??= new ImportPreviewService();

    private static bool IsExpectedModuleException(Exception ex) =>
        ex is InvalidDataException or IOException or UnauthorizedAccessException or JsonException;

    private static bool IsRecoverableModuleException(Exception ex) =>
        ex is not OutOfMemoryException and not StackOverflowException and not AccessViolationException;

    protected override void OnClosed(EventArgs e)
    {
        try { SavePhrases(showSuccess: false); } catch { }
        base.OnClosed(e);
    }
}
