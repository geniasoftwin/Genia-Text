# GeniaText 0.7.0

Free Windows utility for fast insertion of user-created text phrases.

Core workflow:

`Ctrl+Space -> choose phrase -> Enter or Space -> paste into the original field`

## Download

Current stable release: **GeniaText 0.7.0**.

Download `GeniaText-0.7.0-portable-win-x64.zip` from the GitHub **Releases** page.

Portable package SHA-256:

`94F43BB6330F326030502A1E5C85211BBE40B4CA54F7D881EAD213E6E38E3786`

The portable build is self-contained for Windows x64 and does not require a separate .NET runtime installation.

## Free 0.7.0 baseline

All features included in GeniaText 0.7.0 are available free of charge, without activation, licensing checks, or feature restrictions.

GeniaText 0.7.0 remains the free functional baseline of the project. If paid Pro functionality is introduced in future versions, it is intended to consist of new capabilities rather than removing features already available in 0.7.0.

## Main features

- Global hotkey and fast phrase Picker.
- Paste into the previously active application.
- Categories and Favorites.
- Standard phrase editor.
- Editor Pro with batch operations.
- Exact duplicate detection.
- Advanced search.
- Smart templates.
- Import and export.
- Recycle bin and phrase recovery.
- Backup browser and recovery tools.
- Safe import preview.
- Optional modules controlled from **Настройки -> Возможности**.

## Portable mode

The portable package contains exactly two files:

- `GeniaText.exe`
- `GeniaText.portable`

Keep both files together.

User data is created next to `GeniaText.exe`, making the package suitable for a removable drive or a self-contained folder.

## Quick start

1. Extract the portable ZIP to a folder.
2. Keep `GeniaText.exe` and `GeniaText.portable` together.
3. Run `GeniaText.exe`.
4. Press `Ctrl+Space` to open the phrase Picker.
5. Select a phrase and press Enter or Space to insert it into the original field.

## Build verification

Run:

`build-and-test.cmd`

The Release build is compiled with warnings treated as errors and then runs the dependency-free self-test suite.

Current 0.7.0 release gate:

**18 passed, 0 failed.**

## Portable build

Run:

`build-portable.cmd`

The script runs the build/self-test gate first and then creates the self-contained Windows x64 portable package under `artifacts`.

## Project status

GeniaText 0.7.0 is the stable baseline for the 0.7 release line.

The frozen Core 0.6.5 Final RC1 baseline remains unchanged.

Release tag:

`v0.7.0`

Release commit:

`982bd95ec969d7da4902cf2e7ea672a16eed1495`

## Documentation

- `CHANGELOG.md` — release and development history.
- `ARCHITECTURE.md` — architecture overview.
- `BETA-TEST-CHECKLIST.md` — Windows release test checklist.
- `PORTABLE-BUILD.md` — portable build details.
- `SECURITY-AUDIT.md` — security audit notes.
- `STATIC-VALIDATION.md` — static validation.
- `TEST-PLAN.md` — test plan.

## Source-code license

The prebuilt GeniaText 0.7.0 application is available free of charge.

No open-source license has been selected for the source code. Public availability of this repository does not by itself grant permission to use, modify, or redistribute the source code.
