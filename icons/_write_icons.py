"""
_write_icons.py
Creates Adwaita-style symbolic SVG icons for the fModLoader Glyph Editor toolbar.
Run once: python icons/_write_icons.py
"""
import os
os.chdir(os.path.dirname(os.path.abspath(__file__)))

ICONS = {
    "pointer.svg": """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <path fill="none" stroke="#2e3436" stroke-width="1.4" stroke-linejoin="round"
        d="M3 1 L3 12 L6 9 L8.5 14.5 L10 13.8 L7.5 8.3 L11.5 8.3 Z"/>
</svg>""",

    "magnify.svg": """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <circle fill="none" stroke="#2e3436" stroke-width="1.5" cx="6.5" cy="6.5" r="4.5"/>
  <line stroke="#2e3436" stroke-width="1.8" stroke-linecap="round" x1="10" y1="10" x2="14" y2="14"/>
  <line stroke="#2e3436" stroke-width="1.2" stroke-linecap="round" x1="4.5" y1="6.5" x2="8.5" y2="6.5"/>
  <line stroke="#2e3436" stroke-width="1.2" stroke-linecap="round" x1="6.5" y1="4.5" x2="6.5" y2="8.5"/>
</svg>""",

    "pencil.svg": """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <path fill="none" stroke="#2e3436" stroke-width="1.4" stroke-linejoin="round"
        d="M10.5 1.5 L14.5 5.5 L5 15 L1 15 L1 11 Z"/>
  <line stroke="#2e3436" stroke-width="1.2" x1="9" y1="3" x2="13" y2="7"/>
</svg>""",

    "pan.svg": """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <path fill="none" stroke="#2e3436" stroke-width="1.4" stroke-linejoin="round"
        d="M6 1 L6 4 M10 1 L10 4 M6 1 Q5 1 5 2 L5 9 Q4 9 4 8 L4 6 Q4 5 3 5 Q2 5 2 6 L2 10 Q2 13 5 14 L11 14 Q14 13 14 10 L14 6 Q14 5 13 5 Q12 5 12 6 L12 8 Q12 9 11 9 L11 2 Q11 1 10 1 L10 4 L6 4 L6 2 Q6 1 6 1Z"/>
</svg>""",

    "knife.svg": """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <path fill="none" stroke="#2e3436" stroke-width="1.4" stroke-linejoin="round"
        d="M2 14 L8 4 Q9 2 11 2 L14 2 L14 5 Q14 7 12 8 L2 14 Z"/>
  <line stroke="#2e3436" stroke-width="1.3" x1="10" y1="2" x2="14" y2="5"/>
</svg>""",

    "ruler.svg": """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <rect fill="none" stroke="#2e3436" stroke-width="1.4" x="1" y="5" width="14" height="6" rx="1"/>
  <line stroke="#2e3436" stroke-width="1.2" x1="4" y1="5" x2="4" y2="7.5"/>
  <line stroke="#2e3436" stroke-width="1.2" x1="7" y1="5" x2="7" y2="8"/>
  <line stroke="#2e3436" stroke-width="1.2" x1="10" y1="5" x2="10" y2="7.5"/>
  <line stroke="#2e3436" stroke-width="1.2" x1="13" y1="5" x2="13" y2="7.5"/>
</svg>""",

    "pen.svg": """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <path fill="none" stroke="#2e3436" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round"
        d="M1 14 Q4 8 8 8 Q12 8 15 2"/>
  <circle fill="#2e3436" cx="8" cy="8" r="1.4"/>
  <circle fill="none" stroke="#2e3436" stroke-width="1.2" cx="1" cy="14" r="1.2"/>
  <circle fill="none" stroke="#2e3436" stroke-width="1.2" cx="15" cy="2" r="1.2"/>
</svg>""",

    "spiral.svg": """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <path fill="none" stroke="#2e3436" stroke-width="1.4" stroke-linecap="round"
        d="M8 8 Q10 4 12 6 Q14 8 12 10 Q10 12 8 10 Q5 8 7 5 Q9 2 13 3 Q15 5 14 8"/>
</svg>""",

    "nodes.svg": """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <path fill="none" stroke="#2e3436" stroke-width="1.3" d="M2 12 Q6 4 14 4"/>
  <rect fill="#2e3436" x="1" y="11" width="2.5" height="2.5"/>
  <circle fill="#2e3436" cx="14" cy="4" r="1.4"/>
  <circle fill="none" stroke="#2e3436" stroke-width="1.2" cx="8" cy="7" r="1.4"/>
</svg>""",

    "corner.svg": """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <path fill="none" stroke="#2e3436" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round"
        d="M2 13 L8 3 L14 13"/>
  <circle fill="#2e3436" cx="8" cy="3" r="1.5"/>
  <circle fill="none" stroke="#2e3436" stroke-width="1.2" cx="2" cy="13" r="1.3"/>
  <circle fill="none" stroke="#2e3436" stroke-width="1.2" cx="14" cy="13" r="1.3"/>
</svg>""",

    "rectangle.svg": """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <rect fill="none" stroke="#2e3436" stroke-width="1.5" x="2" y="4" width="12" height="8" rx="1"/>
  <circle fill="#2e3436" cx="2" cy="4" r="1.2"/>
  <circle fill="#2e3436" cx="14" cy="4" r="1.2"/>
  <circle fill="#2e3436" cx="2" cy="12" r="1.2"/>
  <circle fill="#2e3436" cx="14" cy="12" r="1.2"/>
</svg>""",

    "ellipse.svg": """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <ellipse fill="none" stroke="#2e3436" stroke-width="1.5" cx="8" cy="8" rx="6" ry="4"/>
  <circle fill="#2e3436" cx="2" cy="8" r="1.2"/>
  <circle fill="#2e3436" cx="14" cy="8" r="1.2"/>
  <circle fill="#2e3436" cx="8" cy="4" r="1.2"/>
  <circle fill="#2e3436" cx="8" cy="12" r="1.2"/>
</svg>""",

    "add-layer.svg": """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <path fill="none" stroke="#2e3436" stroke-width="1.3" stroke-linejoin="round"
        d="M1 5 L8 2 L15 5 L8 8 Z"/>
  <path fill="none" stroke="#2e3436" stroke-width="1.3"
        d="M1 8 L8 11 L15 8 M1 11 L8 14 L15 11"/>
  <line stroke="#3584e4" stroke-width="1.8" stroke-linecap="round" x1="12" y1="13" x2="15" y2="13"/>
  <line stroke="#3584e4" stroke-width="1.8" stroke-linecap="round" x1="13.5" y1="11.5" x2="13.5" y2="14.5"/>
</svg>""",

    "guide.svg": """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <line stroke="#3584e4" stroke-width="1.4" stroke-dasharray="2,2" x1="8" y1="0" x2="8" y2="16"/>
  <line stroke="#3584e4" stroke-width="1.4" stroke-dasharray="2,2" x1="0" y1="8" x2="16" y2="8"/>
</svg>""",

    "layers.svg": """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <path fill="none" stroke="#2e3436" stroke-width="1.3" stroke-linejoin="round"
        d="M1 4 L8 1 L15 4 L8 7 Z"/>
  <path fill="none" stroke="#2e3436" stroke-width="1.3"
        d="M1 7 L8 10 L15 7"/>
  <path fill="none" stroke="#2e3436" stroke-width="1.3"
        d="M1 10 L8 13 L15 10"/>
</svg>""",
}

for fname, svg in ICONS.items():
    with open(fname, "w", encoding="utf-8") as f:
        f.write(svg)
    print(f"Written: {fname}")

print("Done.")
