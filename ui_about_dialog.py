"""
ui_about_dialog.py
The About dialog for fModLoader, styled to match the reference screenshot:
  - Dark red/maroon gradient background
  - Shield + heartbeat logo on the left
  - "Font Mod Loader" title, v1.0.1 BETA, Project Aurion subtitle
  - Description paragraphs
  - Updated GitHub link and removed Discord
"""

from PyQt6.QtWidgets import (
    QDialog, QVBoxLayout, QHBoxLayout, QLabel,
    QPushButton, QWidget, QFrame, QSizePolicy
)
from PyQt6.QtGui import (
    QFont, QPainter, QColor, QLinearGradient, QBrush,
    QPen, QPainterPath, QFontDatabase, QCursor, QPolygonF
)
from PyQt6.QtCore import Qt, QPointF, QRectF, QSize
import math


# ─────────────────────────────── Shield Widget ───────────────────────────────

class ShieldWidget(QWidget):
    """Draws a red shield with an 'f' and heartbeat line."""

    def __init__(self, parent=None):
        super().__init__(parent)
        self.setFixedSize(110, 130)

    def paintEvent(self, event):
        p = QPainter(self)
        p.setRenderHint(QPainter.RenderHint.Antialiasing)

        w, h = self.width(), self.height()

        # Shield gradient
        grad = QLinearGradient(0, 0, 0, h)
        grad.setColorAt(0.0, QColor("#cc1a1a"))
        grad.setColorAt(0.5, QColor("#8b0000"))
        grad.setColorAt(1.0, QColor("#5a0000"))

        # Build shield path
        path = QPainterPath()
        path.moveTo(w * 0.5, h * 0.95)
        path.cubicTo(w * 0.05, h * 0.75, 0, h * 0.5, 0, h * 0.25)
        path.lineTo(0, h * 0.1)
        path.lineTo(w, h * 0.1)
        path.lineTo(w, h * 0.25)
        path.cubicTo(w, h * 0.5, w * 0.95, h * 0.75, w * 0.5, h * 0.95)
        path.closeSubpath()

        # Border glow
        p.setPen(QPen(QColor("#ff4444"), 3))
        p.setBrush(QBrush(grad))
        p.drawPath(path)

        # Outer border highlight
        p.setPen(QPen(QColor("#ff8888"), 1.5))
        p.setBrush(Qt.BrushStyle.NoBrush)
        p.drawPath(path)

        # Heartbeat line on shield
        p.setPen(QPen(QColor(255, 255, 255, 180), 1.5))
        pts = []
        n = 30
        for i in range(n + 1):
            x = w * 0.1 + (w * 0.8) * i / n
            phase = i / n
            if 0.3 < phase < 0.45:
                # spike up
                amp = math.sin((phase - 0.3) / 0.15 * math.pi) * h * 0.15
            elif 0.45 < phase < 0.55:
                amp = -math.sin((phase - 0.45) / 0.10 * math.pi) * h * 0.08
            else:
                amp = 0
            y = h * 0.55 + amp
            pts.append(QPointF(x, y))

        for i in range(len(pts) - 1):
            p.drawLine(pts[i], pts[i + 1])

        # Letter "f"
        f_font = QFont("Georgia", int(h * 0.32), QFont.Weight.Bold)
        f_font.setItalic(True)
        p.setFont(f_font)
        p.setPen(QColor("white"))
        p.drawText(QRectF(w * 0.25, h * 0.3, w * 0.5, h * 0.45),
                   Qt.AlignmentFlag.AlignCenter, "f")

        p.end()


# ─────────────────────────────── Link Button ─────────────────────────────────

