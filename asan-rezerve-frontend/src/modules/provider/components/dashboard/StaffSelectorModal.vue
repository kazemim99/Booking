<!--
  StaffSelectorModal Component
  Modal for selecting staff members to assign to bookings
-->

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="modelValue" class="modal-overlay" @click="handleOverlayClick" dir="rtl">
        <div class="modal-container" @click.stop>
          <!-- Header -->
          <div class="modal-header">
            <h2>انتخاب کارمند</h2>
            <button class="close-btn" @click="close" :disabled="loading">
              <i class="icon-x"></i>
            </button>
          </div>

          <!-- Content -->
          <div class="modal-content">
            <!-- Search Box -->
            <div class="search-box">
              <i class="icon-search"></i>
              <input
                v-model="searchQuery"
                type="text"
                placeholder="جستجو بر اساس نام کارمند..."
                :disabled="loading"
              />
            </div>

            <!-- Loading State -->
            <div v-if="loading" class="loading-container">
              <div class="spinner"></div>
              <p>در حال بارگذاری...</p>
            </div>

            <!-- Error State -->
            <div v-else-if="error" class="error-container">
              <i class="icon-alert"></i>
              <p>{{ error }}</p>
              <button @click="loadStaff" class="retry-btn">تلاش مجدد</button>
            </div>

            <!-- Empty State -->
            <div v-else-if="filteredStaff.length === 0" class="empty-container">
              <i class="icon-users"></i>
              <p v-if="searchQuery">کارمندی با این نام یافت نشد</p>
              <p v-else>هیچ کارمند فعالی وجود ندارد</p>
            </div>

            <!-- Staff List -->
            <div v-else class="staff-list">
              <div
                v-for="staff in filteredStaff"
                :key="staff.id"
                class="staff-item"
                :class="{ selected: selectedStaff?.id === staff.id }"
                @click="handleStaffClick(staff)"
              >
                <!-- Avatar -->
                <div class="staff-avatar">
                  <img v-if="staff.profilePhotoUrl" :src="staff.profilePhotoUrl" :alt="staff.fullName" />
                  <div v-else class="avatar-placeholder">
                    {{ getInitials(staff) }}
                  </div>
                  <div v-if="!staff.isActive" class="inactive-badge">غیرفعال</div>
                </div>

                <!-- Info -->
                <div class="staff-info">
                  <div class="staff-header">
                    <h3 class="staff-name">{{ getStaffName(staff) }}</h3>
                    <span v-if="staff.role" class="staff-role">{{ staff.role }}</span>
                  </div>
                  <div class="staff-details">
                    <span v-if="staff.email" class="detail-item">
                      <i class="icon-mail"></i>
                      {{ staff.email }}
                    </span>
                    <span v-if="staff.phoneNumber" class="detail-item">
                      <i class="icon-phone"></i>
                      {{ staff.phoneNumber }}
                    </span>
                  </div>
                  <p v-if="staff.biography" class="staff-bio">{{ staff.biography }}</p>
                </div>

                <!-- Selection Indicator -->
                <div class="selection-indicator">
                  <i class="icon-check"></i>
                </div>
              </div>
            </div>
          </div>

          <!-- Footer -->
          <div class="modal-footer">
            <button class="btn btn-secondary" @click="close" :disabled="loading">
              لغو
            </button>
            <button
              class="btn btn-primary"
              @click="handleConfirm"
              :disabled="!selectedStaff || loading"
            >
              <span v-if="loading" class="spinner-small"></span>
              <span v-else>تایید انتخاب</span>
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { staffService } from '@/modules/provider/services/staff.service'
import type { Staff } from '@/modules/provider/types/staff.types'

// ==================== Props & Emits ====================

interface Props {
  modelValue: boolean
  providerId: string
  currentStaffId?: string // Pre-select current staff member
}

const props = defineProps<Props>()

interface Emits {
  (e: 'update:modelValue', value: boolean): void
  (e: 'staff-selected', staffId: string, staff: Staff): void
}

const emit = defineEmits<Emits>()

// ==================== State ====================

const loading = ref(false)
const error = ref<string | null>(null)
const searchQuery = ref('')
const allStaff = ref<Staff[]>([])
const selectedStaff = ref<Staff | null>(null)

