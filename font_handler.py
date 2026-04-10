"""
font_handler.py
Handles reading, validating, patching, and restoring .modcompat.ttf/.otf fonts.
Uses fontTools for all binary font operations.
"""

import os
import shutil
import json
from pathlib import Path

try:
    from fontTools.ttLib import TTFont
    FONTTOOLS_AVAILABLE = True
except ImportError:
    FONTTOOLS_AVAILABLE = False

# Tag used to mark a font as mod-compatible in the OS/2 table (vendor ID field)
MODCOMPAT_VENDOR_ID = "FMOD"
BACKUP_SUFFIX = ".fml_backup"


def is_fonttools_available() -> bool:
    return FONTTOOLS_AVAILABLE


def is_modcompat_font(font_path: str) -> bool:
    """
    Check if a font file is a valid .modcompat font by:
    1. Checking filename extension convention.
    2. Reading the OS/2 table vendor ID to confirm FMOD marker.
    """
    if not FONTTOOLS_AVAILABLE:
        return False
    path = Path(font_path)
    name = path.name.lower()
    if ".modcompat.ttf" not in name and ".modcompat.otf" not in name:
        return False
    try:
        font = TTFont(font_path)
        os2 = font.get("OS/2")
        if os2 and hasattr(os2, "achVendID"):
            return os2.achVendID == MODCOMPAT_VENDOR_ID
        font.close()
    except Exception:
        pass
    return False


def get_font_info(font_path: str) -> dict:
    """Return basic metadata from a font file."""
    if not FONTTOOLS_AVAILABLE:
        return {}
    info = {}
    try:
        font = TTFont(font_path)
        name_table = font.get("name")
        if name_table:
            for record in name_table.names:
                if record.nameID == 1:
                    info["family"] = record.toUnicode()
                elif record.nameID == 2:
                    info["style"] = record.toUnicode()
                elif record.nameID == 4:
                    info["full_name"] = record.toUnicode()
        os2 = font.get("OS/2")
        if os2:
            info["vendor_id"] = getattr(os2, "achVendID", "")
            info["x_height"] = getattr(os2, "sxHeight", None)
            info["cap_height"] = getattr(os2, "sCapHeight", None)
            info["ascender"] = getattr(os2, "sTypoAscender", None)
            info["descender"] = getattr(os2, "sTypoDescender", None)
        head = font.get("head")
        if head:
            info["units_per_em"] = head.unitsPerEm
        info["glyph_count"] = len(font.getGlyphOrder())
        font.close()
    except Exception as e:
        info["error"] = str(e)
    return info


def create_modcompat_font(source_path: str, output_path: str) -> bool:
    """
    Convert a standard TTF/OTF into a modcompat font by injecting the FMOD
    vendor ID into its OS/2 table and saving with the .modcompat extension.
    """
    if not FONTTOOLS_AVAILABLE:
        return False
    try:
        font = TTFont(source_path)
        os2 = font.get("OS/2")
        if os2:
            os2.achVendID = MODCOMPAT_VENDOR_ID
        font.save(output_path)
        font.close()
        return True
    except Exception as e:
        print(f"[FontHandler] Error creating modcompat font: {e}")
        return False


def backup_font(font_path: str) -> str | None:
    """
    Create a backup of the font before patching.
    Returns the backup path or None on failure.
    """
    backup_path = font_path + BACKUP_SUFFIX
    if os.path.exists(backup_path):
        return backup_path  # backup already exists
    try:
        shutil.copy2(font_path, backup_path)
        return backup_path
    except Exception as e:
        print(f"[FontHandler] Backup failed: {e}")
        return None


def restore_font(font_path: str) -> bool:
    """
    Restore a font from its backup, overwriting the patched version.
    """
    backup_path = font_path + BACKUP_SUFFIX
    if not os.path.exists(backup_path):
        return False
    try:
        shutil.copy2(backup_path, font_path)
        return True
    except Exception as e:
        print(f"[FontHandler] Restore failed: {e}")
        return False


def has_backup(font_path: str) -> bool:
    return os.path.exists(font_path + BACKUP_SUFFIX)


def apply_mod_glyphs(font_path: str, glyph_map: dict) -> tuple[bool, str]:
    """
    Apply SVG glyphs from a mod to a modcompat font.

    glyph_map: dict mapping unicode codepoint (int) -> SVG string content
    Returns (success: bool, message: str)
    """
    if not FONTTOOLS_AVAILABLE:
        return False, "fontTools is not installed."
    try:
        font = TTFont(font_path)
        cmap = font.getBestCmap()
        if cmap is None:
            font.close()
            return False, "Font has no usable cmap table."

        glyf_table = font.get("glyf")
        is_otf = "CFF " in font or "CFF2" in font

        patched = 0
        for codepoint, svg_data in glyph_map.items():
            cp = int(codepoint)
            glyph_name = cmap.get(cp)
            if glyph_name is None:
                # Add new glyph name
                glyph_name = f"uni{cp:04X}"

            # For SVG glyphs, we inject them as SVG table entries
            if "SVG " not in font:
                _inject_svg_table(font, cp, svg_data, glyph_name)
            else:
                svg_table = font["SVG "]
                # Append or replace SVG document
                glyph_order = font.getGlyphOrder()
                glyph_idx = glyph_order.index(glyph_name) if glyph_name in glyph_order else len(glyph_order)
                existing = {doc[1]: doc for doc in svg_table.docList}
                existing[glyph_idx] = (svg_data.encode("utf-8"), glyph_idx, glyph_idx)
                svg_table.docList = list(existing.values())
            patched += 1

        font.save(font_path)
        font.close()
        return True, f"Successfully patched {patched} glyph(s)."
    except Exception as e:
        return False, f"Error applying mod: {e}"


def _inject_svg_table(font: "TTFont", codepoint: int, svg_data: str, glyph_name: str):
    """Inject a new SVG table into a font with one document."""
    from fontTools.ttLib.tables.S_V_G_ import table_S_V_G_
    svg_table = table_S_V_G_()
    glyph_order = font.getGlyphOrder()
    if glyph_name not in glyph_order:
        font.setGlyphOrder(glyph_order + [glyph_name])
        glyph_idx = len(glyph_order)
    else:
        glyph_idx = glyph_order.index(glyph_name)
    svg_table.docList = [(svg_data.encode("utf-8"), glyph_idx, glyph_idx)]
    font["SVG "] = svg_table


def scan_for_modcompat_fonts(directories: list[str]) -> list[str]:
    """
    Scan a list of directories for .modcompat.ttf and .modcompat.otf files.
    Returns a list of absolute paths.
    """
    found = []
    for d in directories:
        if not os.path.isdir(d):
            continue
        for root, _, files in os.walk(d):
            for f in files:
                name = f.lower()
                if ".modcompat.ttf" in name or ".modcompat.otf" in name:
                    full = os.path.join(root, f)
                    found.append(full)
    return sorted(found)
