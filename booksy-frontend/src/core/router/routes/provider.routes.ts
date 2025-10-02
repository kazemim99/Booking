// src/router/routes/provider.routes.ts

import type { RouteRecordRaw } from 'vue-router'

/**
 * Provider Module Routes
 * Defines all routes for provider browsing and management
 */

const providerRoutes: RouteRecordRaw[] = [
  // ============================================
  // Public Routes (No Auth Required)
  // ============================================

  {
    path: '/providers',
    name: 'ProviderList',
    component: () => import('../../../modules/provider/views/ProviderListView.vue'),
    meta: {
      requiresAuth: false,
      title: 'Browse Providers',
      description: 'Find and book services from verified providers',
    },
  },
  {
    path: '/providers/search',
    name: 'ProviderSearch',
    component: () => import('../../../modules/provider/views/ProviderSearchView.vue'),
    meta: {
      requiresAuth: false,
      title: 'Search Providers',
      description: 'Advanced provider search with filters',
    },
  },
  {
    path: '/providers/:id',
    name: 'ProviderDetails',
    component: () => import('../../../modules/provider/views/ProviderDetailsView.vue'),
    meta: {
      requiresAuth: false,
      title: 'Provider Details',
    },
    props: true,
  },

  // ============================================
  // Provider Registration (Auth Required)
  // ============================================

  {
    path: '/providers/register',
    name: 'ProviderRegister',
    component: () => import('../../../modules/provider/views/ProviderRegisterView.vue'),
    meta: {
      requiresAuth: true,
      title: 'Become a Provider',
      description: 'Register your business and start accepting bookings',
    },
  },

  // ============================================
  // Provider Dashboard (Auth + Role Required)
  // ============================================

  {
    path: '/provider',
    redirect: '/provider/dashboard',
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'Admin'],
    },
  },
  {
    path: '/provider/dashboard',
    name: 'ProviderDashboard',
    component: () => import('../../../modules/provider/views/ProviderDashboardView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'Admin'],
      title: 'Provider Dashboard',
      icon: 'dashboard',
    },
  },
  {
    path: '/provider/profile',
    name: 'ProviderProfile',
    component: () => import('../../../modules/provider/views/ProviderProfileView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'Admin'],
      title: 'My Business Profile',
      icon: 'business',
    },
  },
  {
    path: '/provider/services',
    name: 'ProviderServices',
    component: () => import('../../../modules/provider/views/ProviderServicesView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'Admin'],
      title: 'Manage Services',
      icon: 'services',
    },
  },
  {
    path: '/provider/staff',
    name: 'ProviderStaff',
    component: () => import('../../../modules/provider/views/ProviderStaffView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'Admin'],
      title: 'Manage Staff',
      icon: 'people',
    },
  },
  {
    path: '/provider/schedule',
    name: 'ProviderSchedule',
    component: () => import('../../../modules/provider/views/ProviderScheduleView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'Admin'],
      title: 'Business Hours',
      icon: 'schedule',
    },
  },
  {
    path: '/provider/bookings',
    name: 'ProviderBookings',
    component: () => import('../../../modules/provider/views/ProviderBookingsView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'Admin'],
      title: 'Manage Bookings',
      icon: 'calendar',
    },
  },
  {
    path: '/provider/analytics',
    name: 'ProviderAnalytics',
    component: () => import('../../../modules/provider/views/ProviderAnalyticsView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'Admin'],
      title: 'Analytics & Reports',
      icon: 'analytics',
    },
  },
  {
    path: '/provider/settings',
    name: 'ProviderSettings',
    component: () => import('../../../modules/provider/views/ProviderSettingsView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'Admin'],
      title: 'Provider Settings',
      icon: 'settings',
    },
  },

  // ============================================
  // Admin Routes (Admin Only)
  // ============================================

  {
    path: '/admin/providers',
    name: 'AdminProviders',
        component: () => import('../../../modules/provider/views/AdminProvidersView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Admin'],
      title: 'Manage All Providers',
    },
  },
  {
    path: '/admin/providers/:id/verify',
    name: 'AdminProviderVerify',
    component: () => import('../../../modules/provider/views/AdminProviderVerifyView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Admin'],
      title: 'Verify Provider',
    },
    props: true,
  },
]

export default providerRoutes
