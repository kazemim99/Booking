<template>
  <div class="booking-calendar">
    <!-- Debug Info (temporary) -->
    <div v-if="bookings.length > 0" class="debug-info">
      مجموع رزروها: {{ convertToPersian(bookings.length) }} |
      رزروهای این ماه: {{ convertToPersian(currentMonthBookingsCount) }}
    </div>

    <!-- Calendar Header -->
    <div class="calendar-header">
      <button class="nav-btn" @click="previousMonth">
        <svg class="nav-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
      </button>
      <h2 class="calendar-title">{{ currentMonthName }} {{ currentYear }}</h2>
      <button class="nav-btn" @click="nextMonth">
        <svg class="nav-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
      </button>
    </div>

    <!-- Weekday Headers -->
    <div class="calendar-grid">
      <div v-for="day in weekDays" :key="day" class="weekday-header">
        {{ day }}
      </div>

      <!-- Calendar Days -->
      <div
        v-for="day in calendarDays"
        :key="`${day.date}`"
        :class="[
          'calendar-day',
          {
            'other-month': day.isOtherMonth,
            'today': day.isToday,
            'selected': isSelectedDay(day),
            'has-bookings': day.bookingsCount > 0
          }
        ]"
        @click="selectDay(day)"
      >
        <div class="day-number">{{ day.dayNumber }}</div>
        <div v-if="day.bookingsCount > 0" class="bookings-badge">
          {{ convertToPersian(day.bookingsCount) }}
        </div>
      </div>
    </div>

    <!-- Selected Day Bookings -->
    <div v-if="selectedDay && selectedDayBookings.length > 0" class="day-bookings">
      <h3 class="day-bookings-title">
        رزروهای {{ selectedDay.dayNumber }} {{ currentMonthName }}
      </h3>
      <div class="bookings-list">
        <div
          v-for="booking in selectedDayBookings"
          :key="booking.id"
          class="booking-item"
          @click="$emit('booking-click', booking)"
        >
          <div class="booking-time">{{ booking.time }}</div>
          <div class="booking-info">
            <div class="booking-customer">{{ booking.customerName }}</div>
            <div class="booking-service">{{ booking.service }}</div>
          </div>
          <div :class="['booking-status', `status-${booking.status}`]">
            {{ getStatusLabel(booking.status) }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'

interface Booking {
  id: string
  customerName: string
  service: string
  date: string
  time: string
  status: string
}

interface CalendarDay {
  date: Date
  dayNumber: number
  isOtherMonth: boolean
  isToday: boolean
  bookingsCount: number
}

interface Props {
  bookings: Booking[]
}

const props = defineProps<Props>()

const emit = defineEmits<{
  'booking-click': [booking: Booking]
}>()

const currentDate = ref(new Date())
const selectedDay = ref<CalendarDay | null>(null)

const weekDays = ['شنبه', 'یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنج‌شنبه', 'جمعه']

const persianMonths = [
  'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
  'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
]

const currentMonthName = computed(() => {
  const month = currentDate.value.getMonth()
  // Simple approximation for demo - in production use proper Jalali conversion
  const jalaliMonth = (month + 9) % 12
  return persianMonths[jalaliMonth]
})

const currentYear = computed(() => {
  return convertEnglishToPersianNumbers((currentDate.value.getFullYear() - 621).toString())
})

const currentMonthBookingsCount = computed(() => {
  const year = currentDate.value.getFullYear()
  const month = currentDate.value.getMonth() + 1
  return props.bookings.filter(booking => {
    const [bookingYear, bookingMonth] = booking.date.split('-')
    return parseInt(bookingYear) === year && parseInt(bookingMonth) === month
  }).length
})

const calendarDays = computed(() => {
  const year = currentDate.value.getFullYear()
  const month = currentDate.value.getMonth()

  const firstDay = new Date(year, month, 1)
  const lastDay = new Date(year, month + 1, 0)

  // Adjust Saturday as first day (day 6 in JS, we want it as 0)
  const firstDayOfWeek = (firstDay.getDay() + 1) % 7
  const daysInMonth = lastDay.getDate()

  const days: CalendarDay[] = []

  // Previous month days
  const prevMonthLastDay = new Date(year, month, 0)
  const prevMonthDays = prevMonthLastDay.getDate()
  for (let i = firstDayOfWeek - 1; i >= 0; i--) {
    const date = new Date(year, month - 1, prevMonthDays - i)
    days.push({
      date,
      dayNumber: prevMonthDays - i,
      isOtherMonth: true,
      isToday: false,
      bookingsCount: getBookingsCountForDate(date)
    })
  }

  // Current month days
  const today = new Date()
  for (let i = 1; i <= daysInMonth; i++) {
    const date = new Date(year, month, i)
    const isToday = date.toDateString() === today.toDateString()
    days.push({
      date,
      dayNumber: i,
      isOtherMonth: false,
      isToday,
      bookingsCount: getBookingsCountForDate(date)
    })
  }

  // Next month days
  const remainingDays = 42 - days.length // 6 rows * 7 days
  for (let i = 1; i <= remainingDays; i++) {
    const date = new Date(year, month + 1, i)
    days.push({
      date,
      dayNumber: i,
      isOtherMonth: true,
      isToday: false,
      bookingsCount: getBookingsCountForDate(date)
    })
  }

  return days
})

const selectedDayBookings = computed(() => {
  if (!selectedDay.value) return []

  const selectedDate = formatDateToString(selectedDay.value.date)
  return props.bookings.filter(booking => booking.date === selectedDate)
})

const formatDateToString = (date: Date): string => {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

const getBookingsCountForDate = (date: Date) => {
  const dateStr = formatDateToString(date)
  return props.bookings.filter(booking => booking.date === dateStr).length
}

const isSelectedDay = (day: CalendarDay) => {
  if (!selectedDay.value) return false
  return day.date.toDateString() === selectedDay.value.date.toDateString()
}

const selectDay = (day: CalendarDay) => {
  selectedDay.value = day
}

const previousMonth = () => {
  currentDate.value = new Date(
    currentDate.value.getFullYear(),
    currentDate.value.getMonth() - 1,
    1
  )
  selectedDay.value = null
}

const nextMonth = () => {
  currentDate.value = new Date(
    currentDate.value.getFullYear(),
    currentDate.value.getMonth() + 1,
    1
  )
  selectedDay.value = null
}

const convertToPersian = (num: number) => {
  return convertEnglishToPersianNumbers(num.toString())
}

const getStatusLabel = (status: string) => {
  const labels: Record<string, string> = {
    pending: 'در انتظار تایید',
    confirmed: 'تایید شده',
    completed: 'انجام شده',
    cancelled: 'لغو شده',
  }
  return labels[status] || status
}

// Auto-select today on mount
watch(() => props.bookings, () => {
  if (!selectedDay.value) {
    const today = calendarDays.value.find(day => day.isToday && !day.isOtherMonth)
    if (today) {
      selectedDay.value = today
    }
  }
}, { immediate: true })
</script>

<style scoped lang="scss">
.booking-calendar {
  background: white;
  border-radius: 8px;
  padding: 24px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.debug-info {
  padding: 12px;
  background: #fff3cd;
  border-radius: 8px;
  margin-bottom: 16px;
  font-size: 14px;
  color: #856404;
  text-align: center;
  font-weight: 600;
}

.calendar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
}

.calendar-title {
  font-size: 20px;
  font-weight: 600;
  color: #1f2937;
  margin: 0;
}

.nav-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border: none;
  background: #f3f4f6;
  border-radius: 50%;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: #e5e7eb;
  }

  &:active {
    transform: scale(0.95);
  }
}

