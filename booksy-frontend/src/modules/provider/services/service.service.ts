// src/modules/provider/services/service.service.ts

import { serviceCategoryClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'
import type {
  Service,
  ServiceSummary,
  CreateServiceRequest,
  UpdateServiceRequest,
  ServiceFilters,
  ServiceStatus,
} from '../types/service.types'
import type { PagedResult } from '@/core/types/common.types'

const API_VERSION = 'v1'
const API_BASE = `/${API_VERSION}/services`

// Backend response type (ServiceDetailResponse from C#)
interface ServiceDetailResponse {
  id: string
  name: string
  durationMinutes: number
  price: number
}

// Helper function to map backend response to frontend Service type
function mapToService(
  backendResponse: ServiceDetailResponse,
  providerId: string,
  requestData?: CreateServiceRequest | UpdateServiceRequest
): Service {
  return {
    id: backendResponse.id,
    providerId: providerId,
    name: backendResponse.name || requestData?.serviceName || '',
    description: requestData?.description || '',
    category: (requestData?.category as any) || 'Other',
    type: 'Standard' as any,
    basePrice: backendResponse.price || 0,
    currency: requestData?.currency || 'USD',
    duration: backendResponse.durationMinutes || 0,
    preparationTime: 0,
    bufferTime: 0,
    status: 'Active' as ServiceStatus,
    allowOnlineBooking: true,
    availableAtLocation: true,
    availableAsMobile: requestData?.isMobileService || false,
    maxAdvanceBookingDays: 30,
    minAdvanceBookingHours: 1,
    requiresDeposit: false,
    depositPercentage: 0,
    qualifiedStaffIds: [],
    tags: [],
    displayOrder: 0,
    createdAt: new Date().toISOString(),
    lastModifiedAt: new Date().toISOString(),
  } as Service
}

class ServiceService {
  // ============================================
  // Service CRUD Operations
  // ============================================

  /**
   * Get all services for a provider
   * GET /api/v1/services/provider/{providerId}
   * Returns paginated response
   */
  async getServicesByProvider(providerId: string): Promise<Service[]> {
    try {
      console.log(`[ServiceService] Fetching services for provider: ${providerId}`)

      // Backend returns: { success, statusCode, message, data: { items: [...], pageNumber, ... } }
      const response = await serviceCategoryClient.get<any>(
        `${API_BASE}/provider/${providerId}`,
        {
          params: {
            pageNumber: 1,
            pageSize: 100 // Get all services (adjust if needed)
          }
        }
      )

      console.log(`[ServiceService] Services response:`, response.data)

      // Extract items from paginated response
      // Handle both wrapped { data: { items: [] } } and direct { items: [] } formats
      let services: Service[]

      if (response.data?.data?.items) {
        // Wrapped format: { success, data: { items: [] } }
        services = response.data.data.items
      } else if (response.data?.items) {
        // Direct paginated format: { items: [] }
        services = response.data.items
      } else if (Array.isArray(response.data)) {
        // Direct array format: [...]
        services = response.data
      } else {
        console.warn('[ServiceService] Unexpected response format:', response.data)
        services = []
      }

      console.log(`[ServiceService] ${services.length} services retrieved`)
      return services
    } catch (error) {
      console.error(`[ServiceService] Error fetching services for provider ${providerId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Get service by ID
   */
  async getServiceById(id: string): Promise<Service> {
    try {
      console.log(`[ServiceService] Fetching service: ${id}`)
      const response = await serviceCategoryClient.get<Service>(`${API_BASE}/${id}`)
      console.log(`[ServiceService] Service retrieved:`, response.data)
      return response.data!
    } catch (error) {
      console.error(`[ServiceService] Error fetching service ${id}:`, error)
      throw this.handleError(error)
    }
  }


  async createService(data: CreateServiceRequest): Promise<Service> {
    try {
      const { providerId, ...requestBody } = data

      console.log(`[ServiceService] Creating service for provider ${providerId}:`, requestBody)

      // Backend expects: POST /api/v1/providers/{providerId}/services
      // with serviceName, description, durationHours, durationMinutes, price, currency, category
      const response = await serviceCategoryClient.post<ApiResponse<ServiceDetailResponse>>(
        `/v1/services/${providerId}`,
        requestBody
      )

      console.log(`[ServiceService] Service created - Full response:`, response.data)

      // Backend wraps response in { success, data, message, metadata }
      // Extract the actual service from response.data.data
      if (!response.data) {
        throw new Error('No response data received from server')
      }

      // Check if response is wrapped (has success/data fields)
      const isWrapped = 'success' in response.data && 'data' in response.data
      const serviceData = isWrapped ? response.data.data : response.data

      if (!serviceData) {
        console.error('[ServiceService] Invalid response structure:', response.data)
        throw new Error('Invalid service data received from server')
      }

      console.log(`[ServiceService] Extracted service data:`, serviceData)

      // Map minimal backend response to full Service object
      const mappedService = mapToService(serviceData, providerId, data)
      console.log(`[ServiceService] Mapped service:`, mappedService)
      return mappedService
    } catch (error) {
      console.error(`[ServiceService] Error creating service:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Update an existing service
   */
  async updateService(id: string, data: UpdateServiceRequest): Promise<Service> {
    try {
      const { providerId, ...requestBody } = data

      console.log(`[ServiceService] Updating service ${id} for provider ${providerId}:`, requestBody)

      // Backend expects: PUT /api/v1/services/{providerId}/{serviceId}
      // with serviceName, description, durationHours, durationMinutes, price, currency, category, isMobileService
      const response = await serviceCategoryClient.put<ApiResponse<ServiceDetailResponse>>(
        `${API_BASE}/${providerId}/${id}`,
        requestBody
      )
      console.log(`[ServiceService] Service updated - Full response:`, response.data)

      // Backend wraps response in { success, data, message, metadata }
      // Extract the actual service from response.data.data
      if (!response.data) {
        throw new Error('No response data received from server')
      }

      // Check if response is wrapped (has success/data fields)
      const isWrapped = 'success' in response.data && 'data' in response.data
      const serviceData = isWrapped ? response.data.data : response.data

      if (!serviceData) {
        console.error('[ServiceService] Invalid response structure:', response.data)
        throw new Error('Invalid service data received from server')
      }

      console.log(`[ServiceService] Extracted service data:`, serviceData)

      // Map minimal backend response to full Service object
      const mappedService = mapToService(serviceData, providerId, data)
      console.log(`[ServiceService] Mapped service:`, mappedService)
      return mappedService
    } catch (error) {
      console.error(`[ServiceService] Error updating service ${id}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Delete a service
   */
  async deleteService(id: string, providerId: string): Promise<void> {
    try {
      console.log(`[ServiceService] Deleting service ${id} for provider ${providerId}`)

      // Backend expects: DELETE /api/v1/services/{providerId}/{serviceId}
      await serviceCategoryClient.delete(`${API_BASE}/${providerId}/${id}`)
      console.log(`[ServiceService] Service deleted successfully`)
    } catch (error) {
      console.error(`[ServiceService] Error deleting service ${id}:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Service Status Management
  // ============================================

  /**
   * Activate a service
   */
  async activateService(id: string): Promise<Service> {
    try {
      console.log(`[ServiceService] Activating service: ${id}`)
      const response = await serviceCategoryClient.post<Service>(`${API_BASE}/${id}/activate`)
      console.log(`[ServiceService] Service activated successfully`)
      return response.data!
    } catch (error) {
      console.error(`[ServiceService] Error activating service ${id}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Deactivate a service
   */
  async deactivateService(id: string): Promise<Service> {
    try {
      console.log(`[ServiceService] Deactivating service: ${id}`)
      const response = await serviceCategoryClient.post<Service>(`${API_BASE}/${id}/deactivate`)
      console.log(`[ServiceService] Service deactivated successfully`)
      return response.data!
    } catch (error) {
      console.error(`[ServiceService] Error deactivating service ${id}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Archive a service
   */
  async archiveService(id: string): Promise<Service> {
    try {
      console.log(`[ServiceService] Archiving service: ${id}`)
      const response = await serviceCategoryClient.post<Service>(`${API_BASE}/${id}/archive`)
      console.log(`[ServiceService] Service archived successfully`)
      return response.data!
    } catch (error) {
      console.error(`[ServiceService] Error archiving service ${id}:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Search & Filter
  // ============================================

  /**
   * Search services with filters
   */
  async searchServices(providerId: string, filters: ServiceFilters): Promise<ServiceSummary[]> {
    try {
      console.log(`[ServiceService] Searching services for provider ${providerId}:`, filters)
      const response = await serviceCategoryClient.get<ServiceSummary[]>(`${API_BASE}/provider/${providerId}/search`, {
        params: filters,
      })
      return response.data || []
    } catch (error) {
      console.error(`[ServiceService] Error searching services:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Bulk Operations
  // ============================================

  /**
   * Update display order for multiple services
   */
  async updateServiceOrder(providerId: string, serviceIds: string[]): Promise<void> {
    try {
      console.log(`[ServiceService] Updating service order for provider ${providerId}:`, serviceIds)
      await serviceCategoryClient.put(`${API_BASE}/provider/${providerId}/reorder`, { serviceIds })
      console.log(`[ServiceService] Service order updated successfully`)
    } catch (error) {
      console.error(`[ServiceService] Error updating service order:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Bulk activate services
   */
  async bulkActivateServices(serviceIds: string[]): Promise<void> {
    try {
      console.log(`[ServiceService] Bulk activating services:`, serviceIds)
      await serviceCategoryClient.post(`${API_BASE}/bulk/activate`, { serviceIds })
      console.log(`[ServiceService] Services activated successfully`)
    } catch (error) {
      console.error(`[ServiceService] Error bulk activating services:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Bulk deactivate services
   */
  async bulkDeactivateServices(serviceIds: string[]): Promise<void> {
    try {
      console.log(`[ServiceService] Bulk deactivating services:`, serviceIds)
      await serviceCategoryClient.post(`${API_BASE}/bulk/deactivate`, { serviceIds })
      console.log(`[ServiceService] Services deactivated successfully`)
    } catch (error) {
      console.error(`[ServiceService] Error bulk deactivating services:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Bulk delete services
   */
  async bulkDeleteServices(serviceIds: string[]): Promise<void> {
    try {
      console.log(`[ServiceService] Bulk deleting services:`, serviceIds)
      await serviceCategoryClient.post(`${API_BASE}/bulk/delete`, { serviceIds })
      console.log(`[ServiceService] Services deleted successfully`)
    } catch (error) {
      console.error(`[ServiceService] Error bulk deleting services:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Helper Methods
  // ============================================

  /**
   * Centralized error handling with support for validation errors
   */
  private handleError(error: unknown): Error {
    // If already an Error with validation info, return as-is
    if (error instanceof Error) {
      // Check if this is already a validation error from HTTP client
      if ((error as any).isValidationError && (error as any).validationErrors) {
        return error
      }
      return error
    }

    return new Error('An unexpected error occurred')
  }
}

// Export singleton instance
export const serviceService = new ServiceService()
