"""
ui_main_window.py
Main application window for fModLoader v1.0.4 Beta.
Matches the reference screenshot:
  - Yellow/black hazard tape banner ("UNDER CONSTRUCTION / BETA")
  - Red menu bar: File | Mods | Tools | Settings | Help
  - Light background with faint 'f' watermarks and heartbeat line
  - Left column: Select Font File dropdown + Browse button + disclaimer
  - Right column: Select Mod File dropdown + Browse button + disclaimer
  - Centre: Big red APPLY button with 'f' heart icon
  - Status bar with heartbeat animation
"""

import os
import math
from pathlib import Path

from PyQt6.QtWidgets import (
    QMainWindow, QWidget, QVBoxLayout, QHBoxLayout,
    QLabel, QPushButton, QComboBox, QFrame, QStatusBar,
    QMenuBar, QMenu, QMessageBox, QSizePolicy,
    QProgressDialog
)
from ui_file_dialog import FMLFileDialog
from PyQt6.QtGui import (
    QPainter, QColor, QLinearGradient, QBrush, QPen,
    QFont, QFontMetrics, QPainterPath, QCursor, QIcon,
    QPixmap, QTransform, QPolygonF
)
from PyQt6.QtCore import (
    Qt, QTimer, QPointF, QRectF, QSize, QThread, pyqtSignal
)

from font_handler import (
    scan_for_modcompat_fonts, is_modcompat_font, get_font_info,
    backup_font, restore_font, has_backup, apply_mod_glyphs,
    create_modcompat_font, is_fonttools_available
)
from mod_handler import (
    scan_for_mods, load_mod, extract_glifs, create_demo_mod, is_valid_mod_file
)
from ui_about_dialog import AboutDialog
from ui_help_dialog import HelpDialog
from ui_font_editor import FontEditorWindow


# ───────────────────────────── Directories ───────────────────────────────────
APP_DIR = Path(__file__).parent
MODS_DIR = APP_DIR / "mods"
FONTS_DIR = APP_DIR / "fonts"
for _d in (MODS_DIR, FONTS_DIR):
    _d.mkdir(exist_ok=True)


# ───────────────────────────── Hazard Tape Widget ────────────────────────────