.nav-icon {
  width: 20px;
  height: 20px;
  color: #6b7280;
}

.calendar-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 4px;
}

.weekday-header {
  padding: 12px;
  text-align: center;
  font-size: 14px;
  font-weight: 600;
  color: #6b7280;
  background: #f9fafb;
  border-radius: 4px;
}

.calendar-day {
  position: relative;
  aspect-ratio: 1;
  padding: 8px;
  border: 2px solid #e5e7eb;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  background: white;

  &:hover {
    border-color: #1976d2;
    transform: translateY(-2px);
    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  }

  &.other-month {
    opacity: 0.3;
  }

  &.today {
    background: #e3f2fd;
    border-color: #1976d2;
  }

  &.selected {
    background: #1976d2;
    border-color: #1976d2;

    .day-number {
      color: white;
    }

    .bookings-badge {
      background: white;
      color: #1976d2;
    }
  }

  &.has-bookings {
    border-color: #4caf50;
  }
}

.day-number {
  font-size: 16px;
  font-weight: 600;
  color: #1f2937;
  text-align: center;
}

.bookings-badge {
  position: absolute;
  bottom: 4px;
  right: 4px;
  min-width: 20px;
  height: 20px;
  padding: 0 6px;
  background: #4caf50;
  color: white;
  border-radius: 10px;
  font-size: 11px;
  font-weight: 600;
  display: flex;
  align-items: center;
  justify-content: center;
}

.day-bookings {
  margin-top: 32px;
  padding-top: 24px;
  border-top: 2px solid #e5e7eb;
}

.day-bookings-title {
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 16px 0;
}

.bookings-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.booking-item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px;
  background: #f9fafb;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: #f3f4f6;
    transform: translateX(-4px);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  }
}

.booking-time {
  font-size: 14px;
  font-weight: 600;
  color: #1976d2;
  min-width: 60px;
}

.booking-info {
  flex: 1;
}

.booking-customer {
  font-size: 15px;
  font-weight: 600;
  color: #1f2937;
  margin-bottom: 4px;
}

.booking-service {
  font-size: 13px;
  color: #6b7280;
}

.booking-status {
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
  white-space: nowrap;
}

.status-pending {
  background: #fff3cd;
  color: #856404;
}

.status-confirmed {
  background: #d1ecf1;
  color: #0c5460;
}

.status-completed {
  background: #d4edda;
  color: #155724;
}

.status-cancelled {
  background: #f8d7da;
  color: #721c24;
}
</style>
