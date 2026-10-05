# GeniaText 0.7.0-RC1

Windows WPF utility for fast insertion of user-created text phrases.

Core workflow remains intentionally small:
`Ctrl+Space -> choose phrase -> Enter or Space -> paste into the original field`.

This RC1 build continues the modular model introduced in 0.6.5. Every optional module is disabled by default and lives under **Настройки -> Возможности**.

## RC1: release candidate

RC1 promotes the verified 0.7.0-beta.1 baseline with release/package metadata updated to `0.7.0-rc.1`. The feature set remains frozen. The only RC1 production UI change clarifies that Escape hides the Picker rather than exits the tray application; Core 0.6.5 Final RC1 remains unchanged.

## Beta.1: feature freeze

Beta.1 promotes the field-tested alpha.14 codebase without changing production behavior. The 0.7.0 feature set is now frozen; only release-blocking defects, regression fixes, packaging, and documentation changes are allowed on the path to RC1 and Stable.

## Alpha.14: clearer backup timestamps

Backup rows now include seconds (`dd.MM.yyyy HH:mm:ss`). This makes separate copies created during the same minute easy to distinguish without changing how backups are created, retained, previewed, deleted, or restored.

## Alpha.13: backup housekeeping and recovery polish

The backup browser now includes **Удалить копию** with an irreversible-action confirmation. Deletion is confined to selected `phrases-*.json` files inside the local `Backups` folder, cancels stale preview work, reloads the list from disk, and never deletes the live `phrases.json`. The normal Core retention policy remains unchanged: up to 10 copies, or up to 30 with advanced backups enabled.

Recovery labels consistently say **резервная копия** rather than **снимок**. Alpha.14 adds seconds to the backup timestamp display. The list keeps the alpha.12 one-way binding fix for read-only metadata.

The main Picker entry remains **Фразы** rather than **Редактор**. The destination window is titled **Управление фразами**, matching its broader role: editing, import/export, recovery, and settings. **Редактор Pro** keeps its existing name as the dedicated advanced editor.

## Alpha.9: text-input and Recovery stability

The shared classic TextBox template now applies its inner spacing exactly once and rounds layout at display-scaling boundaries. Search, editor, Editor Pro, Settings, and read-only Recovery text fields therefore share the same unclipped Segoe UI rendering, including Cyrillic descenders such as `у`, `д`, and `р`.

The standard editor commits `Название` and `Категория` when focus leaves the field (and explicitly before save/selection changes), rather than sending each keystroke into the store's trimming pass. A trailing Space can no longer be trimmed during active editing and reset the caret to the start; the multiline phrase body continues to update and scroll normally.

Backup snapshots are now enumerated only when the **Резервные копии** tab is first opened, or when its direct editor button opens that tab. Initialization is guarded, preview bindings are read-only, and ordinary enumeration/preview failures are logged and shown as a warning instead of escaping the Recovery window.

## Alpha.8: clearer recovery entry points

The phrase editor now shows **Корзина** instead of the generic **Восстановление** button. It is disabled while the recycle bin is empty, becomes active immediately after a phrase is recycled, and returns to the disabled state after the last entry is restored or removed. Its tooltip shows the current count or `Корзина пуста`, including on the disabled button.

Backup browsing remains an independent module. When enabled, it has its own **Резервные копии** button and opens the recovery window directly on the backup tab. The recycle-bin file is checked only when the phrase editor opens and only when that module is enabled; application startup and the main phrase-selection flow remain unchanged.

Editor Pro now uses the same compact one-line phrase rows as the standard editor. Category and Favorite markers remain available on the right without increasing row height. Classic single-line text fields no longer move their contents vertically under the mouse wheel; multiline phrase text keeps normal scrolling.

## Alpha.7: recovery-window polish

Read-only `Название` and `Категория` values in the recovery window are now rendered as non-scrollable text inside classic field borders. Normal values are vertically centered, long values wrap to additional lines, and the full value remains available as a tooltip. User-facing `Picker` wording was replaced with the clearer **окно выбора фраз**; internal class names remain unchanged to preserve Core.

## Alpha.6: Safety Pack

Three independent recovery modules remain OFF until explicitly enabled:

- **Корзина удалённых фраз** writes a local copy before the editor removes a phrase. If that write fails, the phrase is not deleted.
- **Просмотр резервных копий** opens existing local `Backups` copies and can restore one phrase, merge/replace the current base through the existing validated import path, or explicitly delete a selected backup copy.
- **Безопасный предпросмотр импорта** validates the selected file and shows new phrases, exact repeats, ID conflicts, and skipped empty entries before the user chooses an action.

The modules are lazy: no recovery file is opened and no recovery UI exists outside Settings while the corresponding flags are off. The normal startup and insertion path remains `Ctrl+Space -> phrase -> Enter/Space -> paste`.

## Alpha module: Editor Pro

Enable **Редактор Pro** to add a separate batch-editing window. It can:

- assign or clear a category for several selected phrases;
- add several phrases to Favorites or remove them;
- find exact duplicate phrase texts after normalizing line endings and outer whitespace.

Duplicate search is read-only: it selects matches but never deletes anything. The module does not participate in the hotkey, Picker or paste path.

RC1 is the release-candidate baseline for the 0.7.0 release line. Core 0.6.5 Final RC1 remains the frozen Core baseline.

Alpha.2 also gives the Picker search field and category filter the same 40 px height, with vertically centered text, selected values, and drop-down items. The styles are local to the Picker and do not alter editor/settings controls.

Alpha.5 completes the Windows 7-inspired desktop pass with a shared classic title bar on every window, including minimize, maximize/restore, and close controls. Typography, buttons, fields, tabs, cards, and list rows are more compact. The Picker search and filter remain equal at 34 px with vertically centered content. The custom ComboBox template now correctly honors `DisplayMemberPath`, so filters and hotkey choices show their human-readable labels instead of record/debug representations. The changes remain isolated to XAML and do not alter keyboard behavior, storage, modules, or Core logic.

Alpha.6 reduces the left padding of text fields and asks WPF to decode the Picker header image directly at 32 px, avoiding an unnecessary ICO rescale. The 16 px title-bar icon is rendered on device pixels.

## Build verification
Run `build-and-test.cmd`. The build is compiled in Release with warnings treated as errors and then dependency-free self-tests run.

See `BETA-TEST-CHECKLIST.md` for the RC1 Windows regression checklist.

## Compact portable build
Run `build-portable.cmd`. It first runs the build/self-test gate, then creates a self-contained single-file Windows x64 package under `artifacts` containing:
- `GeniaText.exe`
- `GeniaText.portable`

Keep both files together. Portable user data is created next to the EXE.
