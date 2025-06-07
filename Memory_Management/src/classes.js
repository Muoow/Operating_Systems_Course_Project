// 内存块类
export class MemoryBlock {
  // 构造函数
  constructor(start, size, allocated = false) {
    this.start = start;
    this.size = size;
    this.allocated = allocated;
    this.processId = null;
  }
  // 内存块加载任务
  allocate(processId) {
    this.allocated = true;
    this.processId = processId;
  }
  // 内存块释放任务
  free() {
    this.allocated = false;
    this.processId = null;
  }
}
// 内存管理类
export class MemoryManager {
  // 构造函数
  constructor(totalSize) {
    this.totalSize = totalSize;
    this.blocks = [new MemoryBlock(0, totalSize)];
  }
  // 首次适应算法
  firstFit(processId, size){
    for (let i = 0; i < this.blocks.length; i++) {
      const block = this.blocks[i];
      if (!block.allocated && block.size >= size) {
        this.allocateMemory(processId, size, block, i);
        break;
      }
    }
  }
  // 最佳适应算法
  bestFit(processId, size){
    let bestIndex = -1;
    let bestSize = Infinity;
    for (let i = 0; i < this.blocks.length; i++) {
      const block = this.blocks[i];
      if (!block.allocated && block.size >= size && block.size < bestSize) {
        bestSize = block.size;
        bestIndex = i;
      }
    }
    if (bestIndex !== -1) {
      this.allocateMemory(processId, size, this.blocks[bestIndex]);
    }
  }
  // 加载内存块
  allocateMemory(processId, size, block){
    if (block.size === size) {
      block.allocate(processId);
    }else {
      const newBlock = new MemoryBlock(
        block.start + size,
        block.size - size
      );
      block.size = size;
      block.allocate(processId);
      this.blocks.push(newBlock);
      this.blocks.sort((a, b) => a.start - b.start);
    }
  }
  // 释放内存块
  releaseMemory(processId){
    const block = this.blocks.find(block => block.processId === processId);
    if (block) {
      block.free();
      this.mergeMemory();
    }
  }
  // 合并空闲的内存块
  mergeMemory(){
    for (let i = 0; i < this.blocks.length - 1; i++){
      const current = this.blocks[i];
      const next = this.blocks[i + 1];
      if (!current.allocated && !next.allocated) {
        current.size += next.size;
        this.blocks.splice(i + 1, 1);
        i--;
      }
    }
  }
  // 得到所有内存块
  getMemoryBlocks(){
    return this.blocks;
  }
}

export class MemoryPage {
  constructor(page, size) {
    this.page = page;
    this.size = size;
  }
  // 检查指令是否在当前页面中
  ifFound(index) {
    return this.page >= 0 && index >= this.page * this.size
      && index < this.page * this.size + this.size;
  }
  // 替换页面内容
  replacePage(newPage) {
    this.page = newPage;
  }
}

export class MemoryState {
  // 构造函数
  constructor() {
    this.logs = [];
  }
  // 增加记录
  addLog(instructionId, memory, isPageFault, insertedBlock, removedPage) {
    const logEntry = {
      instructionId: instructionId,                                   // 指令代号
      pages: memory.pages.map(page => page ? page.page : '--'),       // 内存块状态
      isPageFault: isPageFault,                                       // 是否缺页
      insertedBlock: insertedBlock,                                   // 放入的块(+1显示)
      removedPage: removedPage                                        // 换出的页
    };
    this.logs.push(logEntry);
  }
}

