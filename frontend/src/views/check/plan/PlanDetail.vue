<template>
  <div class="plan-detail">
    <!-- 返回 -->
    <div class="back-bar">
      <el-button :icon="ArrowLeft" @click="router.push({ name: 'PlanList' })">返回列表</el-button>
      <span class="refresh-time">上次刷新: {{ lastRefresh }}</span>
      <el-button :icon="Refresh" circle @click="refreshAll" />
    </div>

    <!-- 计划信息 -->
    <el-card shadow="never" class="info-card">
      <el-descriptions :column="3" border>
        <el-descriptions-item label="计划单号">{{ plan.plan_no }}</el-descriptions-item>
        <el-descriptions-item label="计划名称">{{ plan.plan_name }}</el-descriptions-item>
        <el-descriptions-item label="仓库">{{ plan.warehouse_name }}</el-descriptions-item>
        <el-descriptions-item label="盘点类型">{{ planTypeMap[plan.plan_type] }}</el-descriptions-item>
        <el-descriptions-item label="盘点模式">{{ checkModeMap[plan.check_mode] }}</el-descriptions-item>
        <el-descriptions-item label="计划日期">{{ plan.plan_date }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="planStatusMap[plan.status]?.type">{{ planStatusMap[plan.status]?.label }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="创建人">{{ plan.created_by_name }}</el-descriptions-item>
        <el-descriptions-item label="备注">{{ plan.remark || '-' }}</el-descriptions-item>
      </el-descriptions>

      <!-- 操作按钮 -->
      <div class="action-bar">
        <el-button
          v-if="plan.status === 'DRAFT'"
          v-permission="'check:publish'"
          type="success"
          @click="handlePublish"
        >
          发布计划
        </el-button>
        <el-button
          v-if="plan.status === 'IN_PROGRESS'"
          v-permission="'check:plan:create'"
          type="primary"
          @click="handleComplete"
        >
          完成计划
        </el-button>
        <el-button
          v-if="plan.status === 'COMPLETED'"
          type="warning"
          @click="router.push({ name: 'DiffSummary', params: { planId } })"
        >
          差异处理
        </el-button>
      </div>
    </el-card>

    <!-- 进度概览 -->
    <el-card shadow="never" class="progress-card">
      <template #header>进度概览</template>
      <el-row :gutter="16">
        <el-col :span="6">
          <el-statistic title="总任务数" :value="progress.total" />
        </el-col>
        <el-col :span="6">
          <el-statistic title="已复核" :value="progress.reviewed">
            <template #suffix>/ {{ progress.total }}</template>
          </el-statistic>
        </el-col>
        <el-col :span="6">
          <el-statistic title="完成率">
            <template #default>
              <el-progress type="circle" :percentage="progress.progress_pct" :width="80" />
            </template>
          </el-statistic>
        </el-col>
        <el-col :span="6">
          <el-statistic title="差异项" :value="progress.diff_count" class="diff-stat" />
        </el-col>
      </el-row>
    </el-card>

    <!-- 子任务列表 -->
    <el-card shadow="never">
      <template #header>子任务列表</template>
      <el-table v-loading="taskLoading" :data="tasks" border stripe>
        <el-table-column prop="task_no" label="任务编号" width="160" />
        <el-table-column prop="location_code" label="货位编码" width="140" />
        <el-table-column prop="zone" label="区域" width="100" />
        <el-table-column prop="assigned_name" label="执行人" width="100">
          <template #default="{ row }">{{ row.assigned_name || '-' }}</template>
        </el-table-column>
        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="taskStatusMap[row.status as TaskStatus]?.type" size="small">
              {{ taskStatusMap[row.status as TaskStatus]?.label }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="盘点进度" width="120" align="center">
          <template #default="{ row }">
            {{ row.scanned_count }} / {{ row.item_count }}
          </template>
        </el-table-column>
        <el-table-column prop="diff_count" label="差异数" width="80" align="center">
          <template #default="{ row }">
            <span :class="{ 'text-danger': row.diff_count > 0 }">{{ row.diff_count }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="claimed_at" label="领取时间" width="170" />
        <el-table-column prop="submitted_at" label="提交时间" width="170" />
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="{ row }">
            <el-button
              v-if="row.status === 'SUBMITTED'"
              link
              type="primary"
              @click="router.push({ name: 'TaskReview', params: { taskId: row.task_id } })"
            >
              复核
            </el-button>
            <el-button
              v-else-if="row.status === 'REVIEWED'"
              link
              type="info"
              @click="router.push({ name: 'TaskReview', params: { taskId: row.task_id } })"
            >
              查看
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowLeft, Refresh } from '@element-plus/icons-vue'
import {
  getCheckPlan, getPlanProgress, getCheckTasks,
  publishPlan, completePlan,
} from '@/api/check'
import { planStatusMap, planTypeMap, checkModeMap, taskStatusMap } from '@/utils/dict'
import type { CheckPlan, CheckTask, PlanProgress, TaskStatus } from '@/types/check'
import dayjs from 'dayjs'

const props = defineProps<{ planId: string }>()
const router = useRouter()

const plan = reactive<CheckPlan>({} as CheckPlan)
const progress = reactive<PlanProgress>({ total: 0, pending: 0, claimed: 0, counting: 0, submitted: 0, reviewed: 0, progress_pct: 0, diff_count: 0 })
const tasks = ref<CheckTask[]>([])
const taskLoading = ref(false)
const lastRefresh = ref('')
let timer: ReturnType<typeof setInterval> | null = null

async function loadPlan() {
  const res = await getCheckPlan(Number(props.planId))
  Object.assign(plan, res)
}

async function loadProgress() {
  const res = await getPlanProgress(Number(props.planId))
  Object.assign(progress, res)
}

async function loadTasks() {
  taskLoading.value = true
  try {
    const res = await getCheckTasks({ planId: Number(props.planId), pageSize: 200 })
    tasks.value = res.list
  } finally {
    taskLoading.value = false
  }
}

function refreshAll() {
  loadProgress()
  loadTasks()
  lastRefresh.value = dayjs().format('HH:mm:ss')
}

async function handlePublish() {
  await ElMessageBox.confirm('确认发布该盘点计划？发布后将自动生成子任务', '确认发布')
  const res = await publishPlan(Number(props.planId))
  ElMessage.success(`已发布，生成${res.taskCount}个子任务`)
  loadPlan()
  refreshAll()
}

async function handleComplete() {
  await ElMessageBox.confirm('确认完成该盘点计划？', '确认完成')
  await completePlan(Number(props.planId))
  ElMessage.success('计划已完成')
  loadPlan()
}

onMounted(() => {
  loadPlan()
  refreshAll()
  // 每30秒自动刷新
  timer = setInterval(refreshAll, 30000)
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
})
</script>

<style scoped lang="scss">
.plan-detail {
  padding: 16px;

  .back-bar {
    display: flex;
    align-items: center;
    gap: 12px;
    margin-bottom: 16px;

    .refresh-time {
      margin-left: auto;
      color: #909399;
      font-size: 13px;
    }
  }

  .info-card {
    margin-bottom: 16px;

    .action-bar {
      margin-top: 16px;
      display: flex;
      gap: 8px;
    }
  }

  .progress-card {
    margin-bottom: 16px;
  }

  .diff-stat :deep(.el-statistic__number) {
    color: #f56c6c;
  }

  .text-danger {
    color: #f56c6c;
    font-weight: bold;
  }
}
</style>
