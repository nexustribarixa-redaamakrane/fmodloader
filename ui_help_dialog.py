"""
ui_help_dialog.py
Help / Manual dialog for fModLoader.
Describes features, open-source status, beta notice, and community call-to-action.
"""

from PyQt6.QtWidgets import (
    QDialog, QVBoxLayout, QHBoxLayout, QLabel,
    QPushButton, QScrollArea, QWidget, QFrame
)
from PyQt6.QtGui import (
    QFont, QPainter, QColor, QLinearGradient, QBrush,
    QPen, QDesktopServices, QCursor
)
from PyQt6.QtCore import Qt, QUrl
import math


HELP_SECTIONS = [
    {
        "title": "📖 What is fModLoader?",
        "body": (
            "fModLoader is an open-source tool for dynamically loading and swapping "
            "font glyphs on a per-codepoint basis. It allows you to apply custom SVG "
            "glyph definitions onto any compatible ('modcompat') font, letting you "
            "preview and use modified glyphs system-wide instantly.\n\n"
            "⚠️  This project is currently in its BETA stages because it is vibecoded — "
            "meaning it was rapidly prototyped with heavy AI assistance. It works, but "
            "it needs YOU — the community — to help refine, test, and expand it!"
        ),
    },
    {
        "title": "🗂️ Supported File Types",
        "body": (
            "• .ttfm / .otfm  — Font Mod packages (ZIP archives containing SVG glyphs "
            "and a metadata.json describing the glyph map and em-box metrics).\n\n"
            "• .modcompat.ttf / .modcompat.otf  — Mod-compatible base fonts. These are "
            "standard font files with a special marker injected into the OS/2 table "
            "(vendor ID = 'FMOD'). They remain fully readable by the OS and all apps.\n\n"
            "⛔  JAR files are NOT supported and will be rejected."
        ),
    },
    {
        "title": "🎛️ How to Use",
        "body": (
            "1. Select Font File — Browse or choose from the dropdown a .modcompat font.\n"
            "2. Select Mod File  — Browse or choose a .ttfm or .otfm mod package.\n"
            "3. Click APPLY     — The app will extract SVGs from the mod and patch "
            "them into the target font's codepoints using fontTools.\n"
            "4. Revert          — Select 'No Mod' in the Mod dropdown and click APPLY "
            "to restore the original glyphs from the automatic backup.\n\n"
            "Tip: Use Tools → Create Modcompat Font to convert any font into a "
            "mod-compatible version."
        ),
    },
    {
        "title": "🛠️ Tools Menu",
        "body": (
            "• Create Modcompat Font — Takes any TTF/OTF and injects the FMOD vendor "
            "ID marker, saving it as a .modcompat.ttf/otf file.\n\n"
            "• Create Demo Mod — Generates a sample .ttfm file so you can see how "
            "mod packages are structured.\n\n"
            "• Open Mods Directory / Open Fonts Directory — Quick access to the "
            "default scan folders."
        ),
    },
    {
        "title": "🌐 Open Source & Community",
        "body": (
            "fModLoader is free and open-source software released under the "
            "GNU General Public License v3 (GPL-3.0). The source code lives on GitHub.\n\n"
            "We NEED community help! Whether you're a font engineer, Python developer, "
            "UI designer, or just an enthusiastic tester — your contribution matters. "
            "Please:\n"
            "  • Report bugs via GitHub Issues\n"
            "  • Submit pull requests\n"
            "  • Join the Community Discord\n"
            "  • Spread the word!\n\n"
            "Thank you for being part of the font-mod community."
        ),
    },
]


