<template>
  <div class="availability-calendar" dir="rtl">
    <!-- Calendar Header -->
    <div class="calendar-header">
      <button class="nav-btn" @click="previousWeek">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
      </button>

      <div class="month-year">
        <h3>{{ currentMonthYear }}</h3>
      </div>

      <button class="nav-btn" @click="nextWeek">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
      </button>
    </div>

    <!-- Day Headers -->
    <div class="day-headers">
      <div v-for="day in weekDays" :key="day" class="day-header">
        {{ day }}
      </div>
    </div>

    <!-- Calendar Days -->
    <div class="calendar-grid">
      <div
        v-for="day in calendarDays"
        :key="day.dateString"
        class="calendar-day"
        :class="{
          today: day.isToday,
          selected: day.dateString === selectedDate,
          disabled: day.isPast || !day.hasAvailability,
          'other-month': !day.isCurrentMonth
        }"
        @click="selectDate(day)"
      >
        <div class="day-number">{{ convertToPersianNumber(day.day) }}</div>
        <div v-if="day.availableSlots > 0" class="availability-indicator">
          <div class="dots">
            <span v-for="i in Math.min(day.availableSlots, 3)" :key="i" class="dot"></span>
          </div>
          <span class="slots-count">{{ convertToPersianNumber(day.availableSlots) }} موجود</span>
        </div>
        <div v-else-if="!day.isPast && day.isCurrentMonth" class="no-availability">
          تکمیل
        </div>
      </div>
    </div>

    <!-- Legend -->
    <div class="calendar-legend">
      <div class="legend-item">
        <div class="legend-dot available"></div>
        <span>زمان‌های موجود</span>
      </div>
      <div class="legend-item">
        <div class="legend-dot booked"></div>
        <span>تکمیل ظرفیت</span>
      </div>
      <div class="legend-item">
        <div class="legend-dot selected"></div>
        <span>انتخاب شده</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { availabilityService } from '@/modules/booking/api/availability.service'

interface CalendarDay {
  date: Date
  dateString: string
  day: number
  month: number
  year: number
  isToday: boolean
  isPast: boolean
  isCurrentMonth: boolean
  hasAvailability: boolean
  availableSlots: number
}

interface Props {
  selectedDate: string | null
  providerId: string
  serviceId: string | null
}

const props = defineProps<Props>()

const emit = defineEmits<{
  (e: 'date-selected', dateString: string): void
}>()

// State
const currentDate = ref(new Date())
const weekDays = ['ش', 'ی', 'د', 'س', 'چ', 'پ', 'ج'] // شنبه to جمعه
const availabilityData = ref<Record<string, number>>({})
const loading = ref(false)

// Computed
const currentMonthYear = computed(() => {
  const _months = [
    'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
    'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
  ]

  // This is simplified - for production, use jalaali-js for accurate conversion
  const gregorianMonths = [
    'ژانویه', 'فوریه', 'مارس', 'آوریل', 'می', 'ژوئن',
    'ژوئیه', 'اوت', 'سپتامبر', 'اکتبر', 'نوامبر', 'دسامبر'
  ]

  const month = gregorianMonths[currentDate.value.getMonth()]
  const year = convertToPersianNumber(currentDate.value.getFullYear())

  return `${month} ${year}`
})

const calendarDays = computed((): CalendarDay[] => {
  const days: CalendarDay[] = []
  const year = currentDate.value.getFullYear()
  const month = currentDate.value.getMonth()

  // Get first day of month
  const firstDay = new Date(year, month, 1)
  const lastDay = new Date(year, month + 1, 0)

  // Get day of week for first day (0 = Sunday, convert to Saturday = 0)
  let startDay = firstDay.getDay()
  startDay = startDay === 0 ? 1 : startDay + 1 // Convert to Saturday = 0
  if (startDay === 7) startDay = 0

  // Add days from previous month
  for (let i = startDay - 1; i >= 0; i--) {
    const date = new Date(year, month, -i)
    days.push(createCalendarDay(date, false))
  }

  // Add days of current month
  for (let day = 1; day <= lastDay.getDate(); day++) {
    const date = new Date(year, month, day)
    days.push(createCalendarDay(date, true))
  }

  // Add days from next month to complete the grid
  const remainingDays = 7 - (days.length % 7)
  if (remainingDays < 7) {
    for (let i = 1; i <= remainingDays; i++) {
      const date = new Date(year, month + 1, i)
      days.push(createCalendarDay(date, false))
    }
  }

  return days
})

// Methods
const fetchAvailability = async () => {
  if (!props.serviceId) {
    console.warn('[AvailabilityCalendar] No service selected, skipping availability fetch')
    return
  }

  loading.value = true
  try {
    const year = currentDate.value.getFullYear()
    const month = currentDate.value.getMonth()

    // Get first and last day of current month
    const fromDate = new Date(year, month, 1).toISOString().split('T')[0]
    const toDate = new Date(year, month + 1, 0).toISOString().split('T')[0]

    console.log('[AvailabilityCalendar] Fetching availability for:', { providerId: props.providerId, serviceId: props.serviceId, fromDate, toDate })

    const response = await availabilityService.getAvailableDates({
      providerId: props.providerId,
      serviceId: props.serviceId,
      fromDate,
      toDate,
    })

    console.log('[AvailabilityCalendar] Availability response:', response)

    // Convert to our format
    const newAvailabilityData: Record<string, number> = {}
    response.dates.forEach(dateInfo => {
      newAvailabilityData[dateInfo.date] = dateInfo.availableSlots
    })

    availabilityData.value = newAvailabilityData
  } catch (error) {
    console.error('[AvailabilityCalendar] Failed to fetch availability:', error)
    // Keep existing data or set to empty
    availabilityData.value = {}
  } finally {
    loading.value = false
  }
}

