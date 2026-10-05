# GeniaText 0.6.5 final release checklist

This release candidate intentionally freezes new feature work. The goal is stability.

## Automated gate
1. Run `build-and-test.cmd`.
2. Required result: 0 build errors, 0 warnings (warnings are errors), all self-tests PASS.
3. Run `build-portable.cmd`. The portable ZIP and matching `.sha256` file must be created.

## Core regression (all optional modules OFF)
- Ctrl+Space opens Picker from Notepad, browser text field, messenger/editor used in daily work.
- Up/Down changes selection one item at a time.
- Enter inserts selected phrase.
- Space inserts when search is empty.
- Space remains a normal character after a search term has been entered.
- Esc closes Picker without changing the target field.
- Previous clipboard content is restored after successful insertion.
- If automatic paste fails, phrase stays in clipboard for manual Ctrl+V.
- Rapid repeated activation does not start overlapping paste transactions.
- Editor buttons react to a single normal click.
- Move Up/Down and drag/drop keep deterministic phrase order after restart.
- Import/export round-trip preserves text/category/favorite/order.
- Portable mode keeps phrases/settings/backups/log next to EXE.

## Optional modules (enable ONE AT A TIME)
- Smart templates: `{date}`, `{time}`, `{datetime}` expand only when enabled.
- Advanced search: multi-term and typo cases; no change when disabled.
- Usage history: successful insert updates Recent; failed insert does not.
- App profiles: mapped process selects category; unmapped process falls back to All.
- Compatibility mode: test only against an app where standard focus restoration is unstable.
- Advanced backups: verify periodic backup and retention.

## Resilience
- Corrupt `phrases.json`: original is quarantined and app starts with defaults.
- Lock/read-deny `phrases.json`: app must not overwrite the inaccessible source.
- Corrupt `settings.json`: original is quarantined; defaults are used.
- Lock/read-deny `settings.json`: app starts with defaults but MUST NOT overwrite the original.
- Very large settings/phrase database: load is refused without replacing the source.
- Disconnect/reconnect a monitor and restart the app.
- Sleep/resume Windows, then test Ctrl+Space and paste.
- Restart Explorer; tray/hotkey behavior should remain usable (tray icon may be recreated only on app restart depending on shell behavior).

## Release
- Keep `.pdb` out of the public package.
- Keep `GeniaText.portable` beside `GeniaText.exe` in the portable build.
- Publish the `.sha256` value/file alongside the ZIP.
- Authenticode signing remains recommended for a public release.

Only after this checklist passes should 0.6.5 be renamed from release candidate to final.
