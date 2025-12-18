<template>
  <div
    class="provider-card"
    :class="[`view-mode-${viewMode}`, { clickable: isClickable }]"
    @click="handleClick"
  >
    <!-- Profile Image / Logo -->
    <div class="provider-image">
      <img
        v-if="providerImageUrl"
        :src="providerImageUrl"
        :alt="provider.businessName"
        @error="handleImageError"
      />
      <div v-else class="placeholder-logo">
        <span class="initials">{{ getInitials(provider.businessName) }}</span>
      </div>

      <!-- Status Badge -->
      <div
        v-if="showStatus"
        class="status-badge"
        :class="`status-${provider.status.toLowerCase()}`"
      >
        {{ provider.status }}
      </div>
    </div>

    <!-- Content -->
    <div class="provider-content">
      <!-- Header -->
      <div class="provider-header">
        <h3 class="provider-name">{{ provider.businessName }}</h3>
        <div class="badges-group">
          <Badge :variant="getTypeVariant(provider.type)">
            {{ provider.type }}
          </Badge>
          <!-- Organization Badge -->
          <Badge v-if="isOrganization" variant="primary" class="organization-badge">
            <svg
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              class="badge-icon"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
              />
            </svg>
            سازمان
          </Badge>
          <!-- Staff Count Badge -->
          <Badge v-if="isOrganization && staffCount > 0" variant="info" class="staff-count-badge">
            <svg
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              class="badge-icon"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
              />
            </svg>
            {{ staffCount }} کارمند
          </Badge>
        </div>
      </div>

      <!-- Description -->
      <p class="provider-description">
        {{ truncateText(provider.description, descriptionLength) }}
      </p>

      <!-- Location -->
      <div class="provider-location">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"
          />
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"
          />
        </svg>
        <span>{{ formatLocation(provider.city, provider.state, provider.country) }}</span>
      </div>

      <!-- Features -->
      <div class="provider-features">
        <div v-if="provider.allowOnlineBooking" class="feature-badge">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
            />
          </svg>
          <span>Online Booking</span>
        </div>

        <div v-if="provider.offersMobileServices" class="feature-badge">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M5 8h14M5 8a2 2 0 110-4h14a2 2 0 110 4M5 8v10a2 2 0 002 2h10a2 2 0 002-2V8m-9 4h4"
            />
          </svg>
          <span>Mobile Service</span>
        </div>

        <!-- Staff Count Badge for Organizations -->
        <div v-if="provider.staffCount && provider.staffCount > 0" class="feature-badge staff-badge">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
            />
          </svg>
          <span>{{ provider.staffCount }} Staff</span>
        </div>
      </div>

      <!-- Professionals Preview (for Organizations) -->
      <div v-if="isOrganization && staffMembers.length > 0" class="professionals-preview">
        <div class="professionals-header">
          <span class="professionals-label">متخصصین این مرکز:</span>
        </div>
        <div class="professionals-avatars">
          <div
            v-for="(staff, index) in visibleStaff"
            :key="staff.id"
            class="professional-avatar"
            :title="getStaffDisplayName(staff)"
          >
            <img
              v-if="staff.photoUrl"
              :src="staff.photoUrl"
              :alt="getStaffDisplayName(staff)"
              @error="handleImageError"
            />
            <span v-else class="avatar-initials" :style="{ background: getAvatarColor(index) }">
              {{ getStaffInitials(staff) }}
            </span>
          </div>
          <div v-if="hasMoreStaff" class="professional-avatar more-indicator">
            <span>+{{ remainingStaffCount }}</span>
          </div>
        </div>
      </div>

      <!-- Tags -->
      <div v-if="provider.tags && provider.tags.length > 0" class="provider-tags">
        <span v-for="(tag, index) in visibleTags" :key="index" class="tag">
          {{ tag }}
        </span>
        <span v-if="hasMoreTags" class="tag-more"> +{{ remainingTagsCount }} </span>
      </div>

      <!-- Footer -->
      <div class="provider-footer">
        <div class="provider-meta">
          <span class="meta-item">
            <svg
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            {{ formatDate(provider.registeredAt) }}
          </span>
        </div>

        <button v-if="showBookButton" class="btn-book" @click.stop="handleBookClick">
          Book Now
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Badge } from '../../../shared/components'
import { buildProviderImageUrl } from '@/core/utils/url.service'
import type { ProviderSummary } from '../types/provider.types'

// Props
interface Props {
  provider: ProviderSummary
  viewMode?: 'grid' | 'list'
  isClickable?: boolean
  showStatus?: boolean
  showBookButton?: boolean
  maxTags?: number
  descriptionLength?: number
}

const props = withDefaults(defineProps<Props>(), {
  viewMode: 'grid',
  isClickable: true,
  showStatus: false,
  showBookButton: true,
  maxTags: 3,
  descriptionLength: 120,
})

