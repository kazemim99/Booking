/**
 * Staff Management Types
 *
 * Type definitions for staff members, schedules, and related entities.
 * Based on backend Staff entity and DTOs.
 */

// ============================================================================
// ENUMS
// ============================================================================

export enum StaffRole {
  Owner = 'Owner',
  Manager = 'Manager',
  Receptionist = 'Receptionist',
  ServiceProvider = 'ServiceProvider',
  Specialist = 'Specialist',
  Assistant = 'Assistant',
  Cleaner = 'Cleaner',
  Security = 'Security',
  Maintenance = 'Maintenance',
}

export enum RemovalReason {
  Resignation = 'Resignation',
  Terminated = 'Terminated',
  EndOfContract = 'EndOfContract',
  Retirement = 'Retirement',
  Relocation = 'Relocation',
  CareerChange = 'CareerChange',
  Disciplinary = 'Disciplinary',
  Restructuring = 'Restructuring',
  TemporaryLeave = 'TemporaryLeave',
  Other = 'Other',
}

// ============================================================================
// LABEL MAPPINGS
// ============================================================================

export const STAFF_ROLE_LABELS: Record<StaffRole, string> = {
  [StaffRole.Owner]: 'Owner',
  [StaffRole.Manager]: 'Manager',
  [StaffRole.Receptionist]: 'Receptionist',
  [StaffRole.ServiceProvider]: 'Service Provider',
  [StaffRole.Specialist]: 'Specialist',
  [StaffRole.Assistant]: 'Assistant',
  [StaffRole.Cleaner]: 'Cleaner',
  [StaffRole.Security]: 'Security',
  [StaffRole.Maintenance]: 'Maintenance',
}

export const REMOVAL_REASON_LABELS: Record<RemovalReason, string> = {
  [RemovalReason.Resignation]: 'Resignation',
  [RemovalReason.Terminated]: 'Terminated',
  [RemovalReason.EndOfContract]: 'End of Contract',
  [RemovalReason.Retirement]: 'Retirement',
  [RemovalReason.Relocation]: 'Relocation',
  [RemovalReason.CareerChange]: 'Career Change',
  [RemovalReason.Disciplinary]: 'Disciplinary Action',
  [RemovalReason.Restructuring]: 'Company Restructuring',
  [RemovalReason.TemporaryLeave]: 'Temporary Leave',
  [RemovalReason.Other]: 'Other',
}

// ============================================================================
// CORE ENTITIES
// ============================================================================

/**
 * Operating hours for a specific period
 */
export interface OperatingHours {
  startTime: string // HH:mm format
  endTime: string // HH:mm format
}

/**
 * Staff schedule for a specific day
 */
export interface StaffSchedule {
  id: string
  staffId: string
  dayOfWeek: number // 0 = Sunday, 6 = Saturday
  workingHours: OperatingHours | null
  breakTimes: OperatingHours[]
  isAvailable: boolean
  effectiveFrom: string // ISO date
  effectiveTo?: string // ISO date, optional
  createdAt: string
  updatedAt: string
}

/**
 * Staff member entity (MVP - simplified)
 */
export interface Staff {
  id: string
  providerId: string
  firstName: string
  lastName: string
  fullName?: string
  phoneNumber?: string
  email?: string
  role?: string
  isActive: boolean
  hiredAt?: string // ISO date
  createdAt?: string
  updatedAt?: string
}

// ============================================================================
// REQUEST/RESPONSE TYPES
// ============================================================================

/**
 * Request to create a new staff member (MVP - simplified)
 */
export interface CreateStaffRequest {
  firstName: string
  lastName: string
  phoneNumber?: string
  email?: string
  countryCode?: string
  role?: string
  notes?: string
}

/**
 * Request to update staff member (MVP - simplified)
 */
export interface UpdateStaffRequest {
  firstName: string
  lastName: string
  phoneNumber?: string
  email?: string
  countryCode?: string
  role?: string
  notes?: string
}

/**
 * Request to deactivate staff member
 */