// ==================== Computed ====================

const filteredStaff = computed(() => {
  if (!searchQuery.value.trim()) {
    return allStaff.value.filter(s => s.isActive)
  }

  const query = searchQuery.value.toLowerCase()
  return allStaff.value.filter(staff => {
    if (!staff.isActive) return false

    const name = getStaffName(staff).toLowerCase()
    const email = staff.email?.toLowerCase() || ''
    const phone = staff.phoneNumber || ''

    return name.includes(query) || email.includes(query) || phone.includes(query)
  })
})

// ==================== Methods ====================

function close() {
  if (loading.value) return
  emit('update:modelValue', false)
  resetModal()
}

function handleOverlayClick() {
  close()
}

function resetModal() {
  searchQuery.value = ''
  selectedStaff.value = null
  error.value = null
}

async function loadStaff() {
  if (!props.providerId) return

  loading.value = true
  error.value = null

  try {
    const staff = await staffService.getStaffByProvider(props.providerId, true) // activeOnly = true
    allStaff.value = staff

    // Pre-select current staff if provided
    if (props.currentStaffId) {
      selectedStaff.value = staff.find(s => s.id === props.currentStaffId) || null
    }
  } catch (err) {
    console.error('Error loading staff:', err)
    error.value = 'خطا در بارگذاری لیست کارمندان'
    allStaff.value = []
  } finally {
    loading.value = false
  }
}

function handleStaffClick(staff: Staff) {
  selectedStaff.value = staff
}

function handleConfirm() {
  if (!selectedStaff.value) return

  emit('staff-selected', selectedStaff.value.id, selectedStaff.value)
  close()
}

function getStaffName(staff: Staff): string {
  return staff.fullName || `${staff.firstName} ${staff.lastName}`.trim() || 'بدون نام'
}

function getInitials(staff: Staff): string {
  const firstName = staff.firstName || ''
  const lastName = staff.lastName || ''

  if (!firstName && !lastName) return '??'

  return `${firstName.charAt(0)}${lastName.charAt(0)}`.toUpperCase()
}

// ==================== Lifecycle ====================

// Load staff when modal opens
watch(
  () => props.modelValue,
  async (isOpen) => {
    if (isOpen) {
      await loadStaff()
    } else {
      resetModal()
    }
  }
)
</script>

<style scoped>
/* ==================== Modal Overlay ==================== */

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
  z-index: 9999;
  padding: 20px;
  overflow-y: auto;
}

.modal-container {
  background: white;
  border-radius: 16px;
  width: 100%;
  max-width: 600px;
  max-height: 80vh;
  display: flex;
  flex-direction: column;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  direction: rtl;
}

/* ==================== Modal Header ==================== */

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 24px;
  border-bottom: 1px solid var(--color-gray-300);
}

.modal-header h2 {
  font-size: 20px;
  font-weight: 700;
  color: var(--color-gray-900);
  margin: 0;
}

.close-btn {
  background: transparent;
  border: none;
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 8px;
  cursor: pointer;
  color: var(--color-gray-600);
  transition: all 0.2s ease;
}

.close-btn:hover:not(:disabled) {
  background: var(--color-gray-100);
  color: var(--color-gray-900);
}

/* ==================== Modal Content ==================== */

.modal-content {
  flex: 1;
  overflow-y: auto;
  padding: 24px;
  min-height: 300px;
}

/* Search Box */
.search-box {
  position: relative;
  margin-bottom: 20px;
}

.search-box i {
  position: absolute;
  right: 12px;
  top: 50%;
  transform: translateY(-50%);
  color: var(--color-gray-500);
  font-size: 18px;
}

.search-box input {
  width: 100%;
  padding: 12px 12px 12px 45px;
  border: 1px solid var(--color-gray-400);
  border-radius: 8px;
  font-size: 14px;
  transition: all 0.2s ease;
}

.search-box input:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

/* Staff List */
.staff-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.staff-item {
  display: flex;
  align-items: flex-start;
  gap: 16px;
  padding: 16px;
  border: 2px solid var(--color-gray-300);
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;
}

.staff-item:hover {
  border-color: var(--color-gray-400);
  background: var(--color-gray-50);
}

