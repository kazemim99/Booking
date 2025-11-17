<template>
  <div class="slot-selection" dir="rtl">
    <div class="step-header">
      <h2 class="step-title">انتخاب تاریخ و زمان</h2>
      <p class="step-description">
        زمان مناسب برای رزرو خود را انتخاب کنید
      </p>
    </div>

    <div class="selection-container">
      <!-- Persian Calendar Section -->
      <div class="calendar-section">
        <div class="persian-calendar-wrapper">
          <h3>انتخاب تاریخ</h3>
          <VuePersianDatetimePicker
            v-model="selectedDateModel"
            display-format="jYYYY/jMM/jDD"
            format="YYYY-MM-DD"
            :min="minDate"
            :max="maxDate"
            :auto-submit="true"
            :clearable="false"
            :inline="true"
            type="date"
            @change="handleDateChange"
          />
        </div>
      </div>

      <!-- Time Slots Section -->
      <div class="slots-section">
        <div v-if="!selectedDate" class="no-date-selected">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
          <h3>یک تاریخ را انتخاب کنید</h3>
          <p>لطفاً ابتدا یک تاریخ از تقویم انتخاب کنید</p>
        </div>

        <div v-else class="slots-content">
          <div class="selected-date-header">
            <h3>{{ formatSelectedDate(selectedDate) }}</h3>
            <p>زمان‌های موجود برای این روز</p>
          </div>

          <!-- Loading -->
          <div v-if="loadingSlots" class="loading-slots">
            <div class="loading-spinner"></div>
            <p>در حال بارگذاری زمان‌های موجود...</p>
          </div>

          <!-- Time Slots Grid -->
          <div v-else-if="availableSlots.length > 0" class="time-slots-grid">
            <div
              v-for="slot in availableSlots"
              :key="`${slot.startTime}-${slot.staffId}`"
              class="time-slot"
              :class="{ selected: selectedTime === slot.startTime && selectedStaffId === slot.staffId }"
              @click="selectTimeSlot(slot)"
            >
              <div class="slot-time">{{ convertToPersianTime(slot.startTime) }}</div>
              <div class="slot-staff">{{ slot.staffName }}</div>
            </div>
          </div>

          <!-- No Slots -->
          <div v-else class="no-slots">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <h3>زمانی موجود نیست</h3>
            <p>متأسفانه برای این روز زمان خالی موجود نیست. لطفاً روز دیگری را انتخاب کنید.</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import VuePersianDatetimePicker from 'vue3-persian-datetime-picker'
import { availabilityService } from '@/modules/booking/api/availability.service'
import type { TimeSlot as AvailabilityTimeSlot } from '@/modules/booking/api/availability.service'

interface TimeSlot {
  startTime: string
  endTime: string
  staffId: string
  staffName: string
  available: boolean
}

interface Props {
  providerId: string
  serviceId: string | null
  selectedDate: string | null
  selectedTime: string | null
  selectedStaffId: string | null
}

const props = defineProps<Props>()

const emit = defineEmits<{
  (e: 'slot-selected', slot: { date: string; startTime: string; endTime: string; staffId: string; staffName: string }): void
}>()

// State
const selectedDate = ref(props.selectedDate)
const selectedDateModel = ref(props.selectedDate || '')
const loadingSlots = ref(false)
const availableSlots = ref<TimeSlot[]>([])

// Computed
const minDate = computed(() => {
  const today = new Date()
  return today.toISOString().split('T')[0]
})

const maxDate = computed(() => {
  const maxDate = new Date()
  maxDate.setMonth(maxDate.getMonth() + 3) // 3 months ahead
  return maxDate.toISOString().split('T')[0]
})

// Watch for date selection
watch(() => props.selectedDate, (newDate) => {
  selectedDate.value = newDate
  if (newDate) {
    selectedDateModel.value = newDate
  }
})

// Methods
const handleDateChange = async (dateString: string) => {
  if (!dateString) return

  selectedDate.value = dateString
  await loadAvailableSlots(dateString)
}

const loadAvailableSlots = async (dateString: string) => {
  if (!props.serviceId) {
    console.warn('[SlotSelection] No service selected')
    return
  }

  loadingSlots.value = true

  try {
    console.log('[SlotSelection] Fetching slots for:', { providerId: props.providerId, serviceId: props.serviceId, date: dateString })

    // Call real availability API
    const response = await availabilityService.getAvailableSlots({
      providerId: props.providerId,
      serviceId: props.serviceId!,
      date: dateString,
    })

    console.log('[SlotSelection] Slots received:', response)

    // Filter for available slots and map to our interface
    availableSlots.value = response.slots
      .filter(slot => slot.available)
      .map(slot => ({
        startTime: new Date(slot.startTime).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false }),
        endTime: new Date(slot.endTime).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false }),
        staffId: slot.staffMemberId || '',
        staffName: slot.staffName || 'کارشناس',
        available: true,
      }))

    console.log('[SlotSelection] Processed slots:', availableSlots.value)
  } catch (error) {
    console.error('[SlotSelection] Failed to load slots:', error)
    availableSlots.value = []
  } finally {
    loadingSlots.value = false
  }
}

