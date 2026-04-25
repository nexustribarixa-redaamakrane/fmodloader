"""
make_modcompat.py
Quick CLI helper to convert standard font(s) into .modcompat fonts for fModLoader.

Usage:
    python make_modcompat.py                         # converts popular Windows fonts
    python make_modcompat.py C:/path/to/myfont.ttf  # converts a specific font
    python make_modcompat.py --all-windows           # converts ALL fonts in C:\\Windows\\Fonts
"""

import sys
import shutil
from pathlib import Path
from font_handler import create_modcompat_font, is_fonttools_available

FONTS_DIR = Path(__file__).parent / "fonts"
FONTS_DIR.mkdir(exist_ok=True)

# A curated list of popular, commonly available Windows system fonts
POPULAR_WINDOWS_FONTS = [
    r"C:\Windows\Fonts\arial.ttf",
    r"C:\Windows\Fonts\arialbd.ttf",
    r"C:\Windows\Fonts\times.ttf",
    r"C:\Windows\Fonts\timesbd.ttf",
    r"C:\Windows\Fonts\cour.ttf",
    r"C:\Windows\Fonts\courbd.ttf",
    r"C:\Windows\Fonts\verdana.ttf",
    r"C:\Windows\Fonts\verdanab.ttf",
    r"C:\Windows\Fonts\georgia.ttf",
    r"C:\Windows\Fonts\georgiab.ttf",
    r"C:\Windows\Fonts\trebuc.ttf",
    r"C:\Windows\Fonts\calibri.ttf",
    r"C:\Windows\Fonts\calibrib.ttf",
    r"C:\Windows\Fonts\segoeui.ttf",
    r"C:\Windows\Fonts\segoeuib.ttf",
    r"C:\Windows\Fonts\comic.ttf",
    r"C:\Windows\Fonts\impact.ttf",
    r"C:\Windows\Fonts\tahoma.ttf",
]


def convert(src: str) -> bool:
    p = Path(src)
    if not p.exists():
        print(f"  [SKIP] Not found: {p.name}")
        return False

    suffix = ".modcompat.ttf" if p.suffix.lower() == ".ttf" else ".modcompat.otf"
    out = FONTS_DIR / (p.stem + suffix)

    if out.exists():
        print(f"  [EXISTS] {out.name}")
        return True

    ok = create_modcompat_font(src, str(out))
    if ok:
        print(f"  [OK]   {p.name}  ->  {out.name}")
    else:
        print(f"  [FAIL] {p.name}")
    return ok


def main():
    if not is_fonttools_available():
        print("ERROR: fontTools is not installed. Run: pip install fonttools")
        sys.exit(1)

    args = sys.argv[1:]

    if "--all-windows" in args:
        windows_fonts = Path(r"C:\Windows\Fonts")
        sources = list(windows_fonts.glob("*.ttf")) + list(windows_fonts.glob("*.otf"))
        print(f"Converting ALL {len(sources)} fonts from C:\\Windows\\Fonts ...\n")
    elif args:
        sources = [Path(a) for a in args if not a.startswith("--")]
        print(f"Converting {len(sources)} specified font(s)...\n")
    else:
        sources = [Path(f) for f in POPULAR_WINDOWS_FONTS]
        print("Converting popular Windows fonts (default set)...\n")
        print("TIP: Pass --all-windows to convert every font, or pass specific paths.\n")

    ok_count = 0
    for src in sources:
        if convert(str(src)):
            ok_count += 1

    print(f"\nDone. {ok_count}/{len(sources)} font(s) converted -> {FONTS_DIR}")
    print("Open fModLoader and they will appear in the 'Select Font File' dropdown.")


if __name__ == "__main__":
    main()
