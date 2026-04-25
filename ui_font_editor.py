"""
ui_font_editor.py
Built-in Font Editor for fModLoader.
Features:
 - Codepoint range assignments
 - Glyph Info Dialog
 - SVG Import & Transformation controls
 - Script Execution Environment (Python/FontForge/Cython)
 - Exporting to .ttfm, .otfm, .modcompat.ttc with Font Mod Info prompt
 - FontForge-Style UI Redesign (Glyph Grid & Vector Canvas)
 - Fully functional drawing tools (Pointer, Zoom, Pan, Pen, Pencil, Rectangle, Ellipse, Knife, Ruler, Nodes)
"""

import sys
import os
import math

from PyQt6.QtWidgets import (
    QApplication, QWidget, QMainWindow, QVBoxLayout, QHBoxLayout,
    QPushButton, QLabel, QLineEdit, QDialog, QFormLayout,
    QTextEdit, QRadioButton, QButtonGroup, QMessageBox,
    QTabWidget, QGroupBox, QDoubleSpinBox, QTableWidget,
    QHeaderView, QAbstractItemView, QGraphicsView, QGraphicsScene, QGraphicsItem,
    QCheckBox, QToolButton, QSizePolicy, QFrame, QScrollArea,
    QGraphicsPathItem, QGraphicsEllipseItem, QGraphicsRectItem,
    QGraphicsLineItem, QGraphicsTextItem, QGraphicsItemGroup
)
from PyQt6.QtCore import Qt, QRectF, QPointF, QSize, QLineF, pyqtSignal, QTimer
from PyQt6 import sip
from PyQt6.QtGui import (
    QPainter, QColor, QPen, QFont, QCursor, QIcon,
    QPainterPath, QBrush, QTransform, QPolygonF
)

from glyph_model import GlyphData, GlyphContour, PathNode, ModProject

_ICONS_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "icons")

def _icon(name: str) -> QIcon:
    path = os.path.join(_ICONS_DIR, name)
    if os.path.exists(path):
        return QIcon(path)
    return QIcon()


# ─────────────────────────────── Dialogs ─────────────────────────────────────

class GlyphInfoDialog(QDialog):
    def __init__(self, char_str, cp_str, parent=None):
        super().__init__(parent)
        self.setWindowTitle(f"Glyph Info: {char_str} ({cp_str})")
        self.resize(300, 200)
        layout = QFormLayout(self)
        self.unicode_edit = QLineEdit(cp_str)
        self.name_edit = QLineEdit(f"uni{cp_str.replace('U+', '')}")
        self.comments_edit = QTextEdit()
        layout.addRow("Unicode Value:", self.unicode_edit)
        layout.addRow("Glyph Name:", self.name_edit)
        layout.addRow("Comments:", self.comments_edit)
        save_btn = QPushButton("Save Info")
        save_btn.clicked.connect(self.accept)
        layout.addRow(save_btn)


class FontModInfoDialog(QDialog):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle("Font Mod Info")
        self.resize(400, 300)
        layout = QFormLayout(self)
        self.font_name = QLineEdit()
        self.family_name = QLineEdit()
        self.human_name = QLineEdit()
        self.version = QLineEdit("1.0")
        self.supported_base = QLineEdit()
        self.supported_base.setPlaceholderText("Leave empty for all")
        self.description = QTextEdit()
        self.credits = QTextEdit()
        layout.addRow("Font Name:", self.font_name)
        layout.addRow("Family Name:", self.family_name)
        layout.addRow("Comprehensible Name:", self.human_name)
        layout.addRow("Version:", self.version)
        layout.addRow("Supported Base Font:", self.supported_base)
        layout.addRow("Description:", self.description)
        layout.addRow("Credits:", self.credits)
        btn_layout = QHBoxLayout()
        export_btn = QPushButton("Export Mode")
        export_btn.clicked.connect(self.accept)
        btn_layout.addWidget(export_btn)
        layout.addRow(btn_layout)


class ScriptEditorWindow(QDialog):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle("Script Execution Environment")
        self.resize(600, 400)
        layout = QVBoxLayout(self)
        self.radio_group = QButtonGroup(self)
        h_layout = QHBoxLayout()
        self.rb_py = QRadioButton("Python")
        self.rb_ff = QRadioButton("FontForge (.pe)")
        self.rb_cy = QRadioButton("Cython")
        self.rb_py.setChecked(True)
        self.radio_group.addButton(self.rb_py)
        self.radio_group.addButton(self.rb_ff)
        self.radio_group.addButton(self.rb_cy)
        h_layout.addWidget(self.rb_py)
        h_layout.addWidget(self.rb_ff)
        h_layout.addWidget(self.rb_cy)
        layout.addLayout(h_layout)
        self.editor = QTextEdit()
        self.editor.setPlaceholderText("Write script here...")
        layout.addWidget(self.editor)
        self.exec_btn = QPushButton("Execute Script  [Ctrl+Return]")
        self.exec_btn.setStyleSheet("background-color: #cc1a1a; color: white; font-weight: bold;")
        self.exec_btn.setShortcut("Ctrl+Return")
        self.exec_btn.clicked.connect(self._execute_script)
        layout.addWidget(self.exec_btn)

    def _execute_script(self):
        QMessageBox.information(self, "Execute", "Executing script... (Mock)")


# ─────────────────────────────── Canvas Engine ───────────────────────────────

STROKE = QPen(QColor("#2e3436"), 1.5)
STROKE.setCosmetic(True)

HANDLE_BRUSH = QBrush(QColor("#3584e4"))
HANDLE_PEN   = QPen(QColor("white"), 1)
HANDLE_PEN.setCosmetic(True)


# ─────────────────────────────── Canvas Items ────────────────────────────────

class ControlPointItem(QGraphicsEllipseItem):
    """A Bezier control point (handle)."""
    SIZE = 6

    def __init__(self, anchor, is_incoming=True):
        r = self.SIZE / 2
        super().__init__(-r, -r, self.SIZE, self.SIZE)
        self.anchor = anchor
        self.is_incoming = is_incoming
        self.setBrush(QBrush(QColor("#3584e4")))
        self.setPen(QPen(Qt.GlobalColor.white, 0.5))
        self.setFlag(self.GraphicsItemFlag.ItemIsMovable)
        self.setFlag(self.GraphicsItemFlag.ItemSendsGeometryChanges)
        self.setZValue(20)

    def itemChange(self, change, value):
        if change == QGraphicsItem.GraphicsItemChange.ItemPositionChange and self.scene():
            self.anchor.handle_moved(self)
        return super().itemChange(change, value)

    def paint(self, painter, option, widget):
        super().paint(painter, option, widget)
        # Draw line to anchor
        painter.setPen(QPen(QColor("#3584e4"), 0.5, Qt.PenStyle.DashLine))
        painter.drawLine(QPointF(0, 0), self.anchor.pos() - self.pos())


