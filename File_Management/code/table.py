from node import FileNode
from PyQt5.QtCore import QObject, pyqtSignal

class FileTable(QObject):
    path_changed = pyqtSignal(str)
    file_changed = pyqtSignal()
    
    def __init__(self):
        super().__init__()
        self.root = FileNode("根目录", True)
        self.current_node = self.root 

    def get_current_dir(self):
        return self.current_node.get_path()
    
    def path_forward(self, name):
        if(self.current_node.find_child(name)):
            self.current_node = self.current_node.get_child(name)
            self.path_changed.emit(self.get_current_dir())
    
    def path_backward(self):
        if(self.current_node.parent != None):
            self.current_node = self.current_node.parent
            self.path_changed.emit(self.get_current_dir())
    
    def create_file(self, file_name="新建文件"):
        existing_names = [child.name for child in self.current_node.children.values() if not child.is_dir]
        new_name = file_name
        counter = 1
        while new_name in existing_names:
            new_name = f"{file_name}({counter})"
            counter += 1
        new_file = FileNode(new_name, False, self.current_node)
        self.current_node.add_child(new_file)
        self.file_changed.emit()
        
    def create_dir(self, dir_name="新建文件夹"):
        existing_names = [child.name for child in self.current_node.children.values() if child.is_dir]
        new_name = dir_name
        counter = 1
        while new_name in existing_names:
            new_name = f"{dir_name}({counter})"
            counter += 1
        new_dir = FileNode(new_name, True, self.current_node)
        self.current_node.add_child(new_dir)
        self.file_changed.emit()
        
    def delete_file(self, file_name):
        self.current_node.remove_child(file_name)
        self.file_changed.emit()
    
    def delete_dir(self, dir_name):
        child = self.current_node.get_child(dir_name)
        
        def delete_recursive(node):
            if node.is_dir:
                for name in list(node.children.keys()):
                    delete_recursive(node.get_child(name))
                    node.remove_child(name)
            else:
                parent = node.parent
                parent.remove_child(node.name)
        
        delete_recursive(child)
        self.current_node.remove_child(dir_name)
        self.file_changed.emit()
        
    def get_current_files(self):
        return list(self.current_node.children.values()) if self.current_node.is_dir else []