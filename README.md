# fModLoader

fModLoader (FML) is a C# font-glyph mod editor and patching tool. The repository
contains an Avalonia desktop app (`app/`), shared font and archive code
(`core/`), a command-line client (`cli/`), and packaging scripts. The app and
CLI target .NET 6, 7, and 8; the installer projects have their own Windows
target frameworks.

## What the code does

- Reads SFNT font metadata and identifies a mod-compatible font by the
  `.modcompat.ttf`, `.modcompat.otf`, or `.modcompat.ttc` filename suffix and
  the `FMOD` vendor ID in its `OS/2` table.
- Creates and restores font backups through `BackupService`.
- Reads `.ttfm` and `.otfm` ZIP archives. The mod loader looks for
  `metadata.json`, `mod.json`, `info.json`, or `metadata.xml`; the metadata
  contains a glyph map and archive paths to glyph data.
- Represents editor outlines as `GlyphData`/`GlyphContour`/`PathNode`, parses
  SVG path data through `SvgPathParser`, and applies mapped glyph outlines
  through `FontHandlerService`.
- Provides GUI and batch entry points that share the same core services.

The core is a project-specific SFNT patcher, not a general font-authoring
library. In particular, a mod cannot be applied to an arbitrary font: the CLI
requires a font carrying the FML compatibility marker. Review and keep the
backup created before patching. The first TTC face is used for the metadata
inspection path.

## Build and run

Requirements: .NET SDK 6.0, 7.0, or 8.0. Avalonia packages are restored from
NuGet by `dotnet`.

From the repository root:

```powershell
dotnet restore app/fModLoader.csproj
dotnet build app/fModLoader.csproj -c Release
dotnet run --project app/fModLoader.csproj
```

Build and invoke the CLI independently:

```powershell
dotnet build cli/fModLoader_CLI.csproj -c Release
dotnet run --project cli/fModLoader_CLI.csproj -- --help
dotnet run --project cli/fModLoader_CLI.csproj -- scan-fonts
dotnet run --project cli/fModLoader_CLI.csproj -- scan-mods
```

The CLI also defines `info`, `mod-info`, `mod-list`, `apply`, `restore`,
`make-modcompat`, and `create-demo`. Run `--help` for the current argument
forms. `apply` takes a compatible font followed by one or more mod archives.
`make-modcompat` converts a source font to the marked format; it does not make
all font table layouts safe to patch.

There is no C# unit-test project in this solution. The `installer_smoke/`
project is a separate installer-oriented program, not a test suite for the
font patching code.

## Layout and packaging

- `app/` — Avalonia UI, views, view models, and editor controls.
- `core/Models/` — font targets, mod metadata, glyph data, and editor projects.
- `core/Services/` — font parsing/patching, mod archive handling, discovery,
  backups, and SVG path parsing.
- `cli/` — console commands for scanning, applying, inspecting, and creating
  compatible fonts or demo mods.
- `installer/`, `packaging/`, `scripts/` — installer and platform packaging.

`.github/workflows/build-platforms.yml` builds release packages when a tag is
pushed or the workflow is manually dispatched. It contains Linux, macOS,
FreeBSD, Windows, Flatpak, and Snap jobs; it is a packaging workflow, not a
cross-platform runtime test matrix. `ff_fml_plugin.py` is a separate plugin
script and is not the implementation of a Python/Cython/FontForge script
runtime inside the C# editor.
