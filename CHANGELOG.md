# GeniaText 0.7.0-RC1

## Release candidate
- Promoted the verified 0.7.0-beta.1 baseline to RC1 with release/package metadata `0.7.0-rc.1`.
- Clarified Picker help text so Escape is described as hiding the Picker; runtime behavior is unchanged and GeniaText continues running in the tray.
- Automated Release build/self-test gate passes: 18 passed, 0 failed.
- Manual RC smoke/regression checks covered the standard editor, Editor Pro, recycle bin/recovery, backup browser, hotkey/Picker/paste, tray behavior, and the portable package.

## Core Guard
- Frozen Core 0.6.5 Final RC1 files are unchanged.
- Phrase JSON schema and Core hotkey/paste/storage behavior are unchanged.

# GeniaText 0.7.0-beta.1

## Beta freeze
- Promoted the tested alpha.14 codebase to the first 0.7.0 beta with no production-code behavior changes.
- Feature set is frozen. Beta work is limited to release-blocking defects, regression fixes, packaging, and documentation.
- Recovery backup timestamps remain `dd.MM.yyyy HH:mm:ss`; manual deletion, preview, merge, replace, and retention behavior are unchanged.

## Core Guard
- All five frozen Core 0.6.5 Final RC1 files remain byte-identical to the baseline.
- Phrase JSON schema remains unchanged.

# GeniaText 0.7.0-alpha.14

## Backup timestamp clarity
- Backup rows now show seconds using `dd.MM.yyyy HH:mm:ss`, so multiple copies created within the same minute are visually distinct.

## Core Guard
- No frozen Core 0.6.5 Final RC1 file was changed.
- Backup creation, retention, deletion, preview, and restore behavior are unchanged from alpha.13.

# GeniaText 0.7.0-alpha.13

## Backup housekeeping and recovery polish
- Added `Удалить копию` to `Восстановление -> Резервные копии` with an explicit irreversible-action confirmation.
- Backup deletion is path-confined to GeniaText `phrases-*.json` files inside the `Backups` directory; deleting a selected copy never touches the live phrase base.
- The backup browser reloads its list from disk after deletion and cancels any stale preview for the removed file.
- Snapshot preview files now allow delete sharing so a user-initiated removal cannot be blocked by an in-flight read.
- Backup timestamps use the unambiguous `dd.MM.yyyy HH:mm` UI format.
- Clarified recovery labels: `Резервных копий`, `Фраз в копии`, `Объединить с текущими`, and copy-oriented status/error text.
- Settings now state the retention policy: up to 10 copies normally and up to 30 with advanced backups.

## Core Guard
- No frozen Core 0.6.5 Final RC1 file was changed.
- Backup retention itself remains the existing Core behavior; alpha.13 only adds optional manual cleanup in the recovery module.

# GeniaText 0.7.0-alpha.12

## Recovery binding fix
- Fixed the backup-list WPF binding that attempted to write through `Run.Text` into the read-only computed `BackupSnapshotInfo.SizeLabel` property.
- Backup reason and size metadata are now explicitly `Mode=OneWay`, so opening `Резервные копии` can render the list without a TwoWay/OneWayToSource binding exception.

## Phrase-management terminology
- Renamed the Picker entry button from `Редактор` to `Фразы`, reflecting that the destination now contains editing, import/export, recovery, and settings.
- Renamed the standard window heading/title from `Редактор фраз` to `Управление фразами` and clarified the subtitle.
- Kept `Редактор Pro` unchanged as the dedicated advanced editor.

## Core Guard
- No frozen Core 0.6.5 Final RC1 file was changed.
- Phrase JSON schema, hotkey, paste, and storage behavior remain unchanged.

# GeniaText 0.7.0-alpha.11

## Non-blocking backup browser
- Moved backup-directory enumeration off the WPF dispatcher so opening `Резервные копии` cannot freeze the recovery window while storage is slow.
- Stopped automatically selecting and parsing the newest snapshot when the window opens; snapshot contents are loaded only after an explicit user selection.
- Moved snapshot file reading, JSON deserialization, and validation to a worker task; preview now deserializes directly from `FileStream` and avoids a second full clone of snapshot objects.
- Added cancellation for snapshot preview work when the selection changes or the recovery window closes.
- Kept all changes inside the optional recovery module; frozen Core 0.6.5 Final RC1 files remain byte-identical.

# GeniaText 0.7.0-alpha.10

## Recovery crash containment
- Deferred the initial backup enumeration until `RecoveryCenterWindow` has completed WPF `Loaded`; backup-tab selection events raised during visual-tree construction are ignored.
- Added an optional-module exception boundary around construction and `ShowDialog()` of the recovery center so an ordinary recovery-window failure is logged and reported without terminating GeniaText.
- Kept snapshot enumeration/preview failures contained inside the recovery module and retained lazy loading: no backup access occurs until the backup tab is actually opened.

## Core Guard
- No frozen Core 0.6.5 Final RC1 file was changed.
- Phrase JSON schema and Core hotkey/paste/storage behavior remain unchanged.

