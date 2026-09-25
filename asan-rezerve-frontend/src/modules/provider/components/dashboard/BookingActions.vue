<!--
  BookingActions Component
  Provides action buttons for managing bookings (reschedule, assign staff, add notes, no-show, etc.)
-->

<template>
  <div class="booking-actions" dir="rtl">
    <!-- Quick Actions (most common) -->
    <div class="quick-actions">
      <!-- Confirm Action (Pending bookings) -->
      <button
        v-if="canConfirm"
        class="action-btn action-confirm"
        @click="handleConfirm"
        :disabled="loading"
        title="تایید رزرو"
      >
        <i class="icon-check"></i>
        <span>تایید</span>
      </button>

      <!-- Complete Action (Confirmed/InProgress bookings) -->
      <button
        v-if="canComplete"
        class="action-btn action-complete"
        @click="handleComplete"
        :disabled="loading"
        title="تکمیل رزرو"
      >
        <i class="icon-check-circle"></i>
        <span>تکمیل</span>
      </button>

      <!-- Cancel Action -->
      <button
        v-if="canCancel"
        class="action-btn action-cancel"
        @click="handleCancel"
        :disabled="loading"
        title="لغو رزرو"
      >
        <i class="icon-x"></i>
        <span>لغو</span>
      </button>
    </div>

    <!-- More Actions Dropdown -->
    <div class="more-actions" v-if="hasMoreActions">
      <button
        class="action-btn action-more"
        @click="toggleDropdown"
        :disabled="loading"
        title="اقدامات بیشتر"
      >
        <i class="icon-more"></i>
      </button>

      <!-- Dropdown Menu -->
      <div v-if="showDropdown" class="dropdown-menu">
        <button
          v-if="canReschedule"
          class="dropdown-item"
          @click="handleReschedule"
        >
          <i class="icon-calendar"></i>
          <span>تغییر زمان</span>
        </button>

        <button
          v-if="canAssignStaff"
          class="dropdown-item"
          @click="handleAssignStaff"
        >
          <i class="icon-user"></i>
          <span>تخصیص کارمند</span>
        </button>

        <button
          v-if="canAddNotes"
          class="dropdown-item"
          @click="handleAddNotes"
        >
          <i class="icon-file-text"></i>
          <span>افزودن یادداشت</span>
        </button>

        <div v-if="canMarkNoShow" class="dropdown-divider"></div>

        <button
          v-if="canMarkNoShow"
          class="dropdown-item dropdown-item-danger"
          @click="handleMarkNoShow"
        >
          <i class="icon-user-x"></i>
          <span>عدم حضور</span>
        </button>
      </div>
    </div>

    <!-- Loading Overlay -->
    <div v-if="loading" class="loading-overlay">
      <div class="spinner"></div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import type { Appointment } from '@/modules/booking/types/booking.types'
import { AppointmentStatus } from '@/modules/booking/types/booking.types'

// ==================== Props & Emits ====================

interface Props {
  booking: Appointment
  loading?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
})

interface Emits {
  (e: 'confirm', bookingId: string): void
  (e: 'complete', bookingId: string): void
  (e: 'cancel', bookingId: string): void
  (e: 'reschedule', bookingId: string): void
  (e: 'assign-staff', bookingId: string): void
  (e: 'add-notes', bookingId: string): void
  (e: 'mark-no-show', bookingId: string): void
}

const emit = defineEmits<Emits>()

// ==================== State ====================

const showDropdown = ref(false)

// ==================== Computed - Action Availability ====================

/**
 * Can confirm booking (Pending → Confirmed)
 */
const canConfirm = computed(() => {
  return props.booking.status === AppointmentStatus.Pending
})

/**
 * Can complete booking (Confirmed/InProgress → Completed)
 */
const canComplete = computed(() => {
  return (
    props.booking.status === AppointmentStatus.Confirmed ||
    props.booking.status === AppointmentStatus.InProgress
  )
})

/**
 * Can cancel booking (not already Cancelled, Completed, or NoShow)
 */
const canCancel = computed(() => {
  return ![
    AppointmentStatus.Cancelled,
    AppointmentStatus.Completed,
    AppointmentStatus.NoShow,
  ].includes(props.booking.status)
})

/**
 * Can reschedule booking (Pending or Confirmed)
 */
const canReschedule = computed(() => {
  return (
    props.booking.status === AppointmentStatus.Pending ||
    props.booking.status === AppointmentStatus.Confirmed
  )
})

/**
 * Can assign/reassign staff (any active booking)
 */
const canAssignStaff = computed(() => {
  return ![
    AppointmentStatus.Cancelled,
    AppointmentStatus.Completed,
    AppointmentStatus.NoShow,
  ].includes(props.booking.status)
})

/**
 * Can add notes (any booking)
 */
const canAddNotes = computed(() => {
  return true // Can always add notes
})

/**
 * Can mark as no-show (Confirmed bookings past their time)
 */
const canMarkNoShow = computed(() => {
  if (props.booking.status !== AppointmentStatus.Confirmed) {
    return false
  }

  // Check if booking time has passed
  const scheduledTime = new Date(props.booking.scheduledStartTime)
  const now = new Date()
  return scheduledTime < now
})

