<template>
  <div class="time-slot-picker" dir="rtl">
    <!-- Date Selection -->
    <div class="date-selector">
      <h3 class="section-title">انتخاب تاریخ</h3>
      <div class="date-buttons">
        <button
          v-for="dateOption in availableDateOptions"
          :key="dateOption.value"
          class="date-btn"
          :class="{ active: selectedDate === dateOption.value }"
          @click="selectDate(dateOption.value)"
        >
          <div class="date-label">{{ dateOption.label }}</div>
          <div class="date-value">{{ dateOption.display }}</div>
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="loading-container">
      <div class="spinner"></div>
      <p>در حال بارگذاری زمان‌های خالی...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="error-container">
      <p>{{ error }}</p>
      <button @click="loadSlots" class="retry-btn">تلاش مجدد</button>
    </div>

    <!-- Time Slots -->
    <div v-else-if="selectedDate && groupedSlots" class="slots-container">
      <!-- Morning Slots -->
      <div v-if="groupedSlots.morning.length > 0" class="time-period">
        <h4 class="period-title">
          <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor" class="period-icon">
            <path d="M10 2a6 6 0 100 12 6 6 0 000-12zm0 1a5 5 0 110 10 5 5 0 010-10zm-.5 2v3.5l2.5 1.5.5-.866L10 7.268V5h-1z"/>
          </svg>
          صبح
        </h4>
        <div class="slots-grid">
          <button
            v-for="slot in groupedSlots.morning"
            :key="slot.startTime"
            class="slot-btn"
            :class="{
              selected: selectedSlot?.startTime === slot.startTime,
              unavailable: !slot.available
            }"
            :disabled="!slot.available"
            @click="selectSlot(slot)"
          >
            {{ formatSlotTime(slot) }}
          </button>
        </div>
      </div>

      <!-- Afternoon Slots -->
      <div v-if="groupedSlots.afternoon.length > 0" class="time-period">
        <h4 class="period-title">
          <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor" class="period-icon">
            <path d="M10 2a8 8 0 100 16 8 8 0 000-16zm0 1a7 7 0 110 14 7 7 0 010-14zm-.5 2v5.5l3.5 2 .5-.866-3-1.732V5h-1z"/>
          </svg>
          بعدازظهر
        </h4>
        <div class="slots-grid">
          <button
            v-for="slot in groupedSlots.afternoon"
            :key="slot.startTime"
            class="slot-btn"
            :class="{
              selected: selectedSlot?.startTime === slot.startTime,
              unavailable: !slot.available
            }"
            :disabled="!slot.available"
            @click="selectSlot(slot)"
          >
            {{ formatSlotTime(slot) }}
          </button>
        </div>
      </div>

      <!-- Evening Slots -->
      <div v-if="groupedSlots.evening.length > 0" class="time-period">
        <h4 class="period-title">
          <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor" class="period-icon">
            <path d="M10 2C5.03 2 1 6.03 1 11s4.03 9 9 9 9-4.03 9-9-4.03-9-9-9zm0 16c-3.86 0-7-3.14-7-7s3.14-7 7-7 7 3.14 7 7-3.14 7-7 7z"/>
          </svg>
          عصر
        </h4>
        <div class="slots-grid">
          <button
            v-for="slot in groupedSlots.evening"
            :key="slot.startTime"
            class="slot-btn"
            :class="{
              selected: selectedSlot?.startTime === slot.startTime,
              unavailable: !slot.available
            }"
            :disabled="!slot.available"
            @click="selectSlot(slot)"
          >
            {{ formatSlotTime(slot) }}
          </button>
        </div>
      </div>

      <!-- No Slots Available -->
      <div v-if="allSlots.length === 0" class="no-slots">
        <svg width="48" height="48" viewBox="0 0 48 48" fill="none" class="no-slots-icon">
          <circle cx="24" cy="24" r="22" stroke="#e5e7eb" stroke-width="2"/>
          <path d="M24 14v12m0 4h.01" stroke="#9ca3af" stroke-width="2" stroke-linecap="round"/>
        </svg>
        <p>متأسفانه در این تاریخ زمان خالی وجود ندارد</p>
        <p class="no-slots-hint">لطفاً تاریخ دیگری را انتخاب کنید</p>
      </div>
    </div>

    <!-- Selection Summary -->
    <div v-if="selectedSlot" class="selection-summary">
      <div class="summary-content">
        <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor" class="summary-icon">
          <path d="M10 18a8 8 0 100-16 8 8 0 000 16zm-1-11h2v5h-2V7zm0 6h2v2h-2v-2z"/>
        </svg>
        <div class="summary-text">
          <strong>زمان انتخاب شده:</strong>
          {{ formatSelectedSlotFull() }}
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { availabilityService, type TimeSlot } from '../api/availability.service'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'

