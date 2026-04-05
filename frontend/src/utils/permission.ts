import { useUserStore } from '@/store/user'

/** 检查当前用户是否拥有指定权限 */
export function hasPermission(code: string): boolean {
  const userStore = useUserStore()
  return userStore.permissions.includes(code) || userStore.permissions.includes('*')
}
