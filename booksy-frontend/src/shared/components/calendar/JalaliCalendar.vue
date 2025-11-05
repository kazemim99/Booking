<template>
  <div class="jalali-calendar" dir="rtl">
    <!-- Calendar Header -->
    <div class="calendar-header">
      <button type="button" class="nav-btn" @click="previousMonth" title="ماه قبل">
        <svg class="nav-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
      </button>

      <div class="month-year">
        <span class="month-name">{{ currentMonthName }}</span>
        <span class="year-name">{{ currentYear }}</span>
      </div>

      <button type="button" class="nav-btn" @click="nextMonth" title="ماه بعد">
        <svg class="nav-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
      </button>
    </div>

    <!-- Weekday Headers -->
    <div class="weekdays">
      <div v-for="day in weekDayNames" :key="day" class="weekday-name">
        {{ day }}
      </div>
    </div>

    <!-- Calendar Days -->
    <div class="calendar-days">
      <div
        v-for="(day, index) in calendarDays"
        :key="index"
        :class="[
          'calendar-day',
          {
            'other-month': !day.isCurrentMonth,
            'today': day.isToday,
            'weekend': day.isWeekend,
            'holiday': day.isHoliday,
            'exception': day.hasException,
            'special-hours': day.hasSpecialHours,
            'selected': day.isSelected
          }
        ]"
        @click="handleDayClick(day)"
      >
        <span class="day-number">{{ convertToPersianNumber(day.day) }}</span>
        <div v-if="day.isHoliday || day.hasException || day.hasSpecialHours" class="day-indicator">
          <span v-if="day.isHoliday" class="indicator-dot holiday-dot" title="تعطیل"></span>
          <span v-if="day.hasException" class="indicator-dot exception-dot" title="استثنا"></span>
          <span v-if="day.hasSpecialHours" class="indicator-dot special-dot" title="ساعات ویژه"></span>
        </div>
      </div>
    </div>

    <!-- Legend -->
    <div class="calendar-legend">
      <div class="legend-item">
        <span class="legend-dot today-legend"></span>
        <span class="legend-label">امروز</span>
      </div>
      <div class="legend-item">
        <span class="legend-dot holiday-legend"></span>
        <span class="legend-label">تعطیل</span>
      </div>
      <div class="legend-item">
        <span class="legend-dot exception-legend"></span>
        <span class="legend-label">استثنا</span>
      </div>
      <div class="legend-item">
        <span class="legend-dot special-legend"></span>
        <span class="legend-label">ساعات ویژه</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import {
  getCurrentJalaliDate,
  getJalaliMonthName,
  convertEnglishToPersianNumbers,
  jalaliToGregorian,
  gregorianToJalali,
  isValidJalaliDate
} from '@/shared/utils/date/jalali.utils'

export interface CalendarDay {
  day: number
  month: number
  year: number
  gregorianDate: Date
  isCurrentMonth: boolean
  isToday: boolean
  isWeekend: boolean
  isHoliday: boolean
  hasException: boolean
  hasSpecialHours: boolean
  isSelected: boolean
}

interface Props {
  holidays?: string[] // Array of dates in 'YYYY-MM-DD' format
  exceptions?: string[] // Array of dates with exceptions
  specialHours?: string[] // Array of dates with special hours
  selectedDate?: string | null
}

interface Emits {
  (e: 'dayClick', day: CalendarDay): void
  (e: 'monthChange', year: number, month: number): void
}

const props = withDefaults(defineProps<Props>(), {
  holidays: () => [],
  exceptions: () => [],
  specialHours: () => [],
  selectedDate: null
})

const emit = defineEmits<Emits>()

// Persian weekday names (Saturday to Friday)
const weekDayNames = ['ش', 'ی', 'د', 'س', 'چ', 'پ', 'ج']

// Current calendar state
const currentYear = ref(0)
const currentMonth = ref(0)
const today = getCurrentJalaliDate()

