import type { RouteRecordRaw } from 'vue-router'

const authRoutes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/login'
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/modules/auth/views/LoginView.vue'),
    meta: {
      isPublic: true,
      title: 'Log In or Sign Up'
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
