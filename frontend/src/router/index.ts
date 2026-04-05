import { createRouter, createWebHistory } from 'vue-router'
import NProgress from 'nprogress'
import 'nprogress/nprogress.css'
import { useUserStore } from '@/store/user'
import { getMe } from '@/api/auth'

NProgress.configure({ showSpinner: false })

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      redirect: '/dashboard',
    },
    {
      path: '/login',
      name: 'Login',
      component: () => import('@/views/login/LoginPage.vue'),
      meta: { title: '登录', public: true },
    },
    // ===== 看板 =====
    {
      path: '/dashboard',
      component: () => import('@/layouts/PcLayout.vue'),
      children: [
        {
          path: '',
          name: 'Dashboard',
          component: () => import('@/views/dashboard/DashboardPage.vue'),
          meta: { title: '工作台' },
        },
      ],
    },
    // ===== PC端 =====
    {
      path: '/check',
      component: () => import('@/layouts/PcLayout.vue'),
      children: [
        {
          path: 'plans',
          name: 'PlanList',
          component: () => import('@/views/check/plan/PlanList.vue'),
          meta: { title: '盘点计划', permission: 'check:plan' },
        },
        {
          path: 'plans/:planId',
          name: 'PlanDetail',
          component: () => import('@/views/check/plan/PlanDetail.vue'),
          meta: { title: '计划详情', permission: 'check:plan' },
          props: true,
        },
        {
          path: 'review/:taskId',
          name: 'TaskReview',
          component: () => import('@/views/check/review/TaskReview.vue'),
          meta: { title: '盘点复核', permission: 'check:review' },
          props: true,
        },
        {
          path: 'diff/:planId',
          name: 'DiffSummary',
          component: () => import('@/views/check/diff/DiffSummary.vue'),
          meta: { title: '差异处理', permission: 'report:diff' },
          props: true,
        },
      ],
    },
    // ===== PDA端 =====
    {
      path: '/pda',
      component: () => import('@/layouts/PdaLayout.vue'),
      children: [
        {
          path: 'lobby',
          name: 'PdaLobby',
          component: () => import('@/views/pda/lobby/TaskLobby.vue'),
          meta: { title: '任务大厅', permission: 'check:scan' },
        },
        {
          path: 'scan/:taskId',
          name: 'PdaScan',
          component: () => import('@/views/pda/scan/ScanPage.vue'),
          meta: { title: '扫码盘点', permission: 'check:scan' },
          props: true,
        },
      ],
    },
  ],
})

router.beforeEach(async (to, _from, next) => {
  NProgress.start()
  document.title = (to.meta.title as string) || '盘点管理'

  // 公开页面直接放行
  if (to.meta.public) {
    next()
    return
  }

  const token = localStorage.getItem('token')
  if (!token) {
    next({ name: 'Login', query: { redirect: to.fullPath } })
    return
  }

  // 如果 store 中没有用户信息，尝试从后端恢复
  const userStore = useUserStore()
  if (userStore.userId === 0) {
    try {
      const res = await getMe()
      userStore.setUser({
        userId: res.userId,
        realName: res.realName,
        permissions: res.permissions,
        warehouses: res.warehouses,
      })
    } catch {
      localStorage.removeItem('token')
      next({ name: 'Login', query: { redirect: to.fullPath } })
      return
    }
  }

  next()
})

router.afterEach(() => {
  NProgress.done()
})

export default router
