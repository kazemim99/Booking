<template>
  <div v-if="isOpen" class="modal-overlay" @click="handleClose">
    <div class="modal-container" @click.stop>
      <div class="modal-header">
        <h2 class="modal-title">جزئیات کارمند</h2>
        <button class="close-button" @click="handleClose" title="بستن">
          ×
        </button>
      </div>

      <div class="modal-body">
        <!-- Avatar and Name -->
        <div class="staff-profile">
          <div v-if="staff.photoUrl" class="staff-avatar">
            <img :src="staff.photoUrl" :alt="staff.fullName" />
          </div>
          <div v-else class="staff-avatar avatar-placeholder">
            <span>{{ initials }}</span>
          </div>

          <h3 class="staff-name">{{ staff.fullName }}</h3>
          <div class="status-badge" :class="statusClass">
            <span class="status-dot"></span>
            <span class="status-text">{{ statusText }}</span>
          </div>
        </div>

        <!-- Details Grid -->
        <div class="details-grid">
          <!-- Contact Information -->
          <div class="detail-section">
            <h4 class="section-title">اطلاعات تماس</h4>
            <div class="detail-items">
              <div v-if="staff.phoneNumber" class="detail-item">
                <span class="detail-label">شماره تماس:</span>
                <span class="detail-value" dir="ltr">{{ staff.phoneNumber }}</span>
              </div>
              <div v-if="staff.email" class="detail-item">
                <span class="detail-label">ایمیل:</span>
                <span class="detail-value">{{ staff.email }}</span>
              </div>
            </div>
          </div>

          <!-- Employment Information -->
          <div class="detail-section">
            <h4 class="section-title">اطلاعات شغلی</h4>
            <div class="detail-items">
              <div class="detail-item">
                <span class="detail-label">نقش:</span>
                <span class="detail-value">{{ staff.role }}</span>
              </div>
              <div v-if="staff.title" class="detail-item">
                <span class="detail-label">عنوان:</span>
                <span class="detail-value">{{ staff.title }}</span>
              </div>
              <div class="detail-item">
                <span class="detail-label">تاریخ پیوستن:</span>
                <span class="detail-value">{{ formatDate(staff.joinedAt) }}</span>
              </div>
            </div>
          </div>

          <!-- Bio -->
          <div v-if="staff.bio" class="detail-section full-width">
            <h4 class="section-title">درباره</h4>
            <p class="bio-text">{{ staff.bio }}</p>
          </div>

          <!-- Specializations -->
          <div v-if="staff.specializations && staff.specializations.length > 0" class="detail-section full-width">
            <h4 class="section-title">تخصص‌ها</h4>
            <div class="tags">
              <span v-for="spec in staff.specializations" :key="spec" class="tag">
                {{ spec }}
              </span>
            </div>
          </div>
        </div>
      </div>

      <div class="modal-footer">
        <AppButton variant="outline" @click="handleClose">
          بستن
        </AppButton>
        <AppButton variant="primary" @click="viewProfile">
          مشاهده پروفایل کامل
        </AppButton>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { StaffMember } from '../../types/hierarchy.types'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'

// ============================================
// Props
// ============================================

interface Props {
  isOpen: boolean
  staff: StaffMember | null
}

const props = defineProps<Props>()

// ============================================
// Emits
// ============================================

const emit = defineEmits<{
  (e: 'close'): void
}>()

// ============================================
// Computed
// ============================================

const initials = computed(() => {
  if (!props.staff) return '??'

  const fullName = props.staff.fullName || `${props.staff.firstName || ''} ${props.staff.lastName || ''}`.trim()

  if (!fullName) return '??'

  const names = fullName.split(' ')
  if (names.length >= 2) {
    return `${names[0][0]}${names[1][0]}`.toUpperCase()
  }
  return fullName.substring(0, 2).toUpperCase()
})

const statusClass = computed(() => {
  return props.staff?.isActive ? 'status-active' : 'status-inactive'
})

const statusText = computed(() => {
  return props.staff?.isActive ? 'فعال' : 'غیرفعال'
})

// ============================================
// Methods
// ============================================

function handleClose(): void {
  emit('close')
}

function viewProfile(): void {
  if (props.staff) {
    window.open(`/provider/${props.staff.providerId}`, '_blank')
  }
}

function formatDate(dateString: Date | string): string {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('fa-IR', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  }).format(date)
}
</script>

<style scoped lang="scss">
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-container {
  background: #fff;
  border-radius: 16px;
  width: 100%;
  max-width: 600px;
  max-height: 90vh;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}

.modal-header {
  padding: 1.5rem;
  border-bottom: 1px solid #e5e7eb;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1a1a1a;
  margin: 0;
}

.close-button {
  width: 36px;
  height: 36px;
  border: none;
  background: transparent;
  border-radius: 8px;
  cursor: pointer;
  font-size: 2rem;
  line-height: 1;
  color: #6b7280;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;

  &:hover {
    background: #f3f4f6;
    color: #1a1a1a;
  }
}

.modal-body {
  padding: 2rem;
  overflow-y: auto;
  flex: 1;
}

.staff-profile {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-bottom: 2rem;
  padding-bottom: 2rem;
  border-bottom: 1px solid #e5e7eb;
}

.staff-avatar {
  width: 100px;
  height: 100px;
  border-radius: 50%;
  overflow: hidden;
  border: 4px solid #f3f4f6;
  margin-bottom: 1rem;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  &.avatar-placeholder {
    background: linear-gradient(135deg, #7c3aed 0%, #9333ea 100%);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 2.5rem;
    font-weight: 700;
    color: #fff;
  }
}

.staff-name {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1a1a1a;
  margin: 0 0 0.5rem 0;
}

.status-badge {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  border-radius: 12px;
  font-size: 0.875rem;
  font-weight: 600;

  &.status-active {
    background: #d1fae5;
    color: #065f46;

    .status-dot {
      background: #10b981;
    }
  }

  &.status-inactive {
    background: #fee2e2;
    color: #991b1b;

    .status-dot {
      background: #ef4444;
    }
  }
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}

.details-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
}

.detail-section {
  &.full-width {
    grid-column: 1 / -1;
  }
}

.section-title {
  font-size: 1rem;
  font-weight: 600;
  color: #374151;
  margin: 0 0 1rem 0;
}

.detail-items {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.detail-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.detail-label {
  font-size: 0.875rem;
  color: #6b7280;
  font-weight: 500;
}

.detail-value {
  font-size: 1rem;
  color: #1a1a1a;
  font-weight: 400;
}

.bio-text {
  font-size: 0.95rem;
  color: #4b5563;
  line-height: 1.6;
  margin: 0;
}

.tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.tag {
  padding: 0.5rem 1rem;
  background: #f3f4f6;
  border-radius: 8px;
  font-size: 0.875rem;
  color: #4b5563;
  font-weight: 500;
}

.modal-footer {
  padding: 1.5rem;
  border-top: 1px solid #e5e7eb;
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
}
</style>
