using GeniaText.Models;
using GeniaText.Services;
using GeniaText.Features;
using System.Collections.Specialized;
using System.Windows.Data;

namespace GeniaText;

public partial class PickerWindow : Window
{
    private readonly PhraseStore _phraseStore;
    private readonly PasteService _pasteService;
    private readonly AppSettingsStore _settingsStore;
    private readonly ICollectionView _phrasesView;
    private readonly SmartTemplateService _smartTemplates = new();
    private readonly AdvancedSearchService _advancedSearch = new();
    private readonly AppProfileService _appProfiles = new();
    private IntPtr _targetWindow;
    private bool _placementReady;
    private bool _suppressAutoHide;
    private bool _insertInProgress;

    public PickerWindow(PhraseStore phraseStore, PasteService pasteService, AppSettingsStore settingsStore)
    {
        InitializeComponent();
        _phraseStore = phraseStore;
        _pasteService = pasteService;
        _settingsStore = settingsStore;

        RestoreWindowPlacement();
        // Use a private view for the picker. The default WPF view is shared by every
        // control bound to this collection; sorting that shared view by SortOrder made
        // the editor list re-sort while Move Up/Down was renumbering items, which
        // looked like phrases were jumping unpredictably.
        _phrasesView = new ListCollectionView(_phraseStore.Phrases);
        _phrasesView.Filter = PhraseMatchesSearch;
        PhraseList.ItemsSource = _phrasesView;
        _phraseStore.Phrases.CollectionChanged += Phrases_CollectionChanged;

        RefreshFilterOptions();
        ApplySort();
        _placementReady = true;
    }

    public bool AllowClose { get; set; }
    public event EventHandler? EditRequested;

    public void CaptureTargetWindow() => _targetWindow = GetForegroundWindow();

    public void ShowPicker()
    {
        RefreshFilterOptions();
        ApplyAppProfileFilter();
        _phrasesView.Refresh();
        SelectFirstPhrase();
        UpdateEmptyMessage();

        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;

        Topmost = true;
        Activate();
        Topmost = false;
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private bool PhraseMatchesSearch(object item)
    {
        if (item is not PhraseEntry phrase) return false;

        string search = SearchBox?.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(search))
        {
            bool match = _settingsStore.Current.Features.AdvancedSearchEnabled
                ? _advancedSearch.Matches(phrase, search)
                : phrase.Title.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                  phrase.Text.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                  phrase.Category.Contains(search, StringComparison.CurrentCultureIgnoreCase);
            if (!match) return false;
        }

        if (FilterBox?.SelectedItem is not FilterOption filter)
            return true;

        return filter.Kind switch
        {
            PhraseFilterKind.All => true,
            PhraseFilterKind.Favorites => phrase.IsFavorite,
            PhraseFilterKind.Recent => phrase.LastUsedAt.HasValue,
            PhraseFilterKind.Category => string.Equals(phrase.Category.Trim(), filter.Category,
                StringComparison.CurrentCultureIgnoreCase),
            _ => true
        };
    }

    private void RefreshFilterOptions()
    {
        string oldKey = (FilterBox.SelectedItem as FilterOption)?.Key ?? "all";
        var options = new List<FilterOption>
        {
            new("all", "Все фразы", PhraseFilterKind.All),
            new("favorites", "★ Избранное", PhraseFilterKind.Favorites)
        };
        if (_settingsStore.Current.Features.UsageHistoryEnabled)
            options.Add(new("recent", "Недавние", PhraseFilterKind.Recent));
        options.AddRange(_phraseStore.GetCategories()
            .Select(category => new FilterOption("category:" + category, category,
                PhraseFilterKind.Category, category)));

        FilterBox.ItemsSource = options;
        FilterBox.SelectedItem = options.FirstOrDefault(o => o.Key == oldKey) ?? options[0];
    }

    private void ApplyAppProfileFilter()
    {
        FeatureSettings features = _settingsStore.Current.Features;
        if (!features.AppProfilesEnabled || string.IsNullOrWhiteSpace(features.AppProfileRules))
            return;

        if (FilterBox.ItemsSource is not IEnumerable<FilterOption> options)
            return;

        string? category = _appProfiles.GetCategoryForWindow(_targetWindow, features.AppProfileRules);
        FilterOption? match = string.IsNullOrWhiteSpace(category)
            ? null
            : options.FirstOrDefault(option =>
                option.Kind == PhraseFilterKind.Category &&
                string.Equals(option.Category, category, StringComparison.CurrentCultureIgnoreCase));

        // A mapped application gets its category. Unmapped applications fall back
        // to All instead of inheriting the previous application's profile filter.
        FilterBox.SelectedItem = match ?? options.First(option => option.Kind == PhraseFilterKind.All);
    }

