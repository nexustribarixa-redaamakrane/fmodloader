# fModLoader (FML) v1.0.1 Beta
**"Project Aurion"**

An open-source, community-driven desktop utility for dynamic font glyph modification. fModLoader (FML) uses a modular Python architecture (PyQt6 + fontTools) to provide secure backend font engineering paired with a sleek, automated UI.

---

## Features

- **Dynamic Font Modding:** Modify font glyphs on the fly using our custom engine.
- **Strict Dual-File Dependency & Format Support:** Full compatibility for creating `.modcompat` files and loading `.ttfm` / `.otfm` mods.
- **Modern UI/UX:** A robust Windows 11-style interface built on PyQt6, complete with a high-resolution custom splash screen.
- **System Integration:** Features reliable system-wide font updates.
- **Secure Backend Validation:** Utilizes `fontTools` for safe parsing and restructuring without risking corruption.

## Prerequisites

- Python 3.9 or newer
- Dependencies: `PyQt6`, `fontTools`

## Installation

1. **Clone or Download** this repository to your local machine.
2. **Install Required Libraries:**
   ```bash
   pip install PyQt6 fontTools
   ```

## Usage

To start fModLoader, run the main entry point from your terminal:

```bash
python main.py
```

### Core Architecture

- `main.py`: Entry point handling high-DPI scaling, the custom splash screen, and app initialization.
- `ui_main_window.py`: Contains the primary PyQt6 MainWindow implementation.
- `font_handler.py`: Houses the backend font processing logic (the actual modifying engine).

## Contributing

Created affectionately by the **font-mod Development Team**.
Contributions, issues, and feature requests are welcome!

---
*Open-source • Vibecoded • Community-driven*
