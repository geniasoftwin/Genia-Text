# GeniaText 0.7.0-beta.1 static validation

Performed in the packaging environment before creating the test source archive.

## Passed checks

- The 0.6.5 Final RC1 source remains the declared Core baseline.
- All 11 XAML/project/publish XML files parsed successfully with external DTD/schema access disabled.
- All 45 referenced WPF event-handler bindings were found in their matching code-behind files.
- All 28 production C# files passed a comment/string-aware structural brace check.
- All 34 declared XAML resource keys resolve across 198 `StaticResource` references.
- Version metadata and portable/release script versions are consistent at `0.7.0-beta.1`. Backup timestamps use `dd.MM.yyyy HH:mm:ss`.
- The application icon remains a valid 10-resolution Windows ICO and the source PNG remains valid.
- The Picker header explicitly decodes `GeniaText.png` at 32 px; the title-bar image snaps the 16 px icon to device pixels.
- Shared fields apply one 5 px visual left inset in the template while `TextBox.Padding` remains zero, preventing duplicate text-metric shrinkage. Inherited layout rounding covers fractional display scaling.
- The Picker search field is 34 px high with the shared 5 px visual left inset and vertical centering. The category filter remains 34 px high and vertically centered.
- The shared classic TextBox style disables vertical scrolling by default for single-line fields; every multiline phrase/rules/preview field explicitly restores `Auto` vertical scrolling.
- Editor Pro phrase rows use the standard editor's `10,8` padding and 12 px one-line title layout; category and Favorite metadata remain in right-side columns.
- Recovery title/category values use non-scrollable wrapping `TextBlock` presenters inside classic borders; only the long phrase-text preview retains a vertical scrollbar.
- User-facing Recovery and Settings text uses `окно выбора фраз`; technical `PickerWindow` identifiers remain unchanged.
- `RecycleBinEnabled`, `BackupBrowserEnabled`, and `ImportPreviewEnabled` have the default value `false` and are included in settings JSON round-trip tests.
- The test harness declares 18 pure-logic self-tests, including Safety Pack classification, recovery GUID collision handling, size bounds, and default-OFF checks.
- Safety Pack source has no reference to `PasteService`, clipboard APIs, foreground-window APIs, P/Invoke, sockets, or HTTP.
- Recovery services are lazy properties of the editor. With their flags OFF they are not constructed, perform no disk access, and expose no button outside Settings.
- `RecycleBinService` uses bounded JSON and atomic replacement. The editor archives before removing a live phrase; a failed archive blocks deletion.
- `BackupBrowserService` confines selected snapshots to `Backups`, limits full snapshots to the frozen import boundary, and performs no live-base writes. Its only direct write-side action is explicit deletion of a selected `phrases-*.json` backup after UI confirmation. Complete restores still call the existing `PhraseStore.Import` path.
- `ImportPreviewService` is read-only. Confirmed mutations still call the existing `PhraseStore.Import` implementation.
- `Корзина` and `Резервные копии` are independent editor entry points; an empty loaded bin disables only the recycle-bin button.
- Backup enumeration is deferred until the backup tab is selected and runs off the WPF dispatcher. No snapshot is auto-selected; preview file I/O, JSON deserialization, and validation run in a worker task only after explicit selection. Selection changes/close cancel stale preview work, and ordinary failures remain contained and logged.
- Backup row bindings for computed read-only `CreatedAtLabel`/`SizeLabel` and `Reason` are OneWay; no WPF write-back path exists.
- Manual backup deletion is limited to selected `phrases-*.json` files inside `Backups`, cancels stale preview work, and reloads the list from disk after success.
- Picker entry terminology is `Фразы`; the destination title/heading is `Управление фразами`, while `Редактор Pro` remains a separate advanced editor label.
- Standard-editor title/category bindings commit on focus loss and explicitly before save/list mouse selection, avoiding trim-driven caret resets during active input.
- `PhraseEntry.cs` is byte-identical to 0.6.5, so beta.1 does not change the phrase JSON schema.

## Core Guard verification

These five Core files are byte-identical to 0.6.5 Final RC1 and match the hashes in `CORE-CONTRACT.md`:

- `GeniaText/Services/GlobalHotkey.cs` — `96bfcb1cb6880ae0de4b8d3e010f67f8cefcf0072656f32b4032de0862a1e630`
- `GeniaText/Services/PasteService.cs` — `a8d1d636a5c2fd67aa17ed5dbbff278ba1e09a24005d424aa608cae477d93466`
- `GeniaText/Services/PhraseStore.cs` — `1cf8d699cbbcaf086ea475aa3503108fdbc519b29b32341285ac7bf8854e55ab`
- `GeniaText/Services/AppSettingsStore.cs` — `9b27dab0f861dfbdef31211c4ab9e30a2d503d8045a2ae86b7b6bb0143c44a84`
- `GeniaText/PickerWindow.xaml.cs` — `34604dfc4e6d65e62aa64281d685f0e01b8c15fa803311cfd8909c6319573f47`

`GeniaText/Models/PhraseEntry.cs` also retains its baseline hash:
`77f283151b93d74572659fc2169d0508afe4b8cacda214018b05ba6205d37520`.

## Required Windows gate

This environment does not contain the .NET 10 SDK or a Windows/WPF runtime. Compilation, execution of the 18 self-tests, UI layout, icon rendering, `WindowChrome` behavior, Win32 hotkey/paste behavior, recovery I/O, and portable publishing could not be run here.

Before using an executable, run `build-and-test.cmd`, then `build-portable.cmd`, and complete `BETA-TEST-CHECKLIST.md` on Windows. Static validation is not a substitute for that gate.
