import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { Warehouse } from '@/types/check'

export const useUserStore = defineStore('user', () => {
  const userId = ref<number>(0)
  const realName = ref('')
  const permissions = ref<string[]>([])
  const warehouses = ref<Warehouse[]>([])

  function setUser(data: { userId: number; realName: string; permissions: string[]; warehouses: Warehouse[] }) {
    userId.value = data.userId
    realName.value = data.realName
    permissions.value = data.permissions
    warehouses.value = data.warehouses
  }

  function reset() {
    userId.value = 0
    realName.value = ''
    permissions.value = []
    warehouses.value = []
  }

  return { userId, realName, permissions, warehouses, setUser, reset }
})
