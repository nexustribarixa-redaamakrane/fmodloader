"""
glyph_model.py
Data model for the Glyph Editor: PathNode, GlyphContour, GlyphData, ModProject.
"""

import json
import zipfile
from copy import deepcopy
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional


@dataclass
class PathNode:
    x: float
    y: float
    cp_in: Optional[tuple] = None   # (x, y) incoming control point
    cp_out: Optional[tuple] = None  # (x, y) outgoing control point
    smooth: bool = False

    def copy(self):
        return deepcopy(self)


@dataclass
class GlyphContour:
    nodes: list = field(default_factory=list)  # list[PathNode]
    closed: bool = False

    def to_svg_commands(self) -> str:
        """Convert contour to SVG path d-attribute commands."""
        if not self.nodes:
            return ""
        cmds = []
        n = self.nodes
        first = n[0]
        cmds.append(f"M {first.x:.2f} {first.y:.2f}")

        for i in range(1, len(n)):
            prev = n[i - 1]
            curr = n[i]
            p_out = prev.cp_out
            c_in = curr.cp_in
            if p_out is None and c_in is None:
                cmds.append(f"L {curr.x:.2f} {curr.y:.2f}")
            else:
                ox, oy = p_out if p_out else (prev.x, prev.y)
                ix, iy = c_in if c_in else (curr.x, curr.y)
                cmds.append(f"C {ox:.2f} {oy:.2f} {ix:.2f} {iy:.2f} {curr.x:.2f} {curr.y:.2f}")

        if self.closed and len(n) > 1:
            last = n[-1]
            first = n[0]
            p_out = last.cp_out
            c_in = first.cp_in
            if p_out is None and c_in is None:
                cmds.append("Z")
            else:
                ox, oy = p_out if p_out else (last.x, last.y)
                ix, iy = c_in if c_in else (first.x, first.y)
                cmds.append(f"C {ox:.2f} {oy:.2f} {ix:.2f} {iy:.2f} {first.x:.2f} {first.y:.2f} Z")

        return " ".join(cmds)

    def copy(self):
        return deepcopy(self)


class GlyphData:
    def __init__(self):
        self.contours: list[GlyphContour] = []

    def to_svg_path(self) -> str:
        return " ".join(c.to_svg_commands() for c in self.contours if c.nodes)

    def to_svg_string(self, units_per_em: int = 1000, ascender: int = 800,
                      descender: int = -200) -> str:
        vb_h = units_per_em
        path_d = self.to_svg_path()
        if not path_d:
            path_d = ""
        return (
            f'<svg xmlns="http://www.w3.org/2000/svg" '
            f'viewBox="0 {descender} {units_per_em} {vb_h}">\n'
            f'  <path d="{path_d}"/>\n'
            f'</svg>'
        )

    def is_empty(self) -> bool:
        return all(len(c.nodes) == 0 for c in self.contours)

    def copy(self):
        return deepcopy(self)


class ModProject:
    """Holds a full mod in progress: metadata + per-codepoint GlyphData."""

    def __init__(self):
        self.name = "My Mod"
        self.version = "1.0"
        self.author = ""
        self.description = ""
        self.target_family = "Any"
        self.units_per_em = 1000
        self.ascender = 800
        self.descender = -200
        self.x_height = 500
        self.cap_height = 700
        # codepoint (int) -> GlyphData
        self.glyphs: dict[int, GlyphData] = {}

    # ── Glyph management ─────────────────────────────────────────────────────

    def add_glyph(self, codepoint: int) -> GlyphData:
        if codepoint not in self.glyphs:
            self.glyphs[codepoint] = GlyphData()
        return self.glyphs[codepoint]

    def remove_glyph(self, codepoint: int):
        self.glyphs.pop(codepoint, None)

    def get_glyph(self, codepoint: int) -> GlyphData | None:
        return self.glyphs.get(codepoint)

    # ── Serialisation ─────────────────────────────────────────────────────────

    def _build_metadata(self) -> dict:
        glyph_map = {}
        for cp, glyph in self.glyphs.items():
            svg_filename = f"glyphs/U{cp:04X}.svg"
            glyph_map[f"0x{cp:04X}"] = svg_filename
        return {
            "name": self.name,
            "version": self.version,
            "author": self.author,
            "description": self.description,
            "target_family": self.target_family,
            "em_box": {
                "units_per_em": self.units_per_em,
                "ascender": self.ascender,
                "descender": self.descender,
                "x_height": self.x_height,
                "cap_height": self.cap_height,
            },
            "glyph_map": glyph_map,
        }

    def save(self, path: str) -> tuple[bool, str]:
        try:
            meta = self._build_metadata()
            with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as zf:
                zf.writestr("metadata.json", json.dumps(meta, indent=2))
                for cp, glyph in self.glyphs.items():
                    svg = glyph.to_svg_string(
                        self.units_per_em, self.ascender, self.descender
                    )
                    zf.writestr(f"glyphs/U{cp:04X}.svg", svg)
            return True, f"Saved {len(self.glyphs)} glyph(s) to {Path(path).name}"
        except Exception as e:
            return False, str(e)

    def load(self, path: str) -> tuple[bool, str]:
        """Load an existing .ttfm/.otfm back into this project for editing."""
        try:
            with zipfile.ZipFile(path, "r") as zf:
                if "metadata.json" not in zf.namelist():
                    return False, "No metadata.json in archive."
                meta = json.loads(zf.read("metadata.json").decode())
                self.name = meta.get("name", self.name)
                self.version = meta.get("version", self.version)
                self.author = meta.get("author", self.author)
                self.description = meta.get("description", self.description)
                self.target_family = meta.get("target_family", self.target_family)
                em = meta.get("em_box", {})
                self.units_per_em = em.get("units_per_em", self.units_per_em)
                self.ascender = em.get("ascender", self.ascender)
                self.descender = em.get("descender", self.descender)
                self.x_height = em.get("x_height", self.x_height)
                self.cap_height = em.get("cap_height", self.cap_height)
                # We can't reconstruct stroke data from SVG paths (lossy),
                # but we at least know which codepoints exist
                glyph_map = meta.get("glyph_map", {})
                for cp_str in glyph_map:
                    cp = int(cp_str, 16) if cp_str.startswith("0x") else int(cp_str)
                    self.glyphs[cp] = GlyphData()  # empty – SVG not re-parsed
            return True, f"Loaded {len(self.glyphs)} glyph slot(s)."
        except Exception as e:
            return False, str(e)