class HazardTapeWidget(QWidget):
    """Draws animated under-construction hazard tape banner."""

    def __init__(self, parent=None):
        super().__init__(parent)
        self.setFixedHeight(44)
        self._offset = 0
        self._timer = QTimer(self)
        self._timer.timeout.connect(self._tick)
        self._timer.start(30)

    def _tick(self):
        self._offset = (self._offset + 1) % 120
        self.update()

    def paintEvent(self, event):
        p = QPainter(self)
        p.setRenderHint(QPainter.RenderHint.Antialiasing)
        w, h = self.width(), self.height()

        # Draw striped tape
        stripe_w = 60
        for i in range(-2, (w // stripe_w) + 4):
            x = i * stripe_w - self._offset
            poly = QPolygonF([
                QPointF(x, 0),
                QPointF(x + stripe_w, 0),
                QPointF(x + stripe_w - 20, h),
                QPointF(x - 20, h),
            ])
            color = QColor("#f5c518") if i % 2 == 0 else QColor("#1a1a1a")
            p.setBrush(QBrush(color))
            p.setPen(Qt.PenStyle.NoPen)
            p.drawPolygon(poly)

        # Overlay text on tape
        font = QFont("Arial Black")
        font.setPixelSize(11)
        font.setWeight(QFont.Weight.Black)
        font.setLetterSpacing(QFont.SpacingType.AbsoluteSpacing, 3)
        p.setFont(font)

        texts = ["UNDER CONSTRUCTION", "BETA", "UNDER CONSTRUCTION", "BETA",
                 "UNDER CONSTRUCTION", "BETA"]
        x = -self._offset * 2
        for txt in texts:
            p.setPen(QColor("#cc1a1a"))
            p.drawText(int(x) + 2, int(h * 0.72) + 1, txt)
            p.setPen(QColor("white"))
            p.drawText(int(x), int(h * 0.72), txt)
            fm = QFontMetrics(font)
            x += fm.horizontalAdvance(txt) + 30
            if x > w + 300:
                break
        p.end()


# ───────────────────────────── Heartbeat Line Widget ─────────────────────────

class HeartbeatWidget(QWidget):
    """Animated ECG/heartbeat line widget."""

    def __init__(self, parent=None, height=30, color="#cc1a1a", bg=None):
        super().__init__(parent)
        self.setFixedHeight(height)
        self._color = QColor(color)
        self._bg = bg
        self._phase = 0.0
        self._timer = QTimer(self)
        self._timer.timeout.connect(self._tick)
        self._timer.start(25)

    def _tick(self):
        self._phase = (self._phase + 0.04) % 1.0
        self.update()

    def paintEvent(self, event):
        p = QPainter(self)
        p.setRenderHint(QPainter.RenderHint.Antialiasing)
        w, h = self.width(), self.height()

        if self._bg:
            p.fillRect(self.rect(), self._bg)

        mid = h / 2
        p.setPen(QPen(self._color, 2))

        pts = []
        n = w
        for i in range(n + 1):
            t = (i / n + self._phase) % 1.0
            # Q-R-S spike window
            if 0.3 < t < 0.38:
                amp = math.sin((t - 0.3) / 0.08 * math.pi) * (h * 0.38)
            elif 0.38 < t < 0.44:
                amp = -math.sin((t - 0.38) / 0.06 * math.pi) * (h * 0.22)
            else:
                amp = math.sin(t * math.pi * 3) * 1.2
            pts.append(QPointF(float(i), mid - amp))

        for i in range(len(pts) - 1):
            p.drawLine(pts[i], pts[i + 1])
        p.end()


# ───────────────────────────── Red Apply Button ───────────────────────────────

class ApplyButton(QPushButton):
    """Large red gradient APPLY button with 'f' icon and heartbeat pulse."""

    def __init__(self, parent=None):
        super().__init__(parent)
        self.setText("APPLY")
        self.setFixedSize(240, 72)
        self.setCursor(QCursor(Qt.CursorShape.PointingHandCursor))
        self._hover = False
        self._pressed_state = False
        self._pulse = 0.0
        self._timer = QTimer(self)
        self._timer.timeout.connect(self._tick)
        self._timer.start(30)

    def _tick(self):
        self._pulse = (self._pulse + 0.05) % (2 * math.pi)
        self.update()

    def enterEvent(self, e):
        self._hover = True
        self.update()
        super().enterEvent(e)

    def leaveEvent(self, e):
        self._hover = False
        self.update()
        super().leaveEvent(e)

    def mousePressEvent(self, e):
        self._pressed_state = True
        self.update()
        super().mousePressEvent(e)

    def mouseReleaseEvent(self, e):
        self._pressed_state = False
        self.update()
        super().mouseReleaseEvent(e)

    def paintEvent(self, event):
        p = QPainter(self)
        p.setRenderHint(QPainter.RenderHint.Antialiasing)
        w, h = self.width(), self.height()

        # Glow pulse when idle
        glow_radius = int(8 + 4 * math.sin(self._pulse))
        if not self._pressed_state:
            glow = QLinearGradient(0, 0, 0, h + glow_radius * 2)
            glow.setColorAt(0, QColor(220, 30, 30, 60))
            glow.setColorAt(1, QColor(100, 0, 0, 0))
            p.setBrush(QBrush(glow))
            p.setPen(Qt.PenStyle.NoPen)
            glow_rect = QRectF(-glow_radius, -glow_radius,
                               w + glow_radius * 2, h + glow_radius * 2)
            p.drawRoundedRect(glow_rect, 14, 14)

        # Button body gradient
        if self._pressed_state:
            c1, c2 = QColor("#7a0000"), QColor("#500000")
        elif self._hover:
            c1, c2 = QColor("#ee2222"), QColor("#aa0000")
        else:
            c1, c2 = QColor("#dd1a1a"), QColor("#8b0000")

        grad = QLinearGradient(0, 0, 0, h)
        grad.setColorAt(0, c1)
        grad.setColorAt(0.5, c2)
        grad.setColorAt(1, QColor(c2.red() - 20, 0, 0))

        path = QPainterPath()
        path.addRoundedRect(QRectF(0, 0, w, h), 10, 10)
        p.setBrush(QBrush(grad))
        p.setPen(QPen(QColor("#ff4444") if self._hover else QColor("#cc0000"), 1.5))
        p.drawPath(path)

        # Top highlight
        hi_grad = QLinearGradient(0, 0, 0, h * 0.4)
        hi_grad.setColorAt(0, QColor(255, 255, 255, 50))
        hi_grad.setColorAt(1, QColor(255, 255, 255, 0))
        hi_path = QPainterPath()
        hi_path.addRoundedRect(QRectF(2, 2, w - 4, h * 0.4), 8, 8)
        p.setBrush(QBrush(hi_grad))
        p.setPen(Qt.PenStyle.NoPen)
        p.drawPath(hi_path)

        # Heartbeat mini line on button
        p.setPen(QPen(QColor(255, 255, 255, 160), 1.5))
        pts = []
        n = int(w * 0.4)
        x0 = int(w * 0.05)
        phase = self._pulse / (2 * math.pi)
        for i in range(n + 1):
            t = (i / n + phase) % 1.0
            x = x0 + i
            if 0.3 < t < 0.38:
                amp = math.sin((t - 0.3) / 0.08 * math.pi) * 8
            elif 0.38 < t < 0.44:
                amp = -math.sin((t - 0.38) / 0.06 * math.pi) * 5
            else:
                amp = math.sin(t * math.pi * 4) * 1
            pts.append(QPointF(float(x), h / 2 - amp))

        for i in range(len(pts) - 1):
            p.drawLine(pts[i], pts[i + 1])

        # 'f' icon circle
        circle_r = 22
        cx = int(w * 0.35)
        cy = h // 2
        p.setBrush(QBrush(QColor(255, 255, 255, 30)))
        p.setPen(QPen(QColor(255, 255, 255, 100), 1))
        p.drawEllipse(QPointF(cx, cy), circle_r, circle_r)

        f_font = QFont("Georgia")
        f_font.setPixelSize(18)
        f_font.setWeight(QFont.Weight.Bold)
        f_font.setItalic(True)
        p.setFont(f_font)
        p.setPen(QColor("white"))
        p.drawText(QRectF(cx - circle_r, cy - circle_r, circle_r * 2, circle_r * 2),
                   Qt.AlignmentFlag.AlignCenter, "f")

        # Label
        lbl_font = QFont("Arial Black")
        lbl_font.setPixelSize(18)
        lbl_font.setWeight(QFont.Weight.Black)
        lbl_font.setLetterSpacing(QFont.SpacingType.AbsoluteSpacing, 3)
        p.setFont(lbl_font)
        p.setPen(QColor("white"))
        p.drawText(QRectF(w * 0.45, 0, w * 0.52, h),
                   Qt.AlignmentFlag.AlignVCenter | Qt.AlignmentFlag.AlignLeft,
                   "APPLY")
        p.end()


# ───────────────────────────── Red Combo Box ──────────────────────────────────

COMBO_STYLE = """
QComboBox {
    background: white;
    color: #222;
    border: 1px solid #cccccc;
    border-radius: 4px;
    padding: 5px 10px;
    font-family: 'Segoe UI', Arial;
    font-size: 12px;
    min-height: 28px;
}
QComboBox:hover {
    border-color: #cc1a1a;
}
QComboBox::drop-down {
    subcontrol-origin: padding;
    subcontrol-position: top right;
    width: 28px;
    border-left: 1px solid #cccccc;
    border-top-right-radius: 4px;
    border-bottom-right-radius: 4px;
    background: qlineargradient(x1:0,y1:0,x2:0,y2:1,
        stop:0 #cc1a1a, stop:1 #8b0000);
}
QComboBox::down-arrow {
    image: none;
    border-left: 5px solid transparent;
    border-right: 5px solid transparent;
    border-top: 6px solid white;
    width: 0;
    height: 0;
}
QComboBox QAbstractItemView {
    background: white;
    border: 1px solid #cc1a1a;
    selection-background-color: #cc1a1a;
    selection-color: white;
    font-family: 'Segoe UI', Arial;
    font-size: 12px;
    outline: none;
}
"""

BROWSE_STYLE = """
QPushButton {
    background: qlineargradient(x1:0,y1:0,x2:0,y2:1,
        stop:0 #f5f5f5, stop:1 #dddddd);
    color: #333;
    border: 1px solid #aaaaaa;
    border-radius: 4px;
    padding: 5px 14px;
    font-family: 'Segoe UI', Arial;
    font-size: 12px;
    min-height: 28px;
}
QPushButton:hover {
    background: qlineargradient(x1:0,y1:0,x2:0,y2:1,
        stop:0 #ffffff, stop:1 #eeeeee);
    border-color: #cc1a1a;
}
QPushButton:pressed {
    background: #dddddd;
}
"""


# ───────────────────────────── Background Widget ──────────────────────────────

class MainBackground(QWidget):
    """Light background with faint 'f' watermarks and wide heartbeat line."""

    def paintEvent(self, event):
        p = QPainter(self)
        p.setRenderHint(QPainter.RenderHint.Antialiasing)
        w, h = self.width(), self.height()

        # Light gradient background
        grad = QLinearGradient(0, 0, 0, h)
        grad.setColorAt(0, QColor("#ffffff"))
        grad.setColorAt(1, QColor("#f5f5f5"))
        p.fillRect(self.rect(), QBrush(grad))

        # Faint 'f' watermarks
        wm_font = QFont("Georgia")
        wm_font.setPixelSize(80)
        wm_font.setWeight(QFont.Weight.Bold)
        wm_font.setItalic(True)
        p.setFont(wm_font)
        p.setPen(QColor(180, 0, 0, 14))
        positions = [(30, 200), (180, 100), (340, 280), (500, 120), (650, 310), (780, 90), (100, 380)]
        for x, y in positions:
            p.drawText(x, y, "f")

        # Wide heartbeat line watermark across middle
        p.setPen(QPen(QColor(180, 0, 0, 20), 2))
        mid_y = h * 0.55
        pts = []
        n = w
        for i in range(n + 1):
            t = i / n
            if 0.42 < t < 0.47:
                amp = math.sin((t - 0.42) / 0.05 * math.pi) * 22
            elif 0.47 < t < 0.51:
                amp = -math.sin((t - 0.47) / 0.04 * math.pi) * 12
            else:
                amp = math.sin(t * math.pi * 6) * 2
            pts.append(QPointF(float(i), mid_y - amp))

        for i in range(len(pts) - 1):
            p.drawLine(pts[i], pts[i + 1])

        p.end()


# ───────────────────────────── Apply Worker Thread ───────────────────────────

class ApplyWorker(QThread):
    progress = pyqtSignal(str)
    finished = pyqtSignal(bool, str)

    def __init__(self, font_path, mod_path, revert=False):
        super().__init__()
        self.font_path = font_path
        self.mod_path = mod_path
        self.revert = revert

    def run(self):
        if self.revert:
            self.progress.emit("Restoring original glyphs...")
            ok = restore_font(self.font_path)
            if ok:
                self.finished.emit(True, "Font restored to original successfully.")
            else:
                self.finished.emit(False, "No backup found — font has not been modified.")
            return

        self.progress.emit("Loading mod package...")
        meta, err = load_mod(self.mod_path)
        if meta is None:
            self.finished.emit(False, f"Mod load failed: {err}")
            return

        self.progress.emit("Backing up original font...")
        if not backup_font(self.font_path):
            self.finished.emit(False, "Unable to create backup. Aborting for safety.")
            return

        self.progress.emit("Extracting GLIF glyphs...")
        glif_map = extract_glifs(self.mod_path, meta.glif_map)
        if not glif_map:
            self.finished.emit(False, "No glyph data found in mod package.")
            return

        self.progress.emit(f"Patching {len(glif_map)} glyph(s) into font...")
        ok, msg = apply_mod_glyphs(self.font_path, glif_map)
        self.finished.emit(ok, msg)


# ───────────────────────────── Main Window ───────────────────────────────────

class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("fModLoader v1.0.4 Beta")
        self.setMinimumSize(800, 500)
        self.resize(920, 540)

        # Track current selections
        self._font_path: str | None = None
        self._mod_path: str | None = None
        self._worker: ApplyWorker | None = None
        self._untitled_counter = 0

        self._build_menu()
        self._build_ui()
        self._build_statusbar()
        self._refresh_font_list()
        self._refresh_mod_list()

    # ── Menu Bar ─────────────────────────────────────────────────────────────

    def _build_menu(self):
        mb = self.menuBar()
        mb.setStyleSheet("""
            QMenuBar {
                background: qlineargradient(x1:0,y1:0,x2:0,y2:1,
                    stop:0 #cc1a1a, stop:1 #8b0000);
                color: white;
                font-family: 'Segoe UI', Arial;
                font-size: 13px;
                padding: 2px;
                spacing: 4px;
            }
            QMenuBar::item {
                padding: 6px 14px;
                background: transparent;
            }
            QMenuBar::item:selected {
                background: rgba(255,255,255,30);
                border-radius: 4px;
            }
            QMenuBar::item:pressed {
                background: rgba(0,0,0,30);
            }
            QMenu {
                background: #ffffff;
                color: #222;
                border: 1px solid #cc1a1a;
                font-family: 'Segoe UI', Arial;
                font-size: 12px;
            }
            QMenu::item {
                padding: 7px 28px 7px 16px;
            }
            QMenu::item:selected {
                background: #cc1a1a;
                color: white;
            }
            QMenu::separator {
                height: 1px;
                background: #eeeeee;
                margin: 4px 10px;
            }
        """)

        # File
        file_menu = mb.addMenu("File")
        act_open_editor = file_menu.addAction("Open Glyph / Font Editor...", self._open_glyph_editor)
        act_open_editor.setShortcut("Ctrl+E")
        file_menu.addSeparator()
        act_exit = file_menu.addAction("Exit", self.close)
        act_exit.setShortcut("Alt+F4")

        # Mods
        mods_menu = mb.addMenu("Mods")
        act_refresh_mods = mods_menu.addAction("Refresh Mod List", self._refresh_mod_list)
        act_refresh_mods.setShortcut("Ctrl+Shift+M")
        act_demo = mods_menu.addAction("Create Demo Mod…", self._action_create_demo_mod)
        act_demo.setShortcut("Ctrl+Shift+D")
        act_open_mods_dir = mods_menu.addAction("Open Mods Folder", lambda: os.startfile(str(MODS_DIR)))
        act_open_mods_dir.setShortcut("Ctrl+Shift+O")

        # Tools
        tools_menu = mb.addMenu("Tools")
        act_create_compat = tools_menu.addAction("Create Modcompat Font…", self._action_create_modcompat)
        act_create_compat.setShortcut("Ctrl+Shift+C")
        act_refresh_fonts = tools_menu.addAction("Refresh Font List", self._refresh_font_list)
        act_refresh_fonts.setShortcut("Ctrl+R")
        act_open_fonts_dir = tools_menu.addAction("Open Fonts Folder", lambda: os.startfile(str(FONTS_DIR)))
        act_open_fonts_dir.setShortcut("Ctrl+Shift+F")

        # Settings
        settings_menu = mb.addMenu("Settings")
        act_prefs = settings_menu.addAction("Preferences…", self._action_preferences)
        act_prefs.setShortcut("Ctrl+,")

        # Help
        help_menu = mb.addMenu("Help")
        act_help = help_menu.addAction("Help & Manual", self._show_help)
        act_help.setShortcut("F1")
        help_menu.addSeparator()
        act_about = help_menu.addAction("About fModLoader", self._show_about)
        act_about.setShortcut("Ctrl+Shift+A")

    # ── Central Widget UI ─────────────────────────────────────────────────────

    def _build_ui(self):
        central = QWidget()
        self.setCentralWidget(central)
        root = QVBoxLayout(central)
        root.setContentsMargins(0, 0, 0, 0)
        root.setSpacing(0)

        # Hazard tape banner
        self._tape = HazardTapeWidget()
        root.addWidget(self._tape)

        # App title label
        title_bar = QWidget()
        title_bar.setFixedHeight(52)
        title_bar.setStyleSheet("background: white;")
        tbl = QHBoxLayout(title_bar)
        tbl.setContentsMargins(20, 0, 20, 0)
        tbl.setAlignment(Qt.AlignmentFlag.AlignCenter)

        app_lbl = QLabel("fModLoader — BETA VERSION 1.0.4")
        app_lbl.setStyleSheet("""
            color: #1a1a1a;
            font-size: 22px;
            font-family: 'Segoe UI', 'Arial Black', Arial;
            font-weight: 900;
            letter-spacing: 1px;
        """)
        tbl.addWidget(app_lbl)
        root.addWidget(title_bar)

        # Main content area with background
        self._bg = MainBackground()
        bg_layout = QVBoxLayout(self._bg)
        bg_layout.setContentsMargins(30, 20, 30, 10)
        bg_layout.setSpacing(0)

        # ── Two-panel row ─────────────────────────────────────────────────────
        panel_row = QHBoxLayout()
        panel_row.setSpacing(40)

        # Left panel — Font
        panel_row.addLayout(self._build_font_panel(), 1)
        # Right panel — Mod
        panel_row.addLayout(self._build_mod_panel(), 1)

        bg_layout.addLayout(panel_row)
        bg_layout.addSpacing(14)

        # ── Apply button centred ──────────────────────────────────────────────
        apply_row = QHBoxLayout()
        apply_row.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self._apply_btn = ApplyButton()
        self._apply_btn.clicked.connect(self._on_apply)
        self._apply_btn.setShortcut("Return")
        apply_row.addWidget(self._apply_btn)
        bg_layout.addLayout(apply_row)

        bg_layout.addSpacing(8)

        # ── Status heartbeat line ─────────────────────────────────────────────
        self._status_lbl = QLabel("Status: Awaiting font and mod selection…")
        self._status_lbl.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self._status_lbl.setStyleSheet("""
            color: #333;
            font-size: 13px;
            font-family: 'Segoe UI', Arial;
            background: transparent;
        """)
        self._hb_widget = HeartbeatWidget(color="#cc1a1a", height=28)

        bg_layout.addWidget(self._hb_widget)
        bg_layout.addWidget(self._status_lbl)
        bg_layout.addStretch()

        root.addWidget(self._bg, 1)

    def _build_font_panel(self) -> QVBoxLayout:
        lay = QVBoxLayout()
        lay.setSpacing(6)

        title = QLabel("Select Font File")
        title.setStyleSheet("""
            color: #1a1a1a;
            font-size: 15px;
            font-family: 'Segoe UI', Arial;
            font-weight: bold;
        """)
        lay.addWidget(title)

        row = QHBoxLayout()
        row.setSpacing(8)
        self._font_combo = QComboBox()
        self._font_combo.setStyleSheet(COMBO_STYLE)
        self._font_combo.setPlaceholderText("Select a font file…")
        self._font_combo.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Fixed)
        self._font_combo.currentIndexChanged.connect(self._on_font_selected)

        browse_font = QPushButton("Browse… [Ctrl+B]")
        browse_font.setStyleSheet(BROWSE_STYLE)
        browse_font.setShortcut("Ctrl+B")
        browse_font.clicked.connect(self._browse_font)

        row.addWidget(self._font_combo, 1)
        row.addWidget(browse_font)
        lay.addLayout(row)

        disc = QLabel("Disclaimer: Only .modcompat.ttf or\n.modcompat.otf fonts are supported.")
        disc.setStyleSheet("""
            color: #cc1a1a;
            font-size: 11px;
            font-family: 'Segoe UI', Arial;
        """)
        lay.addWidget(disc)
        return lay

    def _build_mod_panel(self) -> QVBoxLayout:
        lay = QVBoxLayout()
        lay.setSpacing(6)

        title = QLabel("Select Mod File")
        title.setStyleSheet("""
            color: #1a1a1a;
            font-size: 15px;
            font-family: 'Segoe UI', Arial;
            font-weight: bold;
        """)
        lay.addWidget(title)

        row = QHBoxLayout()
        row.setSpacing(8)
        self._mod_combo = QComboBox()
        self._mod_combo.setStyleSheet(COMBO_STYLE)
        self._mod_combo.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Fixed)
        self._mod_combo.currentIndexChanged.connect(self._on_mod_selected)

        browse_mod = QPushButton("Browse… [Ctrl+Shift+B]")
        browse_mod.setStyleSheet(BROWSE_STYLE)
        browse_mod.setShortcut("Ctrl+Shift+B")
        browse_mod.clicked.connect(self._browse_mod)

        row.addWidget(self._mod_combo, 1)
        row.addWidget(browse_mod)
        lay.addLayout(row)

        disc = QLabel("Disclaimer: Mods must be .ttfm or .otfm files.\nJAR files are NOT supported.")
        disc.setStyleSheet("""
            color: #cc1a1a;
            font-size: 11px;
            font-family: 'Segoe UI', Arial;
        """)
        lay.addWidget(disc)
        return lay

    # ── Status Bar ────────────────────────────────────────────────────────────

    def _build_statusbar(self):
        sb = self.statusBar()
        sb.setStyleSheet("""
            QStatusBar {
                background: qlineargradient(x1:0,y1:0,x2:0,y2:1,
                    stop:0 #cc1a1a, stop:1 #8b0000);
                color: white;
                font-family: 'Segoe UI', Arial;
                font-size: 11px;
                padding-left: 8px;
            }
        """)
        self._sb_label = QLabel("fModLoader v1.0.4 Beta | Status: Idle")
        self._sb_label.setStyleSheet("color: white; background: transparent; font-size: 11px;")
        sb.addWidget(self._sb_label)

    def _set_status(self, text: str, sb_text: str | None = None):
        self._status_lbl.setText(f"Status: {text}")
        self._sb_label.setText(f"fModLoader v1.0.4 Beta | Status: {sb_text or text}")

    # ── Font list management ──────────────────────────────────────────────────

    def _refresh_font_list(self):
        self._font_combo.blockSignals(True)
        self._font_combo.clear()
        self._font_combo.addItem("Select a font file…", None)
        fonts = scan_for_modcompat_fonts([str(FONTS_DIR)])
        for f in fonts:
            self._font_combo.addItem(Path(f).name, f)
        self._font_combo.blockSignals(False)
        self._set_status("Font list refreshed.")

    def _refresh_mod_list(self):
        self._mod_combo.blockSignals(True)
        current_mod = self._mod_path
        self._mod_combo.clear()
        self._mod_combo.addItem("None selected…", None)
        self._mod_combo.addItem("— No Mod (Revert) —", "__REVERT__")
        mods = scan_for_mods([str(MODS_DIR)])
        for m in mods:
            self._mod_combo.addItem(Path(m).name, m)
        # Re-select if still present
        if current_mod:
            idx = self._mod_combo.findData(current_mod)
            if idx >= 0:
                self._mod_combo.setCurrentIndex(idx)
        self._mod_combo.blockSignals(False)

    # ── Selections ───────────────────────────────────────────────────────────

    def _on_font_selected(self, idx):
        data = self._font_combo.currentData()
        if data:
            self._font_path = data
            info = get_font_info(data)
            name = info.get("full_name") or Path(data).name
            backed = " [backup exists]" if has_backup(data) else ""
            self._set_status(f"Font selected: {name}{backed}")
        else:
            self._font_path = None
            self._set_status("Awaiting font selection…")

    def _on_mod_selected(self, idx):
        data = self._mod_combo.currentData()
        if data == "__REVERT__":
            self._mod_path = None
            self._set_status("Revert mode — click Apply to restore original glyphs.")
        elif data:
            self._mod_path = data
            meta, err = load_mod(data)
            if meta:
                self._set_status(f"Mod selected: {meta.display_name()}")
            else:
                self._set_status(f"Warning: {err}")
        else:
            self._mod_path = None
            self._set_status("Awaiting mod selection…")

    # ── Browse for files ──────────────────────────────────────────────────────

    def _browse_font(self):
        path, _ = FMLFileDialog.getOpenFileName(
            self, "Select Modcompat Font", str(FONTS_DIR),
            "Modcompat Fonts (*.modcompat.ttf *.modcompat.otf);;All Files (*)"
        )
        if not path:
            return
        if not is_modcompat_font(path):
            QMessageBox.warning(
                self, "Invalid Font",
                "The selected file is not a valid .modcompat font.\n\n"
                "Only fonts with 'FMOD' vendor ID in their OS/2 table are accepted.\n"
                "Use Tools → Create Modcompat Font to convert a standard font."
            )
            return
        # Add to combo and select
        idx = self._font_combo.findData(path)
        if idx < 0:
            self._font_combo.addItem(Path(path).name, path)
            idx = self._font_combo.count() - 1
        self._font_combo.setCurrentIndex(idx)
        self._font_path = path

    def _browse_mod(self):
        path, _ = FMLFileDialog.getOpenFileName(
            self, "Select Font Mod File", str(MODS_DIR),
            "Font Mods (*.ttfm *.otfm);;All Files (*)"
        )
        if not path:
            return
        if path.lower().endswith(".jar"):
            QMessageBox.critical(self, "Unsupported File",
                                 "JAR files are NOT supported.")
            return
        if not is_valid_mod_file(path):
            QMessageBox.warning(self, "Invalid Mod",
                                "The selected file is not a valid .ttfm or .otfm mod package.")
            return
        idx = self._mod_combo.findData(path)
        if idx < 0:
            self._mod_combo.addItem(Path(path).name, path)
            idx = self._mod_combo.count() - 1
        self._mod_combo.setCurrentIndex(idx)
        self._mod_path = path

    # ── Apply logic ───────────────────────────────────────────────────────────

    def _on_apply(self):
        if not self._font_path:
            QMessageBox.warning(self, "No Font Selected",
                                "Please select a target .modcompat font first.")
            return

        mod_data = self._mod_combo.currentData()
        revert = (mod_data == "__REVERT__")

        if not revert and not self._mod_path:
            QMessageBox.warning(self, "No Mod Selected",
                                "Please select a mod file, or choose '— No Mod (Revert) —' to restore.")
            return

        if not is_fonttools_available():
            QMessageBox.critical(self, "fontTools Missing",
                                 "fontTools is not installed.\n\nRun:  pip install fonttools")
            return

        self._apply_btn.setEnabled(False)
        self._set_status("Working…", "Applying mod…")

        self._worker = ApplyWorker(
            self._font_path,
            self._mod_path or "",
            revert=revert
        )
        self._worker.progress.connect(self._set_status)
        self._worker.finished.connect(self._on_apply_done)
        self._worker.start()

    def _on_apply_done(self, success: bool, message: str):
        self._apply_btn.setEnabled(True)
        if success:
            self._set_status(message, "Done")
            QMessageBox.information(self, "Success", message)
        else:
            self._set_status(f"Failed: {message}", "Error")
            QMessageBox.critical(self, "Operation Failed", message)

    # ── Tool actions ──────────────────────────────────────────────────────────

    def _action_create_modcompat(self):
        src, _ = FMLFileDialog.getOpenFileName(
            self, "Select Source Font", "",
            "Fonts (*.ttf *.otf);;All Files (*)"
        )
        if not src:
            return
        out_name = Path(src).stem + ".modcompat" + Path(src).suffix
        out_path, _ = FMLFileDialog.getSaveFileName(
            self, "Save Modcompat Font", str(FONTS_DIR / out_name),
            "Modcompat Fonts (*.modcompat.ttf *.modcompat.otf);;All Files (*)"
        )
        if not out_path:
            return
        ok = create_modcompat_font(src, out_path)
        if ok:
            self._refresh_font_list()
            QMessageBox.information(self, "Done",
                                    f"Modcompat font saved:\n{out_path}")
        else:
            QMessageBox.critical(self, "Failed", "Could not create modcompat font.")

    def _action_create_demo_mod(self):
        out_path, _ = FMLFileDialog.getSaveFileName(
            self, "Save Demo Mod",
            str(MODS_DIR / "demo_mod.ttfm"),
            "fModLoader Mod (*.ttfm *.otfm)"
        )
        if out_path:
            if create_demo_mod(out_path):
                self._set_status("Demo mod created successfully.", "Demo Mod Created")
                self._refresh_mod_list()
            else:
                QMessageBox.critical(self, "Error", "Failed to create demo mod.")

    _GLYPH_EDITOR_FILTERS = (
        "All Fonts (*.modcompat.ttf *.modcompat.otf *.modcompat.woff *.modcompat.woff2 "
        "*.modcompat.ttc *.modcompat.otc *.modcompat.pfa *.modcompat.pfb *.modcompat.cff "
        "*.modcompat.bdf *.modcompat.pcf *.modcompat.sfd *.modcompat.ufo *.svg *.ttfm *.otfm);;"
        "Outline Fonts (*.modcompat.ttf *.modcompat.otf *.modcompat.woff *.modcompat.woff2 "
        "*.modcompat.ttc *.modcompat.otc *.modcompat.pfa *.modcompat.pfb *.modcompat.cff);;"
        "Bitmap Fonts (*.modcompat.bdf *.modcompat.pcf *.modcompat.fon *.modcompat.fnt);;"
        "TeX Bitmap Fonts (*.modcompat.gf *.modcompat.pk);;"
        "PostScript (*.modcompat.pfa *.modcompat.pfb *.modcompat.ps *.modcompat.cid);;"
        "TrueType (*.modcompat.ttf *.modcompat.ttc);;"
        "OpenType (*.modcompat.otf *.modcompat.otc);;"
        "Type1 (*.modcompat.pfa *.modcompat.pfb);;"
        "Type2 (*.modcompat.cff);;"
        "Type3 (*.modcompat.t3 *.modcompat.ps);;"
        "SVG (*.svg);;"
        "UFO (*.modcompat.ufo);;"
        "SFD (*.modcompat.sfd);;"
        "Backup SFD (*.modcompat.sfd~);;"
        "Extract from PDF (*.pdf);;"
        "TTFM (*.ttfm);;"
        "OTFM (*.otfm);;"
        "Archives (*.zip *.tar.gz *.7z *.rar)"
    )

    def _open_glyph_editor(self):
        path, _ = FMLFileDialog.getOpenFileName(
            self, "Open Glyph / Font Editor", str(MODS_DIR),
            self._GLYPH_EDITOR_FILTERS, new_file_mode=True
        )
        if not path:
            return
        
        if path == "__NEW_FILE__":
            self._untitled_counter += 1
            if self._untitled_counter == 1:
                title = "untitled"
            else:
                title = f"untitled {self._untitled_counter}"
            self._glyph_editor = FontEditorWindow(self, filepath=title)
        else:
            self._glyph_editor = FontEditorWindow(self, filepath=path)
            
        self._glyph_editor.show()

    def _action_preferences(self):
        QMessageBox.information(self, "Preferences",
                                "Preferences panel coming in a future version.\n"
                                "Currently using default scan directories:\n"
                                f"  Fonts: {FONTS_DIR}\n"
                                f"  Mods:  {MODS_DIR}")

    # ── Dialogs ───────────────────────────────────────────────────────────────

    def _show_about(self):
        dlg = AboutDialog(self)
        dlg.exec()

    def _show_help(self):
        dlg = HelpDialog(self)
        dlg.exec()
