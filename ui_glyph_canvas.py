"""
ui_glyph_canvas.py
Interactive vector drawing canvas for the fModLoader Glyph Editor.
Supports pen tool (click=corner, drag=curve), select + drag nodes/handles,
delete nodes, undo, zoom/pan, and em-box guide lines.
"""

import math
from PyQt6.QtWidgets import QWidget, QSizePolicy
from PyQt6.QtGui import (
    QPainter, QColor, QPen, QBrush, QPainterPath,
    QCursor, QTransform, QFont
)
from PyQt6.QtCore import Qt, QPointF, QRectF, pyqtSignal

from glyph_model import GlyphData, GlyphContour, PathNode

# ── Constants ─────────────────────────────────────────────────────────────────
NODE_RADIUS = 5
HANDLE_RADIUS = 4
HIT_RADIUS = 8
ZOOM_STEP = 1.15


class GlyphCanvas(QWidget):
    """Vector drawing canvas.  Emits changed() when glyph is modified."""
    changed = pyqtSignal()

    # tools
    TOOL_PEN    = "pen"
    TOOL_SELECT = "select"
    TOOL_DELETE = "delete"

    def __init__(self, parent=None):
        super().__init__(parent)
        self.setMinimumSize(400, 400)
        self.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Expanding)
        self.setMouseTracking(True)
        self.setCursor(Qt.CursorShape.CrossCursor)
        self.setFocusPolicy(Qt.FocusPolicy.StrongFocus)

        # em-box metrics (canvas units)
        self.units_per_em = 1000
        self.ascender      = 800
        self.descender     = -200
        self.x_height      = 500
        self.cap_height    = 700

        # view transform
        self._zoom = 1.0
        self._pan  = QPointF(0, 0)
        self._pan_start = None
        self._pan_origin = None

        # glyph data
        self._glyph: GlyphData | None = None

        # drawing state
        self.tool = self.TOOL_PEN
        self._active_contour: GlyphContour | None = None
        self._drag_node = None        # (contour_idx, node_idx)
        self._drag_handle = None      # (contour_idx, node_idx, 'in'|'out')
        self._drag_start = None       # QPointF canvas coords at drag start
        self._hover_pos: QPointF | None = None
        self._undo_stack: list = []

        self._reset_view()

    # ── Public API ────────────────────────────────────────────────────────────

    def set_glyph(self, glyph: GlyphData):
        self._glyph = glyph
        self._active_contour = None
        self._drag_node = None
        self._drag_handle = None
        self._undo_stack.clear()
        self.update()

    def get_glyph(self) -> GlyphData | None:
        return self._glyph

    def set_tool(self, tool: str):
        self.tool = tool
        if tool == self.TOOL_PEN:
            self.setCursor(Qt.CursorShape.CrossCursor)
        elif tool == self.TOOL_SELECT:
            self.setCursor(Qt.CursorShape.ArrowCursor)
        else:
            self.setCursor(Qt.CursorShape.ForbiddenCursor)
        self._active_contour = None
        self.update()

    def set_metrics(self, units_per_em, ascender, descender, x_height, cap_height):
        self.units_per_em = units_per_em
        self.ascender = ascender
        self.descender = descender
        self.x_height = x_height
        self.cap_height = cap_height
        self._reset_view()
        self.update()

    def clear_glyph(self):
        if self._glyph:
            self._push_undo()
            self._glyph.contours.clear()
            self._active_contour = None
            self.changed.emit()
            self.update()

    def finish_path(self):
        """End current open path without closing it."""
        self._active_contour = None
        self.update()

    def undo(self):
        if self._undo_stack and self._glyph:
            self._glyph.contours = self._undo_stack.pop()
            self._active_contour = None
            self.changed.emit()
            self.update()

    # ── View helpers ──────────────────────────────────────────────────────────

    def _reset_view(self):
        """Fit the em-box into the widget."""
        self._zoom = 1.0
        self._pan  = QPointF(0, 0)
        self.update()

    def _em_to_widget(self, ex: float, ey: float) -> QPointF:
        """Convert em-box coords to widget pixel coords."""
        w, h = self.width(), self.height()
        margin = 50
        available_h = h - 2 * margin
        available_w = w - 2 * margin
        em_h = self.ascender - self.descender
        scale = min(available_w / self.units_per_em, available_h / em_h) * self._zoom
        origin_x = w / 2 + self._pan.x()
        origin_y = h / 2 + self._pan.y() + (self.ascender + self.descender) / 2 * scale
        px = origin_x + ex * scale
        py = origin_y - ey * scale  # Y flipped
        return QPointF(px, py)

    def _widget_to_em(self, px: float, py: float) -> QPointF:
        """Convert widget pixel coords to em-box coords."""
        w, h = self.width(), self.height()
        margin = 50
        available_h = h - 2 * margin
        available_w = w - 2 * margin
        em_h = self.ascender - self.descender
        scale = min(available_w / self.units_per_em, available_h / em_h) * self._zoom
        origin_x = w / 2 + self._pan.x()
        origin_y = h / 2 + self._pan.y() + (self.ascender + self.descender) / 2 * scale
        ex = (px - origin_x) / scale
        ey = -(py - origin_y) / scale
        return QPointF(ex, ey)

    # ── Push undo ─────────────────────────────────────────────────────────────

    def _push_undo(self):
        import copy
        if self._glyph:
            self._undo_stack.append(copy.deepcopy(self._glyph.contours))
            if len(self._undo_stack) > 50:
                self._undo_stack.pop(0)

    # ── Hit testing ───────────────────────────────────────────────────────────

    def _hit_node(self, pos: QPointF):
        """Returns (contour_idx, node_idx) or None."""
        if not self._glyph:
            return None
        for ci, contour in enumerate(self._glyph.contours):
            for ni, node in enumerate(contour.nodes):
                wp = self._em_to_widget(node.x, node.y)
                if (pos - wp).manhattanLength() < HIT_RADIUS:
                    return (ci, ni)
        return None

    def _hit_handle(self, pos: QPointF):
        """Returns (contour_idx, node_idx, 'in'|'out') or None."""
        if not self._glyph:
            return None
        for ci, contour in enumerate(self._glyph.contours):
            for ni, node in enumerate(contour.nodes):
                for side, cp in [("in", node.cp_in), ("out", node.cp_out)]:
                    if cp is not None:
                        wp = self._em_to_widget(cp[0], cp[1])
                        if (pos - wp).manhattanLength() < HIT_RADIUS:
                            return (ci, ni, side)
        return None

    # ── Mouse events ──────────────────────────────────────────────────────────

    def mousePressEvent(self, event):
        pos = event.position()

        # Middle button or Space+drag = pan
        if event.button() == Qt.MouseButton.MiddleButton:
            self._pan_start = pos
            self._pan_origin = QPointF(self._pan)
            return

        ep = self._widget_to_em(pos.x(), pos.y())

        if self.tool == self.TOOL_PEN:
            self._pen_press(event, pos, ep)
        elif self.tool == self.TOOL_SELECT:
            self._select_press(event, pos, ep)
        elif self.tool == self.TOOL_DELETE:
            self._delete_press(pos)

    def mouseMoveEvent(self, event):
        pos = event.position()
        self._hover_pos = pos

        # Pan
        if self._pan_start is not None:
            delta = pos - self._pan_start
            self._pan = self._pan_origin + delta
            self.update()
            return

        # Drag node
        if self._drag_node is not None:
            ep = self._widget_to_em(pos.x(), pos.y())
            ci, ni = self._drag_node
            node = self._glyph.contours[ci].nodes[ni]
            dx = ep.x() - node.x
            dy = ep.y() - node.y
            # Move control points with node
            if node.cp_in:
                node.cp_in = (node.cp_in[0] + dx, node.cp_in[1] + dy)
            if node.cp_out:
                node.cp_out = (node.cp_out[0] + dx, node.cp_out[1] + dy)
            node.x = ep.x()
            node.y = ep.y()
            self.changed.emit()
            self.update()
            return

        # Drag handle
        if self._drag_handle is not None:
            ep = self._widget_to_em(pos.x(), pos.y())
            ci, ni, side = self._drag_handle
            node = self._glyph.contours[ci].nodes[ni]
            new_pt = (ep.x(), ep.y())
            if side == "out":
                node.cp_out = new_pt
                if node.smooth and node.cp_in is not None:
                    dx = node.x - ep.x()
                    dy = node.y - ep.y()
                    ln = math.hypot(dx, dy)
                    if ln > 0 and node.cp_in:
                        old_ln = math.hypot(node.cp_in[0] - node.x, node.cp_in[1] - node.y)
                        node.cp_in = (node.x + dx / ln * old_ln, node.y + dy / ln * old_ln)
            else:
                node.cp_in = new_pt
                if node.smooth and node.cp_out is not None:
                    dx = node.x - ep.x()
                    dy = node.y - ep.y()
                    ln = math.hypot(dx, dy)
                    if ln > 0 and node.cp_out:
                        old_ln = math.hypot(node.cp_out[0] - node.x, node.cp_out[1] - node.y)
                        node.cp_out = (node.x + dx / ln * old_ln, node.y + dy / ln * old_ln)
            self.changed.emit()
            self.update()
            return

        self.update()

    def mouseReleaseEvent(self, event):
        if event.button() == Qt.MouseButton.MiddleButton:
            self._pan_start = None
            self._pan_origin = None
        self._drag_node = None
        self._drag_handle = None
        self._drag_start = None

    def wheelEvent(self, event):
        factor = ZOOM_STEP if event.angleDelta().y() > 0 else 1 / ZOOM_STEP
        self._zoom = max(0.1, min(10.0, self._zoom * factor))
        self.update()

    def keyPressEvent(self, event):
        if event.key() == Qt.Key.Key_Escape:
            self.finish_path()
        elif event.modifiers() & Qt.KeyboardModifier.ControlModifier and event.key() == Qt.Key.Key_Z:
            self.undo()
        elif event.key() == Qt.Key.Key_Delete or event.key() == Qt.Key.Key_Backspace:
            self._delete_selected()

    # ── Tool implementations ──────────────────────────────────────────────────

    def _pen_press(self, event, pos, ep):
        if not self._glyph:
            return
        self._push_undo()

        # Check if clicking the first node of active contour -> close path
        if self._active_contour and len(self._active_contour.nodes) > 1:
            first = self._active_contour.nodes[0]
            wp = self._em_to_widget(first.x, first.y)
            if (pos - wp).manhattanLength() < HIT_RADIUS:
                self._active_contour.closed = True
                self._active_contour = None
                self.changed.emit()
                self.update()
                return

        # Start new contour if needed
        if self._active_contour is None:
            self._active_contour = GlyphContour()
            self._glyph.contours.append(self._active_contour)

        node = PathNode(x=ep.x(), y=ep.y())
        self._active_contour.nodes.append(node)
        self._drag_node = (len(self._glyph.contours) - 1,
                           len(self._active_contour.nodes) - 1)
        self.changed.emit()
        self.update()

    def _select_press(self, event, pos, ep):
        # Try handle first, then node
        hit_h = self._hit_handle(pos)
        if hit_h:
            self._push_undo()
            self._drag_handle = hit_h
            return
        hit_n = self._hit_node(pos)
        if hit_n:
            self._push_undo()
            self._drag_node = hit_n
            return
        # Right click on node -> toggle smooth
        if event.button() == Qt.MouseButton.RightButton:
            hit_n = self._hit_node(pos)
            if hit_n:
                ci, ni = hit_n
                node = self._glyph.contours[ci].nodes[ni]
                node.smooth = not node.smooth

    def _delete_press(self, pos):
        hit = self._hit_node(pos)
        if hit and self._glyph:
            self._push_undo()
            ci, ni = hit
            contour = self._glyph.contours[ci]
            contour.nodes.pop(ni)
            if not contour.nodes:
                self._glyph.contours.pop(ci)
                if self._active_contour is contour:
                    self._active_contour = None
            self.changed.emit()
            self.update()

    def _delete_selected(self):
        """Delete key pressed — remove last node of active contour."""
        if self._active_contour and self._active_contour.nodes:
            self._push_undo()
            self._active_contour.nodes.pop()
            if not self._active_contour.nodes:
                if self._active_contour in self._glyph.contours:
                    self._glyph.contours.remove(self._active_contour)
                self._active_contour = None
            self.changed.emit()
            self.update()

    # ── Paint ─────────────────────────────────────────────────────────────────

    def paintEvent(self, event):
        p = QPainter(self)
        p.setRenderHint(QPainter.RenderHint.Antialiasing)
        w, h = self.width(), self.height()

        # Background
        p.fillRect(self.rect(), QColor("#1e1e1e"))

        self._draw_grid(p, w, h)
        self._draw_guides(p, w, h)
        if self._glyph:
            self._draw_glyph(p)
        self._draw_tool_cursor(p)
        p.end()

    def _draw_grid(self, p, w, h):
        p.setPen(QPen(QColor(60, 60, 60), 1))
        # Draw vertical lines every 100 em units
        step = 100
        for x in range(0, self.units_per_em + step, step):
            a = self._em_to_widget(x, self.ascender)
            b = self._em_to_widget(x, self.descender)
            p.drawLine(a, b)
        for y in range(self.descender - 200, self.ascender + 200, step):
            a = self._em_to_widget(0, y)
            b = self._em_to_widget(self.units_per_em, y)
            p.drawLine(a, b)

    def _draw_guides(self, p, w, h):
        guides = [
            (self.ascender,  QColor("#cc4444"), "Ascender"),
            (self.cap_height, QColor("#cc8844"), "Cap"),
            (self.x_height,  QColor("#ccaa33"), "x-height"),
            (0,              QColor("#4488cc"), "Baseline"),
            (self.descender, QColor("#6666cc"), "Descender"),
        ]
        for y_em, color, label in guides:
            a = self._em_to_widget(0, y_em)
            b = self._em_to_widget(self.units_per_em, y_em)
            p.setPen(QPen(color, 1, Qt.PenStyle.DashLine))
            p.drawLine(a, b)
            p.setPen(color)
            font = QFont("Segoe UI", 8)
            p.setFont(font)
            p.drawText(int(a.x()) + 4, int(a.y()) - 3, label)

        # Left & right sidebearing walls
        p.setPen(QPen(QColor("#888888"), 1, Qt.PenStyle.DotLine))
        tl = self._em_to_widget(0, self.ascender)
        bl = self._em_to_widget(0, self.descender)
        tr = self._em_to_widget(self.units_per_em, self.ascender)
        br = self._em_to_widget(self.units_per_em, self.descender)
        p.drawLine(tl, bl)
        p.drawLine(tr, br)

    def _draw_glyph(self, p):
        for ci, contour in enumerate(self._glyph.contours):
            if not contour.nodes:
                continue
            is_active = (contour is self._active_contour)

            # Build QPainterPath
            path = QPainterPath()
            n = contour.nodes
            wp0 = self._em_to_widget(n[0].x, n[0].y)
            path.moveTo(wp0)

            for i in range(1, len(n)):
                prev = n[i - 1]
                curr = n[i]
                p_out = prev.cp_out
                c_in  = curr.cp_in
                wp    = self._em_to_widget(curr.x, curr.y)
                if p_out is None and c_in is None:
                    path.lineTo(wp)
                else:
                    ox, oy = p_out if p_out else (prev.x, prev.y)
                    ix, iy = c_in  if c_in  else (curr.x, curr.y)
                    wpo = self._em_to_widget(ox, oy)
                    wpi = self._em_to_widget(ix, iy)
                    path.cubicTo(wpo, wpi, wp)

            if contour.closed and len(n) > 1:
                last = n[-1]
                first = n[0]
                p_out = last.cp_out
                c_in  = first.cp_in
                wp    = self._em_to_widget(first.x, first.y)
                if p_out is None and c_in is None:
                    path.closeSubpath()
                else:
                    ox, oy = p_out if p_out else (last.x, last.y)
                    ix, iy = c_in  if c_in  else (first.x, first.y)
                    wpo = self._em_to_widget(ox, oy)
                    wpi = self._em_to_widget(ix, iy)
                    path.cubicTo(wpo, wpi, wp)
                    path.closeSubpath()

            # Fill (semi-transparent)
            if contour.closed:
                p.setBrush(QBrush(QColor(200, 80, 80, 60)))
            else:
                p.setBrush(Qt.BrushStyle.NoBrush)

            stroke_color = QColor("#ff6666") if is_active else QColor("#dd4444")
            p.setPen(QPen(stroke_color, 2))
            p.drawPath(path)

            # Draw nodes and handles
            for ni, node in enumerate(n):
                wp = self._em_to_widget(node.x, node.y)

                # Handle lines and dots
                for cp, other_side in [(node.cp_in, "in"), (node.cp_out, "out")]:
                    if cp is not None:
                        wcp = self._em_to_widget(cp[0], cp[1])
                        p.setPen(QPen(QColor("#888888"), 1))
                        p.drawLine(wp, wcp)
                        p.setPen(Qt.PenStyle.NoPen)
                        p.setBrush(QBrush(QColor("#aaaaff")))
                        p.drawEllipse(wcp, HANDLE_RADIUS, HANDLE_RADIUS)

                # Node itself
                is_first = (ni == 0)
                is_corner = (node.cp_in is None and node.cp_out is None)
                p.setPen(QPen(QColor("white"), 1.5))
                if is_corner:
                    # Square = corner
                    p.setBrush(QBrush(QColor("#cc2222")))
                    r = NODE_RADIUS
                    p.drawRect(QRectF(wp.x() - r, wp.y() - r, r * 2, r * 2))
                else:
                    # Circle = smooth/curve
                    p.setBrush(QBrush(QColor("#cc6622")))
                    p.drawEllipse(wp, NODE_RADIUS, NODE_RADIUS)

                if is_first and not contour.closed:
                    # Highlight first node
                    p.setPen(QPen(QColor("white"), 2))
                    p.setBrush(Qt.BrushStyle.NoBrush)
                    p.drawEllipse(wp, NODE_RADIUS + 3, NODE_RADIUS + 3)

    def _draw_tool_cursor(self, p):
        if self._hover_pos and self.tool == self.TOOL_PEN and self._active_contour:
            ep = self._widget_to_em(self._hover_pos.x(), self._hover_pos.y())
            wp = self._em_to_widget(ep.x(), ep.y())
            last = self._active_contour.nodes[-1] if self._active_contour.nodes else None
            if last:
                wp_last = self._em_to_widget(last.x, last.y)
                p.setPen(QPen(QColor(200, 200, 200, 80), 1, Qt.PenStyle.DashLine))
                p.drawLine(wp_last, wp)
            p.setPen(QPen(QColor("white"), 1))
            p.setBrush(QBrush(QColor("#cc2222")))
            r = NODE_RADIUS - 1
            p.drawRect(QRectF(wp.x() - r, wp.y() - r, r * 2, r * 2))
