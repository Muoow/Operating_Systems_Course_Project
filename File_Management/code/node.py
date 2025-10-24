import time

class FileNode():
    def __init__(self, name, is_dir, parent=None):
        self.name = name
        self.is_dir = is_dir
        self.parent = parent
        self.modification_time = time.time()
        
        if is_dir:
            self.children = {}  
        else:
            self.size = 0

    def get_path(self):
        if self.parent is None:
            return "> 根目录/"
        parts = []
        current = self
        while current.parent is not None:
            parts.append(current.name)
            current = current.parent
        parts.reverse()
        return "> 根目录/" + "/".join(parts)
    
    def add_child(self, child_node):
        self.children[child_node.name] = child_node
        child_node.parent = self
        self.modification_time = time.time()
    
    def remove_child(self, name):
        if name in self.children:
            del self.children[name]
            self.modification_time = time.time()
        
    def get_child(self, name):
        return self.children.get(name)
        
    def find_child(self, name):
        return name in self.children