<template>
  <div class="task-lobby">
    <van-nav-bar title="任务大厅" />

    <van-pull-refresh v-model="refreshing" @refresh="onRefresh">
      <van-list
        v-model:loading="loading"
        :finished="finished"
        finished-text="没有更多了"
        @load="loadMore"
      >
        <van-empty v-if="!loading && tasks.length === 0" description="暂无可领取任务" />

        <van-cell-group v-for="task in tasks" :key="task.task_id" inset class="task-card">
          <van-cell :title="task.plan_name" :label="`任务编号: ${task.task_no}`" />
          <van-cell title="货位编码" :value="task.location_code" />
          <van-cell title="区域" :value="task.zone" />
          <van-cell title="待盘物料数" :value="String(task.item_count)" />
          <div class="card-action">
            <van-button
              type="primary"
              size="small"
              round
              :loading="claimingId === task.task_id"
              @click="handleClaim(task)"
            >
              领取任务
            </van-button>
          </div>
        </van-cell-group>
      </van-list>
    </van-pull-refresh>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { showSuccessToast, showFailToast } from 'vant'
import { getCheckTasks, claimTask } from '@/api/check'
import type { CheckTask } from '@/types/check'

const router = useRouter()

const tasks = ref<CheckTask[]>([])
const loading = ref(false)
const finished = ref(false)
const refreshing = ref(false)
const claimingId = ref<number | null>(null)
let page = 1

async function loadMore() {
  try {
    const res = await getCheckTasks({ status: 'PENDING', myWarehouse: true, page, pageSize: 20 })
    tasks.value.push(...res.list)
    page++
    if (tasks.value.length >= res.total) {
      finished.value = true
    }
  } finally {
    loading.value = false
  }
}

function onRefresh() {
  tasks.value = []
  page = 1
  finished.value = false
  refreshing.value = false
  loadMore()
}

async function handleClaim(task: CheckTask) {
  claimingId.value = task.task_id
  try {
    await claimTask(task.task_id)
    showSuccessToast('任务领取成功')
    router.push({ name: 'PdaScan', params: { taskId: task.task_id } })
  } catch (e: any) {
    if (e.response?.status === 409) {
      showFailToast('该任务已被其他人领取')
      onRefresh()
    }
  } finally {
    claimingId.value = null
  }
}
</script>

<style scoped lang="scss">
.task-lobby {
  min-height: 100vh;
  background: #f7f8fa;

  .task-card {
    margin: 12px 16px;

    .card-action {
      display: flex;
      justify-content: flex-end;
      padding: 8px 16px 12px;
    }
  }
}
</style>
