<template>
  <div class="staff-member-card" :class="{ inactive: !staff.isActive }">
    <!-- Header with Avatar -->
    <div class="card-header">
      <div class="avatar-section">
        <div v-if="staff.photoUrl" class="avatar">
          <img :src="staff.photoUrl" :alt="staff.fullName" />
        </div>
        <div v-else class="avatar avatar-placeholder">
          <span>{{ initials }}</span>
        </div>

        <div class="status-badge" :class="statusClass">
          <span class="status-dot"></span>
          <span class="status-text">{{ statusText }}</span>
        </div>
      </div>

      <div class="card-menu">
        <button class="menu-button" @click="toggleMenu" title="منوی بیشتر">
          <i class="icon-more-vertical">⋮</i>
        </button>

        <div v-if="showMenu" class="menu-dropdown">
          <button class="menu-item" @click="handleView">
            <i class="icon-eye"></i>
            <span>مشاهده جزئیات</span>
          </button>
          <button class="menu-item danger" @click="handleRemove">
            <i class="icon-trash"></i>
            <span>حذف کارمند</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Staff Info -->
    <div class="card-body">
      <h3 class="staff-name">{{ staff.fullName || `${staff.firstName} ${staff.lastName}` }}</h3>

      <div v-if="staff.title" class="staff-title">
        {{ staff.title }}
      </div>

      <!-- Contact Info -->
      <div class="contact-info">
        <div v-if="staff.email" class="contact-item">
          <i class="icon-mail"></i>
          <a :href="`mailto:${staff.email}`" class="contact-link">{{ staff.email }}</a>
        </div>
        <div v-if="staff.phoneNumber" class="contact-item">
          <i class="icon-phone"></i>
          <a :href="`tel:${staff.phoneNumber}`" class="contact-link" dir="ltr">{{ staff.phoneNumber }}</a>
        </div>
      </div>

      <!-- Specializations -->
      <div v-if="staff.specializations.length > 0" class="specializations">
        <div class="section-label">تخصص‌ها:</div>
        <div class="tags">
          <span
            v-for="specialization in staff.specializations.slice(0, 3)"
            :key="specialization"
            class="tag"
          >
            {{ specialization }}
          </span>
          <span v-if="staff.specializations.length > 3" class="tag tag-more">
            +{{ staff.specializations.length - 3 }}
          </span>
        </div>
      </div>

      <!-- Services Count -->
      <div class="services-info">
        <i class="icon-briefcase"></i>
        <span>تخصص‌ها و خدمات</span>
      </div>

      <!-- Joined Date -->
      <div class="joined-info">
        <i class="icon-calendar"></i>
        <span>پیوسته در {{ formatDate(String(staff.joinedAt)) }}</span>
      </div>
    </div>

    <!-- Card Footer -->
    <div class="card-footer">
      <AppButton variant="ghost" size="small" block @click="handleView">
        مشاهده پروفایل
      </AppButton>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onClickOutside } from '@vueuse/core'
import type { StaffMember } from '../../types/hierarchy.types'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import { getNameInitials, formatDate } from '@/core/utils'

// ============================================
// Props
// ============================================

interface Props {
  staff: StaffMember
}

const props = defineProps<Props>()

// ============================================
// Emits
// ============================================

const emit = defineEmits<{
  (e: 'view', staff: StaffMember): void
  (e: 'remove', staff: StaffMember): void
}>()

// ============================================
// State
// ============================================

const showMenu = ref(false)
const menuRef = ref<HTMLElement | null>(null)

// ============================================
// Computed
// ============================================

const initials = computed(() => {
  // Fallback to firstName + lastName if fullName is not available
  const fullName = props.staff.fullName || `${props.staff.firstName || ''} ${props.staff.lastName || ''}`.trim()

  return fullName ? getNameInitials(fullName) : '??'
})

const statusClass = computed(() => {
  return props.staff.isActive ? 'status-active' : 'status-inactive'
})

const statusText = computed(() => {
  return props.staff.isActive ? 'فعال' : 'غیرفعال'
})

// ============================================
// Methods
// ============================================

function toggleMenu(): void {
  showMenu.value = !showMenu.value
}

function handleView(): void {
  showMenu.value = false
  emit('view', props.staff)
}

function handleRemove(): void {
  showMenu.value = false
  emit('remove', props.staff)
}

// Close menu when clicking outside
onClickOutside(menuRef, () => {
  showMenu.value = false
})
</script>

<style scoped lang="scss">
.staff-member-card {
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  overflow: hidden;
  transition: all 0.3s ease;

  &:hover {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    transform: translateY(-2px);
  }

  &.inactive {
    opacity: 0.7;
    background: #f9fafb;
  }
}

.card-header {
  padding: 1.5rem;
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  border-bottom: 1px solid #f3f4f6;
}

.avatar-section {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
}

.avatar {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  overflow: hidden;
  border: 3px solid #f3f4f6;

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
    font-size: 1.75rem;
    font-weight: 700;
    color: #fff;
  }
}

.status-badge {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.375rem 0.75rem;
  border-radius: 12px;
  font-size: 0.75rem;
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
  width: 6px;
  height: 6px;
  border-radius: 50%;
}

.card-menu {
  position: relative;
}

.menu-button {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid #e5e7eb;
  background: #fff;
  border-radius: 6px;
  cursor: pointer;
  color: #6b7280;
  transition: all 0.2s;
  font-size: 1.25rem;
  font-weight: bold;

  &:hover {
    background: #f3f4f6;
    color: #1a1a1a;
    border-color: #d1d5db;
  }

  i {
    font-style: normal;
  }
}

.menu-dropdown {
  position: absolute;
  top: 100%;
  left: 0;
  margin-top: 0.5rem;
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  min-width: 180px;
  z-index: 10;
  overflow: hidden;
}

.menu-item {
  width: 100%;
  padding: 0.75rem 1rem;
  border: none;
  background: transparent;
  text-align: right;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-size: 0.9rem;
  color: #374151;
  transition: background 0.2s;

  &:hover {
    background: #f9fafb;
  }

  &.danger {
    color: #dc2626;

    &:hover {
      background: #fee2e2;
    }
  }

  i {
    font-size: 1rem;
  }
}

.menu-divider {
  height: 1px;
  background: #e5e7eb;
  margin: 0.25rem 0;
}

.card-body {
  padding: 1.5rem;
}

.staff-name {
  font-size: 1.25rem;
  font-weight: 700;
  color: #1a1a1a;
  margin-bottom: 0.25rem;
}

.staff-title {
  font-size: 0.95rem;
  color: #6b7280;
  margin-bottom: 1rem;
}

.contact-info {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.contact-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #6b7280;

  i {
    font-size: 0.875rem;
    color: #9ca3af;
  }
}

.contact-link {
  color: #7c3aed;
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}

.specializations {
  margin-bottom: 1rem;
}

.section-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: #6b7280;
  margin-bottom: 0.5rem;
}

.tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.tag {
  padding: 0.375rem 0.75rem;
  background: #f3f4f6;
  border-radius: 6px;
  font-size: 0.8rem;
  color: #4b5563;

  &.tag-more {
    background: #e5e7eb;
    font-weight: 600;
  }
}

.services-info,
.joined-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #6b7280;
  margin-top: 0.75rem;

  i {
    font-size: 0.875rem;
    color: #9ca3af;
  }
}

.card-footer {
  padding: 1rem 1.5rem;
  background: #f9fafb;
  border-top: 1px solid #e5e7eb;
}
</style>