class HelpDialog(QDialog):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle("fModLoader — Help & Manual")
        self.setMinimumSize(580, 520)
        self.resize(620, 560)
        self._build_ui()

    def _build_ui(self):
        main_layout = QVBoxLayout(self)
        main_layout.setContentsMargins(0, 0, 0, 0)
        main_layout.setSpacing(0)

        # ── Header ───────────────────────────────────────────────────────────
        header = QWidget()
        header.setFixedHeight(64)
        header.setStyleSheet("""
            background: qlineargradient(x1:0,y1:0,x2:1,y2:0,
                stop:0 #8b0000, stop:0.5 #cc1a1a, stop:1 #8b0000);
        """)
        h_lay = QHBoxLayout(header)
        h_lay.setContentsMargins(20, 0, 20, 0)

        icon_lbl = QLabel("f")
        icon_lbl.setStyleSheet("""
            color: white;
            font-size: 28px;
            font-family: Georgia, serif;
            font-style: italic;
            font-weight: bold;
        """)
        title_lbl = QLabel("fModLoader  —  Help & Manual")
        title_lbl.setStyleSheet("""
            color: white;
            font-size: 17px;
            font-family: 'Segoe UI', Arial, sans-serif;
            font-weight: bold;
        """)
        beta_lbl = QLabel("BETA")
        beta_lbl.setStyleSheet("""
            color: #ffeeaa;
            background: rgba(0,0,0,80);
            border: 1px solid #ffeeaa;
            border-radius: 3px;
            padding: 2px 8px;
            font-size: 10px;
            font-family: 'Segoe UI', Arial;
            font-weight: bold;
        """)

        h_lay.addWidget(icon_lbl)
        h_lay.addSpacing(10)
        h_lay.addWidget(title_lbl)
        h_lay.addStretch()
        h_lay.addWidget(beta_lbl)

        main_layout.addWidget(header)

        # ── Beta warning banner ───────────────────────────────────────────────
        warn = QLabel(
            "⚠️  This application is in active BETA. It is vibecoded and needs community contributions to mature."
        )
        warn.setStyleSheet("""
            background: #fffbe6;
            color: #7a4f00;
            font-size: 11px;
            font-family: 'Segoe UI', Arial;
            padding: 6px 16px;
            border-bottom: 1px solid #f0d080;
        """)
        warn.setWordWrap(True)
        main_layout.addWidget(warn)

        # ── Scrollable content ────────────────────────────────────────────────
        scroll = QScrollArea()
        scroll.setWidgetResizable(True)
        scroll.setStyleSheet("""
            QScrollArea { border: none; background: #fafafa; }
            QScrollBar:vertical {
                background: #eeeeee; width: 10px; border-radius: 5px;
            }
            QScrollBar::handle:vertical {
                background: #cc1a1a; border-radius: 5px; min-height: 30px;
            }
        """)

        content = QWidget()
        content.setStyleSheet("background: #fafafa;")
        c_lay = QVBoxLayout(content)
        c_lay.setContentsMargins(24, 20, 24, 20)
        c_lay.setSpacing(20)

        for section in HELP_SECTIONS:
            # Section title
            stitle = QLabel(section["title"])
            stitle.setStyleSheet("""
                color: #8b0000;
                font-size: 14px;
                font-family: 'Segoe UI', Arial, sans-serif;
                font-weight: bold;
                padding-bottom: 2px;
                border-bottom: 2px solid #cc1a1a;
            """)
            c_lay.addWidget(stitle)

            # Section body
            sbody = QLabel(section["body"])
            sbody.setWordWrap(True)
            sbody.setStyleSheet("""
                color: #333333;
                font-size: 12px;
                font-family: 'Segoe UI', Arial, sans-serif;
                line-height: 1.6;
                padding-left: 8px;
            """)
            c_lay.addWidget(sbody)

        c_lay.addStretch()
        scroll.setWidget(content)
        main_layout.addWidget(scroll, 1)

        # ── Footer buttons ────────────────────────────────────────────────────
        footer = QWidget()
        footer.setFixedHeight(52)
        footer.setStyleSheet("background: #f0f0f0; border-top: 1px solid #dddddd;")
        f_lay = QHBoxLayout(footer)
        f_lay.setContentsMargins(16, 8, 16, 8)
        f_lay.setSpacing(10)

        def make_footer_btn(text, url="", primary=False):
            btn = QPushButton(text)
            btn.setCursor(QCursor(Qt.CursorShape.PointingHandCursor))
            if primary:
                btn.setStyleSheet("""
                    QPushButton {
                        background: qlineargradient(x1:0,y1:0,x2:0,y2:1,
                            stop:0 #cc1a1a, stop:1 #8b0000);
                        color: white;
                        border: 1px solid #6b0000;
                        border-radius: 5px;
                        padding: 6px 18px;
                        font-weight: bold;
                        font-family: 'Segoe UI', Arial;
                        font-size: 12px;
                    }
                    QPushButton:hover { background: #dd2222; }
                    QPushButton:pressed { background: #7a0000; }
                """)
            else:
                btn.setStyleSheet("""
                    QPushButton {
                        background: white;
                        color: #333;
                        border: 1px solid #cccccc;
                        border-radius: 5px;
                        padding: 6px 14px;
                        font-family: 'Segoe UI', Arial;
                        font-size: 12px;
                    }
                    QPushButton:hover { background: #f5f5f5; border-color: #999; }
                    QPushButton:pressed { background: #eeeeee; }
                """)
            if url:
                btn.clicked.connect(lambda: QDesktopServices.openUrl(QUrl(url)))
            return btn

        btn_github = make_footer_btn("GitHub Repository",
                                     "https://github.com/fmod-loader/fmodloader")
        btn_discord = make_footer_btn("Community Discord",
                                      "https://discord.gg/fmodloader")
        btn_close = make_footer_btn("Close", primary=True)
        btn_close.clicked.connect(self.accept)

        f_lay.addWidget(btn_github)
        f_lay.addWidget(btn_discord)
        f_lay.addStretch()
        f_lay.addWidget(btn_close)

        main_layout.addWidget(footer)
