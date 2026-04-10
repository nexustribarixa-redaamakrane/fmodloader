"""
mod_handler.py
Handles reading, parsing, and extracting .ttfm and .otfm font mod packages.
These are ZIP archives containing a metadata JSON/XML and SVG glyph files.
"""

import os
import json
import zipfile
from pathlib import Path
from dataclasses import dataclass, field


@dataclass
class ModMetadata:
    name: str = "Unknown Mod"
    version: str = "1.0"
    author: str = "Unknown"
    description: str = ""
    target_family: str = ""
    em_box: dict = field(default_factory=dict)
    # Maps unicode codepoint (as str "0x0041") -> svg filename inside zip
    glyph_map: dict = field(default_factory=dict)
    file_path: str = ""

    def display_name(self) -> str:
        return f"{self.name} v{self.version} by {self.author}"


SUPPORTED_EXTENSIONS = (".ttfm", ".otfm")
METADATA_FILENAMES = ("metadata.json", "mod.json", "info.json", "metadata.xml")


def is_valid_mod_file(path: str) -> bool:
    """Check that the file has supported extension and is a valid ZIP."""
    return (
        path.lower().endswith(SUPPORTED_EXTENSIONS)
        and zipfile.is_zipfile(path)
    )


def load_mod(path: str) -> tuple[ModMetadata | None, str]:
    """
    Load and parse a .ttfm or .otfm mod package.
    Returns (ModMetadata, "") on success or (None, error_message) on failure.
    """
    if not is_valid_mod_file(path):
        return None, f"'{Path(path).name}' is not a valid .ttfm or .otfm mod file."

    try:
        with zipfile.ZipFile(path, "r") as zf:
            names = zf.namelist()

            # Find metadata file
            meta_file = None
            for candidate in METADATA_FILENAMES:
                if candidate in names:
                    meta_file = candidate
                    break

            if meta_file is None:
                return None, "No metadata file found inside mod archive."

            meta = ModMetadata(file_path=path)

            if meta_file.endswith(".json"):
                raw = zf.read(meta_file).decode("utf-8")
                data = json.loads(raw)
                meta.name = data.get("name", meta.name)
                meta.version = data.get("version", meta.version)
                meta.author = data.get("author", meta.author)
                meta.description = data.get("description", meta.description)
                meta.target_family = data.get("target_family", meta.target_family)
                meta.em_box = data.get("em_box", {})
                meta.glyph_map = data.get("glyph_map", {})
            elif meta_file.endswith(".xml"):
                meta = _parse_xml_metadata(zf.read(meta_file), path)

            return meta, ""

    except zipfile.BadZipFile:
        return None, "Archive is corrupted or not a valid ZIP."
    except json.JSONDecodeError as e:
        return None, f"Metadata JSON is malformed: {e}"
    except Exception as e:
        return None, f"Failed to load mod: {e}"


def _parse_xml_metadata(xml_bytes: bytes, file_path: str) -> ModMetadata:
    """Parse legacy XML metadata format into a ModMetadata object."""
    import xml.etree.ElementTree as ET
    meta = ModMetadata(file_path=file_path)
    try:
        root = ET.fromstring(xml_bytes.decode("utf-8"))
        meta.name = root.findtext("name", meta.name)
        meta.version = root.findtext("version", meta.version)
        meta.author = root.findtext("author", meta.author)
        meta.description = root.findtext("description", meta.description)
        meta.target_family = root.findtext("target_family", meta.target_family)

        em_el = root.find("em_box")
        if em_el is not None:
            meta.em_box = {child.tag: child.text for child in em_el}

        glyphs_el = root.find("glyph_map")
        if glyphs_el is not None:
            for glyph in glyphs_el.findall("glyph"):
                cp = glyph.get("codepoint", "")
                svg = glyph.get("svg", "")
                if cp and svg:
                    meta.glyph_map[cp] = svg
    except Exception:
        pass
    return meta


def extract_svgs(mod_path: str, glyph_map: dict) -> dict[int, str]:
    """
    Extract SVG content from a mod's ZIP archive based on glyph_map.
    glyph_map: {codepoint_str -> svg_filename}
    Returns: {codepoint_int -> svg_string}
    """
    result = {}
    if not is_valid_mod_file(mod_path):
        return result
    try:
        with zipfile.ZipFile(mod_path, "r") as zf:
            for cp_str, svg_filename in glyph_map.items():
                if svg_filename in zf.namelist():
                    svg_data = zf.read(svg_filename).decode("utf-8")
                    # Support "0x0041" hex or "65" decimal codepoint formats
                    if cp_str.startswith("0x") or cp_str.startswith("0X"):
                        cp_int = int(cp_str, 16)
                    else:
                        cp_int = int(cp_str)
                    result[cp_int] = svg_data
    except Exception as e:
        print(f"[ModHandler] SVG extraction error: {e}")
    return result


def list_mod_contents(mod_path: str) -> list[str]:
    """List all files inside a mod archive."""
    try:
        with zipfile.ZipFile(mod_path, "r") as zf:
            return zf.namelist()
    except Exception:
        return []


def scan_for_mods(directories: list[str]) -> list[str]:
    """
    Scan directories for .ttfm and .otfm files.
    Returns sorted list of absolute paths.
    """
    found = []
    for d in directories:
        if not os.path.isdir(d):
            continue
        for root, _, files in os.walk(d):
            for f in files:
                if f.lower().endswith(SUPPORTED_EXTENSIONS):
                    found.append(os.path.join(root, f))
    return sorted(found)


def create_demo_mod(output_path: str) -> bool:
    """
    Create a minimal example .ttfm file for testing purposes.
    Writes a ZIP with metadata.json and one SVG placeholder.
    """
    metadata = {
        "name": "Demo Mod",
        "version": "1.0",
        "author": "fModLoader Dev Team",
        "description": "A minimal demo mod for testing purposes.",
        "target_family": "Any",
        "em_box": {
            "x_height": 500,
            "ascender": 800,
            "descender": -200,
            "units_per_em": 1000
        },
        "glyph_map": {
            "0x0041": "glyphs/A.svg",
            "0x0042": "glyphs/B.svg"
        }
    }
    svg_a = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1000 1000">
  <path d="M 100 800 L 500 100 L 900 800 L 750 800 L 500 300 L 250 800 Z
           M 200 550 L 800 550 L 800 650 L 200 650 Z"/>
</svg>"""
    svg_b = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1000 1000">
  <path d="M 100 100 L 100 800 L 550 800 Q 800 800 800 600 Q 800 500 650 450
           Q 800 400 800 250 Q 800 100 550 100 Z
           M 250 450 L 250 680 L 520 680 Q 660 680 660 565 Q 660 450 520 450 Z
           M 250 200 L 250 380 L 510 380 Q 640 380 640 290 Q 640 200 510 200 Z"/>
</svg>"""
    try:
        with zipfile.ZipFile(output_path, "w", zipfile.ZIP_DEFLATED) as zf:
            zf.writestr("metadata.json", json.dumps(metadata, indent=2))
            zf.writestr("glyphs/A.svg", svg_a)
            zf.writestr("glyphs/B.svg", svg_b)
        return True
    except Exception as e:
        print(f"[ModHandler] Failed to create demo mod: {e}")
        return False
