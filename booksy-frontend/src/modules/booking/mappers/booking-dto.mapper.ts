/**
 * Booking DTO Mappers
 * Convert between backend DTOs and frontend domain models
 */

import type { CustomerBookingDto } from '../types/booking-api.types'
import type { Booking } from '@/core/types/entities.types'
import type { Appointment } from '../types/booking.types'

/**
 * Map CustomerBookingDto (from API) to Booking (domain model)
 * Converts the enriched API response to the app's domain model
 */
export function mapCustomerBookingDtoToBooking(dto: CustomerBookingDto): Booking {
  return {
    id: dto.bookingId,
    customerId: dto.customerId,
    providerId: dto.providerId,
    serviceId: dto.serviceId,
    staffId: dto.staffId || undefined,
    status: dto.status as any, // BookingStatus
    startTime: dto.startTime,
    endTime: dto.endTime,
    duration: dto.durationMinutes,

    // Money object from separate price and currency fields
    totalAmount: {
      amount: dto.totalPrice,
      currency: dto.currency,
    },

    paidAmount: {
      amount: 0, // Not provided by CustomerBookingDto
      currency: dto.currency,
    },

    depositAmount: undefined, // Not provided by CustomerBookingDto

    customerNotes: dto.customerNotes || undefined,
    staffNotes: undefined, // Not provided by CustomerBookingDto

    cancellationReason: undefined, // Not provided by CustomerBookingDto
    cancelledBy: undefined,
    cancelledAt: undefined,
    confirmedAt: dto.confirmedAt || undefined,
    completedAt: undefined, // Not provided by CustomerBookingDto
    noShowAt: undefined,
    reminderSentAt: undefined,

    source: 'web', // Default
    referenceNumber: dto.bookingId, // Use booking ID as reference
    metadata: {
      serviceName: dto.serviceName,
      providerName: dto.providerName,
      staffName: dto.staffName,
      paymentStatus: dto.paymentStatus,
      requestedAt: dto.requestedAt,
    },

    // BaseEntity fields
    createdAt: dto.requestedAt,
    updatedAt: dto.confirmedAt || dto.requestedAt,
  }
}

/**
 * Map CustomerBookingDto to Appointment (legacy type)
 * For backward compatibility with existing components
 */
export function mapCustomerBookingDtoToAppointment(dto: CustomerBookingDto): Appointment {
  return {
    id: dto.bookingId,
    providerId: dto.providerId,
    clientId: dto.customerId,
    serviceId: dto.serviceId,
    staffProviderId: dto.staffId || undefined,

    // Scheduling
    scheduledStartTime: dto.startTime,
    scheduledEndTime: dto.endTime,
    actualStartTime: undefined,
    actualEndTime: undefined,
    duration: dto.durationMinutes,

    // Status & Info
    status: dto.status as any, // AppointmentStatus
    bookingNotes: dto.customerNotes || undefined,
    internalNotes: undefined,

    // Pricing
    basePrice: dto.totalPrice,
    totalPrice: dto.totalPrice,
    deposit: undefined,
    currency: dto.currency,

    // Cancellation
    cancellationReason: undefined,
    cancelledAt: undefined,
    cancelledBy: undefined,

    // Metadata
    isRecurring: false,
    recurringGroupId: undefined,
    remindersEnabled: true,
    lastReminderSentAt: undefined,

    // Timestamps
    createdAt: dto.requestedAt,
    confirmedAt: dto.confirmedAt || undefined,
    completedAt: undefined,
    lastModifiedAt: dto.confirmedAt || dto.requestedAt,
  }
}

/**
 * Enriched booking view model for UI components
 * Includes formatted display values
 */
export interface EnrichedBookingView extends CustomerBookingDto {
  // Formatted display values
  formattedStartTime: string
  formattedEndTime: string
  formattedDate: string
  formattedTime: string
  formattedPrice: string
  formattedDuration: string

  // Status badge properties
  statusColor: string
  statusLabel: string

  // Flags
  isUpcoming: boolean
  isPast: boolean
  canCancel: boolean
  canReschedule: boolean
}

/**
 * Map CustomerBookingDto to EnrichedBookingView with formatted values
 * Adds display formatting for Persian/Jalali calendar and currency
 */
export function mapToEnrichedBookingView(dto: CustomerBookingDto): EnrichedBookingView {
  const startDate = new Date(dto.startTime)
  const endDate = new Date(dto.endTime)
  const now = new Date()

  const isUpcoming = startDate > now
  const isPast = endDate < now

  // Status colors and labels
  const statusConfig: Record<string, { color: string; label: string }> = {
    Requested: { color: 'info', label: 'درخواست شده' },
    Pending: { color: 'warning', label: 'در انتظار تایید' },
    Confirmed: { color: 'success', label: 'تایید شده' },
    InProgress: { color: 'info', label: 'در حال انجام' },
    Completed: { color: 'success', label: 'انجام شده' },
    Cancelled: { color: 'error', label: 'لغو شده' },
    NoShow: { color: 'error', label: 'عدم حضور' },
  }

  const status = statusConfig[dto.status] || { color: 'default', label: dto.status }

  return {
    ...dto,

    // Formatted values
    formattedStartTime: startDate.toLocaleString('fa-IR'),
    formattedEndTime: endDate.toLocaleString('fa-IR'),
    formattedDate: startDate.toLocaleDateString('fa-IR'),
    formattedTime: startDate.toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit' }),
    formattedPrice: `${dto.totalPrice.toLocaleString('fa-IR')} ${dto.currency}`,
    formattedDuration: `${dto.durationMinutes} دقیقه`,

    // Status
    statusColor: status.color,
    statusLabel: status.label,

    // Flags
    isUpcoming,
    isPast,
    canCancel: isUpcoming && ['Requested', 'Pending', 'Confirmed'].includes(dto.status),
    canReschedule: isUpcoming && ['Requested', 'Pending', 'Confirmed'].includes(dto.status),
  }
}
