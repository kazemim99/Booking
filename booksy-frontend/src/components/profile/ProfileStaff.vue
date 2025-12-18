<template>
  <section class="profile-staff" dir="rtl">
    <div class="staff-header">
      <h2 class="section-title">متخصصین ما</h2>
      <p class="section-subtitle">
        تیم متخصصین ما آماده ارائه خدمات با کیفیت به شما هستند
      </p>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="loading-container">
      <div class="loading-spinner"></div>
      <p>در حال بارگذاری...</p>
    </div>

    <!-- Staff Grid -->
    <div v-else-if="staffMembers.length > 0" class="staff-grid">
      <div
        v-for="(staff, index) in staffMembers"
        :key="getStaffId(staff)"
        class="staff-card"
        :class="{ selected: selectedStaffId === getStaffId(staff) }"
        :style="{ animationDelay: `${index * 0.1}s` }"
        @click="handleStaffSelect(staff)"
      >
        <!-- Staff Avatar -->
        <div class="staff-avatar">
          <img
            v-if="getStaffPhotoUrl(staff)"
            :src="getStaffPhotoUrl(staff)"
            :alt="getStaffName(staff)"
            @error="handleImageError"
          />
          <div v-else class="avatar-placeholder" :style="{ background: getAvatarGradient(index) }">
            <span class="avatar-initials">{{ getInitials(staff) }}</span>
          </div>
          <div v-if="isStaffActive(staff)" class="status-indicator active"></div>
          <div v-else class="status-indicator inactive"></div>
        </div>

        <!-- Staff Info -->
        <div class="staff-info">
          <h3 class="staff-name">{{ getStaffName(staff) }}</h3>
          <p v-if="'title' in staff && staff.title" class="staff-title">{{ staff.title }}</p>

          <!-- For StaffProvider: Show rating and service count -->
          <div v-if="isDisplayingStaffProviders && 'averageRating' in staff" class="staff-stats">
            <span v-if="staff.averageRating > 0" class="stat-item">
              ⭐ {{ staff.averageRating.toFixed(1) }}
            </span>
            <span v-if="staff.serviceCount > 0" class="stat-item">
              {{ staff.serviceCount }} خدمت
            </span>
          </div>

          <p v-if="staff.bio" class="staff-bio">{{ truncateText(staff.bio, 80) }}</p>

          <!-- Specializations -->
          <div v-if="staff.specializations && staff.specializations.length > 0" class="staff-specializations">
            <span v-for="spec in staff.specializations.slice(0, 3)" :key="spec" class="spec-tag">
              {{ spec }}
            </span>
            <span v-if="staff.specializations.length > 3" class="spec-more">
              +{{ staff.specializations.length - 3 }}
            </span>
          </div>
        </div>

        <!-- Selection Indicator -->
        <div v-if="selectable" class="selection-indicator">
          <svg v-if="selectedStaffId === getStaffId(staff)" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
            <path fill-rule="evenodd" d="M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12zm13.36-1.814a.75.75 0 10-1.22-.872l-3.236 4.53L9.53 12.22a.75.75 0 00-1.06 1.06l2.25 2.25a.75.75 0 001.14-.094l3.75-5.25z" clip-rule="evenodd" />
          </svg>
          <span v-else class="select-text">انتخاب</span>
        </div>

        <!-- Book Button (when not selectable) -->
        <button
          v-if="!selectable && provider.allowOnlineBooking"
          class="btn-book-staff"
          @click.stop="handleBookWithStaff(staff)"
        >
          رزرو با این متخصص
        </button>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="empty-state">
      <div class="empty-icon">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
        </svg>
      </div>
      <h3>متخصصی ثبت نشده است</h3>
      <p>این ارائه‌دهنده هنوز اطلاعات متخصصین خود را ثبت نکرده است.</p>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import type { Provider, StaffMember, StaffProvider } from '@/modules/provider/types/provider.types'

interface Props {
  provider: Provider
  selectable?: boolean // Enable selection mode for booking flow
  selectedStaffId?: string | null
}

const props = withDefaults(defineProps<Props>(), {
  selectable: false,
  selectedStaffId: null,
})