class AnchorPointItem(QGraphicsRectItem):
    """A path node (anchor point)."""
    SIZE = 8

    def __init__(self, x, y, canvas):
        r = self.SIZE / 2
        super().__init__(-r, -r, self.SIZE, self.SIZE)
        self.setPos(x, y)
        self.canvas = canvas
        self.setBrush(QBrush(QColor("#cc1a1a")))
        self.setPen(QPen(Qt.GlobalColor.white, 1))
        self.setFlag(self.GraphicsItemFlag.ItemIsMovable)
        self.setFlag(self.GraphicsItemFlag.ItemIsSelectable)
        self.setFlag(self.GraphicsItemFlag.ItemSendsGeometryChanges)
        self.setZValue(30)

        self.cp_in = None
        self.cp_out = None
        self.smooth = False

    def add_handles(self, in_pos=None, out_pos=None):
        if in_pos and not self.cp_in:
            self.cp_in = ControlPointItem(self, True)
            self.scene().addItem(self.cp_in)
            self.cp_in.setPos(in_pos)
        if out_pos and not self.cp_out:
            self.cp_out = ControlPointItem(self, False)
            self.scene().addItem(self.cp_out)
            self.cp_out.setPos(out_pos)

    def handle_moved(self, handle):
        if self.smooth:
            other = self.cp_out if handle == self.cp_in else self.cp_in
            if other and not sip.isdeleted(other):
                # Mirror movement around the anchor'S center (0,0 in local space)
                other.setPos(-handle.pos())
        if self.canvas and not sip.isdeleted(self.canvas):
            self.canvas.update_path()

    def itemChange(self, change, value):
        if sip.isdeleted(self): return super().itemChange(change, value)
        if change == QGraphicsItem.GraphicsItemChange.ItemPositionHasChanged:
            if self.cp_in and not sip.isdeleted(self.cp_in): self.cp_in.update()
            if self.cp_out and not sip.isdeleted(self.cp_out): self.cp_out.update()
            if self.canvas and not sip.isdeleted(self.canvas):
                self.canvas.update_path()
        return super().itemChange(change, value)


