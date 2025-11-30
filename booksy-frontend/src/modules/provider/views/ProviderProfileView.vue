<template>
  <div class="profile-container">
    <!-- Loading State -->
    <div v-if="isLoading" class="loading-container">
      <div class="spinner-large"></div>
      <p>Loading your profile...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error && !profile" class="error-container">
      <div class="error-icon">⚠️</div>
      <h2>Unable to Load Profile</h2>
      <p>{{ error }}</p>
      <button @click="refresh" class="btn btn-primary">Try Again</button>
    </div>

    <!-- Profile Content -->
    <div v-else-if="profile" class="profile-content">
      <!-- Profile Header -->
      <div class="profile-header">
        <div class="cover-image" :style="coverImageStyle">
          <button class="btn-icon btn-edit-cover" title="Change Cover">
            <svg
              xmlns="http://www.w3.org/2000/svg"
              width="20"
              height="20"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <path
                d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"
              ></path>
              <circle cx="12" cy="13" r="4"></circle>
            </svg>
          </button>
        </div>

        <div class="header-content">
          <div class="avatar-section">
            <div class="avatar-wrapper">
              <img
                v-if="profile.avatarUrl"
                :src="profile.avatarUrl"
                :alt="fullName"
                class="avatar"
              />
              <div v-else class="avatar avatar-placeholder">
                <span>{{ initials }}</span>
              </div>

              <!-- Avatar Upload Progress -->
              <div v-if="avatarUpload.uploading" class="avatar-overlay">
                <div class="progress-ring">
                  <svg width="120" height="120">
                    <circle cx="60" cy="60" r="54" class="progress-ring-circle-bg" />
                    <circle
                      cx="60"
                      cy="60"
                      r="54"
                      class="progress-ring-circle"
                      :style="progressStyle"
                    />
                  </svg>
                  <span class="progress-text">{{ avatarUpload.progress }}%</span>
                </div>
              </div>

              <!-- Avatar Actions -->
              <div class="avatar-actions">
                <input
                  ref="avatarInput"
                  type="file"
                  accept="image/*"
                  @change="handleAvatarUpload"
                  style="display: none"
                />
                <button
                  @click="avatarInput?.click()"
                  class="btn-icon btn-edit-avatar"
                  :disabled="avatarUpload.uploading"
                  title="Change Avatar"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                  >
                    <path
                      d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"
                    ></path>
                    <circle cx="12" cy="13" r="4"></circle>
                  </svg>
                </button>
                <button
                  v-if="profile.avatarUrl"
                  @click="handleDeleteAvatar"
                  class="btn-icon btn-delete-avatar"
                  :disabled="avatarUpload.uploading"
                  title="Remove Avatar"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                  >
                    <polyline points="3 6 5 6 21 6"></polyline>
                    <path
                      d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"
                    ></path>
                  </svg>
                </button>
              </div>
            </div>

            <div class="user-info">
              <h1 class="user-name">{{ fullName }}</h1>
              <p class="user-email">{{ profile.email }}</p>
              <div class="verification-badges">
                <span v-if="isEmailVerified" class="badge badge-success">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M9 16.2L4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z" />
                  </svg>
                  Email Verified
                </span>
                <button v-else @click="sendEmailVerification" class="badge badge-warning">
                  Email Not Verified - Click to Verify
                </button>

                <span v-if="isPhoneVerified" class="badge badge-success">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M9 16.2L4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z" />
                  </svg>
                  Phone Verified
                </span>
                <button
                  v-else-if="profile.phoneNumber"
                  @click="sendPhoneVerification"
                  class="badge badge-warning"
                >
                  Phone Not Verified - Click to Verify
                </button>
              </div>
            </div>
          </div>

          <!-- Profile Stats -->
          <div v-if="stats" class="profile-stats">
            <div class="stat-item">
              <span class="stat-value">{{ stats.totalBookings }}</span>
              <span class="stat-label">Bookings</span>
            </div>
            <div class="stat-item">
              <span class="stat-value">${{ stats.totalSpent.toLocaleString() }}</span>
              <span class="stat-label">Total Spent</span>
            </div>
            <div class="stat-item">
              <span class="stat-value">{{ stats.favoriteProviders }}</span>
              <span class="stat-label">Favorites</span>
            </div>
            <div class="stat-item">
              <span class="stat-value">{{ stats.reviewsGiven }}</span>
              <span class="stat-label">Reviews</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Profile Tabs -->
      <div class="profile-tabs">
        <div class="tabs-header">
          <button
            v-for="tab in tabs"
            :key="tab.id"
            @click="activeTab = tab.id"
            :class="['tab-button', { active: activeTab === tab.id }]"
          >
            <span v-html="tab.icon" class="tab-icon"></span>
            <span class="tab-label">{{ tab.label }}</span>
          </button>
        </div>

        <div class="tabs-content">
          <!-- Personal Information Tab -->
          <div v-show="activeTab === 'personal'" class="tab-pane">
            <ProfilePersonalInfo
              :profile="profile"
              :is-saving="isSaving"
              @update="handleUpdateProfile"
            />
          </div>

          <!-- Gallery Tab -->
          <div v-show="activeTab === 'gallery'" class="tab-pane">
            <ProfileGallery />
          </div>

          <!-- Preferences Tab -->
          <div v-show="activeTab === 'preferences'" class="tab-pane">
            <ProfilePreferences
              :preferences="profile.preferences"
              :is-saving="isSaving"
              @update="handleUpdatePreferences"
            />
          </div>

          <!-- Privacy Tab -->
          <div v-show="activeTab === 'privacy'" class="tab-pane">
            <ProfilePrivacy
              :privacy-settings="profile.privacySettings"
              :is-saving="isSaving"
              @update="handleUpdatePrivacy"
            />
          </div>

          <!-- Security Tab -->
          <div v-show="activeTab === 'security'" class="tab-pane">
            <ProfileSecurity :is-saving="isSaving" @change-password="handleChangePassword" />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useUserProfile } from '@/modules/user-management/composables/useUserProfile'
