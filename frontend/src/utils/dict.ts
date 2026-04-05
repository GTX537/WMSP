import type { PlanStatus, PlanType, CheckMode, TaskStatus } from '@/types/check'

/** 计划状态映射 */
export const planStatusMap: Record<PlanStatus, { label: string; type: 'info' | '' | 'warning' | 'success' | 'danger' }> = {
  DRAFT: { label: '草稿', type: 'info' },
  PUBLISHED: { label: '已发布', type: '' },
  IN_PROGRESS: { label: '进行中', type: 'warning' },
  COMPLETED: { label: '已完成', type: 'success' },
  CANCELLED: { label: '已取消', type: 'danger' },
}

/** 盘点类型映射 */
export const planTypeMap: Record<PlanType, string> = {
  FULL: '全盘',
  ZONE: '区域盘',
  SPOT: '抽盘',
}

/** 盘点模式映射 */
export const checkModeMap: Record<CheckMode, string> = {
  BLIND: '盲盘',
  OPEN: '明盘',
}

/** 任务状态映射 */
export const taskStatusMap: Record<TaskStatus, { label: string; type: 'info' | '' | 'warning' | 'success' | 'danger' }> = {
  PENDING: { label: '待领取', type: 'info' },
  CLAIMED: { label: '已领取', type: '' },
  COUNTING: { label: '盘点中', type: 'warning' },
  SUBMITTED: { label: '已提交', type: 'success' },
  REVIEWED: { label: '已复核', type: 'success' },
}

/** 差异原因选项 */
export const diffReasonOptions = [
  { label: '盘盈-供应商多发', value: 'SURPLUS_OVERSUPPLY' },
  { label: '盘盈-入库未登记', value: 'SURPLUS_UNREGISTERED' },
  { label: '盘亏-自然损耗', value: 'LOSS_NATURAL' },
  { label: '盘亏-破损报废', value: 'LOSS_DAMAGED' },
  { label: '盘亏-被盗', value: 'LOSS_THEFT' },
  { label: '盘亏-出库未登记', value: 'LOSS_UNREGISTERED' },
  { label: '账目错误', value: 'ACCOUNTING_ERROR' },
  { label: '其他', value: 'OTHER' },
]