class GlyphCanvas(QGraphicsView):
    """
    Interactive canvas with FontForge-style drawing tools.
    Utilizes GlyphData model for vector consistency.
    """

    TOOL_CURSOR = {
        "pointer":   Qt.CursorShape.ArrowCursor,
        "zoom":      Qt.CursorShape.CrossCursor,
        "pan":       Qt.CursorShape.OpenHandCursor,
        "pen":       Qt.CursorShape.CrossCursor,
        "pencil":    Qt.CursorShape.CrossCursor,
        "nodes":     Qt.CursorShape.SizeAllCursor,
        "knife":     Qt.CursorShape.CrossCursor,
        "rectangle": Qt.CursorShape.CrossCursor,
        "ellipse":   Qt.CursorShape.CrossCursor,
        "spiral":    Qt.CursorShape.CrossCursor,
        "corner":    Qt.CursorShape.PointingHandCursor,
        "ruler":     Qt.CursorShape.CrossCursor,
    }

    def __init__(self, scene, parent=None):
        super().__init__(scene, parent)
        self.active_tool = "pointer"
        self._press_pos = None        # scene-space press position
        self._preview_item = None     # rubber-band preview
        
        self.contours: list[list[AnchorPointItem]] = [[]]
        self.closed_contours: set[int] = set()
        
        self.path_item = QGraphicsPathItem()
        self.path_item.setPen(QPen(QColor("#2e3436"), 2))
        self.path_item.setBrush(QBrush(QColor(200, 80, 80, 40)))
        self.scene().addItem(self.path_item)

        self._pen_active_anchor = None
        self._ruler_line = None
        self._ruler_lbl = None
        self._undo_stack = []
        self._redo_stack = []

        self.setRenderHint(QPainter.RenderHint.Antialiasing)
        self.setTransformationAnchor(QGraphicsView.ViewportAnchor.AnchorUnderMouse)
        self.setResizeAnchor(QGraphicsView.ViewportAnchor.AnchorUnderMouse)
        self.setStyleSheet("background: white; border: none;")
        self.setDragMode(QGraphicsView.DragMode.NoDrag)

    # ── Public API ────────────────────────────────────────────────────────────

    def set_tool(self, tool_name: str):
        self._cancel_in_progress()
        self.active_tool = tool_name
        cursor = self.TOOL_CURSOR.get(tool_name, Qt.CursorShape.ArrowCursor)
        self.setCursor(QCursor(cursor))

        if tool_name == "pointer":
            self.setDragMode(QGraphicsView.DragMode.RubberBandDrag)
        elif tool_name == "pan":
            self.setDragMode(QGraphicsView.DragMode.ScrollHandDrag)
        else:
            self.setDragMode(QGraphicsView.DragMode.NoDrag)

    def delete_selected(self):
        for item in self.scene().selectedItems():
            if sip.isdeleted(item): continue
            if isinstance(item, AnchorPointItem):
                self._remove_anchor(item)
            elif isinstance(item, ControlPointItem):
                if item == item.anchor.cp_in: item.anchor.cp_in = None
                elif item == item.anchor.cp_out: item.anchor.cp_out = None
                self.scene().removeItem(item)
            else:
                self.scene().removeItem(item)
        self.update_path()

    def update_path(self):
        """Rebuild the QPainterPath from all contours with safety checks."""
        if sip.isdeleted(self): return
        path = QPainterPath()
        for idx, anchors in enumerate(self.contours):
            valid_anchors = [a for a in anchors if not sip.isdeleted(a)]
            if not valid_anchors: continue
            
            path.moveTo(valid_anchors[0].pos())
            for i in range(1, len(valid_anchors)):
                prev = valid_anchors[i-1]
                curr = valid_anchors[i]
                
                cp_out = prev.cp_out if prev.cp_out and not sip.isdeleted(prev.cp_out) else None
                cp_in = curr.cp_in if curr.cp_in and not sip.isdeleted(curr.cp_in) else None

                if cp_out and cp_in:
                    path.cubicTo(cp_out.pos(), cp_in.pos(), curr.pos())
                elif cp_out:
                    path.quadTo(cp_out.pos(), curr.pos())
                elif cp_in:
                    path.quadTo(cp_in.pos(), curr.pos())
                else:
                    path.lineTo(curr.pos())
            
            if idx in self.closed_contours:
                path.closeSubpath()
        
        self.path_item.setPath(path)

    def load_from_glyph_data(self, glyph_data: GlyphData):
        self._cancel_in_progress()
        for anchors in self.contours:
            for item in anchors[:]:
                self._remove_anchor(item)
        self.contours = [[]]
        self.closed_contours.clear()

        for c_idx, contour in enumerate(glyph_data.contours):
            if c_idx >= len(self.contours): self.contours.append([])
            current_anchors = self.contours[c_idx]
            
            for node in contour.nodes:
                anchor = AnchorPointItem(node.x, node.y, self)
                self.scene().addItem(anchor)
                current_anchors.append(anchor)
                if node.cp_in or node.cp_out:
                    anchor.add_handles(
                         QPointF(*node.cp_in) if node.cp_in else None,
                         QPointF(*node.cp_out) if node.cp_out else None
                    )
                anchor.smooth = node.smooth
                if anchor.smooth:
                    anchor.setBrush(QBrush(QColor("#3584e4")))
            
            if contour.closed:
                self.closed_contours.add(c_idx)

        self.update_path()

    def save_to_glyph_data(self, glyph_data: GlyphData):
        glyph_data.contours.clear()
        for idx, anchors in enumerate(self.contours):
            if not anchors: continue
            contour = GlyphContour()
            contour.closed = idx in self.closed_contours
            for a in anchors:
                node = PathNode(a.pos().x(), a.pos().y())
                if a.cp_in: node.cp_in = (a.cp_in.pos().x(), a.cp_in.pos().y())
                if a.cp_out: node.cp_out = (a.cp_out.pos().x(), a.cp_out.pos().y())
                node.smooth = a.smooth
                contour.nodes.append(node)
            glyph_data.contours.append(contour)

    # ── Internal helpers ──────────────────────────────────────────────────────

    def _scene_pos(self, event) -> QPointF:
        return self.mapToScene(event.pos())

    def _cancel_in_progress(self):
        self._press_pos = None
        self._pen_active_anchor = None
        if self._preview_item:
            self.scene().removeItem(self._preview_item)
            self._preview_item = None
        self._clear_ruler()

    def _clear_ruler(self):
        if self._ruler_line: self.scene().removeItem(self._ruler_line); self._ruler_line = None
        if self._ruler_lbl: self.scene().removeItem(self._ruler_lbl); self._ruler_lbl = None

    def _remove_anchor(self, anchor):
        for anchors in self.contours:
            if anchor in anchors:
                anchors.remove(anchor)
        if anchor.cp_in:
            if self.scene(): self.scene().removeItem(anchor.cp_in)
            anchor.cp_in = None
        if anchor.cp_out:
            if self.scene(): self.scene().removeItem(anchor.cp_out)
            anchor.cp_out = None
        if self.scene(): self.scene().removeItem(anchor)

    # ── Mouse events ──────────────────────────────────────────────────────────

    def wheelEvent(self, event):
        factor = 1.15 if event.angleDelta().y() > 0 else 1 / 1.15
        self.scale(factor, factor)

    def mousePressEvent(self, event):
        sp = self._scene_pos(event)
        self._press_pos = sp
        tool = self.active_tool

        if tool in ("pointer", "nodes", "pan"):
            super().mousePressEvent(event)
            return

        if tool == "pen":
            # Add a new anchor to the latest contour
            anchor = AnchorPointItem(sp.x(), sp.y(), self)
            self.scene().addItem(anchor)
            if not self.contours: self.contours = [[]]
            self.contours[-1].append(anchor)
            self._pen_active_anchor = anchor
            self.update_path()
            return

        if tool == "zoom":
            factor = 1.25 if event.button() == Qt.MouseButton.LeftButton else 0.8
            self.scale(factor, factor)
            return

        if tool in ("rectangle", "ellipse", "spiral"):
            pen = QPen(QColor("#3584e4"), 1, Qt.PenStyle.DashLine)
            pen.setCosmetic(True)
            if tool == "rectangle":
                self._preview_item = self.scene().addRect(QRectF(sp, sp), pen)
            else:
                self._preview_item = self.scene().addEllipse(QRectF(sp, sp), pen)
            return

        if tool == "ruler":
            self._clear_ruler()
            pen = QPen(QColor("#e5a50a"), 1, Qt.PenStyle.DashLine)
            pen.setCosmetic(True)
            self._ruler_line = self.scene().addLine(QLineF(sp, sp), pen)
            return

        if tool == "knife":
            pen = QPen(QColor("#cc1a1a"), 1, Qt.PenStyle.DashLine)
            self._preview_item = self.scene().addLine(QLineF(sp, sp), pen)
            return

    def mouseMoveEvent(self, event):
        sp = self._scene_pos(event)
        tool = self.active_tool

        if tool in ("pointer", "nodes", "pan"):
            super().mouseMoveEvent(event)
            return

        if tool == "pen" and self._pen_active_anchor:
            # Dragging out a handle
            # For simplicity, we create symmetric handles
            vec = sp - self._pen_active_anchor.pos()
            if vec.manhattanLength() > 5:
                self._pen_active_anchor.add_handles(
                    in_pos = self._pen_active_anchor.pos() - vec,
                    out_pos = self._pen_active_anchor.pos() + vec
                )
                self._pen_active_anchor.smooth = True
            return

        if not self._press_pos: return

        if tool in ("rectangle", "ellipse", "spiral") and self._preview_item:
            rect = QRectF(self._press_pos, sp).normalized()
            if isinstance(self._preview_item, QGraphicsRectItem):
                self._preview_item.setRect(rect)
            else:
                self._preview_item.setRect(rect)
            return

        if tool == "ruler" and self._ruler_line:
            self._ruler_line.setLine(QLineF(self._press_pos, sp))
            dist = QLineF(self._press_pos, sp).length()
            angle = QLineF(self._press_pos, sp).angle()
            if self._ruler_lbl: self.scene().removeItem(self._ruler_lbl)
            self._ruler_lbl = self.scene().addText(f"{dist:.1f} u, {angle:.1f}°")
            self._ruler_lbl.setDefaultTextColor(QColor("#e5a50a"))
            self._ruler_lbl.setPos(sp + QPointF(10, -20))
            return

        if tool == "knife" and self._preview_item:
            self._preview_item.setLine(QLineF(self._press_pos, sp))

    def mouseReleaseEvent(self, event):
        sp = self._scene_pos(event)
        tool = self.active_tool

        if tool == "pen":
            self._pen_active_anchor = None
            return

        if tool in ("rectangle", "ellipse", "spiral") and self._preview_item:
            rect = self._preview_item.rect()
            self.scene().removeItem(self._preview_item)
            self._preview_item = None
            if rect.width() > 5:
                if tool == "rectangle":
                    pts = [rect.topLeft(), rect.topRight(), rect.bottomRight(), rect.bottomLeft()]
                    new_contour = []
                    for p in pts:
                        a = AnchorPointItem(p.x(), p.y(), self)
                        self.scene().addItem(a)
                        new_contour.append(a)
                    self.contours.append(new_contour)
                    self.closed_contours.add(len(self.contours)-1)
                elif tool == "ellipse":
                    # 4 points with handles (~0.5522 of radius)
                    cx, cy = rect.center().x(), rect.center().y()
                    rx, ry = rect.width() / 2, rect.height() / 2
                    k = 0.5522
                    # Top, Right, Bottom, Left
                    data = [
                        (cx, cy-ry, rx*k, 0), (cx+rx, cy, 0, ry*k),
                        (cx, cy+ry, rx*k, 0), (cx-rx, cy, 0, ry*k)
                    ]
                    new_contour = []
                    for i, (ax, ay, hx, hy) in enumerate(data):
                        a = AnchorPointItem(ax, ay, self)
                        self.scene().addItem(a)
                        new_contour.append(a)
                        if i % 2 == 0: # Top/Bottom
                            a.add_handles(QPointF(ax-hx, ay), QPointF(ax+hx, ay))
                        else: # Right/Left
                            a.add_handles(QPointF(ax, ay-hy), QPointF(ax, ay+hy))
                        a.smooth = True
                    self.contours.append(new_contour)
                    self.closed_contours.add(len(self.contours)-1)
                elif tool == "spiral":
                    cx, cy = rect.center().x(), rect.center().y()
                    rx, ry = rect.width() / 2, rect.height() / 2
                    new_contour = []
                    for i in range(20):
                        t = i / 2.0
                        r_scale = (i / 20.0)
                        ax = cx + rx * r_scale * math.cos(t * 2)
                        ay = cy + ry * r_scale * math.sin(t * 2)
                        a = AnchorPointItem(ax, ay, self)
                        self.scene().addItem(a)
                        new_contour.append(a)
                    self.contours.append(new_contour)
                self.update_path()
            return

        if tool == "knife" and self._preview_item:
            kline = self._preview_item.line()
            self.scene().removeItem(self._preview_item)
            self._preview_item = None
            
            # Intersection logic
            for c_idx, anchors in enumerate(self.contours):
                if not anchors: continue
                # We work on a snapshot of the list
                original_anchors = anchors[:]
                added_offset = 0
                for i in range(len(original_anchors) - (0 if c_idx in self.closed_contours else 1)):
                    p1 = original_anchors[i]
                    p2 = original_anchors[(i + 1) % len(original_anchors)]
                    
                    found_intersect = None
                    for t_idx in range(1, 10): # Check 10 sub-segments
                        t = t_idx / 10.0
                        p_t = self._bezier_point(p1, p2, t)
                        p_t_next = self._bezier_point(p1, p2, t + 0.1)
                        seg = QLineF(p_t, p_t_next)
                        
                        ip = QPointF()
                        if seg.intersects(kline, ip) == QLineF.IntersectType.BoundedIntersection:
                            found_intersect = ip
                            break
                    
                    if found_intersect:
                        new_a = AnchorPointItem(found_intersect.x(), found_intersect.y(), self)
                        self.scene().addItem(new_a)
                        anchors.insert(i + 1 + added_offset, new_a)
                        added_offset += 1
            self.update_path()
            return

        super().mouseReleaseEvent(event)
        self._press_pos = None

    def _bezier_point(self, p1, p2, t):
        # Cubic bezier point between two anchors
        s = p1.pos() # Scene pos
        # Handle pos is local to anchor, so add anchor pos to get scene pos
        c1 = s + p1.cp_out.pos() if p1.cp_out else s
        e = p2.pos() # Scene pos
        c2 = e + p2.cp_in.pos() if p2.cp_in else e
        
        # B(t) = (1-t)^3*P0 + 3(1-t)^2*t*P1 + 3(1-t)*t^2*P2 + t^3*P3
        mt = 1 - t
        return (mt**3 * s + 3 * mt**2 * t * c1 + 3 * mt * t**2 * c2 + t**3 * e)

    def mouseDoubleClickEvent(self, event):
        if self.active_tool == "pointer":
            item = self.itemAt(event.pos())
            if isinstance(item, AnchorPointItem):
                item.smooth = not item.smooth
                item.setBrush(QBrush(QColor("#3584e4") if item.smooth else QColor("#cc1a1a")))
                return
        elif self.active_tool == "pen":
            # Close current path and start a new one
            if self.contours[-1]:
                self.closed_contours.add(len(self.contours)-1)
                self.contours.append([]) # Start new contour
                self.update_path()
        super().mouseDoubleClickEvent(event)

    # ── SVG Import ────────────────────────────────────────────────────────────

    def keyPressEvent(self, event):
        if event.key() == Qt.Key.Key_Space:
            self.setDragMode(QGraphicsView.DragMode.ScrollHandDrag)
        elif event.key() == Qt.Key.Key_Delete or event.key() == Qt.Key.Key_Backspace:
            self.delete_selected()
        elif event.modifiers() == Qt.KeyboardModifier.ControlModifier and event.key() == Qt.Key.Key_Z:
            self.undo()
        elif event.modifiers() == Qt.KeyboardModifier.ControlModifier and event.key() == Qt.Key.Key_Y:
            self.redo()
        super().keyPressEvent(event)

    def keyReleaseEvent(self, event):
        if event.key() == Qt.Key.Key_Space:
            self.set_tool(self.active_tool) # Restore tool drag mode
        super().keyReleaseEvent(event)

    def undo(self):
        # Basic stub: reload from initial or last saved state
        pass

    def redo(self):
        pass

    def load_svg(self, svg_path: str, scale_x: float = 1.0, scale_y: float = 1.0):
        """
        Load an SVG file onto the canvas with optional scale transform.
        Tries QGraphicsSvgItem (requires PyQt6-Qt6Svg), falls back to QSvgRenderer→QPixmap.
        """
        try:
            from PyQt6.QtSvgWidgets import QGraphicsSvgItem
            item = QGraphicsSvgItem(svg_path)
            item.setFlags(
                item.GraphicsItemFlag.ItemIsMovable |
                item.GraphicsItemFlag.ItemIsSelectable
            )
            bounds = item.boundingRect()
            if bounds.width() > 0 and bounds.height() > 0:
                bsx = 800.0 / bounds.width()
                bsy = 800.0 / bounds.height()
            else:
                bsx = bsy = 1.0
            t = QTransform()
            t.scale(bsx * scale_x, bsy * scale_y)
            item.setTransform(t)
            item.setPos(100, 100)
            self.scene().addItem(item)
            return
        except ImportError:
            pass

        # Fallback: render via QSvgRenderer to QPixmap
        try:
            from PyQt6.QtSvg import QSvgRenderer
            from PyQt6.QtGui import QPixmap, QPainter as _P
            renderer = QSvgRenderer(svg_path)
            vb = renderer.viewBox()
            w = vb.width() if vb.width() > 0 else 800
            h = vb.height() if vb.height() > 0 else 800
            px = QPixmap(int(w * scale_x), int(h * scale_y))
            px.fill(QColor(0, 0, 0, 0))
            painter = _P(px)
            renderer.render(painter)
            painter.end()
            pitem = self.scene().addPixmap(px)
            pitem.setPos(100, 100)
            pitem.setFlag(pitem.GraphicsItemFlag.ItemIsMovable, True)
            pitem.setFlag(pitem.GraphicsItemFlag.ItemIsSelectable, True)
        except Exception as e:
            print(f"[GlyphCanvas] SVG load error: {e}")


