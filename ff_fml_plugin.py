"""
ff_fml_plugin.py
FontForge Extension Script for fModLoader.

Instructions: 
Place this heavily commented script inside your ~/.FontForge/python/ directory.
This plugin registers 5 custom export options in the FontForge 'File' menu.
"""

try:
    import fontforge  # type: ignore
except ImportError:
    class fontforge:
        @staticmethod
        def logWarning(msg): pass
        @staticmethod
        def registerMenuItem(*args): pass

def export_modcompat_ttf(font, *args):
    # Mirrors standard .ttf export
    out_path = font.fontname + ".modcompat.ttf"
    font.generate(out_path)
    fontforge.logWarning(f"Generated {out_path}")

def export_modcompat_otf(font, *args):
    # Mirrors standard .otf export
    out_path = font.fontname + ".modcompat.otf"
    font.generate(out_path)
    fontforge.logWarning(f"Generated {out_path}")

def export_modcompat_ttc(font, *args):
    # Mirrors standard .ttc export logic (requires multiple fonts theoretically)
    # This is a stub for the TTC wrapper
    fontforge.logWarning("Stub: Generating .modcompat.ttc")

def export_ttfm_mod(font, *args):
    # Custom pipeline for .ttfm (UFO-like .glif format packaging)
    fontforge.logWarning("Stub: Generating .ttfm font mod based on active glyphs.")

def export_otfm_mod(font, *args):
    # Custom pipeline for .otfm
    fontforge.logWarning("Stub: Generating .otfm font mod based on active glyphs.")

# --- Register Menu Items ---
# fontforge.registerMenuItem( 
#     macro_func, 
#     setup_func, 
#     shortcut, 
#     menu_name, 
#     modifier
# )

fontforge.registerMenuItem(
    export_modcompat_ttf,
    None,
    None,
    "Font",
    None,
    "Generate .modcompat.ttf..."
)

fontforge.registerMenuItem(
    export_modcompat_otf,
    None,
    None,
    "Font",
    None,
    "Generate .modcompat.otf..."
)

fontforge.registerMenuItem(
    export_modcompat_ttc,
    None,
    None,
    "Font",
    None,
    "Generate .modcompat.ttc..."
)

fontforge.registerMenuItem(
    export_ttfm_mod,
    None,
    None,
    "Font",
    None,
    "Generate .ttfm Mod..."
)

fontforge.registerMenuItem(
    export_otfm_mod,
    None,
    None,
    "Font",
    None,
    "Generate .otfm Mod..."
)
