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
      <h2 class="calendar-title">{{ jalaliMonthNames[currentJalaliMonth - 1] }} {{ convertToPersian(currentJalaliYear) }}</h2>
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
            'has-bookings': day.bookingsCount > 0
          }
        ]"
        @click="openDayModal(day)"
      >
        <div class="day-header">
          <div class="day-number">{{ convertToPersian(day.dayNumber) }}</div>
          <div v-if="day.bookingsCount > 0" class="bookings-count">
            {{ convertToPersian(day.bookingsCount) }}
          </div>
        </div>

        <!-- Mini booking previews (max 3) -->
        <div v-if="day.bookingsCount > 0" class="mini-bookings">
          <div
            v-for="booking in getDayBookings(day).slice(0, 3)"
            :key="booking.id"
            :class="['mini-booking', `mini-${booking.status}`]"
            @click.stop="$emit('booking-click', booking)"
          >
            <span class="mini-time">{{ booking.time }}</span>
            <span class="mini-customer">{{ truncateText(booking.customerName, 10) }}</span>
          </div>
          <div v-if="day.bookingsCount > 3" class="mini-more">
            +{{ convertToPersian(day.bookingsCount - 3) }} دیگر
          </div>
        </div>
      </div>
    </div>

    <!-- Day Details Modal -->
    <Modal v-model="showDayModal" :title="dayModalTitle" size="medium">
      <div v-if="selectedDay" class="day-modal-content">
        <div v-if="selectedDayBookings.length === 0" class="no-bookings-modal">
          <svg class="no-bookings-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
          <p>هیچ رزروی برای این روز وجود ندارد</p>
        </div>
        <div v-else class="modal-bookings-list">
          <div
            v-for="booking in selectedDayBookings"
            :key="booking.id"
            class="modal-booking-item"
            @click="$emit('booking-click', booking)"
          >
            <div class="modal-booking-time">
              <svg class="time-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              {{ booking.time }}
            </div>
            <div class="modal-booking-info">
              <div class="modal-booking-customer">
                <svg class="customer-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                </svg>
                {{ booking.customerName }}
              </div>
              <div class="modal-booking-service">{{ booking.service }}</div>
            </div>
            <div :class="['modal-booking-status', `status-${booking.status}`]">
              {{ getStatusLabel(booking.status) }}
            </div>
          </div>
        </div>
      </div>
    </Modal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { convertEnglishToPersianNumbers, gregorianToJalali } from '@/shared/utils/date/jalali.utils'
import jalaali from 'jalaali-js'
import Modal from '@/shared/components/Modal.vue'

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
const showDayModal = ref(false)

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

  return bookings
})

const dayModalTitle = computed(() => {
  if (!selectedDay.value) return ''
  return `رزروهای ${convertToPersian(selectedDay.value.dayNumber)} ${jalaliMonthNames[currentJalaliMonth.value - 1]}`
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

const getDayBookings = (day: CalendarDay) => {
  const dateStr = formatDateToString(day.gregorianDate)
  return props.bookings.filter(booking => booking.date === dateStr)
}

const truncateText = (text: string, maxLength: number): string => {
  if (text.length <= maxLength) return text
  return text.substring(0, maxLength) + '...'
}

const openDayModal = (day: CalendarDay) => {
  selectedDay.value = day
  showDayModal.value = true
}

const isSelectedDay = (day: CalendarDay) => {
  if (!selectedDay.value) return false
  return isSameDay(day.gregorianDate, selectedDay.value.gregorianDate)
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

const convertToPersian = (num: number | string) => {
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
  min-height: 120px;
  padding: 6px;
  border: 2px solid #e5e7eb;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  background: white;
  display: flex;
  flex-direction: column;
  gap: 4px;

  &:hover {
    border-color: #1976d2;
    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  }

  &.other-month {
    opacity: 0.3;
  }

  &.today {
    background: #e3f2fd;
    border-color: #1976d2;
  }

  &.has-bookings {
    border-color: #4caf50;
  }
}

.day-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 4px;
  flex-shrink: 0;
}

.day-number {
  font-size: 16px;
  font-weight: 600;
  color: #1f2937;
}

.bookings-count {
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

.mini-bookings {
  display: flex;
  flex-direction: column;
  gap: 2px;
  flex: 1;
  overflow: hidden;
}

.mini-booking {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px 6px;
  border-radius: 4px;
  font-size: 11px;
  cursor: pointer;
  transition: all 0.2s;
  border-left: 3px solid transparent;

  &:hover {
    transform: translateX(-2px);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  }

  &.mini-pending {
    background: #fff3cd;
    border-left-color: #ff9800;
  }

  &.mini-confirmed {
    background: #d1ecf1;
    border-left-color: #0c5460;
  }

  &.mini-completed {
    background: #d4edda;
    border-left-color: #4caf50;
  }

  &.mini-cancelled {
    background: #f8d7da;
    border-left-color: #ef4444;
  }
}

.mini-time {
  font-weight: 600;
  color: #1f2937;
  flex-shrink: 0;
}

.mini-customer {
  color: #6b7280;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.mini-more {
  padding: 4px 6px;
  text-align: center;
  font-size: 10px;
  font-weight: 600;
  color: #6b7280;
  background: #f3f4f6;
  border-radius: 4px;
  margin-top: 2px;
}

// Modal Styles
.day-modal-content {
  min-height: 200px;
}

.no-bookings-modal {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  color: #6b7280;
}

.no-bookings-icon {
  width: 64px;
  height: 64px;
  color: #d1d5db;
  margin-bottom: 16px;
}

.modal-bookings-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.modal-booking-item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px;
  background: #f9fafb;
  border-radius: 8px;
  border: 1px solid #e5e7eb;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: white;
    border-color: #1976d2;
    box-shadow: 0 2px 8px rgba(25, 118, 210, 0.15);
    transform: translateX(-4px);
  }
}

.modal-booking-time {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
  color: #1976d2;
  min-width: 80px;

  .time-icon {
    width: 18px;
    height: 18px;
  }
}

.modal-booking-info {
  flex: 1;
  min-width: 0;
}

.modal-booking-customer {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 16px;
  font-weight: 600;
  color: #1f2937;
  margin-bottom: 6px;

  .customer-icon {
    width: 16px;
    height: 16px;
    color: #6b7280;
  }
}

.modal-booking-service {
  font-size: 14px;
  color: #6b7280;
}

.modal-booking-status {
  padding: 6px 14px;
  border-radius: 16px;
  font-size: 13px;
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
