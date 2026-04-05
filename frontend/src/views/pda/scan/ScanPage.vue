<template>
  <div class="scan-page">
    <!-- 顶部信息栏 -->
    <van-nav-bar
      :title="`扫码盘点`"
      left-arrow
      @click-left="confirmBack"
    >
      <template #right>
        <van-tag :type="online ? 'success' : 'danger'">{{ online ? '在线' : '离线' }}</van-tag>
      </template>
    </van-nav-bar>

    <!-- 货位信息 + 进度 -->
    <div class="status-bar">
      <div class="location-info">
        <van-icon name="location-o" />
        <span>{{ locationInfo || '请扫描货位条码' }}</span>
      </div>
      <div class="progress-info">
        已盘 <strong>{{ scannedCount }}</strong> 项 / 共 <strong>{{ materials.length }}</strong> 项
      </div>
    </div>

    <!-- 第一步: 扫货位 (未扫描时显示) -->
    <div v-if="step === 'SCAN_LOCATION'" class="scan-prompt">
      <van-empty image="search" description="请扫描货位条码开始盘点">
        <van-button type="primary" size="small" @click="simulateScanLocation">
          模拟扫码(开发用)
        </van-button>
      </van-empty>
    </div>

    <!-- 第二步: 物料列表 -->
    <div v-else class="material-list">
      <van-cell-group inset>
        <van-cell
          v-for="item in materials"
          :key="item.detail_id"
          :class="{ scanned: item.actual_qty != null, unscanned: item.actual_qty == null }"
          clickable
          @click="selectMaterial(item)"
        >
          <template #title>
            <div class="material-title">
              <span class="code">{{ item.material_code }}</span>
              <van-tag v-if="item.actual_qty != null" type="success">✓</van-tag>
              <van-tag v-else type="warning">未盘</van-tag>
            </div>
          </template>
          <template #label>
            <div class="material-info">
              <span>{{ item.material_name }}</span>
              <span class="unit">{{ item.unit }}</span>
            </div>
            <div class="qty-row">
              <span v-if="!isBlindMode">账面: {{ item.book_qty }}</span>
              <span v-if="item.actual_qty != null">实盘: <strong>{{ item.actual_qty }}</strong></span>
            </div>
          </template>
        </van-cell>
      </van-cell-group>

      <!-- 提交按钮 -->
      <div class="submit-bar">
        <van-button type="primary" block size="large" @click="handleSubmit">
          提交盘点结果
        </van-button>
      </div>
    </div>

    <!-- 数量输入弹窗 -->
    <van-dialog
      v-model:show="qtyDialogVisible"
      :title="`录入: ${currentMaterial?.material_code || ''}`"
      show-cancel-button
      @confirm="confirmQty"
    >
      <div class="qty-dialog-body">
        <p class="material-name">{{ currentMaterial?.material_name }}</p>
        <p v-if="!isBlindMode" class="book-qty">账面数量: {{ currentMaterial?.book_qty }}</p>
        <van-field
          ref="qtyInputRef"
          v-model="inputQty"
          type="digit"
          placeholder="请输入实盘数量"
          input-align="center"
          class="qty-input"
          :rules="[{ required: true, message: '请输入实盘数量' }]"
        />
      </div>
    </van-dialog>

    <!-- 差异预警弹窗 -->
    <van-overlay :show="diffWarningVisible" class="diff-overlay" @click="diffWarningVisible = false">
      <div class="diff-warning-popup" @click.stop>
        <van-icon name="warning-o" size="64" color="#e6a23c" />
        <h2>⚠️ 差异预警</h2>
        <p>物料: {{ diffWarningData.materialCode }}</p>
        <p>差异数量: <strong :class="diffWarningData.diffQty > 0 ? 'surplus' : 'loss'">
          {{ diffWarningData.diffQty > 0 ? '+' : '' }}{{ diffWarningData.diffQty }}
        </strong></p>
        <p class="hint">请确认实盘数量是否准确</p>
        <div class="diff-actions">
          <van-button type="warning" size="small" @click="confirmDiff">确认无误</van-button>
          <van-button size="small" @click="reScan">重新录入</van-button>
        </div>
      </div>
    </van-overlay>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { showSuccessToast, showConfirmDialog, showFailToast } from 'vant'
