<!--
  AddNotesModal Component
  Modal for adding internal or customer-visible notes to bookings
-->

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="modelValue" class="modal-overlay" @click="handleOverlayClick" dir="rtl">
        <div class="modal-container" @click.stop>
          <!-- Header -->
          <div class="modal-header">
            <h2>افزودن یادداشت</h2>
            <button class="close-btn" @click="close" :disabled="loading">
              <i class="icon-x"></i>
            </button>
          </div>

          <!-- Content -->
          <div class="modal-content">
            <!-- Booking Info -->
            <div v-if="booking" class="booking-info">
              <div class="info-row">
                <span class="label">تاریخ و زمان:</span>
                <span class="value">{{ formatDateTime(booking.scheduledStartTime) }}</span>
              </div>
              <div class="info-row">
                <span class="label">مشتری:</span>
                <span class="value">{{ booking.clientId }}</span>
              </div>
              <div class="info-row">
                <span class="label">سرویس:</span>
                <span class="value">{{ booking.serviceId }}</span>
              </div>
            </div>

            <!-- Note Type Selection -->
            <div class="note-type-selector">
              <label class="radio-option" :class="{ active: isInternal }">
                <input
                  type="radio"
                  name="noteType"
                  :value="true"
                  v-model="isInternal"
                  :disabled="loading"
                />
                <div class="option-content">
                  <div class="option-header">
                    <i class="icon-lock"></i>
                    <span class="option-title">یادداشت داخلی</span>
                  </div>
                  <span class="option-description">فقط برای تیم شما قابل مشاهده است</span>
                </div>
              </label>

              <label class="radio-option" :class="{ active: !isInternal }">
                <input
                  type="radio"
                  name="noteType"
                  :value="false"
                  v-model="isInternal"
                  :disabled="loading"
                />
                <div class="option-content">
                  <div class="option-header">
                    <i class="icon-eye"></i>
                    <span class="option-title">یادداشت عمومی</span>
                  </div>
                  <span class="option-description">برای مشتری نیز قابل مشاهده خواهد بود</span>
                </div>
              </label>
            </div>

            <!-- Notes Textarea -->
            <div class="form-group">
              <label for="notes">متن یادداشت</label>
              <textarea
                id="notes"
                v-model="notes"
                placeholder="یادداشت خود را اینجا بنویسید..."
                rows="6"
                :disabled="loading"
                ref="notesTextarea"
              ></textarea>
              <div class="char-counter" :class="{ warning: notes.length > 500 }">
                {{ notes.length }} / 1000 کاراکتر
              </div>
            </div>

            <!-- Existing Notes (if any) -->
            <div v-if="hasExistingNotes" class="existing-notes">
              <h3>یادداشت‌های قبلی</h3>
              <div class="notes-list">
                <div v-if="booking?.internalNotes" class="note-item internal">
                  <div class="note-header">
                    <i class="icon-lock"></i>
                    <span>یادداشت داخلی</span>
                  </div>
                  <p>{{ booking.internalNotes }}</p>
                </div>
                <div v-if="booking?.bookingNotes" class="note-item public">
                  <div class="note-header">
                    <i class="icon-eye"></i>
                    <span>یادداشت عمومی</span>
                  </div>
                  <p>{{ booking.bookingNotes }}</p>
                </div>
              </div>
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
              @click="handleAddNotes"
              :disabled="!notes.trim() || notes.length > 1000 || loading"
            >
              <span v-if="loading" class="spinner"></span>
              <span v-else>افزودن یادداشت</span>
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import { bookingService } from '@/modules/booking/api/booking.service'
import type { Appointment } from '@/modules/booking/types/booking.types'
import { formatDateTime } from '@/core/utils'

// ==================== Props & Emits ====================

interface Props {
  modelValue: boolean
  booking: Appointment | null
}

const props = defineProps<Props>()

interface Emits {
  (e: 'update:modelValue', value: boolean): void
  (e: 'notes-added', booking: Appointment): void
}

const emit = defineEmits<Emits>()

// ==================== State ====================

const loading = ref(false)
const error = ref<string | null>(null)
const notes = ref('')
const isInternal = ref(true)
const notesTextarea = ref<HTMLTextAreaElement | null>(null)

// ==================== Computed ====================

