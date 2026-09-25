import type { RouteRecordRaw } from 'vue-router'

const authRoutes: RouteRecordRaw[] = [
  // Customer Authentication Routes
  {
    path: '/customer/login',
    name: 'CustomerLogin',
    component: () => import('@/modules/auth/views/LoginView.vue'),
    meta: {
      isPublic: true,
      title: 'ورود مشتری - رزرو نوبت | AsanRezerve'
    }
  },
  {
    path: '/customer/phone-verification',
    name: 'CustomerPhoneVerification',
    component: () => import('@/modules/auth/views/VerificationView.vue'),
    meta: {
      isPublic: true,
      title: 'تأیید شماره تلفن - مشتری | AsanRezerve',
      userType: 'Customer' // Meta field to identify route type
    }
  },

  // Provider Authentication Routes
  {
    path: '/provider/login',
    name: 'ProviderLogin',
    component: () => import('@/modules/auth/views/ProviderLoginView.vue'),
    meta: {
      isPublic: true,
      title: 'ورود ارائه‌دهندگان - پنل کسب و کار | AsanRezerve'
    }
  },
  {
    path: '/provider/phone-verification',
    name: 'ProviderPhoneVerification',
    component: () => import('@/modules/auth/views/VerificationView.vue'),
    meta: {
      isPublic: true,
      title: 'تأیید شماره تلفن - ارائه‌دهنده | AsanRezerve',
      userType: 'Provider' // Meta field to identify route type
    }
  },

  // Legacy routes (redirects for backwards compatibility)
  {
    path: '/login',
    redirect: '/customer/login'
  },
  {
    path: '/phone-verification',
    redirect: '/customer/phone-verification'
  }
]

export default authRoutes
