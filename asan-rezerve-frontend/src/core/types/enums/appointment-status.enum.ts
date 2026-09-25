/**
 * Appointment/Booking Status Enumeration
 * Re-export from main enums file for modular access
 */

export { BookingStatus } from '../enums.types'

// Additional booking status specific helpers
export const APPOINTMENT_STATUS_LABELS: Record<string, string> = {
  requested: 'درخواست شده',
  pending: 'در انتظار',
  confirmed: 'تایید شده',
  in_progress: 'در حال انجام',
  completed: 'تکمیل شده',
  cancelled: 'لغو شده',
  no_show: 'عدم حضور',
  rescheduled: 'زمان‌بندی مجدد',
}

export const APPOINTMENT_STATUS_COLORS: Record<string, string> = {
  requested: 'blue',
  pending: 'yellow',
  confirmed: 'green',
  in_progress: 'purple',
  completed: 'green',
  cancelled: 'red',
  no_show: 'orange',
  rescheduled: 'indigo',
}

export const APPOINTMENT_STATUS_ICONS: Record<string, string> = {
  requested: 'clock',
  pending: 'hourglass',
  confirmed: 'check-circle',
  in_progress: 'play-circle',
  completed: 'check-double',
  cancelled: 'x-circle',
  no_show: 'alert-triangle',
  rescheduled: 'refresh-cw',
}

// Status groups for filtering
export const ACTIVE_STATUSES = ['requested', 'pending', 'confirmed', 'in_progress']
export const COMPLETED_STATUSES = ['completed']
export const CANCELLED_STATUSES = ['cancelled', 'no_show']
export const ALL_STATUSES = [
  'requested',
  'pending',
  'confirmed',
  'in_progress',
  'completed',
  'cancelled',
  'no_show',
  'rescheduled',
]