# GeniaText 0.7.0-alpha.9

## Text-field rendering
- Removed duplicate TextBox inset calculation from the shared classic template and retained one 5 px visual left inset.
- Added inherited layout rounding so field glyphs remain fully visible at 100%, 125%, and 150% scaling.
- Applied the correction to every TextBox-based surface: Picker search, standard editor, Editor Pro, Settings, and Recovery previews.

## Stable Space/caret editing
- Changed standard-editor title and category bindings to commit on focus loss rather than on every keystroke.
- Explicitly commits those fields before Save, window close, and phrase-list mouse selection.
- Prevents the store's normal trim-on-save pass from removing an actively typed trailing Space and resetting the caret to the beginning.

## Recovery crash guard
- Delayed backup enumeration until the backup tab is actually opened.
- Initialized recovery services before XAML event handlers can run and ignored bubbled child-list selection events at the tab boundary.
- Converted Recovery preview bindings to one-way and contained/logged all ordinary snapshot enumeration and preview failures.

## Core Guard
- Frozen Core and phrase-schema files remain unchanged.

# GeniaText 0.7.0-alpha.8

## Clear recovery entry points
- Renamed the phrase-editor `Восстановление` button to `Корзина`.
- The button is visible only when the recycle-bin module is enabled and disabled while the bin has no entries.
- Added a disabled-state tooltip (`Корзина пуста`) and an active-state deleted-phrase count.
- The button state refreshes immediately after recycling a phrase and after closing the recovery window.
- Kept corrupted/unreadable trash distinguishable from an empty bin: the button remains available so the fail-closed error can be inspected.

## Independent backup browser
- Added a separate `Резервные копии` editor button when that module is enabled.
- Each entry point opens the combined recovery window on its corresponding tab.
- No recycle-bin check occurs at application startup or while the module is disabled.

## Compact editor presentation
- Matched Editor Pro phrase-row padding, type size, separators, and one-line height to the standard editor.
- Kept category and Favorite indicators in a compact right-side row instead of a second text line.
- Disabled vertical wheel scrolling inside classic single-line text fields, including the standard editor's title and category fields; multiline phrase text remains scrollable.

## Core Guard
- Frozen Core and phrase-schema files remain unchanged.

# GeniaText 0.7.0-alpha.7

## Recovery-window field correction
- Replaced the read-only single-line TextBoxes for trash-item title and category with non-scrollable text presenters inside classic borders.
- Normal values are no longer vertically clipped or movable with the mouse wheel.
- Long title/category values wrap to additional lines and remain available in full through a tooltip.
- Kept scrolling only where it is useful: the potentially long phrase-text preview.

## Clearer terminology
- Replaced the user-facing word `Picker` with `окно выбора фраз` in Recovery and Settings.
- Kept the internal `PickerWindow` class, settings property names, and frozen Picker code-behind unchanged.

## Core Guard
- This increment changes presentation text/layout and versioned documentation only.
- All five frozen Core hashes and the phrase-schema hash remain unchanged.

# GeniaText 0.7.0-alpha.6

## Optional Safety Pack
- Added three independent, default-OFF flags: recycle bin, backup browser, and safe import preview.
- Deletion with the recycle-bin module enabled archives the phrase atomically before removing it from the live collection; an archive failure leaves the phrase untouched.
- Added a recovery window for restoring trash entries, permanently clearing selected trash, browsing bounded local backups, restoring one phrase, and applying a complete snapshot.
- Added a read-only import preview that classifies new phrases, normalized exact repeats, ID conflicts, and empty entries before the existing `PhraseStore.Import` path performs the mutation.
- Recovery clones resolve GUID collisions without changing the phrase JSON schema.
- All recovery services are created lazily and perform no startup/background work while disabled.

## Classic UI polish
- Reduced shared text-field left padding from 7 px to 5 px and Picker search padding from 9 px to 5 px.
- The Picker header now decodes the source PNG directly at 32 px; title-bar icon scaling snaps to the native 16 px frame.
- Kept search and filter at the same 34 px height and preserved vertical centering.

## Core Guard
- The frozen hotkey, paste, phrase-store, settings-store, and Picker code-behind files retain their 0.6.5 Final RC1 hashes.
- The phrase model/schema is unchanged.

# GeniaText 0.7.0-alpha.5

## Full classic window chrome and compact controls
- Added one reusable Windows 7-inspired title bar to every application window.
- Added working minimize, maximize/restore, close, title dragging, double-click maximize/restore, and resize borders through WPF `WindowChrome` and `SystemCommands`.
- Fixed custom ComboBox presentation so `DisplayMemberPath` is honored for both the selected value and drop-down rows; internal `FilterOption` and `HotkeyOption` records are no longer shown to users.
- Reduced heading, button, field, tab, card, and list-row sizing for a denser classic desktop layout.
- Kept Picker search and filter equal at 34 px with vertically centered text and symbols.
- No C# production source or Core behavior changed.

