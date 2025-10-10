import type { RouteRecordRaw } from 'vue-router'

const adminRoutes: RouteRecordRaw[] = [
  {
    path: '/admin',
    redirect: '/admin/dashboard',
    component: () => import('@/modules/admin/layouts/AdminLayout.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Admin'], // ✅ Only Admins
    },
    children: [
      {
        path: 'dashboard',
        name: 'AdminDashboard',
        component: () => import('@/modules/admin/views/AdminDashboardView.vue'),
        meta: { title: 'Admin Dashboard' },
      },

      // ✅ Provider Management (Admin only)
      {
        path: 'providers',
        name: 'AdminProviderList',
        component: () => import('@/modules/provider/views/ProviderListView.vue'),
        meta: { title: 'Manage Providers' },
      },
      {
        path: 'providers/:id',
        name: 'AdminProviderDetails',
        component: () => import('@/modules/provider/views/ProviderDetailsView.vue'),
        meta: { title: 'Provider Details' },
      },

      // ✅ User Management
      {
        path: 'users',
        name: 'AdminUserList',
        component: () => import('@/modules/admin/views/UserListView.vue'),
        meta: { title: 'Manage Users' },
      },

      // ✅ Booking Management
      {
        path: 'bookings',
        name: 'AdminBookingList',
        component: () => import('@/modules/admin/views/BookingListView.vue'),
        meta: { title: 'Manage Bookings' },
      },
    ],
  },
]

export default adminRoutes
