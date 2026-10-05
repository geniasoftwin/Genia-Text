# GeniaText 0.7.0-RC1 architecture

The design rule is: **optional features must not change the core path while disabled**.

## Core (always present)
- `GlobalHotkey` — global Ctrl+Space-style activation.
- `PickerWindow` — selection and Enter/Space insertion.
- `PasteService` — clipboard transport, SendInput Ctrl+V, clipboard restoration.
- `PhraseStore` / `AppSettingsStore` — local JSON storage, atomic writes, backups.
- Editor, import/export, tray, portable mode, autostart.

## Optional modules
Stored under `AppSettings.Features`; every flag defaults to `false`.

- `EditorProService` and `EditorProWindow`
- `SmartTemplateService`
- `AdvancedSearchService`
- `AppProfileService`
- Usage-history switch around existing `UseCount` / `LastUsedAt`
- Compatibility switch inside `PasteService`
- Advanced-backup switch inside `PhraseStore`
- `RecycleBinService` and `RecoveryCenterWindow`
- `BackupBrowserService` and `RecoveryCenterWindow`
- `ImportPreviewService` and `ImportPreviewWindow`

When all flags are false, Picker does not inspect the calling process, does not transform phrase text, does not fuzzy-search, does not update usage history, and PasteService uses the same focus/timing path as 0.6.3.

## 0.7 module boundary

Editor Pro is a compile-time module, not an external DLL plug-in. The normal editor exposes one button only while `EditorProEnabled` is true. The button opens a separate window that operates on the existing in-memory phrase collection through `EditorProService`.

The module may update existing `Category` and `IsFavorite` fields, but it does not change the phrase schema, open the Picker, inspect another process, touch the clipboard, or call `PasteService`. Duplicate detection is read-only.

The frozen Core contract and baseline hashes are documented in `CORE-CONTRACT.md`.

## Safety Pack boundary

The three alpha.6 recovery functions are independent flags and lazy editor extensions. Their constructors do not read disk. When the recycle-bin flag is enabled, the trash file is read as the phrase editor opens so the `Корзина` button can reflect empty/non-empty state; with the flag disabled it is not constructed or read. Backups are enumerated only when their tab is selected (including a direct open on that tab), and an import is previewed only after the user selects a file.

`RecycleBinService` owns `trash.json` and uses atomic replacement. The editor archives first and removes second, so a write failure cannot turn a requested recycle operation into irreversible deletion. Restores clone values and replace a colliding GUID before calling the existing phrase-store save path.

`BackupBrowserService` accepts only files beneath the configured `Backups` directory and bounds file size/count/field lengths. Preview remains read-only. Alpha.13 adds one explicit housekeeping write-side action: after UI confirmation it may delete only a selected `phrases-*.json` file inside `Backups`. Full restore operations pass the validated path to the frozen `PhraseStore.Import`, retaining its pre-import backup and validation behavior.

`ImportPreviewService` is read-only. After confirmation, the frozen `PhraseStore.Import` remains authoritative. None of these modules references `GlobalHotkey`, `PickerWindow`, `PasteService`, clipboard APIs, target-window APIs, sockets, or HTTP.

## Presentation boundary

Alpha.6 keeps presentation behind a single application resource boundary: `Themes/Windows7.xaml`. `App.xaml` only merges that dictionary; windows consume stable semantic resource keys such as `WindowBackground`, `EditorTextBox`, `EditorComboBox`, and the button styles. The theme owns classic control templates, the shared `WindowChrome` title bar, system-command buttons, and visual states, while Picker-specific sizing remains local to `PickerWindow.xaml`.

Alpha.7 corrects only the read-only title/category presentation in `RecoveryCenterWindow.xaml` and user-facing terminology. It does not rename `PickerWindow`, change settings keys, or touch the frozen Picker code-behind.

Alpha.8 gives recycle-bin and backup-browser modules separate editor entry points. `Корзина` reflects the already bounded `RecycleBinService.Items` count; an unreadable bin stays actionable rather than being mistaken for an empty one, so the detailed fail-closed error remains accessible.

Alpha.9 keeps the text-input correction inside the shared presentation template and editor bindings. Recovery initializes its services before view events, filters bubbled tab-selection events, loads backup metadata on first use, and contains ordinary backup enumeration/preview exceptions at the recovery-window boundary. These safeguards do not add startup work or enter the Picker/paste path.

Alpha.11 moves backup metadata enumeration and snapshot preview parsing off the WPF dispatcher. Opening the backup tab never auto-selects a snapshot; preview work starts only after an explicit selection and stale work is cancelled on reselection/window close. Snapshot preview reads directly from a bounded `FileStream` and reuses detached deserialized objects, reducing peak memory without changing restore semantics.

Alpha.13 adds manual backup housekeeping without changing Core retention. The recovery UI confirms deletion, cancels stale preview work, deletes only a validated `phrases-*.json` file under `Backups`, and reloads the directory listing.

Alpha.14 changes only backup timestamp presentation by adding seconds (`dd.MM.yyyy HH:mm:ss`) to `BackupSnapshotInfo.CreatedAtLabel`; enumeration, sorting, retention, deletion, preview, and restore semantics are unchanged.

The presentation layer changes no event handlers, models, persistence, Picker code-behind, hotkey, clipboard, or paste behavior. Caption buttons call the framework's window commands directly from XAML. Replacing or refining the theme therefore does not require a Core change.
