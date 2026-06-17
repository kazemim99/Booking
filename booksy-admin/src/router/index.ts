import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '../stores/auth.store'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('../views/Login.vue'),
    meta: { requiresAuth: false },
  },
  {
    path: '/',
    component: () => import('../layouts/AdminLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        redirect: '/dashboard',
      },
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('../views/dashboard/DashboardView.vue'),
      },
      {
        path: 'users',
        name: 'Users',
        component: () => import('../views/users/UsersList.vue'),
      },
      {
        path: 'providers',
        name: 'Providers',
        component: () => import('../views/providers/ProvidersList.vue'),
      },
      {
        path: 'providers/:id',
        name: 'ProviderDetails',
        component: () => import('../views/providers/ProviderDetails.vue'),
      },
      {
        path: 'gallery',
        name: 'Gallery',
        component: () => import('../views/gallery/GalleryManagement.vue'),
      },
      {
        path: 'services',
        name: 'Services',
        component: () => import('../views/services/ServicesList.vue'),
      },
      {
        path: 'analytics',
        name: 'Analytics',
        component: () => import('../views/analytics/AnalyticsView.vue'),
      },
      {
        path: 'payments',
        name: 'Payments',
        component: () => import('../views/payments/PaymentsList.vue'),
      },
      {
        path: 'orders',
        name: 'Orders',
        component: () => import('../views/orders/OrdersList.vue'),
      },
      {
        path: 'logs',
        name: 'Logs',
        component: () => import('../views/logs/LogsView.vue'),
      },
      {
        path: 'settings',
        name: 'Settings',
        component: () => import('../views/settings/SettingsView.vue'),
      },
    ],
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('../views/NotFound.vue'),
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach(async (to, _from, next) => {
  const authStore = useAuthStore()

  // Allow access to login page without auth
  if (to.meta.requiresAuth === false) {
    if (authStore.isAuthenticated && to.name === 'Login') {
      next({ name: 'Dashboard' })
    } else {
      next()
    }
    return
  }

  // Check if user is authenticated
  if (!authStore.isAuthenticated) {
    next({ name: 'Login', query: { redirect: to.fullPath } })
    return
  }

  // Fetch user data if not already loaded
  if (!authStore.user) {
    await authStore.fetchCurrentUser()
  }

  // Check admin role - log for debugging
  if (!authStore.isAdmin) {
    console.error('Access denied: User is not an admin', {
      user: authStore.user,
      roles: authStore.user?.roles,
      isAdmin: authStore.isAdmin
    })
    next({ name: 'Login' })
    return
  }

  next()
})

export default router
