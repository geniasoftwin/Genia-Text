using GeniaText.Features;
using GeniaText.Models;
using GeniaText.Services;

namespace GeniaText;

public enum RecoverySection
{
    Trash,
    Backups
}

public partial class RecoveryCenterWindow : Window
{
    private readonly PhraseStore _phraseStore;
    private readonly RecycleBinService _recycleBin;
    private readonly BackupBrowserService _backupBrowser;
    private readonly SafetyPackService _safetyPack = new();
    private bool _viewReady;
    private Task? _backupsLoadTask;
    private CancellationTokenSource? _backupPreviewCts;
    private bool _closing;

    public RecoveryCenterWindow(
        PhraseStore phraseStore,
        RecycleBinService recycleBin,
        BackupBrowserService backupBrowser,
        bool recycleBinEnabled,
        bool backupBrowserEnabled,
        RecoverySection initialSection)
    {
        _phraseStore = phraseStore ?? throw new ArgumentNullException(nameof(phraseStore));
        _recycleBin = recycleBin ?? throw new ArgumentNullException(nameof(recycleBin));
        _backupBrowser = backupBrowser ?? throw new ArgumentNullException(nameof(backupBrowser));
        InitializeComponent();
        _viewReady = true;

        TrashTab.Visibility = recycleBinEnabled ? Visibility.Visible : Visibility.Collapsed;
        BackupsTab.Visibility = backupBrowserEnabled ? Visibility.Visible : Visibility.Collapsed;

        if (recycleBinEnabled)
            LoadTrash();
        RecoveryTabs.SelectedItem = initialSection == RecoverySection.Backups && backupBrowserEnabled
            ? BackupsTab
            : recycleBinEnabled
                ? TrashTab
                : BackupsTab;

        // Do not enumerate/preview backups from inside the constructor. WPF can raise
        // nested SelectionChanged events while the visual tree is still being created.
        // Deferring the first backup load until Loaded keeps the optional recovery module
        // isolated from application startup/editor lifetime even if a snapshot is damaged.
        Loaded += RecoveryCenterWindow_Loaded;
    }

    public bool DataChanged { get; private set; }

    private void LoadTrash()
    {
        try
        {
            _recycleBin.Load();
            TrashList.ItemsSource = _recycleBin.Items;
            RefreshTrashState();
        }
        catch (Exception ex) when (IsRecoverableRecoveryException(ex))
        {
            TrashTab.IsEnabled = false;
            ShowFailure("Корзину не удалось открыть", ex);
        }
    }

    private void RefreshTrashState()
    {
        TrashCountText.Text = $"Удалённых фраз: {_recycleBin.Items.Count:N0}";
        if (TrashList.SelectedIndex < 0 && _recycleBin.Items.Count > 0)
            TrashList.SelectedIndex = 0;
    }

    private async Task LoadBackupsAsync()
    {
        BackupCountText.Text = "Загрузка списка…";
        SnapshotSummaryText.Text = "Выберите резервную копию после загрузки списка";
        BackupList.IsEnabled = false;
        DeleteBackupButton.IsEnabled = false;
        BackupList.ItemsSource = null;
        BackupPhraseList.ItemsSource = null;

        try
        {
            // Directory enumeration and FileInfo metadata access can block on slow/removable
            // storage. Keep all of it off the WPF dispatcher so the recovery window remains
            // responsive even when the Backups folder is large or temporarily slow.
            IReadOnlyList<BackupSnapshotInfo> snapshots = await Task.Run(_backupBrowser.ListSnapshots);
            if (_closing) return;

            BackupList.ItemsSource = snapshots;
            BackupCountText.Text = $"Резервных копий: {snapshots.Count:N0}";
            BackupList.SelectedIndex = -1;
            SnapshotSummaryText.Text = snapshots.Count == 0
                ? "Резервных копий пока нет"
                : "Выберите резервную копию";
        }
        catch (Exception ex) when (IsRecoverableRecoveryException(ex))
        {
            if (_closing) return;
            BackupsTab.IsEnabled = false;
            BackupCountText.Text = "Список недоступен";
            SnapshotSummaryText.Text = "Резервные копии не удалось открыть";
            ShowFailure("Список резервных копий не удалось открыть", ex);
        }
        finally
        {
            if (!_closing && BackupsTab.IsEnabled)
                BackupList.IsEnabled = true;
        }
    }

