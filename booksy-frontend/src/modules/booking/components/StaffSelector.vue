<template>
  <div class="staff-selector">
    <h3 class="selector-title">انتخاب کارمند</h3>
    <p class="selector-description">لطفاً کارمندی که می‌خواهید خدمت را از او دریافت کنید انتخاب کنید.</p>

    <!-- Loading State -->
    <div v-if="isLoading" class="loading-state">
      <div class="spinner"></div>
      <p>در حال بارگذاری کارمندان...</p>
    </div>

    <!-- Staff List -->
    <div v-else-if="staffMembers.length > 0" class="staff-list">
      <div
        v-for="staff in staffMembers"
        :key="staff.id"
        class="staff-item"
        :class="{ selected: selectedStaffId === staff.id, disabled: !staff.isActive }"
        @click="handleSelect(staff)"
      >
        <div class="staff-avatar">
          <img v-if="staff.photoUrl" :src="staff.photoUrl" :alt="staff.fullName" />
          <div v-else class="avatar-placeholder">
            <i class="icon-user"></i>
          </div>
          <div v-if="selectedStaffId === staff.id" class="selected-badge">
            <i class="icon-check"></i>
          </div>
        </div>

        <div class="staff-info">
          <h4 class="staff-name">{{ staff.fullName }}</h4>
          <p v-if="staff.title" class="staff-title">{{ staff.title }}</p>
          <p v-if="staff.bio" class="staff-bio">{{ staff.bio }}</p>

          <div class="staff-meta">
            <span v-if="staff.specializations?.length" class="meta-item">
              <i class="icon-award"></i>
              {{ staff.specializations.join(', ') }}
            </span>
            <span v-if="staff.servicesCount" class="meta-item">
              <i class="icon-briefcase"></i>
              {{ staff.servicesCount }} خدمت
            </span>
          </div>
        </div>

        <div v-if="!staff.isActive" class="inactive-badge">
          غیرفعال
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="empty-state">
      <i class="icon-users"></i>
      <p>کارمندی در دسترس نیست</p>
    </div>

    <!-- Selected Staff Summary -->
    <div v-if="selectedStaff" class="selected-summary">
      <i class="icon-check-circle"></i>
      <span>کارمند انتخاب شده: <strong>{{ selectedStaff.fullName }}</strong></span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useHierarchyStore } from '@/modules/provider/stores/hierarchy.store'
import type { StaffMember } from '@/modules/provider/types/hierarchy.types'

interface Props {
  organizationId: string
  modelValue?: string | null
}

interface Emits {
  (e: 'update:modelValue', value: string | null): void
  (e: 'select', staff: StaffMember): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const hierarchyStore = useHierarchyStore()

const isLoading = ref(false)
const selectedStaffId = ref<string | null>(props.modelValue || null)

const staffMembers = computed(() => hierarchyStore.staffMembers?.items || [])
const selectedStaff = computed(() =>
  staffMembers.value.find((s) => s.id === selectedStaffId.value)
)

onMounted(async () => {
  await loadStaffMembers()
})

async function loadStaffMembers() {
  if (!props.organizationId) return

  isLoading.value = true

  try {
    await hierarchyStore.loadStaffMembers({
      organizationId: props.organizationId,
      isActive: true,
    })
  } catch (error) {
    console.error('Error loading staff members:', error)
  } finally {
    isLoading.value = false
  }
}

function handleSelect(staff: StaffMember) {
  if (!staff.isActive) return

  selectedStaffId.value = staff.id
  emit('update:modelValue', staff.id)
  emit('select', staff)
}
</script>

<style scoped lang="scss">
.staff-selector {
  padding: 1.5rem;
  background: white;
  border-radius: 12px;
  border: 1px solid #e2e8f0;
}

.selector-title {
  font-size: 1.25rem;
  color: #1a202c;
  margin-bottom: 0.5rem;
}

.selector-description {
  color: #718096;
  margin-bottom: 1.5rem;
  font-size: 0.95rem;
}

.loading-state,
.empty-state {
  text-align: center;
  padding: 2rem 1rem;

  .spinner {
    width: 40px;
    height: 40px;
    border: 3px solid #f3f4f6;
    border-top-color: #667eea;
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin: 0 auto 1rem;
  }

  i {
    font-size: 2.5rem;
    color: #cbd5e0;
    margin-bottom: 0.5rem;
  }

  p {
    color: #a0aec0;
  }
}

.staff-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.staff-item {
  display: flex;
  align-items: flex-start;
  gap: 1rem;
  padding: 1rem;
  border: 2px solid #e2e8f0;
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.2s;
  position: relative;

  &:hover:not(.disabled) {
    border-color: #667eea;
    background: #f7fafc;
  }

  &.selected {
    border-color: #667eea;
    background: #ebf4ff;
  }

  &.disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  .staff-avatar {
    width: 60px;
    height: 60px;
    flex-shrink: 0;
    position: relative;

    img {
      width: 100%;
      height: 100%;
      border-radius: 50%;
      object-fit: cover;
    }

    .avatar-placeholder {
      width: 100%;
      height: 100%;
      border-radius: 50%;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      font-size: 1.5rem;
    }

    .selected-badge {
      position: absolute;
      bottom: -2px;
      left: -2px;
      width: 24px;
      height: 24px;
      background: #48bb78;
      border-radius: 50%;
      border: 2px solid white;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      font-size: 0.75rem;
    }
  }

  .staff-info {
    flex: 1;

    .staff-name {
      font-size: 1.125rem;
      color: #1a202c;
      margin-bottom: 0.25rem;
      font-weight: 600;
    }

    .staff-title {
      color: #667eea;
      font-size: 0.875rem;
      margin-bottom: 0.5rem;
      font-weight: 500;
    }

    .staff-bio {
      color: #718096;
      font-size: 0.875rem;
      margin-bottom: 0.75rem;
      line-height: 1.5;
    }

    .staff-meta {
      display: flex;
      flex-wrap: wrap;
      gap: 1rem;

      .meta-item {
        display: flex;
        align-items: center;
        gap: 0.375rem;
        color: #a0aec0;
        font-size: 0.875rem;

        i {
          color: #667eea;
        }
      }
    }
  }

  .inactive-badge {
    position: absolute;
    top: 0.5rem;
    left: 0.5rem;
    padding: 0.25rem 0.75rem;
    background: #fed7d7;
    color: #c53030;
    border-radius: 12px;
    font-size: 0.75rem;
    font-weight: 500;
  }
}

.selected-summary {
  margin-top: 1.5rem;
  padding: 1rem;
  background: #f0fff4;
  border: 1px solid #9ae6b4;
  border-radius: 8px;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  color: #22543d;

  i {
    color: #38a169;
    font-size: 1.25rem;
  }

  strong {
    font-weight: 600;
  }
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 640px) {
  .staff-item {
    flex-direction: column;
    text-align: center;

    .staff-avatar {
      margin: 0 auto;
    }

    .staff-meta {
      justify-content: center;
    }
  }
}
</style>
