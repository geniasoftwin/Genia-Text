# Recovery notes — GeniaText 0.6.2 → maintained 0.6.5 codebase

## Основание восстановления

В исходной release-сборке были найдены `GeniaText.dll`, `GeniaText.exe`, `GeniaText.pdb`, `.deps.json`, `.runtimeconfig.json`. Portable PDB сохранил имена исходных файлов и локальных переменных. Основная логика находится в managed `GeniaText.dll`; `GeniaText.exe` является apphost.

Восстановленная исходная структура соответствует данным PDB:

- `App.xaml(.cs)`
- `Models/AppSettings.cs`, `Models/PhraseEntry.cs`
- `Services/ApplicationPaths.cs`
- `Services/AppSettingsStore.cs`
- `Services/AutostartService.cs`
- `Services/GlobalHotkey.cs`
- `Services/PasteService.cs`
- `Services/PhraseStore.cs`
- `PhraseEditorWindow.xaml(.cs)`
- `PickerWindow.xaml(.cs)`
- `SettingsWindow.xaml(.cs)`

## Подтверждённые параметры 0.6.2

- Target: `net10.0-windows`, .NET 10, WPF, C# 14, Release, AnyCPU.
- Default hotkey: modifiers `0x0002` (Ctrl), virtual key `0x20` (Space).
- Picker defaults: 680×520.
- Single-instance mutex: `Local\GeniaText.SingleInstance`.
- Portable marker: `GeniaText.portable`.
- Data: `phrases.json`, `settings.json`, `Backups`.
- Autostart: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, value `GeniaText`, migration from `QuickPhrase`.
- Hotkey registration uses `MOD_NOREPEAT` (`0x4000`).
- Phrase autosave debounce in 0.6.2: 600 ms (это уже было хорошо реализовано и сохранено).
- Backup retention in 0.6.2: 10 файлов.
- Clipboard set/restore retries: 5 attempts with short delays.

## Что нельзя восстановить побайтно

Компиляция уничтожает исходное форматирование, комментарии и часть синтаксического выбора разработчика. XAML внутри release хранится как BAML; интерфейс в этом проекте реконструирован по скриншотам и обнаруженным именам control/event-handler-ов, а не гарантированно побайтно декомпилирован.

Поэтому 0.6.3 следует считать **функционально восстановленной и улучшенной кодовой базой**, а не криптографически идентичным исходником 0.6.2.
