import type { RouteRecordRaw } from 'vue-router'

/**
 * Payment return routes (gap B4).
 *
 * The backend's ZarinPal callback verifies the payment server-side and then redirects the customer's browser to
 * `{Application:ClientUrl}/payment/success` or `/payment/failure`. Those routes did not exist, so a paying customer
 * landed on a 404. These are that landing surface, and they are the HTTPS web fallback for every client — including
 * the mobile app, whose external-browser checkout returns to the same URLs. (A later phase may add Android App
 * Links / iOS Universal Links to intercept these same URLs and open the app instead; nothing here presumes that.)
 *
 * Deliberately public: the customer arrives via a gateway redirect and must never be bounced to a login screen and
 * lose sight of the payment outcome. The pages therefore show only non-sensitive identifiers from the query string
 * and re-read the authoritative state from the server — the URL is never treated as proof of payment.
 */
const paymentRoutes: RouteRecordRaw[] = [
  {
    path: '/payment/success',
    name: 'PaymentSuccess',
    component: () => import('@/modules/booking/views/PaymentSuccessView.vue'),
    meta: {
      requiresAuth: false,
      isPublic: true,
      title: 'Payment Result',
      layout: 'focused',
    },
  },
  {
    path: '/payment/failure',
    name: 'PaymentFailure',
    component: () => import('@/modules/booking/views/PaymentFailureView.vue'),
    meta: {
      requiresAuth: false,
      isPublic: true,
      title: 'Payment Failed',
      layout: 'focused',
    },
  },
]

export default paymentRoutes
