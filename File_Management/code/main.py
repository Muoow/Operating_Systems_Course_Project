from table import FileTable
from PyQt5.QtWidgets import (QApplication, QMainWindow, QWidget, QVBoxLayout, 
                             QLineEdit, QPushButton, QTableView, QAbstractItemView,
                             QSplitter, QHBoxLayout, QToolBar, QTreeView, QStyle, QStyleFactory, QHeaderView, QAction, QMessageBox)
from PyQt5.QtCore import Qt, QModelIndex, QAbstractItemModel, QAbstractTableModel, pyqtSlot
from PyQt5.QtGui import QFont
import time

class FileSystemModel(QAbstractItemModel):
    def __init__(self, root_node, parent=None):
        super().__init__(parent)
        self.root_node = root_node

    def index(self, row, column, parent=QModelIndex()):
        if not self.hasIndex(row, column, parent):
            return QModelIndex()
        
        parent_node = self.root_node if not parent.isValid() else parent.internalPointer()
        child_node = list(parent_node.children.values())[row]
        return self.createIndex(row, column, child_node)

    def parent(self, index):
        if not index.isValid():
            return QModelIndex()
            
        child_node = index.internalPointer()
        parent_node = child_node.parent
        
        if parent_node == self.root_node:
            return QModelIndex()
        
        row = list(parent_node.parent.children.values()).index(parent_node)
        return self.createIndex(row, 0, parent_node)

    def rowCount(self, parent=QModelIndex()):
        parent_node = self.root_node if not parent.isValid() else parent.internalPointer()
        return len(parent_node.children) if parent_node.is_dir else 0

    def columnCount(self, parent=QModelIndex()):
        return 1

    def data(self, index, role=Qt.DisplayRole):
        node = index.internalPointer()
        if role == Qt.DisplayRole:
            return node.name
        elif role == Qt.UserRole:
            return node
        elif role == Qt.DecorationRole:
            style = QApplication.style()
            if node.is_dir:
                return style.standardIcon(QStyle.SP_DirIcon)
            else:
                return style.standardIcon(QStyle.SP_FileIcon)
        elif role == Qt.FontRole:
            font = QFont()
            font.setFamily("Microsoft YaHei")
            font.setPointSize(7)
            return font
        
        return None

    def headerData(self, section, orientation, role):
        if orientation == Qt.Horizontal and role == Qt.DisplayRole and section == 0:
            return "根目录"
        elif orientation == Qt.Horizontal and role == Qt.DecorationRole and section == 0:
            return QApplication.style().standardIcon(QStyle.SP_DirIcon)
        elif role == Qt.FontRole:
            font = QFont()
            font.setFamily("Microsoft YaHei")
            font.setPointSize(7)
            font.setBold(True) 
            return font
        
        return None

class FileListModel(QAbstractTableModel):
    def __init__(self, file_nodes, parent=None):
        super().__init__(parent)
        self.file_nodes = file_nodes
        self.headers = ["文件/文件夹名", "类型", "大小", "修改时间"]
    
    def rowCount(self, parent=QModelIndex()):
        return len(self.file_nodes)

    def columnCount(self, parent=QModelIndex()):
        return len(self.headers)

    def data(self, index, role=Qt.DisplayRole):
        if not index.isValid():
            return None
            
        node = self.file_nodes[index.row()]
        
        if role == Qt.DisplayRole:
            if index.column() == 0: 
                return node.name
            elif index.column() == 1: 
                return "文件夹" if node.is_dir else "文本文件"
            elif index.column() == 2: 
                return "" if node.is_dir else f"{node.size} B"
            elif index.column() == 3:
                return time.strftime("%Y-%m-%d %H:%M:%S", time.localtime(node.modification_time))
        elif role == Qt.UserRole:
            return node
        elif role == Qt.DecorationRole and index.column() == 0:
            if node.is_dir:
                return QApplication.style().standardIcon(QStyle.SP_DirIcon)
            else:
                return QApplication.style().standardIcon(QStyle.SP_FileIcon)
        elif role == Qt.FontRole:
            font = QFont()
            font.setFamily("Microsoft YaHei")
            font.setPointSize(7)
            return font
            
        return None

    def headerData(self, section, orientation, role=Qt.DisplayRole):
        if orientation == Qt.Horizontal and role == Qt.DisplayRole:
            return self.headers[section]
        elif role == Qt.FontRole:
            font = QFont()
            font.setFamily("Microsoft YaHei")
            font.setPointSize(7)
            font.setBold(True) 
            return font
        return None

