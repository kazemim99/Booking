import type { RouteRecordRaw } from 'vue-router'

/**
 * The signed-in person's notification inbox. Named `Notifications` because that is the name the old header
 * bell already navigated to — a route that did not exist until now.
 */
const notificationRoutes: RouteRecordRaw[] = [
  {
    path: '/notifications',
    name: 'Notifications',
    component: () => import('@/modules/notifications/views/NotificationsView.vue'),
    meta: {
      requiresAuth: true,
      title: 'اعلان‌ها',
    },
  },
]

export default notificationRoutes
