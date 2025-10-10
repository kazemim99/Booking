// src/modules/user-management/composables/useUserProfile.ts
import { ref, computed } from 'vue'
import { userManagementClient } from '@/core/api/client/http-client'
import { useNotificationStore } from '@/core/stores/modules/notification.store'
import type {
  UserProfile,
  UpdateProfileRequest,
  UpdatePreferencesRequest,
  UpdatePrivacySettingsRequest,
  ChangePasswordRequest,
  ProfileStats,
  AvatarUploadProgress
} from '../types/user-profile.types'

export function useUserProfile() {
  const notificationStore = useNotificationStore()

  // State
  const profile = ref<UserProfile | null>(null)
  const stats = ref<ProfileStats | null>(null)
  const isLoading = ref(false)
  const isSaving = ref(false)
  const error = ref<string | null>(null)
  const avatarUpload = ref<AvatarUploadProgress>({
    uploading: false,
    progress: 0
  })

  // Computed
  const fullName = computed(() => {
    if (!profile.value) return ''
    return `${profile.value.firstName} ${profile.value.lastName}`.trim()
  })

  const hasAddress = computed(() => {
    return profile.value?.address !== undefined
  })

  const isEmailVerified = computed(() => profile.value?.emailVerified ?? false)
  const isPhoneVerified = computed(() => profile.value?.phoneVerified ?? false)

  // ============================================
  // API Methods
  // ============================================

  /**
   * Fetch user profile
   */
  async function fetchProfile(): Promise<void> {
    try {

      isLoading.value = true
      error.value = null

      const response = await userManagementClient.get<UserProfile>('/api/v1/users/profile')

      if (response.success && response.data) {
        profile.value = response.data
      } else {
        throw new Error(response.message || 'Failed to fetch profile')
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load profile'
      error.value = errorMessage
      notificationStore.error('Error', errorMessage)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Update profile information
   */
  async function updateProfile(data: UpdateProfileRequest): Promise<boolean> {
    try {
      isSaving.value = true
      error.value = null

      const response = await userManagementClient.put<UserProfile>(
        '/api/v1/users/profile',
        data
      )

      if (response.success && response.data) {
        profile.value = response.data
        notificationStore.success('Success', 'Profile updated successfully')
        return true
      } else {
        throw new Error(response.message || 'Failed to update profile')
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to update profile'
      error.value = errorMessage
      notificationStore.error('Error', errorMessage)
      return false
    } finally {
      isSaving.value = false
    }
  }

  /**
   * Update user preferences
   */
  async function updatePreferences(data: UpdatePreferencesRequest): Promise<boolean> {
    try {
      isSaving.value = true
      error.value = null

      const response = await userManagementClient.put<UserProfile>(
        '/api/v1/users/profile/preferences',
        data
      )

      if (response.success && response.data) {
        profile.value = response.data
        notificationStore.success('Success', 'Preferences updated successfully')
        return true
      } else {
        throw new Error(response.message || 'Failed to update preferences')
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to update preferences'
      error.value = errorMessage
      notificationStore.error('Error', errorMessage)
      return false
    } finally {
      isSaving.value = false
    }
  }

  /**
   * Update privacy settings
   */
  async function updatePrivacySettings(data: UpdatePrivacySettingsRequest): Promise<boolean> {
    try {
      isSaving.value = true
      error.value = null

      const response = await userManagementClient.put<UserProfile>(
        '/api/v1/users/profile/privacy',
        data
      )

      if (response.success && response.data) {
        profile.value = response.data
        notificationStore.success('Success', 'Privacy settings updated successfully')
        return true
      } else {
        throw new Error(response.message || 'Failed to update privacy settings')
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to update privacy settings'
      error.value = errorMessage
      notificationStore.error('Error', errorMessage)
      return false
    } finally {
      isSaving.value = false
    }
  }

  /**
   * Change password
   */
  async function changePassword(data: ChangePasswordRequest): Promise<boolean> {
    try {
      isSaving.value = true
      error.value = null

      const response = await userManagementClient.post<null>(
        '/api/v1/users/profile/change-password',
        data
      )

      if (response.success) {
        notificationStore.success('Success', 'Password changed successfully')
        return true
      } else {
        throw new Error(response.message || 'Failed to change password')
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to change password'
      error.value = errorMessage
      notificationStore.error('Error', errorMessage)
      return false
    } finally {
      isSaving.value = false
    }
  }

  /**
   * Upload avatar
   */
  async function uploadAvatar(file: File): Promise<boolean> {
    try {
      avatarUpload.value = { uploading: true, progress: 0 }
      error.value = null

      // Simulate progress updates
      const progressInterval = setInterval(() => {
        if (avatarUpload.value.progress < 90) {
          avatarUpload.value.progress += 10
        }
      }, 200)

      const formData = new FormData()
      formData.append('avatar', file)

      const response = await userManagementClient.post<UserProfile>(
        '/api/v1/users/profile/avatar',
        formData,
        {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        }
      )

      clearInterval(progressInterval)
      avatarUpload.value.progress = 100

      if (response.success && response.data) {
        profile.value = response.data
        notificationStore.success('Success', 'Avatar uploaded successfully')
        return true
      } else {
        throw new Error(response.message || 'Failed to upload avatar')
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to upload avatar'
      error.value = errorMessage
      avatarUpload.value.error = errorMessage
      notificationStore.error('Error', errorMessage)
      return false
    } finally {
      setTimeout(() => {
        avatarUpload.value = { uploading: false, progress: 0 }
      }, 1000)
    }
  }

  /**
   * Delete avatar
   */
  async function deleteAvatar(): Promise<boolean> {
    try {
      isSaving.value = true
      error.value = null

      const response = await userManagementClient.delete<UserProfile>(
        '/api/v1/users/profile/avatar'
      )

      if (response.success && response.data) {
        profile.value = response.data
        notificationStore.success('Success', 'Avatar removed successfully')
        return true
      } else {
        throw new Error(response.message || 'Failed to delete avatar')
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to delete avatar'
      error.value = errorMessage
      notificationStore.error('Error', errorMessage)
      return false
    } finally {
      isSaving.value = false
    }
  }

  /**
   * Fetch profile statistics
   */
  async function fetchStats(): Promise<void> {
    try {
      const response = await userManagementClient.get<ProfileStats>(
        '/api/v1/users/profile/stats'
      )

      if (response.success && response.data) {
        stats.value = response.data
      }
    } catch (err: unknown) {
      console.error('Failed to fetch profile stats:', err)
    }
  }

  /**
   * Send email verification
   */
  async function sendEmailVerification(): Promise<boolean> {
    try {
      const response = await userManagementClient.post<null>(
        '/api/v1/users/profile/verify-email'
      )

      if (response.success) {
        notificationStore.success('Success', 'Verification email sent')
        return true
      }
      return false
    } catch {
      notificationStore.error('Error', 'Failed to send verification email')
      return false
    }
  }

  /**
   * Send phone verification
   */
  async function sendPhoneVerification(): Promise<boolean> {
    try {
      const response = await userManagementClient.post<null>(
        '/api/v1/users/profile/verify-phone'
      )

      if (response.success) {
        notificationStore.success('Success', 'Verification SMS sent')
        return true
      }
      return false
    } catch {
      notificationStore.error('Error', 'Failed to send verification SMS')
      return false
    }
  }

  /**
   * Refresh profile data
   */
  async function refresh(): Promise<void> {
    await Promise.all([fetchProfile(), fetchStats()])
  }

  return {
    // State
    profile,
    stats,
    isLoading,
    isSaving,
    error,
    avatarUpload,

    // Computed
    fullName,
    hasAddress,
    isEmailVerified,
    isPhoneVerified,

    // Methods
    fetchProfile,
    updateProfile,
    updatePreferences,
    updatePrivacySettings,
    changePassword,
    uploadAvatar,
    deleteAvatar,
    fetchStats,
    sendEmailVerification,
    sendPhoneVerification,
    refresh,
  }
}
