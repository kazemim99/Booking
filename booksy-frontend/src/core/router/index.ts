import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { authGuard } from './guards/auth.guard'
import { roleGuard } from './guards/role.guard'
import { navigationGuard } from './guards/navigation.guard'

// Import route modules
import authRoutes from './routes/auth.routes'
import bookingRoutes from './routes/booking.routes'
import profileRoutes from './routes/profile.routes'
import adminRoutes from './routes/admin.routes'
import providerRoutes from '@/core/router/routes/provider.routes'
import customerRoutes from './routes/customer.routes'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'Home',
    component: () => import('@/views/HomeView.vue'),
    meta: {
      requiresAuth: false, // Landing page is public
      title: 'Booksy - Find & Book Beauty & Wellness Services',
    },
  },
  ...authRoutes,
  ...bookingRoutes,
  ...profileRoutes,
  ...customerRoutes,
  ...providerRoutes,
  ...adminRoutes,
  // Error routes
  {
    path: '/forbidden',
    name: 'Forbidden',
    component: () => import('@/shared/components/layout/Forbidden.vue'),
    meta: {
      title: '403 - Forbidden',
    },
  },
  {
    path: '/server-error',
    name: 'ServerError',
    component: () => import('@/shared/components/layout/ServerError.vue'),
    meta: {
      title: '500 - Server Error',
    },
  },
  // 404 - Must be last
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/shared/components/layout/NotFound.vue'),
    meta: {
      title: '404 - Page Not Found',
    },
  },
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior(to, _from, savedPosition) {
    if (savedPosition) {
      return savedPosition
    } else if (to.hash) {
      return {
        el: to.hash,
        behavior: 'smooth',
      }
    } else {
      return { top: 0 }
    }
  },
})

// Apply guards
router.beforeEach(authGuard)
router.beforeEach(roleGuard)
router.beforeEach(navigationGuard)

// After each navigation
router.afterEach((to) => {
  // Update document title
  document.title = to.meta.title ? `${to.meta.title} | Booksy` : 'Booksy'
})

// Global error handler
router.onError((error) => {
  console.error('Router error:', error)
  router.push({ name: 'ServerError' })
})

export default router