// Emits
const emit = defineEmits<{
  (e: 'click', provider: ProviderSummary): void
  (e: 'book', provider: ProviderSummary): void
}>()

// Computed
const providerImageUrl = computed(() => {

  // Use centralized URL service to build image URL
  // Automatically handles relative paths, absolute URLs, and API base URL
  return buildProviderImageUrl(props.provider.profileImageUrl, props.provider.logoUrl)
})

const visibleTags = computed(() => {
  return props.provider.tags.slice(0, props.maxTags)
})

const hasMoreTags = computed(() => {
  return props.provider.tags.length > props.maxTags
})

const remainingTagsCount = computed(() => {
  return props.provider.tags.length - props.maxTags
})

const isOrganization = computed(() => {
  return props.provider.hierarchyType === 'Organization'
})

const staffCount = computed(() => {
  return props.provider.staffCount || 0
})

const staffMembers = computed(() => {
  return props.provider.staff || []
})

const maxVisibleStaff = 4

const visibleStaff = computed(() => {
  return staffMembers.value.slice(0, maxVisibleStaff)
})

const hasMoreStaff = computed(() => {
  return staffMembers.value.length > maxVisibleStaff
})

const remainingStaffCount = computed(() => {
  return staffMembers.value.length - maxVisibleStaff
})

// Methods
const handleClick = () => {
  if (props.isClickable) {
    emit('click', props.provider)
  }
}

const handleBookClick = () => {
  emit('book', props.provider)
}

const handleImageError = (event: Event) => {
  const img = event.target as HTMLImageElement
  img.style.display = 'none'
}

const getInitials = (name: string): string => {
  return name
    .split(' ')
    .map((word) => word[0])
    .join('')
    .toUpperCase()
    .slice(0, 2)
}

const getTypeVariant = (type: string): 'primary' | 'success' | 'warning' | 'info' | 'secondary' => {
  const variants: Record<string, 'primary' | 'success' | 'warning' | 'info' | 'secondary'> = {
    Individual: 'info',
    Salon: 'success',
    Clinic: 'warning',
    Spa: 'success',
    Studio: 'info',
    Professional: 'secondary',
  }
  return variants[type] || 'secondary'
}

const truncateText = (text: string, maxLength: number): string => {
  if (text.length <= maxLength) return text
  return text.slice(0, maxLength).trim() + '...'
}

const formatLocation = (city: string, state: string, country: string): string => {
  const parts = [city, state, country].filter(Boolean)
  return parts.join(', ')
}

const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  const now = new Date()
  const diffTime = Math.abs(now.getTime() - date.getTime())
  const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24))

  if (diffDays < 30) {
    return `${diffDays} days ago`
  } else if (diffDays < 365) {
    const months = Math.floor(diffDays / 30)
    return `${months} month${months > 1 ? 's' : ''} ago`
  } else {
    const years = Math.floor(diffDays / 365)
    return `${years} year${years > 1 ? 's' : ''} ago`
  }
}

const getStaffDisplayName = (staff: any): string => {
  return `${staff.firstName || ''} ${staff.lastName || ''}`.trim() || 'Unknown'
}

const getStaffInitials = (staff: any): string => {
  const first = staff.firstName?.charAt(0) || ''
  const last = staff.lastName?.charAt(0) || ''
  return `${first}${last}`.toUpperCase() || '??'
}

const avatarColors = [
  'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
  'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
  'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
  'linear-gradient(135deg, #43e97b 0%, #38f9d7 100%)',
  'linear-gradient(135deg, #fa709a 0%, #fee140 100%)',
]

const getAvatarColor = (index: number): string => {
  return avatarColors[index % avatarColors.length]
}
</script>

<style scoped>
.provider-card {
  background: white;
  border: 1px solid var(--color-border);
  border-radius: 12px;
  overflow: hidden;
  transition: all 0.3s ease;
}

.provider-card.clickable {
  cursor: pointer;
}

.provider-card.clickable:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.1);
  border-color: var(--color-primary);
}

/* Grid View (Default) */
.provider-card.view-mode-grid {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.provider-card.view-mode-grid .provider-image {
  position: relative;
  width: 100%;
  height: 200px;
  overflow: hidden;
  background: var(--color-bg-secondary);
}

.provider-card.view-mode-grid .provider-content {
  padding: 1.25rem;
  flex: 1;
  display: flex;
  flex-direction: column;
}

/* List View */
.provider-card.view-mode-list {
  display: flex;
  flex-direction: row;
  height: auto;
}

.provider-card.view-mode-list .provider-image {
  position: relative;
  width: 200px;
  height: 200px;
  flex-shrink: 0;
  overflow: hidden;
  background: var(--color-bg-secondary);
}

.provider-card.view-mode-list .provider-content {
  padding: 1.5rem;
  flex: 1;
  display: flex;
  flex-direction: column;
}

/* Image */
.provider-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.placeholder-logo {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark) 100%);
}

