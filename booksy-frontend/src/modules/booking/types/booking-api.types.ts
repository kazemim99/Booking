/**
 * Booking API Response Types
 * These types match the backend C# DTOs exactly
 * Auto-generated from backend models - DO NOT modify manually
 */

/**
 * Customer booking DTO with enriched data (service/provider/staff names)
 * Matches backend: Booksy.ServiceCatalog.Application.Queries.Booking.GetCustomerBookings.CustomerBookingDto
 */
export interface CustomerBookingDto {
  bookingId: string
  customerId: string
  providerId: string
  serviceId: string
  staffId: string | null
  serviceName: string
  providerName: string
  staffName: string | null
  startTime: string // ISO 8601 DateTime
  endTime: string // ISO 8601 DateTime
  durationMinutes: number
  status: string // BookingStatus enum as string
  totalPrice: number // decimal
  currency: string
  paymentStatus: string // PaymentStatus enum as string
  requestedAt: string // ISO 8601 DateTime
  confirmedAt: string | null // ISO 8601 DateTime
  customerNotes: string | null
}

/**
 * Generic paginated result wrapper
 * Matches backend: Booksy.Core.Application.DTOs.PagedResult<T>
 */
export interface PagedResult<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalPages: number
  totalCount: number
  count: number
  hasPreviousPage: boolean
  hasNextPage: boolean
  isFirstPage: boolean
  isLastPage: boolean
  previousPageNumber: number | null
  nextPageNumber: number | null
  itemRange: string
}

/**
 * Pagination request parameters
 * Matches backend: Booksy.Core.Application.DTOs.PaginationRequest
 */
export interface PaginationRequest {
  page?: number // PageNumber in backend (default: 1)
  size?: number // PageSize in backend (default: 10, max: 100)
  sort?: string // Sorting field
  sortDesc?: boolean // Sort descending (default: false)
}

/**
 * Booking response (basic booking info without enrichment)
 * Matches backend: Booksy.ServiceCatalog.Api.Models.Responses.BookingResponse
 */
export interface BookingResponse {
  id: string
  customerId: string
  providerId: string
  serviceId: string
  staffProviderId: string | null
  status: string
  startTime: string
  endTime: string
  durationMinutes: number
  totalPrice: number
  currency: string
  paymentStatus: string
  createdAt: string
}

/**
 * Customer bookings paginated response
 * Used by GET /api/v1/bookings/my-bookings endpoint
 */
export type CustomerBookingsResponse = PagedResult<CustomerBookingDto>

/**
 * Booking status enum (matches backend)
 */
export enum BookingStatus {
  Pending = 'Pending',
  Confirmed = 'Confirmed',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
  NoShow = 'NoShow',
}

/**
 * Payment status enum (matches backend)
 */
export enum PaymentStatus {
  Pending = 'Pending',
  PartiallyPaid = 'PartiallyPaid',
  Paid = 'Paid',
  Refunded = 'Refunded',
  Failed = 'Failed',
}

/**
 * Get my bookings query parameters
 */
export interface GetMyBookingsParams extends PaginationRequest {
  status?: string
  from?: string // ISO 8601 DateTime for FromDate
  to?: string // ISO 8601 DateTime for ToDate
}
