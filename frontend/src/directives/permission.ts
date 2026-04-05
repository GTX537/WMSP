import type { App, Directive } from 'vue'
import { hasPermission } from '@/utils/permission'

const permissionDirective: Directive<HTMLElement, string> = {
  mounted(el, binding) {
    if (!hasPermission(binding.value)) {
      el.parentNode?.removeChild(el)
    }
  },
}

export function setupPermissionDirective(app: App) {
  app.directive('permission', permissionDirective)
}