/**
 * Has any actions in the "more" dropdown
 */
const hasMoreActions = computed(() => {
  return canReschedule.value || canAssignStaff.value || canAddNotes.value || canMarkNoShow.value
})

// ==================== Methods ====================

function toggleDropdown() {
  showDropdown.value = !showDropdown.value
}

function closeDropdown() {
  showDropdown.value = false
}

function handleConfirm() {
  emit('confirm', props.booking.id)
}

function handleComplete() {
  emit('complete', props.booking.id)
}

function handleCancel() {
  emit('cancel', props.booking.id)
}

function handleReschedule() {
  closeDropdown()
  emit('reschedule', props.booking.id)
}

function handleAssignStaff() {
  closeDropdown()
  emit('assign-staff', props.booking.id)
}

function handleAddNotes() {
  closeDropdown()
  emit('add-notes', props.booking.id)
}

function handleMarkNoShow() {
  closeDropdown()
  emit('mark-no-show', props.booking.id)
}

// Close dropdown when clicking outside
if (typeof window !== 'undefined') {
  window.addEventListener('click', (e) => {
    const target = e.target as HTMLElement
    if (!target.closest('.more-actions')) {
      closeDropdown()
    }
  })
}
</script>

<style scoped>
.booking-actions {
  position: relative;
  display: flex;
  align-items: center;
  gap: 8px;
  direction: rtl;
}

/* ==================== Quick Actions ==================== */

.quick-actions {
  display: flex;
  gap: 6px;
  align-items: center;
}

.action-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 12px;
  border: none;
  border-radius: 6px;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  white-space: nowrap;
}

.action-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.action-btn i {
  font-size: 16px;
}

/* Confirm Action */
.action-confirm {
  background: var(--color-success-500);
  color: white;
}

.action-confirm:hover:not(:disabled) {
  background: var(--color-success-600);
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);
}

/* Complete Action */
.action-complete {
  background: var(--color-primary-500);
  color: white;
}

.action-complete:hover:not(:disabled) {
  background: var(--color-primary-600);
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
}

/* Cancel Action */
.action-cancel {
  background: var(--color-danger-500);
  color: white;
}

.action-cancel:hover:not(:disabled) {
  background: var(--color-danger-600);
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(239, 68, 68, 0.3);
}

/* More Actions Button */
.action-more {
  background: var(--color-gray-100);
  color: var(--color-gray-600);
  padding: 8px 10px;
}

.action-more:hover:not(:disabled) {
  background: var(--color-gray-300);
  color: var(--color-gray-800);
}

/* ==================== Dropdown Menu ==================== */

.more-actions {
  position: relative;
}

.dropdown-menu {
  position: absolute;
  left: 0;
  top: calc(100% + 4px);
  background: white;
  border-radius: 8px;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1), 0 4px 6px rgba(0, 0, 0, 0.05);
  min-width: 200px;
  padding: 6px;
  z-index: 1000;
  animation: dropdownFadeIn 0.2s ease;
}

@keyframes dropdownFadeIn {
  from {
    opacity: 0;
    transform: translateY(-8px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.dropdown-item {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  padding: 10px 12px;
  background: transparent;
  border: none;
  border-radius: 6px;
  font-size: 14px;
  color: var(--color-gray-800);
  text-align: right;
  cursor: pointer;
  transition: all 0.15s ease;
}

.dropdown-item:hover {
  background: var(--color-gray-100);
  color: var(--color-gray-900);
}

.dropdown-item i {
  font-size: 18px;
  color: var(--color-gray-600);
}

.dropdown-item:hover i {
  color: var(--color-gray-800);
}

.dropdown-item-danger {
  color: var(--color-danger-600);
}

.dropdown-item-danger:hover {
  background: var(--color-danger-50);
  color: var(--color-danger-600);
}

.dropdown-item-danger i {
  color: var(--color-danger-500);
}

.dropdown-item-danger:hover i {
  color: var(--color-danger-600);
}

.dropdown-divider {
  height: 1px;
  background: var(--color-gray-300);
  margin: 4px 0;
}

/* ==================== Loading Overlay ==================== */

.loading-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(255, 255, 255, 0.8);
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.spinner {
  width: 20px;
  height: 20px;
  border: 2px solid var(--color-gray-300);
  border-top-color: var(--color-primary-500);
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* ==================== Responsive ==================== */

@media (max-width: 768px) {
  .action-btn span {
    display: none;
  }

  .action-btn {
    padding: 8px 10px;
  }

  .dropdown-menu {
    min-width: 180px;
  }
}

/* ==================== Icon Fallbacks (if icon library not loaded) ==================== */

.icon-check::before {
  content: '✓';
}

.icon-check-circle::before {
  content: '✓';
}

.icon-x::before {
  content: '✕';
}

.icon-more::before {
  content: '⋯';
}

.icon-calendar::before {
  content: '📅';
}

.icon-user::before {
  content: '👤';
}

.icon-file-text::before {
  content: '📝';
}

.icon-user-x::before {
  content: '🚫';
}
</style>
