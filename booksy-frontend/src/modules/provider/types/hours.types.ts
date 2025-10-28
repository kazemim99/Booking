// hours.types.ts - Working hours management types

// ============================================
// Enums
// ============================================

export enum RecurrencePattern {
  None = 'None',
  Daily = 'Daily',
  Weekly = 'Weekly',
  Monthly = 'Monthly',
  Yearly = 'Yearly',
}

export enum DayStatus {
  Open = 'Open',
  Closed = 'Closed',
  Holiday = 'Holiday',
  Exception = 'Exception',
}

// ============================================
// Break Period
// ============================================

export interface BreakPeriod {
  startTime: string // HH:mm format
  endTime: string // HH:mm format
  label?: string // e.g., "Lunch Break"
}

// ============================================
// Holiday Schedule
// ============================================

export interface HolidaySchedule {
  id?: string
  date: string // YYYY-MM-DD format
  reason: string
  isRecurring: boolean
  recurrencePattern?: RecurrencePattern
  createdAt?: string
}

// ============================================
// Exception Schedule
// ============================================

export interface ExceptionSchedule {
  id?: string
  date: string // YYYY-MM-DD format
  openTime?: string // HH:mm format (null if closed)
  closeTime?: string // HH:mm format (null if closed)
  reason: string
  isClosed: boolean
  createdAt?: string
}

// ============================================
// Business Hours (Extended)
// ============================================

export interface BusinessHoursWithBreaks {
  dayOfWeek: number // 0-6 (Sunday-Saturday)
  isOpen: boolean
  openTime?: string // HH:mm format
  closeTime?: string // HH:mm format
  breaks?: BreakPeriod[]
}

// ============================================
// Calendar Day (for rendering)
// ============================================

export interface CalendarDay {
  date: string // YYYY-MM-DD
  dayOfWeek: number
  dayStatus: DayStatus
  baseHours?: {
    openTime: string
    closeTime: string
  }
  breaks?: BreakPeriod[]
  holiday?: HolidaySchedule
  exception?: ExceptionSchedule
  bookingCount?: number
  isToday: boolean
  isPast: boolean
}

// ============================================
// Availability Slot
// ============================================

export interface AvailabilitySlot {
  startTime: string // HH:mm format
  endTime: string // HH:mm format
}

export interface DayAvailability {
  date: string // YYYY-MM-DD
  isAvailable: boolean
  reason?: string // if unavailable
  slots: AvailabilitySlot[]
}

// ============================================
// Request Types
// ============================================

export interface UpdateBusinessHoursRequest {
  providerId: string
  businessHours: BusinessHoursWithBreaks[]
}

export interface AddHolidayRequest {
  providerId: string
  holiday: Omit<HolidaySchedule, 'id' | 'createdAt'>
}

export interface AddExceptionRequest {
  providerId: string
  exception: Omit<ExceptionSchedule, 'id' | 'createdAt'>
}

// ============================================
// Response Types
// ============================================

export interface HolidaysResponse {
  holidays: HolidaySchedule[]
  totalCount: number
}

export interface ExceptionsResponse {
  exceptions: ExceptionSchedule[]
  totalCount: number
}

// ============================================
// UI State Types
// ============================================

export enum CalendarViewMode {
  Week = 'Week',
  Month = 'Month',
  List = 'List',
}

export interface HoursFilters {
  year?: number
  month?: number
  showPast?: boolean
}
