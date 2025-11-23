<template>
  <div class="profile-team" dir="rtl">
    <!-- Header -->
    <div class="team-header">
      <h2 class="team-title">تیم ما</h2>
      <p class="team-subtitle">
        متخصصین حرفه‌ای آماده ارائه بهترین خدمات به شما
      </p>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="loading-state">
      <div class="spinner"></div>
      <p>در حال بارگذاری...</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="!staffMembers || staffMembers.length === 0" class="empty-state">
      <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
      </svg>
      <h3>هنوز عضوی به تیم اضافه نشده است</h3>
    </div>

    <!-- Team Members Grid -->
    <div v-else class="team-grid">
      <div
        v-for="member in staffMembers"
        :key="member.id"
        class="team-member-card"
      >
        <!-- Avatar -->
        <div class="member-avatar">
          <div class="avatar-circle">
            <span class="avatar-initials">{{ getInitials(member) }}</span>
          </div>
          <div v-if="member.isActive" class="status-indicator" title="فعال"></div>
        </div>

        <!-- Info -->
        <div class="member-info">
          <h3 class="member-name">{{ getMemberName(member) }}</h3>
          <p v-if="member.role" class="member-role">{{ member.role }}</p>
          <p v-if="member.specialties && member.specialties.length > 0" class="member-specialties">
            {{ member.specialties.join('، ') }}
          </p>
        </div>

        <!--Stats -->
        <div class="member-stats">
          <div v-if="member.averageRating" class="stat-item">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10.868 2.884c-.321-.772-1.415-.772-1.736 0l-1.83 4.401-4.753.381c-.833.067-1.171 1.107-.536 1.651l3.62 3.102-1.106 4.637c-.194.813.691 1.456 1.405 1.02L10 15.591l4.069 2.485c.713.436 1.598-.207 1.404-1.02l-1.106-4.637 3.62-3.102c.635-.544.297-1.584-.536-1.65l-4.752-.382-1.831-4.401z" clip-rule="evenodd" />
            </svg>
            <span>{{ formatRating(member.averageRating) }}</span>
          </div>
          <div v-if="member.totalBookings" class="stat-item">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z" clip-rule="evenodd" />
            </svg>
            <span>{{ convertToPersian(member.totalBookings) }} رزرو</span>
          </div>
        </div>

        <!-- Action Button -->
        <button
          class="btn-book"
          @click="$emit('book-with-staff', member.id)"
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
            <path d="M10 2a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 2zM10 15a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 15zM10 7a3 3 0 100 6 3 3 0 000-6zM15.657 5.404a.75.75 0 10-1.06-1.06l-1.061 1.06a.75.75 0 001.06 1.06l1.06-1.06zM6.464 14.596a.75.75 0 10-1.06-1.06l-1.06 1.06a.75.75 0 001.06 1.06l1.06-1.06zM18 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 0118 10zM5 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 015 10zM14.596 15.657a.75.75 0 001.06-1.06l-1.06-1.061a.75.75 0 10-1.061 1.06l1.06 1.06zM5.404 6.464a.75.75 0 001.06-1.06l-1.06-1.06a.75.75 0 10-1.061 1.06l1.06 1.06z" />
          </svg>
          رزرو با این متخصص
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useHierarchyStore } from '@/modules/provider/stores/hierarchy.store'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'
import type { StaffMember } from '@/modules/provider/types/hierarchy.types'
import type { Provider } from '@/modules/provider/types/provider.types'

interface Props {
  provider: Provider
}

const props = defineProps<Props>()

defineEmits<{
  'book-with-staff': [staffId: string]
}>()

const hierarchyStore = useHierarchyStore()

const isLoading = ref(false)
const staffMembers = ref<StaffMember[]>([])

// Get initials from name
const getInitials = (member: StaffMember): string => {
  const nameParts = member.fullName.split(' ')
  if (nameParts.length >= 2) {
    return (nameParts[0][0] + nameParts[nameParts.length - 1][0]).toUpperCase()
  }
  return member.fullName.substring(0, 2).toUpperCase()
}

