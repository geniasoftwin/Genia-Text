# GeniaText 0.7.0-beta.1 field-test checklist

Test on Windows 10 or 11 with the .NET 10 SDK. Use a copy of real user data, not the only copy.

## 1. Automated gate

1. Run `build-and-test.cmd`.
2. Require 0 build errors, 0 warnings, and all self-tests `PASS`.
3. Run `build-portable.cmd`.
4. Verify that the ZIP and matching `.sha256` file are created.

## 2. Upgrade safety

1. Copy the 0.6.5 `phrases.json` and `settings.json` into a disposable test folder.
2. Start beta.1 against that copy.
3. Confirm every optional module, including all three Safety Pack switches, is OFF unless it was explicitly stored as enabled.
4. Confirm phrases, order, categories, Favorites, and existing feature settings are preserved.

## 3. Core regression with every module OFF

- Run section B of `TEST-PLAN.md` in Notepad, a browser, a messenger/editor, and one Electron application.
- Verify Enter, Space, mouse double-click, clipboard restoration, rapid activation protection, phrase ordering, import/export, and portable storage.
- Treat any regression here as an beta blocker.

## 4. Editor Pro module

1. With Editor Pro OFF, open the standard phrase editor. The `Редактор Pro` button must not exist.
2. Enable Editor Pro in `Настройки -> Возможности`, save, and return to the already-open editor. The button must appear without an application restart.
3. Open Editor Pro. Select several phrases with Ctrl and with Shift.
4. Assign a new category. Close both editors, restart, and verify persistence.
5. Select several phrases and add them to Favorites. Remove them again. Verify that unselected phrases never change.
6. Enter an empty category and apply it; this must safely clear the category.
7. Create two phrases with identical text but CRLF/LF differences and outer spaces. `Найти и выделить дубли` must select both.
8. Create another phrase differing only by letter case. It must not join the duplicate group.
9. Confirm duplicate search reports results but never deletes or rewrites text.
10. Compare the same phrases in Editor Pro and the standard editor: each row must have the same compact one-line height, 12 px title text, separator, hover, and selection treatment. Category and Favorite markers must stay on the same line without clipping the title.
11. Disable Editor Pro. Its button must disappear; Core behavior must remain unchanged.

## 5. Safety Pack modules

Test each switch separately first, then all three together.

1. With all three switches OFF, confirm there are no `Корзина` or `Резервные копии` buttons, normal deletion uses the original confirmation, and Import uses the original Yes/No/Cancel dialog.
2. Enable only `Корзина удалённых фраз`. Delete a phrase, open `Корзина`, restore it, restart, and verify its text/category/favorite state. A colliding GUID must be replaced, not duplicated.
3. Make the data folder read-only and try to recycle a phrase. The operation must fail visibly and the live phrase must remain present.
4. Corrupt `trash.json`, restart, and open the recycle module. The source file must remain unchanged and trash writes must be blocked for that run.
5. Enable only `Просмотр резервных копий`. Verify copies display `dd.MM.yyyy HH:mm:ss`, reason, size, and bounded phrase previews. Create two copies in the same minute and verify the seconds distinguish them. Restore one phrase, merge a copy, and replace from a disposable copy.
6. Select a disposable backup and press `Удалить копию`. Cancel once and confirm the file remains; confirm once and verify only that selected `phrases-*.json` disappears, the list refreshes, and `phrases.json` is unchanged.
7. Delete or move a listed backup externally before applying it. Recovery must fail gracefully without changing the current base.
8. Enable only `Безопасный предпросмотр импорта`. Preview a file containing a new phrase, normalized exact repeat, duplicate GUID with different text, and an empty phrase. Counts and row labels must match.
9. Cancel the preview and verify `phrases.json` is byte-for-byte unchanged. Then test append and replace; both must still create/use the existing pre-import backup path.
10. Disable all three switches again. Both recovery entry-point buttons must disappear and the Core regression must remain unchanged.
11. In the trash-item preview, verify that `Название` and `Категория` are fully visible and vertically centered. The mouse wheel must not move text inside these two fields; long values may wrap and must be available in full through the tooltip.
12. Confirm the Recovery and Settings UI says `окно выбора фраз`; the unexplained word `Picker` must not appear in user-facing text.
13. Enable only the recycle bin with an empty `trash.json`: `Корзина` must be visible but disabled, and its disabled tooltip must say `Корзина пуста`.
14. Recycle one phrase: the already-open editor must enable `Корзина` immediately. Restore/remove the final trash entry and close Recovery: the button must become disabled again.
15. Enable only backup browsing: `Резервные копии` must be visible and enabled, `Корзина` absent, and the recovery window must open directly on the backup tab. With both modules enabled, both buttons must remain independent.
16. Enable both recovery modules, open `Корзина`, and switch repeatedly to `Резервные копии` and back. Repeat with an empty `Backups` folder, a vanished snapshot, a malformed snapshot, and an unreadable folder. GeniaText must remain running; expected failures must show a warning and be recorded without phrase text in `geniatext.log`.
17. From a fresh editor session, click `Резервные копии` directly 10 times (close/reopen each time), then repeat after corrupting one snapshot. The recovery window may show a warning, but the editor, tray icon, and global hotkey process must remain alive.
18. On direct open, the window must remain paintable/responsive while the list loads, and **no snapshot may be selected automatically**. The right side must say `Выберите резервную копию` after loading.
19. Click one backup. While `Загрузка копии…` is shown, move/resize the window and switch selection to another snapshot. The UI must remain responsive; stale preview work must be abandoned and only the current selection may populate the phrase list.
20. Confirm each backup row renders its reason and size without a warning about `TwoWay`, `OneWayToSource`, or the read-only `SizeLabel` property.

