"""
ui_file_dialog.py
Custom-themed file browser dialog for fModLoader.
Styled to match the dark red/maroon fModLoader aesthetic.
Drop-in replacement for QFileDialog.getOpenFileName / getSaveFileName.
"""

import os

from PyQt6.QtWidgets import (
    QDialog, QVBoxLayout, QHBoxLayout, QLabel, QLineEdit,
    QPushButton, QListView, QComboBox, QFrame, QWidget,
    QSizePolicy, QMessageBox, QInputDialog, QToolButton, QAbstractItemView,
    QListWidget, QListWidgetItem, QMenu
)
from PyQt6.QtCore import Qt, QDir, QSize, QModelIndex, QSortFilterProxyModel
from PyQt6.QtGui import (
    QPainter, QColor, QLinearGradient, QBrush, QPen, QFont,
    QIcon, QPixmap, QPainterPath
)
try:
    from PyQt6.QtWidgets import QFileSystemModel
except ImportError:
    from PyQt6.QtGui import QFileSystemModel  # type: ignore


# ─────────────────────────────── Styled Widgets ──────────────────────────────

class _GradientHeader(QWidget):
    """Draws the crimson gradient toolbar header."""
    def paintEvent(self, event):
        p = QPainter(self)
        p.setRenderHint(QPainter.RenderHint.Antialiasing)
        grad = QLinearGradient(0, 0, self.width(), 0)
        grad.setColorAt(0.0,  QColor("#6b0000"))
        grad.setColorAt(0.35, QColor("#cc1a1a"))
        grad.setColorAt(0.65, QColor("#cc1a1a"))
        grad.setColorAt(1.0,  QColor("#6b0000"))
        p.fillRect(self.rect(), QBrush(grad))
        p.end()


class _GradientFooter(QWidget):
    """Draws the crimson gradient footer."""
    def paintEvent(self, event):
        p = QPainter(self)
        p.setRenderHint(QPainter.RenderHint.Antialiasing)
        grad = QLinearGradient(0, 0, 0, self.height())
        grad.setColorAt(0.0, QColor("#a01010"))
        grad.setColorAt(1.0, QColor("#6b0000"))
        p.fillRect(self.rect(), QBrush(grad))
        p.end()


def _toolbar_btn(icon_text: str, tooltip: str) -> QToolButton:
    """Create a flat icon-style toolbar button."""
    btn = QToolButton()
    btn.setText(icon_text)
    btn.setToolTip(tooltip)
    btn.setFixedSize(30, 30)
    btn_font = QFont("Segoe UI Symbol")
    btn_font.setPixelSize(13)
    btn.setFont(btn_font)
    btn.setStyleSheet("""
        QToolButton {
            color: white;
            background: transparent;
            border: none;
            border-radius: 4px;
        }
        QToolButton:hover {
            background: rgba(255,255,255,30);
        }
        QToolButton:pressed {
            background: rgba(0,0,0,30);
        }
    """)
    return btn


def _footer_combo(items: list[str]) -> QComboBox:
    cb = QComboBox()
    cb.addItems(items)
    cb.setStyleSheet("""
        QComboBox {
            background: rgba(255,255,255,220);
            color: #1a0000;
            border: 1px solid #cc1a1a;
            border-radius: 3px;
            padding: 2px 6px;
            font-size: 11px;
            min-width: 120px;
        }
        QComboBox::drop-down {
            border: none;
        }
        QComboBox::down-arrow {
            image: none;
            width: 10px;
        }
        QComboBox QAbstractItemView {
            background: white;
            color: #1a0000;
            selection-background-color: #cc1a1a;
            selection-color: white;
        }
    """)
    return cb


def _action_btn(label: str, primary=False) -> QPushButton:
    btn = QPushButton(label)
    btn.setFixedSize(72, 24)
    if primary:
        style = """
            QPushButton {
                background: qlineargradient(x1:0,y1:0,x2:0,y2:1,
                    stop:0 #e63333, stop:1 #8b0000);
                color: white;
                border: 1px solid #6b0000;
                border-radius: 4px;
                font-weight: bold;
                font-size: 12px;
            }
            QPushButton:hover { background: #dd2222; }
            QPushButton:pressed { background: #7a0000; }
        """
    else:
        style = """
            QPushButton {
                background: qlineargradient(x1:0,y1:0,x2:0,y2:1,
                    stop:0 rgba(255,255,255,220), stop:1 rgba(220,200,200,220));
                color: #3a0000;
                border: 1px solid rgba(180,80,80,200);
                border-radius: 4px;
                font-size: 12px;
            }
            QPushButton:hover { background: rgba(255,240,240,240); }
            QPushButton:pressed { background: rgba(200,160,160,240); }
        """
    btn.setStyleSheet(style)
    return btn


