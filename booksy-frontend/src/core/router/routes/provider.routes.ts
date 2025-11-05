import type { RouteRecordRaw, NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useProviderStore } from '@/modules/provider/stores/provider.store'

const providerRoutes: RouteRecordRaw[] = [
  // Public Provider Pages (customer-facing)
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
  {
    path: '/provider/registration',
    name: 'ProviderRegistration',
    component: () => import('@/modules/provider/views/registration/ProviderRegistrationView.vue'),
    meta: {
      requiresAuth: true,
      title: 'Complete Your Provider Profile',
    },
  },


  // Provider Portal (provider-only, uses ProviderLayout)
  {
    path: '/provider',
    redirect: '/provider/dashboard',
    component: () => import('@/modules/provider/layouts/ProviderLayout.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'ServiceProvider'], // ✅ Only Providers
    },
    children: [
      // Dashboard
      {
        path: 'dashboard',
        name: 'ProviderDashboard',
        component: () => import('@/modules/provider/views/dashboard/ProviderDashboardView.vue'),
        meta: { title: 'Provider Dashboard' },
      },

      // Profile Management
      {
        path: 'profile',
        name: 'ProviderProfile',
        component: () => import('@/modules/provider/views/ProviderProfileView.vue'),
        meta: { title: 'Business Profile' },
        beforeEnter: async (_to: RouteLocationNormalized, _from: RouteLocationNormalized, next: NavigationGuardNext) => {
          const providerStore = useProviderStore()
          if (!providerStore.currentProvider) {
            await providerStore.loadCurrentProvider()
          }

          const p = providerStore.currentProvider
          const isComplete = !!(
            p &&
            p.profile.businessName &&
            p.profile.description &&
            p.contactInfo.email &&
            p.contactInfo.primaryPhone &&
            p.address.addressLine1 &&
            p.businessHours && p.businessHours.length > 0
          )

          if (!isComplete) {
            return next({ name: 'ProviderOnboarding' })
          }
          next()
        },
      },
      {
        path: 'profile/business-info',
        name: 'ProviderBusinessInfo',
        component: () => import('@/modules/provider/views/profile/BusinessInfoView.vue'),
        meta: { title: 'Business Information' },
      },
      {
        path: 'hours',
        name: 'ProviderBusinessHours',
        component: () => import('@/modules/provider/views/hours/BusinessHoursView.vue'),
        meta: { title: 'Business Hours' },
      },
      {
        path: 'gallery',
        name: 'ProviderGallery',
        component: () => import('@/modules/provider/views/gallery/GalleryView.vue'),
        meta: { title: 'Photo Gallery' },
      },

      // Services
      {
        path: 'services',
        name: 'ProviderServices',
        component: () => import('@/modules/provider/views/ProviderServicesView.vue'),
        meta: { title: 'Manage Services' },
      },
      {
        path: 'services/new',
        name: 'ServiceEditor',
        component: () => import('@/modules/provider/views/services/ServiceEditorViewSimple.vue'),
        meta: { title: 'Add Service' },
      },
      {
        path: 'services/:id/edit',
        name: 'ServiceEditorEdit',
        component: () => import('@/modules/provider/views/services/ServiceEditorViewSimple.vue'),
        meta: { title: 'Edit Service' },
      },

      // Staff
      {
        path: 'staff',
        name: 'StaffList',
        component: () => import('@/modules/provider/views/staff/StaffListView.vue'),
        meta: { title: 'Manage Staff' },
      },

      // Settings
      {
        path: 'settings',
        name: 'ProviderSettings',
        component: () => import('@/modules/provider/views/ProviderSettingsView.vue'),
        meta: { title: 'Settings' },
      },

      // Bookings (Future)
      {
        path: 'bookings',
        name: 'ProviderBookings',
        component: () => import('@/modules/provider/views/bookings/BookingCalendarView.vue'),
        meta: { title: 'Bookings Calendar' },
      },

      // Analytics (Future)
      {
        path: 'analytics',
        name: 'ProviderAnalytics',
        component: () => import('@/modules/provider/views/analytics/ProviderAnalyticsView.vue'),
        meta: { title: 'Business Analytics' },
      },
    ],
  },
]

export default providerRoutes
