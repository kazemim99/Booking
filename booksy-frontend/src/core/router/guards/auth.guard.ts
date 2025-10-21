// src/core/router/guards/auth.guard.ts
import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { ProviderStatus } from '@/modules/provider/types/provider.types'

export async function authGuard(
  to: RouteLocationNormalized,
  from: RouteLocationNormalized,
  next: NavigationGuardNext,
) {
  console.log(from)
  const authStore = useAuthStore()
  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth)
  const isPublic = to.matched.some((record) => record.meta.isPublic)
  const requiredRoles = to.meta.roles as string[] | undefined

  // Allow public routes
  if (isPublic) {
    // If authenticated user tries to access login/register
    if (authStore.isAuthenticated && (to.name === 'Login' || to.name === 'Register')) {
      // Use redirectToDashboard which now handles provider status checking
      await authStore.redirectToDashboard()
      return
    }
    next()
    return
  }

  // Check authentication
  if (requiresAuth && !authStore.isAuthenticated) {
    next({
      name: 'Login',
      query: { redirect: to.fullPath },
    })
    return
  }

  // Provider status-based routing
  // Check if user is a Provider and needs status-based routing
  if (authStore.isAuthenticated && authStore.hasAnyRole(['Provider', 'ServiceProvider'])) {
    // Fetch provider status if not already loaded
    if (authStore.providerStatus === null && authStore.providerId === null) {
      try {
        await authStore.fetchProviderStatus()
      } catch (err) {
        console.error('[AuthGuard] Error fetching provider status:', err)
        // On error, redirect to registration as a safe fallback
        if (to.name !== 'ProviderRegistration') {
          next({ name: 'ProviderRegistration' })
          return
        }
      }
    }

    // Redirect based on provider status
    // Drafted or no provider record: redirect to registration
    if (
      (authStore.providerStatus === ProviderStatus.Drafted || authStore.providerStatus === null) &&
      to.name !== 'ProviderRegistration'
    ) {
      next({ name: 'ProviderRegistration' })
      return
    }
    debugger
    // Prevent completed providers from accessing registration route
    if (
      (authStore.providerStatus === ProviderStatus.Verified ||
        authStore.providerStatus === ProviderStatus.Active ||
        authStore.providerStatus === ProviderStatus.PendingVerification) &&
      to.name === 'Home'
    ) {
      next({ path: '/provider/dashboard' })
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
