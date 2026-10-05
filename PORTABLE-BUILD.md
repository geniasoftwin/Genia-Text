# GeniaText 0.7.0-beta.1 compact portable build

Run `build-portable.cmd` on Windows with the .NET 10 SDK installed.

The script first builds the entire solution with warnings treated as errors and runs `GeniaText.Tests`. Publishing is aborted if that gate fails.

Then it publishes `win-x64`, self-contained, single-file, without trimming and without public PDB symbols. The package contains exactly:

- `GeniaText.exe`
- `GeniaText.portable`

On first run the program creates `phrases.json`, `settings.json`, `Backups`, and `geniatext.log` beside the EXE.
