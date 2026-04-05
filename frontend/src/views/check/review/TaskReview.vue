<template>
  <div class="task-review">
    <div class="back-bar">
      <el-button :icon="ArrowLeft" @click="router.back()">返回</el-button>
      <h3>盘点复核 — {{ taskInfo?.task_no }}</h3>
    </div>

    <el-card shadow="never">
      <el-table v-loading="loading" :data="details" border stripe>
        <el-table-column prop="material_code" label="物料编码" width="140" />
        <el-table-column prop="material_name" label="物料名称" min-width="180" show-overflow-tooltip />
        <el-table-column prop="unit" label="单位" width="70" align="center" />
        <el-table-column prop="book_qty" label="账面数量" width="100" align="right" />
        <el-table-column prop="actual_qty" label="实盘数量" width="100" align="right">
          <template #default="{ row }">
            {{ row.actual_qty ?? '-' }}
          </template>
        </el-table-column>
        <el-table-column label="差异数量" width="100" align="right">
          <template #default="{ row }">
            <span :class="{ 'diff-highlight': row.diff_qty !== 0 }">
              {{ row.diff_qty }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="差异原因" width="200">
          <template #default="{ row }">
            <el-select
              v-if="row.diff_qty !== 0 && isEditable"
              v-model="row.diff_reason"
              placeholder="请选择"
              clearable
              style="width: 100%"
            >
              <el-option
                v-for="opt in diffReasonOptions"
                :key="opt.value"
                :label="opt.label"
                :value="opt.value"
              />
            </el-select>
            <span v-else>{{ row.diff_reason || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="scan_time" label="扫码时间" width="170" />
        <el-table-column prop="operator_name" label="操作人" width="100" />
        <el-table-column label="二次确认" width="80" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.is_rechecked" type="success" size="small">是</el-tag>
            <el-tag v-else type="info" size="small">否</el-tag>
          </template>
        </el-table-column>
      </el-table>

      <!-- 汇总统计 -->
      <div class="summary-bar">
        <span>共 {{ details.length }} 项物料</span>
        <span>差异项: <strong class="diff-highlight">{{ diffItems.length }}</strong></span>
        <span>盘盈: <strong style="color:#67c23a">{{ surplusCount }}</strong></span>
        <span>盘亏: <strong class="diff-highlight">{{ lossCount }}</strong></span>
      </div>

      <!-- 操作按钮 -->
      <div v-if="isEditable" class="action-bar">
        <el-button
          v-permission="'check:review'"
          type="success"
          size="large"
          @click="handleApprove"
        >
          复核通过
        </el-button>
        <el-button
          v-permission="'check:review'"
          type="danger"
          size="large"
          @click="handleReject"
        >
          退回重盘
        </el-button>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowLeft } from '@element-plus/icons-vue'
import { getTaskDetails, reviewTask, getCheckTasks } from '@/api/check'
import { diffReasonOptions } from '@/utils/dict'
import type { CheckDetail, CheckTask } from '@/types/check'

const props = defineProps<{ taskId: string }>()
const router = useRouter()

const loading = ref(false)
const details = ref<CheckDetail[]>([])
const taskInfo = ref<CheckTask | null>(null)

const isEditable = computed(() => taskInfo.value?.status === 'SUBMITTED')

const diffItems = computed(() => details.value.filter(d => d.diff_qty !== 0))
const surplusCount = computed(() => diffItems.value.filter(d => d.diff_qty > 0).length)
const lossCount = computed(() => diffItems.value.filter(d => d.diff_qty < 0).length)

async function loadDetails() {
  loading.value = true
  try {
    details.value = await getTaskDetails(Number(props.taskId))
  } finally {
    loading.value = false
  }
}

async function handleApprove() {
  // 校验: 所有差异项必须有差异原因
  const missing = diffItems.value.filter(d => !d.diff_reason)
  if (missing.length > 0) {
    ElMessage.warning('差异项必须填写差异原因')
    return
  }

  await ElMessageBox.confirm('确认复核通过？', '确认')
  await reviewTask(Number(props.taskId), {
    approved: true,
    details: diffItems.value.map(d => ({
      detail_id: d.detail_id,
      diff_reason: d.diff_reason!,
    })),
  })
  ElMessage.success('复核通过')
  router.back()
}

async function handleReject() {
  await ElMessageBox.confirm('确认退回重盘？实盘数据将被清空', '确认退回', { type: 'warning' })
  await reviewTask(Number(props.taskId), { approved: false })
  ElMessage.success('已退回重盘，已通知工人')
  router.back()
}

onMounted(() => {
  loadDetails()
  // 加载任务基本信息
  getCheckTasks({ planId: undefined, pageSize: 1 }).catch(() => {})
})
</script>

<style scoped lang="scss">
.task-review {
  padding: 16px;

  .back-bar {
    display: flex;
    align-items: center;
    gap: 12px;
    margin-bottom: 16px;

    h3 { margin: 0; }
  }

  .diff-highlight {
    color: #f56c6c;
    font-weight: bold;
  }

  .summary-bar {
    display: flex;
    gap: 24px;
    padding: 16px 0;
    font-size: 14px;
    border-top: 1px solid #ebeef5;
    margin-top: 16px;
  }

  .action-bar {
    display: flex;
    justify-content: center;
    gap: 16px;
    padding-top: 16px;
  }
}
</style>
