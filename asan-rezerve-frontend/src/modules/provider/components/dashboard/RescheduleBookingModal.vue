<!--
  RescheduleBookingModal Component
  Modal for rescheduling bookings with time slot selection
-->

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="modelValue" class="modal-overlay" @click="handleOverlayClick" dir="rtl">
        <div class="modal-container" @click.stop>
          <!-- Header -->
          <div class="modal-header">
            <h2>تغییر زمان رزرو</h2>
            <button class="close-btn" @click="close" :disabled="loading">
              <i class="icon-x"></i>
            </button>
          </div>

          <!-- Content -->
          <div class="modal-content">
            <!-- Current Booking Info -->
            <div v-if="booking" class="current-booking-info">
              <h3>اطلاعات رزرو فعلی</h3>
              <div class="info-grid">
                <div class="info-item">
                  <span class="label">تاریخ و زمان:</span>
                  <span class="value">{{ formatDateTime(booking.scheduledStartTime) }}</span>
                </div>
                <div class="info-item">
                  <span class="label">مشتری:</span>
                  <span class="value">{{ booking.clientId }}</span>
                </div>
                <div class="info-item">
                  <span class="label">سرویس:</span>
                  <span class="value">{{ booking.serviceId }}</span>
                </div>
              </div>
            </div>

            <!-- Time Slot Picker -->
            <div class="time-picker-section">
              <h3>انتخاب زمان جدید</h3>
              <TimeSlotPicker
                v-if="booking"
                :provider-id="booking.providerId"
                :service-id="booking.serviceId"
                :staff-member-id="booking.staffProviderId"
                @slot-selected="handleSlotSelected"
                @slot-deselected="selectedSlot = null"
              />
            </div>

            <!-- Selected Slot Summary -->
            <div v-if="selectedSlot" class="selected-slot-summary">
              <div class="summary-header">
                <i class="icon-check-circle"></i>
                <span>زمان جدید انتخاب شد</span>
              </div>
              <div class="summary-content">
                <strong>{{ formatDateTime(selectedSlot.startTime) }}</strong>
                <span class="duration">
                  ({{ formatDuration(selectedSlot.startTime, selectedSlot.endTime) }})
                </span>
              </div>
            </div>

            <!-- Reason Field -->
            <div class="form-group">
              <label for="reason">دلیل تغییر زمان (اختیاری)</label>
              <textarea
                id="reason"
                v-model="reason"
                placeholder="مثال: درخواست مشتری، تغییر برنامه کاری، ..."
                rows="3"
                :disabled="loading"
              ></textarea>
            </div>

            <!-- Error Message -->
            <div v-if="error" class="error-message">
              <i class="icon-alert"></i>
              <span>{{ error }}</span>
            </div>
          </div>

          <!-- Footer -->
          <div class="modal-footer">
            <button class="btn btn-secondary" @click="close" :disabled="loading">
              لغو
            </button>
            <button
              class="btn btn-primary"
              @click="handleReschedule"
              :disabled="!selectedSlot || loading"
            >
              <span v-if="loading" class="spinner"></span>
              <span v-else>تایید تغییر زمان</span>
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { bookingService } from '@/modules/booking/api/booking.service'
import TimeSlotPicker from '@/modules/booking/components/TimeSlotPicker.vue'
import type { Appointment } from '@/modules/booking/types/booking.types'
import type { TimeSlot } from '@/modules/booking/api/availability.service'
import { formatDateTime } from '@/core/utils'

// ==================== Props & Emits ====================

interface Props {
  modelValue: boolean
  booking: Appointment | null
}

const props = defineProps<Props>()

interface Emits {
  (e: 'update:modelValue', value: boolean): void
  (e: 'rescheduled', booking: Appointment): void
}

const emit = defineEmits<Emits>()

// ==================== State ====================

const loading = ref(false)
const error = ref<string | null>(null)
const selectedSlot = ref<TimeSlot | null>(null)
const reason = ref('')

// ==================== Methods ====================

function close() {
  if (loading.value) return
  emit('update:modelValue', false)
  resetForm()
}

function handleOverlayClick() {
  close()
}

function resetForm() {
  selectedSlot.value = null
  reason.value = ''
  error.value = null
}

