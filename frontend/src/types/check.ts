/** 盘点计划状态 */
export type PlanStatus = 'DRAFT' | 'PUBLISHED' | 'IN_PROGRESS' | 'COMPLETED' | 'CANCELLED'

/** 盘点类型 */
export type PlanType = 'FULL' | 'ZONE' | 'SPOT'

/** 盘点模式 */
export type CheckMode = 'BLIND' | 'OPEN'

/** 子任务状态 */
export type TaskStatus = 'PENDING' | 'CLAIMED' | 'COUNTING' | 'SUBMITTED' | 'REVIEWED'

/** 盘点计划 */
export interface CheckPlan {
  plan_id: number
  plan_no: string
  plan_name: string
  warehouse_id: number
  warehouse_name: string
  plan_type: PlanType
  check_mode: CheckMode
  target_zones: string | null
  sample_rate: number | null
  plan_date: string
  status: PlanStatus
  progress: number
  created_by: number
  created_by_name: string
  created_at: string
  updated_at: string
  remark: string | null
  completed_at: string | null
}

/** 新建盘点计划表单 */
export interface CreatePlanForm {
  plan_name: string
  warehouse_id: number | null
  plan_type: PlanType
  check_mode: CheckMode
  plan_date: string
  zones: string[]
  sample_rate: number | null
  remark: string
}

/** 盘点子任务 */
export interface CheckTask {
  task_id: number
  task_no: string
  plan_id: number
  plan_name: string
  location_id: number
  location_code: string
  zone: string
  assigned_to: number | null
  assigned_name: string | null
  status: TaskStatus
  item_count: number
  scanned_count: number
  diff_count: number
  claimed_at: string | null
  submitted_at: string | null
  reviewed_by: number | null
  reviewed_at: string | null
}

/** 盘点明细 */
export interface CheckDetail {
  detail_id: number
  task_id: number
  material_id: number
  material_code: string
  material_name: string
  unit: string
  location_id: number
  batch_no: string | null
  book_qty: number
  actual_qty: number | null
  diff_qty: number
  diff_reason: string | null
  scan_time: string | null
  is_rechecked: boolean
  operator_id: number
  operator_name: string
  barcode: string
}

/** 进度概览 */
export interface PlanProgress {
  total: number
  pending: number
  claimed: number
  counting: number
  submitted: number
  reviewed: number
  progress_pct: number
  diff_count: number
}

/** 差异汇总 */
export interface DiffSummaryItem {
  material_code: string
  material_name: string
  location_code: string
  unit: string
  book_qty: number
  actual_qty: number
  diff_qty: number
  unit_cost: number | null
  diff_amount: number | null
  diff_reason: string | null
}

/** 分页参数 */
export interface PageQuery {
  page: number
  pageSize: number
}

/** 分页响应 */
export interface PageResult<T> {
  list: T[]
  total: number
  page: number
  pageSize: number
}

/** 计划列表查询参数 */
export interface PlanQuery extends PageQuery {
  status?: PlanStatus | ''
  warehouse_id?: number | ''
  start_date?: string
  end_date?: string
  keyword?: string
}

/** 通用下拉选项 */
export interface SelectOption {
  label: string
  value: string | number
}

/** 仓库 */
export interface Warehouse {
  warehouse_id: number
  warehouse_name: string
}