const emit = defineEmits<{
  (e: 'staff-selected', staff: StaffMember | StaffProvider): void
  (e: 'book-with-staff', staff: StaffMember | StaffProvider): void
}>()

const router = useRouter()

// State
const loading = ref(false)

// Computed
const staffMembers = computed(() => {
  // For organizations, use staffProviders (individual providers linked to the organization)
  if (props.provider.hierarchyType === 'Organization' && props.provider.staffProviders) {
    return props.provider.staffProviders
  }
  // Otherwise, use traditional staff members (filter to only show active staff)
  return (props.provider.staff || []).filter(s => s.isActive)
})

// Check if we're displaying staff providers (organizations) or traditional staff members
const isDisplayingStaffProviders = computed(() => {
  return props.provider.hierarchyType === 'Organization' && props.provider.staffProviders && props.provider.staffProviders.length > 0
})

// Methods
const getStaffName = (staff: StaffMember | StaffProvider): string => {
  // Check if it's a StaffProvider (has businessName)
  if ('businessName' in staff) {
    return staff.businessName || 'بدون نام'
  }
  // It's a StaffMember (has firstName/lastName)
  return `${staff.firstName} ${staff.lastName}`.trim() || 'بدون نام'
}

const getInitials = (staff: StaffMember | StaffProvider): string => {
  // Check if it's a StaffProvider
  if ('businessName' in staff) {
    const words = staff.businessName.split(' ')
    if (words.length >= 2) {
      return `${words[0].charAt(0)}${words[1].charAt(0)}`.toUpperCase()
    }
    return staff.businessName.substring(0, 2).toUpperCase() || '??'
  }
  // It's a StaffMember
  const first = staff.firstName?.charAt(0) || ''
  const last = staff.lastName?.charAt(0) || ''
  return `${first}${last}`.toUpperCase() || '??'
}

const getStaffId = (staff: StaffMember | StaffProvider): string => {
  // StaffProvider uses providerId, StaffMember uses id
  if ('providerId' in staff) {
    return staff.providerId
  }
  // It's a StaffMember
  return (staff as StaffMember).id
}

const isStaffActive = (staff: StaffMember | StaffProvider): boolean => {
  // StaffProvider checks status, StaffMember checks isActive
  if ('isActive' in staff) {
    return staff.isActive
  }
  // For StaffProvider, consider active if status is Active or PendingVerification
  return staff.status === 'Active' || staff.status === 'PendingVerification'
}

const getStaffPhotoUrl = (staff: StaffMember | StaffProvider): string | undefined => {
  // StaffProvider uses profileImageUrl, StaffMember uses photoUrl
  if ('profileImageUrl' in staff) {
    return staff.profileImageUrl
  }
  // It's a StaffMember
  return (staff as StaffMember).photoUrl
}

const getAvatarGradient = (index: number): string => {
  const gradients = [
    'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
    'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
    'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
    'linear-gradient(135deg, #43e97b 0%, #38f9d7 100%)',
    'linear-gradient(135deg, #fa709a 0%, #fee140 100%)',
    'linear-gradient(135deg, #30cfd0 0%, #330867 100%)',
  ]
  return gradients[index % gradients.length]
}

const truncateText = (text: string, maxLength: number): string => {
  if (!text) return ''
  return text.length > maxLength ? text.substring(0, maxLength) + '...' : text
}

const handleImageError = (event: Event) => {
  const img = event.target as HTMLImageElement
  img.style.display = 'none'
}

const handleStaffSelect = (staff: StaffMember | StaffProvider) => {
  if (props.selectable) {
    emit('staff-selected', staff)
  }
}

const handleBookWithStaff = (staff: StaffMember | StaffProvider) => {
  emit('book-with-staff', staff)
  router.push({
    name: 'NewBooking',
    query: {
      providerId: props.provider.id,
      staffId: getStaffId(staff),
    },
  })
}
</script>

<style scoped>
.profile-staff {
  padding: 2rem 0;
}

.staff-header {
  text-align: center;
  margin-bottom: 3rem;
}

.section-title {
  font-size: 2rem;
  font-weight: 800;
  color: #1e293b;
  margin: 0 0 0.75rem 0;
}

