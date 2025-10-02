// src/modules/provider/services/provider.service.ts

import httpClient from '@/core/api/client/http-client'
import type {
  Provider,
  ProviderSummary,
  RegisterProviderRequest,
  UpdateProviderRequest,
  ActivateProviderRequest,
  DeactivateProviderRequest,
  ProviderStatistics,
  ProviderStatus,
  ProviderResponse,
  ProviderType,
  ProviderSearchFilters,
} from '../types/provider.types'
import type { PagedResult } from '@/core/types/common.types'
/**
 * Provider Service - API communication layer
 * Handles all HTTP requests to the Provider API endpoints
 */

const API_VERSION = 'v1'
const API_BASE = `/${API_VERSION}/providers`

class ProviderService {
  // ============================================
  // Public Endpoints (No Auth Required)
  // ============================================

  /**
   * Search providers with advanced filtering and pagination
   */
  async searchProviders(filters: ProviderSearchFilters): Promise<PagedResult<ProviderSummary>> {
    const { data } = await httpClient.get<PagedResult<ProviderSummary>>(`${API_BASE}/search`, {
      params: { ...filters },
    })
    return data
  }

  /**
   * Get provider by ID with optional related data
   */
  async getProviderById(
    id: string,
    includeServices = false,
    includeStaff = false,
  ): Promise<Provider> {
    try {
      const response = await httpClient.get<ProviderResponse>(`${API_BASE}/${id}`, {
        params: {
          includeServices,
          includeStaff,
        },
      })
      return this.mapProviderResponse(response.data)
    } catch (error) {
      console.error(`Error fetching provider ${id}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Get featured providers (public homepage)
   */
  async getFeaturedProviders(categoryFilter?: string, limit = 20): Promise<ProviderSummary[]> {
    const response = await httpClient.get<ProviderSummary[]>(`${API_BASE}/featured`, {
      params: { categoryFilter, limit },
    })
    return response.data.map(this.mapProviderSummaryResponse)
  }

  /**
   * Get providers by location (geo search)
   */
  async getProvidersByLocation(
    latitude: number,
    longitude: number,
    radiusKm: number,
    limit = 50,
  ): Promise<ProviderSummary[]> {
    try {
      const response = await httpClient.get<ProviderSummary[]>(`${API_BASE}/by-location`, {
        params: { latitude, longitude, radiusKm, limit },
      })
      return response.data.map(this.mapProviderSummaryResponse)
    } catch (error) {
      console.error('Error fetching providers by location:', error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Provider Management (Auth Required)
  // ============================================

  /**
   * Register a new provider
   */
  async registerProvider(data: RegisterProviderRequest): Promise<Provider> {
    try {
      const response = await httpClient.post<ProviderResponse>(`${API_BASE}/register`, data)
      return this.mapProviderResponse(response.data)
    } catch (error) {
      console.error('Error registering provider:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Update provider profile
   */
  async updateProvider(id: string, data: UpdateProviderRequest): Promise<Provider> {
    try {
      const response = await httpClient.put<ProviderResponse>(`${API_BASE}/${id}`, data)
      return this.mapProviderResponse(response.data)
    } catch (error) {
      console.error(`Error updating provider ${id}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Activate provider account
   */
  async activateProvider(id: string, data?: ActivateProviderRequest): Promise<void> {
    try {
      await httpClient.post(`${API_BASE}/${id}/activate`, data)
    } catch (error) {
      console.error(`Error activating provider ${id}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Deactivate provider account
   */
  async deactivateProvider(id: string, data: DeactivateProviderRequest): Promise<void> {
    try {
      await httpClient.post(`${API_BASE}/${id}/deactivate`, data)
    } catch (error) {
      console.error(`Error deactivating provider ${id}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Suspend provider (temporary)
   */
  async suspendProvider(id: string, reason: string, notes?: string): Promise<void> {
    try {
      await httpClient.post(`${API_BASE}/${id}/suspend`, { reason, notes })
    } catch (error) {
      console.error(`Error suspending provider ${id}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Reactivate suspended provider
   */
  async reactivateProvider(id: string, notes?: string): Promise<void> {
    try {
      await httpClient.post(`${API_BASE}/${id}/reactivate`, { notes })
    } catch (error) {
      console.error(`Error reactivating provider ${id}:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Statistics & Analytics
  // ============================================

  /**
   * Get provider statistics
   */
  async getProviderStatistics(id: string): Promise<ProviderStatistics> {
    try {
      const response = await httpClient.get<ProviderStatistics>(`${API_BASE}/${id}/statistics`)
      return response.data
    } catch (error) {
      console.error(`Error fetching statistics for provider ${id}:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Admin Endpoints
  // ============================================

  /**
   * Get providers by status (admin only)
   */
  async getProvidersByStatus(status: ProviderStatus, maxResults = 100): Promise<ProviderSummary[]> {
    try {
      const response = await httpClient.get<ProviderSummary[]>(`${API_BASE}/by-status/${status}`, {
        params: { maxResults },
      })
      return response.data.map(this.mapProviderSummaryResponse)
    } catch (error) {
      console.error(`Error fetching providers by status ${status}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Verify provider (admin only)
   */
  async verifyProvider(id: string, notes?: string): Promise<void> {
    try {
      await httpClient.post(`${API_BASE}/${id}/verify`, { notes })
    } catch (error) {
      console.error(`Error verifying provider ${id}:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Helper Methods
  // ============================================

  /**
   * Map API response to domain model
   */
  private mapProviderResponse(response: ProviderResponse): Provider {
    return {
      id: response.id,
      ownerId: response.ownerId,
      profile: {
        businessName: response.businessName,
        description: response.description,
        logoUrl: response.logoUrl,
        coverImageUrl: response.coverImageUrl,
        websiteUrl: response.websiteUrl,
      },
      status: response.status as ProviderStatus,
      type: response.type as ProviderType,
      contactInfo: {
        email: response.email,
        primaryPhone: response.primaryPhone,
        secondaryPhone: response.secondaryPhone,
      },
      address: {
        addressLine1: response.addressLine1,
        addressLine2: response.addressLine2,
        city: response.city,
        state: response.state,
        postalCode: response.postalCode,
        country: response.country,
        latitude: response.latitude,
        longitude: response.longitude,
      },
      requiresApproval: response.requiresApproval,
      allowOnlineBooking: response.allowOnlineBooking,
      offersMobileServices: response.offersMobileServices,
      tags: response.tags || [],
      services: response.services,
      staff: response.staff,
      businessHours: response.businessHours,
      registeredAt: response.registeredAt,
      activatedAt: response.activatedAt,
      verifiedAt: response.verifiedAt,
      lastActiveAt: response.lastActiveAt,
      createdAt: response.registeredAt,
      lastModifiedAt: response.lastActiveAt,
    }
  }

  /**
   * Map summary response
   */
  private mapProviderSummaryResponse(response: ProviderSummary): ProviderSummary {
    return {
      id: response.id,
      businessName: response.businessName,
      description: response.description,
      type: response.type,
      status: response.status,
      logoUrl: response.logoUrl,
      city: response.city,
      state: response.state,
      country: response.country,
      allowOnlineBooking: response.allowOnlineBooking,
      offersMobileServices: response.offersMobileServices,
      tags: response.tags || [],
      registeredAt: response.registeredAt,
      lastActiveAt: response.lastActiveAt,
    }
  }

  /**
   * Centralized error handling
   */
  private handleError(error: unknown): Error {
    if (error && typeof error === 'object' && 'response' in error) {
      const axiosError = error as { response?: { data?: { message?: string } } }
      if (axiosError.response?.data?.message) {
        return new Error(axiosError.response.data.message)
      }
    }

    if (error instanceof Error) {
      return error
    }

    return new Error('An unexpected error occurred')
  }
}

// Export singleton instance
export const providerService = new ProviderService()
