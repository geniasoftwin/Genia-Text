using GeniaText.Features;
using GeniaText.Models;

internal static class Program
{
    private static int _passed;
    private static int _failed;

    private static int Main()
    {
        Run("Feature defaults are OFF", FeatureDefaultsAreOff);
        Run("Smart templates expand deterministic tokens", SmartTemplatesExpand);
        Run("Smart templates preserve unknown tokens", SmartTemplatesPreserveUnknown);
        Run("Advanced search handles multi-term query", AdvancedSearchMultiTerm);
        Run("Advanced search tolerates one typo", AdvancedSearchTypo);
        Run("Advanced search does not over-fuzz short query", AdvancedSearchShortTerm);
        Run("App profile parser accepts .exe and comments", AppProfileParser);
        Run("App profile matching is case-insensitive", AppProfileMatching);
        Run("Editor Pro finds normalized exact duplicates", EditorProFindsDuplicates);
        Run("Editor Pro batch operations are deterministic", EditorProBatchOperations);
        Run("Editor Pro rejects oversized categories", EditorProRejectsOversizedCategory);
        Run("Import preview classifies without mutation", ImportPreviewClassifies);
        Run("Recovery clone resolves ID collision", RecoveryCloneResolvesIdCollision);
        Run("Safety Pack rejects oversized phrase", SafetyPackRejectsOversizedPhrase);
        Run("Settings JSON round-trip preserves modules", SettingsRoundTrip);
        Run("Settings sanitizer repairs invalid values", SettingsSanitization);
        Run("Settings sanitizer bounds app profile rules", SettingsRulesAreBounded);
        Run("Phrase display title fallback remains stable", PhraseDisplayTitle);

        Console.WriteLine();
        Console.WriteLine($"GeniaText self-tests: {_passed} passed, {_failed} failed.");
        return _failed == 0 ? 0 : 1;
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            _passed++;
            Console.WriteLine($"PASS  {name}");
        }
        catch (Exception ex)
        {
            _failed++;
            Console.WriteLine($"FAIL  {name}: {ex.Message}");
        }
    }

    private static void FeatureDefaultsAreOff()
    {
        FeatureSettings f = new();
        Assert(!f.EditorProEnabled && !f.SmartTemplatesEnabled && !f.AdvancedSearchEnabled && !f.UsageHistoryEnabled &&
               !f.AppProfilesEnabled && !f.CompatibilityModeEnabled && !f.AdvancedBackupsEnabled &&
               !f.RecycleBinEnabled && !f.BackupBrowserEnabled && !f.ImportPreviewEnabled,
            "An optional module is enabled by default.");
    }

    private static void SmartTemplatesExpand()
    {
        var service = new SmartTemplateService();
        var now = new DateTimeOffset(2026, 8, 25, 18, 42, 0, TimeSpan.FromHours(3));
        string actual = service.Expand("Дата {date}; время {time}; всё {datetime}", now);
        AssertEqual("Дата 25.08.2026; время 18:42; всё 25.08.2026 18:42", actual);
    }

    private static void SmartTemplatesPreserveUnknown()
    {
        var service = new SmartTemplateService();
        var now = new DateTimeOffset(2026, 1, 2, 3, 4, 0, TimeSpan.Zero);
        AssertEqual("A {client} B", service.Expand("A {client} B", now));
    }

    private static PhraseEntry SearchPhrase() => new()
    {
        Title = "Приветствие клиента",
        Category = "Поддержка",
        Text = "Здравствуйте! Чем я могу вам помочь сегодня?"
    };

    private static void AdvancedSearchMultiTerm()
    {
        var service = new AdvancedSearchService();
        Assert(service.Matches(SearchPhrase(), "приветствие поддержка"), "Expected both terms to match across fields.");
        Assert(!service.Matches(SearchPhrase(), "приветствие доставка"), "Unrelated term unexpectedly matched.");
    }

    private static void AdvancedSearchTypo()
    {
        var service = new AdvancedSearchService();
        Assert(service.Matches(SearchPhrase(), "преветствие"), "Single typo should match a long word.");
    }

    private static void AdvancedSearchShortTerm()
    {
        var service = new AdvancedSearchService();
        Assert(!service.Matches(SearchPhrase(), "пру"), "Short unrelated fragment should not fuzzy-match.");
    }

    private static void AppProfileParser()
    {
        string rules = "# comment\nchrome.exe=Web\ntelegram = Telegram\nbroken-line\n=bad";
        var parsed = AppProfileService.ParseRules(rules);
        AssertEqual(2, parsed.Count);
        AssertEqual("chrome", parsed[0].Process);
        AssertEqual("Web", parsed[0].Category);
    }

    private static void AppProfileMatching()
    {
        string rules = "Chrome=Web\nTELEGRAM.EXE=Chat";
        AssertEqual("Web", AppProfileService.MatchCategory("chrome", rules));
        AssertEqual("Chat", AppProfileService.MatchCategory("telegram", rules));
        AssertEqual<string?>(null, AppProfileService.MatchCategory("notepad", rules));
    }

    private static void EditorProFindsDuplicates()
    {
        var service = new EditorProService();
        var first = new PhraseEntry { Text = "Hello\r\nWorld ", SortOrder = 2 };
        var second = new PhraseEntry { Text = " Hello\nWorld", SortOrder = 1 };
        var differentCase = new PhraseEntry { Text = "hello\nWorld", SortOrder = 3 };
        var empty = new PhraseEntry { Text = "   ", SortOrder = 4 };

        IReadOnlyList<DuplicatePhraseGroup> groups = service.FindDuplicateGroups(
            [first, second, differentCase, empty]);

        AssertEqual(1, groups.Count);
        AssertEqual(2, groups[0].Phrases.Count);
        Assert(ReferenceEquals(second, groups[0].Phrases[0]), "Duplicates should retain phrase order.");
        Assert(ReferenceEquals(first, groups[0].Phrases[1]), "Duplicates should retain phrase order.");
    }

    private static void EditorProBatchOperations()
    {
        var service = new EditorProService();
        var first = new PhraseEntry { Category = "Old" };
        var second = new PhraseEntry { Category = "Other" };

        AssertEqual(2, service.ApplyCategory([first, second, first], "  Support  "));
        AssertEqual("Support", first.Category);
        AssertEqual("Support", second.Category);
        AssertEqual(2, service.SetFavorite([first, second, first], true));
        Assert(first.IsFavorite && second.IsFavorite, "Selected phrases should become favorites.");
        AssertEqual(0, service.SetFavorite([first, second], true));
    }

    private static void EditorProRejectsOversizedCategory()
    {
        var service = new EditorProService();
        bool rejected = false;
        try
        {
            service.ApplyCategory([new PhraseEntry()], new string('x', EditorProService.MaxCategoryLength + 1));
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        Assert(rejected, "Oversized category should be rejected.");
    }

    private static void ImportPreviewClassifies()
    {
        var service = new SafetyPackService();
        Guid existingId = Guid.NewGuid();
        var existing = new PhraseEntry { Id = existingId, Text = "Hello\r\nWorld" };
        var duplicate = new PhraseEntry { Id = Guid.NewGuid(), Text = " Hello\nWorld " };
        var idConflict = new PhraseEntry { Id = existingId, Text = "Different text" };
        var newPhrase = new PhraseEntry { Id = Guid.NewGuid(), Text = "Brand new" };
        var empty = new PhraseEntry { Text = "   " };

        ImportPreviewResult result = service.AnalyzeImport(
            [duplicate, idConflict, newPhrase, empty], [existing]);

        AssertEqual(4, result.SourceCount);
        AssertEqual(1, result.DuplicateCount);
        AssertEqual(1, result.IdConflictCount);
        AssertEqual(1, result.NewCount);
        AssertEqual(1, result.SkippedEmptyCount);
        AssertEqual("Hello\r\nWorld", existing.Text);
        AssertEqual(" Hello\nWorld ", duplicate.Text);
    }

    private static void RecoveryCloneResolvesIdCollision()
    {
        var service = new SafetyPackService();
        Guid occupiedId = Guid.NewGuid();
        var source = new PhraseEntry
        {
            Id = occupiedId,
            Title = "Recovered",
            Text = "Text",
            Category = "Support",
            SortOrder = 2
        };

        PhraseEntry clone = service.CloneForInsert(source, [occupiedId], 7);
        Assert(clone.Id != occupiedId && clone.Id != Guid.Empty, "A colliding ID was not replaced.");
        AssertEqual("Recovered", clone.Title);
        AssertEqual("Text", clone.Text);
        AssertEqual(7, clone.SortOrder);
        Assert(!ReferenceEquals(source, clone), "Recovery must clone instead of reusing the source object.");
    }

    private static void SafetyPackRejectsOversizedPhrase()
    {
        bool rejected = false;
        try
        {
            SafetyPackService.ValidatePhrase(new PhraseEntry
            {
                Text = new string('x', SafetyPackService.MaxPhraseTextLength + 1)
            });
        }
        catch (System.IO.InvalidDataException)
        {
            rejected = true;
        }
        Assert(rejected, "Oversized recovery data should be rejected.");
    }

    private static void SettingsRoundTrip()
    {
        var original = new AppSettings();
        original.Features.EditorProEnabled = true;
        original.Features.SmartTemplatesEnabled = true;
        original.Features.AppProfilesEnabled = true;
        original.Features.RecycleBinEnabled = true;
        original.Features.BackupBrowserEnabled = true;
        original.Features.ImportPreviewEnabled = true;
        original.Features.AppProfileRules = "chrome=Web";
        var options = new JsonSerializerOptions
        {
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
        string json = JsonSerializer.Serialize(original, options);
        AppSettings copy = JsonSerializer.Deserialize<AppSettings>(json, options) ?? throw new Exception("Deserialization returned null.");
        Assert(copy.Features.EditorProEnabled, "Editor Pro flag was lost.");
        Assert(copy.Features.SmartTemplatesEnabled, "SmartTemplates flag was lost.");
        Assert(copy.Features.AppProfilesEnabled, "AppProfiles flag was lost.");
        Assert(copy.Features.RecycleBinEnabled, "RecycleBin flag was lost.");
        Assert(copy.Features.BackupBrowserEnabled, "BackupBrowser flag was lost.");
        Assert(copy.Features.ImportPreviewEnabled, "ImportPreview flag was lost.");
        AssertEqual("chrome=Web", copy.Features.AppProfileRules);
    }

    private static void SettingsSanitization()
    {
        var settings = new AppSettings
        {
            HotkeyModifiers = 0,
            HotkeyVirtualKey = 0,
            PickerWidth = double.PositiveInfinity,
            PickerHeight = -1,
            PickerLeft = double.NegativeInfinity,
            PickerTop = double.NaN
        };

        SettingsSanitizer.Sanitize(settings);
        AssertEqual<uint>(0x0002, settings.HotkeyModifiers);
        AssertEqual<uint>(0x20, settings.HotkeyVirtualKey);
        AssertEqual(680d, settings.PickerWidth);
        AssertEqual(520d, settings.PickerHeight);
        Assert(double.IsNaN(settings.PickerLeft), "Invalid PickerLeft should become NaN.");
        Assert(double.IsNaN(settings.PickerTop), "Invalid PickerTop should remain/become NaN.");
    }

    private static void SettingsRulesAreBounded()
    {
        var settings = new AppSettings();
        settings.Features.AppProfileRules = new string('x', SettingsSanitizer.MaxAppProfileRulesLength + 100);
        SettingsSanitizer.Sanitize(settings);
        AssertEqual(SettingsSanitizer.MaxAppProfileRulesLength, settings.Features.AppProfileRules.Length);
    }

    private static void PhraseDisplayTitle()
    {
        var phrase = new PhraseEntry { Text = "Первая строка\nВторая строка" };
        AssertEqual("Первая строка Вторая строка", phrase.DisplayTitle);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected '{expected}', got '{actual}'.");
    }
}
