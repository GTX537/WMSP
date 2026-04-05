<template>
  <div class="dashboard-page">
    <!-- 统计卡片 -->
    <el-row :gutter="16" class="stat-row">
      <el-col :span="6" v-for="card in statCards" :key="card.label">
        <div class="stat-card" :style="{ borderTop: `3px solid ${card.color}` }">
          <div class="stat-info">
            <div class="stat-label">{{ card.label }}</div>
            <div class="stat-value" :style="{ color: card.color }">{{ card.value }}</div>
          </div>
          <div class="stat-icon" :style="{ backgroundColor: card.color + '18', color: card.color }">
            <el-icon :size="28"><component :is="card.icon" /></el-icon>
          </div>
        </div>
      </el-col>
    </el-row>

    <!-- 图表行 -->
    <el-row :gutter="16" class="chart-row">
      <el-col :span="16">
        <div class="chart-card">
          <div class="chart-header">
            <h3>近两周盘点趋势</h3>
          </div>
          <div ref="barChartRef" class="chart-body"></div>
        </div>
      </el-col>
      <el-col :span="8">
        <div class="chart-card">
          <div class="chart-header">
            <h3>任务状态分布</h3>
          </div>
          <div ref="pieChartRef" class="chart-body"></div>
        </div>
      </el-col>
    </el-row>

    <!-- 最近计划 -->
    <div class="chart-card table-card">
      <div class="chart-header">
        <h3>最近盘点计划</h3>
        <el-button type="primary" text @click="$router.push('/check/plans')">查看全部</el-button>
      </div>
      <el-table :data="recentPlans" stripe style="width: 100%">
        <el-table-column prop="planNo" label="计划编号" width="160" />
        <el-table-column prop="planName" label="计划名称" min-width="160" />
        <el-table-column prop="warehouseName" label="仓库" width="120" />
        <el-table-column prop="planType" label="类型" width="80" align="center">
          <template #default="{ row }">
            <el-tag size="small" :type="planTypeTag(row.planType)">{{ planTypeMap[row.planType] || row.planType }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag size="small" :type="statusTag(row.status)">{{ planStatusMap[row.status] || row.status }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="planDate" label="计划日期" width="120" />
        <el-table-column label="进度" width="120" align="center">
          <template #default="{ row }">
            <span v-if="row.taskCount === 0">-</span>
            <el-progress v-else :percentage="Math.round(row.reviewedCount / row.taskCount * 100)" :stroke-width="8" />
          </template>
        </el-table-column>
        <el-table-column prop="creatorName" label="创建人" width="100" />
      </el-table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, markRaw } from 'vue'
import * as echarts from 'echarts/core'
import { BarChart, PieChart } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  GridComponent,
  LegendComponent,
} from 'echarts/components'
import { CanvasRenderer } from 'echarts/renderers'
import {
  TrendCharts,
  Document,
  Warning,
  Finished,
} from '@element-plus/icons-vue'
import { getDashboard } from '@/api/dashboard'
import type { DashboardData, RecentPlan } from '@/api/dashboard'
import { planStatusMap, planTypeMap } from '@/utils/dict'

echarts.use([BarChart, PieChart, TitleComponent, TooltipComponent, GridComponent, LegendComponent, CanvasRenderer])

const barChartRef = ref<HTMLElement>()
const pieChartRef = ref<HTMLElement>()
let barChart: echarts.ECharts | null = null
let pieChart: echarts.ECharts | null = null

const dashboard = ref<DashboardData | null>(null)
const recentPlans = ref<RecentPlan[]>([])

const statCards = computed(() => {
  const s = dashboard.value?.stats
  if (!s) return []
  return [
    { label: '进行中计划', value: s.activePlans, color: '#409EFF', icon: markRaw(TrendCharts) },
    { label: '待处理任务', value: s.pendingTasks, color: '#E6A23C', icon: markRaw(Document) },
    { label: '已完成任务', value: s.completedTasks, color: '#67C23A', icon: markRaw(Finished) },
    { label: '差异项数', value: s.totalDiffItems, color: '#F56C6C', icon: markRaw(Warning) },
  ]
})

