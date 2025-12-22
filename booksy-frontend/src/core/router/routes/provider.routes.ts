import type { RouteRecordRaw, NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { ProviderStatus } from '@/core/types/enums.types'
import {
  organizationOnlyGuard,
  staffMemberOnlyGuard,
  independentIndividualOnlyGuard
} from '../guards/hierarchy.guard'

const providerRoutes: RouteRecordRaw[] = [
  // Public Provider Pages (customer-facing)
  {
    path: '/providers/search',
    name: 'ProviderSearch',
    component: () => import('@/modules/provider/views/ProviderSearchView.vue'),
    meta: {
      requiresAuth: false,
      layout: 'focused', // Use FocusedLayout for search page
      title: 'Search Providers',
    },
  },
  {
    path: '/providers',
    name: 'ProviderList',
    component: () => import('@/modules/provider/views/ProviderListView.vue'),
    meta: {
      requiresAuth: false,
      layout: 'focused', // Use FocusedLayout for browse page
      title: 'Browse Providers',
    },
  },
  {
    path: '/providers/:id',
    name: 'ProviderDetails',
    component: () => import('@/modules/provider/views/ProviderDetailsView.vue'),
    meta: {
      requiresAuth: false,
      layout: 'focused', // Use FocusedLayout for provider details
      title: 'Provider Details',
    },
  },

  // Provider Registration (after phone verification)
  // User must be authenticated (phone verification creates User + returns JWT)
  // This route is for Providers with Drafted status to complete their profile
  {
    path: '/provider/registration',
    name: 'ProviderRegistration',
    component: () => import('@/modules/provider/views/registration/ProviderRegistrationView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Complete Your Provider Profile',
    },
    beforeEnter(_to: RouteLocationNormalized, _from: RouteLocationNormalized, next: NavigationGuardNext) {
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

  // Organization Registration Flow
  {
    path: '/provider/registration/organization',
    name: 'OrganizationRegistration',
    component: () => import('@/modules/provider/views/registration/OrganizationRegistrationFlow.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Organization Registration',
    },
    beforeEnter(_to: RouteLocationNormalized, _from: RouteLocationNormalized, next: NavigationGuardNext) {
      const authStore = useAuthStore()
      const tokenProviderStatus = authStore.providerStatus

      // Only allow if no provider or drafted status
      if (tokenProviderStatus === null || tokenProviderStatus === ProviderStatus.Drafted) {
        next()
      } else {
        next({ name: 'ProviderDashboard' })
      }
    },
  },

  // Individual Registration Flow - DISABLED: Everyone registers as Organization
  // {
  //   path: '/provider/registration/individual',
  //   name: 'IndividualRegistration',
  //   component: () => import('@/modules/provider/views/registration/IndividualRegistrationFlow.vue'),
  //   meta: {
  //     requiresAuth: true,
  //     roles: ['Provider', 'ServiceProvider'],
  //     title: 'Individual Registration',
  //   },
  //   beforeEnter(_to: RouteLocationNormalized, _from: RouteLocationNormalized, next: NavigationGuardNext) {
  //     const authStore = useAuthStore()
  //     const tokenProviderStatus = authStore.providerStatus

  //     // Only allow if no provider or drafted status
  //     if (tokenProviderStatus === null || tokenProviderStatus === ProviderStatus.Drafted) {
  //       next()
  //     } else {
  //       next({ name: 'ProviderDashboard' })
  //     }
  //   },
  // },

  // Provider Dashboard (main overview page)
  {
    path: '/provider/dashboard',
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
    path: '/provider/bookings',
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
    path: '/provider/profile',
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
    path: '/provider/financial',
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
    path: '/provider/hours',
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
    path: '/provider/gallery',
    name: 'ProviderGallery',
    component: () => import('@/modules/provider/views/gallery/GalleryView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Gallery',
    },
  },

  // Services Management
  {
    path: '/provider/services',
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
  //   path: '/provider/staff',
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
    path: '/provider/settings',
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
    path: '/provider/analytics',
    name: 'ProviderAnalytics',
    component: () => import('@/modules/provider/views/ProviderAnalyticsView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Analytics',
    },
  },

  // Staff Management (Organization Only)
  {
    path: '/provider/staff',
    name: 'ProviderStaffManagement',
    component: () => import('@/modules/provider/views/staff/StaffManagementView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Staff Management',
    },
    beforeEnter: organizationOnlyGuard,
  },

  // Invitation Routes
  {
    path: '/provider/invitations/:id/accept',
    name: 'AcceptInvitation',
    component: () => import('@/modules/provider/views/invitation/AcceptInvitationView.vue'),
    meta: {
      requiresAuth: false,
      layout: 'auth',
      title: 'Accept Invitation',
    },
  },

  // Join Request Routes
  {
    path: '/provider/organizations/search',
    name: 'SearchOrganizations',
    component: () => import('@/modules/provider/components/joinrequest/SearchOrganizations.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Search Organizations',
    },
  },
  {
    path: '/provider/join-requests',
    name: 'MyJoinRequests',
    component: () => import('@/modules/provider/views/joinrequest/MyJoinRequestsView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'My Join Requests',
    },
  },

  // Conversion Route (Independent Individual Only)
  {
    path: '/provider/convert-to-organization',
    name: 'ConvertToOrganization',
    component: () => import('@/modules/provider/views/conversion/ConvertToOrganizationView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'Convert to Organization',
    },
    beforeEnter: independentIndividualOnlyGuard,
  },

  // ============================================================================
  // STAFF MEMBER ROUTES (Individual with Parent Organization)
  // ============================================================================

  // My Bookings (Staff Member Only)
  {
    path: '/provider/my-bookings',
    name: 'MyBookings',
    component: () => import('@/modules/provider/views/staff/MyBookingsView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'My Bookings',
    },
    beforeEnter: staffMemberOnlyGuard,
  },

  // My Earnings (Staff Member Only)
  {
    path: '/provider/my-earnings',
    name: 'MyEarnings',
    component: () => import('@/modules/provider/views/staff/MyEarningsView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'My Earnings',
    },
    beforeEnter: staffMemberOnlyGuard,
  },

  // My Profile (Staff Member Only)
  {
    path: '/provider/my-profile',
    name: 'MyProfile',
    component: () => import('@/modules/provider/views/staff/MyProfileView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'My Profile',
    },
    beforeEnter: staffMemberOnlyGuard,
  },

  // My Organization (Staff Member Only - Read-only view)
  {
    path: '/provider/my-organization',
    name: 'MyOrganization',
    component: () => import('@/modules/provider/views/staff/MyOrganizationView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'],
      title: 'My Organization',
    },
    beforeEnter: staffMemberOnlyGuard,
  },

]

export default providerRoutes