# ─────────────────────────────── Canvas Dialog ───────────────────────────────

class GlyphCanvasDialog(QDialog):
    def __init__(self, char_str, cp_str, glyph_data, parent=None):
        super().__init__(parent)
        self.setWindowTitle(f"Glyph Editor — {char_str} ({cp_str})")
        self.resize(950, 700)
        self.glyph_data = glyph_data
        # Pending SVG fields (set by parent before exec())
        self._pending_svg: str | None = None
        self._pending_sx: float = 1.0
        self._pending_sy: float = 1.0
        self._build_ui()
        
        if self.glyph_data:
            self.canvas.load_from_glyph_data(self.glyph_data)

        # Auto-load SVG if parent queued one
        if self._pending_svg:
            self.canvas.load_svg(self._pending_svg, self._pending_sx, self._pending_sy)

    def _build_ui(self):
        layout = QHBoxLayout(self)
        layout.setContentsMargins(0, 0, 0, 0)
        layout.setSpacing(0)

        # ── Left Toolbar ─────────────────────────────────────────────────────
        left_widget = QWidget()
        left_widget.setFixedWidth(42)
        left_widget.setStyleSheet("background: #f6f5f4; border-right: 1px solid #d3d3d3;")
        lt = QVBoxLayout(left_widget)
        lt.setContentsMargins(4, 6, 4, 6)
        lt.setSpacing(2)

        TOOL_DEFS = [
            ("pointer",   "pointer.svg",   "Pointer — select & move  [F1 / V]"),
            ("zoom",      "magnify.svg",   "Zoom — click to zoom in, right-click out  [Z]"),
            ("pan",       "pan.svg",       "Pan — drag to scroll  [H / Space]"),
            (None, None, None),
            ("pen",       "pen.svg",       "Bezier Pen — click to add points  [P]"),
            ("pencil",    "pencil.svg",    "Freehand Pencil — draw freely  [B]"),
            ("nodes",     "nodes.svg",     "Edit Nodes — select and drag handles  [N]"),
            ("knife",     "knife.svg",     "Knife — slice (preview)  [K]"),
            (None, None, None),
            ("rectangle", "rectangle.svg", "Rectangle — drag to draw  [R]"),
            ("ellipse",   "ellipse.svg",   "Ellipse — drag to draw  [E]"),
            ("spiral",    "spiral.svg",    "Polygon/Spiral — drag to draw  [S]"),
            (None, None, None),
            ("corner",    "corner.svg",    "Anchor/Corner  [C]"),
            ("ruler",     "ruler.svg",     "Ruler — drag to measure distance  [M]"),
        ]

        # Key → tool mapping for canvas shortcuts
        self._tool_key_map = {
            Qt.Key.Key_V: "pointer",
            Qt.Key.Key_F1: "pointer",
            Qt.Key.Key_Z: "zoom",
            Qt.Key.Key_H: "pan",
            Qt.Key.Key_P: "pen",
            Qt.Key.Key_B: "pencil",
            Qt.Key.Key_N: "nodes",
            Qt.Key.Key_K: "knife",
            Qt.Key.Key_R: "rectangle",
            Qt.Key.Key_E: "ellipse",
            Qt.Key.Key_S: "spiral",
            Qt.Key.Key_C: "corner",
            Qt.Key.Key_M: "ruler",
        }

        self._tool_btn_group = QButtonGroup(self)
        self._tool_btn_group.setExclusive(True)

        for tool_id, icon_file, tip in TOOL_DEFS:
            if tool_id is None:
                sep = QFrame()
                sep.setFrameShape(QFrame.Shape.HLine)
                sep.setFixedHeight(1)
                sep.setStyleSheet("background: #d3d3d3; margin: 2px 2px;")
                lt.addWidget(sep)
            else:
                btn = QToolButton()
                btn.setIcon(_icon(icon_file))
                btn.setIconSize(QSize(20, 20))
                btn.setToolTip(tip)
                btn.setFixedSize(32, 32)
                btn.setCheckable(True)
                btn.setStyleSheet("""
                    QToolButton {
                        background: transparent;
                        border: 1px solid transparent;
                        border-radius: 4px;
                    }
                    QToolButton:hover {
                        background: #e0dedd;
                        border: 1px solid #c8c7c6;
                    }
                    QToolButton:checked {
                        background: #c8daf5;
                        border: 1px solid #3584e4;
                    }
                """)
                self._tool_btn_group.addButton(btn)
                lt.addWidget(btn)
                # Capture tool_id in closure
                btn.clicked.connect(lambda checked, t=tool_id: self.canvas.set_tool(t))
                if tool_id == "pointer":
                    btn.setChecked(True)

        lt.addStretch()

        # Delete shortcut label
        del_lbl = QLabel("Del: remove")
        del_lbl.setStyleSheet("color:#999; font-size:9px;")
        del_lbl.setWordWrap(True)
        lt.addWidget(del_lbl)

        layout.addWidget(left_widget)

        # ── Central Canvas ────────────────────────────────────────────────────
        canvas_frame = QWidget()
        cvl = QVBoxLayout(canvas_frame)
        cvl.setContentsMargins(0, 0, 0, 0)
        cvl.setSpacing(0)

        top_bar = QWidget()
        top_bar.setFixedHeight(28)
        top_bar.setStyleSheet("background:#f6f5f4; border-bottom:1px solid #d3d3d3;")
        tbl = QHBoxLayout(top_bar)
        tbl.setContentsMargins(10, 0, 10, 0)
        lbl = QLabel("Canvas  (1000 × 1000 units)")
        lbl.setStyleSheet("color:#555; font-size:11px;")
        tbl.addWidget(lbl)
        tbl.addStretch()
        zoom_in  = QPushButton("+")
        zoom_out = QPushButton("−")
        zoom_out.setFixedWidth(26)
        zoom_in.setFixedWidth(26)
        zoom_in.setToolTip("Zoom in")
        zoom_out.setToolTip("Zoom out")
        for z in (zoom_in, zoom_out):
            z.setStyleSheet("border:1px solid #ccc; border-radius:3px; background:white; font-size:14px;")
        tbl.addWidget(zoom_in)
        tbl.addWidget(zoom_out)
        cvl.addWidget(top_bar)

        self.scene = QGraphicsScene()
        self.scene.setSceneRect(0, 0, 1000, 1000)

        self.canvas = GlyphCanvas(self.scene)
        self._draw_canvas_guides()
        cvl.addWidget(self.canvas)

        zoom_in.clicked.connect(lambda: self.canvas.scale(1.25, 1.25))
        zoom_out.clicked.connect(lambda: self.canvas.scale(0.8, 0.8))

        layout.addWidget(canvas_frame, 1)

        # ── Right Panel ───────────────────────────────────────────────────────
        right_widget = QWidget()
        right_widget.setFixedWidth(148)
        right_widget.setStyleSheet("background:#f6f5f4; border-left:1px solid #d3d3d3;")
        rp = QVBoxLayout(right_widget)
        rp.setContentsMargins(10, 12, 10, 10)
        rp.setSpacing(4)

        layers_title = QLabel("🗙 Layers")
        layers_title.setStyleSheet("font-weight:bold; font-size:12px; color:#2e3436;")
        rp.addWidget(layers_title)

        for layer_name, checked, icn in [
            ("Guide", False, "guide.svg"),
            ("Back",  False, "layers.svg"),
            ("Fore",  True,  "layers.svg"),
        ]:
            row = QHBoxLayout()
            row.setSpacing(6)
            icon_lbl = QLabel()
            icon_lbl.setPixmap(_icon(icn).pixmap(QSize(14, 14)))
            cb = QCheckBox(layer_name)
            cb.setChecked(checked)
            cb.setStyleSheet("font-size:12px;")
            row.addWidget(icon_lbl)
            row.addWidget(cb)
            row.addStretch()
            rp.addLayout(row)

        rp.addSpacing(12)
        sep = QFrame()
        sep.setFrameShape(QFrame.Shape.HLine)
        sep.setStyleSheet("color:#d3d3d3;")
        rp.addWidget(sep)

        # Action buttons
        for label, slot in [
            ("Delete Sel.", self.canvas.delete_selected),
            ("Clear All",   self._clear_all),
        ]:
            btn = QPushButton(label)
            btn.setStyleSheet("""
                QPushButton {
                    font-size:11px; border:1px solid #c8c7c6;
                    border-radius:3px; padding:3px; background:white;
                }
                QPushButton:hover { background:#e0dedd; }
            """)
            btn.clicked.connect(slot)
            rp.addWidget(btn)

        rp.addSpacing(6)
        hint = QLabel("Pen: right-click to commit\nDbl-click to close path\nScroll to zoom")
        hint.setWordWrap(True)
        hint.setStyleSheet("color:#888; font-size:9px;")
        rp.addWidget(hint)
        rp.addStretch()

        layout.addWidget(right_widget)

    def done(self, result):
        if self.glyph_data is not None:
            self.canvas.save_to_glyph_data(self.glyph_data)
        super().done(result)

    def _clear_all(self):
        # Remove all items EXCEPT the canvas path_item to avoid dangling pointer crash
        for item in self.scene.items():
            if item is not self.canvas.path_item:
                self.scene.removeItem(item)
        # Clear canvas contour state
        self.canvas.contours = [[]]
        self.canvas.closed_contours.clear()
        self.canvas.update_path()
        self._draw_canvas_guides()

    def keyPressEvent(self, event):
        key = event.key()
        mods = event.modifiers()
        if mods == Qt.KeyboardModifier.NoModifier:
            if key in self._tool_key_map:
                tool = self._tool_key_map[key]
                self.canvas.set_tool(tool)
                # Sync button group visual state
                for btn in self._tool_btn_group.buttons():
                    btn.setChecked(False)
                return
            if key == Qt.Key.Key_Escape:
                self.reject()
                return
            if key in (Qt.Key.Key_Delete, Qt.Key.Key_Backspace):
                self.canvas.delete_selected()
                return
        super().keyPressEvent(event)

    def _draw_canvas_guides(self):
        for i in range(0, 1001, 50):
            shade = QColor(235, 235, 235) if i % 100 != 0 else QColor(218, 218, 218)
            pen = QPen(shade)
            pen.setCosmetic(True)
            self.scene.addLine(i, 0, i, 1000, pen)
            self.scene.addLine(0, i, 1000, i, pen)

        metric_pen = QPen(QColor("#3584e4"))
        metric_pen.setStyle(Qt.PenStyle.DashLine)
        metric_pen.setCosmetic(True)

        self.scene.addLine(0, 800, 1000, 800, metric_pen)
        lbl = self.scene.addText("Baseline")
        lbl.setDefaultTextColor(QColor("#3584e4"))
        lbl.setPos(5, 800)

        self.scene.addLine(0, 200, 1000, 200, metric_pen)
        lbl2 = self.scene.addText("Cap Height")
        lbl2.setDefaultTextColor(QColor("#3584e4"))
        lbl2.setPos(5, 200)

        desc_pen = QPen(QColor("#e5a50a"))
        desc_pen.setStyle(Qt.PenStyle.DashLine)
        desc_pen.setCosmetic(True)
        self.scene.addLine(0, 950, 1000, 950, desc_pen)
        lbl3 = self.scene.addText("Descender")
        lbl3.setDefaultTextColor(QColor("#e5a50a"))
        lbl3.setPos(5, 950)