# ── Internal Icon Generators ─────────────────────────────────────────────────

def _arrow_path(dir: int) -> QPainterPath:
    path = QPainterPath()
    if dir < 0: # back
        path.moveTo(10, 3); path.lineTo(4, 8); path.lineTo(10, 13)
        path.moveTo(4, 8); path.lineTo(14, 8)
    else:
        path.moveTo(6, 3); path.lineTo(12, 8); path.lineTo(6, 13)
        path.moveTo(12, 8); path.lineTo(2, 8)
    return path

def _star_path() -> QPainterPath:
    path = QPainterPath()
    path.moveTo(8, 1); path.lineTo(10, 6); path.lineTo(15, 6)
    path.lineTo(11, 9); path.lineTo(13, 14); path.lineTo(8, 11)
    path.lineTo(3, 14); path.lineTo(5, 9); path.lineTo(1, 6)
    path.lineTo(6, 6); path.closeSubpath()
    return path

def _gen_menu_icon(itype: str) -> QIcon:
    pm = QPixmap(16, 16)
    pm.fill(Qt.GlobalColor.transparent)
    p = QPainter(pm)
    p.setRenderHint(QPainter.RenderHint.Antialiasing)
    
    if itype in ("back", "forward"):
        p.setPen(QPen(QColor("#1a0000"), 2, cap=Qt.PenCapStyle.RoundCap, join=Qt.PenJoinStyle.RoundJoin))
        p.drawPath(_arrow_path(-1 if itype == "back" else 1))
    elif itype == "star":
        p.setPen(Qt.PenStyle.NoPen)
        p.setBrush(QColor("#f5c518"))
        p.drawPath(_star_path())
        p.setPen(QPen(QColor("#cc1a1a"), 1))
        p.setBrush(Qt.BrushStyle.NoBrush)
        p.drawPath(_star_path())
    elif itype == "remove":
        p.setPen(Qt.PenStyle.NoPen)
        p.setBrush(QColor("#f5c518"))
        p.drawPath(_star_path())
        # do not enter sign overlay
        p.setPen(Qt.PenStyle.NoPen)
        p.setBrush(QColor("#cc1a1a"))
        p.drawEllipse(3, 3, 12, 12)
        p.setPen(QPen(QColor("white"), 2))
        p.drawLine(5, 9, 13, 9)
    p.end()
    return QIcon(pm)

class RemoveBookmarksDialog(QDialog):
    def __init__(self, bookmarks, parent=None):
        super().__init__(parent)
        self.setWindowTitle("Remove bookmarks")
        self.setFixedSize(280, 240)
        self.bookmarks = list(bookmarks)
        ml = QVBoxLayout(self)
        
        lbl = QLabel("Remove selected bookmarks")
        lbl.setAlignment(Qt.AlignmentFlag.AlignCenter)
        lbl.setStyleSheet("color: #1a0000; font-size: 13px;")
        ml.addWidget(lbl)
        
        self.list_widget = QListWidget()
        self.list_widget.setSelectionMode(QAbstractItemView.SelectionMode.ExtendedSelection)
        self.list_widget.addItems(self.bookmarks)
        self.list_widget.setStyleSheet("background: white; color: #1a0000; border: 1px solid #aaa;")
        ml.addWidget(self.list_widget)
        
        h1 = QHBoxLayout()
        btn_sel_all = QPushButton("Select All")
        btn_none = QPushButton("None")
        for b in (btn_sel_all, btn_none):
            b.setFixedWidth(80)
        btn_sel_all.clicked.connect(self.list_widget.selectAll)
        btn_none.clicked.connect(self.list_widget.clearSelection)
        # Match layout exactly
        h1.addStretch()
        h1.addWidget(btn_sel_all)
        h1.addWidget(btn_none)
        h1.addStretch()
        ml.addLayout(h1)
        
        h2 = QHBoxLayout()
        btn_remove = QPushButton("Remove")
        btn_cancel = QPushButton("Cancel")
        for b in (btn_remove, btn_cancel):
            b.setFixedWidth(80)
        btn_remove.clicked.connect(self._do_remove)
        btn_cancel.clicked.connect(self.reject)
        h2.addWidget(btn_remove)
        h2.addStretch()
        h2.addWidget(btn_cancel)
        ml.addLayout(h2)

    def _do_remove(self):
        selected_texts = {item.text() for item in self.list_widget.selectedItems()}
        self.bookmarks = [b for b in self.bookmarks if b not in selected_texts]
        self.accept()

