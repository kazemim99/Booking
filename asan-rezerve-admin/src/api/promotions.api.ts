import apiClient from '../utils/axios'

/**
 * Discounts and campaigns (openspec/changes/add-discounts-and-campaigns). Every route is admin-only on the server
 * (`AdminOnly`). A platform campaign is opt-in for salons and funded by each salon that joins it; the admin can pause
 * or end any salon's own promotion but not reword it.
 */

export type PromotionOwner = 'Platform' | 'Provider'
export type PromotionActivation = 'Automatic' | 'Code'
export type DiscountKind = 'Percentage' | 'FixedAmount'
export type PromotionStatus = 'Active' | 'Paused' | 'Ended'
export type PromotionState = 'Scheduled' | 'Active' | 'Paused' | 'Expired' | 'Exhausted' | 'Ended'
export type LifecycleAction = 'pause' | 'resume' | 'end'

export interface Promotion {
  id: string
  owner: PromotionOwner
  providerId?: string | null
  providerName?: string | null
  title: string
  description?: string | null
  activation: PromotionActivation
  code?: string | null
  discountKind: DiscountKind
  discountValue: number
  maxDiscountAmount?: number | null
  minimumSubtotal?: number | null
  newCustomersOnly: boolean
  serviceIds: string[]
  /** 0 = Sunday … 6 = Saturday. Empty means every day. */
  daysOfWeek: number[]
  dailyStartTime?: string | null
  dailyEndTime?: string | null
  startsAt: string
  endsAt?: string | null
  totalUsageLimit?: number | null
  perCustomerLimit?: number | null
  status: PromotionStatus
  state: PromotionState
  pausedByPlatform: boolean
  uses: number
  totalDiscount: number
  joinedSalons?: number | null
  currency: string
  createdAt: string
}

/** What the admin submits. Instants are UTC ISO strings; times of day "HH:mm". */
export interface PromotionTermsInput {
  title: string
  description?: string | null
  activation: PromotionActivation
  code?: string | null
  discountKind: DiscountKind
  discountValue: number
  maxDiscountAmount?: number | null
  minimumSubtotal?: number | null
  newCustomersOnly: boolean
  serviceIds?: string[] | null
  daysOfWeek?: number[] | null
  dailyStartTime?: string | null
  dailyEndTime?: string | null
  startsAt?: string | null
  endsAt?: string | null
  totalUsageLimit?: number | null
  perCustomerLimit?: number | null
}

export interface PromotionPage {
  items: Promotion[]
  totalCount: number
  page: number
  pageSize: number
}

export interface CampaignParticipant {
  providerId: string
  providerName?: string | null
  joinedAt: string
}

export interface PromotionDetails {
  promotion: Promotion
  participants: CampaignParticipant[]
}

export interface PromotionsQuery {
  owner?: PromotionOwner | 'all'
  providerId?: string
  status?: PromotionStatus | 'all'
  search?: string
  page?: number
  pageSize?: number
}

export const promotionsApi = {
  async search(query: PromotionsQuery = {}): Promise<PromotionPage> {
    const params: Record<string, unknown> = { page: query.page ?? 1, pageSize: query.pageSize ?? 20 }
    if (query.owner && query.owner !== 'all') params.owner = query.owner
    if (query.status && query.status !== 'all') params.status = query.status
    if (query.providerId) params.providerId = query.providerId
    if (query.search?.trim()) params.search = query.search.trim()
    const response = await apiClient.get<PromotionPage>('/admin/promotions', { params })
    return response.data
  },

  async details(id: string): Promise<PromotionDetails> {
    const response = await apiClient.get<PromotionDetails>(`/admin/promotions/${id}`)
    return response.data
  },

  async createCampaign(terms: PromotionTermsInput): Promise<Promotion> {
    const response = await apiClient.post<Promotion>('/admin/promotions', terms)
    return response.data
  },

  async updateCampaign(id: string, terms: PromotionTermsInput): Promise<Promotion> {
    const response = await apiClient.put<Promotion>(`/admin/promotions/${id}`, terms)
    return response.data
  },

  async changeStatus(id: string, action: LifecycleAction): Promise<Promotion> {
    const response = await apiClient.post<Promotion>(`/admin/promotions/${id}/${action}`, {})
    return response.data
  },
}
