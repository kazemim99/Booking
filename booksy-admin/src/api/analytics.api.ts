import apiClient from '../utils/axios'
import type { DashboardStats } from '../types'

export interface DateRange {
  startDate: string
  endDate: string
}

export const analyticsApi = {
  getDashboardStats: async (): Promise<DashboardStats> => {
    const response = await apiClient.get<DashboardStats>('/analytics/dashboard')
    return response.data
  },

  getUserGrowth: async (range: DateRange) => {
    const response = await apiClient.get('/analytics/user-growth', { params: range })
    return response.data
  },

  getBookingTrends: async (range: DateRange) => {
    const response = await apiClient.get('/analytics/booking-trends', { params: range })
    return response.data
  },

  getRevenueTrends: async (range: DateRange) => {
    const response = await apiClient.get('/analytics/revenue-trends', { params: range })
    return response.data
  },

  getTopProviders: async (limit: number = 10) => {
    const response = await apiClient.get('/analytics/top-providers', { params: { limit } })
    return response.data
  },

  getTopServices: async (limit: number = 10) => {
    const response = await apiClient.get('/analytics/top-services', { params: { limit } })
    return response.data
  },

  getCategoryDistribution: async () => {
    const response = await apiClient.get('/analytics/category-distribution')
    return response.data
  },

  getPaymentStats: async (range: DateRange) => {
    const response = await apiClient.get('/analytics/payment-stats', { params: range })
    return response.data
  },
}