export interface DeactivateStaffRequest {
  terminationReason: string
  terminatedAt?: string // Optional, defaults to now
}

/**
 * Request to create/update staff schedule
 */
export interface UpsertStaffScheduleRequest {
  staffId: string
  dayOfWeek: number
  workingHours?: OperatingHours
  breakTimes?: OperatingHours[]
  isAvailable: boolean
  effectiveFrom: string
  effectiveTo?: string
}

/**
 * Request to assign services to staff
 */
export interface AssignServicesRequest {
  staffId: string
  serviceIds: string[]
}

// ============================================================================
// QUERY TYPES
// ============================================================================

/**
 * Staff list query parameters
 */
export interface StaffQueryParams {
  providerId?: string
  role?: StaffRole
  isActive?: boolean
  searchTerm?: string
  includeSchedules?: boolean
  includeServices?: boolean
  page?: number
  pageSize?: number
  sortBy?: 'name' | 'role' | 'hiredAt' | 'createdAt'
  sortOrder?: 'asc' | 'desc'
}

/**
 * Staff availability query
 */
export interface StaffAvailabilityQuery {
  staffId: string
  date: string // ISO date
  startTime: string // HH:mm
  endTime: string // HH:mm
}

// ============================================================================
// VIEW MODELS
// ============================================================================

/**
 * Simplified staff card view
 */
export interface StaffCardView {
  id: string
  fullName: string
  email: string
  phone?: string
  role: StaffRole
  roleLabel: string
  isActive: boolean
  hiredAt: string
  assignedServicesCount?: number
  avatarUrl?: string
}

/**
 * Staff details view
 */
export interface StaffDetailsView extends Staff {
  roleLabel: string
  serviceNames?: string[]
  totalHoursPerWeek?: number
  availability?: {
    dayOfWeek: number
    dayName: string
    workingHours: OperatingHours | null
    isAvailable: boolean
  }[]
}

// ============================================================================
// FILTER/SEARCH TYPES
// ============================================================================

/**
 * Staff filter options (MVP - simplified)
 */
export interface StaffFilters {
  role?: string
  isActive?: boolean | 'all'
  searchTerm?: string
}

/**
 * Staff statistics
 */
export interface StaffStatistics {
  total: number
  active: number
  inactive: number
  byRole: Record<StaffRole, number>
  averageTenure: number // in months
  recentHires: number // hired in last 30 days
}

// ============================================================================
// UTILITY TYPES
// ============================================================================

/**
 * Day of week helper
 */
export const DAYS_OF_WEEK = [
  { value: 0, label: 'Sunday', short: 'Sun' },
  { value: 1, label: 'Monday', short: 'Mon' },
  { value: 2, label: 'Tuesday', short: 'Tue' },
  { value: 3, label: 'Wednesday', short: 'Wed' },
  { value: 4, label: 'Thursday', short: 'Thu' },
  { value: 5, label: 'Friday', short: 'Fri' },
  { value: 6, label: 'Saturday', short: 'Sat' },
]

/**
 * Staff role colors for UI
 */
export const STAFF_ROLE_COLORS: Record<StaffRole, string> = {
  [StaffRole.Owner]: '#7c3aed', // purple
  [StaffRole.Manager]: '#2563eb', // blue
  [StaffRole.Receptionist]: '#0891b2', // cyan
  [StaffRole.ServiceProvider]: '#059669', // green
  [StaffRole.Specialist]: '#d97706', // amber
  [StaffRole.Assistant]: '#64748b', // slate
  [StaffRole.Cleaner]: '#6b7280', // gray
  [StaffRole.Security]: '#dc2626', // red
  [StaffRole.Maintenance]: '#ea580c', // orange
}

// ============================================================================
// TYPE GUARDS
// ============================================================================

export function isStaff(obj: unknown): obj is Staff {
  return (
    typeof obj === 'object' &&
    obj !== null &&
    'id' in obj &&
    'firstName' in obj &&
    'lastName' in obj &&
    'email' in obj &&
    'role' in obj &&
    'isActive' in obj
  )
}

