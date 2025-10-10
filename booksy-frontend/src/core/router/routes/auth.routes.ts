import type { RouteRecordRaw } from 'vue-router'

const authRoutes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/login'
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/modules/auth/views/PhoneLoginView.vue'),
    meta: {
      isPublic: true,
      title: 'Log In or Sign Up'
    }
  },

  {
    path: '/provider-register',
    name: 'ProviderRegister',
    component: () => import('@/modules/provider/views/registration/ProviderRegistrationView.vue'),
    meta: {
      isPublic: true,
      title: 'Register'
    }
  }

]

export default authRoutes