class FileManager(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("文件管理器 | 张翔 2352985")
        self.setGeometry(100, 100, 800, 600)
        self.table = FileTable()
        self.tree_model = None
        self.list_model = None
        self.init_ui()
    
    def init_ui(self):
        font = QFont("Microsoft YaHei", 8)
        
        main_widget = QWidget()
        main_layout = QVBoxLayout()
        main_layout.setContentsMargins(5, 5, 5, 5)
        main_widget.setLayout(main_layout)
        self.setCentralWidget(main_widget)

        fuc_toolbar = QToolBar()
        fuc_toolbar.setMovable(False)

        new_dir_action = QAction("新建文件夹", self)
        new_dir_action.triggered.connect(lambda: self.table.create_dir("新建文件夹"))
        new_dir_action.setFont(font)
        fuc_toolbar.addAction(new_dir_action)
        
        new_file_action = QAction("新建文件", self)
        new_file_action.triggered.connect(lambda: self.table.create_file("新建文件"))
        new_file_action.setFont(font)
        fuc_toolbar.addAction(new_file_action)
        
        delete_action = QAction("删除", self)
        delete_action.triggered.connect(self.delete_selected)
        delete_action.setFont(font)
        fuc_toolbar.addAction(delete_action)
        
        main_layout.addWidget(fuc_toolbar)
        
        state_layout = QHBoxLayout()
        
        back_button = QPushButton("<")
        back_button.setFixedWidth(20)  
        back_button.setFixedHeight(20) 
        back_button.clicked.connect(self.table.path_backward)
        
        forward_button = QPushButton(">")
        forward_button.setFixedWidth(20)  
        forward_button.setFixedHeight(20)  
        forward_button.clicked.connect(self.table.path_forward)
        
        self.path_display = QLineEdit()
        self.path_display.setReadOnly(True)
        self.path_display.setFont(font)
        
        state_layout.addWidget(back_button)
        state_layout.addWidget(forward_button)
        state_layout.addWidget(self.path_display)
        
        main_layout.addLayout(state_layout)
        
        splitter = QSplitter(Qt.Horizontal)
        main_layout.addWidget(splitter)
        
        model = FileSystemModel(self.table.root)
        self.tree_view = QTreeView()
        self.tree_view.expandAll()
        self.tree_view.setModel(model)
        self.tree_view.setStyle(QStyleFactory.create("windows"))
        self.tree_view.setFont(font)
        splitter.addWidget(self.tree_view)
        
        self.table_view = QTableView()
        self.table_view.setSelectionBehavior(QAbstractItemView.SelectRows)
        self.table_view.setColumnWidth(0, 150) 
        self.table_view.setColumnWidth(1, 50) 
        self.table_view.setColumnWidth(2, 50) 
        self.table_view.setColumnWidth(3, 100)  
        header = self.table_view.horizontalHeader()
        header.setSectionResizeMode(QHeaderView.Interactive) 
        header.setStretchLastSection(True) 
        header.setFont(font)
        self.table_view.setFont(font)
        splitter.addWidget(self.table_view)
        
        splitter.setSizes([250, 550])
        
        self.update_path(self.table.get_current_dir())
        self.table.path_changed.connect(self.update_path)
        self.table.file_changed.connect(self.update_views)
    
        self.table_view.doubleClicked.connect(self.on_table_view_double_clicked)
    
    def update_path(self, path):
        self.path_display.setText(path)
        file_nodes = self.table.get_current_files()
        self.table_view.setModel(FileListModel(file_nodes))
    
    def update_views(self):
        model = FileSystemModel(self.table.root)
        self.tree_view.setModel(model)
        self.tree_view.expandAll()
        file_nodes = self.table.get_current_files()
        self.table_view.setModel(FileListModel(file_nodes))

    @pyqtSlot(QModelIndex)
    def on_table_view_double_clicked(self, index): 
        model = self.table_view.model()
        node = model.data(index, Qt.UserRole)
        if node and node.is_dir:
            self.table.path_forward(node.name)

    def delete_selected(self):
        if self.table_view.selectionModel().hasSelection():
            indexes = self.table_view.selectionModel().selectedRows()
            if indexes:
                index = indexes[0]
                model = self.table_view.model()
                node = model.data(index, Qt.UserRole)
                if node:
                    reply = QMessageBox.question(
                        self, 
                        "确认删除", 
                        f"确定要删除 {node.name} 吗？",
                        QMessageBox.Yes | QMessageBox.No, 
                        QMessageBox.No
                    )
                    if reply == QMessageBox.Yes:
                        if node.is_dir:
                            self.table.delete_dir(node.name)
                        else:
                            self.table.delete_file(node.name)
                        return

if __name__ == "__main__":
    app = QApplication([])
    window = FileManager()
    window.show()
    app.exec_()