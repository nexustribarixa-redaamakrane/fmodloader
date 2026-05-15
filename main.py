"""
main.py
Entry point for fModLoader v1.0.6 Beta.
"""

import sys
import os
import traceback
from PyQt6.QtWidgets import QApplication, QMessageBox, QSplashScreen, QLabel
from PyQt6.QtCore import Qt

# Ensure the app directory is in sys.path
APP_DIR = os.path.dirname(os.path.abspath(__file__))
if APP_DIR not in sys.path:
    sys.path.insert(0, APP_DIR)

def exception_hook(exctype, value, tb):
    """Global exception handler to prevent silent crashes and provide diagnostics."""
    error_msg = "".join(traceback.format_exception(exctype, value, tb))
    print(error_msg) # Still log to console
    
    # Try to show a dialog
    dialog = QMessageBox()
    dialog.setIcon(QMessageBox.Icon.Critical)
    dialog.setWindowTitle("Application Error — Diagnostic Report")
    dialog.setText("An unexpected error occurred within fModLoader.")
    dialog.setInformativeText("A detailed diagnostic report has been generated. Please review the technical details below.")
    dialog.setDetailedText(error_msg)
    dialog.setStandardButtons(QMessageBox.StandardButton.Ok)
    dialog.exec()

# Install the hook
sys.excepthook = exception_hook

from PyQt6.QtGui import (
    QIcon, QPixmap, QPainter, QColor, QLinearGradient,
    QBrush, QPen, QFont, QPainterPath, QPolygonF
)
from PyQt6.QtCore import Qt, QTimer, QPointF, QSize
import math


# ─────────────────────────────── App Icon ────────────────────────────────────