# GeniaText 0.7.0-alpha.4

## Windows 7-inspired interface
- Added one centralized `Themes/Windows7.xaml` presentation layer for the whole application.
- Restyled buttons, text fields, combo boxes and their drop-down rows, check boxes, tabs, and list frames with a restrained Windows 7/Aero-era treatment.
- Added consistent light-blue hover, focus, and selection states plus classic thin control borders.
- Added a subtle title gradient to the custom Picker frame while retaining its existing layout and drag behavior.
- Preserved the Picker's equal 40 px search/filter height and vertical content centering.
- No C# production source or Core behavior changed in this visual-only increment.

# GeniaText 0.7.0-alpha.3

## Classic interface pass
- Removed corner rounding from the custom Picker frame, application buttons, panels, cards, and list rows.
- Phrase lists now use contiguous square rows with thin separators instead of detached cards.
- Reduced the Picker shadow to a more restrained desktop-style treatment.
- Preserved colors, spacing, selection states, keyboard flow, and all feature behavior.
- No C# production source changed in this visual-only increment.

# GeniaText 0.7.0-alpha.2

## Picker alignment
- Search and category-filter controls now share an explicit 40 px height.
- Search text, selected filter value, arrow content, and drop-down rows use vertical centering.
- The small optical top-padding correction removes the visible upward bias of Segoe UI glyphs.
- Picker-specific styles leave editor and settings controls unchanged.
- Picker code-behind and the frozen Core services remain unchanged.

# GeniaText 0.7.0-alpha.1

## Core Guard
- Based on the verified 0.6.5 Final RC1 source package.
- The standard `GlobalHotkey`, `PickerWindow`, `PasteService`, `PhraseStore`, and `AppSettingsStore` implementations are unchanged.
- All optional features, including the new module, remain OFF by default.
- Added `CORE-CONTRACT.md` with the frozen baseline hashes and compatibility rules.

## Editor Pro module
- Added a separate, opt-in batch editor window.
- Multi-select phrases with Ctrl/Shift and assign or clear their category.
- Add selected phrases to Favorites or remove them from Favorites.
- Detect repeated phrase text after line-ending and outer-whitespace normalization.
- Duplicate detection only selects matches; it never automatically removes data.
- Added pure-logic self-tests for duplicate matching, batch edits, category bounds, and settings round-trip.

## Packaging
- Versioned source and Windows build scripts as `0.7.0-alpha.1`.
- Added an alpha-specific Windows field-test checklist.

# GeniaText 0.6.5

## Final stability hardening
- Frozen feature scope: no new default behavior; optional modules remain OFF by default.
- Settings storage now mirrors phrase storage safety: a transient read/ACL failure blocks future settings writes so an existing `settings.json` cannot be silently overwritten with defaults.
- Added size bounds for local settings and phrase database loading.
- Bounded app-profile rules in memory/UI.
- Fixed first-run settings serialization when Picker coordinates are still `NaN`; settings now round-trip named floating-point sentinels safely.
- Saturated usage counters at `Int32.MaxValue`.
- Added process/UI/background unhandled-exception diagnostics without logging phrase/clipboard contents.
- Release and portable ZIPs now emit SHA-256 checksum files.
- Expanded dependency-free self-tests for settings sanitization/bounds.
- Added `FINAL-RELEASE-CHECKLIST.md` for strict Windows field testing.

# GeniaText 0.6.4

## Core stability
- Preserved the proven default insertion path from 0.6.3: hide picker -> 80 ms -> restore target -> 100 ms -> SendInput(Ctrl+V) -> 350 ms -> guarded clipboard restore.
- Added a paste serialization gate so two UI events cannot run two clipboard/paste transactions at the same time.
- Added best-effort Ctrl/V key release if Windows accepts only part of a SendInput sequence.
- Added a process-level foreground guard before SendInput: transient HWND changes are allowed, switching to a different application blocks automatic paste.
- Existing clipboard marker + sequence-number guard remains in place.
- Optional compatibility retry is isolated behind a disabled-by-default module.

## Modular features (all OFF by default)
- Smart templates: `{date}`, `{time}`, `{datetime}`.
- Advanced search: multi-term matching and conservative typo tolerance.
- Usage history: local successful-use counters and the `Недавние` filter.
- App profiles: `process=category` rules that auto-select a category for the calling application.
- Extended paste compatibility: an additional focus retry only when explicitly enabled.
- Extended backups: periodic phrase database backup, at most once per 10 minutes, up to 30 copies.

## UI / identity
- New GeniaText application/tray icon: blue text bubble + red insertion cursor.
- Settings are split into `Основные` and `Возможности` so the basic workflow stays uncluttered.

## Testing / release gate
- Added dependency-free `GeniaText.Tests` console self-tests for feature defaults, templates, search, app-profile parsing/matching, JSON round-trip, and phrase title behavior.
- Added `build-and-test.cmd`: Release build with warnings-as-errors + self-tests.
- Portable/release scripts now run the same build/self-test gate before publishing.