import { scanLocation, scanMaterial, submitTask } from '@/api/check'
import type { CheckDetail } from '@/types/check'

const props = defineProps<{ taskId: string }>()
const router = useRouter()

const step = ref<'SCAN_LOCATION' | 'SCAN_MATERIAL'>('SCAN_LOCATION')
const locationInfo = ref('')
const materials = ref<CheckDetail[]>([])
const isBlindMode = ref(false)
const online = ref(navigator.onLine)

// 数量输入
const qtyDialogVisible = ref(false)
const currentMaterial = ref<CheckDetail | null>(null)
const inputQty = ref('')
const qtyInputRef = ref()

// 差异预警
const diffWarningVisible = ref(false)
const diffWarningData = ref({ materialCode: '', diffQty: 0 })

// 扫码队列
const scanQueue = ref<Array<() => Promise<void>>>([])
let processing = false

const scannedCount = computed(() => materials.value.filter(m => m.actual_qty != null).length)

// PDA硬件扫码监听
function onScanResult(e: any) {
  const barcode = e.detail?.barcode || e.data
  if (!barcode) return

  if (step.value === 'SCAN_LOCATION') {
    doScanLocation(barcode)
  } else {
    enqueueScan(barcode)
  }
}

async function doScanLocation(barcode: string) {
  try {
    const res = await scanLocation({ taskId: Number(props.taskId), barcode })
    materials.value = res
    locationInfo.value = `货位: ${barcode}`
    step.value = 'SCAN_MATERIAL'
    // 检测盲盘模式(book_qty为null表示盲盘)
    isBlindMode.value = res.length > 0 && res[0].book_qty == null
  } catch {
    showFailToast('货位条码无效')
  }
}

function enqueueScan(barcode: string) {
  scanQueue.value.push(async () => {
    const matched = materials.value.find(m => m.barcode === barcode)
    if (!matched) {
      showFailToast('该物料不在本次盘点清单中')
      return
    }
    currentMaterial.value = matched
    inputQty.value = isBlindMode.value ? '' : String(matched.book_qty)
    qtyDialogVisible.value = true
  })
  processQueue()
}

async function processQueue() {
  if (processing) return
  processing = true
  while (scanQueue.value.length > 0) {
    const task = scanQueue.value.shift()!
    await task()
    // 等待弹窗关闭
    await new Promise<void>(resolve => {
      const check = () => {
        if (!qtyDialogVisible.value) resolve()
        else setTimeout(check, 200)
      }
      check()
    })
  }
  processing = false
}

function selectMaterial(item: CheckDetail) {
  currentMaterial.value = item
  inputQty.value = item.actual_qty != null ? String(item.actual_qty) : (isBlindMode.value ? '' : '')
  qtyDialogVisible.value = true
}

async function confirmQty() {
  const qty = Number(inputQty.value)
  if (isNaN(qty) || qty < 0) {
    showFailToast('实盘数量必须≥0')
    return
  }
  if (!currentMaterial.value) return

  try {
    const res = await scanMaterial(Number(props.taskId), {
      barcode: currentMaterial.value.barcode,
      actual_qty: qty,
    })

    // 更新本地数据
    const idx = materials.value.findIndex(m => m.detail_id === currentMaterial.value!.detail_id)
    if (idx >= 0) {
      materials.value[idx].actual_qty = qty
    }

    showSuccessToast('录入成功')

    // 差异预警
    if (res.hasDiff) {
      diffWarningData.value = {
        materialCode: currentMaterial.value.material_code,
        diffQty: res.diff_qty,
      }
      diffWarningVisible.value = true
      // 震动
      if (navigator.vibrate) {
        navigator.vibrate([200, 100, 200])
      }
    }
  } catch {
    showFailToast('录入失败')
  }

  qtyDialogVisible.value = false
}