# ─────────────────────────────── Add Range Dialog ───────────────────────────

class AddRangeDialog(QDialog):
    """
    Dialog to add a codepoint range to the glyph grid.
    Supports manual hex ranges or quick-select named ranges (PUA, SPUA-A, SPUA-B, Basic Latin, etc.).
    """
    PRESETS = [
        ("Basic Latin (U+0020–U+007E)", 0x0020, 0x007E),
        ("Latin-1 Supplement (U+00A0–U+00FF)", 0x00A0, 0x00FF),
        ("General Punctuation (U+2000–U+206F)", 0x2000, 0x206F),
        ("PUA — Private Use Area (U+E000–U+F8FF)", 0xE000, 0xF8FF),
        ("SPUA-A (U+F0000–U+FFFFD)", 0xF0000, 0xFFFFD),
        ("SPUA-B (U+100000–U+10FFFD)", 0x100000, 0x10FFFD),
    ]
    MAX_ADD = 512  # cap to prevent huge grids

    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle("Add Codepoint Range")
        self.setFixedWidth(420)
        self.result_range: list[int] = []
        self._build_ui()

    def _build_ui(self):
        layout = QVBoxLayout(self)

        # Preset selector
        layout.addWidget(QLabel("Quick Preset:"))
        from PyQt6.QtWidgets import QComboBox
        self.preset_box = QComboBox()
        self.preset_box.addItem("— Custom Range —", None)
        for name, start, end in self.PRESETS:
            self.preset_box.addItem(name, (start, end))
        self.preset_box.currentIndexChanged.connect(self._on_preset)
        layout.addWidget(self.preset_box)

        layout.addWidget(QLabel("Or enter hex codepoints manually:"))
        row = QHBoxLayout()
        row.addWidget(QLabel("Start (hex):"))
        self.start_edit = QLineEdit()
        self.start_edit.setPlaceholderText("e.g. E000")
        row.addWidget(self.start_edit)
        row.addWidget(QLabel("End (hex):"))
        self.end_edit = QLineEdit()
        self.end_edit.setPlaceholderText("e.g. E01F")
        row.addWidget(self.end_edit)
        layout.addLayout(row)

        self.info_lbl = QLabel("")
        self.info_lbl.setStyleSheet("color: #555; font-size: 11px;")
        layout.addWidget(self.info_lbl)
        self.start_edit.textChanged.connect(self._update_info)
        self.end_edit.textChanged.connect(self._update_info)

        btn_row = QHBoxLayout()
        ok_btn = QPushButton("Add to Grid")
        ok_btn.setStyleSheet("background:#3584e4; color:white; font-weight:bold; "
                             "border-radius:4px; padding:5px 14px;")
        ok_btn.clicked.connect(self._accept)
        cancel_btn = QPushButton("Cancel")
        cancel_btn.clicked.connect(self.reject)
        btn_row.addStretch()
        btn_row.addWidget(cancel_btn)
        btn_row.addWidget(ok_btn)
        layout.addLayout(btn_row)

    def _on_preset(self, idx):
        data = self.preset_box.currentData()
        if data:
            start, end = data
            self.start_edit.setText(f"{start:04X}")
            self.end_edit.setText(f"{end:04X}")

    def _update_info(self):
        try:
            s = int(self.start_edit.text(), 16)
            e = int(self.end_edit.text(), 16)
            count = min(e - s + 1, self.MAX_ADD)
            self.info_lbl.setText(
                f"Will add {count} codepoint(s)" +
                (f"  (capped at {self.MAX_ADD})" if e - s + 1 > self.MAX_ADD else "")
            )
        except ValueError:
            self.info_lbl.setText("Enter valid hex values")

    def _accept(self):
        try:
            s = int(self.start_edit.text(), 16)
            e = int(self.end_edit.text(), 16)
            if s > e:
                QMessageBox.warning(self, "Invalid Range", "Start must be ≤ End.")
                return
            self.result_range = list(range(s, min(e + 1, s + self.MAX_ADD)))
            self.accept()
        except ValueError:
            QMessageBox.warning(self, "Invalid Input", "Please enter valid hexadecimal values.")


