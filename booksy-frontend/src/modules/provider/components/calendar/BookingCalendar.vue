<template>
  <div class="booking-calendar">
    <!-- Debug Info (temporary) -->
    <div v-if="bookings.length > 0" class="debug-info">
      مجموع رزروها: {{ convertToPersian(bookings.length) }} |
      رزروهای این ماه: {{ convertToPersian(currentMonthBookingsCount) }} |
      رزروهای روز انتخاب شده: {{ selectedDay ? convertToPersian(selectedDayBookings.length) : '۰' }}
    </div>

    <!-- Calendar Header -->
    <div class="calendar-header">
      <button class="nav-btn" @click="previousMonth">
        <svg class="nav-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
      </button>
      <h2 class="calendar-title">{{ jalaliMonthNames[currentJalaliMonth - 1] }} {{ convertToPersian(currentJalaliYear.toString()) }}</h2>
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
        v-for="(day, index) in calendarDays"
        :key="`day-${index}`"
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
        <div class="day-number">{{ convertToPersian(day.dayNumber.toString()) }}</div>
        <div v-if="day.bookingsCount > 0" class="bookings-badge">
          {{ convertToPersian(day.bookingsCount.toString()) }}
        </div>
      </div>
    </div>

    <!-- Selected Day Bookings -->
    <div v-if="selectedDay" class="day-bookings">
      <h3 class="day-bookings-title">
        رزروهای {{ convertToPersian(selectedDay.dayNumber.toString()) }} {{ jalaliMonthNames[currentJalaliMonth - 1] }}
      </h3>
      <div v-if="selectedDayBookings.length === 0" class="no-bookings">
        هیچ رزروی برای این روز وجود ندارد
      </div>
      <div v-else class="bookings-list">
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
import { convertEnglishToPersianNumbers, gregorianToJalali } from '@/shared/utils/date/jalali.utils'
import jalaali from 'jalaali-js'

interface Booking {
  id: string
  customerName: string
  service: string
  date: string
  time: string
  status: string
}

