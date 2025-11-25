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
            <p v-if="!validationMessages.length">متأسفانه برای این روز زمان خالی موجود نیست. لطفاً روز دیگری را انتخاب کنید.</p>
            <div v-else class="validation-messages">
              <p v-for="(message, index) in validationMessages" :key="index" class="validation-message">
                {{ message }}
              </p>
            </div>
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
// import type { TimeSlot as AvailabilityTimeSlot } from '@/modules/booking/api/availability.service'

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
const validationMessages = ref<string[]>([])

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

// Watch for date selection from props
watch(() => props.selectedDate, (newDate) => {
  selectedDate.value = newDate
  if (newDate) {
    selectedDateModel.value = newDate
  }
})

// Watch for date selection from calendar v-model
watch(selectedDateModel, (newDateValue) => {
  console.log('[SlotSelection] selectedDateModel changed:', newDateValue)
  if (newDateValue) {
    handleDateChange(newDateValue)
  }
})

// Methods
const handleDateChange = async (dateValue: any) => {
  console.log('[SlotSelection] handleDateChange called with:', dateValue, 'Type:', typeof dateValue)

  if (!dateValue) {
    console.warn('[SlotSelection] No date value provided')
    return
  }

  // Extract clean date string from various possible formats
  let dateString: string = ''

  if (typeof dateValue === 'string') {
    // Already a string - use directly
    dateString = dateValue
    console.log('[SlotSelection] Date is string:', dateString)
  } else if (typeof dateValue === 'object') {
    // Could be moment object or Date object
    console.log('[SlotSelection] Date is object, keys:', Object.keys(dateValue))

    // Try moment object format (_i property)
    if (dateValue._i) {
      // Check if _i is a string before splitting
      if (typeof dateValue._i === 'string') {
        dateString = dateValue._i.split('T')[0]
        console.log('[SlotSelection] Extracted from moment._i (string):', dateString)
      } else if (typeof dateValue._i === 'object' && dateValue._i.year) {
        // _i might be an object with year, month, day properties
        const year = dateValue._i.year
        const month = String(dateValue._i.month + 1).padStart(2, '0') // month is 0-indexed
        const day = String(dateValue._i.date || dateValue._i.day).padStart(2, '0')
        dateString = `${year}-${month}-${day}`
        console.log('[SlotSelection] Extracted from moment._i (object):', dateString)
      }
    }
    // Try formatted value
    else if (dateValue.format && typeof dateValue.format === 'function') {
      dateString = dateValue.format('YYYY-MM-DD')
      console.log('[SlotSelection] Extracted using .format():', dateString)
    }
    // Try direct conversion
    else if (dateValue.toString) {
      const str = dateValue.toString()
      // Extract date pattern YYYY-MM-DD
      const match = str.match(/\d{4}-\d{2}-\d{2}/)
      if (match) {
        dateString = match[0]
        console.log('[SlotSelection] Extracted from toString:', dateString)
      }
    }
  }

  // Validate we got a proper date string
  if (!dateString || !/^\d{4}-\d{2}-\d{2}$/.test(dateString)) {
    console.error('[SlotSelection] Could not extract valid date string from:', dateValue)
    console.error('[SlotSelection] Extracted value:', dateString)
    return
  }

  console.log('[SlotSelection] ✅ Final date string:', dateString)
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

    // Capture validation messages if present
    validationMessages.value = response.validationMessages || []

    // Filter for available slots and map to our interface
    availableSlots.value = response.slots
      .filter(slot => slot.isAvailable)
      .map(slot => ({
        startTime: new Date(slot.startTime).toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit', hour12: false }),
        endTime: new Date(slot.endTime).toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit', hour12: false }),
        staffId: slot.availableStaffId || '',
        staffName: slot.availableStaffName || 'کارشناس',
        available: true,
      }))

    console.log('[SlotSelection] Processed slots:', availableSlots.value)
    console.log('[SlotSelection] Validation messages:', validationMessages.value)
  } catch (error) {
    console.error('[SlotSelection] Failed to load slots:', error)
    availableSlots.value = []
    validationMessages.value = []
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

const formatSelectedDate = (dateString: string | null): string => {
  if (!dateString || typeof dateString !== 'string') {
    return ''
  }

  const weekDays = ['یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنج‌شنبه', 'جمعه', 'شنبه']
  const persianMonths = [
    'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
    'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
  ]

  // Convert Gregorian to Jalali/Persian
  const [year, month, day] = dateString.split('-').map(Number)
  const date = new Date(year, month - 1, day)
  const dayName = weekDays[date.getDay()]

  // Jalali conversion algorithm
  const jalaliDate = gregorianToJalali(year, month, day)
  const persianYear = convertToPersianNumber(jalaliDate[0])
  const persianMonth = convertToPersianNumber(jalaliDate[1])
  const persianDay = convertToPersianNumber(jalaliDate[2])
  const monthName = persianMonths[jalaliDate[1] - 1]

  return `${dayName}، ${persianDay} ${monthName} ${persianYear}`
}

const gregorianToJalali = (gy: number, gm: number, gd: number): [number, number, number] => {
  const g_d_m = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334]

  let jy = gy <= 1600 ? 0 : 979
  gy -= gy <= 1600 ? 621 : 1600

  const gy2 = gm > 2 ? gy + 1 : gy
  let days = 365 * gy + Math.floor((gy2 + 3) / 4) - Math.floor((gy2 + 99) / 100) +
             Math.floor((gy2 + 399) / 400) - 80 + gd + g_d_m[gm - 1]

  jy += 33 * Math.floor(days / 12053)
  days %= 12053
  jy += 4 * Math.floor(days / 1461)
  days %= 1461

  if (days > 365) {
    jy += Math.floor((days - 1) / 365)
    days = (days - 1) % 365
  }

  const jm = days < 186 ? 1 + Math.floor(days / 31) : 7 + Math.floor((days - 186) / 30)
  const jd = 1 + (days < 186 ? days % 31 : (days - 186) % 30)

  return [jy, jm, jd]
}

const convertToPersianTime = (time: string): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return time.split('').map(char => {
    const digit = parseInt(char)
    return !isNaN(digit) ? persianDigits[digit] : char
  }).join('')
}

const convertToPersianNumber = (num: number | string): string => {
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
  font-size: 1.1rem;
  min-height: 400px;
}

.persian-calendar-wrapper :deep(.vpd-container) {
  width: 100%;
}

.persian-calendar-wrapper :deep(.vpd-icon-btn) {
  width: 36px;
  height: 36px;
}

.persian-calendar-wrapper :deep(.vpd-day) {
  height: 42px;
  line-height: 42px;
  font-size: 1rem;
}

.persian-calendar-wrapper :deep(.vpd-week-day) {
  height: 36px;
  font-size: 0.95rem;
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

.validation-messages {
  margin-top: 1rem;
  text-align: center;
}

.validation-message {
  font-size: 0.95rem;
  color: #dc2626;
  background-color: #fee2e2;
  padding: 0.75rem 1rem;
  border-radius: 0.5rem;
  margin: 0.5rem 0;
  border-right: 4px solid #dc2626;
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