const createCalendarDay = (date: Date, isCurrentMonth: boolean): CalendarDay => {
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  date.setHours(0, 0, 0, 0)

  const dateString = date.toISOString().split('T')[0]
  const availableSlots = availabilityData.value[dateString] || 0

  return {
    date,
    dateString,
    day: date.getDate(),
    month: date.getMonth(),
    year: date.getFullYear(),
    isToday: date.getTime() === today.getTime(),
    isPast: date < today,
    isCurrentMonth,
    hasAvailability: availableSlots > 0,
    availableSlots,
  }
}

const selectDate = (day: CalendarDay) => {
  if (day.isPast || !day.hasAvailability) return
  emit('date-selected', day.dateString)
}

const previousWeek = () => {
  const newDate = new Date(currentDate.value)
  newDate.setDate(newDate.getDate() - 7)
  currentDate.value = newDate
}

const nextWeek = () => {
  const newDate = new Date(currentDate.value)
  newDate.setDate(newDate.getDate() + 7)
  currentDate.value = newDate
}

const convertToPersianNumber = (num: number): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return num.toString().split('').map(char => {
    const digit = parseInt(char)
    return !isNaN(digit) ? persianDigits[digit] : char
  }).join('')
}

// Lifecycle
onMounted(async () => {
  await fetchAvailability()
})

// Watch for service changes or month changes
watch(() => props.serviceId, async (newServiceId) => {
  if (newServiceId) {
    await fetchAvailability()
  }
})

watch(currentDate, async () => {
  await fetchAvailability()
})
</script>

<style scoped>
.availability-calendar {
  background: white;
  border-radius: 20px;
  padding: 2rem;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.06);
}

.calendar-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
}

.nav-btn {
  width: 40px;
  height: 40px;
  border-radius: 10px;
  border: 2px solid #e2e8f0;
  background: white;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.3s;
}

.nav-btn svg {
  width: 20px;
  height: 20px;
  color: #64748b;
}

.nav-btn:hover {
  background: #f8fafc;
  border-color: #cbd5e1;
}

.month-year h3 {
  font-size: 1.375rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0;
}

.day-headers {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 0.5rem;
  margin-bottom: 0.75rem;
}

.day-header {
  text-align: center;
  font-size: 0.875rem;
  font-weight: 700;
  color: #64748b;
  padding: 0.5rem;
}

.calendar-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 0.5rem;
  margin-bottom: 2rem;
}

.calendar-day {
  aspect-ratio: 1;
  border-radius: 12px;
  padding: 0.75rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.3s;
  border: 2px solid transparent;
  background: #f8fafc;
  position: relative;
}

.calendar-day:not(.disabled):hover {
  background: #e0e7ff;
  border-color: #818cf8;
  transform: scale(1.05);
}

.calendar-day.today {
  border-color: #667eea;
}

.calendar-day.selected {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-color: #667eea;
  color: white;
}

.calendar-day.selected .day-number {
  color: white;
}

.calendar-day.selected .availability-indicator {
  color: white;
}

.calendar-day.disabled {
  opacity: 0.4;
  cursor: not-allowed;
  background: #f1f5f9;
}

.calendar-day.other-month {
  opacity: 0.3;
}

.day-number {
  font-size: 1.125rem;
  font-weight: 700;
  color: #1e293b;
  margin-bottom: 0.25rem;
}

.availability-indicator {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.25rem;
}

.dots {
  display: flex;
  gap: 2px;
}

.dot {
  width: 4px;
  height: 4px;
  border-radius: 50%;
  background: #10b981;
}

.calendar-day.selected .dot {
  background: white;
}

.slots-count {
  font-size: 0.625rem;
  font-weight: 600;
  color: #10b981;
}

.calendar-day.selected .slots-count {
  color: white;
}

.no-availability {
  font-size: 0.625rem;
  font-weight: 600;
  color: #ef4444;
}

.calendar-legend {
  display: flex;
  gap: 2rem;
  justify-content: center;
  padding-top: 1.5rem;
  border-top: 2px solid #f1f5f9;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #64748b;
}

.legend-dot {
  width: 16px;
  height: 16px;
  border-radius: 4px;
}

.legend-dot.available {
  background: #10b981;
}

.legend-dot.booked {
  background: #ef4444;
}

.legend-dot.selected {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

/* Responsive */
@media (max-width: 768px) {
  .availability-calendar {
    padding: 1.5rem;
  }

  .calendar-day {
    padding: 0.5rem;
  }

  .day-number {
    font-size: 1rem;
  }

  .slots-count,
  .no-availability {
    font-size: 0.5rem;
  }

  .calendar-legend {
    flex-direction: column;
    gap: 0.75rem;
    align-items: flex-start;
  }
}
</style>