// Get full member name
const getMemberName = (member: StaffMember): string => {
  return member.fullName || 'بدون نام'
}

// Format rating
const formatRating = (rating: number): string => {
  return convertToPersian(rating.toFixed(1))
}

// Convert to Persian numbers
const convertToPersian = (value: number | string): string => {
  return convertEnglishToPersianNumbers(value.toString())
}

// Load staff members
onMounted(async () => {
  if (!props.provider?.id) return

  isLoading.value = true
  try {
    await hierarchyStore.loadProviderHierarchy(props.provider.id)
    const hierarchy = hierarchyStore.currentHierarchy

    if (hierarchy?.staffMembers) {
      // Filter to show only active staff members
      staffMembers.value = hierarchy.staffMembers.filter(
        (member: StaffMember) => member.isActive
      )
    }
  } catch (error) {
    console.error('Error loading staff members:', error)
  } finally {
    isLoading.value = false
  }
})
</script>

<style scoped lang="scss">
.profile-team {
  display: flex;
  flex-direction: column;
  gap: 32px;
}

.team-header {
  text-align: center;
  padding: 20px 0;
}

.team-title {
  font-size: 28px;
  font-weight: 700;
  color: #1f2937;
  margin: 0 0 12px 0;
}

.team-subtitle {
  font-size: 16px;
  color: #6b7280;
  margin: 0;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: #6b7280;

  .spinner {
    width: 48px;
    height: 48px;
    border: 4px solid #e5e7eb;
    border-top-color: #667eea;
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin-bottom: 16px;
  }

  p {
    font-size: 14px;
    margin: 0;
  }
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  text-align: center;

  svg {
    width: 64px;
    height: 64px;
    color: #9ca3af;
    margin-bottom: 16px;
  }

  h3 {
    font-size: 18px;
    font-weight: 600;
    color: #374151;
    margin: 0;
  }
}

.team-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 300px), 1fr));
  gap: 24px;
}

.team-member-card {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 16px;
  padding: 24px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
  transition: all 0.3s ease;

  &:hover {
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.1);
    transform: translateY(-4px);
  }
}

.member-avatar {
  position: relative;
}

.avatar-circle {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
}

.avatar-initials {
  font-size: 24px;
  font-weight: 700;
  color: white;
}

.status-indicator {
  position: absolute;
  bottom: 4px;
  left: 4px;
  width: 16px;
  height: 16px;
  background: #10b981;
  border: 3px solid white;
  border-radius: 50%;
}

.member-info {
  text-align: center;
  width: 100%;
}

.member-name {
  font-size: 18px;
  font-weight: 700;
  color: #1f2937;
  margin: 0 0 8px 0;
}

.member-role {
  font-size: 14px;
  font-weight: 600;
  color: #667eea;
  margin: 0 0 8px 0;
}

.member-specialties {
  font-size: 13px;
  color: #6b7280;
  margin: 0;
  line-height: 1.5;
}

.member-stats {
  display: flex;
  gap: 16px;
  padding: 12px 0;
  border-top: 1px solid #e5e7eb;
  border-bottom: 1px solid #e5e7eb;
  width: 100%;
  justify-content: center;
}

.stat-item {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  color: #6b7280;
  font-weight: 500;

  svg {
    width: 16px;
    height: 16px;
    color: #f59e0b;
  }
}

.btn-book {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 12px 24px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 12px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
  font-family: 'B Nazanin', sans-serif;

  svg {
    width: 18px;
    height: 18px;
  }

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 8px 20px rgba(102, 126, 234, 0.4);
  }

  &:active {
    transform: translateY(0);
  }
}

// Responsive design
@media (max-width: 768px) {
  .team-grid {
    grid-template-columns: 1fr;
  }

  .team-title {
    font-size: 24px;
  }

  .team-member-card {
    padding: 20px;
  }

  .avatar-circle {
    width: 64px;
    height: 64px;
  }

  .avatar-initials {
    font-size: 20px;
  }
}
</style>