export function isStaffSchedule(obj: unknown): obj is StaffSchedule {
  return (
    typeof obj === 'object' &&
    obj !== null &&
    'staffId' in obj &&
    'dayOfWeek' in obj &&
    'isAvailable' in obj
  )
}

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

/**
 * Get role label for display
 */
export function getStaffRoleLabel(role: StaffRole): string {
  return STAFF_ROLE_LABELS[role] || role
}

/**
 * Get role color for display
 */
export function getStaffRoleColor(role: StaffRole): string {
  return STAFF_ROLE_COLORS[role] || '#6b7280'
}

/**
 * Get day name from day of week number
 */
export function getDayName(dayOfWeek: number): string {
  const day = DAYS_OF_WEEK.find((d) => d.value === dayOfWeek)
  return day?.label || 'Unknown'
}

/**
 * Get short day name from day of week number
 */
export function getShortDayName(dayOfWeek: number): string {
  const day = DAYS_OF_WEEK.find((d) => d.value === dayOfWeek)
  return day?.short || '?'
}

/**
 * Calculate staff tenure in months
 */
export function calculateTenure(hiredAt: string, terminatedAt?: string): number {
  const startDate = new Date(hiredAt)
  const endDate = terminatedAt ? new Date(terminatedAt) : new Date()
  const months = (endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24 * 30)
  return Math.max(0, Math.round(months))
}

/**
 * Format tenure for display
 */
export function formatTenure(months: number): string {
  if (months < 1) return 'Less than a month'
  if (months === 1) return '1 month'
  if (months < 12) return `${months} months`

  const years = Math.floor(months / 12)
  const remainingMonths = months % 12

  if (remainingMonths === 0) {
    return years === 1 ? '1 year' : `${years} years`
  }

  return `${years} year${years > 1 ? 's' : ''}, ${remainingMonths} month${remainingMonths > 1 ? 's' : ''}`
}

/**
 * Check if staff is working on a specific day
 */
export function isWorkingOnDay(staff: Staff, dayOfWeek: number): boolean {
  if (!staff.schedules) return false
  const schedule = staff.schedules.find((s) => s.dayOfWeek === dayOfWeek && s.isAvailable)
  return !!schedule && !!schedule.workingHours
}

/**
 * Get total working hours per week
 */
export function getTotalWeeklyHours(schedules: StaffSchedule[]): number {
  let totalMinutes = 0

  for (const schedule of schedules) {
    if (!schedule.isAvailable || !schedule.workingHours) continue

    const start = parseTime(schedule.workingHours.startTime)
    const end = parseTime(schedule.workingHours.endTime)
    const workMinutes = (end.hours * 60 + end.minutes) - (start.hours * 60 + start.minutes)

    // Subtract break times
    for (const breakTime of schedule.breakTimes) {
      const breakStart = parseTime(breakTime.startTime)
      const breakEnd = parseTime(breakTime.endTime)
      const breakMinutes = (breakEnd.hours * 60 + breakEnd.minutes) - (breakStart.hours * 60 + breakStart.minutes)
      totalMinutes -= breakMinutes
    }

    totalMinutes += workMinutes
  }

  return Math.round((totalMinutes / 60) * 10) / 10 // Round to 1 decimal
}

/**
 * Parse time string (HH:mm) to hours and minutes
 */
function parseTime(time: string): { hours: number; minutes: number } {
  const [hours, minutes] = time.split(':').map(Number)
  return { hours: hours || 0, minutes: minutes || 0 }
}

/**
 * Format time for display
 */
export function formatTime(time: string): string {
  const { hours, minutes } = parseTime(time)
  const period = hours >= 12 ? 'PM' : 'AM'
  const displayHours = hours % 12 || 12
  return `${displayHours}:${minutes.toString().padStart(2, '0')} ${period}`
}

/**
 * Format operating hours for display
 */
export function formatOperatingHours(hours: OperatingHours): string {
  return `${formatTime(hours.startTime)} - ${formatTime(hours.endTime)}`
}
