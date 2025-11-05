// src/modules/provider/services/staff.service.ts

import { serviceCategoryClient } from '@/core/api/client/http-client'
import type {
  Staff,
  CreateStaffRequest,
  UpdateStaffRequest,
} from '../types/staff.types'

const API_VERSION = 'v1'
const PROVIDERS_BASE = `/${API_VERSION}/providers`

class StaffService {
  // ============================================
  // Staff CRUD Operations
  // ============================================

  /**
   * Get all staff for a provider
   * Backend: GET /api/v1/providers/{id}/staff?activeOnly={boolean}
   */
  async getStaffByProvider(providerId: string, activeOnly = false): Promise<Staff[]> {
    try {
      console.log(`[StaffService] Fetching staff for provider: ${providerId}`)
      const response = await serviceCategoryClient.get<Staff[]>(
        `${PROVIDERS_BASE}/${providerId}/staff`,
        { params: { activeOnly } }
      )
      console.log(`[StaffService] Staff retrieved:`, response.data)
      return response.data || []
    } catch (error) {
      console.error(`[StaffService] Error fetching staff for provider ${providerId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Create new staff member
   * Backend: POST /api/v1/providers/{id}/staff
   */
  async createStaff(providerId: string, data: CreateStaffRequest): Promise<Staff> {
    try {
      console.log(`[StaffService] Creating staff for provider ${providerId}:`, data)
      const response = await serviceCategoryClient.post<Staff>(
        `${PROVIDERS_BASE}/${providerId}/staff`,
        data
      )
      console.log(`[StaffService] Staff created:`, response.data)
      return response.data!
    } catch (error) {
      console.error(`[StaffService] Error creating staff:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Update staff member
   * Backend: PUT /api/v1/providers/{id}/staff/{staffId}
   */
  async updateStaff(providerId: string, staffId: string, data: UpdateStaffRequest): Promise<Staff> {
    try {
      console.log(`[StaffService] Updating staff ${staffId} for provider ${providerId}:`, data)
      const response = await serviceCategoryClient.put<Staff>(
        `${PROVIDERS_BASE}/${providerId}/staff/${staffId}`,
        data
      )
      console.log(`[StaffService] Staff updated:`, response.data)
      return response.data!
    } catch (error) {
      console.error(`[StaffService] Error updating staff ${staffId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Remove/Deactivate staff member
   * Backend: DELETE /api/v1/providers/{id}/staff/{staffId}
   */
  async deleteStaff(providerId: string, staffId: string): Promise<void> {
    try {
      console.log(`[StaffService] Removing staff ${staffId} from provider ${providerId}`)
      await serviceCategoryClient.delete(`${PROVIDERS_BASE}/${providerId}/staff/${staffId}`)
      console.log(`[StaffService] Staff removed successfully`)
    } catch (error) {
      console.error(`[StaffService] Error removing staff ${staffId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Upload staff profile photo
   * Backend: POST /api/v1/providers/{id}/staff/{staffId}/photo
   */
  async uploadStaffPhoto(
    providerId: string,
    staffId: string,
    file: File,
    onUploadProgress?: (progressEvent: any) => void
  ): Promise<{ imageUrl: string; thumbnailUrl: string }> {
    try {
      console.log(`[StaffService] Uploading photo for staff ${staffId}`)
      const formData = new FormData()
      formData.append('file', file)

      const response = await serviceCategoryClient.post<{ imageUrl: string; thumbnailUrl: string }>(
        `${PROVIDERS_BASE}/${providerId}/staff/${staffId}/photo`,
        formData,
        {
          headers: {
            'Content-Type': 'multipart/form-data'
          },
          onUploadProgress
        }
      )

      console.log(`[StaffService] Photo uploaded successfully:`, response.data)
      return response.data!
    } catch (error) {
      console.error(`[StaffService] Error uploading staff photo:`, error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Error Handling
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

    return new Error('An unknown error occurred')
  }
}

// Export singleton instance
export const staffService = new StaffService()
export default staffService