const taskStatusLabelMap: Record<string, string> = {
  PENDING: '待认领',
  CLAIMED: '已认领',
  COUNTING: '盘点中',
  SUBMITTED: '已提交',
  REVIEWED: '已复核',
}
const taskStatusColorMap: Record<string, string> = {
  PENDING: '#909399',
  CLAIMED: '#E6A23C',
  COUNTING: '#409EFF',
  SUBMITTED: '#67C23A',
  REVIEWED: '#0DAC7B',
}

function statusTag(status: string) {
  const map: Record<string, string> = { DRAFT: 'info', PUBLISHED: '', COMPLETED: 'success', CANCELLED: 'danger', IN_PROGRESS: 'warning' }
  return map[status] || ''
}
function planTypeTag(type: string) {
  const map: Record<string, string> = { FULL: '', ZONE: 'success', SPOT: 'warning' }
  return map[type] || ''
}

function renderBarChart() {
  if (!barChartRef.value || !dashboard.value) return
  barChart = echarts.init(barChartRef.value)
  const data = dashboard.value.dailyChecks
  barChart.setOption({
    tooltip: { trigger: 'axis' },
    legend: { data: ['提交数', '复核数'], bottom: 0 },
    grid: { top: 20, right: 20, bottom: 40, left: 50 },
    xAxis: { type: 'category', data: data.map((d) => d.date) },
    yAxis: { type: 'value', minInterval: 1 },
    series: [
      {
        name: '提交数',
        type: 'bar',
        data: data.map((d) => d.submittedCount),
        itemStyle: { color: '#409EFF', borderRadius: [4, 4, 0, 0] },
        barMaxWidth: 24,
      },
      {
        name: '复核数',
        type: 'bar',
        data: data.map((d) => d.reviewedCount),
        itemStyle: { color: '#67C23A', borderRadius: [4, 4, 0, 0] },
        barMaxWidth: 24,
      },
    ],
  })
}

function renderPieChart() {
  if (!pieChartRef.value || !dashboard.value) return
  pieChart = echarts.init(pieChartRef.value)
  const data = dashboard.value.tasksByStatus.map((t) => ({
    name: taskStatusLabelMap[t.status] || t.status,
    value: t.count,
    itemStyle: { color: taskStatusColorMap[t.status] || '#909399' },
  }))
  const total = data.reduce((sum, d) => sum + d.value, 0)
  pieChart.setOption({
    tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
    legend: { orient: 'vertical', right: 10, top: 'center' },
    series: [
      {
        type: 'pie',
        radius: ['45%', '70%'],
        center: ['35%', '50%'],
        avoidLabelOverlap: false,
        label: {
          show: true,
          position: 'center',
          formatter: `合计\n{total|${total}}`,
          rich: { total: { fontSize: 24, fontWeight: 'bold', lineHeight: 36, color: '#303133' } },
          fontSize: 13,
          color: '#909399',
        },
        emphasis: { label: { show: true, fontSize: 14 } },
        data,
      },
    ],
  })
}

function handleResize() {
  barChart?.resize()
  pieChart?.resize()
}

onMounted(async () => {
  try {
    dashboard.value = await getDashboard()
    recentPlans.value = dashboard.value.recentPlans
    renderBarChart()
    renderPieChart()
    window.addEventListener('resize', handleResize)
  } catch {
    // handled by interceptor
  }
})

onBeforeUnmount(() => {
  barChart?.dispose()
  pieChart?.dispose()
  window.removeEventListener('resize', handleResize)
})
</script>

<style scoped lang="scss">
.dashboard-page {
  padding: 20px;
}

.stat-row {
  margin-bottom: 16px;
}

.stat-card {
  background: #fff;
  border-radius: 8px;
  padding: 20px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.06);

  .stat-info {
    .stat-label {
      font-size: 13px;
      color: #909399;
      margin-bottom: 8px;
    }
    .stat-value {
      font-size: 28px;
      font-weight: 700;
    }
  }

  .stat-icon {
    width: 52px;
    height: 52px;
    border-radius: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
  }
}

.chart-row {
  margin-bottom: 16px;
}

.chart-card {
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.06);
  padding: 20px;

  .chart-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 12px;

    h3 {
      margin: 0;
      font-size: 15px;
      color: #303133;
    }
  }

  .chart-body {
    height: 300px;
  }
}

.table-card {
  margin-bottom: 20px;
}
</style>
