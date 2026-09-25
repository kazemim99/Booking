/**
 * Discounts as a customer meets them (openspec/changes/add-discounts-and-campaigns): a salon's automatic offers for
 * its public page, and the server's quote for a visit before it is confirmed. The server decides every price; this
 * client only asks.
 */

import { serviceCategoryClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'

/** An automatic offer at a salon. Codes are never listed. Days: 0 = Sunday … 6 = Saturday; empty = every day. */
export interface PublicOffer {
  id: string
  title: string
  description?: string | null
  owner: 'Platform' | 'Provider'
  discountKind: 'Percentage' | 'FixedAmount'
  discountValue: number
  maxDiscountAmount?: number | null
  minimumSubtotal?: number | null
  newCustomersOnly: boolean
  serviceIds: string[]
  daysOfWeek: number[]
  dailyStartTime?: string | null
  dailyEndTime?: string | null
  endsAt?: string | null
}

export type CodeOutcome = 'None' | 'Applied' | 'NotFound' | 'NotEligible' | 'BetterOfferApplied'

export interface AppliedDiscount {
  promotionId: string
  title: string
  code?: string | null
  owner: 'Platform' | 'Provider'
  amount: number
}

export interface PriceQuote {
  subtotal: number
  discount: number
  total: number
  currency: string
  appliedDiscount?: AppliedDiscount | null
  codeOutcome: CodeOutcome
  /** Persian, for the customer: why the code did or did not apply. */
  codeMessage?: string | null
}

export interface QuoteRequest {
  providerId: string
  serviceIds: string[]
  /** The appointment's start, as sent to booking creation. */
  startTime: string
  promotionCode?: string | null
}

function unwrap<T>(response: ApiResponse<T> | T): T {
  const wrapped = response as ApiResponse<T>
  return (wrapped && typeof wrapped === 'object' && 'data' in wrapped ? wrapped.data : response) as T
}

export const promotionService = {
  async getOffers(providerId: string): Promise<PublicOffer[]> {
    const response = await serviceCategoryClient.get<PublicOffer[]>(`/v1/providers/${providerId}/offers`)
    const offers = unwrap<PublicOffer[]>(response)
    return Array.isArray(offers) ? offers : []
  },

  async quote(request: QuoteRequest): Promise<PriceQuote> {
    const code = request.promotionCode?.trim()
    const response = await serviceCategoryClient.post<PriceQuote>('/v1/Bookings/quote', {
      providerId: request.providerId,
      serviceIds: request.serviceIds,
      startTime: request.startTime,
      promotionCode: code ? code : null,
    })
    return unwrap<PriceQuote>(response)
  },
}
