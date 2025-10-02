import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'

export const authGuard = async (
  to: RouteLocationNormalized,
  _from: RouteLocationNormalized,
  next: NavigationGuardNext,
) => {
  const authStore = useAuthStore()
  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth)
  const isPublic = to.matched.some((record) => record.meta.isPublic)

  // Check if user is authenticated
  const isAuthenticated = authStore.isAuthenticated

  // Route requires authentication
  if (requiresAuth && !isAuthenticated) {
    console.log('[Auth Guard] Redirecting to login - authentication required')
    return next({
      name: 'Login',
      query: { redirect: to.fullPath },
    })
  }

  // User is authenticated and trying to access public-only pages (login, register)
  if (isPublic && isAuthenticated) {
    console.log('[Auth Guard] Redirecting to home - already authenticated')
    return next({ name: 'Home' })
  }

  next()
}
