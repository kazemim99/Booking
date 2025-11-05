<template>
  <div class="hours-calendar-view">
    <!-- Calendar Header -->
    <div class="calendar-header">
      <div class="view-toggle">
        <button
          type="button"
          :class="['toggle-btn', { active: viewMode === 'week' }]"
          @click="setViewMode('week')"
        >
          Week
        </button>
        <button
          type="button"
          :class="['toggle-btn', { active: viewMode === 'month' }]"
          @click="setViewMode('month')"
        >
          Month
        </button>
      </div>

      <div class="calendar-navigation">
        <button type="button" class="nav-btn" @click="navigatePrevious" aria-label="Previous">
          ‹
        </button>
        <h3 class="current-period">{{ currentPeriodLabel }}</h3>
        <button type="button" class="nav-btn" @click="navigateNext" aria-label="Next">
          ›
        </button>
        <button type="button" class="today-btn" @click="goToToday">Today</button>
      </div>
    </div>

    <!-- Week View -->
    <div v-if="viewMode === 'week'" class="week-view">
      <div class="week-grid">
        <div
          v-for="day in weekDays"
          :key="day.date"
          class="day-column"
        >
          <CalendarDayCell
            :day="day"
            @click="handleDayClick(day)"
          />
        </div>
      </div>
    </div>

    <!-- Month View -->
    <div v-else class="month-view">
      <div class="month-weekdays">
        <div v-for="weekday in weekdayLabels" :key="weekday" class="weekday-label">
          {{ weekday }}
        </div>
      </div>
      <div class="month-grid">
        <div
          v-for="day in monthDays"
          :key="day.date"
          :class="['month-day-cell', { 'other-month': !day.isCurrentMonth }]"
        >
          <CalendarDayCell
            :day="day"
            :compact="true"
            @click="handleDayClick(day)"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { CalendarDay, CalendarViewMode, DayStatus } from '@/modules/provider/types/hours.types'
import CalendarDayCell from './CalendarDayCell.vue'
import { useI18n } from 'vue-i18n'
import {
  getWeekDays,
  getMonthDays,
  formatMonthYear,
  formatWeekRange,
  addDays,
  addWeeks,
  addMonths,
  startOfWeek,
  startOfMonth,
  isSameDay,
  parseDate,
  formatDate,
  getShortDayName,
  PERSIAN_WEEKDAYS_SHORT
} from '@/modules/provider/utils/dateHelpers'
import { isIranianPublicHoliday } from '@/modules/provider/utils/iranianHolidays'

// Props
interface Props {
  businessHours?: any[] // Business hours data
  holidays?: any[] // Holidays data
  exceptions?: any[] // Exceptions data
}

const props = withDefaults(defineProps<Props>(), {
  businessHours: () => [],
  holidays: () => [],
  exceptions: () => []
})

// Emits
const emit = defineEmits<{
  dayClick: [day: CalendarDay]
}>()

// Composables
const { locale } = useI18n()

// State
const viewMode = ref<'week' | 'month'>('week')
const currentDate = ref<Date>(new Date())

// Computed
const weekdayLabels = computed(() => {
  if (locale.value === 'fa') {
    // Persian calendar starts on Saturday
    return PERSIAN_WEEKDAYS_SHORT
  }
  // Gregorian calendar starts on Sunday
  return ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']
})

const currentPeriodLabel = computed(() => {
  if (viewMode.value === 'week') {
    return formatWeekRange(currentDate.value, locale.value)
  } else {
    return formatMonthYear(currentDate.value, locale.value)
  }
})

const weekDays = computed(() => {
  const weekStart = startOfWeek(currentDate.value)
  return getWeekDays(weekStart).map(date => buildCalendarDay(date))
})

const monthDays = computed(() => {
  const monthStart = startOfMonth(currentDate.value)
  return getMonthDays(monthStart).map(day => buildCalendarDay(day.date, day.isCurrentMonth))
})