interface CalendarDay {
  gregorianDate: Date
  jalaliDay: number
  jalaliMonth: number
  jalaliYear: number
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

// Current Jalali date
const today = new Date()
const todayJalali = gregorianToJalali(today)
const currentJalaliYear = ref(todayJalali.year)
const currentJalaliMonth = ref(todayJalali.month)

const selectedDay = ref<CalendarDay | null>(null)

const weekDays = ['شنبه', 'یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنج‌شنبه', 'جمعه']

const jalaliMonthNames = [
  'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
  'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
]

const currentMonthBookingsCount = computed(() => {
  return props.bookings.filter(booking => {
    const bookingDate = new Date(booking.date)
    const bookingJalali = gregorianToJalali(bookingDate)
    return bookingJalali.year === currentJalaliYear.value &&
           bookingJalali.month === currentJalaliMonth.value
  }).length
})

const calendarDays = computed(() => {
  const days: CalendarDay[] = []

  // Get first day of current Jalali month in Gregorian
  const firstDayGregorian = jalaali.toGregorian(
    currentJalaliYear.value,
    currentJalaliMonth.value,
    1
  )
  const firstDate = new Date(firstDayGregorian.gy, firstDayGregorian.gm - 1, firstDayGregorian.gd)

  // Get days in current Jalali month
  const daysInMonth = jalaali.jalaaliMonthLength(currentJalaliYear.value, currentJalaliMonth.value)

  // Adjust Saturday as first day (day 6 in JS Date, we want it as 0)
  let firstDayOfWeek = (firstDate.getDay() + 1) % 7

  // Previous month days
  const prevMonth = currentJalaliMonth.value === 1 ? 12 : currentJalaliMonth.value - 1
  const prevYear = currentJalaliMonth.value === 1 ? currentJalaliYear.value - 1 : currentJalaliYear.value
  const daysInPrevMonth = jalaali.jalaaliMonthLength(prevYear, prevMonth)

  for (let i = firstDayOfWeek - 1; i >= 0; i--) {
    const jalaliDay = daysInPrevMonth - i
    const gregorian = jalaali.toGregorian(prevYear, prevMonth, jalaliDay)
    const gregorianDate = new Date(gregorian.gy, gregorian.gm - 1, gregorian.gd)

    days.push({
      gregorianDate,
      jalaliDay,
      jalaliMonth: prevMonth,
      jalaliYear: prevYear,
      dayNumber: jalaliDay,
      isOtherMonth: true,
      isToday: isSameDay(gregorianDate, today),
      bookingsCount: getBookingsCountForDate(gregorianDate)
    })
  }

  // Current month days
  for (let i = 1; i <= daysInMonth; i++) {
    const gregorian = jalaali.toGregorian(currentJalaliYear.value, currentJalaliMonth.value, i)
    const gregorianDate = new Date(gregorian.gy, gregorian.gm - 1, gregorian.gd)

    days.push({
      gregorianDate,
      jalaliDay: i,
      jalaliMonth: currentJalaliMonth.value,
      jalaliYear: currentJalaliYear.value,
      dayNumber: i,
      isOtherMonth: false,
      isToday: isSameDay(gregorianDate, today),
      bookingsCount: getBookingsCountForDate(gregorianDate)
    })
  }

  // Next month days
  const nextMonth = currentJalaliMonth.value === 12 ? 1 : currentJalaliMonth.value + 1
  const nextYear = currentJalaliMonth.value === 12 ? currentJalaliYear.value + 1 : currentJalaliYear.value
  const remainingDays = 42 - days.length // 6 rows * 7 days

  for (let i = 1; i <= remainingDays; i++) {
    const gregorian = jalaali.toGregorian(nextYear, nextMonth, i)
    const gregorianDate = new Date(gregorian.gy, gregorian.gm - 1, gregorian.gd)

    days.push({
      gregorianDate,
      jalaliDay: i,
      jalaliMonth: nextMonth,
      jalaliYear: nextYear,
      dayNumber: i,
      isOtherMonth: true,
      isToday: isSameDay(gregorianDate, today),
      bookingsCount: getBookingsCountForDate(gregorianDate)
    })
  }

  return days
})

const selectedDayBookings = computed(() => {
  if (!selectedDay.value) return []

  const selectedDate = formatDateToString(selectedDay.value.gregorianDate)
  const bookings = props.bookings.filter(booking => booking.date === selectedDate)

  console.log('Selected date:', selectedDate, 'Bookings found:', bookings.length)
  return bookings
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

const isSameDay = (date1: Date, date2: Date): boolean => {
  return date1.getFullYear() === date2.getFullYear() &&
         date1.getMonth() === date2.getMonth() &&
         date1.getDate() === date2.getDate()
}

const isSelectedDay = (day: CalendarDay) => {
  if (!selectedDay.value) return false
  return isSameDay(day.gregorianDate, selectedDay.value.gregorianDate)
}

const selectDay = (day: CalendarDay) => {
  selectedDay.value = day
  console.log('Day selected:', day.dayNumber, 'Month:', day.jalaliMonth, 'Bookings:', day.bookingsCount)
}

const previousMonth = () => {
  if (currentJalaliMonth.value === 1) {
    currentJalaliMonth.value = 12
    currentJalaliYear.value--
  } else {
    currentJalaliMonth.value--
  }
  selectedDay.value = null
}

const nextMonth = () => {
  if (currentJalaliMonth.value === 12) {
    currentJalaliMonth.value = 1
    currentJalaliYear.value++
  } else {
    currentJalaliMonth.value++
  }
  selectedDay.value = null
}

const convertToPersian = (num: string) => {
  return convertEnglishToPersianNumbers(num)
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
    const todayDay = calendarDays.value.find(day => day.isToday && !day.isOtherMonth)
    if (todayDay) {
      selectedDay.value = todayDay
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

.no-bookings {
  padding: 24px;
  text-align: center;
  color: #6b7280;
  font-size: 14px;
  background: #f9fafb;
  border-radius: 8px;
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