def _build_icon_pixmap(size: int = 64) -> QPixmap:
    """Programmatically draw the fModLoader shield icon."""
    pm = QPixmap(size, size)
    pm.fill(Qt.GlobalColor.transparent)
    p = QPainter(pm)
    p.setRenderHint(QPainter.RenderHint.Antialiasing)

    w = h = size

    # Shield body
    grad = QLinearGradient(0, 0, 0, h)
    grad.setColorAt(0.0, QColor("#dd1a1a"))
    grad.setColorAt(0.5, QColor("#8b0000"))
    grad.setColorAt(1.0, QColor("#5a0000"))

    shield = QPainterPath()
    shield.moveTo(w * 0.5, h * 0.96)
    shield.cubicTo(w * 0.05, h * 0.78, 0, h * 0.52, 0, h * 0.26)
    shield.lineTo(0, h * 0.08)
    shield.lineTo(w, h * 0.08)
    shield.lineTo(w, h * 0.26)
    shield.cubicTo(w, h * 0.52, w * 0.95, h * 0.78, w * 0.5, h * 0.96)
    shield.closeSubpath()

    p.setBrush(QBrush(grad))
    p.setPen(QPen(QColor("#ff4444"), max(1, size // 24)))
    p.drawPath(shield)

    # 'f' letter
    f_font = QFont("Georgia")
    f_font.setPixelSize(max(8, size // 3))
    f_font.setWeight(QFont.Weight.Bold)
    f_font.setItalic(True)
    p.setFont(f_font)
    p.setPen(QColor("white"))
    p.drawText(
        int(w * 0.2), int(h * 0.25), int(w * 0.6), int(h * 0.6),
        Qt.AlignmentFlag.AlignCenter, "f"
    )

    p.end()
    return pm


# ─────────────────────────────── Splash Screen ───────────────────────────────

def _build_splash() -> QSplashScreen:
    """Create a styled splash screen."""
    w, h = 480, 240
    pm = QPixmap(w, h)
    pm.fill(Qt.GlobalColor.transparent)

    p = QPainter(pm)
    p.setRenderHint(QPainter.RenderHint.Antialiasing)

    # Gradient background
    bg = QLinearGradient(0, 0, 0, h)
    bg.setColorAt(0, QColor("#7a0000"))
    bg.setColorAt(0.6, QColor("#5a0000"))
    bg.setColorAt(1, QColor("#3d0000"))
    p.fillRect(0, 0, w, h, QBrush(bg))

    # Rounded border
    p.setPen(QPen(QColor("#cc1a1a"), 2))
    p.setBrush(Qt.BrushStyle.NoBrush)
    p.drawRoundedRect(2, 2, w - 4, h - 4, 10, 10)

    # Faint 'f' watermarks
    wm_font = QFont("Georgia")
    wm_font.setPixelSize(60)
    wm_font.setWeight(QFont.Weight.Bold)
    wm_font.setItalic(True)
    p.setFont(wm_font)
    p.setPen(QColor(255, 255, 255, 12))
    for x, y in [(10, 70), (120, 190), (260, 80), (380, 180), (430, 60)]:
        p.drawText(x, y, "f")

    # Heartbeat line
    p.setPen(QPen(QColor(255, 255, 255, 40), 1.5))
    mid_y = h * 0.72
    pts = []
    for i in range(w + 1):
        t = i / w
        if 0.38 < t < 0.44:
            amp = math.sin((t - 0.38) / 0.06 * math.pi) * 16
        elif 0.44 < t < 0.49:
            amp = -math.sin((t - 0.44) / 0.05 * math.pi) * 9
        else:
            amp = math.sin(t * math.pi * 5) * 1.5
        pts.append(QPointF(float(i), mid_y - amp))
    for i in range(len(pts) - 1):
        p.drawLine(pts[i], pts[i + 1])

    # Project title
    title_font = QFont("Arial Black")
    title_font.setPixelSize(26)
    title_font.setWeight(QFont.Weight.Black)
    p.setFont(title_font)
    p.setPen(QColor("white"))
    p.drawText(0, 30, w, 70, Qt.AlignmentFlag.AlignCenter, "fModLoader")

    sub_font = QFont("Segoe UI")
    sub_font.setPixelSize(13)
    p.setFont(sub_font)
    p.setPen(QColor("#ffcccc"))
    p.drawText(0, 95, w, 34, Qt.AlignmentFlag.AlignCenter,
               'v1.0.6 BETA  •  "Project Vectoris"')

    # Loading line
    p.setPen(QColor(255, 255, 255, 140))
    load_font = QFont("Segoe UI")
    load_font.setPixelSize(10)
    p.setFont(load_font)
    p.drawText(0, 155, w, 28, Qt.AlignmentFlag.AlignCenter,
               "Loading font modding engine…")

    p.setPen(QColor(255, 220, 220, 100))
    small_font = QFont("Segoe UI")
    small_font.setPixelSize(8)
    p.setFont(small_font)
    p.drawText(0, 210, w, 22, Qt.AlignmentFlag.AlignCenter,
               "Open-source • Vibecoded • Community-driven")

    p.end()

    splash = QSplashScreen(pm, Qt.WindowType.WindowStaysOnTopHint)
    splash.setMask(pm.mask())
    return splash


# ─────────────────────────────── Main ────────────────────────────────────────

def main():
    # Enable high-DPI scaling
    QApplication.setHighDpiScaleFactorRoundingPolicy(
        Qt.HighDpiScaleFactorRoundingPolicy.PassThrough
    )

    app = QApplication(sys.argv)
    app.setApplicationName("fModLoader")
    app.setApplicationVersion("1.0.6 Beta")
    app.setOrganizationName("Nexus Tribarixa")

    # App-wide stylesheet
    app.setStyleSheet("""
        QToolTip {
            background: #333;
            color: white;
            border: 1px solid #cc1a1a;
            font-family: 'Segoe UI', Arial;
            font-size: 11px;
            padding: 4px;
        }
        QMessageBox {
            font-family: 'Segoe UI', Arial;
            font-size: 12px;
        }
        QMessageBox QPushButton {
            min-width: 80px;
            padding: 5px 14px;
            border-radius: 4px;
            border: 1px solid #aaa;
            background: qlineargradient(x1:0,y1:0,x2:0,y2:1,
                stop:0 #f5f5f5, stop:1 #e0e0e0);
            font-family: 'Segoe UI', Arial;
        }
        QMessageBox QPushButton:hover {
            border-color: #cc1a1a;
        }
        QFileDialog {
            font-family: 'Segoe UI', Arial;
        }
    """)

    # Set app icon
    icon_pm = _build_icon_pixmap(64)
    app_icon = QIcon()
    for sz in (16, 24, 32, 48, 64):
        app_icon.addPixmap(_build_icon_pixmap(sz), QIcon.Mode.Normal, QIcon.State.Off)
    app.setWindowIcon(app_icon)

    # Splash screen
    splash = _build_splash()
    splash.show()
    app.processEvents()

    # Delayed import so splash shows first
    from ui_main_window import MainWindow
    window = MainWindow()
    window.setWindowIcon(app_icon)

    # Close splash and show main window after short delay
    def _show_main():
        splash.finish(window)
        window.show()
        window.activateWindow()

    QTimer.singleShot(1800, _show_main)

    sys.exit(app.exec())


if __name__ == "__main__":
    main()
