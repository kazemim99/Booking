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
  if (to.meta.confirmLeave && from.meta.hasUnsavedChanges) {
    const answer = window.confirm('You have unsaved changes. Are you sure you want to leave?')
    if (!answer) {
      uiStore.stopLoading()
      return next(false)
    }
  }

  next()
}
