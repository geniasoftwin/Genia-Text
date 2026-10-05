# GeniaText 0.7.0-RC1 hard test plan

The full 0.6.5 Core regression remains mandatory. Run it first with every optional module OFF, then test Editor Pro and each Safety Pack module separately using `BETA-TEST-CHECKLIST.md`.

## A. Automated gate — must be green before a build is used
Run `build-and-test.cmd`.

It performs:
1. `dotnet build GeniaText.sln -c Release -warnaserror`
2. dependency-free self-tests for optional-feature defaults, Editor Pro operations, Safety Pack classification/cloning/bounds, smart templates, advanced search, app-profile rules, settings JSON round-trip, and phrase display behavior.

Portable publishing runs this gate automatically too.

## B. Core regression — all optional modules OFF
This is the release blocker. Test with modules OFF first.

1. Notepad: cursor in text -> Ctrl+Space -> select with arrows -> Enter. Exactly one phrase is inserted.
2. Repeat with Space instead of Enter.
3. Repeat with mouse double-click and the Insert button.
4. Put unrelated text in clipboard before insertion. After successful insertion, Ctrl+V elsewhere must paste the original clipboard text.
5. Copy new text during the ~350 ms restore window. GeniaText must not overwrite that newer clipboard value.
6. Press Enter/Space rapidly several times. Only one insertion transaction should occur; Ctrl must never remain logically held.
7. Switch to another window immediately after choosing a phrase. Confirm no unexpected duplicate paste occurs.
8. Test Notepad, browser text field, messenger, Visual Studio/VS Code, and one Electron app if available.
9. Leave GeniaText in tray for several hours; sleep/resume Windows; repeat Ctrl+Space insertion.
10. Move/reorder phrases repeatedly; restart; order must persist exactly.
11. Add/edit/delete/duplicate phrases; restart; data must persist.
12. Export -> import append -> import replace; malformed JSON and oversized import must be rejected without destroying the current database.
13. Portable build: confirm phrases/settings/backups/log remain next to EXE and `%APPDATA%\GeniaText` is not created by a clean portable start.
14. At 100%, 125%, and 150% display scaling, type in the Picker search field and open the category filter. Both controls must remain 34 px high; search text, selected value, arrow, and drop-down rows must be vertically centered without clipping. The filter must show `Все фразы`, never the internal `FilterOption` record.
15. Inspect Picker, phrase editor, settings, and Editor Pro: the Windows 7-inspired styling must be consistent across buttons, fields, combo boxes, check boxes, tabs, and lists; no control may be clipped or lose its focus/selection indication.
16. Exercise normal, hover, keyboard-focus, pressed, selected, drop-down-open, and disabled states. Text must remain readable, the active state must remain visible, and no modern-style control may appear among the themed controls.
17. In every window, test title dragging, double-click maximize/restore, minimize, maximize/restore, close, edge resizing, and the red close-button hover/pressed states. Settings must remain usable after resizing to its minimum size.
18. In Settings, both hotkey ComboBoxes must show labels such as `Ctrl` and `Space`, never internal `HotkeyOption` records, in both the closed field and drop-down list.
19. Confirm the Picker search text uses the reduced left inset and that the 32 px header icon and 16 px title icon are crisp at 100%, 125%, and 150% scaling.
20. In the standard editor, rotate the mouse wheel over the single-line title and category fields: their text must not shift vertically. The multiline phrase-text field must retain vertical scrolling.
21. Across every TextBox surface, render Cyrillic/Latin capitals, lowercase text, and descenders at 100%, 125%, and 150% scaling. No lower glyph segment may be clipped.
22. In editor title/category fields, type Space at the middle and end of a long line, wait beyond the autosave interval, and continue typing. The caret and horizontal viewport must not reset to the beginning; Save/restart must preserve the committed text.

## C. Module tests — enable one module at a time
### Smart templates
- Phrase: `Сегодня {date}, время {time}` -> values are expanded only when module is enabled.
- Unknown `{client}` remains literal.
- Disable module -> the same phrase inserts tokens literally.

### Advanced search
- Search across title/category/text with multiple words.
- Confirm a small typo in a long word can match.
- Confirm short unrelated terms do not create broad false matches.
- Stress: 1000+ phrases; typing in search must remain responsive.

### Usage history
- Enable -> successful insert increments usage and creates/updates `Недавние`.
- Failed paste must not count as successful use.
- Disable -> no further counters/timestamps are updated and `Недавние` disappears.

### App profiles
Create categories `Web` and `Chat`, then rules:
`chrome=Web`
`telegram=Chat`
- Call GeniaText from each app and verify the mapped category is selected.
- Call from an unmapped app -> filter must fall back to `Все фразы`, not inherit the previous app.
- Disable module -> no automatic category switching.