// Methods
function buildCalendarDay(date: Date, isCurrentMonth = true): CalendarDay {
  const dateStr = formatDate(date)
  const today = new Date()
  const dayOfWeek = date.getDay() // 0 = Sunday, 6 = Saturday

  // Find business hours for this day (search by dayOfWeek property)
  const businessHour = props.businessHours?.find(h => h.dayOfWeek === dayOfWeek)

  // Check for holiday
  const holiday = props.holidays?.find(h => h.date === dateStr)

  // Check for exception
  const exception = props.exceptions?.find(e => e.date === dateStr)

  // Check for Iranian public holiday (only for Persian locale)
  const iranianHoliday = locale.value === 'fa' ? isIranianPublicHoliday(date) : null

  // Determine day status
  let dayStatus = DayStatus.Closed
  if (holiday) {
    dayStatus = DayStatus.Holiday
  } else if (exception) {
    dayStatus = exception.isClosed ? DayStatus.Closed : DayStatus.Exception
  } else if (businessHour?.isOpen) {
    dayStatus = DayStatus.Open
  }

  return {
    date: dateStr,
    dayOfWeek,
    dayStatus,
    baseHours: businessHour?.isOpen ? {
      openTime: businessHour.openTime,
      closeTime: businessHour.closeTime
    } : undefined,
    breaks: businessHour?.breaks || [],
    holiday,
    exception,
    iranianHoliday, // Add Iranian public holiday info
    isToday: isSameDay(date, today),
    isPast: date < today && !isSameDay(date, today),
    isCurrentMonth
  }
}

function setViewMode(mode: 'week' | 'month') {
  viewMode.value = mode
}

function navigatePrevious() {
  if (viewMode.value === 'week') {
    currentDate.value = addWeeks(currentDate.value, -1)
  } else {
    currentDate.value = addMonths(currentDate.value, -1)
  }
}

function navigateNext() {
  if (viewMode.value === 'week') {
    currentDate.value = addWeeks(currentDate.value, 1)
  } else {
    currentDate.value = addMonths(currentDate.value, 1)
  }
}

function goToToday() {
  currentDate.value = new Date()
}

function handleDayClick(day: CalendarDay) {
  emit('dayClick', day)
}
</script>

<style scoped>
.hours-calendar-view {
  width: 100%;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  overflow: hidden;
}

.calendar-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  border-bottom: 1px solid #e5e7eb;
  background-color: #f9fafb;
  gap: 1rem;
}

.view-toggle {
  display: flex;
  gap: 0.25rem;
  background: #e5e7eb;
  border-radius: 0.375rem;
  padding: 0.25rem;
}

.toggle-btn {
  padding: 0.5rem 1rem;
  border: none;
  background: transparent;
  border-radius: 0.25rem;
  font-size: 0.875rem;
  font-weight: 500;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.2s;
}

.toggle-btn.active {
  background: white;
  color: #111827;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
}

.calendar-navigation {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.current-period {
  font-size: 1rem;
  font-weight: 600;
  color: #111827;
  margin: 0;
  min-width: 200px;
  text-align: center;
}

.nav-btn {
  width: 2rem;
  height: 2rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid #d1d5db;
  background: white;
  border-radius: 0.375rem;
  font-size: 1.5rem;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.2s;
}

.nav-btn:hover {
  border-color: #3b82f6;
  color: #3b82f6;
}

.today-btn {
  padding: 0.5rem 1rem;
  border: 1px solid #d1d5db;
  background: white;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  font-weight: 500;
  color: #111827;
  cursor: pointer;
  transition: all 0.2s;
}

.today-btn:hover {
  border-color: #3b82f6;
  background: #eff6ff;
  color: #3b82f6;
}

/* Week View */
.week-view {
  padding: 1rem;
}

.week-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 1rem;
}

.day-column {
  min-height: 200px;
}

/* Month View */
.month-view {
  padding: 1rem;
}

.month-weekdays {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.weekday-label {
  text-align: center;
  font-size: 0.75rem;
  font-weight: 600;
  color: #6b7280;
  text-transform: uppercase;
  padding: 0.5rem 0;
}

.month-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 0.5rem;
}

.month-day-cell {
  min-height: 80px;
}

.month-day-cell.other-month {
  opacity: 0.4;
}

/* RTL Support for Persian/Arabic */
:dir(rtl) .hours-calendar-view {
  direction: rtl;
}

:dir(rtl) .calendar-header {
  flex-direction: row-reverse;
}

:dir(rtl) .calendar-navigation {
  flex-direction: row-reverse;
}

:dir(rtl) .nav-btn:first-of-type {
  transform: scaleX(-1);
}

:dir(rtl) .nav-btn:nth-of-type(2) {
  transform: scaleX(-1);
}

/* Mobile Responsive */
@media (max-width: 768px) {
  .calendar-header {
    flex-direction: column;
    align-items: stretch;
    gap: 0.75rem;
  }

  .view-toggle {
    justify-content: center;
  }

  .calendar-navigation {
    justify-content: center;
  }

  .current-period {
    min-width: auto;
  }

  .week-grid {
    grid-template-columns: 1fr;
    gap: 0.5rem;
  }

  .day-column {
    min-height: 100px;
  }

  .month-grid {
    gap: 0.25rem;
  }

  .month-day-cell {
    min-height: 60px;
  }
}
</style>
