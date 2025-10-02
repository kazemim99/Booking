import type { RouteRecordRaw } from 'vue-router'

const adminRoutes: RouteRecordRaw[] = [
  {
    path: '/admin',
    name: 'Admin',
    redirect: '/admin/dashboard',
    meta: {
      requiresAuth: true,
      roles: ['Admin']
    },
    children: [
      {
        path: 'dashboard',
        name: 'AdminDashboard',
        component: () => import('@/modules/admin/views/AdminDashboardView.vue'),
        meta: {
          title: 'Admin Dashboard'
        }
      },
      {
        path: 'users',
        name: 'AdminUsers',
        component: () => import('@/modules/admin/views/AdminUsersView.vue'),
        meta: {
          title: 'User Management'
        }
      },
      {
        path: 'reports',
        name: 'AdminReports',
        component: () => import('@/modules/admin/views/AdminReportsView.vue'),
        meta: {
          title: 'Reports'
        }
      }
    ]
  }
]

export default adminRoutes