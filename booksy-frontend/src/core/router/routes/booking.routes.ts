import type { RouteRecordRaw } from 'vue-router'

const bookingRoutes: RouteRecordRaw[] = [
  {
    path: '/my-appointments',
    name: 'Bookings',
    component: () => import('@/modules/booking/views/AppointmentListView.vue'),
    meta: {
      requiresAuth: true,
      title: 'My Appointments'
    }
  },
  {
    path: '/bookings/new',
    name: 'NewBooking',
    component: () => import('@/modules/booking/views/BookingView.vue'),
    meta: {
      requiresAuth: true,
      title: 'New Booking',
      layout: 'focused'
    }
  },
  {
    path: '/bookings/:id',
    name: 'BookingDetails',
    component: () => import('@/modules/booking/views/AppointmentDetailsView.vue'),
    meta: {
      requiresAuth: true,
      title: 'Booking Details'
    }
  },
  {
    path: '/schedule',
    name: 'Schedule',
    component: () => import('@/modules/booking/views/ScheduleManagementView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Provider', 'Admin'],
      title: 'Manage Schedule'
    }
  }
]

export default bookingRoutes