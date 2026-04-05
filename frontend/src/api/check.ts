import request from '@/utils/request'
import type {
  CheckPlan, CheckTask, CheckDetail, PlanProgress,
  DiffSummaryItem, CreatePlanForm, PlanQuery, PageResult, Warehouse,
} from '@/types/check'

// ===== 盘点计划 =====

/** 盘点计划列表(分页) */
export function getCheckPlans(params: PlanQuery) {
  return request.get<any, PageResult<CheckPlan>>('/check-plans', { params })
}

/** 盘点计划详情 */
export function getCheckPlan(planId: number) {
  return request.get<any, CheckPlan>(`/check-plans/${planId}`)
}

/** 创建盘点计划 */
export function createCheckPlan(data: CreatePlanForm) {
  return request.post<any, CheckPlan>('/check-plans', data)
}

/** 编辑盘点计划 */
export function updateCheckPlan(planId: number, data: Partial<CreatePlanForm>) {
  return request.put<any, void>(`/check-plans/${planId}`, data)
}

/** 发布计划 */
export function publishPlan(planId: number) {
  return request.post<any, { taskCount: number }>(`/check-plans/${planId}/publish`)
}

/** 完成计划 */
export function completePlan(planId: number) {
  return request.post<any, void>(`/check-plans/${planId}/complete`)
}

/** 取消计划 */
export function cancelPlan(planId: number) {
  return request.post<any, void>(`/check-plans/${planId}/cancel`)
}

/** 实时进度 */
export function getPlanProgress(planId: number) {
  return request.get<any, PlanProgress>(`/check-plans/${planId}/progress`)
}

// ===== 子任务 =====

/** 子任务列表 */
export function getCheckTasks(params: { planId?: number; status?: string; myWarehouse?: boolean; page?: number; pageSize?: number }) {
  return request.get<any, PageResult<CheckTask>>('/check-tasks', { params })
}

/** 领取任务 */
export function claimTask(taskId: number) {
  return request.post<any, void>(`/check-tasks/${taskId}/claim`)
}

/** PDA扫货位码 */
export function scanLocation(params: { taskId: number; barcode: string }) {
  return request.get<any, CheckDetail[]>('/check-tasks/scan-location', { params })
}

/** PDA扫物料码+录入数量 */
export function scanMaterial(taskId: number, data: { barcode: string; actual_qty: number }) {
  return request.post<any, { hasDiff: boolean; diff_qty: number }>(`/check-tasks/${taskId}/scan-material`, data)
}

/** 提交盘点结果 */
export function submitTask(taskId: number) {
  return request.post<any, void>(`/check-tasks/${taskId}/submit`)
}

/** 复核页明细 */
export function getTaskDetails(taskId: number) {
  return request.get<any, CheckDetail[]>(`/check-tasks/${taskId}/details`)
}

/** 复核(通过/退回) */
export function reviewTask(taskId: number, data: { approved: boolean; details?: { detail_id: number; diff_reason: string }[] }) {
  return request.post<any, void>(`/check-tasks/${taskId}/review`, data)
}

// ===== 差异与调账 =====

/** 差异汇总 */
export function getDiffSummary(planId: number) {
  return request.get<any, DiffSummaryItem[]>('/reports/check-diff', { params: { planId } })
}

/** 一键调账 */
export function adjustInventory(planId: number) {
  return request.post<any, { adjustedCount: number }>('/inventory/adjust', { planId })
}

// ===== 公共 =====

/** 用户可见仓库列表 */
export function getMyWarehouses() {
  return request.get<any, Warehouse[]>('/warehouses/mine')
}

/** 仓库下区域列表 */
export function getWarehouseZones(warehouseId: number) {
  return request.get<any, string[]>(`/warehouses/${warehouseId}/zones`)
}
