// src/core/router/guards/auth.guard.ts
import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { ProviderStatus } from '@/modules/provider/types/provider.types'

export async function authGuard(
  to: RouteLocationNormalized,
  from: RouteLocationNormalized,
  next: NavigationGuardNext,
) {
  console.log('[AuthGuard] Navigating from:', from.path, 'to:', to.path, 'route name:', to.name)
  const authStore = useAuthStore()
  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth)
  const isPublic = to.matched.some((record) => record.meta.isPublic)
  const requiredRoles = to.meta.roles as string[] | undefined

  // Allow public routes
  if (isPublic) {
    // If authenticated user tries to access login/register pages (but NOT registration page)
    // Registration page may be needed for providers with Drafted status
    if (authStore.isAuthenticated && (to.name === 'CustomerLogin' || to.name === 'ProviderLogin' || to.name === 'Login' || to.name === 'Register')) {
      // Check if there's a redirect param (e.g., customer trying to book)
      const redirectPath = to.query.redirect as string | undefined

      // Use redirectToDashboard which now handles provider status checking AND redirect path
      await authStore.redirectToDashboard(redirectPath)
      return
    }
    next()
    return
  }

  // Check authentication
  if (requiresAuth && !authStore.isAuthenticated) {
    // Determine appropriate login page based on target route
    // Provider routes should redirect to provider login
    const isProviderRoute = to.path.startsWith('/dashboard') ||
                           to.path.startsWith('/provider/') ||
                           to.name?.toString().startsWith('Provider')

    const loginRoute = isProviderRoute ? 'ProviderLogin' : 'CustomerLogin'

    next({
      name: loginRoute,
      query: { redirect: to.fullPath },
    })
    return
  }

  // Provider status-based routing
  // Check if user is a Provider and needs status-based routing
  if (authStore.isAuthenticated && authStore.hasAnyRole(['Provider', 'ServiceProvider'])) {
    // Define all registration routes that should skip status checking
    const registrationRoutes = ['ProviderRegistration', 'OrganizationRegistration', 'IndividualRegistration']

    // If already going to any registration route, allow it without fetching status
    // This prevents unnecessary API calls and redirect loops for new users
    if (registrationRoutes.includes(to.name as string)) {
      next()
      return
    }

    // Fetch provider status if not already loaded
    if (authStore.providerStatus === null && authStore.providerId === null) {
      try {
        await authStore.fetchProviderStatus()
      } catch (err) {
        console.error('[AuthGuard] Error fetching provider status:', err)
        // On error, redirect to registration as a safe fallback
        const allowedRoutesOnError = [...registrationRoutes, 'Forbidden', 'NotFound']
        if (!allowedRoutesOnError.includes(to.name as string)) {
          next({ name: 'ProviderRegistration' })
          return
        }
      }
    }

    // Redirect based on provider status
    // Drafted or no provider record: redirect to registration
    // BUT allow access to profile, settings, and other general routes
    if (
      (authStore.providerStatus === ProviderStatus.Drafted || authStore.providerStatus === null) &&
      !registrationRoutes.includes(to.name as string)
    ) {
      // Allow access to profile, settings, and booking even for incomplete providers
      const allowedGeneralRoutes = ['ProviderProfile', 'ProviderSettings', 'NewBooking', 'BookingDetails', 'Bookings', 'Forbidden', 'NotFound', 'ServerError']
      if (!allowedGeneralRoutes.includes(to.name as string)) {
        next({ name: 'ProviderRegistration' })
        return
      }
    }

    // Prevent completed providers from accessing registration route or home
    // Redirect ONLY from Home or ProviderRegistration to dashboard
    if (
      authStore.providerStatus === ProviderStatus.Verified ||
      authStore.providerStatus === ProviderStatus.Active ||
      authStore.providerStatus === ProviderStatus.PendingVerification
    ) {
      if (to.name === 'Home' || to.name === 'ProviderRegistration') {
        console.log('[AuthGuard] Redirecting from', to.name, 'to /dashboard')
        next({ path: '/dashboard' })
        return
      }
    }
  }

  // Check role-based access
  if (requiresAuth && requiredRoles && requiredRoles.length > 0) {
    const hasRequiredRole = authStore.hasAnyRole(requiredRoles)

    if (!hasRequiredRole) {
      console.log('[AuthGuard] Access denied - insufficient permissions for', to.name)
      next({ name: 'Unauthorized' })
      return
    }
  }

  console.log('[AuthGuard] Allowing navigation to:', to.path)
  next()
}