function confirmDiff() {
  diffWarningVisible.value = false
}

function reScan() {
  diffWarningVisible.value = false
  if (currentMaterial.value) {
    inputQty.value = ''
    qtyDialogVisible.value = true
  }
}

async function handleSubmit() {
  const unscannedCount = materials.value.filter(m => m.actual_qty == null).length
  if (unscannedCount > 0) {
    try {
      await showConfirmDialog({
        title: '提示',
        message: `还有${unscannedCount}项物料未盘点，确认提交吗？`,
      })
    } catch {
      return
    }
  }

  await submitTask(Number(props.taskId))
  showSuccessToast('盘点结果提交成功')
  router.push({ name: 'PdaLobby' })
}

function confirmBack() {
  if (scannedCount.value > 0) {
    showConfirmDialog({
      title: '提示',
      message: '离开将丢失未提交的盘点数据，确认离开？',
    }).then(() => router.back()).catch(() => {})
  } else {
    router.back()
  }
}

// 模拟扫码(开发用)
function simulateScanLocation() {
  doScanLocation('LOC-A01-01')
}

// 网络状态监听
function updateOnline() { online.value = navigator.onLine }

onMounted(() => {
  window.addEventListener('scan', onScanResult)
  window.addEventListener('online', updateOnline)
  window.addEventListener('offline', updateOnline)
})

onUnmounted(() => {
  window.removeEventListener('scan', onScanResult)
  window.removeEventListener('online', updateOnline)
  window.removeEventListener('offline', updateOnline)
})
</script>

<style scoped lang="scss">
.scan-page {
  min-height: 100vh;
  background: #f7f8fa;

  .status-bar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 10px 16px;
    background: #fff;
    border-bottom: 1px solid #ebeef5;
    font-size: 14px;

    .location-info {
      display: flex;
      align-items: center;
      gap: 4px;
      color: #303133;
      font-weight: 500;
    }

    .progress-info {
      color: #606266;
      strong { color: #409eff; }
    }
  }

  .scan-prompt {
    padding-top: 60px;
  }

  .material-list {
    padding-bottom: 80px;

    .scanned { background: #f0f9eb; }
    .unscanned { background: #fff; }

    .material-title {
      display: flex;
      align-items: center;
      gap: 8px;
      .code { font-weight: 600; }
    }

    .material-info {
      display: flex;
      justify-content: space-between;
      .unit { color: #909399; }
    }

    .qty-row {
      display: flex;
      gap: 16px;
      margin-top: 4px;
      font-size: 13px;
      color: #606266;
      strong { color: #409eff; }
    }
  }

  .submit-bar {
    position: fixed;
    bottom: 0;
    left: 0;
    right: 0;
    padding: 12px 16px;
    background: #fff;
    box-shadow: 0 -2px 8px rgba(0, 0, 0, 0.08);
  }

  // 数量输入弹窗
  .qty-dialog-body {
    padding: 16px;
    text-align: center;

    .material-name { color: #303133; font-size: 15px; margin: 0 0 8px; }
    .book-qty { color: #909399; font-size: 13px; margin: 0 0 16px; }

    .qty-input {
      :deep(input) {
        font-size: 48px !important;
        font-weight: bold;
        height: 80px;
        text-align: center;
      }
    }
  }

  // 差异预警遮罩
  .diff-overlay {
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(230, 162, 60, 0.85);
  }

  .diff-warning-popup {
    background: #fff;
    border-radius: 16px;
    padding: 32px 24px;
    text-align: center;
    width: 85%;
    max-width: 360px;

    h2 { margin: 12px 0 16px; color: #e6a23c; }
    p { margin: 4px 0; font-size: 15px; color: #303133; }
    .surplus { color: #67c23a; font-size: 24px; }
    .loss { color: #f56c6c; font-size: 24px; }
    .hint { color: #909399; font-size: 13px; margin-top: 12px; }

    .diff-actions {
      display: flex;
      justify-content: center;
      gap: 12px;
      margin-top: 20px;
    }
  }
}
</style>