// Initialize calendar to current month
onMounted(() => {
  currentYear.value = today.year
  currentMonth.value = today.month
})

// Computed properties
const currentMonthName = computed(() => getJalaliMonthName(currentMonth.value))

const convertToPersianNumber = (num: number) => {
  return convertEnglishToPersianNumbers(num.toString())
}

// Get number of days in a Jalali month
const getDaysInMonth = (year: number, month: number): number => {
  if (month <= 6) return 31
  if (month <= 11) return 30
  // Month 12 (Esfand) - check for leap year
  const isLeap = ((year - 474) % 128) % 33 < 29
  return isLeap ? 30 : 29
}

// Get the day of week for the first day of the month (0 = Saturday)
const getFirstDayOfMonth = (year: number, month: number): number => {
  const gregorian = jalaliToGregorian(year, month, 1)
  const day = gregorian.getDay()
  // Convert JavaScript day (0 = Sunday) to Persian day (0 = Saturday)
  return day === 6 ? 0 : day + 1
}

// Generate calendar days
const calendarDays = computed((): CalendarDay[] => {
  const days: CalendarDay[] = []
  const daysInMonth = getDaysInMonth(currentYear.value, currentMonth.value)
  const firstDayOfWeek = getFirstDayOfMonth(currentYear.value, currentMonth.value)

  // Previous month days
  const prevMonth = currentMonth.value === 1 ? 12 : currentMonth.value - 1
  const prevYear = currentMonth.value === 1 ? currentYear.value - 1 : currentYear.value
  const daysInPrevMonth = getDaysInMonth(prevYear, prevMonth)

  for (let i = firstDayOfWeek - 1; i >= 0; i--) {
    const day = daysInPrevMonth - i
    const gregorianDate = jalaliToGregorian(prevYear, prevMonth, day)
    const dateString = formatDateString(prevYear, prevMonth, day)

    days.push({
      day,
      month: prevMonth,
      year: prevYear,
      gregorianDate,
      isCurrentMonth: false,
      isToday: false,
      isWeekend: false,
      isHoliday: props.holidays.includes(dateString),
      hasException: props.exceptions.includes(dateString),
      hasSpecialHours: props.specialHours.includes(dateString),
      isSelected: props.selectedDate === dateString
    })
  }

  // Current month days
  for (let day = 1; day <= daysInMonth; day++) {
    const gregorianDate = jalaliToGregorian(currentYear.value, currentMonth.value, day)
    const dateString = formatDateString(currentYear.value, currentMonth.value, day)
    const dayOfWeek = gregorianDate.getDay()
    const persianDayOfWeek = dayOfWeek === 6 ? 0 : dayOfWeek + 1

    days.push({
      day,
      month: currentMonth.value,
      year: currentYear.value,
      gregorianDate,
      isCurrentMonth: true,
      isToday: day === today.day && currentMonth.value === today.month && currentYear.value === today.year,
      isWeekend: persianDayOfWeek === 6, // Friday
      isHoliday: props.holidays.includes(dateString),
      hasException: props.exceptions.includes(dateString),
      hasSpecialHours: props.specialHours.includes(dateString),
      isSelected: props.selectedDate === dateString
    })
  }

  // Next month days to fill the grid
  const remainingDays = 42 - days.length // 6 rows × 7 days
  const nextMonth = currentMonth.value === 12 ? 1 : currentMonth.value + 1
  const nextYear = currentMonth.value === 12 ? currentYear.value + 1 : currentYear.value

  for (let day = 1; day <= remainingDays; day++) {
    const gregorianDate = jalaliToGregorian(nextYear, nextMonth, day)
    const dateString = formatDateString(nextYear, nextMonth, day)

    days.push({
      day,
      month: nextMonth,
      year: nextYear,
      gregorianDate,
      isCurrentMonth: false,
      isToday: false,
      isWeekend: false,
      isHoliday: props.holidays.includes(dateString),
      hasException: props.exceptions.includes(dateString),
      hasSpecialHours: props.specialHours.includes(dateString),
      isSelected: props.selectedDate === dateString
    })
  }

  return days
})

