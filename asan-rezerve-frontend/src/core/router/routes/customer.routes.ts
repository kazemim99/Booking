/**
 * Customer Routes
 * Routes for customer-facing pages and features
 */

import type { RouteRecordRaw } from 'vue-router'

const customerRoutes: RouteRecordRaw[] = [
  {
    path: '/customer',
    component: () => import('@/modules/customer/layouts/CustomerLayout.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Customer'],
    },
    children: [
      {
        path: '',
        redirect: '/customer/dashboard',
      },
      {
        path: 'dashboard',
        name: 'CustomerDashboard',
        component: () => import('@/modules/customer/views/CustomerDashboardView.vue'),
        meta: {
          title: 'داشبورد',
          breadcrumb: 'داشبورد',
        },
      },
      {
        path: 'browse',
        name: 'ServiceBrowse',
        component: () => import('@/modules/customer/views/ServiceBrowseView.vue'),
        meta: {
          title: 'جستجوی خدمات',
          breadcrumb: 'خدمات',
        },
      },
      {
        path: 'providers',
        name: 'ProviderList',
        component: () => import('@/modules/customer/views/ProviderListView.vue'),
        meta: {
          title: 'ارائه‌دهندگان',
          breadcrumb: 'ارائه‌دهندگان',
        },
      },
      {
        path: 'provider/:id',
        name: 'ProviderDetail',
        component: () => import('@/modules/customer/views/ProviderDetailView.vue'),
        meta: {
          title: 'جزئیات ارائه‌دهنده',
          breadcrumb: 'جزئیات',
        },
      },
      {
        path: 'my-bookings',
        name: 'MyBookings',
        component: () => import('@/modules/customer/views/MyBookingsView.vue'),
        meta: {
          title: 'رزروهای من',
          breadcrumb: 'رزروها',
        },
      },
      {
        path: 'booking/:id',
        name: 'BookingDetail',
        component: () => import('@/modules/customer/views/BookingDetailView.vue'),
        meta: {
          title: 'جزئیات رزرو',
          breadcrumb: 'جزئیات رزرو',
        },
      },
      {
        path: 'book',
        name: 'CustomerBooking',
        component: () => import('@/modules/customer/views/BookingWizardView.vue'),
        meta: {
          title: 'رزرو جدید',
          breadcrumb: 'رزرو جدید',
        },
      },
      {
        path: 'profile',
        name: 'CustomerProfile',
        component: () => import('@/modules/customer/views/CustomerProfileView.vue'),
        meta: {
          title: 'پروفایل من',
          breadcrumb: 'پروفایل',
        },
      },
      {
        path: 'favorites',
        name: 'MyFavorites',
        component: () => import('@/modules/customer/views/MyFavoritesView.vue'),
        meta: {
          title: 'علاقه‌مندی‌ها',
          breadcrumb: 'علاقه‌مندی‌ها',
        },
      },
    ],
  },
]

export default customerRoutes
