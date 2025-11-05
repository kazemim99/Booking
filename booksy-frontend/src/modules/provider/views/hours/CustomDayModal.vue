<template>
  <div v-if="isOpen" class="modal-overlay" @click.self="handleClose">
    <div class="modal-content" dir="rtl">
      <div class="modal-header">
        <h3 class="modal-title">تنظیم ساعات کاری روز {{ formattedDate }}</h3>
        <button class="close-btn" @click="handleClose">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <line x1="18" y1="6" x2="6" y2="18"></line>
            <line x1="6" y1="6" x2="18" y2="18"></line>
          </svg>
        </button>
      </div>

      <div class="modal-body">
        <!-- Closed Checkbox -->
        <div class="checkbox-group">
          <label class="checkbox-label">
            <input
              type="checkbox"
              v-model="isClosed"
              class="checkbox-input"
            />
            <span class="checkbox-text">تعطیل در این روز</span>
          </label>
        </div>

        <!-- Closed Reason (if closed) -->
        <div v-if="isClosed" class="form-group">
          <label class="form-label">دلیل تعطیلی</label>
          <textarea
            v-model="closedReason"
            class="form-textarea"
            placeholder="مثال: تعطیلات، مرخصی، مناسبت خاص..."
            rows="4"
          ></textarea>
        </div>

        <!-- Time Inputs (if not closed) -->
        <template v-else>
          <div class="time-row">
            <div class="form-group">
              <label class="form-label">ساعت شروع</label>
              <PersianTimePicker
                v-model="startTime"
                placeholder="۱۰:۰۰"
              />
            </div>
            <div class="form-group">
              <label class="form-label">ساعت پایان</label>
              <PersianTimePicker
                v-model="endTime"
                placeholder="۲۲:۰۰"
              />
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">زمان استراحت شروع (اختیاری)</label>
            <PersianTimePicker
              v-model="breakTime"
              placeholder="۱۴:۰۰"
            />
            <p class="form-hint">در این بازه زمانی رزروی ثبت نمی‌شود</p>
          </div>
        </template>
      </div>

      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" @click="handleClose">
          انصراف
        </button>
        <button type="button" class="btn btn-primary" @click="handleSave">
          ذخیره تغییرات
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { convertEnglishToPersianNumbers, gregorianToJalali } from '@/shared/utils/date/jalali.utils'
import PersianTimePicker from '@/shared/components/calendar/PersianTimePicker.vue'

export interface CustomDayData {
  date: Date
  startTime: string
  endTime: string
  breakTime: string
  isClosed: boolean
  closedReason: string
}

interface Props {
  isOpen: boolean
  selectedDate: Date | null
}

const props = defineProps<Props>()
const emit = defineEmits<{
  close: []
  save: [data: CustomDayData]
}>()

const startTime = ref('10:00')
const endTime = ref('22:00')
const breakTime = ref('')
const isClosed = ref(false)
const closedReason = ref('')

const formattedDate = computed(() => {
  if (!props.selectedDate) return ''
  const jalali = gregorianToJalali(props.selectedDate)
  return `${convertEnglishToPersianNumbers(jalali.day.toString())} / ${convertEnglishToPersianNumbers(jalali.month.toString())} / ${convertEnglishToPersianNumbers(jalali.year.toString())}`
})

const handleClose = () => {
  emit('close')
}

const handleSave = () => {
  if (!props.selectedDate) return

  const data: CustomDayData = {
    date: props.selectedDate,
    startTime: startTime.value,
    endTime: endTime.value,
    breakTime: breakTime.value,
    isClosed: isClosed.value,
    closedReason: closedReason.value,
  }

  emit('save', data)

  // Reset form
  startTime.value = '10:00'
  endTime.value = '22:00'
  breakTime.value = ''
  isClosed.value = false
  closedReason.value = ''
}

// Reset form when modal closes
watch(() => props.isOpen, (isOpen) => {
  if (!isOpen) {
    startTime.value = '10:00'
    endTime.value = '22:00'
    breakTime.value = ''
    isClosed.value = false
    closedReason.value = ''
  }
})
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
  animation: fadeIn 0.2s;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

.modal-content {
  background: white;
  border-radius: 0.75rem;
  max-width: 500px;
  width: 90%;
  max-height: 90vh;
  overflow: auto;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
  animation: slideUp 0.3s;
}

@keyframes slideUp {
  from {
    transform: translateY(20px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.5rem;
  border-bottom: 1px solid #e5e7eb;
}

.modal-title {
  font-size: 1.25rem;
  font-weight: 600;
  color: #1a1a1a;
}

.close-btn {
  width: 2rem;
  height: 2rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  background: transparent;
  border-radius: 0.375rem;
  cursor: pointer;
  color: #6b7280;
  transition: all 0.2s;

  svg {
    width: 1.25rem;
    height: 1.25rem;
    stroke-width: 2;
  }

  &:hover {
    background: #f3f4f6;
    color: #1a1a1a;
  }
}

.modal-body {
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.checkbox-group {
  display: flex;
  align-items: center;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
}

.checkbox-input {
  width: 1.25rem;
  height: 1.25rem;
  border: 2px solid #d1d5db;
  border-radius: 0.25rem;
  cursor: pointer;
  transition: all 0.2s;

  &:checked {
    background-color: #8b5cf6;
    border-color: #8b5cf6;
  }
}

.checkbox-text {
  font-size: 0.9375rem;
  color: #1a1a1a;
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

.form-input,
.form-textarea {
  width: 100%;
  padding: 0.625rem 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  font-size: 0.9375rem;
  transition: all 0.2s;

  &:focus {
    outline: none;
    border-color: #8b5cf6;
    box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
  }

  &::placeholder {
    color: #9ca3af;
  }
}

.form-textarea {
  resize: vertical;
  min-height: 100px;
  font-family: inherit;
}

.form-hint {
  font-size: 0.75rem;
  color: #6b7280;
  margin-top: -0.25rem;
}

.time-row {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1rem;
}

.modal-footer {
  display: flex;
  gap: 0.75rem;
  padding: 1.5rem;
  border-top: 1px solid #e5e7eb;
  justify-content: flex-end;
}

.btn {
  padding: 0.625rem 1.25rem;
  border-radius: 0.5rem;
  font-size: 0.9375rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  border: none;

  &:active {
    transform: scale(0.98);
  }
}

.btn-secondary {
  background: white;
  color: #374151;
  border: 1px solid #d1d5db;

  &:hover {
    background: #f9fafb;
  }
}

.btn-primary {
  background: #8b5cf6;
  color: white;

  &:hover {
    background: #7c3aed;
  }
}
</style>
