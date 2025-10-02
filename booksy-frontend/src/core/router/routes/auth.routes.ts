import type { RouteRecordRaw } from 'vue-router'

const authRoutes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/modules/auth/views/LoginView.vue'),
    meta: {
      isPublic: true,
      title: 'Login'
    }
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('@/modules/auth/views/RegisterView.vue'),
    meta: {
      isPublic: true,
      title: 'Register'
    }
  },
  {
    path: '/forgot-password',
    name: 'ForgotPassword',
    component: () => import('@/modules/auth/views/ForgotPasswordView.vue'),
    meta: {
      isPublic: true,
      title: 'Forgot Password'
    }
  },
  {
    path: '/reset-password/:token',
    name: 'ResetPassword',
    component: () => import('@/modules/auth/views/ResetPasswordView.vue'),
    meta: {
      isPublic: true,
      title: 'Reset Password'
    }
  }
]

export default authRoutes