.staff-item.selected {
  border-color: var(--color-primary-500);
  background: var(--color-primary-50);
}

/* Avatar */
.staff-avatar {
  position: relative;
  flex-shrink: 0;
}

.staff-avatar img,
.avatar-placeholder {
  width: 60px;
  height: 60px;
  border-radius: 50%;
  object-fit: cover;
}

.avatar-placeholder {
  background: linear-gradient(135deg, var(--color-primary-500) 0%, var(--color-primary-500) 100%);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: 700;
}

.inactive-badge {
  position: absolute;
  bottom: -4px;
  right: -4px;
  background: var(--color-danger-600);
  color: white;
  font-size: 10px;
  padding: 2px 6px;
  border-radius: 10px;
  font-weight: 600;
}

/* Info */
.staff-info {
  flex: 1;
  min-width: 0;
}

.staff-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
  flex-wrap: wrap;
}

.staff-name {
  font-size: 16px;
  font-weight: 600;
  color: var(--color-gray-900);
  margin: 0;
}

.staff-role {
  display: inline-flex;
  padding: 4px 8px;
  background: #e0e7ff;
  color: var(--color-primary-700);
  font-size: 12px;
  font-weight: 500;
  border-radius: 4px;
}

.staff-details {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin-bottom: 8px;
}

.detail-item {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  color: var(--color-gray-600);
}

.detail-item i {
  font-size: 14px;
}

.staff-bio {
  font-size: 13px;
  color: var(--color-gray-600);
  margin: 0;
  line-height: 1.5;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

/* Selection Indicator */
.selection-indicator {
  display: none;
  width: 32px;
  height: 32px;
  background: var(--color-primary-500);
  color: white;
  border-radius: 50%;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.staff-item.selected .selection-indicator {
  display: flex;
}

.selection-indicator i {
  font-size: 18px;
}

/* Loading, Error, Empty States */
.loading-container,
.error-container,
.empty-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  text-align: center;
  color: var(--color-gray-600);
}

.spinner {
  width: 40px;
  height: 40px;
  border: 3px solid var(--color-gray-300);
  border-top-color: var(--color-primary-500);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-bottom: 16px;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.error-container i,
.empty-container i {
  font-size: 48px;
  margin-bottom: 16px;
  color: var(--color-gray-500);
}

.retry-btn {
  margin-top: 16px;
  padding: 8px 16px;
  background: var(--color-primary-500);
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 14px;
  transition: background 0.2s;
}

.retry-btn:hover {
  background: var(--color-primary-600);
}

/* ==================== Modal Footer ==================== */

.modal-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  padding: 20px 24px;
  border-top: 1px solid var(--color-gray-300);
}

.btn {
  padding: 10px 20px;
  border: none;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-width: 100px;
}

.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-secondary {
  background: var(--color-gray-100);
  color: var(--color-gray-800);
}

.btn-secondary:hover:not(:disabled) {
  background: var(--color-gray-300);
}

.btn-primary {
  background: linear-gradient(135deg, var(--color-primary-500) 0%, var(--color-primary-600) 100%);
  color: white;
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
}

.spinner-small {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

/* ==================== Modal Transitions ==================== */

.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.3s ease;
}

.modal-enter-active .modal-container,
.modal-leave-active .modal-container {
  transition: transform 0.3s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-from .modal-container,
.modal-leave-to .modal-container {
  transform: scale(0.95);
}

/* ==================== Responsive ==================== */

@media (max-width: 768px) {
  .modal-container {
    max-width: 100%;
    max-height: 100vh;
    border-radius: 0;
  }

  .staff-item {
    flex-direction: column;
    text-align: center;
  }

  .staff-avatar {
    align-self: center;
  }

  .staff-header {
    justify-content: center;
  }

  .staff-details {
    align-items: center;
  }
}

/* ==================== Icon Fallbacks ==================== */

.icon-x::before {
  content: '✕';
}

.icon-search::before {
  content: '🔍';
}

.icon-alert::before {
  content: '⚠';
}

.icon-users::before {
  content: '👥';
}

.icon-check::before {
  content: '✓';
}

.icon-mail::before {
  content: '✉';
}

.icon-phone::before {
  content: '📞';
}
</style>
