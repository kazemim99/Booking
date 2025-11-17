import type { RouteRecordRaw } from 'vue-router'

const authRoutes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/modules/auth/views/LoginView.vue'),
    meta: {
      isPublic: true,
      title: 'ورود مشتری - رزرو نوبت | Booksy'
    }
  },
  {
    path: '/provider/login',
    name: 'ProviderLogin',
    component: () => import('@/modules/auth/views/ProviderLoginView.vue'),
    meta: {
      isPublic: true,
      title: 'ورود ارائه‌دهندگان - پنل کسب و کار | Booksy'
    }
  },
  {
    path: '/phone-verification',
    name: 'PhoneVerification',
    component: () => import('@/modules/auth/views/VerificationView.vue'),
    meta: {
      isPublic: true,
      title: 'Phone Verification'
    }
  }
]

export default authRoutes