export class PageReplacementSimulator {
  // 构造函数
  constructor(totalInstructions, pageSize, memoryBlocks) {
    this.totalInstructions = totalInstructions;
    this.pageSize = pageSize;
    this.memoryBlocks = memoryBlocks;
    this.memoryPages = [];
    for (let i = 0; i < memoryBlocks; i++)
      this.memoryPages.push(new MemoryPage(-1, pageSize));
    this.instructions = this.generateInstructions();
  }
  // 生成所有指令
  generateInstructions(){
    const executionOrder = [];
    let count = 0;
    let total=this.totalInstructions
    let current = Math.floor(Math.random() * total)
    while (count < total) {
      executionOrder.push(current);
      count++;
      if (current + 1 < total  && count < total) {
        executionOrder.push(current + 1);
        count++;
        current = current + 1;
      }
      if (current > 0 && count < total) {
        const m1 = Math.floor(Math.random() * current);
        executionOrder.push(m1);
        count++;
        current = m1;
      }
      if (current + 1 < total && count < total) {
        executionOrder.push(current + 1);
        count++;
        current = current + 1;
      }
      if (current + 2 < total && count < total) {
        const m2 = current + 2 + Math.floor(Math.random() * (total - 1 - current - 1));
        executionOrder.push(m2);
        count++;
        current = m2;
      }
    }
    return executionOrder;
  }
  // 获取指令所在页
  getPageNumber(instruction) {
    return Math.floor(instruction / this.pageSize);
  }
  // FIFO算法
  simulateFIFO(){
    const fifoQueue = [];
    const memoryState = new MemoryState();
    let pageFaults= 0;
    this.instructions.forEach(inst => {
      let isPageFault=!this.memoryPages.some(p => p.ifFound(inst));
      let inserted = null;
      let removed = null;
      let pageIndex = this.getPageNumber(inst);
      if (isPageFault) {
        inserted = pageIndex;
        pageFaults++;
        let replaceIndex=-1;
        for (let i = 0; i < this.memoryBlocks; i++) {
          if (this.memoryPages[i].page === -1) {
            replaceIndex = i;
            break;
          }
        }
        if (replaceIndex === -1) {
          replaceIndex = fifoQueue.shift();
          removed = this.memoryPages[replaceIndex].page;
        }
        this.memoryPages[replaceIndex].replacePage(pageIndex);
        fifoQueue.push(replaceIndex);
      }
      memoryState.addLog(inst, { pages: [...this.memoryPages] },isPageFault , inserted, removed);
    });
    return {pageFaults: pageFaults, pageFaultRate: pageFaults / 320, logs:memoryState.logs};
  }
  // LRU算法
  simulateLRU() {
    const lastUsed = new Map();
    let time = 0;
    let pageFaults = 0;
    const memoryState = new MemoryState();
    this.instructions.forEach(inst => {
      time++;
      const pageNum = this.getPageNumber(inst);
      const pageIndex = this.memoryPages.findIndex(p => p.ifFound(inst));
      let inserted = null, removed = null;
      if (pageIndex >= 0) {
        lastUsed.set(pageIndex, time);
        memoryState.addLog(inst, { pages: [...this.memoryPages] }, false, inserted, removed);
      }else{
        pageFaults++;
        inserted = pageNum * this.pageSize;
        let replaceIndex=-1;
        for (let i = 0; i < this.memoryBlocks; i++) {
          if(this.memoryPages[i].page === -1){
            replaceIndex=i;
            break;
          }
        }
        if(replaceIndex === -1){
          let minTime = Infinity;
          for (const [key, value] of lastUsed) {
            if (value < minTime) {
              minTime = value;
              replaceIndex = key;
            }
          }
          removed = this.memoryPages[replaceIndex].page;
        }
        this.memoryPages[replaceIndex].replacePage(pageNum);
        lastUsed.set(replaceIndex, time);
        memoryState.addLog(inst, { pages: [...this.memoryPages] }, true, inserted, removed);
      }
    });
    return {pageFaults: pageFaults, pageFaultRate: pageFaults / 320, logs:memoryState.logs};
  }
  resetSimulation() {
    this.memoryPages = Array(this.memoryBlocks).fill(null).map(() => new MemoryPage(-1, 10));
    this.instructions = this.generateInstructions();
  }
}
