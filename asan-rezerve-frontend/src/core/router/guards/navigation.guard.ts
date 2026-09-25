import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useUiStore } from '@/core/stores/modules/ui.store'

export const navigationGuard = (
  to: RouteLocationNormalized,
  from: RouteLocationNormalized,
  next: NavigationGuardNext,
) => {
  const uiStore = useUiStore()

  // Start loading
  uiStore.startLoading()

  // Track navigation
  console.log(`[Navigation] ${from.path} → ${to.path}`)

  // Check for unsaved changes
  // Note: Router guards are synchronous, so we use native confirm here.
  // For Vue component-level confirmations, use ConfirmationModal component instead.
  if (to.meta.confirmLeave && from.meta.hasUnsavedChanges) {
    const answer = window.confirm('آیا مطمئن هستید که بدون ذخیره تغییرات خود را ترک کنید؟')
    if (!answer) {
      uiStore.stopLoading()
      return next(false)
    }
  }

  next()
}