    private void ApplySort()
    {
        using (_phrasesView.DeferRefresh())
        {
            _phrasesView.SortDescriptions.Clear();
            if ((FilterBox.SelectedItem as FilterOption)?.Kind == PhraseFilterKind.Recent)
            {
                _phrasesView.SortDescriptions.Add(new SortDescription(nameof(PhraseEntry.LastUsedAt),
                    ListSortDirection.Descending));
            }
            else
            {
                _phrasesView.SortDescriptions.Add(new SortDescription(nameof(PhraseEntry.SortOrder),
                    ListSortDirection.Ascending));
            }
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _phrasesView?.Refresh();
        SelectFirstPhrase();
        UpdateEmptyMessage();
    }

    private void FilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_phrasesView is null) return;
        ApplySort();
        _phrasesView.Refresh();
        SelectFirstPhrase();
        UpdateEmptyMessage();
    }

    private void SelectFirstPhrase()
    {
        if (PhraseList.Items.Count > 0)
            PhraseList.SelectedIndex = 0;
        else
            PhraseList.SelectedIndex = -1;
    }

    private void UpdateEmptyMessage() =>
        EmptyMessage.Visibility = PhraseList.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    private async Task InsertSelectedPhraseAsync()
    {
        if (_insertInProgress || PhraseList.SelectedItem is not PhraseEntry phrase || string.IsNullOrEmpty(phrase.Text))
            return;

        _insertInProgress = true;
        try
        {
            string text = _settingsStore.Current.Features.SmartTemplatesEnabled
                ? _smartTemplates.Expand(phrase.Text)
                : phrase.Text;

            IntPtr target = _targetWindow;
            Hide();
            bool pasted = await _pasteService.PasteAsync(target, text);
            if (pasted)
            {
                if (_settingsStore.Current.Features.UsageHistoryEnabled)
                    _phraseStore.MarkUsed(phrase);
                SearchBox.Clear();
            }
            else
            {
                MessageBox.Show(
                    "Не удалось автоматически вставить фразу. Текст оставлен в буфере обмена — нажмите Ctrl+V вручную.",
                    "GeniaText", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        finally
        {
            _insertInProgress = false;
        }
    }

    private async void Insert_Click(object sender, RoutedEventArgs e) => await InsertSelectedPhraseAsync();

    private async void PhraseList_MouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        await InsertSelectedPhraseAsync();

    private async void Window_PreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Hide();
            return;
        }

        if (e.Key is Key.Up or Key.Down)
        {
            if (PhraseList.Items.Count == 0) return;
            int direction = e.Key == Key.Down ? 1 : -1;
            int next = Math.Clamp(PhraseList.SelectedIndex + direction, 0, PhraseList.Items.Count - 1);
            PhraseList.SelectedIndex = next;
            PhraseList.ScrollIntoView(PhraseList.SelectedItem);
            e.Handled = true;
            return;
        }

        // 0.6.2 behavior: when the search field is empty, Space is a quick
        // insert key even though keyboard focus normally remains in SearchBox.
        // As soon as the user has typed a search query, Space behaves as a
        // normal character so multi-word searches still work.
        bool isQuickInsertSpace =
            e.Key == Key.Space &&
            Keyboard.Modifiers == ModifierKeys.None &&
            (SearchBox.IsKeyboardFocusWithin || PhraseList.IsKeyboardFocusWithin) &&
            string.IsNullOrWhiteSpace(SearchBox.Text);

        if (e.Key == Key.Enter || isQuickInsertSpace)
        {
            e.Handled = true;
            await InsertSelectedPhraseAsync();
        }
    }

    private void Editor_Click(object sender, RoutedEventArgs e)
    {
        AppLog.Info("Picker: Editor button activated");
        _suppressAutoHide = true;
        Hide();
        EditRequested?.Invoke(this, EventArgs.Empty);
        Dispatcher.BeginInvoke(() => _suppressAutoHide = false, DispatcherPriority.ContextIdle);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Hide();

    private void Header_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed && e.ClickCount == 1)
        {
            try { DragMove(); } catch { }
        }
    }

    private void RestoreWindowPlacement()
    {
        AppSettings settings = _settingsStore.Current;
        Width = Math.Clamp(settings.PickerWidth, MinWidth, 2000);
        Height = Math.Clamp(settings.PickerHeight, MinHeight, 1600);

        if (double.IsFinite(settings.PickerLeft) && double.IsFinite(settings.PickerTop))
        {
            Rect workArea = SystemParameters.WorkArea;
            double left = Math.Clamp(settings.PickerLeft, workArea.Left, Math.Max(workArea.Left, workArea.Right - Width));
            double top = Math.Clamp(settings.PickerTop, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - Height));
            Left = left;
            Top = top;
            WindowStartupLocation = WindowStartupLocation.Manual;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        SaveWindowPlacement();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        SaveWindowPlacement();
    }

    private void SaveWindowPlacement()
    {
        if (!_placementReady || WindowState != WindowState.Normal) return;
        AppSettings settings = _settingsStore.Current;
        settings.PickerLeft = Left;
        settings.PickerTop = Top;
        settings.PickerWidth = ActualWidth;
        settings.PickerHeight = ActualHeight;
        _settingsStore.ScheduleSave();
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (_suppressAutoHide || !IsVisible) return;
        Dispatcher.BeginInvoke(() =>
        {
            if (!_suppressAutoHide && IsVisible && !IsActive)
                Hide();
        }, DispatcherPriority.Background);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        SaveWindowPlacement();
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        _phraseStore.Phrases.CollectionChanged -= Phrases_CollectionChanged;
        base.OnClosing(e);
    }

    private void Phrases_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshFilterOptions();
        _phrasesView.Refresh();
        UpdateEmptyMessage();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}

internal enum PhraseFilterKind
{
    All,
    Favorites,
    Recent,
    Category
}

internal sealed record FilterOption(string Key, string Label, PhraseFilterKind Kind, string? Category = null);
