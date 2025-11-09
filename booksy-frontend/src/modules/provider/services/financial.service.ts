/**
 * Financial Service
 * Handles provider earnings, transactions, and payout management
 */

import { serviceCategoryClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'
import type {
  EarningsSummary,
  GetEarningsRequest,
  GetEarningsResponse,
  Payout,
  CreatePayoutRequest,
  GetPayoutsRequest,
  Transaction,
  GetTransactionsRequest,
  FinancialDashboard,
} from '../types/financial.types'
import type { PagedResult } from '@/core/types/common.types'

const API_VERSION = 'v1'
const FINANCIAL_BASE = `/${API_VERSION}/financial`
const PAYOUT_BASE = `/${API_VERSION}/payouts`

// ==================== Financial Service Class ====================

class FinancialService {
  // ============================================
  // Earnings Operations
  // ============================================

  /**
   * Get provider earnings for a date range
   * GET /api/v1/financial/provider/{providerId}/earnings
   */
  async getProviderEarnings(request: GetEarningsRequest): Promise<GetEarningsResponse> {
    try {
      console.log('[FinancialService] Fetching earnings:', request)

      const response = await serviceCategoryClient.get<ApiResponse<GetEarningsResponse>>(
        `${FINANCIAL_BASE}/provider/${request.providerId}/earnings`,
        {
          params: {
            startDate: request.startDate,
            endDate: request.endDate,
            commissionPercentage: request.commissionPercentage,
            groupBy: request.groupBy,
          },
        }
      )

      console.log('[FinancialService] Earnings retrieved:', response.data)

      // Handle wrapped response
      const data = response.data?.data || response.data

      if (!data) {
        throw new Error('No earnings data received')
      }

      return data as GetEarningsResponse
    } catch (error) {
      console.error('[FinancialService] Error fetching earnings:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Get current month earnings
   * GET /api/v1/financial/provider/{providerId}/current-month
   */
  async getCurrentMonthEarnings(
    providerId: string,
    commissionPercentage?: number
  ): Promise<EarningsSummary> {
    try {
      console.log('[FinancialService] Fetching current month earnings for:', providerId)

      const response = await serviceCategoryClient.get<ApiResponse<EarningsSummary>>(
        `${FINANCIAL_BASE}/provider/${providerId}/current-month`,
        {
          params: {
            commissionPercentage,
          },
        }
      )

      const data = response.data?.data || response.data

      if (!data) {
        throw new Error('No current month earnings data received')
      }

      return data as EarningsSummary
    } catch (error) {
      console.error('[FinancialService] Error fetching current month earnings:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Get previous month earnings
   * GET /api/v1/financial/provider/{providerId}/previous-month
   */
  async getPreviousMonthEarnings(
    providerId: string,
    commissionPercentage?: number
  ): Promise<EarningsSummary> {
    try {
      console.log('[FinancialService] Fetching previous month earnings for:', providerId)

      const response = await serviceCategoryClient.get<ApiResponse<EarningsSummary>>(
        `${FINANCIAL_BASE}/provider/${providerId}/previous-month`,
        {
          params: {
            commissionPercentage,
          },
        }
      )

      const data = response.data?.data || response.data

      if (!data) {
        throw new Error('No previous month earnings data received')
      }

      return data as EarningsSummary
    } catch (error) {
      console.error('[FinancialService] Error fetching previous month earnings:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Get provider revenue (total)
   * GET /api/v1/financial/provider/{providerId}/revenue
   */
  async getProviderRevenue(providerId: string): Promise<number> {
    try {
      console.log('[FinancialService] Fetching provider revenue for:', providerId)

      const response = await serviceCategoryClient.get<ApiResponse<{ totalRevenue: number }>>(
        `${FINANCIAL_BASE}/provider/${providerId}/revenue`
      )

      const data = response.data?.data || response.data

      return data?.totalRevenue || 0
    } catch (error) {
      console.error('[FinancialService] Error fetching provider revenue:', error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Payout Operations
  // ============================================

  /**
   * Get provider payouts
   * GET /api/v1/payouts/provider/{providerId}
   */
  async getProviderPayouts(request: GetPayoutsRequest): Promise<PagedResult<Payout>> {
    try {
      console.log('[FinancialService] Fetching payouts:', request)

      const response = await serviceCategoryClient.get<ApiResponse<PagedResult<Payout>>>(
        `${PAYOUT_BASE}/provider/${request.providerId}`,
        {
          params: {
            status: request.status,
            startDate: request.startDate,
            endDate: request.endDate,
            pageNumber: request.page || 1,
            pageSize: request.pageSize || 20,
          },
        }
      )

      const data = response.data?.data || response.data

      if (!data) {
        throw new Error('No payout data received')
      }

      return data as PagedResult<Payout>
    } catch (error) {
      console.error('[FinancialService] Error fetching payouts:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Get pending payouts
   * GET /api/v1/payouts/provider/{providerId}/pending
   */
  async getPendingPayouts(providerId: string): Promise<Payout[]> {
    try {
      console.log('[FinancialService] Fetching pending payouts for:', providerId)

      const response = await serviceCategoryClient.get<ApiResponse<Payout[]>>(
        `${PAYOUT_BASE}/provider/${providerId}/pending`
      )

      const data = response.data?.data || response.data

      return Array.isArray(data) ? data : []
    } catch (error) {
      console.error('[FinancialService] Error fetching pending payouts:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Get payout by ID
   * GET /api/v1/payouts/{payoutId}
   */
  async getPayoutById(payoutId: string): Promise<Payout> {
    try {
      console.log('[FinancialService] Fetching payout:', payoutId)

      const response = await serviceCategoryClient.get<ApiResponse<Payout>>(
        `${PAYOUT_BASE}/${payoutId}`
      )

      const data = response.data?.data || response.data

      if (!data) {
        throw new Error(`Payout ${payoutId} not found`)
      }

      return data as Payout
    } catch (error) {
      console.error(`[FinancialService] Error fetching payout ${payoutId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Create a new payout request
   * POST /api/v1/payouts
   */
  async createPayout(request: CreatePayoutRequest): Promise<Payout> {
    try {
      console.log('[FinancialService] Creating payout:', request)

      const response = await serviceCategoryClient.post<ApiResponse<Payout>>(
        `${PAYOUT_BASE}`,
        request
      )

      console.log('[FinancialService] Payout created:', response.data)

      const data = response.data?.data || response.data

      if (!data) {
        throw new Error('Failed to create payout')
      }

      return data as Payout
    } catch (error) {
      console.error('[FinancialService] Error creating payout:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Execute/process a payout (admin only)
   * POST /api/v1/payouts/{payoutId}/execute
   */
  async executePayout(payoutId: string): Promise<Payout> {
    try {
      console.log('[FinancialService] Executing payout:', payoutId)

      const response = await serviceCategoryClient.post<ApiResponse<Payout>>(
        `${PAYOUT_BASE}/${payoutId}/execute`
      )

      const data = response.data?.data || response.data

      if (!data) {
        throw new Error('Failed to execute payout')
      }

      return data as Payout
    } catch (error) {
      console.error(`[FinancialService] Error executing payout ${payoutId}:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Transaction Operations
  // ============================================

  /**
   * Get transaction history
   * GET /api/v1/transactions/provider/{providerId}
   */
  async getTransactionHistory(
    request: GetTransactionsRequest
  ): Promise<PagedResult<Transaction>> {
    try {
      console.log('[FinancialService] Fetching transactions:', request)

      const response = await serviceCategoryClient.get<ApiResponse<PagedResult<Transaction>>>(
        `/${API_VERSION}/transactions/provider/${request.providerId}`,
        {
          params: {
            type: request.type,
            startDate: request.startDate,
            endDate: request.endDate,
            pageNumber: request.page || 1,
            pageSize: request.pageSize || 20,
            sortBy: request.sortBy,
            sortOrder: request.sortOrder,
          },
        }
      )

      const data = response.data?.data || response.data

      if (!data) {
        throw new Error('No transaction data received')
      }

      return data as PagedResult<Transaction>
    } catch (error) {
      console.error('[FinancialService] Error fetching transactions:', error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Dashboard Operations
  // ============================================

  /**
   * Get financial dashboard data (aggregated)
   * Combines multiple API calls for dashboard view
   */
  async getFinancialDashboard(providerId: string): Promise<FinancialDashboard> {
    try {
      console.log('[FinancialService] Fetching financial dashboard for:', providerId)

      // Fetch all data in parallel
      const [currentMonth, previousMonth, pendingPayouts, recentPayouts, recentTransactions] =
        await Promise.all([
          this.getCurrentMonthEarnings(providerId),
          this.getPreviousMonthEarnings(providerId),
          this.getPendingPayouts(providerId),
          this.getProviderPayouts({ providerId, page: 1, pageSize: 5 }),
          this.getTransactionHistory({ providerId, page: 1, pageSize: 10 }),
        ])

      // Calculate pending payout amount
      const pendingPayoutAmount = pendingPayouts.reduce((sum, payout) => sum + payout.amount, 0)

      // Find next payout date (earliest pending or processing payout)
      const nextPayoutDate = pendingPayouts
        .filter((p) => p.status === 'Pending' || p.status === 'Processing')
        .sort((a, b) => new Date(a.requestedAt).getTime() - new Date(b.requestedAt).getTime())[0]
        ?.requestedAt

      return {
        currentMonthEarnings: currentMonth,
        previousMonthEarnings: previousMonth,
        pendingPayoutAmount,
        nextPayoutDate,
        recentTransactions: recentTransactions.items,
        recentPayouts: recentPayouts.items,
      }
    } catch (error) {
      console.error('[FinancialService] Error fetching financial dashboard:', error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Helper Methods
  // ============================================

  /**
   * Centralized error handling
   */
  private handleError(error: unknown): Error {
    if (error instanceof Error) {
      // Check if this is already a validation error from HTTP client
      if ((error as any).isValidationError && (error as any).validationErrors) {
        return error
      }
      return error
    }

    return new Error('خطای غیرمنتظره در عملیات مالی')
  }
}

// Export singleton instance
export const financialService = new FinancialService()
export default financialService
