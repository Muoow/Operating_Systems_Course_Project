
<template>
  <h1 style='text-align: center'>Memory Management | 内存管理</h1>
  <h2 style='text-align: center'>2352985 张翔</h2>
  <div class="container">
    <div class="section">
      <h2>动态分区分配方式模拟</h2>
      <h3>Dynamic Partition Allocation Simulation</h3>
      <el-button-group style="margin-bottom: 30px">
        <el-button  type='primary' plain size='large'
          @click="runMemoryManagement('firstFit')">
          首次适应算法
        </el-button>
        <el-button  type='primary' plain size='large'
          @click="runMemoryManagement('bestFit')">
          最佳适应算法
        </el-button>
        <el-button  type='primary' plain size='large'
          @click="resetMemoryManager">
          重置内存空间
        </el-button>
      </el-button-group>
      <div style="display: flex; justify-content: center; gap: 50px; width: 100%">
        <el-table :data="taskSequence" stripe border style="width:480px">
          <el-table-column prop="id" label="任务序号" width="120" align="center" />
          <el-table-column prop="task" label="任务对象" width="120" align="center" />
          <el-table-column prop="description" label="任务内容" width="120" align="center" />
          <el-table-column prop="size" label="内存大小" width="120" align="center" />
        </el-table>
        <el-table :data="memoryBlocks" stripe border style="width:480px">
          <el-table-column label="分区序号" width="120" align="center">
            <template #default="{ $index }">
              {{ $index + 1 }}
            </template>
          </el-table-column>
          <el-table-column prop='start' label='起始地址' width='120' align='center'/>
          <el-table-column prop='size' label='内存大小(K)' width='120' align='center'/>
          <el-table-column label='状态' width='120' align='center'>
            <template #default="{row}">
              {{ row.allocated ? `作业 ${row.processId}` : '空闲中' }}
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>

    <div class="section">
      <h2>请求调页存储管理方式模拟</h2>
      <h3>Demand Paging Storage Management Mode Simulation</h3>
      <h3>缺页次数: {{ pageFaults }}</h3>
      <h3>缺页率: {{ pageFaultRate }}</h3>
      <el-button-group style="margin-bottom: 30px">
        <el-button  type='primary' plain size='large'
          @click="runPartitionAllocationSimulation('fifo')">
          FIFO算法
        </el-button>
        <el-button  type='primary' plain size='large'
          @click="runPartitionAllocationSimulation('lru')">
          LRU算法
        </el-button>
        <el-button  type='primary' plain size='large'
          @click="restartPartitionAllocationSimulation()">
          重置内存空间
        </el-button>
      </el-button-group>
      <div style="display: flex; justify-content: center; width: 100%">
        <el-table :data="memoryState" stripe border style="width:900px">
          <el-table-column label="执行次序" width="100" align="center">
            <template #default="{ $index }">
              {{ $index + 1 }}
            </template>
          </el-table-column>
          <el-table-column prop="instructionId" label="指令序号" width="100" align="center"/>
          <el-table-column label="内存块 1" width="100" align="center">
            <template #default="{row}">
              {{ row.pages[0]>=0 ? `${row.pages[0] * 10}-${row.pages[0] * 10 + 9}`:'空闲中' }}
            </template>
          </el-table-column>
          <el-table-column label="内存块 2" width="100" align="center">
            <template #default="{row}">
              {{ row.pages[1]>=0 ? `${row.pages[1] * 10}-${row.pages[1] * 10 + 9}`:'空闲中' }}
            </template>
          </el-table-column>
          <el-table-column label="内存块 3" width="100" align="center">
            <template #default="{row}">
              {{ row.pages[2]>=0 ? `${row.pages[2] * 10}-${row.pages[2] * 10 + 9}`:'空闲中' }}
            </template>
          </el-table-column>
          <el-table-column label="内存块 4" width="100" align="center">
            <template #default="{row}">
              {{ row.pages[3]>=0 ? `${row.pages[3] * 10}-${row.pages[3] * 10 + 9}`:'空闲中' }}
            </template>
          </el-table-column>
          <el-table-column label="是否缺页" width="100" align="center">
            <template #default="{row}">
              {{ row.isPageFault === true ? "是":"否"}}
            </template>
          </el-table-column>
          <el-table-column label="放入页" width="100" align="center">
            <template #default="{row}">
              {{ row.insertedBlock !== null ? `${row.insertedBlock}` : '--' }}
            </template>
          </el-table-column>
          <el-table-column label="换出页" width="100" align="center">
            <template #default="{row}">
              {{ row.removedPage !== null ? `${row.removedPage}` : '--' }}
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>
  </div>
</template>

<script setup>
import {MemoryManager}from './classes.js'
import {ref} from 'vue'
import {ElMessage} from 'element-plus'

// 动态分区分配方式模拟