// Format date as string for comparison
const formatDateString = (year: number, month: number, day: number): string => {
  return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
}

// Navigation methods
const previousMonth = () => {
  if (currentMonth.value === 1) {
    currentMonth.value = 12
    currentYear.value--
  } else {
    currentMonth.value--
  }
  emit('monthChange', currentYear.value, currentMonth.value)
}

const nextMonth = () => {
  if (currentMonth.value === 12) {
    currentMonth.value = 1
    currentYear.value++
  } else {
    currentMonth.value++
  }
  emit('monthChange', currentYear.value, currentMonth.value)
}

// Handle day click
const handleDayClick = (day: CalendarDay) => {
  if (!day.isCurrentMonth) return
  emit('dayClick', day)
}
</script>

<style scoped>
.jalali-calendar {
  background: white;
  border-radius: 1rem;
  padding: 1.5rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

/* Header */
.calendar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1.5rem;
}

.nav-btn {
  padding: 0.5rem;
  background: transparent;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;

  &:hover {
    background: #f3f4f6;
    border-color: #8b5cf6;
  }
}

.nav-icon {
  width: 1.25rem;
  height: 1.25rem;
  color: #6b7280;
}

.month-year {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.25rem;
}

.month-name {
  font-size: 1.125rem;
  font-weight: 700;
  color: #111827;
}

.year-name {
  font-size: 0.875rem;
  color: #6b7280;
}

/* Weekdays */
.weekdays {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.weekday-name {
  text-align: center;
  font-size: 0.875rem;
  font-weight: 600;
  color: #6b7280;
  padding: 0.5rem;
}

/* Calendar Days */
.calendar-days {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 0.5rem;
}

.calendar-day {
  position: relative;
  aspect-ratio: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.2s ease;
  border: 2px solid transparent;
  background: white;

  &:hover:not(.other-month) {
    background: #f3f4f6;
    border-color: #8b5cf6;
  }

  &.other-month {
    opacity: 0.3;
    cursor: not-allowed;
  }

  &.today {
    background: #eff6ff;
    border-color: #3b82f6;
  }

  &.weekend {
    background: #fef2f2;
  }

  &.holiday {
    background: #fee2e2;
    border-color: #ef4444;
  }

  &.exception {
    border-color: #f59e0b;
  }

  &.special-hours {
    border-color: #8b5cf6;
  }

  &.selected {
    background: #8b5cf6;
    color: white;

    .day-number {
      color: white;
    }
  }
}

.day-number {
  font-size: 0.875rem;
  font-weight: 500;
  color: #111827;
}

.day-indicator {
  display: flex;
  gap: 0.125rem;
  position: absolute;
  bottom: 0.25rem;
}

.indicator-dot {
  width: 0.375rem;
  height: 0.375rem;
  border-radius: 50%;
}

.holiday-dot {
  background: #ef4444;
}

.exception-dot {
  background: #f59e0b;
}

.special-dot {
  background: #8b5cf6;
}

/* Legend */
.calendar-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  margin-top: 1.5rem;
  padding-top: 1rem;
  border-top: 1px solid #e5e7eb;
  justify-content: center;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.75rem;
  color: #6b7280;
}

.legend-dot {
  width: 0.75rem;
  height: 0.75rem;
  border-radius: 50%;
}

.today-legend {
  background: #3b82f6;
}

.holiday-legend {
  background: #ef4444;
}

.exception-legend {
  background: #f59e0b;
}

.special-legend {
  background: #8b5cf6;
}

.legend-label {
  white-space: nowrap;
}

@media (max-width: 640px) {
  .jalali-calendar {
    padding: 1rem;
  }

  .calendar-days {
    gap: 0.25rem;
  }

  .day-number {
    font-size: 0.75rem;
  }

  .month-name {
    font-size: 1rem;
  }
}
</style>
