"""
ff_fml_plugin.py
FontForge Extension Script for fModLoader.

Instructions: 
Place this script inside your ~/.FontForge/python/ directory.
This plugin registers custom export options under the 'Tools > fModLoader' menu in FontForge.
"""

import os
import json
import tempfile
import zipfile
import shutil

try:
    import fontforge  # type: ignore
except ImportError:
    class fontforge:
        @staticmethod
        def logWarning(msg):
            print(f"Log: {msg}")
        @staticmethod
        def registerMenuItem(*args):
            pass

def export_modcompat_ttf(font, *args):
    # Set the OS/2 vendor ID to FMOD to flag it as modcompat
    try:
        font.os2_vendor = "FMOD"
        out_path = font.fontname + ".modcompat.ttf"
        font.generate(out_path)
        fontforge.logWarning(f"Generated {out_path} with FMOD OS/2 vendor ID.")
    except Exception as e:
        fontforge.logWarning(f"Failed to generate .modcompat.ttf: {e}")

def export_modcompat_otf(font, *args):
    try:
        font.os2_vendor = "FMOD"
        out_path = font.fontname + ".modcompat.otf"
        font.generate(out_path)
        fontforge.logWarning(f"Generated {out_path} with FMOD OS/2 vendor ID.")
    except Exception as e:
        fontforge.logWarning(f"Failed to generate .modcompat.otf: {e}")

def export_modcompat_ttc(font, *args):
    try:
        font.os2_vendor = "FMOD"
        out_path = font.fontname + ".modcompat.ttc"
        font.generate(out_path)
        fontforge.logWarning(f"Generated {out_path} with FMOD OS/2 vendor ID.")
    except Exception as e:
        fontforge.logWarning(f"Failed to generate .modcompat.ttc: {e}")

def export_mod_package(font, extension=".ttfm"):
    # Get active/selected glyphs if any, otherwise all unicode glyphs
    glyphs_to_export = []
    try:
        selection = list(font.selection.byGlyphs)
        if len(selection) > 0:
            glyphs_to_export = selection
    except Exception:
        pass
        
    if not glyphs_to_export:
        for name in font:
            g = font[name]
            if g.unicode > 0:
                glyphs_to_export.append(g)

    if not glyphs_to_export:
        fontforge.logWarning("No glyphs found to export.")
        return

    temp_dir = tempfile.mkdtemp()
    glyph_map = {}

    try:
        for g in glyphs_to_export:
            if g.unicode <= 0:
                continue
            
            # Sanitize glyph name for safe filename
            clean_name = "".join([c if c.isalnum() else "_" for c in g.glyphname])
            svg_filename = f"{clean_name}.svg"
            svg_filepath = os.path.join(temp_dir, svg_filename)
            
            try:
                g.export(svg_filepath)
                codepoint_hex = f"0x{g.unicode:04X}"
                glyph_map[codepoint_hex] = svg_filename
            except Exception as e:
                fontforge.logWarning(f"Failed to export glyph {g.glyphname}: {e}")

        if not glyph_map:
            fontforge.logWarning("No valid glyphs with unicode codepoints were exported.")
            return

        # Prepare metadata json structure
        metadata = {
            "name": font.fontname + " Mod",
            "version": font.version if (hasattr(font, "version") and font.version) else "1.0",
            "author": font.copyright if (hasattr(font, "copyright") and font.copyright) else "Unknown",
            "description": "Generated via FontForge fModLoader Plugin",
            "target_family": font.familyname,
            "glyph_map": glyph_map
        }

        meta_filepath = os.path.join(temp_dir, "metadata.json")
        with open(meta_filepath, "w", encoding="utf-8") as f:
            json.dump(metadata, f, indent=2)

        # Create zip package
        out_path = font.fontname + extension
        with zipfile.ZipFile(out_path, 'w', zipfile.ZIP_DEFLATED) as zip_file:
            for root, _, files in os.walk(temp_dir):
                for file in files:
                    file_path = os.path.join(root, file)
                    arc_name = os.path.relpath(file_path, temp_dir)
                    zip_file.write(file_path, arc_name)

        fontforge.logWarning(f"Successfully generated mod package: {out_path} ({len(glyph_map)} glyphs)")
    finally:
        shutil.rmtree(temp_dir)

def export_ttfm_mod(font, *args):
    export_mod_package(font, ".ttfm")

def export_otfm_mod(font, *args):
    export_mod_package(font, ".otfm")

# --- Register Menu Items ---
fontforge.registerMenuItem(
    export_modcompat_ttf,
    None,
    None,
    "Font",
    None,
    "Tools > fModLoader > Generate .modcompat.ttf..."
)

fontforge.registerMenuItem(
    export_modcompat_otf,
    None,
    None,
    "Font",
    None,
    "Tools > fModLoader > Generate .modcompat.otf..."
)

fontforge.registerMenuItem(
    export_modcompat_ttc,
    None,
    None,
    "Font",
    None,
    "Tools > fModLoader > Generate .modcompat.ttc..."
)

fontforge.registerMenuItem(
    export_ttfm_mod,
    None,
    None,
    "Font",
    None,
    "Tools > fModLoader > Generate .ttfm Mod..."
)

fontforge.registerMenuItem(
    export_otfm_mod,
    None,
    None,
    "Font",
    None,
    "Tools > fModLoader > Generate .otfm Mod..."
)
