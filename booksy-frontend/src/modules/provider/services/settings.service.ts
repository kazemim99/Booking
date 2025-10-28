// src/modules/provider/services/settings.service.ts

import { serviceCategoryClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'
import type {
  ProviderSettings,
  UpdateBookingPreferencesRequest,
  UpdateNotificationSettingsRequest,
  UpdateBusinessPoliciesRequest,
  UpdateOperatingPreferencesRequest,
  UpdateIntegrationSettingsRequest,
  UpdateAccountSecurityRequest,
  SettingsUpdateResult,
} from '../types/settings.types'

const API_VERSION = 'v1'
const API_BASE = `/${API_VERSION}/provider-settings`

class SettingsService {
  // ============================================
  // Settings CRUD Operations
  // ============================================

  /**
   * Get provider settings
   * GET /api/v1/provider-settings/{providerId}
   */
  async getProviderSettings(providerId: string): Promise<ProviderSettings> {
    try {
      console.log(`[SettingsService] Fetching settings for provider: ${providerId}`)

      const response = await serviceCategoryClient.get<ApiResponse<ProviderSettings>>(
        `${API_BASE}/${providerId}`
      )

      if (!response.data?.data) {
        throw new Error('No settings data returned from API')
      }

      return response.data.data
    } catch (error: any) {
      console.error('[SettingsService] Error fetching provider settings:', error)
      throw new Error(error.response?.data?.message || 'Failed to fetch provider settings')
    }
  }

  // ============================================
  // Update Settings Sections
  // ============================================

  /**
   * Update booking preferences
   * PUT /api/v1/provider-settings/{providerId}/booking-preferences
   */
  async updateBookingPreferences(
    data: UpdateBookingPreferencesRequest
  ): Promise<SettingsUpdateResult> {
    try {
      console.log(`[SettingsService] Updating booking preferences for provider: ${data.providerId}`)

      const response = await serviceCategoryClient.put<ApiResponse<SettingsUpdateResult>>(
        `${API_BASE}/${data.providerId}/booking-preferences`,
        data.bookingPreferences
      )

      if (!response.data?.data) {
        throw new Error('No result data returned from API')
      }

      return response.data.data
    } catch (error: any) {
      console.error('[SettingsService] Error updating booking preferences:', error)
      throw new Error(error.response?.data?.message || 'Failed to update booking preferences')
    }
  }

  /**
   * Update notification settings
   * PUT /api/v1/provider-settings/{providerId}/notification-settings
   */
  async updateNotificationSettings(
    data: UpdateNotificationSettingsRequest
  ): Promise<SettingsUpdateResult> {
    try {
      console.log(`[SettingsService] Updating notification settings for provider: ${data.providerId}`)

      const response = await serviceCategoryClient.put<ApiResponse<SettingsUpdateResult>>(
        `${API_BASE}/${data.providerId}/notification-settings`,
        data.notificationSettings
      )

      if (!response.data?.data) {
        throw new Error('No result data returned from API')
      }

      return response.data.data
    } catch (error: any) {
      console.error('[SettingsService] Error updating notification settings:', error)
      throw new Error(error.response?.data?.message || 'Failed to update notification settings')
    }
  }

  /**
   * Update business policies
   * PUT /api/v1/provider-settings/{providerId}/business-policies
   */
  async updateBusinessPolicies(
    data: UpdateBusinessPoliciesRequest
  ): Promise<SettingsUpdateResult> {
    try {
      console.log(`[SettingsService] Updating business policies for provider: ${data.providerId}`)

      const response = await serviceCategoryClient.put<ApiResponse<SettingsUpdateResult>>(
        `${API_BASE}/${data.providerId}/business-policies`,
        data.businessPolicies
      )

      if (!response.data?.data) {
        throw new Error('No result data returned from API')
      }

      return response.data.data
    } catch (error: any) {
      console.error('[SettingsService] Error updating business policies:', error)
      throw new Error(error.response?.data?.message || 'Failed to update business policies')
    }
  }

  /**
   * Update operating preferences
   * PUT /api/v1/provider-settings/{providerId}/operating-preferences
   */
  async updateOperatingPreferences(
    data: UpdateOperatingPreferencesRequest
  ): Promise<SettingsUpdateResult> {
    try {
      console.log(`[SettingsService] Updating operating preferences for provider: ${data.providerId}`)

      const response = await serviceCategoryClient.put<ApiResponse<SettingsUpdateResult>>(
        `${API_BASE}/${data.providerId}/operating-preferences`,
        data.operatingPreferences
      )

      if (!response.data?.data) {
        throw new Error('No result data returned from API')
      }

      return response.data.data
    } catch (error: any) {
      console.error('[SettingsService] Error updating operating preferences:', error)
      throw new Error(error.response?.data?.message || 'Failed to update operating preferences')
    }
  }

  /**
   * Update integration settings
   * PUT /api/v1/provider-settings/{providerId}/integration-settings
   */
  async updateIntegrationSettings(
    data: UpdateIntegrationSettingsRequest
  ): Promise<SettingsUpdateResult> {
    try {
      console.log(`[SettingsService] Updating integration settings for provider: ${data.providerId}`)

      const response = await serviceCategoryClient.put<ApiResponse<SettingsUpdateResult>>(
        `${API_BASE}/${data.providerId}/integration-settings`,
        data.integrationSettings
      )

      if (!response.data?.data) {
        throw new Error('No result data returned from API')
      }

      return response.data.data
    } catch (error: any) {
      console.error('[SettingsService] Error updating integration settings:', error)
      throw new Error(error.response?.data?.message || 'Failed to update integration settings')
    }
  }

  /**
   * Update account security settings
   * PUT /api/v1/provider-settings/{providerId}/account-security
   */
  async updateAccountSecurity(
    data: UpdateAccountSecurityRequest
  ): Promise<SettingsUpdateResult> {
    try {
      console.log(`[SettingsService] Updating account security for provider: ${data.providerId}`)

      const response = await serviceCategoryClient.put<ApiResponse<SettingsUpdateResult>>(
        `${API_BASE}/${data.providerId}/account-security`,
        data.accountSecurity
      )

      if (!response.data?.data) {
        throw new Error('No result data returned from API')
      }

      return response.data.data
    } catch (error: any) {
      console.error('[SettingsService] Error updating account security:', error)
      throw new Error(error.response?.data?.message || 'Failed to update account security')
    }
  }
}

// Export singleton instance
export const settingsService = new SettingsService()
