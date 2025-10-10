// src/core/router/guards/auth.guard.ts
import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'

export function authGuard(
  to: RouteLocationNormalized,
  from: RouteLocationNormalized,
  next: NavigationGuardNext
) {

  console.log(from);
  const authStore = useAuthStore()
  const requiresAuth = to.matched.some(record => record.meta.requiresAuth)
  const isPublic = to.matched.some(record => record.meta.isPublic)
  const requiredRoles = to.meta.roles as string[] | undefined

  // Allow public routes
  if (isPublic) {
    // If authenticated user tries to access login/register
    if (authStore.isAuthenticated && (to.name === 'Login' || to.name === 'Register')) {
      // Check if user has Draft status and needs registration
      if (authStore.user?.status === 'Draft' && authStore.hasAnyRole(['Provider', 'ServiceProvider'])) {
        next({ name: 'ProviderRegistration' })
        return
      }
      // Otherwise redirect to dashboard
      authStore.redirectToDashboard()
      return
    }
    next()
    return
  }

  // Check authentication
  if (requiresAuth && !authStore.isAuthenticated) {
    next({
      name: 'Login',
      query: { redirect: to.fullPath }
    })
    return
  }

  // Check if user has Draft status and needs to complete registration
  // Exclude the registration page itself to avoid redirect loops
  if (authStore.isAuthenticated &&
    authStore.user?.status === 'Draft' &&
    to.name !== 'ProviderRegistration') {

    if (authStore.hasAnyRole(['Provider', 'ServiceProvider'])) {
      next({ name: 'ProviderRegistration' })
      return
    }
  }

  // Check role-based access
  if (requiresAuth && requiredRoles && requiredRoles.length > 0) {
    const hasRequiredRole = authStore.hasAnyRole(requiredRoles)

    if (!hasRequiredRole) {
      next({ name: 'Unauthorized' })
      return
    }
  }

  next()
}