const hasExistingNotes = computed(() => {
  return !!(props.booking?.internalNotes || props.booking?.bookingNotes)
})

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
  notes.value = ''
  isInternal.value = true
  error.value = null
}

async function handleAddNotes() {
  if (!props.booking || !notes.value.trim()) return

  if (notes.value.length > 1000) {
    error.value = 'متن یادداشت نباید بیشتر از ۱۰۰۰ کاراکتر باشد'
    return
  }

  loading.value = true
  error.value = null

  try {
    const updatedBooking = await bookingService.addNotes(
      props.booking.id,
      notes.value.trim(),
      isInternal.value
    )

    emit('notes-added', updatedBooking)
    close()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'خطا در افزودن یادداشت'
    console.error('Error adding notes:', err)
  } finally {
    loading.value = false
  }
}

// ==================== Formatting Helpers ====================


// ==================== Lifecycle ====================

// Reset form and focus textarea when modal opens
watch(
  () => props.modelValue,
  async (isOpen) => {
    if (isOpen) {
      resetForm()
      await nextTick()
      notesTextarea.value?.focus()
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

/* ==================== Modal Content ==================== */

.modal-content {
  flex: 1;
  overflow-y: auto;
  padding: 24px;
}

/* Booking Info */
.booking-info {
  background: var(--color-gray-50);
  border-radius: 12px;
  padding: 16px;
  margin-bottom: 24px;
}

.info-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.info-row:last-child {
  margin-bottom: 0;
}

.info-row .label {
  font-size: 13px;
  color: var(--color-gray-600);
  font-weight: 500;
  min-width: 80px;
}

.info-row .value {
  font-size: 14px;
  color: var(--color-gray-900);
  font-weight: 600;
}

/* Note Type Selector */
.note-type-selector {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
  margin-bottom: 24px;
}

.radio-option {
  position: relative;
  cursor: pointer;
  border: 2px solid var(--color-gray-300);
  border-radius: 12px;
  padding: 16px;
  transition: all 0.2s ease;
}

.radio-option:hover {
  border-color: var(--color-gray-400);
  background: var(--color-gray-50);
}

.radio-option.active {
  border-color: var(--color-primary-500);
  background: var(--color-primary-50);
}

.radio-option input[type='radio'] {
  position: absolute;
  opacity: 0;
  pointer-events: none;
}

.option-content {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.option-header {
  display: flex;
  align-items: center;
  gap: 8px;
}

.option-header i {
  font-size: 18px;
  color: var(--color-gray-600);
}

.radio-option.active .option-header i {
  color: var(--color-primary-500);
}

.option-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--color-gray-900);
}

.option-description {
  font-size: 12px;
  color: var(--color-gray-600);
  line-height: 1.4;
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

.char-counter {
  text-align: left;
  font-size: 12px;
  color: var(--color-gray-600);
  margin-top: 6px;
}

.char-counter.warning {
  color: var(--color-danger-600);
  font-weight: 600;
}

/* Existing Notes */
.existing-notes {
  margin-top: 24px;
  padding-top: 24px;
  border-top: 1px solid var(--color-gray-300);
}

.existing-notes h3 {
  font-size: 14px;
  font-weight: 600;
  color: var(--color-gray-800);
  margin: 0 0 12px 0;
}

.notes-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.note-item {
  border-radius: 8px;
  padding: 12px;
  font-size: 13px;
}

.note-item.internal {
  background: #fef3c7;
  border: 1px solid #fde68a;
}

.note-item.public {
  background: #dbeafe;
  border: 1px solid #bfdbfe;
}

.note-header {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 6px;
  font-size: 12px;
  font-weight: 600;
  color: var(--color-gray-800);
}

.note-header i {
  font-size: 14px;
}

.note-item p {
  margin: 0;
  color: var(--color-gray-700);
  line-height: 1.5;
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
  }

  .note-type-selector {
    grid-template-columns: 1fr;
  }

  .modal-content {
    padding: 16px;
  }
}

/* ==================== Icon Fallbacks ==================== */

.icon-x::before {
  content: '✕';
}

.icon-lock::before {
  content: '🔒';
}

.icon-eye::before {
  content: '👁';
}

.icon-alert::before {
  content: '⚠';
}
</style>
