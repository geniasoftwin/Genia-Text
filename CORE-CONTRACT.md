# GeniaText 0.7.0 Core contract

Baseline: verified `GeniaText-0.6.5-final-rc1-source.zip`.

## Invariants

1. Every optional feature flag defaults to `false`.
2. With all flags off, the user path remains `Ctrl+Space -> choose phrase -> Enter/Space -> paste`.
3. The standard paste timing and safety sequence remains unchanged: hide Picker, 80 ms settle, foreground request, 100 ms wait, process guard, `SendInput(Ctrl+V)`, 350 ms wait, guarded clipboard restore.
4. Optional editor features cannot call `PasteService`, register a hotkey, inspect a target process, or alter clipboard state.
5. Settings changes are additive and deserialize safely from a 0.6.5 `settings.json`.
6. The phrase JSON schema remains unchanged from 0.6.5 throughout the 0.7 alpha line.
7. A disabled module contributes no UI entry point outside Settings and performs no background work.
8. Module faults must not require changing the frozen Core files to disable the module.

## Frozen baseline files

The following files must retain their 0.6.5 Final RC1 SHA-256 values in every 0.7 alpha:

| File | SHA-256 |
| --- | --- |
| `GeniaText/Services/GlobalHotkey.cs` | `96bfcb1cb6880ae0de4b8d3e010f67f8cefcf0072656f32b4032de0862a1e630` |
| `GeniaText/Services/PasteService.cs` | `a8d1d636a5c2fd67aa17ed5dbbff278ba1e09a24005d424aa608cae477d93466` |
| `GeniaText/Services/PhraseStore.cs` | `1cf8d699cbbcaf086ea475aa3503108fdbc519b29b32341285ac7bf8854e55ab` |
| `GeniaText/Services/AppSettingsStore.cs` | `9b27dab0f861dfbdef31211c4ab9e30a2d503d8045a2ae86b7b6bb0143c44a84` |
| `GeniaText/PickerWindow.xaml.cs` | `34604dfc4e6d65e62aa64281d685f0e01b8c15fa803311cfd8909c6319573f47` |

Changes to these files require a dedicated Core bug-fix release, explicit regression rationale, and the complete Windows Core test matrix. They are outside the scope of optional modules and presentation-only releases.