import type {
  UpdateProfileRequest,
  UpdatePreferencesRequest,
  UpdatePrivacySettingsRequest,
  ChangePasswordRequest,
} from '@/modules/user-management/types/user-profile.types'

// Import child components (to be created)
import ProfilePersonalInfo from '../components/ProfilePersonalInfo.vue'
import ProfileGallery from '../components/ProfileGallery.vue'
import ProfilePreferences from '../components/ProfilePreferences.vue'
import ProfilePrivacy from '../components/ProfilePrivacy.vue'
import ProfileSecurity from '../components/ProfileSecurity.vue'

// Composables
const {
  profile,
  stats,
  isLoading,
  isSaving,
  error,
  avatarUpload,
  fullName,
  isEmailVerified,
  isPhoneVerified,
  updateProfile,
  updatePreferences,
  updatePrivacySettings,
  changePassword,
  uploadAvatar,
  deleteAvatar,
  sendEmailVerification,
  sendPhoneVerification,
  refresh,
} = useUserProfile()

// Local state
const activeTab = ref('personal')
const avatarInput = ref<HTMLInputElement | null>(null)

// Tabs configuration
const tabs = [
  {
    id: 'personal',
    label: 'Personal Info',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>',
  },
  {
    id: 'gallery',
    label: 'Gallery',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect><circle cx="8.5" cy="8.5" r="1.5"></circle><polyline points="21 15 16 10 5 21"></polyline></svg>',
  },
  {
    id: 'preferences',
    label: 'Preferences',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="3"></circle><path d="M12 1v6m0 6v6m-7-7h6m6 0h6"></path></svg>',
  },
  {
    id: 'privacy',
    label: 'Privacy',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path></svg>',
  },
  {
    id: 'security',
    label: 'Security',
    icon: '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect><path d="M7 11V7a5 5 0 0 1 10 0v4"></path></svg>',
  },
]

// Computed
const initials = computed(() => {
  if (!profile.value) return 'U'
  const first = profile.value.firstName?.[0] || ''
  const last = profile.value.lastName?.[0] || ''
  return (first + last).toUpperCase() || 'U'
})

const coverImageStyle = computed(() => {
  if (profile.value?.coverImageUrl) {
    return { backgroundImage: `url(${profile.value.coverImageUrl})` }
  }
  return { background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)' }
})

const progressStyle = computed(() => {
  const circumference = 2 * Math.PI * 54
  const offset = circumference - (avatarUpload.value.progress / 100) * circumference
  return {
    strokeDasharray: `${circumference} ${circumference}`,
    strokeDashoffset: offset,
  }
})

// Methods
async function handleUpdateProfile(data: UpdateProfileRequest) {
  await updateProfile(data)
}

async function handleUpdatePreferences(data: UpdatePreferencesRequest) {
  await updatePreferences(data)
}

async function handleUpdatePrivacy(data: UpdatePrivacySettingsRequest) {
  await updatePrivacySettings(data)
}

async function handleChangePassword(data: ChangePasswordRequest) {
  await changePassword(data)
}

async function handleAvatarUpload(event: Event) {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (file) {
    await uploadAvatar(file)
    // Reset input
    if (avatarInput.value) {
      avatarInput.value.value = ''
    }
  }
}

async function handleDeleteAvatar() {
  if (confirm('Are you sure you want to remove your profile picture?')) {
    await deleteAvatar()
  }
}

// Lifecycle
onMounted(async () => {
  await refresh()
})
</script>

<style scoped lang="scss">
.profile-container {
  min-height: 100vh;
  background: var(--color-background);
}

// Loading & Error States
.loading-container,
.error-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 50vh;
  padding: 2rem;
}

