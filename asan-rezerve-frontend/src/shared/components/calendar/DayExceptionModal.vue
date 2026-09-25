<template>
  <div v-if="isOpen" class="modal-overlay" @click.self="handleClose">
    <div class="modal-content" dir="rtl">
      <!-- Modal Header -->
      <div class="modal-header">
        <h3 class="modal-title">تنظیمات روز {{ formattedDate }}</h3>
        <button type="button" class="close-btn" @click="handleClose">
          <svg class="close-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <!-- Modal Body -->
      <div class="modal-body">
        <!-- Day Type Selection -->
        <div class="form-section">
          <label class="section-label">نوع روز</label>
          <div class="day-type-options">
            <button
              type="button"
              :class="['type-btn', { active: dayType === 'normal' }]"
              @click="dayType = 'normal'"
            >
              <svg class="type-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <span>عادی</span>
              <span class="type-desc">طبق ساعات کاری هفتگی</span>
            </button>

            <button
              type="button"
              :class="['type-btn', { active: dayType === 'holiday' }]"
              @click="dayType = 'holiday'"
            >
              <svg class="type-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
              <span>تعطیل</span>
              <span class="type-desc">بسته در این روز</span>
            </button>

            <button
              type="button"
              :class="['type-btn', { active: dayType === 'special' }]"
              @click="dayType = 'special'"
            >
              <svg class="type-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
              </svg>
              <span>ساعات ویژه</span>
              <span class="type-desc">ساعات کاری متفاوت</span>
            </button>
          </div>
        </div>

        <!-- Special Hours Form -->
        <div v-if="dayType === 'special'" class="form-section">
          <label class="section-label">ساعات کاری ویژه</label>

          <div class="time-inputs">
            <div class="time-group">
              <label for="openTime" class="time-label">ساعت شروع</label>
              <input
                id="openTime"
                v-model="specialHours.openTime"
                type="time"
                dir="ltr"
                class="time-input"
              />
            </div>

            <div class="time-group">
              <label for="closeTime" class="time-label">ساعت پایان</label>
              <input
                id="closeTime"
                v-model="specialHours.closeTime"
                type="time"
                dir="ltr"
                class="time-input"
              />
            </div>
          </div>
        </div>

        <!-- Note/Reason -->
        <div v-if="dayType !== 'normal'" class="form-section">
          <label for="note" class="section-label">یادداشت (اختیاری)</label>
          <textarea
            id="note"
            v-model="note"
            class="note-textarea"
            rows="3"
            placeholder="دلیل تعطیلی یا توضیحات..."
          />
        </div>

        <!-- Error Message -->
        <div v-if="errorMessage" class="error-message">
          {{ errorMessage }}
        </div>
      </div>

      <!-- Modal Footer -->
      <div class="modal-footer">
        <button
          v-if="dayType !== 'normal' && hasExistingException"
          type="button"
          class="delete-btn"
          @click="handleDelete"
        >
          حذف استثنا
        </button>
        <button type="button" class="cancel-btn" @click="handleClose">
          انصراف
        </button>
        <button type="button" class="save-btn" @click="handleSave" :disabled="isSaving">
          {{ isSaving ? 'در حال ذخیره...' : 'ذخیره' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { formatJalaliDate } from '@/shared/utils/date/jalali.utils'
import type { CalendarDay } from './JalaliCalendar.vue'

export interface DayException {
  date: string // YYYY-MM-DD format
  type: 'holiday' | 'special'
  openTime?: string
  closeTime?: string
  note?: string
}

interface Props {
  isOpen: boolean
  selectedDay: CalendarDay | null
  existingException?: DayException | null
}

interface Emits {
  (e: 'close'): void
  (e: 'save', exception: DayException): void
  (e: 'delete', date: string): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

// Form state
const dayType = ref<'normal' | 'holiday' | 'special'>('normal')
const specialHours = ref({
  openTime: '09:00',
  closeTime: '18:00'
})
const note = ref('')
const errorMessage = ref('')
const isSaving = ref(false)

// Computed
const formattedDate = computed(() => {
  if (!props.selectedDay) return ''
  return formatJalaliDate(
    { year: props.selectedDay.year, month: props.selectedDay.month, day: props.selectedDay.day },
    'long'
  )
})

const hasExistingException = computed(() => {
  return props.existingException !== null && props.existingException !== undefined
})

// Watch for changes in selected day
watch(() => props.selectedDay, (newDay) => {
  if (newDay) {
    // Reset form
    errorMessage.value = ''

    // Load existing exception data
    if (props.existingException) {
      dayType.value = props.existingException.type
      if (props.existingException.type === 'special') {
        specialHours.value = {
          openTime: props.existingException.openTime || '09:00',
          closeTime: props.existingException.closeTime || '18:00'
        }
      }
      note.value = props.existingException.note || ''
    } else {
      // Reset to defaults
      dayType.value = 'normal'
      specialHours.value = {
        openTime: '09:00',
        closeTime: '18:00'
      }
      note.value = ''
    }
  }
}, { immediate: true })

// Methods
const handleClose = () => {
  emit('close')
}

const handleSave = () => {
  errorMessage.value = ''

  if (!props.selectedDay) {
    errorMessage.value = 'خطا: روز انتخاب نشده است'
    return
  }

  // If normal day, just close (remove any existing exception)
  if (dayType.value === 'normal') {
    if (hasExistingException.value) {
      const dateString = formatDateForException(props.selectedDay)
      emit('delete', dateString)
    }
    emit('close')
    return
  }

  // Validate special hours
  if (dayType.value === 'special') {
    if (!specialHours.value.openTime || !specialHours.value.closeTime) {
      errorMessage.value = 'لطفاً ساعات کاری را وارد کنید'
      return
    }

    // Check that close time is after open time
    if (specialHours.value.closeTime <= specialHours.value.openTime) {
      errorMessage.value = 'ساعت پایان باید بعد از ساعت شروع باشد'
      return
    }
  }

  // Create exception object
  const dateString = formatDateForException(props.selectedDay)
  const exception: DayException = {
    date: dateString,
    type: dayType.value as 'holiday' | 'special',
    note: note.value || undefined
  }

  if (dayType.value === 'special') {
    exception.openTime = specialHours.value.openTime
    exception.closeTime = specialHours.value.closeTime
  }

  isSaving.value = true
  emit('save', exception)

  // Reset saving state after a delay (will be closed by parent)
  setTimeout(() => {
    isSaving.value = false
  }, 500)
}

const handleDelete = () => {
  if (!props.selectedDay) return

  const dateString = formatDateForException(props.selectedDay)
  emit('delete', dateString)
  emit('close')
}

const formatDateForException = (day: CalendarDay): string => {
  return `${day.year}-${String(day.month).padStart(2, '0')}-${String(day.day).padStart(2, '0')}`
}
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-content {
  background: white;
  border-radius: 1rem;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
  max-width: 32rem;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
}

/* Header */
.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.5rem;
  border-bottom: 1px solid #e5e7eb;
}

.modal-title {
  font-size: 1.25rem;
  font-weight: 700;
  margin: 0;
  color: #111827;
}

.close-btn {
  padding: 0.5rem;
  background: transparent;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
  transition: background 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;

  &:hover {
    background: #f3f4f6;
  }
}

.close-icon {
  width: 1.5rem;
  height: 1.5rem;
  color: #6b7280;
}

/* Body */
.modal-body {
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-section {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.section-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: #111827;
}

/* Day Type Options */
.day-type-options {
  display: grid;
  grid-template-columns: 1fr;
  gap: 0.75rem;
}

.type-btn {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.25rem;
  padding: 1rem;
  border: 2px solid #e5e7eb;
  border-radius: 0.5rem;
  background: white;
  cursor: pointer;
  transition: all 0.2s ease;
  text-align: right;

  &:hover {
    border-color: #8b5cf6;
    background: rgba(139, 92, 246, 0.05);
  }

  &.active {
    border-color: #8b5cf6;
    background: rgba(139, 92, 246, 0.1);
  }

  span:first-of-type {
    font-size: 1rem;
    font-weight: 600;
    color: #111827;
  }
}

.type-icon {
  width: 1.5rem;
  height: 1.5rem;
  color: #8b5cf6;
  margin-bottom: 0.5rem;
}

.type-desc {
  font-size: 0.75rem;
  color: #6b7280;
  font-weight: normal !important;
}

/* Time Inputs */
.time-inputs {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.time-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.time-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}

.time-input {
  width: 100%;
  padding: 0.75rem 1rem;
  font-size: 1rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  background: white;
  transition: all 0.2s ease;
  text-align: left;

  &:focus {
    outline: none;
    border-color: #8b5cf6;
    box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
  }
}

/* Note Textarea */
.note-textarea {
  width: 100%;
  padding: 0.75rem 1rem;
  font-size: 0.875rem;
  font-family: inherit;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  background: white;
  transition: all 0.2s ease;
  resize: vertical;

  &:focus {
    outline: none;
    border-color: #8b5cf6;
    box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
  }

  &::placeholder {
    color: #9ca3af;
  }
}

/* Error Message */
.error-message {
  padding: 0.75rem 1rem;
  background: #fee2e2;
  border: 1px solid #fecaca;
  border-radius: 0.5rem;
  color: #991b1b;
  font-size: 0.875rem;
}

/* Footer */
.modal-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 0.75rem;
  padding: 1.5rem;
  border-top: 1px solid #e5e7eb;
}

.delete-btn {
  padding: 0.75rem 1.5rem;
  background: white;
  color: #ef4444;
  border: 2px solid #ef4444;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  margin-left: auto;

  &:hover {
    background: #ef4444;
    color: white;
  }
}

.cancel-btn {
  padding: 0.75rem 1.5rem;
  background: white;
  color: #6b7280;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;

  &:hover {
    background: #f3f4f6;
  }
}

.save-btn {
  padding: 0.75rem 1.5rem;
  background: #8b5cf6;
  color: white;
  border: none;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;

  &:hover:not(:disabled) {
    background: #7c3aed;
  }

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

@media (max-width: 640px) {
  .modal-content {
    margin: 0.5rem;
  }

  .time-inputs {
    grid-template-columns: 1fr;
  }
}
</style>