function handleSlotSelected(slot: TimeSlot) {
  selectedSlot.value = slot
  error.value = null
}

async function handleReschedule() {
  if (!props.booking || !selectedSlot.value) return

  loading.value = true
  error.value = null

  try {
    const updatedBooking = await bookingService.rescheduleBooking({
      appointmentId: props.booking.id,
      newStartTime: selectedSlot.value.startTime,
      reason: reason.value || undefined,
    })

    emit('rescheduled', updatedBooking)
    close()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'خطا در تغییر زمان رزرو'
    console.error('Error rescheduling booking:', err)
  } finally {
    loading.value = false
  }
}

// ==================== Formatting Helpers ====================


function formatDuration(startTime: string, endTime: string): string {
  try {
    const start = new Date(startTime)
    const end = new Date(endTime)
    const durationMinutes = Math.round((end.getTime() - start.getTime()) / 60000)
    return `${durationMinutes} دقیقه`
  } catch {
    return ''
  }
}

// ==================== Lifecycle ====================

// Reset form when modal opens
watch(
  () => props.modelValue,
  (isOpen) => {
    if (!isOpen) {
      resetForm()
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
  max-width: 800px;
  max-height: 90vh;
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

.close-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* ==================== Modal Content ==================== */

.modal-content {
  flex: 1;
  overflow-y: auto;
  padding: 24px;
}

/* Current Booking Info */
.current-booking-info {
  background: var(--color-gray-50);
  border-radius: 12px;
  padding: 16px;
  margin-bottom: 24px;
}

.current-booking-info h3 {
  font-size: 16px;
  font-weight: 600;
  color: var(--color-gray-800);
  margin: 0 0 12px 0;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 12px;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.info-item .label {
  font-size: 13px;
  color: var(--color-gray-600);
  font-weight: 500;
}

.info-item .value {
  font-size: 14px;
  color: var(--color-gray-900);
  font-weight: 600;
}

/* Time Picker Section */
.time-picker-section {
  margin-bottom: 24px;
}

.time-picker-section h3 {
  font-size: 16px;
  font-weight: 600;
  color: var(--color-gray-900);
  margin: 0 0 16px 0;
}

/* Selected Slot Summary */
.selected-slot-summary {
  background: linear-gradient(135deg, var(--color-success-500) 0%, var(--color-success-600) 100%);
  border-radius: 12px;
  padding: 16px;
  margin-bottom: 20px;
  color: white;
}

.summary-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
  font-size: 14px;
  font-weight: 500;
  opacity: 0.95;
}

.summary-header i {
  font-size: 18px;
}

.summary-content {
  display: flex;
  align-items: baseline;
  gap: 8px;
}

.summary-content strong {
  font-size: 18px;
  font-weight: 700;
}

.summary-content .duration {
  font-size: 14px;
  opacity: 0.9;
}

/* Form Group */
.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  font-size: 14px;
  font-weight: 600;
  color: var(--color-gray-800);
  margin-bottom: 8px;
}

.form-group textarea {
  width: 100%;
  padding: 12px;
  border: 1px solid var(--color-gray-400);
  border-radius: 8px;
  font-size: 14px;
  font-family: inherit;
  resize: vertical;
  transition: all 0.2s ease;
}

.form-group textarea:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.form-group textarea:disabled {
  background: var(--color-gray-100);
  cursor: not-allowed;
}

/* Error Message */
.error-message {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 16px;
  background: var(--color-danger-50);
  border: 1px solid #fecaca;
  border-radius: 8px;
  color: var(--color-danger-600);
  font-size: 14px;
  margin-top: 16px;
}

.error-message i {
  font-size: 18px;
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
  min-width: 120px;
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

.btn-primary:disabled {
  background: #93c5fd;
}

.spinner {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
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

  .modal-content {
    padding: 16px;
  }

  .modal-header,
  .modal-footer {
    padding: 16px;
  }

  .info-grid {
    grid-template-columns: 1fr;
  }
}

/* ==================== Icon Fallbacks ==================== */

.icon-x::before {
  content: '✕';
}

.icon-check-circle::before {
  content: '✓';
}

.icon-alert::before {
  content: '⚠';
}
</style>
