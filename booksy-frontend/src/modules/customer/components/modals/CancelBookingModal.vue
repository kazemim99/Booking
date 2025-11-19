<template>
  <BaseModal
    :is-open="isOpen"
    @close="handleClose"
    title="لغو نوبت"
    size="sm"
  >
    <div class="cancel-modal-content">
      <!-- Warning Message -->
      <div class="warning-box">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="warning-icon">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        <div class="warning-content">
          <h4>آیا مطمئن هستید؟</h4>
          <p>این نوبت لغو خواهد شد و قابل بازگشت نیست</p>
        </div>
      </div>

      <!-- Booking Details Summary -->
      <div v-if="booking" class="booking-summary">
        <div class="summary-row">
          <span class="label">ارائه‌دهنده:</span>
          <span class="value">{{ booking.providerName }}</span>
        </div>
        <div class="summary-row">
          <span class="label">خدمت:</span>
          <span class="value">{{ booking.serviceName }}</span>
        </div>
        <div class="summary-row">
          <span class="label">زمان:</span>
          <span class="value">{{ formattedDateTime }}</span>
        </div>
      </div>

      <!-- Cancellation Reason -->
      <div class="form-group">
        <label for="cancelReason" class="form-label">دلیل لغو (اختیاری)</label>
        <select
          id="cancelReason"
          v-model="selectedReason"
          class="form-select"
        >
          <option value="">انتخاب کنید</option>
          <option value="customer_request">درخواست شخصی</option>
          <option value="schedule_conflict">تداخل زمانی</option>
          <option value="emergency">اورژانس</option>
          <option value="illness">بیماری</option>
          <option value="other">سایر موارد</option>
        </select>
      </div>

      <!-- Additional Notes (optional) -->
      <div v-if="selectedReason === 'other'" class="form-group">
        <label for="cancelNotes" class="form-label">توضیحات</label>
        <textarea
          id="cancelNotes"
          v-model="notes"
          class="form-textarea"
          rows="3"
          placeholder="لطفا دلیل لغو را توضیح دهید..."
        ></textarea>
      </div>

      <!-- Actions -->
      <div class="modal-actions">
        <button
          @click="handleClose"
          class="btn btn-secondary"
          :disabled="loading"
        >
          انصراف
        </button>
        <button
          @click="handleConfirm"
          class="btn btn-danger"
          :disabled="loading || !selectedReason"
        >
          <span v-if="loading" class="spinner"></span>
          <span v-else>تأیید لغو</span>
        </button>
      </div>
    </div>
  </BaseModal>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import BaseModal from '@/shared/components/ui/BaseModal.vue'
import type { UpcomingBooking } from '../../types/customer.types'

interface Props {
  isOpen: boolean
  booking?: UpcomingBooking | null
}

const props = defineProps<Props>()

const emit = defineEmits<{
  close: []
  confirm: [reason: string, notes?: string]
}>()

const selectedReason = ref('')
const notes = ref('')
const loading = ref(false)

const reasonLabels: Record<string, string> = {
  customer_request: 'درخواست شخصی',
  schedule_conflict: 'تداخل زمانی',
  emergency: 'اورژانس',
  illness: 'بیماری',
  other: 'سایر موارد'
}

const formattedDateTime = computed(() => {
  if (!props.booking) return ''

  const date = new Date(props.booking.startTime)
  const formattedDate = date.toLocaleDateString('fa-IR', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
  const formattedTime = date.toLocaleTimeString('fa-IR', {
    hour: '2-digit',
    minute: '2-digit'
  })

  return `${formattedDate} - ${formattedTime}`
})

function handleClose(): void {
  if (!loading.value) {
    selectedReason.value = ''
    notes.value = ''
    emit('close')
  }
}

function handleConfirm(): void {
  if (!selectedReason.value) return

  const reason = reasonLabels[selectedReason.value] || selectedReason.value
  emit('confirm', reason, notes.value || undefined)
}

// Reset form when modal closes
function resetForm(): void {
  selectedReason.value = ''
  notes.value = ''
  loading.value = false
}

defineExpose({ resetForm })
</script>

<style scoped lang="scss">
.cancel-modal-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.warning-box {
  display: flex;
  gap: 1rem;
  padding: 1rem;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 8px;
}

.warning-icon {
  width: 24px;
  height: 24px;
  color: #ef4444;
  flex-shrink: 0;
}

.warning-content {
  flex: 1;

  h4 {
    margin: 0 0 0.25rem 0;
    font-size: 1rem;
    font-weight: 600;
    color: #991b1b;
  }

  p {
    margin: 0;
    font-size: 0.875rem;
    color: #7f1d1d;
  }
}

.booking-summary {
  background: #f9fafb;
  border-radius: 8px;
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.summary-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.875rem;

  .label {
    color: #6b7280;
    font-weight: 500;
  }

  .value {
    color: #111827;
    font-weight: 600;
  }
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}

.form-select,
.form-textarea {
  padding: 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 0.875rem;
  font-family: inherit;
  transition: border-color 0.2s;

  &:focus {
    outline: none;
    border-color: #667eea;
    box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
  }

  &:disabled {
    background: #f3f4f6;
    cursor: not-allowed;
  }
}

.form-textarea {
  resize: vertical;
  min-height: 80px;
}

.modal-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 0.5rem;
}

.btn {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  border-radius: 8px;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  border: none;

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

.btn-secondary {
  background: white;
  color: #374151;
  border: 1px solid #d1d5db;

  &:hover:not(:disabled) {
    background: #f9fafb;
    border-color: #9ca3af;
  }
}

.btn-danger {
  background: #ef4444;
  color: white;

  &:hover:not(:disabled) {
    background: #dc2626;
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(239, 68, 68, 0.3);
  }

  &:active:not(:disabled) {
    transform: translateY(0);
  }
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

// RTL Support
[dir='rtl'] {
  .warning-box {
    direction: rtl;
  }

  .booking-summary,
  .form-group {
    direction: rtl;
  }
}
</style>
