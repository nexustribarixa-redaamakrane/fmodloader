# fModLoader (FML)

<div align="center">

![fModLoader Banner](https://img.shields.io/badge/fModLoader-v1.0.65%20BETA-cc1a1a?style=for-the-badge&logo=cplusplus&logoColor=white)
![License](https://img.shields.io/badge/license-GPL--3.0-blue?style=for-the-badge)
![C++](https://img.shields.io/badge/C%2B%2B-AvaloniaUI-blue?style=for-the-badge)
![Status](https://img.shields.io/badge/status-BETA-orange?style=for-the-badge)

**A sleek, open-source desktop utility for dynamic font glyph modification.**
Built with a secure C++ (AvaloniaUI) architecture.

*Open-source • Vibecoded • Community-driven*

</div>

---

## 📋 Table of Contents

- [About](#about)
- [Changelog](#changelog)
- [Features](#features)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Usage](#usage)
- [Keyboard Shortcuts](#keyboard-shortcuts)
- [Contributing](#contributing)
- [License](#license)

---

## About

fModLoader (FML) is an open-source desktop application for dynamic font glyph modification on Windows. It allows font artists, modders, and developers to create, edit, and apply custom glyph modifications onto compatible font files — all without requiring commercial font editors.

> ⚠️ **BETA Notice:** This project is community-vibecoded and actively evolving. Please report bugs via [GitHub Issues](https://github.com/nexustribarixa-redaamakrane/fmodloader/issues).

---

## Changelog

### v1.0.65 BETA — "Project Horde" *(current)*

**🆕 Changes**
- Version bump for BETA-v1.0.65-beta

### v1.0.4 BETA — "Project Vectoris"

**🆕 New Features**
- **Built-in Font Editor** — Full FontForge-inspired glyph editor window with:
  - Responsive glyph grid with real-time vector preview for every codepoint
  - Vector drawing canvas (GlyphCanvas) with 13 professional tools
  - Bezier Pen with smooth/corner anchor toggling (double-click)
  - Bezier control point handles with mirror-smooth mode
  - Knife tool with bezier intersection detection
  - Ruler tool with real-time distance & angle readout
  - Rectangle, Ellipse, and Spiral shape tools
  - Freehand Pencil tool
  - Node editor with drag handles
  - Zoom (scroll wheel + dedicated tool) and Pan (Space/H)
- **GlyphData Vector Model** — New `GlyphData` model with `PathNode`, `GlyphContour`, `GlyphData`, and `ModProject` classes providing clean separation between UI and data
- **ModProject Serialization** — Full `.ttfm`/`.otfm` ZIP-based mod save/load with `metadata.json` and per-glyph SVG export
- **Advanced File Dialog** — Native file picker with:
  - Bookmark panel (quick-access saved paths)
  - Back/Forward/Up navigation history
  - Dynamic filter bar for file type selection
  - Rename-in-place support
- **SVG Import & Apply** — Import any SVG file and apply it directly to a selected glyph cell in the canvas editor
- **Script Execution Environment** — Built-in code editor supporting Python, FontForge `.pe`, and Cython script execution
- **Codepoint Range Manager** — Add named Unicode ranges (Basic Latin, PUA, SPUA-A/B, etc.) or custom hex ranges to the editor grid
- **Glyph Info Dialog** — View and edit Unicode value, glyph name, and comments per glyph
- **Comprehensive Keyboard Shortcuts** — Full shortcut coverage across all windows (see [Keyboard Shortcuts](#keyboard-shortcuts))
- **Global Exception Handler** — Unhandled exceptions now display a styled diagnostic report dialog instead of silently crashing
- **CLI Helper** — Added `make-modcompat` command for batch font conversion
- **FontForge Plugin** — Added `ff_fml_plugin.py` skeleton for external use
- Improved mod handler ZIP extraction and metadata validation

**🐛 Bug Fixes**
- Fixed DPI-related point-size warnings across all views — replaced all dynamic font sizing with pixel-size based rendering to be DPI-safe
- Fixed invalid escape sequence warnings in the CLI helper docstring
- Fixed crash when opening Glyph Canvas via "Apply SVG to Selected" — parent control was incorrectly passed as `glyph_data` argument
- Fixed canvas clear causing a dangling native pointer crash — `_clear_all()` now removes items individually, preserving the canvas path item
- Fixed invalid pen arguments in `GlyphCellWidget` paint routine causing silent rendering failure
- Fixed double-invocation of the canvas dialog on glyph double-click — removed redundant event connection
- Fixed `save_to_glyph_data(None)` crash in `GlyphCanvasDialog.done()` — now guarded with `if self.glyph_data is not None`

---

### v1.0.1 BETA — "Project Aurion" *(initial public release)*

**Features**
- Dynamic font glyph modding engine
- Strict dual-file dependency system (`.modcompat` + `.ttfm`/`.otfm`)
- Modern AvaloniaUI with animated hazard tape banner and heartbeat status line
- Custom animated splash screen
- Mod scanning and application via `FontHandlerService` + `ModHandlerService`
- `create_modcompat_font()` — converts any TTF/OTF to a mod-compatible font with `FMOD` vendor ID marker
- Backup/restore system for safe font patching
- About and Help dialogs
- Global exception handler

---

## Features

| Feature | v1.0.1 | v1.0.65 |
|---|:---:|:---:|
| Apply font mods (.ttfm/.otfm) | ✅ | ✅ |
| Create modcompat fonts | ✅ | ✅ |
| Backup & revert system | ✅ | ✅ |
| About / Help dialogs | ✅ | ✅ |
| Animated UI (heartbeat, hazard tape) | ✅ | ✅ |
| Built-in FontForge-style editor | ❌ | ✅ |
| Vector drawing canvas (13 tools) | ❌ | ✅ |
| GlyphData model & SVG export | ❌ | ✅ |
| ModProject save/load (.ttfm) | ❌ | ✅ |
| Advanced file dialog | ❌ | ✅ |
| SVG import & apply | ❌ | ✅ |
| Script execution environment | ❌ | ✅ |
| Codepoint range manager | ❌ | ✅ |
| Comprehensive keyboard shortcuts | ❌ | ✅ |
| DPI-safe font rendering | ❌ | ✅ |

---

## Architecture

```
fmodloader/
├── app/                        # AvaloniaUI desktop application (entry point, views, view models)
│   ├── Program.cs              # Entry point: app init, splash screen, global exception handler
│   ├── Views/                  # MainWindow, FontEditorView, AboutDialog, HelpDialog
│   ├── ViewModels/             # MVVM logic for all windows
│   ├── Controls/               # HazardTape, HeartbeatLine, SurgicalPatternOverlay
│   └── Themes/                 # Colors, Shapes, Typography resource dictionaries
├── core/                       # fModLoader.Core library (backend engine)
│   ├── Models/                 # FontTarget, GlyphData, ModMetadata
│   └── Services/               # FontHandlerService, ModHandlerService,
│                               # BackupService, FontDiscoveryService, SvgPathParser
├── cli/                        # fModLoader_CLI: terminal UI + batch commands
├── installer/                  # Windows installer
├── fonts/                      # Default scan directory for .modcompat fonts
├── mods/                       # Default scan directory for .ttfm/.otfm mods
├── icons/                      # SVG tool icons for the glyph editor toolbar
└── assets/                     # Application icon and shared resources
```

---

## Prerequisites

- **C++ / AvaloniaUI** — Application framework & UI
- **.NET SDK 6.0, 7.0 or 8.0** — Build toolchain

```bash
dotnet restore fml.sln
```

---

## Installation

```bash
# 1. Clone the repository
git clone https://github.com/nexustribarixa-redaamakrane/fmodloader.git
cd fmodloader

# 2. Restore dependencies and build
dotnet build fml.sln

# 3. (Optional) Convert fonts to modcompat format via the CLI
dotnet run --project cli -- make-modcompat            # converts popular Windows fonts
dotnet run --project cli -- make-modcompat --all-windows  # converts ALL fonts in C:\Windows\Fonts
```

---

## Usage

```bash
dotnet run --project app
```

### Workflow

1. **Select Font File** — Pick a `.modcompat.ttf` or `.modcompat.otf` font from the dropdown or browse
2. **Select Mod File** — Pick a `.ttfm` or `.otfm` mod package
3. **Click APPLY** (or press `Enter`) — Patches glyphs into the font
4. **Revert** — Select `— No Mod (Revert) —` and click APPLY to restore the backup

### Creating Mods

1. **File → Open Glyph / Font Editor** (`Ctrl+E`)
2. Double-click any glyph cell to open the canvas editor
3. Draw using the tool panel or import an SVG (`Ctrl+Shift+I`)
4. Close the canvas (saves automatically)
5. **File → Export as .ttfm** (`Ctrl+Shift+E`) to package your mod

---

## Keyboard Shortcuts

### Main Window
| Shortcut | Action |
|---|---|
| `Enter` | Apply mod |
| `Ctrl+E` | Open Font Editor |
| `Ctrl+B` | Browse font file |
| `Ctrl+Shift+B` | Browse mod file |
| `Ctrl+R` | Refresh font list |
| `Ctrl+Shift+M` | Refresh mod list |
| `Ctrl+Shift+C` | Create modcompat font |
| `Ctrl+Shift+D` | Create demo mod |
| `Ctrl+,` | Preferences |
| `F1` | Help & Manual |
| `Ctrl+Shift+A` | About |

### Font Editor Window
| Shortcut | Action |
|---|---|
| `Ctrl+N` | New project |
| `Ctrl+S` | Save project |
| `Ctrl+Shift+E` | Export as .ttfm |
| `Ctrl+Alt+E` | Export as .otfm |
| `Ctrl+Shift+S` | Save as .modcompat.ttc |
| `Ctrl+W` | Close editor |
| `Ctrl+Z / Ctrl+Y` | Undo / Redo |
| `Ctrl+I` | Glyph Info |
| `Ctrl+Shift+R` | Add codepoint range |
| `Ctrl+Shift+I` | Import SVG |
| `Ctrl+Shift+P` | Apply SVG to selected |
| `Ctrl+Shift+X` | Execute script |
| `Ctrl+=` / `Ctrl+-` | Zoom in / out |

### Glyph Canvas (Drawing Tools)
| Key | Tool |
|---|---|
| `V` or `F1` | Pointer / Select |
| `P` | Bezier Pen |
| `B` | Freehand Pencil |
| `N` | Edit Nodes |
| `Z` | Zoom |
| `H` or `Space` | Pan |
| `K` | Knife |
| `R` | Rectangle |
| `E` | Ellipse |
| `S` | Spiral |
| `C` | Corner/Anchor |
| `M` | Ruler |
| `Del` / `Backspace` | Delete selected |
| `Esc` | Close canvas |
| `Ctrl+Return` | Execute script (Script Editor) |

---

## Platform Packages

Prebuilt binaries are produced by CI (`.github/workflows/build-platforms.yml`) on every `BETA-v*` tag:

| Platform | Formats |
|---|---|
| **Windows** | `.exe` installer, `win-x64` self-contained binaries |
| **Linux** | `.deb`, `.rpm`, `.AppImage`, `tar.xz` (x64 + arm64), Flatpak, Snap |
| **macOS** | `.app` bundle + `.dmg` (x64 + arm64) |
| **FreeBSD / Unix** | self-contained `tar.xz` or framework-dependent bundle |

Build locally on any host:

```bash
bash scripts/build-all.sh          # auto-detects OS
bash scripts/build-linux.sh        # tar/deb/rpm/AppImage
bash scripts/build-macos.sh        # .app + .dmg
bash scripts/build-bsd.sh          # FreeBSD / generic Unix
```

---

## Contributing

Created affectionately by **Nexus Tribarixa** ([@nexustribarixa-redaamakrane](https://github.com/nexustribarixa-redaamakrane)).

We actively need community help! Whether you're a font engineer, C++ developer, UI designer, or enthusiastic tester — your contribution matters:

- 🐛 [Report bugs](https://github.com/nexustribarixa-redaamakrane/fmodloader/issues)
- 🔀 Submit pull requests
- ⭐ Star the repo to show support
- 📢 Spread the word!

---

## License

This project is licensed under the **GNU General Public License v3.0 (GPL-3.0)**.
See [LICENSE](LICENSE) for details.

---

<div align="center">

*Open-source • Vibecoded • Community-driven*

**fModLoader v1.0.65 BETA — "Project Horde"**

</div>