## 6. Phrase-management terminology

1. In the Picker, the top-right entry button must read `Фразы`, not `Редактор`.
2. Opening it must show the window title and heading `Управление фразами`.
3. The subtitle must describe editing, import, export, recovery, and settings; `Редактор Pro` remains unchanged.

## 7. Standard editor single-line fields

1. Enter `Нн Уу Дд Рр руда уют` in Picker search, editor title/category/body, Editor Pro category, Settings rules, and the available Recovery previews. No lower glyph segment may be clipped.
2. Repeat at 100%, 125%, and 150% display scaling; field borders and text baselines must remain crisp and complete.
3. In `Название` and `Категория`, type a long value, place the caret in the middle, type Space, pause for more than one second, and continue. Space must remain at the caret and the caret/view must not jump to the beginning.
4. Repeat with Space at the end, then click another phrase and Save. Restart and verify the intended value persists after normal outer-space trimming.
5. Hover each single-line field and rotate the mouse wheel in both directions. The text baseline must not move vertically.
6. Confirm caret movement, selection, typing, horizontal reveal of long text, and undo still work.
7. Confirm the multiline `Текст фразы` and Settings rules fields still scroll vertically.

## 8. Picker alignment

1. Check Windows display scaling at 100%, 125%, and 150%.
2. Type Cyrillic, Latin letters, digits, and punctuation into the search field.
3. Open the category filter and inspect both the selected value and every drop-down row.
4. Require equal 34 px control heights and optical vertical centering without top bias or clipped descenders. The search text should begin at the compact 5 px logical inset, not the earlier 9 px inset.
5. Require readable labels (`Все фразы`, category names) in the selected field and drop-down; any `FilterOption { ... }` text is a blocker.
6. Confirm that text boxes and combo boxes in Editor and Settings use the new compact sizing without clipping.
7. Verify the 32 px Picker header icon and 16 px title-bar icon stay crisp at each scale without selecting a visibly blurred oversized ICO frame.

## 9. Windows 7 theme

1. Inspect Picker, phrase editor, Settings, and Editor Pro at 100%, 125%, and 150% scaling.
2. Verify the same Windows 7-inspired palette, thin borders, Segoe UI text, and compact desktop proportions in every window.
3. Check normal, hover, keyboard-focus, pressed, selected, drop-down-open, and disabled states for buttons, text fields, combo boxes, check boxes, tabs, and phrase lists.
4. Require readable text, visible focus and selection, correctly positioned check marks and arrows, and no clipped borders or content.
5. Confirm every title strip supports dragging and double-click maximize/restore; minimize, maximize/restore, and close buttons must respond to the first click.
6. In Settings, verify that hotkey choices display `Ctrl`, `Alt`, `Shift`, key names, and `Space`; any `HotkeyOption { ... }` text is a blocker.

## 10. Stress and failure checks

Before stress testing, confirm contiguous list separators, visible selection/hover states, and no layout clipping.

- Repeat batch operations with 100+ selected phrases.
- Check responsiveness with 1,000+ phrases.
- Browse a backup containing 10,000 phrases and confirm the recovery window remains responsive; larger snapshots must be rejected.
- Make the data folder read-only and confirm failed saving is reported by the existing storage protections.
- Inspect `geniatext.log`: phrase and clipboard text must not be logged.

## Acceptance

Keep this build at alpha status until the automated gate, full Core regression, Editor Pro, and Safety Pack sections all pass on Windows. Record the Windows version, .NET SDK version, package SHA-256, and any failing step.