const memoryManager = ref(new MemoryManager(640))
const memoryBlocks = ref(memoryManager.value.getMemoryBlocks())
const currentTask = ref(0)
const taskSequence = ref([
  {id: 1, task: '作业 1', description: '申请', size: '120K'},
  {id: 2, task: '作业 2', description: '申请', size: '80K'},
  {id: 3, task: '作业 3', description: '申请', size: '150K'},
  {id: 4, task: '作业 2', description: '释放', size: '80K'},
  {id: 5, task: '作业 4', description: '申请', size: '90K'},
  {id: 6, task: '作业 1', description: '释放', size: '120K'},
  {id: 7, task: '作业 5', description: '申请', size: '200K'},
  {id: 8, task: '作业 3', description: '释放', size: '150K'},
  {id: 9, task: '作业 6', description: '申请', size: '70K'},
  {id: 10, task: '作业 7', description: '申请', size: '100K'},
  {id: 11, task: '作业 4', description: '释放', size: '90K'},
  {id: 12, task: '作业 8', description: '申请', size: '180K'}
])

const firstFitSequence = ref([
  () => memoryManager.value.firstFit(1,120),
  () => memoryManager.value.firstFit(2,80),
  () => memoryManager.value.firstFit(3,150),
  () => memoryManager.value.releaseMemory(2),
  () => memoryManager.value.firstFit(4,150),
  () => memoryManager.value.releaseMemory(1),
  () => memoryManager.value.firstFit(5,200),
  () => memoryManager.value.releaseMemory(3),
  () => memoryManager.value.firstFit(6,70),
  () => memoryManager.value.firstFit(7,100),
  () => memoryManager.value.releaseMemory(4),
  () => memoryManager.value.firstFit(8,180)
])
const bestFitSequence = ref([
  () => memoryManager.value.bestFit(1,120),
  () => memoryManager.value.bestFit(2,80),
  () => memoryManager.value.bestFit(3,150),
  () => memoryManager.value.releaseMemory(2),
  () => memoryManager.value.bestFit(4,150),
  () => memoryManager.value.releaseMemory(1),
  () => memoryManager.value.bestFit(5,200),
  () => memoryManager.value.releaseMemory(3),
  () => memoryManager.value.bestFit(6,70),
  () => memoryManager.value.bestFit(7,100),
  () => memoryManager.value.releaseMemory(4),
  () => memoryManager.value.bestFit(8,180)
])

const runMemoryManagement = (algorithmType) => {
  let sequence = []
  let algorithm = ""
  if (algorithmType === 'firstFit') {
    sequence = firstFitSequence.value
    algorithm = "首次适应算法"
  } else if (algorithmType === 'bestFit') {
    sequence = bestFitSequence.value
    algorithm = "最佳适应算法"
  }
  if (currentTask.value < sequence.length) {
    sequence[currentTask.value]()
    ElMessage({
      showClose: true,
      message: `${taskSequence.value[currentTask.value].description} ${taskSequence.value[currentTask.value].task} 完成`,
      type: 'success',
      duration: 2000
    })
    memoryBlocks.value = memoryManager.value.getMemoryBlocks()
    currentTask.value++
  } else {
    ElMessage({
      showClose: true,
      message: algorithm + '成功完成',
      type: 'success',
      duration: 2000
    })
  }
}

function resetMemoryManager(){
  memoryManager.value = new MemoryManager(640)
  memoryBlocks.value = memoryManager.value.getMemoryBlocks()
  currentTask.value = 0
  ElMessage({
    showClose: true,
    message: '内存空间分配已重置',
    type: 'warning',
    duration: 2000
  })
}

import {PageReplacementSimulator}from './classes.js'

// 请求调页存储管理方式模拟

const replacementSimulator = new PageReplacementSimulator(320,10,4)
const memoryState = ref([]);
const pageFaults = ref(0)
const pageFaultRate = ref(0)

function runPartitionAllocationSimulation(algorithmType) {
  let result;
  let algorithm = "";
  if(algorithmType === 'fifo') {
    result = replacementSimulator.simulateFIFO();
    algorithm="FIFO算法";
  } else if(algorithmType === 'lru') {
    result = replacementSimulator.simulateLRU();
    algorithm="LRU算法";
  }
  memoryState.value = result.logs
  pageFaults.value = result.pageFaults
  pageFaultRate.value = result.pageFaultRate
  ElMessage({
    showClose: true,
    message: algorithm + '成功完成',
    type: 'success',
    duration: 2000
  })
}

function restartPartitionAllocationSimulation() {
  memoryState.value = []
  pageFaults.value = 0
  pageFaultRate.value = 0
  replacementSimulator.resetSimulation()
  ElMessage({
    showClose: true,
    message: '内存空间分配已重置',
    type: 'warning',
    duration: 2000
  })
}
</script>

<style>
.container {
  display: flex;
  flex-direction: column;
  width: 100%;
  gap: 20px;
}
.section{
  display: flex;
  flex-direction: column;
  align-items: center;
}
</style>