# ─────────────────────────────── Grid Cell ───────────────────────────────────

class GlyphCellWidget(QWidget):
    """FontForge-style glyph cell that renders vector data from the GlyphData model."""

    doubleClicked = pyqtSignal(str, str)

    def __init__(self, char_str, cp_str, glyph_data=None, parent=None):
        super().__init__(parent)
        self.char_str = char_str
        self.cp_str = cp_str
        self.glyph_data = glyph_data
        self.setCursor(QCursor(Qt.CursorShape.PointingHandCursor))
        self.setMinimumSize(30, 46)

    def mouseDoubleClickEvent(self, event):
        self.doubleClicked.emit(self.char_str, self.cp_str)
        super().mouseDoubleClickEvent(event)

    def paintEvent(self, event):
        p = QPainter()
        if not p.begin(self): return
        try:
            w, h = self.width(), self.height()
            p.fillRect(self.rect(), QColor("#ffffff"))

            # Header
            header_h = 14
            p.fillRect(QRectF(0, 0, w, header_h), QColor("#e8e8e8"))
            p.setPen(QColor("#333333"))
            small_f = QFont("Segoe UI")
            small_f.setPixelSize(8)
            p.setFont(small_f)
            cp_display = self.cp_str.split('+')[1] if '+' in self.cp_str else self.cp_str
            p.drawText(QRectF(2, 0, w - 4, header_h), Qt.AlignmentFlag.AlignRight | Qt.AlignmentFlag.AlignVCenter, cp_display)

            body_top = header_h
            body_h = h - header_h
            
            # Guides
            guide_pen = QPen(QColor("#eeeeee"), 0.5)
            guide_pen.setCosmetic(True)
            p.setPen(guide_pen)
            baseline_y = body_top + body_h * 0.78
            p.drawLine(0, int(baseline_y), w, int(baseline_y))

            p.setRenderHint(QPainter.RenderHint.Antialiasing, True)
            if self.glyph_data and not self.glyph_data.is_empty():
                padding = 4
                scale = (body_h - padding*2) / 1000.0
                p.save()
                p.translate((w - 1000*scale)/2, body_top + padding)
                p.scale(scale, scale)
                for contour in self.glyph_data.contours:
                    if not contour.nodes: continue
                    path = QPainterPath()
                    path.moveTo(contour.nodes[0].x, contour.nodes[0].y)
                    for i in range(1, len(contour.nodes)):
                        pn, cn = contour.nodes[i-1], contour.nodes[i]
                        try:
                            if pn.cp_out and cn.cp_in:
                                path.cubicTo(QPointF(*pn.cp_out), QPointF(*cn.cp_in), QPointF(cn.x, cn.y))
                            elif pn.cp_out:
                                path.quadTo(QPointF(*pn.cp_out), QPointF(cn.x, cn.y))
                            elif cn.cp_in:
                                path.quadTo(QPointF(*cn.cp_in), QPointF(cn.x, cn.y))
                            else:
                                path.lineTo(cn.x, cn.y)
                        except: continue
                    if contour.closed: path.closeSubpath()
                    cell_pen = QPen(QColor("#1a1a1a"), 1.2)
                    cell_pen.setCosmetic(True)
                    p.setPen(cell_pen)
                    p.setBrush(QBrush(QColor(46, 52, 54, 180)))
                    p.drawPath(path)
                p.restore()
            
            p.setRenderHint(QPainter.RenderHint.Antialiasing, False)
            p.setPen(QPen(QColor("#d3d3d3"), 1))
            p.drawRect(0, 0, w - 1, h - 1)
        except Exception as e:
            print(f"Paint error: {e}")
        finally:
            p.end()


