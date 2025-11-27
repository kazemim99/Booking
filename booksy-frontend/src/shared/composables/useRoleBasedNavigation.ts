import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { ProviderStatus } from '@/modules/provider/types/provider.types'

export type UserRole = 'guest' | 'customer' | 'provider' | 'admin'

export interface MenuItem {
  id: string
  label: string
  icon: string
  path?: string
  action?: () => void
  badge?: string | number
  divider?: boolean
  variant?: 'default' | 'danger'
}

export interface MenuConfig {
  type: UserRole
  menuItems: MenuItem[]
  theme?: 'default' | 'business' | 'admin'
}

/**
 * Composable for role-based navigation and menu configuration
 */
export function useRoleBasedNavigation() {
  const authStore = useAuthStore()
  const router = useRouter()

  /**
   * Determine the current user's primary role
   */
  const userRole = computed<UserRole>(() => {
    if (!authStore.isAuthenticated) return 'guest'

    const roles = authStore.userRoles

    // Priority: Admin > Provider > Customer > Guest
    if (roles.includes('Admin') || roles.includes('Administrator')) return 'admin'
    if (roles.includes('Provider') || roles.includes('ServiceProvider')) return 'provider'
    if (roles.includes('Customer') || roles.includes('Client')) return 'customer'

    return 'guest'
  })

  /**
   * Check if provider has completed onboarding
   */
  const isProviderOnboardingComplete = computed(() => {
    if (userRole.value !== 'provider') return true

    return (
      authStore.providerStatus !== null &&
      authStore.providerStatus !== ProviderStatus.Drafted &&
      authStore.providerId !== null
    )
  })

  /**
   * Get role-specific display label
   */
  const roleLabel = computed(() => {
    switch (userRole.value) {
      case 'admin':
        return 'مدیر سیستم'
      case 'provider':
        return 'ارائه‌دهنده'
      case 'customer':
        return 'مشتری'
      default:
        return 'مهمان'
    }
  })

  /**
   * Get role-specific theme class
   */
  const roleThemeClass = computed(() => {
    switch (userRole.value) {
      case 'admin':
        return 'theme-admin'
      case 'provider':
        return 'theme-business'
      case 'customer':
        return 'theme-customer'
      default:
        return 'theme-default'
    }
  })

  /**
   * Get default path for each role
   */
  const roleDefaultPath = computed(() => {
    switch (userRole.value) {
      case 'admin':
        return '/admin/dashboard'
      case 'provider':
        return isProviderOnboardingComplete.value ? '/provider/dashboard' : '/provider/registration'
      case 'customer':
        return '/'
      default:
        return '/'
    }
  })

  /**
   * Check if a redirect path is appropriate for the current user role
   */
  const isValidRedirectForRole = (redirectPath: string): boolean => {
    // Empty or invalid paths
    if (!redirectPath || redirectPath === '/') {
      return false
    }

    const role = userRole.value

    // Admin can go anywhere except provider/customer specific routes
    if (role === 'admin') {
      return redirectPath.startsWith('/admin')
    }

    // Providers can only redirect to provider routes if onboarding complete
    if (role === 'provider') {
      if (!isProviderOnboardingComplete.value) {
        return false // Force onboarding
      }
      return (
        redirectPath.startsWith('/provider')
      )
    }

    // Customers can redirect to public or customer routes
    if (role === 'customer') {
      const publicRoutes = ['/', '/providers', '/search', '/about', '/contact']
      const isPublicRoute = publicRoutes.some((route) => redirectPath.startsWith(route))
      const isCustomerRoute = redirectPath.startsWith('/booking')

      return isPublicRoute || isCustomerRoute
    }

    // Guest - should not have redirect paths (redirects to login)
    return false
  }

  /**
   * Redirect to appropriate page based on role and optional intent path
   * @param intentPath - Optional path user was trying to access
   */
  const redirectToRoleDefault = async (intentPath?: string) => {
    console.log('[RoleBasedNavigation] Redirecting user', {
      role: userRole.value,
      intentPath,
      isOnboardingComplete: isProviderOnboardingComplete.value,
    })

    // If there's an intent path and it's valid for the role, use it
    if (intentPath && isValidRedirectForRole(intentPath)) {
      console.log('[RoleBasedNavigation] Using intent path:', intentPath)
      await router.push(intentPath)
      return
    }

    // Otherwise, use role default
    const defaultPath = roleDefaultPath.value
    console.log('[RoleBasedNavigation] Using role default path:', defaultPath)
    await router.push(defaultPath)
  }

  /**
   * Check if user should be auto-redirected from homepage
   */
  const shouldRedirectFromHome = computed(() => {
    // Providers with complete onboarding should go to dashboard
    if (userRole.value === 'provider' && isProviderOnboardingComplete.value) {
      return true
    }

    // Admins should go to admin dashboard
    if (userRole.value === 'admin') {
      return true
    }

    // Customers and guests stay on homepage
    return false
  })

  /**
   * Get menu configuration for current role
   * Note: Actual menu items are defined in the UserMenu component
   * This returns metadata about the menu
   */
  const menuConfig = computed<MenuConfig>(() => {
    return {
      type: userRole.value,
      menuItems: [], // Populated by UserMenu component
      theme:
        userRole.value === 'admin'
          ? 'admin'
          : userRole.value === 'provider'
          ? 'business'
          : 'default',
    }
  })

  return {
    // Computed
    userRole,
    roleLabel,
    roleThemeClass,
    roleDefaultPath,
    isProviderOnboardingComplete,
    shouldRedirectFromHome,
    menuConfig,

    // Methods
    isValidRedirectForRole,
    redirectToRoleDefault,
  }
}
