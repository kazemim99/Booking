/**
 * Payment Service
 * Handles payment operations with Iranian payment gateways
 * Currently supports ZarinPal
 */

import { httpClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'

const API_VERSION = 'v1'
const API_BASE = `/${API_VERSION}/payments`

// ==================== Types ====================

/**
 * Payment provider types
 */
export enum PaymentProvider {
  ZarinPal = 'zarinpal',
  Behpardakht = 'behpardakht',
  Mellat = 'mellat',
  Saman = 'saman',
}

/**
 * Payment status
 */
export enum PaymentStatus {
  Pending = 'pending',
  Processing = 'processing',
  Completed = 'completed',
  Failed = 'failed',
  Refunded = 'refunded',
  Cancelled = 'cancelled',
}

/**
 * ZarinPal payment creation request
 * POST /api/v1/payments/zarinpal/create
 */
export interface ZarinPalCreateRequest {
  bookingId: string
  amount: number // Amount in Iranian Rials
  description: string
  callbackUrl: string
  mobile?: string
  email?: string
}

/**
 * ZarinPal payment creation response
 */
export interface ZarinPalCreateResponse {
  authority: string // ZarinPal authority token
  paymentUrl: string // URL to redirect user for payment
  success: boolean
  message?: string
}

/**
 * ZarinPal payment verification request
 * POST /api/v1/payments/zarinpal/verify
 */
export interface ZarinPalVerifyRequest {
  authority: string
  status: string // From ZarinPal callback query parameter
}

/**
 * ZarinPal payment verification response
 */
export interface ZarinPalVerifyResponse {
  refId: string // Reference ID from ZarinPal
  success: boolean
  message: string
  amount?: number
  cardPan?: string // Masked card number
  cardHash?: string
  feeType?: string
  fee?: number
}

/**
 * Payment record
 */
export interface Payment {
  id: string
  bookingId: string
  customerId: string
  providerId: string
  provider: PaymentProvider
  amount: number
  currency: string
  status: PaymentStatus
  authority?: string
  refId?: string
  description: string
  callbackUrl?: string
  cardPan?: string
  createdAt: string
  paidAt?: string
  verifiedAt?: string
  failedAt?: string
  failureReason?: string
}

/**
 * Refund request
 */
export interface RefundRequest {
  paymentId: string
  amount?: number // Partial refund amount, if not specified refunds full amount
  reason: string
  notes?: string
}

/**
 * Refund response
 */
export interface RefundResponse {
  refundId: string
  amount: number
  status: PaymentStatus
  message: string
}

// ==================== Payment Service Class ====================

class PaymentService {
  // ============================================
  // ZarinPal Integration
  // ============================================

  /**
   * Create ZarinPal payment request
   * POST /api/v1/payments/zarinpal/create
   *
   * Example request (from Postman collection):
   * {
   *   "bookingId": "booking-123",
   *   "amount": 2000000,
   *   "description": "پرداخت رزرو کوتاهی مو",
   *   "callbackUrl": "https://booksy.ir/payment/callback",
   *   "mobile": "09123456789",
   *   "email": "user@example.com"
   * }
   *
   * Example response:
   * {
   *   "authority": "A00000000000000000000000000123456789",
   *   "paymentUrl": "https://www.zarinpal.com/pg/StartPay/A00000000000000000000000000123456789",
   *   "success": true
   * }
   */
  async createZarinPalPayment(data: ZarinPalCreateRequest): Promise<ZarinPalCreateResponse> {
    try {
      console.log('[PaymentService] Creating ZarinPal payment:', data)

      const response = await httpClient.post<ApiResponse<ZarinPalCreateResponse>>(
        `${API_BASE}/zarinpal/create`,
        data
      )

      console.log('[PaymentService] ZarinPal payment created:', response.data)

      // Handle wrapped response format
      const paymentData = response.data?.data || response.data
      return paymentData as ZarinPalCreateResponse
    } catch (error) {
      console.error('[PaymentService] Error creating ZarinPal payment:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Verify ZarinPal payment
   * POST /api/v1/payments/zarinpal/verify
   *
   * This endpoint is called after user completes payment on ZarinPal
   * and is redirected back to the callback URL with authority and status
   *
   * Example request:
   * {
   *   "authority": "A00000000000000000000000000123456789",
   *   "status": "OK"
   * }
   *
   * Example response:
   * {
   *   "refId": "123456789",
   *   "success": true,
   *   "message": "پرداخت با موفقیت انجام شد",
   *   "amount": 2000000,
   *   "cardPan": "6219-86**-****-1234"
   * }
   */
  async verifyZarinPalPayment(data: ZarinPalVerifyRequest): Promise<ZarinPalVerifyResponse> {
    try {
      console.log('[PaymentService] Verifying ZarinPal payment:', data)

      const response = await httpClient.post<ApiResponse<ZarinPalVerifyResponse>>(
        `${API_BASE}/zarinpal/verify`,
        data
      )

      console.log('[PaymentService] ZarinPal payment verified:', response.data)

      // Handle wrapped response format
      const verificationData = response.data?.data || response.data
      return verificationData as ZarinPalVerifyResponse
    } catch (error) {
      console.error('[PaymentService] Error verifying ZarinPal payment:', error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Payment Management
  // ============================================

  /**
   * Get payment by ID
   * GET /api/v1/payments/{id}
   */
  async getPaymentById(id: string): Promise<Payment> {
    try {
      console.log(`[PaymentService] Fetching payment: ${id}`)

      const response = await httpClient.get<ApiResponse<Payment>>(
        `${API_BASE}/${id}`
      )

      console.log('[PaymentService] Payment retrieved:', response.data)

      const payment = response.data?.data || response.data
      return payment as Payment
    } catch (error) {
      console.error(`[PaymentService] Error fetching payment ${id}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Get payments for a booking
   * GET /api/v1/payments/booking/{bookingId}
   */
  async getPaymentsByBooking(bookingId: string): Promise<Payment[]> {
    try {
      console.log(`[PaymentService] Fetching payments for booking: ${bookingId}`)

      const response = await httpClient.get<ApiResponse<Payment[]>>(
        `${API_BASE}/booking/${bookingId}`
      )

      console.log('[PaymentService] Payments retrieved:', response.data)

      const payments = response.data?.data || response.data
      return payments as Payment[]
    } catch (error) {
      console.error(`[PaymentService] Error fetching payments for booking ${bookingId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Get customer payments
   * GET /api/v1/payments/customer/{customerId}
   */
  async getCustomerPayments(customerId: string, pageNumber = 1, pageSize = 10): Promise<{
    items: Payment[]
    totalItems: number
    pageNumber: number
    pageSize: number
  }> {
    try {
      console.log(`[PaymentService] Fetching payments for customer: ${customerId}`)

      const response = await httpClient.get<ApiResponse<{
        items: Payment[]
        totalItems: number
        pageNumber: number
        pageSize: number
      }>>(
        `${API_BASE}/customer/${customerId}`,
        {
          params: { pageNumber, pageSize }
        }
      )

      console.log('[PaymentService] Customer payments retrieved:', response.data)

      const payments = response.data?.data || response.data
      return payments as any
    } catch (error) {
      console.error(`[PaymentService] Error fetching customer payments:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Get provider payments
   * GET /api/v1/payments/provider/{providerId}
   */
  async getProviderPayments(providerId: string, pageNumber = 1, pageSize = 10): Promise<{
    items: Payment[]
    totalItems: number
    pageNumber: number
    pageSize: number
  }> {
    try {
      console.log(`[PaymentService] Fetching payments for provider: ${providerId}`)

      const response = await httpClient.get<ApiResponse<{
        items: Payment[]
        totalItems: number
        pageNumber: number
        pageSize: number
      }>>(
        `${API_BASE}/provider/${providerId}`,
        {
          params: { pageNumber, pageSize }
        }
      )

      console.log('[PaymentService] Provider payments retrieved:', response.data)

      const payments = response.data?.data || response.data
      return payments as any
    } catch (error) {
      console.error(`[PaymentService] Error fetching provider payments:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Refunds
  // ============================================

  /**
   * Request a refund
   * POST /api/v1/payments/{id}/refund
   */
  async requestRefund(paymentId: string, data: RefundRequest): Promise<RefundResponse> {
    try {
      console.log(`[PaymentService] Requesting refund for payment: ${paymentId}`, data)

      const response = await httpClient.post<ApiResponse<RefundResponse>>(
        `${API_BASE}/${paymentId}/refund`,
        data
      )

      console.log('[PaymentService] Refund requested:', response.data)

      const refund = response.data?.data || response.data
      return refund as RefundResponse
    } catch (error) {
      console.error(`[PaymentService] Error requesting refund:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Utility Methods
  // ============================================

  /**
   * Format amount in Iranian Rials to readable string
   */
  formatAmount(amount: number): string {
    return new Intl.NumberFormat('fa-IR', {
      style: 'currency',
      currency: 'IRR',
      minimumFractionDigits: 0,
    }).format(amount)
  }

  /**
   * Convert Tomans to Rials (multiply by 10)
   */
  tomansToRials(tomans: number): number {
    return tomans * 10
  }

  /**
   * Convert Rials to Tomans (divide by 10)
   */
  rialsToTomans(rials: number): number {
    return rials / 10
  }

  /**
   * Get human-readable payment status in Persian
   */
  getPaymentStatusLabel(status: PaymentStatus): string {
    const labels: Record<PaymentStatus, string> = {
      [PaymentStatus.Pending]: 'در انتظار پرداخت',
      [PaymentStatus.Processing]: 'در حال پردازش',
      [PaymentStatus.Completed]: 'پرداخت موفق',
      [PaymentStatus.Failed]: 'پرداخت ناموفق',
      [PaymentStatus.Refunded]: 'بازگشت وجه',
      [PaymentStatus.Cancelled]: 'لغو شده',
    }
    return labels[status] || status
  }

  // ============================================
  // Error Handling
  // ============================================

  /**
   * Centralized error handling
   */
  private handleError(error: unknown): Error {
    if (error instanceof Error) {
      return error
    }
    return new Error('خطا در پردازش پرداخت')
  }
}

// Export singleton instance
export const paymentService = new PaymentService()
export default paymentService
