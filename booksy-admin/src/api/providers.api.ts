import apiClient from '../utils/axios'
import type { Provider, PaginatedResponse } from '../types'

export interface ProvidersQuery {
  pageNumber?: number
  pageSize?: number
  search?: string
  status?: 'Pending' | 'Approved' | 'Rejected' | 'Suspended'
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
}

export interface ApproveProviderRequest {
  notes?: string
}

export interface RejectProviderRequest {
  reason: string
}

export const providersApi = {
  getProviders: async (query: ProvidersQuery = {}): Promise<PaginatedResponse<Provider>> => {
    const response = await apiClient.get<{ data: PaginatedResponse<Provider> }>('/Providers/search', { params: query })
    return response.data.data
  },

  getProviderById: async (id: string): Promise<Provider> => {
    const response = await apiClient.get<Provider>(`/Providers/${id}`)
    return response.data
  },

  approveProvider: async (id: string, data?: ApproveProviderRequest): Promise<Provider> => {
    const response = await apiClient.post<Provider>(`/Providers/${id}/approve`, data)
    return response.data
  },

  rejectProvider: async (id: string, data: RejectProviderRequest): Promise<Provider> => {
    const response = await apiClient.post<Provider>(`/Providers/${id}/reject`, data)
    return response.data
  },

  suspendProvider: async (id: string, reason: string): Promise<Provider> => {
    const response = await apiClient.post<Provider>(`/Providers/${id}/suspend`, { reason })
    return response.data
  },

  reactivateProvider: async (id: string): Promise<Provider> => {
    const response = await apiClient.post<Provider>(`/Providers/${id}/reactivate`)
    return response.data
  },

  updateProvider: async (id: string, data: Partial<Provider>): Promise<Provider> => {
    const response = await apiClient.put<Provider>(`/Providers/${id}`, data)
    return response.data
  },

  deleteProvider: async (id: string): Promise<void> => {
    await apiClient.delete(`/Providers/${id}`)
  },

  getProviderStats: async (id: string) => {
    const response = await apiClient.get(`/Providers/${id}/stats`)
    return response.data
  },
}
