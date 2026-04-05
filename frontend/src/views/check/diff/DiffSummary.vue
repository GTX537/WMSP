<template>
  <div class="diff-summary">
    <div class="back-bar">
      <el-button :icon="ArrowLeft" @click="router.back()">返回</el-button>
      <h3>差异处理</h3>
    </div>

    <el-card shadow="never">
      <el-table v-loading="loading" :data="diffList" border stripe>
        <el-table-column prop="material_code" label="物料编码" width="140" />
        <el-table-column prop="material_name" label="物料名称" min-width="180" show-overflow-tooltip />
        <el-table-column prop="location_code" label="货位" width="120" />
        <el-table-column prop="unit" label="单位" width="70" align="center" />
        <el-table-column prop="book_qty" label="账面数量" width="100" align="right" />
        <el-table-column prop="actual_qty" label="实盘数量" width="100" align="right" />
        <el-table-column label="差异数量" width="100" align="right">
          <template #default="{ row }">
            <span :class="row.diff_qty > 0 ? 'text-surplus' : 'text-loss'">
              {{ row.diff_qty > 0 ? '+' : '' }}{{ row.diff_qty }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="单位成本" width="100" align="right">
          <template #default="{ row }">
            {{ row.unit_cost != null ? `¥${row.unit_cost.toFixed(2)}` : '-' }}
          </template>
        </el-table-column>
        <el-table-column label="差异金额" width="120" align="right">
          <template #default="{ row }">
            <span v-if="row.diff_amount != null" :class="row.diff_amount > 0 ? 'text-surplus' : 'text-loss'">
              ¥{{ row.diff_amount.toFixed(2) }}
            </span>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column prop="diff_reason" label="差异原因" width="160" />
      </el-table>

      <!-- 汇总 -->
      <div class="summary-bar">
        <el-row :gutter="32">
          <el-col :span="6">
            <div class="stat-item">
              <span class="stat-label">差异总项数</span>
              <span class="stat-value">{{ diffList.length }}</span>
            </div>
          </el-col>
          <el-col :span="6">
            <div class="stat-item">
              <span class="stat-label">盘盈项</span>
              <span class="stat-value text-surplus">{{ surplusList.length }}</span>
            </div>
          </el-col>
          <el-col :span="6">
            <div class="stat-item">
              <span class="stat-label">盘亏项</span>
              <span class="stat-value text-loss">{{ lossList.length }}</span>
            </div>
          </el-col>
          <el-col :span="6">
            <div class="stat-item">
              <span class="stat-label">差异总金额</span>
              <span class="stat-value" :class="totalAmount >= 0 ? 'text-surplus' : 'text-loss'">
                ¥{{ totalAmount.toFixed(2) }}
              </span>
            </div>
          </el-col>
        </el-row>
      </div>

      <!-- 调账按钮 -->
      <div class="action-bar">
        <el-button
          v-permission="'inventory:adjust'"
          type="warning"
          size="large"
          @click="handleAdjust"
        >
          一键调账
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
import { getDiffSummary, adjustInventory } from '@/api/check'
import type { DiffSummaryItem } from '@/types/check'

const props = defineProps<{ planId: string }>()
const router = useRouter()

const loading = ref(false)
const diffList = ref<DiffSummaryItem[]>([])

const surplusList = computed(() => diffList.value.filter(d => d.diff_qty > 0))
const lossList = computed(() => diffList.value.filter(d => d.diff_qty < 0))
const totalAmount = computed(() =>
  diffList.value.reduce((sum, d) => sum + (d.diff_amount || 0), 0)
)

async function loadDiff() {
  loading.value = true
  try {
    diffList.value = await getDiffSummary(Number(props.planId))
  } finally {
    loading.value = false
  }
}

async function handleAdjust() {
  await ElMessageBox.confirm(
    `确认对${diffList.value.length}项差异进行调账？此操作不可撤销`,
    '确认调账',
    { type: 'warning' },
  )
  const res = await adjustInventory(Number(props.planId))
  ElMessage.success(`调账完成，已调整${res.adjustedCount}项库存`)
  router.push({ name: 'PlanList' })
}

onMounted(() => {
  loadDiff()
})
</script>

<style scoped lang="scss">
.diff-summary {
  padding: 16px;

  .back-bar {
    display: flex;
    align-items: center;
    gap: 12px;
    margin-bottom: 16px;

    h3 { margin: 0; }
  }

  .text-surplus { color: #67c23a; font-weight: bold; }
  .text-loss { color: #f56c6c; font-weight: bold; }

  .summary-bar {
    margin-top: 20px;
    padding: 20px;
    background: #f5f7fa;
    border-radius: 8px;

    .stat-item {
      text-align: center;
      .stat-label { display: block; color: #909399; font-size: 13px; margin-bottom: 8px; }
      .stat-value { font-size: 24px; font-weight: bold; }
    }
  }

  .action-bar {
    display: flex;
    justify-content: center;
    padding-top: 24px;
  }
}
</style>
