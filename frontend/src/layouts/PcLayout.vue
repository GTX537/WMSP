<template>
  <div class="pc-layout">
    <el-container>
      <el-header class="app-header">
        <div class="header-left">
          <h1 class="app-title" @click="$router.push('/dashboard')" style="cursor: pointer">库房物料数字化盘点系统</h1>
          <el-menu
            :default-active="activeMenu"
            mode="horizontal"
            :ellipsis="false"
            class="header-nav"
            router
          >
            <el-menu-item index="/dashboard">工作台</el-menu-item>
            <el-menu-item index="/check/plans">盘点计划</el-menu-item>
          </el-menu>
        </div>
        <div class="header-right">
          <span class="user-name">{{ userStore.realName }}</span>
          <el-button text @click="handleLogout">退出</el-button>
        </div>
      </el-header>
      <el-main class="app-main">
        <router-view />
      </el-main>
    </el-container>
  </div>
</template>

<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router'
import { computed } from 'vue'
import { useUserStore } from '@/store/user'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const activeMenu = computed(() => {
  if (route.path.startsWith('/check')) return '/check/plans'
  return route.path
})

function handleLogout() {
  localStorage.removeItem('token')
  userStore.reset()
  router.push('/login')
}
</script>

<style scoped lang="scss">
.pc-layout {
  min-height: 100vh;
  background: #f0f2f5;

  .app-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    background: #fff;
    box-shadow: 0 1px 4px rgba(0, 21, 41, 0.08);
    padding: 0 24px;
    z-index: 10;
  }

  .header-left {
    display: flex;
    align-items: center;
    gap: 24px;
    height: 100%;
  }

  .app-title {
    margin: 0;
    font-size: 18px;
    color: #303133;
    white-space: nowrap;
  }

  .header-nav {
    border-bottom: none;
    height: 60px;
    background: transparent;
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 12px;

    .user-name {
      color: #606266;
      font-size: 14px;
    }
  }

  .app-main {
    padding: 0;
    min-height: calc(100vh - 60px);
  }
}
</style>