interface Props {
  providerId: string
  serviceId: string
  staffMemberId?: string
  initialDate?: string
}

interface Emits {
  (e: 'slot-selected', slot: TimeSlot): void
  (e: 'slot-deselected'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const selectedDate = ref<string>('')
const selectedSlot = ref<TimeSlot | null>(null)
const allSlots = ref<TimeSlot[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

// Generate date options for next 7 days
const availableDateOptions = computed(() => {
  const options = []
  const today = new Date()

  for (let i = 0; i < 7; i++) {
    const date = new Date(today)
    date.setDate(date.getDate() + i)

    const dateStr = date.toISOString().split('T')[0]
    const dayName = getDayName(date)
    const dayNumber = convertEnglishToPersianNumbers(date.getDate().toString())
    const monthName = getMonthName(date.getMonth())

    options.push({
      value: dateStr,
      label: i === 0 ? 'امروز' : i === 1 ? 'فردا' : dayName,
      display: `${dayNumber} ${monthName}`,
    })
  }

  return options
})

// Group slots by time of day
const groupedSlots = computed(() => {
  if (allSlots.value.length === 0) return null
  return availabilityService.groupSlotsByTimeOfDay(allSlots.value)
})

// Load slots when date changes
watch(selectedDate, async (newDate) => {
  if (newDate) {
    await loadSlots()
  }
})

// Load slots for selected date
async function loadSlots() {
  if (!selectedDate.value) return

  loading.value = true
  error.value = null

  try {
    const response = await availabilityService.getAvailableSlots({
      providerId: props.providerId,
      serviceId: props.serviceId,
      date: selectedDate.value,
      staffMemberId: props.staffMemberId,
    })

    allSlots.value = response.slots
  } catch (err) {
    console.error('Error loading slots:', err)
    error.value = 'خطا در بارگذاری زمان‌های خالی'
    allSlots.value = []
  } finally {
    loading.value = false
  }
}

// Select a date
function selectDate(date: string) {
  selectedDate.value = date
  selectedSlot.value = null
  emit('slot-deselected')
}

// Select a time slot
function selectSlot(slot: TimeSlot) {
  if (!slot.available) return

  selectedSlot.value = slot
  emit('slot-selected', slot)
}

// Format slot time (HH:MM - HH:MM)
function formatSlotTime(slot: TimeSlot): string {
  const formatted = availabilityService.formatTimeSlot(slot)
  return availabilityService.toPersianTime(formatted)
}

// Format selected slot with full details
function formatSelectedSlotFull(): string {
  if (!selectedSlot.value) return ''

  const dateOption = availableDateOptions.value.find(d => d.value === selectedDate.value)
  const timeFormatted = formatSlotTime(selectedSlot.value)

  return `${dateOption?.label} ${dateOption?.display} - ${timeFormatted}`
}

// Get Persian day name
function getDayName(date: Date): string {
  const days = ['یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنج‌شنبه', 'جمعه', 'شنبه']
  return days[date.getDay()]
}

// Get Persian month name
function getMonthName(monthIndex: number): string {
  const months = [
    'ژانویه', 'فوریه', 'مارس', 'آوریل', 'مه', 'ژوئن',
    'ژوئیه', 'اوت', 'سپتامبر', 'اکتبر', 'نوامبر', 'دسامبر'
  ]
  // TODO: Use Jalaali month names
  return months[monthIndex]
}

// Initialize with today's date
onMounted(() => {
  if (props.initialDate) {
    selectedDate.value = props.initialDate
  } else {
    const today = new Date().toISOString().split('T')[0]
    selectedDate.value = today
  }
})
</script>

<style scoped lang="scss">
.time-slot-picker {
  background: white;
  border-radius: 16px;
  padding: 24px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.section-title {
  font-size: 18px;
  font-weight: 600;
  color: #111827;
  margin: 0 0 16px 0;
}

// Date Selector
.date-selector {
  margin-bottom: 24px;
}

.date-buttons {
  display: flex;
  gap: 12px;
  overflow-x: auto;
  padding-bottom: 8px;

  &::-webkit-scrollbar {
    height: 4px;
  }

  &::-webkit-scrollbar-track {
    background: #f3f4f6;
    border-radius: 2px;
  }

  &::-webkit-scrollbar-thumb {
    background: #d1d5db;
    border-radius: 2px;

    &:hover {
      background: #9ca3af;
    }
  }
}

.date-btn {
  flex-shrink: 0;
  padding: 12px 16px;
  background: white;
  border: 2px solid #e5e7eb;
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.2s;
  text-align: center;
  min-width: 100px;

  &:hover {
    border-color: #667eea;
    box-shadow: 0 2px 8px rgba(102, 126, 234, 0.1);
  }

  &.active {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    border-color: #667eea;
    color: white;

    .date-label,
    .date-value {
      color: white;
    }
  }
}

.date-label {
  font-size: 12px;
  font-weight: 500;
  color: #6b7280;
  margin-bottom: 4px;
}

.date-value {
  font-size: 14px;
  font-weight: 600;
  color: #111827;
}

// Loading State
.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  text-align: center;
  color: #6b7280;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 3px solid #e5e7eb;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-bottom: 16px;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

// Error State
.error-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  text-align: center;
  color: #ef4444;
}

.retry-btn {
  margin-top: 16px;
  padding: 8px 16px;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 14px;
  font-weight: 500;
  transition: background-color 0.2s;

  &:hover {
    background: #5568d3;
  }
}

// Time Slots
.slots-container {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.time-period {
  .period-title {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 16px;
    font-weight: 600;
    color: #374151;
    margin: 0 0 12px 0;
  }

  .period-icon {
    color: #9ca3af;
  }
}

.slots-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
  gap: 12px;
}

.slot-btn {
  padding: 12px 16px;
  background: white;
  border: 2px solid #e5e7eb;
  border-radius: 10px;
  font-size: 14px;
  font-weight: 500;
  color: #111827;
  cursor: pointer;
  transition: all 0.2s;
  text-align: center;

  &:hover:not(:disabled) {
    border-color: #667eea;
    box-shadow: 0 2px 8px rgba(102, 126, 234, 0.15);
    transform: translateY(-1px);
  }

  &.selected {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    border-color: #667eea;
    color: white;
    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
  }

  &.unavailable {
    background: #f9fafb;
    border-color: #e5e7eb;
    color: #9ca3af;
    cursor: not-allowed;
    opacity: 0.6;
  }

  &:disabled {
    cursor: not-allowed;
  }
}

// No Slots
.no-slots {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  text-align: center;
  color: #6b7280;

  .no-slots-icon {
    margin-bottom: 16px;
    opacity: 0.5;
  }

  p {
    margin: 0 0 8px 0;
    font-size: 16px;
    font-weight: 500;
    color: #374151;
  }

  .no-slots-hint {
    font-size: 14px;
    color: #9ca3af;
  }
}

// Selection Summary
.selection-summary {
  margin-top: 24px;
  padding: 16px;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
  border: 2px solid #667eea;
  border-radius: 12px;
}

.summary-content {
  display: flex;
  align-items: center;
  gap: 12px;
}

.summary-icon {
  color: #667eea;
  flex-shrink: 0;
}

.summary-text {
  font-size: 14px;
  color: #111827;

  strong {
    font-weight: 600;
    color: #667eea;
  }
}

// Responsive
@media (max-width: 768px) {
  .time-slot-picker {
    padding: 16px;
  }

  .slots-grid {
    grid-template-columns: repeat(auto-fill, minmax(100px, 1fr));
    gap: 8px;
  }

  .slot-btn {
    padding: 10px 12px;
    font-size: 13px;
  }
}
</style>