.spinner-large {
  width: 50px;
  height: 50px;
  border: 4px solid var(--color-border);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

.error-icon {
  font-size: 4rem;
  margin-bottom: 1rem;
}

// Profile Header
.profile-header {
  background: white;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  margin-bottom: 2rem;
}

.cover-image {
  height: 250px;
  background-size: cover;
  background-position: center;
  position: relative;

  .btn-edit-cover {
    position: absolute;
    bottom: 1rem;
    right: 1rem;
    background: rgba(0, 0, 0, 0.6);
    color: white;
    backdrop-filter: blur(10px);

    &:hover {
      background: rgba(0, 0, 0, 0.8);
    }
  }
}

.header-content {
  padding: 0 2rem 2rem;
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  gap: 2rem;
  flex-wrap: wrap;

  @media (max-width: 768px) {
    flex-direction: column;
    align-items: center;
  }
}

.avatar-section {
  display: flex;
  align-items: flex-end;
  gap: 1.5rem;
  margin-top: -60px;

  @media (max-width: 768px) {
    flex-direction: column;
    align-items: center;
    margin-top: -80px;
  }
}

.avatar-wrapper {
  position: relative;
}

.avatar {
  width: 140px;
  height: 140px;
  border-radius: 50%;
  border: 5px solid white;
  object-fit: cover;
  background: var(--color-background);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);

  &.avatar-placeholder {
    display: flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    font-size: 3rem;
    font-weight: 600;
  }
}

.avatar-overlay {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.7);
  border-radius: 50%;
}

.progress-ring {
  position: relative;
  width: 120px;
  height: 120px;

  svg {
    transform: rotate(-90deg);
  }

  .progress-ring-circle-bg {
    fill: none;
    stroke: rgba(255, 255, 255, 0.2);
    stroke-width: 4;
  }

  .progress-ring-circle {
    fill: none;
    stroke: var(--color-primary);
    stroke-width: 4;
    stroke-linecap: round;
    transition: stroke-dashoffset 0.3s ease;
  }

  .progress-text {
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    color: white;
    font-weight: 600;
    font-size: 1.25rem;
  }
}

.avatar-actions {
  position: absolute;
  bottom: 10px;
  right: 0;
  display: flex;
  gap: 0.5rem;

  .btn-icon {
    width: 36px;
    height: 36px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--color-primary);
    color: white;
    border: 2px solid white;
    cursor: pointer;
    transition: all 0.2s;

    &:hover:not(:disabled) {
      transform: scale(1.1);
      background: var(--color-primary-dark);
    }

    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
  }

  .btn-delete-avatar {
    background: var(--color-danger);

    &:hover:not(:disabled) {
      background: var(--color-danger-dark);
    }
  }
}

.user-info {
  flex: 1;
  min-width: 0;

  @media (max-width: 768px) {
    text-align: center;
  }
}

.user-name {
  font-size: 2rem;
  font-weight: 700;
  color: var(--color-text-primary);
  margin: 0 0 0.25rem;
}

.user-email {
  color: var(--color-text-secondary);
  margin: 0 0 0.75rem;
}

.verification-badges {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;

  @media (max-width: 768px) {
    justify-content: center;
  }

  .badge {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
    padding: 0.375rem 0.75rem;
    border-radius: 9999px;
    font-size: 0.75rem;
    font-weight: 500;
    border: none;
    cursor: default;

    &.badge-success {
      background: #d1fae5;
      color: #065f46;
    }

    &.badge-warning {
      background: #fef3c7;
      color: #92400e;
      cursor: pointer;

      &:hover {
        background: #fde68a;
      }
    }
  }
}

.profile-stats {
  display: flex;
  gap: 2rem;

  @media (max-width: 768px) {
    width: 100%;
    justify-content: space-around;
  }

  .stat-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.25rem;

    .stat-value {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--color-primary);
    }

    .stat-label {
      font-size: 0.875rem;
      color: var(--color-text-secondary);
      white-space: nowrap;
    }
  }
}

// Tabs
.profile-tabs {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 1rem;
}

.tabs-header {
  display: flex;
  gap: 0.5rem;
  border-bottom: 2px solid var(--color-border);
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;

  &::-webkit-scrollbar {
    height: 4px;
  }

  &::-webkit-scrollbar-thumb {
    background: var(--color-border);
    border-radius: 2px;
  }
}

.tab-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 1rem 1.5rem;
  background: transparent;
  border: none;
  border-bottom: 3px solid transparent;
  color: var(--color-text-secondary);
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;

  .tab-icon {
    display: flex;
    align-items: center;
  }

  &:hover {
    color: var(--color-primary);
    background: var(--color-background);
  }

  &.active {
    color: var(--color-primary);
    border-bottom-color: var(--color-primary);
  }
}

.tabs-content {
  background: white;
  border-radius: 0.5rem;
  margin-top: 1.5rem;
  padding: 2rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.tab-pane {
  animation: fadeIn 0.3s ease;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

// Utility classes
.btn {
  padding: 0.75rem 1.5rem;
  border-radius: 0.375rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  border: none;

  &.btn-primary {
    background: var(--color-primary);
    color: white;

    &:hover {
      background: var(--color-primary-dark);
    }
  }
}

.btn-icon {
  padding: 0.5rem;
  border-radius: 0.375rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: transparent;
  border: 1px solid var(--color-border);
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: var(--color-background);
    border-color: var(--color-primary);
    color: var(--color-primary);
  }
}
</style>