class FMLSortProxy(QSortFilterProxyModel):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.dir_mode = "first"
        
    def lessThan(self, left: QModelIndex, right: QModelIndex) -> bool:
        src = self.sourceModel()
        l_dir = src.isDir(left)
        r_dir = src.isDir(right)
        
        if self.dir_mode == "first":
            if l_dir and not r_dir: return True
            if not l_dir and r_dir: return False
        elif self.dir_mode == "separate":
            if l_dir and not r_dir: return False
            if not l_dir and r_dir: return True
            
        return super().lessThan(left, right)


# ─────────────────────────────── Main Dialog ─────────────────────────────────

class FMLFileDialog(QDialog):
    """
    Fully custom file browser dialog matching fModLoader's dark red theme.
    Provides static helpers mirroring QFileDialog API:
        FMLFileDialog.getOpenFileName(parent, title, directory, filter)
        FMLFileDialog.getSaveFileName(parent, title, directory, filter)
    """

    def __init__(
        self,
        parent=None,
        title: str = "Open File",
        directory: str = "",
        filter_str: str = "All Files (*)",
        save_mode: bool = False,
        new_file_mode: bool = False,
    ):
        super().__init__(parent)
        self.setWindowTitle(title)
        self._new_file_mode = new_file_mode
        self.setMinimumSize(560, 460)
        self.resize(620, 500)
        self.setWindowFlags(Qt.WindowType.Dialog | Qt.WindowType.WindowCloseButtonHint)

        self._save_mode = save_mode
        self._title = title
        self.selected_path: str = ""

        # Parse filter choices
        self._filters = self._parse_filters(filter_str)
        self._bookmarks: list[str] = [
            str(QDir.homePath()),
            os.path.join(str(QDir.homePath()), "Desktop"),
            os.path.join(str(QDir.homePath()), "Documents"),
        ]

        # Start directory
        start = directory or str(QDir.homePath())
        self._current_dir = QDir(start if os.path.isdir(start) else str(QDir.homePath()))

        self._history: list[str] = [self._current_dir.absolutePath()]
        self._history_idx: int = 0

        self._show_hidden = False
        self._dir_sort_mode = "first"

        self._build_ui()
        self._navigate_to(self._current_dir.absolutePath())

    # ── Static helpers ────────────────────────────────────────────────────────

    @staticmethod
    def getOpenFileName(parent=None, caption="Open File", directory="", filter="All Files (*)", new_file_mode=False):
        dlg = FMLFileDialog(parent, caption, directory, filter, save_mode=False, new_file_mode=new_file_mode)
        if dlg.exec() == QDialog.DialogCode.Accepted:
            return dlg.selected_path, ""
        return "", ""

    @staticmethod
    def getSaveFileName(parent=None, caption="Save File", directory="", filter="All Files (*)"):
        dlg = FMLFileDialog(parent, caption, directory, filter, save_mode=True)
        if dlg.exec() == QDialog.DialogCode.Accepted:
            return dlg.selected_path, ""
        return "", ""

    # ── UI Builder ────────────────────────────────────────────────────────────

    def _build_ui(self):
        root = QVBoxLayout(self)
        root.setContentsMargins(0, 0, 0, 0)
        root.setSpacing(0)

        # ── Gradient Header / Toolbar ─────────────────────────────────────────
        header = _GradientHeader()
        header.setFixedHeight(42)
        hlay = QHBoxLayout(header)
        hlay.setContentsMargins(8, 4, 8, 4)
        hlay.setSpacing(4)

        self._btn_home = _toolbar_btn("⌂", "Go Home")
        self._btn_bkm  = _toolbar_btn("★", "Bookmarks / Favourites")
        self._btn_up   = _toolbar_btn("↑", "Go Up")
        self._btn_gear = _toolbar_btn("⚙", "Options")

        self._path_combo = QComboBox()
        self._path_combo.setEditable(True)
        self._path_combo.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Fixed)
        self._path_combo.setStyleSheet("""
            QComboBox {
                background: rgba(255,255,255,200);
                color: #1a0000;
                border: 1px solid rgba(255,255,255,120);
                border-radius: 4px;
                padding: 2px 8px;
                font-size: 12px;
                min-height: 22px;
            }
            QComboBox::drop-down {
                subcontrol-origin: padding;
                subcontrol-position: top right;
                width: 22px;
                border-left: 1px solid rgba(180,80,80,180);
                border-radius: 0 4px 4px 0;
                background: rgba(180,40,40,180);
            }
            QComboBox::down-arrow {
                color: white;
            }
            QComboBox QAbstractItemView {
                background: white;
                color: #1a0000;
                selection-background-color: #8b0000;
                selection-color: white;
                font-size: 12px;
            }
        """)

        hlay.addWidget(self._btn_home)
        hlay.addWidget(self._btn_bkm)
        hlay.addSpacing(4)
        hlay.addWidget(self._path_combo, 1)
        hlay.addSpacing(4)
        hlay.addWidget(self._btn_up)
        hlay.addWidget(self._btn_gear)
        root.addWidget(header)

        # ── Breadcrumb strip ──────────────────────────────────────────────────
        self._breadcrumb = QLabel()
        self._breadcrumb.setStyleSheet("""
            background: #f0e8e8;
            color: #5a0000;
            font-size: 11px;
            padding: 3px 12px;
            border-bottom: 1px solid #ddc0c0;
        """)
        root.addWidget(self._breadcrumb)

        # ── File list ─────────────────────────────────────────────────────────
        self._fs_model = QFileSystemModel()
        self._fs_model.setRootPath("")
        
        self._fs_proxy = FMLSortProxy(self)
        self._fs_proxy.setSourceModel(self._fs_model)
        self._fs_proxy.setDynamicSortFilter(True)
        self._apply_fs_filters()

        self._list = QListView()
        self._list.setModel(self._fs_proxy)
        self._fs_proxy.sort(0, Qt.SortOrder.AscendingOrder)
        
        self._list.setSelectionMode(QAbstractItemView.SelectionMode.SingleSelection)
        self._list.setStyleSheet("""
            QListView {
                background: #fdf6f6;
                border: none;
                font-size: 13px;
                font-family: 'Segoe UI', Arial;
                outline: none;
            }
            QListView::item {
                padding: 5px 10px;
                border-radius: 0px;
                color: #1a0000;
            }
            QListView::item:selected {
                background: qlineargradient(x1:0,y1:0,x2:1,y2:0,
                    stop:0 #5a0000, stop:0.6 #8b0000, stop:1 #7a0000);
                color: white;
            }
            QListView::item:hover:!selected {
                background: rgba(180,20,20,18);
            }
            QScrollBar:vertical {
                background: #f5eded;
                width: 10px;
                border-radius: 5px;
            }
            QScrollBar::handle:vertical {
                background: #cc1a1a;
                border-radius: 5px;
                min-height: 24px;
            }
            QScrollBar::add-line:vertical, QScrollBar::sub-line:vertical {
                background: none;
                height: 0;
            }
        """)
        root.addWidget(self._list, 1)

        # ── Filename input ────────────────────────────────────────────────────
        fname_bar = QWidget()
        fname_bar.setStyleSheet("background: #f0e8e8;")
        fbl = QHBoxLayout(fname_bar)
        fbl.setContentsMargins(12, 6, 12, 4)
        fbl.setSpacing(8)
        fbl.addWidget(QLabel("File name:"))
        self._filename_edit = QLineEdit()
        self._filename_edit.setPlaceholderText("Select or type a filename…")
        self._filename_edit.setStyleSheet("""
            QLineEdit {
                background: white;
                border: 1px solid #cc8080;
                border-radius: 3px;
                padding: 4px 8px;
                font-size: 12px;
                color: #1a0000;
            }
            QLineEdit:focus { border-color: #cc1a1a; }
        """)
        fbl.addWidget(self._filename_edit, 1)
        root.addWidget(fname_bar)

        # ── Gradient Footer ───────────────────────────────────────────────────
        footer = _GradientFooter()
        footer.setFixedHeight(88)
        fl = QVBoxLayout(footer)
        fl.setContentsMargins(12, 6, 12, 8)
        fl.setSpacing(5)

        # Filter row
        filter_row = QHBoxLayout()
        filter_lbl = QLabel("Filter")
        filter_lbl.setStyleSheet("color: white; font-weight: bold; font-size: 12px;")
        filter_lbl.setFixedWidth(60)
        self._filter_combo = _footer_combo([f[0] for f in self._filters])
        self._filter_combo.currentIndexChanged.connect(self._on_filter_changed)
        filter_row.addWidget(filter_lbl)
        filter_row.addWidget(self._filter_combo)
        filter_row.addStretch()
        fl.addLayout(filter_row)

        # Force rename row
        rename_row = QHBoxLayout()
        rename_lbl = QLabel("Force rename")
        rename_lbl.setStyleSheet("color: white; font-size: 11px;")
        rename_lbl.setFixedWidth(80)
        self._rename_combo = _footer_combo([
            "No Rename", "Adobe Glyph List", "AGL For New Fonts",
            "AGL Without AFII", "AGL With PUA", "Greek Small Caps",
            "TeX Names", "AMS Names"
        ])
        rename_row.addWidget(rename_lbl)
        rename_row.addWidget(self._rename_combo)
        rename_row.addStretch()
        fl.addLayout(rename_row)

        # Action buttons row
        btn_row = QHBoxLayout()
        btn_row.setSpacing(8)
        self._btn_ok     = _action_btn("OK", primary=True)
        self._btn_new    = _action_btn("New")
        self._btn_cancel = _action_btn("Cancel")
        btn_row.addStretch()
        btn_row.addWidget(self._btn_ok)
        btn_row.addWidget(self._btn_new)
        btn_row.addWidget(self._btn_cancel)
        fl.addLayout(btn_row)

        root.addWidget(footer)

        # ── Signals ───────────────────────────────────────────────────────────
        self._btn_home.clicked.connect(lambda: self._navigate_to(str(QDir.homePath())))
        self._btn_up.clicked.connect(self._go_up)
        self._btn_bkm.clicked.connect(self._show_bookmarks)
        self._btn_gear.clicked.connect(self._show_options)
        self._path_combo.lineEdit().returnPressed.connect(self._on_path_enter)
        self._list.activated.connect(self._on_item_activated)
        self._list.selectionModel().selectionChanged.connect(self._on_selection_changed)  # type: ignore
        self._btn_ok.clicked.connect(self._accept)
        self._btn_cancel.clicked.connect(self.reject)
        if getattr(self, "_new_file_mode", False):
            self._btn_new.setText("New File")
            self._btn_new.clicked.connect(self._new_file)
        else:
            self._btn_new.clicked.connect(self._new_folder)

    # ── Navigation ───────────────────────────────────────────────────────────

    def _navigate_to(self, path: str, add_history: bool = True):
        path = QDir(path).absolutePath()
        if not os.path.isdir(path):
            return
            
        if add_history:
            if not self._history or self._history[self._history_idx] != path:
                self._history = self._history[:self._history_idx + 1]
                self._history.append(path)
                self._history_idx = len(self._history) - 1
                
        idx = self._fs_model.setRootPath(path)
        self._list.setRootIndex(self._fs_proxy.mapFromSource(idx))
        self._current_dir = QDir(path)
        self._update_path_ui()
        # Apply filter
        self._on_filter_changed(self._filter_combo.currentIndex())

    def _go_back(self):
        if self._history_idx > 0:
            self._history_idx -= 1
            self._navigate_to(self._history[self._history_idx], add_history=False)

    def _go_forward(self):
        if self._history_idx < len(self._history) - 1:
            self._history_idx += 1
            self._navigate_to(self._history[self._history_idx], add_history=False)

    def _update_path_ui(self):
        path = self._current_dir.absolutePath()
        # Update combo
        self._path_combo.blockSignals(True)
        # Add ancestry items
        parts = []
        d = QDir(path)
        while True:
            parts.insert(0, d.absolutePath())
            if not d.cdUp():
                break
        self._path_combo.clear()
        self._path_combo.addItems(parts)
        self._path_combo.setCurrentText(path)
        self._path_combo.blockSignals(False)

        # Breadcrumb
        home = str(QDir.homePath())
        display = path.replace(home, "~") if path.startswith(home) else path
        parts_display = display.replace("\\", "/").split("/")
        breadcrumb = " › ".join(p for p in parts_display if p)
        self._breadcrumb.setText(breadcrumb)

    def _go_up(self):
        parent = QDir(self._current_dir.absolutePath())
        if parent.cdUp():
            self._navigate_to(parent.absolutePath())

    def _on_path_enter(self):
        path = self._path_combo.currentText().strip()
        if os.path.isdir(path):
            self._navigate_to(path)
        elif os.path.isfile(path):
            self._filename_edit.setText(path)

    def _apply_fs_filters(self):
        base = QDir.Filter.AllEntries | QDir.Filter.NoDotAndDotDot | QDir.Filter.AllDirs
        if self._show_hidden:
            base |= QDir.Filter.Hidden
        self._fs_model.setFilter(base)

    def _on_filter_changed(self, idx: int):
        if idx < 0 or idx >= len(self._filters):
            return
        _, patterns = self._filters[idx]
        self._fs_model.setNameFilters(patterns)
        self._fs_model.setNameFilterDisables(False)

    # ── File interaction ─────────────────────────────────────────────────────

    def _on_item_activated(self, p_idx: QModelIndex):
        idx = self._fs_proxy.mapToSource(p_idx)
        path = self._fs_model.filePath(idx)
        if os.path.isdir(path):
            self._navigate_to(path)
        else:
            self._filename_edit.setText(os.path.basename(path))
            self._accept()

    def _on_selection_changed(self, selected, deselected):
        indexes = self._list.selectionModel().selectedIndexes()
        if indexes:
            idx = self._fs_proxy.mapToSource(indexes[0])
            path = self._fs_model.filePath(idx)
            if os.path.isfile(path):
                self._filename_edit.setText(os.path.basename(path))

    def _new_folder(self):
        name, ok = QInputDialog.getText(self, "New Folder", "Folder name:")
        if ok and name.strip():
            new_path = os.path.join(self._current_dir.absolutePath(), name.strip())
            try:
                os.makedirs(new_path, exist_ok=True)
                self._navigate_to(self._current_dir.absolutePath())
            except OSError as e:
                QMessageBox.critical(self, "Error", str(e))

    def _new_file(self):
        self.selected_path = "__NEW_FILE__"
        self.accept()

    def _show_bookmarks(self):
        menu = QMenu(self)
        menu.setStyleSheet("""
            QMenu { background: white; border: 1px solid #cc1a1a; }
            QMenu::item { padding: 5px 24px 5px 24px; color: #1a0000; }
            QMenu::item:selected { background: #8b0000; color: white; }
            QMenu::separator { height: 1px; background: #ddd; margin: 4px 0; }
        """)
        
        a_back = menu.addAction(_gen_menu_icon("back"), "Back")
        a_back.setEnabled(self._history_idx > 0)
        a_fwd = menu.addAction(_gen_menu_icon("forward"), "Forward")
        a_fwd.setEnabled(self._history_idx < len(self._history) - 1)
        a_add = menu.addAction(_gen_menu_icon("star"), "Bookmark current directory")
        a_rem = menu.addAction(_gen_menu_icon("remove"), "Remove bookmarks")
        menu.addSeparator()
        
        for bk in self._bookmarks:
            action = menu.addAction(os.path.basename(bk) or bk)
            action.setData(("nav", bk))
            
        chosen = menu.exec(self._btn_bkm.mapToGlobal(self._btn_bkm.rect().bottomLeft()))
        if chosen:
            if chosen == a_back:
                self._go_back()
            elif chosen == a_fwd:
                self._go_forward()
            elif chosen == a_add:
                cp = self._current_dir.absolutePath()
                if cp not in self._bookmarks:
                    self._bookmarks.append(cp)
            elif chosen == a_rem:
                dlg = RemoveBookmarksDialog(self._bookmarks, self)
                if dlg.exec() == QDialog.DialogCode.Accepted:
                    self._bookmarks = dlg.bookmarks
            elif chosen.data() and chosen.data()[0] == "nav":
                self._navigate_to(chosen.data()[1])

    def _show_options(self):
        menu = QMenu(self)
        menu.setStyleSheet("""
            QMenu { background: white; border: 1px solid #cc1a1a; }
            QMenu::item { padding: 5px 24px; color: #1a0000; }
            QMenu::item:selected { background: #8b0000; color: white; }
            QMenu::indicator { padding-left: 5px; width: 13px; height: 13px; }
            QMenu::separator { height: 1px; background: #ddd; margin: 4px 0; }
        """)

        # Option: Show Hidden Files
        act_hidden = menu.addAction("Show Hidden Files")
        act_hidden.setCheckable(True)
        act_hidden.setChecked(self._show_hidden)
        
        menu.addSeparator()

        # Group: Dir Sorting
        act_mixed = menu.addAction("Directories Amid Files")
        act_mixed.setCheckable(True)
        act_first = menu.addAction("Directories First")
        act_first.setCheckable(True)
        act_sep = menu.addAction("Directories Separate")
        act_sep.setCheckable(True)

        if self._dir_sort_mode == "mixed": act_mixed.setChecked(True)
        elif self._dir_sort_mode == "first": act_first.setChecked(True)
        elif self._dir_sort_mode == "separate": act_sep.setChecked(True)
            
        menu.addSeparator()
        act_refresh = menu.addAction("Refresh File List")

        chosen = menu.exec(self._btn_gear.mapToGlobal(self._btn_gear.rect().bottomLeft()))
        if chosen:
            if chosen == act_hidden:
                self._show_hidden = act_hidden.isChecked()
                self._apply_fs_filters()
            elif chosen == act_mixed:
                self._dir_sort_mode = "mixed"
            elif chosen == act_first:
                self._dir_sort_mode = "first"
            elif chosen == act_sep:
                self._dir_sort_mode = "separate"
            elif chosen == act_refresh:
                self._navigate_to(self._current_dir.absolutePath(), add_history=False)
                
            if chosen in (act_mixed, act_first, act_sep):
                self._fs_proxy.dir_mode = self._dir_sort_mode
                self._fs_proxy.invalidate()
                self._fs_proxy.sort(0, Qt.SortOrder.AscendingOrder)

    def _accept(self):
        fname = self._filename_edit.text().strip()
        if not fname:
            QMessageBox.warning(self, "No file selected", "Please select or type a filename.")
            return

        if os.path.isabs(fname):
            path = fname
        else:
            path = os.path.join(self._current_dir.absolutePath(), fname)

        # Store selected glyph naming convention (applied during font processing)
        self._selected_rename_scheme = self._rename_combo.currentText()

        if not self._save_mode and not os.path.isfile(path):
            QMessageBox.warning(self, "Not found", f"No such file:\n{path}")
            return

        self.selected_path = path
        self.accept()

    # ── Parsing ──────────────────────────────────────────────────────────────

    @staticmethod
    def _parse_filters(filter_str: str) -> list[tuple[str, list[str]]]:
        """
        Parse "Images (*.png *.jpg);;All Files (*)" into
        [("Images (*.png *.jpg)", ["*.png","*.jpg"]), ("All Files (*)", ["*"])]
        """
        result = []
        for part in filter_str.split(";;"):
            part = part.strip()
            if not part:
                continue
            if "(" in part and ")" in part:
                label = part[:part.index("(")].strip()
                inner = part[part.index("(")+1:part.rindex(")")].strip()
                patterns = inner.split() if inner else ["*"]
                result.append((part, patterns))
            else:
                result.append((part, ["*"]))
        if not result:
            result = [("All Files (*)", ["*"])]
        return result

    def paintEvent(self, event):
        """Subtle maroon border around entire dialog."""
        p = QPainter(self)
        p.setRenderHint(QPainter.RenderHint.Antialiasing)
        pen = QPen(QColor("#8b0000"), 2)
        p.setPen(pen)
        p.setBrush(Qt.BrushStyle.NoBrush)
        p.drawRect(1, 1, self.width()-2, self.height()-2)
        p.end()
        super().paintEvent(event)


if __name__ == "__main__":
    import sys
    from PyQt6.QtWidgets import QApplication
    app = QApplication(sys.argv)
    p, _ = FMLFileDialog.getOpenFileName(None, "Test Open", "", "Python (*.py);;All Files (*)")
    print("Selected:", p)