const selectTimeSlot = (slot: TimeSlot) => {
  emit('slot-selected', {
    date: selectedDate.value!,
    startTime: slot.startTime,
    endTime: slot.endTime,
    staffId: slot.staffId,
    staffName: slot.staffName,
  })
}

const formatSelectedDate = (dateString: string): string => {
  const date = new Date(dateString)
  const weekDays = ['یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنج‌شنبه', 'جمعه', 'شنبه']
  const persianMonths = [
    'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
    'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
  ]

  const dayName = weekDays[date.getDay()]

  // Format date string for display
  const formattedDate = convertToPersianNumber(dateString)

  return `${dayName}، ${formattedDate}`
}

const convertToPersianTime = (time: string): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return time.split('').map(char => {
    const digit = parseInt(char)
    return !isNaN(digit) ? persianDigits[digit] : char
  }).join('')
}

const convertToPersianNumber = (num: number): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return num.toString().split('').map(char => {
    const digit = parseInt(char)
    return !isNaN(digit) ? persianDigits[digit] : char
  }).join('')
}
</script>

<style scoped>
.slot-selection {
  padding: 0;
}

.step-header {
  text-align: center;
  margin-bottom: 3rem;
}

.step-title {
  font-size: 2rem;
  font-weight: 800;
  color: #1e293b;
  margin: 0 0 0.75rem 0;
}

.step-description {
  font-size: 1.05rem;
  color: #64748b;
  margin: 0;
}

.selection-container {
  display: grid;
  grid-template-columns: 1.2fr 1fr;
  gap: 2rem;
}

.calendar-section {
  /* Calendar takes 60% */
}

.persian-calendar-wrapper {
  background: white;
  border-radius: 20px;
  padding: 2rem;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
}

.persian-calendar-wrapper h3 {
  font-size: 1.25rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 1.5rem 0;
  text-align: center;
}

/* Persian Calendar Custom Styles */
.persian-calendar-wrapper :deep(.vpd-main) {
  width: 100%;
  box-shadow: none;
  border: none;
}

.persian-calendar-wrapper :deep(.vpd-header) {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.persian-calendar-wrapper :deep(.vpd-day-effect) {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.persian-calendar-wrapper :deep(.vpd-selected) {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
}

.slots-section {
  background: #f8fafc;
  border-radius: 20px;
  padding: 2rem;
  display: flex;
  flex-direction: column;
}

.no-date-selected {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 3rem 2rem;
  height: 100%;
}

.no-date-selected svg {
  width: 64px;
  height: 64px;
  color: #cbd5e1;
  margin-bottom: 1.5rem;
}

.no-date-selected h3 {
  font-size: 1.25rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.5rem 0;
}

.no-date-selected p {
  font-size: 0.95rem;
  color: #64748b;
  margin: 0;
}

.selected-date-header {
  margin-bottom: 1.5rem;
  padding-bottom: 1.5rem;
  border-bottom: 2px solid #e2e8f0;
}

.selected-date-header h3 {
  font-size: 1.25rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.375rem 0;
}

.selected-date-header p {
  font-size: 0.95rem;
  color: #64748b;
  margin: 0;
}

.loading-slots {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem 2rem;
  gap: 1rem;
}

.loading-spinner {
  width: 40px;
  height: 40px;
  border: 4px solid #e2e8f0;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.loading-slots p {
  font-size: 0.95rem;
  color: #64748b;
}

.time-slots-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 0.75rem;
  max-height: 400px;
  overflow-y: auto;
  padding: 0.25rem;
}

.time-slot {
  background: white;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  padding: 1rem;
  cursor: pointer;
  transition: all 0.3s;
  text-align: center;
}

.time-slot:hover {
  border-color: #cbd5e1;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
}

.time-slot.selected {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-color: #667eea;
  color: white;
}

.slot-time {
  font-size: 1.125rem;
  font-weight: 700;
  color: #1e293b;
  margin-bottom: 0.25rem;
}

.time-slot.selected .slot-time {
  color: white;
}

.slot-staff {
  font-size: 0.875rem;
  color: #64748b;
}

.time-slot.selected .slot-staff {
  color: rgba(255, 255, 255, 0.9);
}

.no-slots {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 3rem 2rem;
}

.no-slots svg {
  width: 64px;
  height: 64px;
  color: #f59e0b;
  margin-bottom: 1.5rem;
}

.no-slots h3 {
  font-size: 1.25rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.5rem 0;
}

.no-slots p {
  font-size: 0.95rem;
  color: #64748b;
  margin: 0;
}

/* Responsive */
@media (max-width: 1024px) {
  .selection-container {
    grid-template-columns: 1fr;
  }

  .time-slots-grid {
    max-height: none;
  }
}

@media (max-width: 768px) {
  .step-title {
    font-size: 1.75rem;
  }

  .slots-section {
    padding: 1.5rem;
  }

  .time-slots-grid {
    grid-template-columns: 1fr;
  }
}
</style>
