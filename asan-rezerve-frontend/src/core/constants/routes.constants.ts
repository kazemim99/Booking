export const ROUTE_NAMES = {
  HOME: 'Home',
  LOGIN: 'Login',
  REGISTER: 'Register',
  FORGOT_PASSWORD: 'ForgotPassword',
  RESET_PASSWORD: 'ResetPassword',
  PROFILE: 'Profile',
  SETTINGS: 'Settings',
  BOOKINGS: 'Bookings',
  NEW_BOOKING: 'NewBooking',
  BOOKING_DETAILS: 'BookingDetails',
  SCHEDULE: 'Schedule',
  ADMIN_DASHBOARD: 'AdminDashboard',
  ADMIN_USERS: 'AdminUsers',
  ADMIN_REPORTS: 'AdminReports'
} as const

export const PUBLIC_ROUTES = [
  ROUTE_NAMES.LOGIN,
  ROUTE_NAMES.REGISTER,
  ROUTE_NAMES.FORGOT_PASSWORD,
  ROUTE_NAMES.RESET_PASSWORD
]

export const ADMIN_ROUTES = [
  ROUTE_NAMES.ADMIN_DASHBOARD,
  ROUTE_NAMES.ADMIN_USERS,
  ROUTE_NAMES.ADMIN_REPORTS
]