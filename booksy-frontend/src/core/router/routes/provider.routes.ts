import type { RouteRecordRaw } from 'vue-router'

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
  {
    path: '/providers/register',
    name: 'ProviderRegister',
    component: () => import('@/modules/provider/views/ProviderRegisterView.vue'),
    meta: {
      requiresAuth: true,
      title: 'Become a Provider',
    },
  },

  // Provider Portal (provider-only, uses ProviderLayout)
  {
    path: '/provider',
    redirect: '/provider/dashboard',
    component: () => import('@/modules/provider/layouts/ProviderLayout.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'Admin'],
    },
    children: [
      // Dashboard
      {
        path: 'dashboard',
        name: 'ProviderDashboard',
        component: () => import('@/modules/provider/views/dashboard/ProviderDashboardView.vue'),
        meta: { title: 'Provider Dashboard' },
      },
      {
        path: 'onboarding',
        name: 'ProviderOnboarding',
        component: () => import('@/modules/provider/views/dashboard/ProviderOnboardingView.vue'),
        meta: { title: 'Complete Your Profile' },
      },

      // Profile Management
      {
        path: 'profile',
        name: 'ProviderProfile',
        component: () => import('@/modules/provider/views/ProviderProfileView.vue'),
        meta: { title: 'Business Profile' },
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
        component: () => import('@/modules/provider/views/profile/BusinessHoursView.vue'),
        meta: { title: 'Business Hours' },
      },
      {
        path: 'gallery',
        name: 'ProviderGallery',
        component: () => import('@/modules/provider/views/profile/MediaGalleryView.vue'),
        meta: { title: 'Photo Gallery' },
      },

      // Services (Future)
      {
        path: 'services',
        name: 'ProviderServices',
        component: () => import('@/modules/provider/views/services/ServiceListView.vue'),
        meta: { title: 'Manage Services' },
      },

      // Staff (Future)
      {
        path: 'staff',
        name: 'ProviderStaff',
        component: () => import('@/modules/provider/views/staff/StaffListView.vue'),
        meta: { title: 'Manage Staff' },
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
