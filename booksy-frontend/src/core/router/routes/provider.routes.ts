import type { RouteRecordRaw, NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { ProviderStatus } from '@/core/types/enums.types'

const providerRoutes: RouteRecordRaw[] = [
  // Public Provider Pages (customer-facing)
  {
    path: '/providers/search',
    name: 'ProviderSearch',
    component: () => import('@/modules/provider/views/ProviderSearchView.vue'),
    meta: {
      requiresAuth: false,
      title: 'Search Providers',
    },
  },
  {
    path: '/providers',
    name: 'ProviderList',
    component: () => import('@/modules/provider/views/ProviderListView.vue'),
    meta: {
      requiresAuth: false,
      title: 'Browse Providers',
    },
  },
  {
    path: '/providers/:id',
    name: 'ProviderDetails',
    component: () => import('@/modules/provider/views/ProviderDetailsView.vue'),
    meta: {
      requiresAuth: false,
      title: 'Provider Details',
    },
  },

  // Provider Registration (after phone verification)
  // User must be authenticated (phone verification creates User + returns JWT)
  // This route is for Providers with Drafted status to complete their profile
  {
    path: '/registration',
    name: 'ProviderRegistration',
    component: () => import('@/modules/provider/views/registration/ProviderRegistrationView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Complete Your Provider Profile',
    },
    beforeEnter(to: RouteLocationNormalized, from: RouteLocationNormalized, next: NavigationGuardNext) {
      // Check provider status from token (no API call needed)
      const authStore = useAuthStore()
      const tokenProviderStatus = authStore.providerStatus

      console.log('[Route Guard] Provider status from token:', tokenProviderStatus)

      // If no provider exists (null), allow access to start registration
      if (tokenProviderStatus === null) {
        console.log('[Route Guard] No provider found in token, allowing registration')
        next()
        return
      }

      // If status is Drafted, allow access to continue registration
      if (tokenProviderStatus === ProviderStatus.Drafted) {
        console.log('[Route Guard] Provider status is Drafted, allowing registration')
        next()
        return
      }

      // For any other status (PendingVerification, Verified, Active, etc.),
      // redirect to dashboard - registration is complete
      console.log('[Route Guard] Provider status is', tokenProviderStatus, '- redirecting to dashboard')
      next({ name: 'ProviderDashboard' })
    },
  },

  // Provider Dashboard (main overview page)
  {
    path: '/dashboard',
    name: 'ProviderDashboard',
    component: () => import('@/modules/provider/views/dashboard/ProviderDashboardView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Dashboard',
    },
  },

  // Provider Bookings
  {
    path: '/bookings',
    name: 'ProviderBookings',
    component: () => import('@/modules/provider/views/ProviderBookingsView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Bookings',
    },
  },

  // Provider Profile
  {
    path: '/profile',
    name: 'ProviderProfile',
    component: () => import('@/modules/provider/views/dashboard/ProviderProfileView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Profile',
    },
  },

  // Provider Financial
  {
    path: '/financial',
    name: 'ProviderFinancial',
    component: () => import('@/modules/provider/views/dashboard/ProviderFinancialView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Financial',
    },
  },

  // Business Hours Management
  {
    path: '/hours',
    name: 'ProviderBusinessHours',
    component: () => import('@/modules/provider/views/hours/BusinessHoursView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Business Hours',
    },
  },

  // Gallery Management
  {
    path: '/gallery',
    name: 'ProviderGallery',
    component: () => import('@/modules/provider/views/gallery/GalleryViewNew.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Gallery',
    },
  },

  // Services Management
  {
    path: '/services',
    name: 'ProviderServices',
    component: () => import('@/modules/provider/views/ProviderServicesView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Services',
    },
  },

  // Staff Management (now integrated into Profile page)
  // {
  //   path: '/staff',
  //   name: 'ProviderStaff',
  //   component: () => import('@/modules/provider/views/ProviderStaffView.vue'),
  //   meta: {
  //     requiresAuth: true,
  //     roles: ['Provider', 'ServiceProvider'],
  //     title: 'Staff',
  //   },
  // },

  // Settings
  {
    path: '/settings',
    name: 'ProviderSettings',
    component: () => import('@/modules/provider/views/ProviderSettingsView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Settings',
    },
  },

  // Analytics
  {
    path: '/analytics',
    name: 'ProviderAnalytics',
    component: () => import('@/modules/provider/views/ProviderAnalyticsView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Analytics',
    },
  },

]

export default providerRoutes