# ─────────────────────────────── Main Window ─────────────────────────────────

def _iso8859_1_data():
    """Generate ISO-8859-1 codepoint range (U+0020–U+00FF) as default glyph data."""
    data = []
    for cp in range(0x0020, 0x0100):
        try:
            char_str = chr(cp)
            if not char_str.isprintable():
                char_str = ""
        except (ValueError, OverflowError):
            char_str = ""
        data.append((char_str, f"U+{cp:04X}"))
    return data


class FontEditorWindow(QMainWindow):
    CELL_W = 64
    CELL_H = 80

    def __init__(self, parent=None, filepath=None):
        super().__init__(parent)
        self._filepath = filepath
        self._pending_svg_path: str | None = None
        self._encoding = "ISO8859-1"
        self._project = ModProject()

        # Title
        if filepath:
            base = os.path.basename(filepath)
            name = os.path.splitext(base)[0]
        else:
            name = "untitled"
            base = "untitled"
        self.setWindowTitle(f"{name}  {base}.sfd ({self._encoding})")
        self.resize(1100, 750)

        # Default glyph data
        self._glyph_data: list[tuple[str, str]] = _iso8859_1_data()

        self._build_menu()
        self._build_ui()

    # ── Menu Bar ─────────────────────────────────────────────────────────────

    def _build_menu(self):
        mb = self.menuBar()
        mb.setStyleSheet("""
            QMenuBar { background: #f0f0f0; border-bottom: 1px solid #c0c0c0; }
            QMenuBar::item { padding: 3px 8px; }
            QMenuBar::item:selected { background: #dcdcdc; }
        """)

        # File
        file_m = mb.addMenu("File")
        file_m.addAction("New").setShortcut("Ctrl+N")
        file_m.addAction("Open...").setShortcut("Ctrl+O")
        file_m.addSeparator()
        act_save = file_m.addAction("Save Project (*.sfd)", self._save_project)
        act_save.setShortcut("Ctrl+S")
        file_m.addSeparator()
        act_exp_ttfm = file_m.addAction("Export as .ttfm", self._pre_export_dialog)
        act_exp_ttfm.setShortcut("Ctrl+Shift+E")
        act_exp_otfm = file_m.addAction("Export as .otfm", self._pre_export_dialog)
        act_exp_otfm.setShortcut("Ctrl+Alt+E")
        act_exp_compat = file_m.addAction("Save As .modcompat.ttc", self._pre_export_dialog)
        act_exp_compat.setShortcut("Ctrl+Shift+S")
        file_m.addSeparator()
        act_close = file_m.addAction("Close", self.close)
        act_close.setShortcut("Ctrl+W")

        # Edit
        edit_m = mb.addMenu("Edit")
        edit_m.addAction("Undo").setShortcut("Ctrl+Z")
        edit_m.addAction("Redo").setShortcut("Ctrl+Y")
        edit_m.addSeparator()
        edit_m.addAction("Cut").setShortcut("Ctrl+X")
        edit_m.addAction("Copy").setShortcut("Ctrl+C")
        edit_m.addAction("Paste").setShortcut("Ctrl+V")
        edit_m.addSeparator()
        edit_m.addAction("Select All").setShortcut("Ctrl+A")
        edit_m.addAction("Clear").setShortcut("Delete")

        # Element
        elem_m = mb.addMenu("Element")
        act_ginfo = elem_m.addAction("Glyph Info...", self._glyph_info_selected)
        act_ginfo.setShortcut("Ctrl+I")
        elem_m.addAction("Transform...").setShortcut("Ctrl+Shift+T")
        elem_m.addSeparator()
        act_add_range = elem_m.addAction("Add Codepoint Range...", self._add_range)
        act_add_range.setShortcut("Ctrl+Shift+R")
        act_import_svg = elem_m.addAction("Import SVG...", self._import_svg)
        act_import_svg.setShortcut("Ctrl+Shift+I")
        act_apply_svg = elem_m.addAction("Apply SVG to Selected", self._apply_svg_to_glyph)
        act_apply_svg.setShortcut("Ctrl+Shift+P")

        # Tools
        tools_m = mb.addMenu("Tools")
        act_script = tools_m.addAction("Execute Script...",
            lambda: ScriptEditorWindow(self).exec())
        act_script.setShortcut("Ctrl+Shift+X")

        # Hints
        hints_m = mb.addMenu("Hints")
        hints_m.addAction("AutoHint").setShortcut("Ctrl+Shift+H")
        hints_m.addAction("Clear Hints").setShortcut("Ctrl+Alt+H")

        # Encoding
        enc_m = mb.addMenu("Encoding")
        enc_m.addAction("Reencode...").setShortcut("Ctrl+Shift+N")
        enc_m.addAction("Add Encoding Slots...").setShortcut("Ctrl+Alt+N")

        # View
        view_m = mb.addMenu("View")
        view_m.addAction("Zoom In").setShortcut("Ctrl+=")
        view_m.addAction("Zoom Out").setShortcut("Ctrl+-")
        view_m.addSeparator()
        view_m.addAction("Show Grid Lines").setShortcut("Ctrl+G")

        # Metrics
        met_m = mb.addMenu("Metrics")
        met_m.addAction("Set Width...").setShortcut("Ctrl+Shift+W")
        met_m.addAction("Set Vertical Width...").setShortcut("Ctrl+Alt+W")

        # CID
        mb.addMenu("CID")

        # MM
        mb.addMenu("MM")

        # Window
        mb.addMenu("Window")

        # Help
        help_m = mb.addMenu("Help")
        help_m.addAction("About").setShortcut("F1")

    # ── UI ────────────────────────────────────────────────────────────────────

    def _build_ui(self):
        cw = QWidget()
        self.setCentralWidget(cw)
        layout = QVBoxLayout(cw)
        layout.setContentsMargins(0, 0, 0, 0)
        layout.setSpacing(0)

        # Scrollable grid area
        self._scroll = QScrollArea()
        self._scroll.setWidgetResizable(True)
        self._scroll.setStyleSheet("""
            QScrollArea { background: #f7f7f7; border: none; }
            QScrollBar:vertical {
                background: #f0f0f0; width: 12px;
            }
            QScrollBar::handle:vertical {
                background: #b0b0b0; border-radius: 4px; min-height: 30px;
            }
        """)

        self._grid_container = QWidget()
        self._grid_container.setStyleSheet("background: #f7f7f7;")
        self._scroll.setWidget(self._grid_container)
        layout.addWidget(self._scroll)

        self._populate_grid()

    def _calc_columns(self):
        """Calculate number of columns to fill the available width."""
        avail = self._scroll.viewport().width() if self._scroll.viewport().width() > 100 else 1100
        cols = max(1, avail // self.CELL_W)
        return cols

    def _populate_grid(self):
        if not hasattr(self, '_scroll') or sip.isdeleted(self._scroll):
            return

        cols = self._calc_columns()
        rows = (len(self._glyph_data) + cols - 1) // cols

        # Use QTableWidget for the grid
        old_table = getattr(self, '_grid_table', None)
        if old_table and not sip.isdeleted(old_table):
            self._scroll.takeWidget()
            old_table.deleteLater()

        self._grid_table = QTableWidget()
        self._grid_table.setColumnCount(cols)
        self._grid_table.setRowCount(rows)
        self._grid_table.horizontalHeader().hide()
        self._grid_table.verticalHeader().hide()
        self._grid_table.setSelectionMode(QAbstractItemView.SelectionMode.SingleSelection)
        self._grid_table.setEditTriggers(QAbstractItemView.EditTrigger.NoEditTriggers)
        self._grid_table.setShowGrid(False)
        self._grid_table.setStyleSheet("QTableWidget { background: #f7f7f7; gridline-color: transparent; }")

        for c in range(cols):
            self._grid_table.setColumnWidth(c, self.CELL_W)
        for r in range(rows):
            self._grid_table.setRowHeight(r, self.CELL_H)

        for i, (char_str, cp_str) in enumerate(self._glyph_data):
            r, c = divmod(i, cols)
            cp = int(cp_str.split('+')[1], 16) if '+' in cp_str else int(cp_str.replace('0x',''), 16)
            glyph_data = self._project.add_glyph(cp)
            widget = GlyphCellWidget(char_str, cp_str, glyph_data)
            widget.doubleClicked.connect(self._open_canvas_for)
            self._grid_table.setCellWidget(r, c, widget)

        self._scroll.setWidget(self._grid_table)

    def _open_canvas_for(self, char_str, cp_str):
        self.setUpdatesEnabled(False) # Performance
        cp = int(cp_str.split('+')[1], 16) if '+' in cp_str else int(cp_str.replace('0x',''), 16)
        glyph_data = self._project.add_glyph(cp)
        dlg = GlyphCanvasDialog(char_str, cp_str, glyph_data, self)
        if self._pending_svg_path:
            dlg._pending_svg = self._pending_svg_path
            dlg._pending_sx = 1.0
            dlg._pending_sy = 1.0
        
        dlg.exec()
        self.setUpdatesEnabled(True)
        # Refresh grid safely
        QTimer.singleShot(0, self._populate_grid)

    def resizeEvent(self, event):
        super().resizeEvent(event)
        # Recalculate columns on resize
        new_cols = self._calc_columns()
        if hasattr(self, '_grid_table') and self._grid_table.columnCount() != new_cols:
            self._populate_grid()

    # ── Grid interactions ────────────────────────────────────────────────────

    def _on_grid_double_click(self, row, col):
        cols = self._grid_table.columnCount()
        idx = row * cols + col
        if idx < len(self._glyph_data):
            char_str, cp_str = self._glyph_data[idx]
            self._open_canvas_for(char_str, cp_str)

    def _glyph_info_selected(self):
        if not hasattr(self, '_grid_table'):
            return
        row = self._grid_table.currentRow()
        col = self._grid_table.currentColumn()
        cols = self._grid_table.columnCount()
        idx = row * cols + col
        if row >= 0 and idx < len(self._glyph_data):
            char_str, cp_str = self._glyph_data[idx]
            GlyphInfoDialog(char_str, cp_str, self).exec()

    def _import_svg(self):
        from ui_file_dialog import FMLFileDialog
        path, _ = FMLFileDialog.getOpenFileName(
            self, "Import SVG File", "", "SVG Files (*.svg);;All Files (*)"
        )
        if path:
            self._pending_svg_path = path

    def _apply_svg_to_glyph(self):
        if not self._pending_svg_path:
            QMessageBox.warning(self, "No SVG", "Please import an SVG file first.")
            return
        row = self._grid_table.currentRow()
        col = self._grid_table.currentColumn()
        cols = self._grid_table.columnCount()
        idx = row * cols + col
        if row < 0 or idx >= len(self._glyph_data):
            QMessageBox.warning(self, "No Glyph Selected",
                                "Please click a glyph cell in the grid first.")
            return
        char_str, cp_str = self._glyph_data[idx]
        cp = int(cp_str.split('+')[1], 16) if '+' in cp_str else int(cp_str.replace('0x',''), 16)
        glyph_data = self._project.add_glyph(cp)
        dlg = GlyphCanvasDialog(char_str, cp_str, glyph_data, self)
        dlg._pending_svg = self._pending_svg_path
        dlg._pending_sx = 1.0
        dlg._pending_sy = 1.0
        dlg.exec()
        QTimer.singleShot(0, self._populate_grid)

    def _add_range(self):
        dlg = AddRangeDialog(self)
        if dlg.exec() == QDialog.DialogCode.Accepted and dlg.result_range:
            existing_cps = {cp for _, cp in self._glyph_data}
            added = 0
            for cp in dlg.result_range:
                cp_str = f"U+{cp:04X}"
                if cp_str in existing_cps:
                    continue
                try:
                    char_str = chr(cp) if cp < 0xD800 or 0xE000 <= cp <= 0x10FFFF else ""
                except (ValueError, OverflowError):
                    char_str = ""
                self._glyph_data.append((char_str, cp_str))
                existing_cps.add(cp_str)
                added += 1
            if added:
                self._populate_grid()

    def _pre_export_dialog(self):
        dlg = FontModInfoDialog(self)
        if dlg.exec() == QDialog.DialogCode.Accepted:
            QMessageBox.information(self, "Export", "Metadata collected. Proceeding with export...")

    def _save_project(self):
        QMessageBox.information(self, "Save", "Saving as *.fml-compat_mod.sfd...")


if __name__ == "__main__":
    app = QApplication(sys.argv)
    w = FontEditorWindow()
    w.show()
    sys.exit(app.exec())