    private async void RecoveryCenterWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= RecoveryCenterWindow_Loaded;
        if (ReferenceEquals(RecoveryTabs.SelectedItem, BackupsTab))
            await EnsureBackupsLoadedAsync();
    }

    private async void RecoveryTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged also bubbles from the ListBoxes inside each tab.
        // Ignore all tab activity until the window has completed its first load.
        if (!_viewReady || !IsLoaded || !ReferenceEquals(e.Source, RecoveryTabs)) return;
        if (ReferenceEquals(RecoveryTabs.SelectedItem, BackupsTab))
            await EnsureBackupsLoadedAsync();
    }

    private Task EnsureBackupsLoadedAsync() =>
        _backupsLoadTask ??= LoadBackupsAsync();

    private async void BackupList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_viewReady || _closing) return;

        _backupPreviewCts?.Cancel();
        _backupPreviewCts = null;

        if (BackupList.SelectedItem is not BackupSnapshotInfo snapshot)
        {
            DeleteBackupButton.IsEnabled = false;
            BackupPhraseList.ItemsSource = null;
            SnapshotSummaryText.Text = "Выберите резервную копию";
            return;
        }

        DeleteBackupButton.IsEnabled = true;

        var cts = new CancellationTokenSource();
        _backupPreviewCts = cts;
        BackupPhraseList.ItemsSource = null;
        BackupPhraseList.SelectedIndex = -1;
        SnapshotSummaryText.Text = "Загрузка копии…";

        try
        {
            // JSON reading, deserialization and phrase validation are intentionally kept
            // outside the dispatcher. alpha.10 did this synchronously and could white-out
            // the entire window while the first snapshot was being parsed.
            IReadOnlyList<PhraseEntry> phrases = await Task.Run(
                () => _backupBrowser.LoadPhrases(snapshot, cts.Token), cts.Token);

            if (_closing || cts.IsCancellationRequested ||
                !ReferenceEquals(BackupList.SelectedItem, snapshot))
                return;

            BackupPhraseList.ItemsSource = phrases;
            BackupPhraseList.SelectedIndex = phrases.Count > 0 ? 0 : -1;
            SnapshotSummaryText.Text = $"Фраз в копии: {phrases.Count:N0}";
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // A newer selection or window close superseded this preview.
        }
        catch (Exception ex) when (IsRecoverableRecoveryException(ex))
        {
            if (_closing || cts.IsCancellationRequested) return;
            BackupPhraseList.ItemsSource = null;
            SnapshotSummaryText.Text = "Копию не удалось прочитать";
            ShowFailure("Резервную копию не удалось прочитать", ex);
        }
        finally
        {
            if (ReferenceEquals(_backupPreviewCts, cts))
                _backupPreviewCts = null;
            cts.Dispose();
        }
    }

    private void RestoreTrash_Click(object sender, RoutedEventArgs e)
    {
        if (TrashList.SelectedItem is not RecycleBinItem item) return;
        PhraseEntry restored = _safetyPack.CloneForInsert(
            item.Phrase, _phraseStore.Phrases.Select(phrase => phrase.Id), _phraseStore.Phrases.Count);

        try
        {
            _phraseStore.Phrases.Add(restored);
            _phraseStore.Flush();
        }
        catch (Exception ex) when (IsRecoverableRecoveryException(ex))
        {
            _phraseStore.Phrases.Remove(restored);
            ShowFailure("Фразу не удалось восстановить", ex);
            return;
        }

        DataChanged = true;
        try
        {
            _recycleBin.Remove(item);
            RefreshTrashState();
            MessageBox.Show("Фраза восстановлена.", "GeniaText", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) when (IsRecoverableRecoveryException(ex))
        {
            RefreshTrashState();
            ShowFailure("Фраза восстановлена, но запись осталась в корзине", ex);
        }
    }

    private void DeleteTrash_Click(object sender, RoutedEventArgs e)
    {
        if (TrashList.SelectedItem is not RecycleBinItem item) return;
        MessageBoxResult result = MessageBox.Show(
            $"Удалить «{item.DisplayTitle}» из корзины без возможности восстановления?",
            "GeniaText", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            _recycleBin.Remove(item);
            RefreshTrashState();
        }
        catch (Exception ex) when (IsRecoverableRecoveryException(ex))
        {
            ShowFailure("Запись не удалось удалить из корзины", ex);
        }
    }

    private void EmptyTrash_Click(object sender, RoutedEventArgs e)
    {
        if (_recycleBin.Items.Count == 0) return;
        MessageBoxResult result = MessageBox.Show(
            "Очистить всю корзину без возможности восстановления?",
            "GeniaText", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            _recycleBin.Clear();
            RefreshTrashState();
        }
        catch (Exception ex) when (IsRecoverableRecoveryException(ex))
        {
            ShowFailure("Корзину не удалось очистить", ex);
        }
    }

    private async void DeleteBackup_Click(object sender, RoutedEventArgs e)
    {
        if (BackupList.SelectedItem is not BackupSnapshotInfo snapshot) return;

        MessageBoxResult result = MessageBox.Show(
            $"Удалить резервную копию от {snapshot.CreatedAtLabel}?\n\nЭто действие нельзя отменить.",
            "Удаление резервной копии", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        _backupPreviewCts?.Cancel();
        _backupPreviewCts = null;
        DeleteBackupButton.IsEnabled = false;
        BackupList.IsEnabled = false;
        BackupList.SelectedItem = null;
        BackupPhraseList.ItemsSource = null;
        SnapshotSummaryText.Text = "Удаление копии…";

        try
        {
            await Task.Run(() => _backupBrowser.DeleteSnapshot(snapshot));
            if (_closing) return;

            // Reload from disk so the UI also reflects any external cleanup that may
            // have happened while the recovery window was open.
            _backupsLoadTask = LoadBackupsAsync();
            await _backupsLoadTask;
        }
        catch (Exception ex) when (IsRecoverableRecoveryException(ex))
        {
            if (_closing) return;
            BackupList.IsEnabled = true;
            DeleteBackupButton.IsEnabled = BackupList.SelectedItem is BackupSnapshotInfo;
            SnapshotSummaryText.Text = "Копию не удалось удалить";
            ShowFailure("Резервную копию не удалось удалить", ex);
        }
    }

    private void RestoreSelectedPhrase_Click(object sender, RoutedEventArgs e)
    {
        if (BackupPhraseList.SelectedItem is not PhraseEntry source) return;
        PhraseEntry restored = _safetyPack.CloneForInsert(
            source, _phraseStore.Phrases.Select(phrase => phrase.Id), _phraseStore.Phrases.Count);
        try
        {
            _phraseStore.Phrases.Add(restored);
            _phraseStore.Flush();
            DataChanged = true;
            MessageBox.Show("Выбранная фраза добавлена в текущую базу.", "GeniaText",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) when (IsRecoverableRecoveryException(ex))
        {
            _phraseStore.Phrases.Remove(restored);
            ShowFailure("Фразу не удалось восстановить", ex);
        }
    }

    private void AppendSnapshot_Click(object sender, RoutedEventArgs e) => RestoreSnapshot(append: true);

    private void ReplaceSnapshot_Click(object sender, RoutedEventArgs e) => RestoreSnapshot(append: false);

    private void RestoreSnapshot(bool append)
    {
        if (BackupList.SelectedItem is not BackupSnapshotInfo snapshot) return;
        string action = append ? "объединить с текущей базой" : "заменить текущую базу";
        MessageBoxResult confirmation = MessageBox.Show(
            $"{char.ToUpperInvariant(action[0])}{action[1..]} выбранной резервной копией?\n\nПеред изменением GeniaText создаст ещё одну резервную копию.",
            "Восстановление GeniaText", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmation != MessageBoxResult.Yes) return;

        try
        {
            int count = _phraseStore.Import(_backupBrowser.GetValidatedPath(snapshot), append);
            DataChanged = true;
            MessageBox.Show($"Восстановлено фраз: {count:N0}.", "GeniaText",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) when (IsRecoverableRecoveryException(ex))
        {
            ShowFailure("Резервную копию не удалось восстановить", ex);
        }
    }

    private static bool IsRecoverableRecoveryException(Exception ex) =>
        ex is not OutOfMemoryException and not StackOverflowException and not AccessViolationException;

    private static void ShowFailure(string title, Exception ex)
    {
        AppLog.Error(title, ex);
        MessageBox.Show($"{title}.\n\n{ex.Message}", "GeniaText",
            MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    protected override void OnClosed(EventArgs e)
    {
        _closing = true;
        _backupPreviewCts?.Cancel();
        _backupPreviewCts = null;
        base.OnClosed(e);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
