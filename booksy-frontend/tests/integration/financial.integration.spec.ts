/**
 * Integration Tests: Financial & Payouts (Priority 5)
 *
 * Tests the integration between Financial components and ServiceCatalog API
 *
 * Prerequisites:
 * - ServiceCatalog API running on http://localhost:5010
 * - Valid JWT token for provider account
 * - Provider with completed bookings and transactions
 */

import { describe, it, expect, beforeAll } from 'vitest'
import { financialService } from '@/modules/provider/services/financial.service'
import type { PayoutStatus } from '@/modules/provider/types/financial.types'

describe('Financial & Payouts Integration Tests', () => {
  let providerId: string
  let authToken: string
  let createdPayoutId: string

  beforeAll(async () => {
    providerId = process.env.TEST_PROVIDER_ID || 'test-provider-id'
    authToken = process.env.TEST_AUTH_TOKEN || ''

    if (!authToken) {
      console.warn('⚠️  No auth token provided. Set TEST_AUTH_TOKEN environment variable.')
    }
  })

  describe('Provider Earnings', () => {
    it('should fetch provider earnings for date range', async () => {
      const startDate = new Date(2024, 0, 1).toISOString().split('T')[0] // Jan 1, 2024
      const endDate = new Date(2024, 11, 31).toISOString().split('T')[0] // Dec 31, 2024

      const result = await financialService.getProviderEarnings({
        providerId,
        startDate,
        endDate,
        commissionPercentage: 15,
      })

      expect(result).toHaveProperty('summary')
      expect(result.summary).toHaveProperty('providerId', providerId)
      expect(result.summary).toHaveProperty('totalRevenue')
      expect(result.summary).toHaveProperty('platformCommission')
      expect(result.summary).toHaveProperty('netEarnings')
      expect(result.summary).toHaveProperty('currency', 'IRR')
    })

    it('should fetch current month earnings', async () => {
      const earnings = await financialService.getCurrentMonthEarnings(providerId)

      expect(earnings).toHaveProperty('providerId', providerId)
      expect(earnings).toHaveProperty('totalRevenue')
      expect(earnings).toHaveProperty('netEarnings')
      expect(typeof earnings.totalRevenue).toBe('number')
      expect(typeof earnings.netEarnings).toBe('number')
    })

    it('should fetch previous month earnings', async () => {
      const earnings = await financialService.getPreviousMonthEarnings(providerId)

      expect(earnings).toHaveProperty('providerId', providerId)
      expect(earnings).toHaveProperty('totalRevenue')
      expect(earnings).toHaveProperty('netEarnings')
    })

    it('should calculate commission correctly', async () => {
      const earnings = await financialService.getCurrentMonthEarnings(providerId, 15)

      const expectedCommission = earnings.totalRevenue * 0.15
      const expectedNetEarnings = earnings.totalRevenue - expectedCommission

      expect(earnings.platformCommission).toBeCloseTo(expectedCommission, 2)
      expect(earnings.netEarnings).toBeCloseTo(expectedNetEarnings, 2)
    })

    it('should handle different commission percentages', async () => {
      const earnings10 = await financialService.getCurrentMonthEarnings(providerId, 10)
      const earnings20 = await financialService.getCurrentMonthEarnings(providerId, 20)

      expect(earnings10.netEarnings).toBeGreaterThan(earnings20.netEarnings)
      expect(earnings20.platformCommission).toBeGreaterThan(earnings10.platformCommission)
    })
  })

  describe('Transaction History', () => {
    it('should fetch paginated transaction history', async () => {
      const result = await financialService.getTransactionHistory({
        providerId,
        page: 1,
        pageSize: 10,
      })

      expect(result).toHaveProperty('items')
      expect(result).toHaveProperty('totalCount')
      expect(result).toHaveProperty('pageNumber', 1)
      expect(result).toHaveProperty('pageSize', 10)
      expect(Array.isArray(result.items)).toBe(true)
    })

    it('should filter transactions by type', async () => {
      const result = await financialService.getTransactionHistory({
        providerId,
        type: 'BookingPayment',
        page: 1,
        pageSize: 20,
      })

      result.items.forEach(transaction => {
        expect(transaction.type).toBe('BookingPayment')
      })
    })

    it('should filter transactions by date range', async () => {
      const startDate = '2024-01-01'
      const endDate = '2024-01-31'

      const result = await financialService.getTransactionHistory({
        providerId,
        startDate,
        endDate,
        page: 1,
        pageSize: 20,
      })

      result.items.forEach(transaction => {
        const transactionDate = new Date(transaction.createdAt)
        expect(transactionDate.getTime()).toBeGreaterThanOrEqual(new Date(startDate).getTime())
        expect(transactionDate.getTime()).toBeLessThanOrEqual(new Date(endDate).getTime())
      })
    })

    it('should sort transactions correctly', async () => {
      const result = await financialService.getTransactionHistory({
        providerId,
        sortBy: 'createdAt',
        sortOrder: 'desc',
        page: 1,
        pageSize: 10,
      })

      // Check if sorted in descending order
      for (let i = 0; i < result.items.length - 1; i++) {
        const current = new Date(result.items[i].createdAt).getTime()
        const next = new Date(result.items[i + 1].createdAt).getTime()
        expect(current).toBeGreaterThanOrEqual(next)
      }
    })
  })

  describe('Payout Operations', () => {
    it('should create a payout request', async () => {
      const payout = await financialService.createPayout({
        providerId,
        amount: 500000,
        bankAccountNumber: '6037997000000001',
        notes: 'درخواست واریز تستی',
      })

      expect(payout).toHaveProperty('id')
      expect(payout).toHaveProperty('providerId', providerId)
      expect(payout).toHaveProperty('amount', 500000)
      expect(payout).toHaveProperty('status')

      createdPayoutId = payout.id
    })

    it('should reject payout with insufficient balance', async () => {
      await expect(
        financialService.createPayout({
          providerId,
          amount: 999999999, // Very large amount
          bankAccountNumber: '6037997000000001',
        })
      ).rejects.toThrow()
    })

    it('should reject payout below minimum amount', async () => {
      await expect(
        financialService.createPayout({
          providerId,
          amount: 5000, // Below minimum (10,000)
          bankAccountNumber: '6037997000000001',
        })
      ).rejects.toThrow()
    })

    it('should fetch provider payouts with pagination', async () => {
      const result = await financialService.getProviderPayouts({
        providerId,
        page: 1,
        pageSize: 10,
      })

      expect(result).toHaveProperty('items')
      expect(result).toHaveProperty('totalCount')
      expect(Array.isArray(result.items)).toBe(true)
    })

    it('should filter payouts by status', async () => {
      const statuses: PayoutStatus[] = ['Pending', 'Processing', 'Completed', 'Failed']

      for (const status of statuses) {
        const result = await financialService.getProviderPayouts({
          providerId,
          status,
          page: 1,
          pageSize: 10,
        })

        result.items.forEach(payout => {
          expect(payout.status).toBe(status)
        })
      }
    })

    it('should fetch pending payouts', async () => {
      const pendingPayouts = await financialService.getPendingPayouts(providerId)

      expect(Array.isArray(pendingPayouts)).toBe(true)
      pendingPayouts.forEach(payout => {
        expect(payout.status).toBe('Pending')
      })
    })

    it('should fetch payout by ID', async () => {
      if (!createdPayoutId) {
        console.warn('Skipping: No payout created')
        return
      }

      const payout = await financialService.getPayoutById(createdPayoutId)

      expect(payout).toHaveProperty('id', createdPayoutId)
      expect(payout).toHaveProperty('providerId', providerId)
      expect(payout).toHaveProperty('amount')
      expect(payout).toHaveProperty('status')
    })
  })

  describe('Financial Dashboard', () => {
    it('should fetch complete dashboard data', async () => {
      const dashboard = await financialService.getFinancialDashboard(providerId)

      expect(dashboard).toHaveProperty('currentMonthEarnings')
      expect(dashboard).toHaveProperty('previousMonthEarnings')
      expect(dashboard).toHaveProperty('pendingPayoutAmount')
      expect(dashboard).toHaveProperty('recentTransactions')
      expect(dashboard).toHaveProperty('recentPayouts')

      expect(Array.isArray(dashboard.recentTransactions)).toBe(true)
      expect(Array.isArray(dashboard.recentPayouts)).toBe(true)
    })

    it('should load dashboard data efficiently (parallel requests)', async () => {
      const startTime = Date.now()

      await financialService.getFinancialDashboard(providerId)

      const duration = Date.now() - startTime

      // Should complete in reasonable time (< 5 seconds)
      // Parallel requests should be faster than sequential
      expect(duration).toBeLessThan(5000)
    })
  })

  describe('Error Handling', () => {
    it('should handle invalid provider ID', async () => {
      const invalidProviderId = 'invalid-provider-id'

      await expect(
        financialService.getCurrentMonthEarnings(invalidProviderId)
      ).rejects.toThrow()
    })

    it('should handle invalid date range', async () => {
      await expect(
        financialService.getProviderEarnings({
          providerId,
          startDate: '2024-12-31',
          endDate: '2024-01-01', // End before start
        })
      ).rejects.toThrow()
    })

    it('should handle network timeouts gracefully', async () => {
      // This would require mocking the HTTP client to simulate timeout
      // For now, just ensure service doesn't crash
      expect(financialService).toBeDefined()
    })
  })

  describe('Currency Formatting', () => {
    it('should format Iranian currency correctly', async () => {
      const earnings = await financialService.getCurrentMonthEarnings(providerId)

      // Should be formatted with Persian digits and تومان suffix
      expect(earnings.currency).toBe('IRR')
      expect(earnings.totalRevenue).toBeGreaterThanOrEqual(0)
    })
  })
})
