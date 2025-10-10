import type { RouteRecordRaw } from 'vue-router'

const profileRoutes: RouteRecordRaw[] = [
  {
    path: '/profile',
    name: 'UserProfile',
    component: () => import('@/modules/user-management/views/UserProfileView.vue'),
    meta: { requiresAuth: true, title: 'My Profile' }
  },
  {
    path: '/settings',
    name: 'Settings',
    component: () => import('@/modules/user-management/views/UserSettingsView.vue'),
    meta: { requiresAuth: true, title: 'Settings' }
  }
]

export default profileRoutes
