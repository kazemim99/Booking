// src/modules/provider/stores/settings.store.ts

import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { NotificationChannel, Language, DateFormat, TimeFormat, Currency } from '../types/settings.types'
import type {
  ProviderSettings,
  BookingPreferences,
  NotificationSettings,
  BusinessPolicies,
  OperatingPreferences,
  IntegrationSettings,
  AccountSecurity,
  UpdateBookingPreferencesRequest,
  UpdateNotificationSettingsRequest,
  UpdateBusinessPoliciesRequest,
  UpdateOperatingPreferencesRequest,
  UpdateIntegrationSettingsRequest,
  UpdateAccountSecurityRequest,
  SettingsUpdateResult,
  SettingsSection,
} from '../types/settings.types'

export const useSettingsStore = defineStore('settings', () => {
  // ============================================
  // State
  // ============================================

  const settings = ref<ProviderSettings | null>(null)
  const currentSection = ref<SettingsSection | null>(null)
  const hasUnsavedChanges = ref(false)

  // UI State
  const isLoading = ref(false)
  const isSaving = ref(false)
  const error = ref<string | null>(null)
  const successMessage = ref<string | null>(null)

  // ============================================
  // Getters (Computed)
  // ============================================

  const hasSettings = computed(() => settings.value !== null)

  const bookingPreferences = computed(() => settings.value?.bookingPreferences || null)

  const notificationSettings = computed(() => settings.value?.notificationSettings || null)

  const businessPolicies = computed(() => settings.value?.businessPolicies || null)

  const operatingPreferences = computed(() => settings.value?.operatingPreferences || null)

  const integrationSettings = computed(() => settings.value?.integrationSettings || null)

  const accountSecurity = computed(() => settings.value?.accountSecurity || null)

  const hasActiveIntegrations = computed(() => {
    if (!integrationSettings.value) return false
    const calendarActive = integrationSettings.value.calendar.some(
      (c) => c.status === 'Connected'
    )
    const paymentActive = integrationSettings.value.paymentGateway.some(
      (p) => p.status === 'Connected'
    )
    const socialActive = integrationSettings.value.socialMedia.some(
      (s) => s.status === 'Connected'
    )
    return calendarActive || paymentActive || socialActive
  })

  const twoFactorEnabled = computed(() => {
    return accountSecurity.value?.twoFactorAuth.enabled || false
  })

  // ============================================
  // Actions - Load Settings
  // ============================================

  /**
   * Load provider settings
   */
  async function loadSettings(providerId: string): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      // TODO: Replace with actual API call when backend is ready
      // For now, return mock data
      settings.value = getMockSettings(providerId)

      console.log('[SettingsStore] Settings loaded for provider:', providerId)
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message || 'Failed to load settings'
      } else {
        error.value = 'Failed to load settings'
      }
      console.error('Load settings error:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  // ============================================
  // Actions - Update Settings
  // ============================================

  /**
   * Update booking preferences
   */
  async function updateBookingPreferences(
    data: UpdateBookingPreferencesRequest
  ): Promise<SettingsUpdateResult> {
    isSaving.value = true
    error.value = null
    successMessage.value = null

    // Store original for rollback
    const original = settings.value?.bookingPreferences
      ? { ...settings.value.bookingPreferences }
      : null

    try {
      // Optimistically update UI
      if (settings.value) {
        settings.value.bookingPreferences = {
          ...settings.value.bookingPreferences,
          ...data.bookingPreferences,
        }
      }

      // TODO: Replace with actual API call when backend is ready
      const result: SettingsUpdateResult = {
        success: true,
        providerId: data.providerId,
        settingsUpdated: ['bookingPreferences'],
        lastModifiedAt: new Date().toISOString(),
      }

      successMessage.value = 'Booking preferences updated successfully'
      hasUnsavedChanges.value = false
      return result
    } catch (err: unknown) {
      // Rollback on failure
      if (original && settings.value) {
        settings.value.bookingPreferences = original
      }

      if (err instanceof Error) {
        error.value = err.message || 'Failed to update booking preferences'
      } else {
        error.value = 'Failed to update booking preferences'
      }
      console.error('Update booking preferences error:', err)
      throw err
    } finally {
      isSaving.value = false
    }
  }

  /**
   * Update notification settings
   */
  async function updateNotificationSettings(
    data: UpdateNotificationSettingsRequest
  ): Promise<SettingsUpdateResult> {
    isSaving.value = true
    error.value = null
    successMessage.value = null

    const original = settings.value?.notificationSettings
      ? { ...settings.value.notificationSettings }
      : null

    try {
      if (settings.value) {
        settings.value.notificationSettings = {
          ...settings.value.notificationSettings,
          ...data.notificationSettings,
        }
      }

      // TODO: Replace with actual API call
      const result: SettingsUpdateResult = {
        success: true,
        providerId: data.providerId,
        settingsUpdated: ['notificationSettings'],
        lastModifiedAt: new Date().toISOString(),
      }

      successMessage.value = 'Notification settings updated successfully'
      hasUnsavedChanges.value = false
      return result
    } catch (err: unknown) {
      if (original && settings.value) {
        settings.value.notificationSettings = original
      }

      if (err instanceof Error) {
        error.value = err.message || 'Failed to update notification settings'
      } else {
        error.value = 'Failed to update notification settings'
      }
      console.error('Update notification settings error:', err)
      throw err
    } finally {
      isSaving.value = false
    }
  }

  /**
   * Update business policies
   */
  async function updateBusinessPolicies(
    data: UpdateBusinessPoliciesRequest
  ): Promise<SettingsUpdateResult> {
    isSaving.value = true
    error.value = null
    successMessage.value = null

    const original = settings.value?.businessPolicies
      ? { ...settings.value.businessPolicies }
      : null

    try {
      if (settings.value) {
        settings.value.businessPolicies = {
          ...settings.value.businessPolicies,
          ...data.businessPolicies,
        }
      }

      // TODO: Replace with actual API call
      const result: SettingsUpdateResult = {
        success: true,
        providerId: data.providerId,
        settingsUpdated: ['businessPolicies'],
        lastModifiedAt: new Date().toISOString(),
      }

      successMessage.value = 'Business policies updated successfully'
      hasUnsavedChanges.value = false
      return result
    } catch (err: unknown) {
      if (original && settings.value) {
        settings.value.businessPolicies = original
      }

      if (err instanceof Error) {
        error.value = err.message || 'Failed to update business policies'
      } else {
        error.value = 'Failed to update business policies'
      }
      console.error('Update business policies error:', err)
      throw err
    } finally {
      isSaving.value = false
    }
  }

  /**
   * Update operating preferences
   */
  async function updateOperatingPreferences(
    data: UpdateOperatingPreferencesRequest
  ): Promise<SettingsUpdateResult> {
    isSaving.value = true
    error.value = null
    successMessage.value = null

    const original = settings.value?.operatingPreferences
      ? { ...settings.value.operatingPreferences }
      : null

    try {
      if (settings.value) {
        settings.value.operatingPreferences = {
          ...settings.value.operatingPreferences,
          ...data.operatingPreferences,
        }
      }

      // TODO: Replace with actual API call
      const result: SettingsUpdateResult = {
        success: true,
        providerId: data.providerId,
        settingsUpdated: ['operatingPreferences'],
        lastModifiedAt: new Date().toISOString(),
      }

      successMessage.value = 'Operating preferences updated successfully'
      hasUnsavedChanges.value = false
      return result
    } catch (err: unknown) {
      if (original && settings.value) {
        settings.value.operatingPreferences = original
      }

      if (err instanceof Error) {
        error.value = err.message || 'Failed to update operating preferences'
      } else {
        error.value = 'Failed to update operating preferences'
      }
      console.error('Update operating preferences error:', err)
      throw err
    } finally {
      isSaving.value = false
    }
  }

  /**
   * Update integration settings
   */
  async function updateIntegrationSettings(
    data: UpdateIntegrationSettingsRequest
  ): Promise<SettingsUpdateResult> {
    isSaving.value = true
    error.value = null
    successMessage.value = null

    const original = settings.value?.integrationSettings
      ? { ...settings.value.integrationSettings }
      : null

    try {
      if (settings.value) {
        settings.value.integrationSettings = {
          ...settings.value.integrationSettings,
          ...data.integrationSettings,
        }
      }

      // TODO: Replace with actual API call
      const result: SettingsUpdateResult = {
        success: true,
        providerId: data.providerId,
        settingsUpdated: ['integrationSettings'],
        lastModifiedAt: new Date().toISOString(),
      }

      successMessage.value = 'Integration settings updated successfully'
      hasUnsavedChanges.value = false
      return result
    } catch (err: unknown) {
      if (original && settings.value) {
        settings.value.integrationSettings = original
      }

      if (err instanceof Error) {
        error.value = err.message || 'Failed to update integration settings'
      } else {
        error.value = 'Failed to update integration settings'
      }
      console.error('Update integration settings error:', err)
      throw err
    } finally {
      isSaving.value = false
    }
  }

  /**
   * Update account security settings
   */
  async function updateAccountSecurity(
    data: UpdateAccountSecurityRequest
  ): Promise<SettingsUpdateResult> {
    isSaving.value = true
    error.value = null
    successMessage.value = null

    const original = settings.value?.accountSecurity
      ? { ...settings.value.accountSecurity }
      : null

    try {
      if (settings.value) {
        settings.value.accountSecurity = {
          ...settings.value.accountSecurity,
          ...data.accountSecurity,
        }
      }

      // TODO: Replace with actual API call
      const result: SettingsUpdateResult = {
        success: true,
        providerId: data.providerId,
        settingsUpdated: ['accountSecurity'],
        lastModifiedAt: new Date().toISOString(),
      }

      successMessage.value = 'Account security settings updated successfully'
      hasUnsavedChanges.value = false
      return result
    } catch (err: unknown) {
      if (original && settings.value) {
        settings.value.accountSecurity = original
      }

      if (err instanceof Error) {
        error.value = err.message || 'Failed to update account security settings'
      } else {
        error.value = 'Failed to update account security settings'
      }
      console.error('Update account security settings error:', err)
      throw err
    } finally {
      isSaving.value = false
    }
  }

  // ============================================
  // Actions - UI State
  // ============================================

  /**
   * Set current settings section
   */
  function setCurrentSection(section: SettingsSection): void {
    currentSection.value = section
  }

  /**
   * Mark settings as having unsaved changes
   */
  function markAsChanged(): void {
    hasUnsavedChanges.value = true
  }

  /**
   * Clear error message
   */
  function clearError(): void {
    error.value = null
  }

  /**
   * Clear success message
   */
  function clearSuccess(): void {
    successMessage.value = null
  }

  /**
   * Reset store to initial state
   */
  function $reset(): void {
    settings.value = null
    currentSection.value = null
    hasUnsavedChanges.value = false
    isLoading.value = false
    isSaving.value = false
    error.value = null
    successMessage.value = null
  }

  // ============================================
  // Mock Data (temporary until backend is ready)
  // ============================================

  function getMockSettings(providerId: string): ProviderSettings {
    return {
      providerId,
      bookingPreferences: {
        bookingWindow: {
          minAdvanceBookingHours: 2,
          maxAdvanceBookingDays: 90,
        },
        approval: {
          requiresApproval: false,
          autoApproveForReturningCustomers: true,
          autoApproveThresholdBookings: 3,
        },
        cancellationPolicy: {
          allowCancellation: true,
          cancellationWindowHours: 24,
          chargeNoShowFee: true,
          noShowFeePercentage: 50,
          allowRescheduling: true,
          rescheduleWindowHours: 12,
        },
        depositSettings: {
          requiresDeposit: false,
          depositType: 'Percentage',
          depositPercentage: 20,
          refundableDeposit: true,
          refundWindowHours: 48,
        },
        allowOnlinePayment: true,
        allowWalkins: true,
        bufferBetweenBookings: 15,
      },
      notificationSettings: {
        newBooking: {
          enabled: true,
          channels: [NotificationChannel.Email, NotificationChannel.Push],
        },
        bookingCancelled: {
          enabled: true,
          channels: [NotificationChannel.Email, NotificationChannel.SMS],
        },
        bookingRescheduled: {
          enabled: true,
          channels: [NotificationChannel.Email],
        },
        bookingConfirmed: {
          enabled: true,
          channels: [NotificationChannel.Email],
        },
        customerReminder: {
          enabled: true,
          channels: [NotificationChannel.Email, NotificationChannel.SMS],
        },
        customerReminderHours: 24,
        providerReminder: {
          enabled: true,
          channels: [NotificationChannel.Push],
        },
        providerReminderMinutes: 30,
        newReview: {
          enabled: true,
          channels: [NotificationChannel.Email, NotificationChannel.InApp],
        },
        reviewResponse: {
          enabled: true,
          channels: [NotificationChannel.Email],
        },
        paymentReceived: {
          enabled: true,
          channels: [NotificationChannel.Email],
        },
        paymentFailed: {
          enabled: true,
          channels: [NotificationChannel.Email, NotificationChannel.SMS],
        },
        payoutProcessed: {
          enabled: true,
          channels: [NotificationChannel.Email],
        },
        recipients: [],
        quietHours: {
          enabled: false,
          startTime: '22:00',
          endTime: '08:00',
          timezone: 'UTC',
        },
      },
      businessPolicies: {
        cancellationPolicy: {
          currentVersion: {
            version: '1.0',
            effectiveDate: new Date().toISOString(),
            content: 'Customers may cancel or reschedule appointments up to 24 hours in advance without penalty.',
            lastModifiedBy: 'System',
            lastModifiedAt: new Date().toISOString(),
          },
          versions: [],
          displayOnBooking: true,
          requiresCustomerAcceptance: false,
        },
        privacyPolicy: {
          currentVersion: {
            version: '1.0',
            effectiveDate: new Date().toISOString(),
            content: 'We respect your privacy and protect your personal information.',
            lastModifiedBy: 'System',
            lastModifiedAt: new Date().toISOString(),
          },
          versions: [],
          displayOnBooking: true,
          requiresCustomerAcceptance: true,
        },
        termsAndConditions: {
          currentVersion: {
            version: '1.0',
            effectiveDate: new Date().toISOString(),
            content: 'By booking a service, you agree to our terms and conditions.',
            lastModifiedBy: 'System',
            lastModifiedAt: new Date().toISOString(),
          },
          versions: [],
          displayOnBooking: true,
          requiresCustomerAcceptance: true,
        },
        customPolicies: [],
      },
      operatingPreferences: {
        defaultServiceSettings: {
          defaultDuration: 60,
          defaultPreparationTime: 0,
          defaultBufferTime: 15,
          defaultAllowOnlineBooking: true,
          defaultRequiresDeposit: false,
          defaultDepositPercentage: 20,
        },
        localization: {
          timezone: 'UTC',
          language: Language.English,
          dateFormat: DateFormat.MMDDYYYY,
          timeFormat: TimeFormat.Hour12,
          currency: Currency.USD,
          firstDayOfWeek: 0,
        },
        displayBusinessHoursOnProfile: true,
        displayPricingOnProfile: true,
        displayStaffOnProfile: true,
        allowCustomerNotes: true,
        allowCustomerCancellation: true,
        allowCustomerRescheduling: true,
      },
      integrationSettings: {
        calendar: [],
        paymentGateway: [],
        socialMedia: [],
      },
      accountSecurity: {
        twoFactorAuth: {
          enabled: false,
          method: 'Email',
        },
        trustedDevices: [],
        staffPermissions: [],
        loginActivity: [],
        passwordExpirationDays: 90,
        requirePasswordChange: false,
      },
      lastModifiedAt: new Date().toISOString(),
      lastModifiedBy: 'System',
    }
  }

  return {
    // State
    settings,
    currentSection,
    hasUnsavedChanges,
    isLoading,
    isSaving,
    error,
    successMessage,

    // Getters
    hasSettings,
    bookingPreferences,
    notificationSettings,
    businessPolicies,
    operatingPreferences,
    integrationSettings,
    accountSecurity,
    hasActiveIntegrations,
    twoFactorEnabled,

    // Actions - Load
    loadSettings,

    // Actions - Update
    updateBookingPreferences,
    updateNotificationSettings,
    updateBusinessPolicies,
    updateOperatingPreferences,
    updateIntegrationSettings,
    updateAccountSecurity,

    // Actions - UI
    setCurrentSection,
    markAsChanged,
    clearError,
    clearSuccess,
    $reset,
  }
})