### Extended compatibility
- Keep OFF when normal insertion works.
- Enable only for an app with focus instability and compare success rate.
- Ensure the module does not cause duplicate pastes.

### Extended backups
- Enable, edit phrases, wait across the 10-minute boundary and save again.
- Backups are created but never exceed 30.
- Disable -> core startup/import backups still remain available.

### Editor Pro
- Keep OFF first: the standard editor must match 0.6.5 and the Pro button must be absent.
- Enable it in Settings: the Pro button must appear without restarting the application.
- Ctrl/Shift-select several phrases, assign a category, close and restart; every selected phrase must retain the category.
- Add/remove several selected phrases from Favorites; unrelated phrases must not change.
- Duplicate detection must select every exact repeated text after CRLF/LF and outer-whitespace normalization.
- Text differing only by letter case must not be treated as identical.
- Duplicate detection must never delete or rewrite a phrase.
- Phrase rows must match the standard editor's compact one-line sizing and 12 px title text; category and Favorite markers remain on that same line.
- Disable the module: the Pro button disappears and Picker/paste behavior remains unchanged.

### Recycle bin
- OFF: deletion is the original direct editor operation and no trash file is read or written.
- ON: archive must complete before live removal. Simulate a read-only folder and a malformed/oversized `trash.json`; the live phrase must not be removed.
- Restore a phrase whose GUID is already present; the restored copy must receive a new non-empty GUID.
- Permanently delete one trash entry and empty the trash only after explicit confirmation.
- Verify title/category preview text is not vertically clipped and cannot be internally scrolled with the mouse wheel. Test empty, short, and long wrapped values.
- Verify the editor button is named `Корзина`, is disabled with an empty bin, enables immediately after recycle, and disables again after the final entry is restored/removed.
- An unreadable/corrupt bin must not masquerade as empty: keep the entry point available and show the fail-closed error in Recovery.

### Backup browser
- OFF: the editor does not enumerate `Backups` and has no recovery button unless the recycle-bin switch is independently ON.
- ON: list only `phrases-*.json` directly beneath the configured backup folder.
- Preview and restore a single phrase. Merge and replace a complete backup; the existing `PhraseStore.Import` validation and pre-import backup must still run.
- Cancel manual deletion once and verify the selected backup remains. Confirm deletion of a disposable copy and verify only that selected `phrases-*.json` disappears, the list reloads, and live `phrases.json` is byte-identical.
- Reject a moved path, missing file, >20 MB backup, >10,000 records, and oversized phrase fields without mutating the live base.
- Verify the independent `Резервные копии` button opens directly on the backup tab and remains usable when `Корзина` is disabled.
- With both modules enabled, open `Корзина` and select the backup tab repeatedly. Empty, missing, malformed, or unreadable backup data must produce an in-window warning/log entry and must never terminate GeniaText.

### Safe import preview
- OFF: Import retains the original Yes/No/Cancel flow.
- ON: preview a new phrase, exact text duplicate after CRLF/LF + outer-space normalization, GUID conflict, null entry, and whitespace-only text.
- Cancel after preview and compare the live collection/file with the pre-preview state; no mutation is allowed.
- Confirm append/replace still flow through `PhraseStore.Import`, including its 20 MB/10,000-record limits and GUID normalization.

## D. Security / abuse cases
- Import JSON with null entries, duplicate GUIDs, very long title/category/text, >10,000 records, >20 MB file.
- Malformed/oversized `trash.json`, backup path outside `Backups`, disappearing backup, >20 MB backup, and duplicate IDs during single-phrase restore.
- `settings.json` with missing `Features`, null `Features`, null rules, absurd picker dimensions.
- Read-only data folder / Program Files scenario.
- Clipboard temporarily locked by another process.
- Target window closed between Picker opening and insertion.
- Run GeniaText non-elevated and target app elevated: failure must be graceful; text may remain in clipboard for manual paste.
- Inspect `geniatext.log`: it must not contain phrase text or clipboard content.

## Release rule
The 0.7.0 RC1 build is accepted only if section A is green and section B has no regression. Optional module defects block promotion to Stable, but they must never break the core path with the module disabled.

## Final 0.6.5 gate additions
- First-run settings save before Picker has ever been moved/resized (covers NaN placement serialization).
- Deny read access to an existing settings.json: verify the file timestamp/content is unchanged after opening/closing settings and exiting.
- Put an oversized dummy settings.json / phrases.json in the data directory: verify GeniaText refuses to replace the source.
- Verify `build-portable.ps1` produces a `.sha256` file matching `Get-FileHash` for the ZIP.
