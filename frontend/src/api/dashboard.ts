import request from '@/utils/request'

export interface DashboardStats {
  totalPlans: number
  activePlans: number
  pendingTasks: number
  completedTasks: number
  totalDiffItems: number
  totalMaterials: number
  totalLocations: number
  totalWarehouses: number
}

export interface StatusCount {
  status: string
  count: number
}

export interface RecentPlan {
  planId: number
  planNo: string
  planName: string
  warehouseName: string
  status: string
  planType: string
  planDate: string
  creatorName: string
  taskCount: number
  reviewedCount: number
}

export interface DailyCheckCount {
  date: string
  submittedCount: number
  reviewedCount: number
}

export interface DashboardData {
  stats: DashboardStats
  plansByStatus: StatusCount[]
  tasksByStatus: StatusCount[]
  recentPlans: RecentPlan[]
  dailyChecks: DailyCheckCount[]
}

export function getDashboard() {
  return request.get<any, DashboardData>('/dashboard')
}