.section-subtitle {
  font-size: 1.05rem;
  color: #64748b;
  margin: 0;
}

/* Loading State */
.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  gap: 1rem;
}

.loading-spinner {
  width: 48px;
  height: 48px;
  border: 4px solid #e2e8f0;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* Staff Grid */
.staff-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 2rem;
}

.staff-card {
  background: white;
  border-radius: 20px;
  padding: 2rem;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  animation: fadeInScale 0.5s ease-out;
  animation-fill-mode: both;
  cursor: pointer;
  position: relative;
}

@keyframes fadeInScale {
  from {
    opacity: 0;
    transform: scale(0.95);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}

.staff-card:hover {
  transform: translateY(-8px);
  box-shadow: 0 12px 32px rgba(102, 126, 234, 0.2);
}

.staff-card.selected {
  border: 2px solid #667eea;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.05) 0%, rgba(118, 75, 162, 0.05) 100%);
}

/* Staff Avatar */
.staff-avatar {
  position: relative;
  width: 120px;
  height: 120px;
  margin-bottom: 1.5rem;
}

.staff-avatar img,
.avatar-placeholder {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  object-fit: cover;
}

.avatar-placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
}

.avatar-initials {
  font-size: 2.5rem;
  font-weight: 800;
  color: white;
}

.status-indicator {
  position: absolute;
  bottom: 8px;
  right: 8px;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  border: 3px solid white;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
}

.status-indicator.active {
  background: #10b981;
}

.status-indicator.inactive {
  background: #94a3b8;
}

/* Staff Info */
.staff-info {
  flex: 1;
  width: 100%;
}

.staff-name {
  font-size: 1.375rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.5rem 0;
}

.staff-title {
  font-size: 0.95rem;
  color: #667eea;
  font-weight: 600;
  margin: 0 0 0.75rem 0;
}

.staff-stats {
  display: flex;
  gap: 1rem;
  justify-content: center;
  margin-bottom: 0.75rem;
}

.stat-item {
  font-size: 0.875rem;
  color: #64748b;
  font-weight: 500;
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
}

.staff-bio {
  font-size: 0.9rem;
  line-height: 1.6;
  color: #64748b;
  margin: 0 0 1rem 0;
}

/* Specializations */
.staff-specializations {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 0.5rem;
  margin-bottom: 1.5rem;
}

.spec-tag {
  padding: 0.375rem 0.875rem;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
  color: #667eea;
  border-radius: 8px;
  font-size: 0.8rem;
  font-weight: 600;
  border: 1px solid rgba(102, 126, 234, 0.2);
}

.spec-more {
  padding: 0.375rem 0.875rem;
  background: #f1f5f9;
  color: #64748b;
  border-radius: 8px;
  font-size: 0.8rem;
  font-weight: 600;
}

/* Selection Indicator */
.selection-indicator {
  margin-top: 1rem;
}

.selection-indicator svg {
  width: 32px;
  height: 32px;
  color: #667eea;
}

.select-text {
  font-size: 0.9rem;
  color: #64748b;
  font-weight: 500;
}

/* Book Button */
.btn-book-staff {
  width: 100%;
  margin-top: 1.5rem;
  padding: 1rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.btn-book-staff:hover {
  transform: scale(1.02);
  box-shadow: 0 6px 20px rgba(102, 126, 234, 0.4);
}

/* Empty State */
.empty-state {
  text-align: center;
  padding: 5rem 2rem;
  background: white;
  border-radius: 24px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.06);
}

.empty-icon {
  margin: 0 auto 1.5rem;
  width: 80px;
  height: 80px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
  border-radius: 50%;
}

.empty-icon svg {
  width: 40px;
  height: 40px;
  color: #667eea;
}

.empty-state h3 {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.75rem 0;
}

.empty-state p {
  font-size: 1rem;
  color: #64748b;
  margin: 0;
}

/* Responsive */
@media (max-width: 768px) {
  .staff-grid {
    grid-template-columns: 1fr;
  }

  .section-title {
    font-size: 1.75rem;
  }

  .staff-avatar {
    width: 100px;
    height: 100px;
  }

  .avatar-initials {
    font-size: 2rem;
  }
}
</style>
