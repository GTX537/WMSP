<template>
  <div class="plan-list">
    <!-- 筛选区 -->
    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline>
        <el-form-item label="状态">
          <el-select v-model="query.status" placeholder="全部" clearable style="width: 140px">
            <el-option
              v-for="(item, key) in planStatusMap"
              :key="key"
              :label="item.label"
              :value="key"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="仓库">
          <el-select v-model="query.warehouse_id" placeholder="全部" clearable style="width: 180px">
            <el-option
              v-for="wh in userStore.warehouses"
              :key="wh.warehouse_id"
              :label="wh.warehouse_name"
              :value="wh.warehouse_id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="盘点日期">
          <el-date-picker
            v-model="dateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            value-format="YYYY-MM-DD"
            style="width: 260px"
            @change="onDateChange"
          />
        </el-form-item>
        <el-form-item label="关键词">
          <el-input
            v-model="query.keyword"
            placeholder="搜索计划名称"
            clearable
            style="width: 200px"
            @keyup.enter="handleSearch"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">查询</el-button>
          <el-button :icon="Refresh" @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 操作栏 + 表格 -->
    <el-card shadow="never" class="table-card">
      <template #header>
        <div class="card-header">
          <span>盘点计划列表</span>
          <el-button
            v-permission="'check:plan:create'"
            type="primary"
            :icon="Plus"
            @click="dialogVisible = true"
          >
            新建计划
          </el-button>
        </div>
      </template>

      <el-table v-loading="loading" :data="tableData" border stripe>
        <el-table-column prop="plan_no" label="计划单号" width="160" />
        <el-table-column prop="plan_name" label="计划名称" min-width="200" show-overflow-tooltip />
        <el-table-column prop="warehouse_name" label="仓库" width="140" />
        <el-table-column label="盘点类型" width="100" align="center">
          <template #default="{ row }">
            {{ planTypeMap[row.plan_type as PlanType] }}
          </template>
        </el-table-column>
        <el-table-column label="盘点模式" width="90" align="center">
          <template #default="{ row }">
            {{ checkModeMap[row.check_mode as CheckMode] }}
          </template>
        </el-table-column>
        <el-table-column prop="plan_date" label="计划日期" width="120" align="center" />
        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="planStatusMap[row.status as PlanStatus]?.type">
              {{ planStatusMap[row.status as PlanStatus]?.label }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="进度" width="150">
          <template #default="{ row }">
            <el-progress :percentage="row.progress || 0" :stroke-width="14" text-inside />
          </template>
        </el-table-column>
        <el-table-column prop="created_by_name" label="创建人" width="100" />
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="goDetail(row)">详情</el-button>
            <el-button
              v-if="row.status === 'DRAFT'"
              link
              type="success"
              @click="handlePublish(row)"
            >
              发布
            </el-button>
            <el-button
              v-if="row.status === 'DRAFT' || row.status === 'PUBLISHED'"
              link
              type="danger"
              @click="handleCancel(row)"
            >
              取消
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="query.page"
          v-model:page-size="query.pageSize"
          :total="total"
          :page-sizes="[20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          background
          @size-change="fetchList"
          @current-change="fetchList"
        />
      </div>
    </el-card>

    <!-- 新建计划弹窗 -->
    <CreatePlanDialog v-model="dialogVisible" @success="onCreateSuccess" />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus } from '@element-plus/icons-vue'
import { useUserStore } from '@/store/user'
import { getCheckPlans, publishPlan, cancelPlan } from '@/api/check'
import { planStatusMap, planTypeMap, checkModeMap } from '@/utils/dict'
import type { CheckPlan, PlanStatus, PlanType, CheckMode, PlanQuery } from '@/types/check'
import CreatePlanDialog from './CreatePlanDialog.vue'

const router = useRouter()
const userStore = useUserStore()

const loading = ref(false)
const tableData = ref<CheckPlan[]>([])
const total = ref(0)
const dialogVisible = ref(false)
const dateRange = ref<string[]>([])

const query = reactive<PlanQuery>({
  page: 1,
  pageSize: 20,
  status: '',
  warehouse_id: '',
  start_date: '',
  end_date: '',
  keyword: '',
})

function onDateChange(val: string[] | null) {
  query.start_date = val?.[0] || ''
  query.end_date = val?.[1] || ''
}

async function fetchList() {
  loading.value = true
  try {
    const res = await getCheckPlans(query)
    tableData.value = res.list
    total.value = res.total
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  query.page = 1
  fetchList()
}

function handleReset() {
  query.status = ''
  query.warehouse_id = ''
  query.start_date = ''
  query.end_date = ''
  query.keyword = ''
  dateRange.value = []
  handleSearch()
}

function goDetail(row: CheckPlan) {
  router.push({ name: 'PlanDetail', params: { planId: row.plan_id } })
}

async function handlePublish(row: CheckPlan) {
  await ElMessageBox.confirm('确认发布该盘点计划？发布后将自动生成子任务', '确认发布', { type: 'info' })
  const res = await publishPlan(row.plan_id)
  ElMessage.success(`盘点计划已发布，已自动生成${res.taskCount}个子任务`)
  fetchList()
}

async function handleCancel(row: CheckPlan) {
  const msg = row.status === 'PUBLISHED'
    ? '有工人已领取任务，确认取消吗？已领取的任务将被释放'
    : '确认取消该盘点计划？'
  await ElMessageBox.confirm(msg, '确认取消', { type: 'warning' })
  await cancelPlan(row.plan_id)
  ElMessage.success('计划已取消')
  fetchList()
}

function onCreateSuccess() {
  dialogVisible.value = false
  ElMessage.success('盘点计划创建成功')
  fetchList()
}

onMounted(() => {
  fetchList()
})
</script>

<style scoped lang="scss">
.plan-list {
  padding: 16px;

  .filter-card {
    margin-bottom: 16px;
  }

  .card-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
  }

  .pagination-wrapper {
    display: flex;
    justify-content: flex-end;
    margin-top: 16px;
  }
}
</style>