.initials {
  font-size: 3rem;
  font-weight: 700;
  color: white;
}

.status-badge {
  position: absolute;
  top: 0.75rem;
  right: 0.75rem;
  padding: 0.25rem 0.75rem;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  background: white;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.status-badge.status-active {
  color: var(--color-success);
}

.status-badge.status-pending {
  color: var(--color-warning);
}

.status-badge.status-inactive,
.status-badge.status-suspended {
  color: var(--color-danger);
}

/* Content */
.provider-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
}

.provider-name {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--color-text-primary);
  margin: 0;
  line-height: 1.3;
  flex: 1;
}

.badges-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  align-items: flex-end;
}

.organization-badge,
.staff-count-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  font-size: 0.75rem;
}

.badge-icon {
  width: 14px;
  height: 14px;
}

.provider-description {
  font-size: 0.95rem;
  color: var(--color-text-secondary);
  line-height: 1.5;
  margin: 0 0 1rem 0;
}

.provider-location {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
  color: var(--color-text-secondary);
  margin-bottom: 1rem;
}

.provider-location svg {
  width: 16px;
  height: 16px;
  flex-shrink: 0;
}

/* Features */
.provider-features {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.feature-badge {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.375rem 0.75rem;
  background: var(--color-bg-secondary);
  border-radius: 6px;
  font-size: 0.8rem;
  font-weight: 500;
  color: var(--color-text-secondary);
}

.feature-badge svg {
  width: 14px;
  height: 14px;
}

.feature-badge.staff-badge {
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.1) 0%, rgba(139, 92, 246, 0.1) 100%);
  color: var(--color-primary);
  border: 1px solid rgba(99, 102, 241, 0.2);
}

.feature-badge.staff-badge svg {
  color: var(--color-primary);
}

/* Professionals Preview */
.professionals-preview {
  margin-bottom: 1rem;
  padding: 0.75rem;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.05) 0%, rgba(118, 75, 162, 0.05) 100%);
  border-radius: 10px;
  border: 1px solid rgba(102, 126, 234, 0.1);
}

.professionals-header {
  margin-bottom: 0.5rem;
}

.professionals-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--color-text-secondary);
}

.professionals-avatars {
  display: flex;
  gap: 0.25rem;
  align-items: center;
}

.professional-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  overflow: hidden;
  border: 2px solid white;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.1);
  margin-right: -8px;
  transition: transform 0.2s;
  cursor: pointer;
}

.professional-avatar:first-child {
  margin-right: 0;
}

.professional-avatar:hover {
  transform: scale(1.1);
  z-index: 1;
}

.professional-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.professional-avatar .avatar-initials {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.75rem;
  font-weight: 600;
  color: white;
}

.professional-avatar.more-indicator {
  background: linear-gradient(135deg, #e2e8f0 0%, #cbd5e1 100%);
  display: flex;
  align-items: center;
  justify-content: center;
}

.professional-avatar.more-indicator span {
  font-size: 0.7rem;
  font-weight: 700;
  color: #64748b;
}

/* Tags */
.provider-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.tag {
  padding: 0.25rem 0.625rem;
  background: var(--color-primary-light);
  color: var(--color-primary-dark);
  border-radius: 4px;
  font-size: 0.8rem;
  font-weight: 500;
}

.tag-more {
  padding: 0.25rem 0.625rem;
  background: var(--color-bg-tertiary);
  color: var(--color-text-secondary);
  border-radius: 4px;
  font-size: 0.8rem;
  font-weight: 500;
}

/* Footer */
.provider-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: auto;
  padding-top: 1rem;
  border-top: 1px solid var(--color-border);
}

.provider-meta {
  display: flex;
  gap: 1rem;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  font-size: 0.85rem;
  color: var(--color-text-tertiary);
}

.meta-item svg {
  width: 14px;
  height: 14px;
}

.btn-book {
  padding: 0.5rem 1.25rem;
  background: var(--color-primary);
  color: white;
  border: none;
  border-radius: 6px;
  font-size: 0.9rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-book:hover {
  background: var(--color-primary-dark);
  transform: scale(1.05);
}

/* Responsive */
@media (max-width: 768px) {
  .provider-card.view-mode-list {
    flex-direction: column;
  }

  .provider-card.view-mode-list .provider-image {
    width: 100%;
    height: 180px;
  }

  .provider-footer {
    flex-direction: column;
    gap: 1rem;
    align-items: stretch;
  }

  .btn-book {
    width: 100%;
  }
}
</style>