class LinkButton(QPushButton):
    def __init__(self, text, url="", parent=None):
        super().__init__(text, parent)
        self.url = url
        self.setCursor(QCursor(Qt.CursorShape.PointingHandCursor))
        self.setFlat(True)
        self.setStyleSheet("""
            QPushButton {
                color: #dddddd;
                background: transparent;
                border: 1px solid #888888;
                border-radius: 4px;
                padding: 6px 14px;
                font-size: 11px;
                font-family: 'Segoe UI', Arial, sans-serif;
            }
            QPushButton:hover {
                color: white;
                border-color: #cccccc;
                background-color: rgba(255,255,255,20);
            }
            QPushButton:pressed {
                background-color: rgba(255,255,255,40);
            }
        """)
        if url:
            self.clicked.connect(self._open_url)

    def _open_url(self):
        from PyQt6.QtGui import QDesktopServices
        from PyQt6.QtCore import QUrl
        QDesktopServices.openUrl(QUrl(self.url))


# ─────────────────────────────── About Dialog ────────────────────────────────

class AboutDialog(QDialog):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle("About fModLoader")
        self.setFixedSize(560, 460)
        self.setWindowFlags(
            Qt.WindowType.Dialog |
            Qt.WindowType.WindowCloseButtonHint
        )
        self._build_ui()

    def _build_ui(self):
        # Root layout
        root = QVBoxLayout(self)
        root.setContentsMargins(0, 0, 0, 0)
        root.setSpacing(0)

        inner = QWidget()
        inner.setObjectName("aboutInner")
        inner.setStyleSheet("""
            #aboutInner { background: transparent; }
            QLabel { background: transparent; color: #eeeeee; }
        """)

        layout = QVBoxLayout(inner)
        layout.setContentsMargins(28, 22, 28, 18)
        layout.setSpacing(12)

        # ── Top section: shield + title ──────────────────────────────────────
        top_row = QHBoxLayout()
        top_row.setSpacing(18)

        shield = ShieldWidget()
        top_row.addWidget(shield, 0, Qt.AlignmentFlag.AlignVCenter)

        title_col = QVBoxLayout()
        title_col.setSpacing(2)

        version_label = QLabel('v1.0.1 BETA\n"Project Aurion"')
        version_label.setStyleSheet("""
            color: #ffcccc;
            font-size: 13px;
            font-family: 'Segoe UI', Arial, sans-serif;
            font-style: italic;
            font-weight: bold;
            line-height: 1.4;
        """)
        version_label.setAlignment(Qt.AlignmentFlag.AlignLeft | Qt.AlignmentFlag.AlignBottom)

        app_title = QLabel("Font\nMod Loader")
        app_title.setStyleSheet("""
            color: white;
            font-size: 34px;
            font-family: 'Segoe UI', 'Arial Black', Arial, sans-serif;
            font-weight: 900;
            line-height: 1.1;
            letter-spacing: 1px;
        """)
        app_title.setAlignment(Qt.AlignmentFlag.AlignLeft)

        title_inner = QHBoxLayout()
        left_title = QVBoxLayout()
        left_title.addStretch()
        left_title.addWidget(app_title)

        right_version = QVBoxLayout()
        right_version.addWidget(version_label)
        right_version.addStretch()

        title_inner.addLayout(left_title)
        title_inner.addStretch()
        title_inner.addLayout(right_version)

        title_col.addLayout(title_inner)
        top_row.addLayout(title_col, 1)
        layout.addLayout(top_row)

        # ── Divider ──────────────────────────────────────────────────────────
        line = QFrame()
        line.setFrameShape(QFrame.Shape.HLine)
        line.setStyleSheet("background-color: rgba(255,255,255,60); max-height: 1px;")
        layout.addWidget(line)

        # ── Description text ─────────────────────────────────────────────────
        body_style = """
            color: #e8e8e8;
            font-size: 12px;
            font-family: 'Segoe UI', Arial, sans-serif;
            line-height: 1.6;
        """

        desc1 = QLabel(
            "fModLoader: The official Font Modding Tool. (v1.0.1 Beta 'Project Aurion').\n"
            "Designed for the font-mod community to manage and apply Font compatibility.\n"
            "Developed by the font-mod community, with dedication and precision."
        )
        desc1.setWordWrap(True)
        desc1.setAlignment(Qt.AlignmentFlag.AlignCenter)
        desc1.setStyleSheet(body_style)
        layout.addWidget(desc1)

        desc2 = QLabel(
            "This software is open-source, released under the GNU General Public License (GPL).\n"
            "We encourage contributions and feedback from all users. Please report any issues\n"
            "or submit feature requests through our official GitHub repository.\n"
            "Thank you for testing the Beta!"
        )
        desc2.setWordWrap(True)
        desc2.setAlignment(Qt.AlignmentFlag.AlignCenter)
        desc2.setStyleSheet(body_style)
        layout.addWidget(desc2)

        credit = QLabel("Vibecoded by Google Antigravity and Nexus Tribarixa the dev.")
        credit.setAlignment(Qt.AlignmentFlag.AlignCenter)
        credit.setStyleSheet("color: #dddddd; font-size: 12px; font-family: 'Segoe UI', Arial;")
        layout.addWidget(credit)

        copy_label = QLabel("(c) 2026 Nexus Tribarixa.")
        copy_label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        copy_label.setStyleSheet("color: #cccccc; font-size: 11px; font-family: 'Segoe UI', Arial;")
        layout.addWidget(copy_label)

        layout.addSpacing(4)

        # ── Bottom link buttons ───────────────────────────────────────────────
        btn_row = QHBoxLayout()
        btn_row.setSpacing(15)
        btn_row.setAlignment(Qt.AlignmentFlag.AlignCenter)

        # Corrected personal GitHub handle
        github_url = "https://github.com/nexustribarixa-redaamakrane/fmodloader"
        btn_source = LinkButton("[View Source Code on GitHub]", github_url)
        btn_license = LinkButton("[License Details]",
                                 "https://www.gnu.org/licenses/gpl-3.0.html")

        btn_row.addWidget(btn_source)
        btn_row.addWidget(btn_license)

        layout.addLayout(btn_row)
        root.addWidget(inner)

    def paintEvent(self, event):
        """Paint the dark red/maroon gradient background with faint 'f' watermarks."""
        p = QPainter(self)
        p.setRenderHint(QPainter.RenderHint.Antialiasing)

        w, h = self.width(), self.height()

        grad = QLinearGradient(0, 0, 0, h)
        grad.setColorAt(0.0, QColor("#5a0000"))
        grad.setColorAt(0.4, QColor("#7a0808"))
        grad.setColorAt(0.7, QColor("#6e0505"))
        grad.setColorAt(1.0, QColor("#3d0000"))
        p.fillRect(self.rect(), QBrush(grad))

        wm_font = QFont("Georgia", 72, QFont.Weight.Bold)
        wm_font.setItalic(True)
        p.setFont(wm_font)
        p.setPen(QColor(255, 255, 255, 12))
        positions = [(20, 80), (160, 220), (300, 100), (420, 280), (500, 60), (80, 350), (380, 390)]
        for (x, y) in positions:
            p.drawText(x, y, "f")

        p.setPen(QPen(QColor(255, 255, 255, 18), 1.5))
        y_base = h * 0.48
        pts = []
        n = 60
        for i in range(n + 1):
            x = w * i / n
            phase = i / n
            if 0.35 < phase < 0.42:
                amp = math.sin((phase - 0.35) / 0.07 * math.pi) * 18
            elif 0.42 < phase < 0.48:
                amp = -math.sin((phase - 0.42) / 0.06 * math.pi) * 10
            else:
                amp = math.sin(phase * math.pi * 4) * 1.5
            pts.append(QPointF(x, y_base + amp))

        for i in range(len(pts) - 1):
            p.drawLine(pts[i], pts[i + 1])

        p.end()
        super().paintEvent(event)
