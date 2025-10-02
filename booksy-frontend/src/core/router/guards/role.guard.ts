import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'

export const roleGuard = (
  to: RouteLocationNormalized,
  _from: RouteLocationNormalized,
  next: NavigationGuardNext,
) => {
  const authStore = useAuthStore()
  const requiredRoles = to.meta.roles as string[] | undefined

  // No role requirements
  if (!requiredRoles || requiredRoles.length === 0) {
    return next()
  }

  // Check if user has required role
  const userRoles = authStore.user?.roles ?? []
  const hasRequiredRole = userRoles.some((role) => requiredRoles.includes(role))

  if (!hasRequiredRole) {
    console.warn('[Role Guard] Access denied - insufficient permissions')
    return next({
      name: 'Forbidden',
      params: { message: 'You do not have permission to access this page' },
    })
  }

  next()
}
