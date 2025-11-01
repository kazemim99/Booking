<template>
  <div
    :class="[
      'calendar-day-cell',
      `status-${day.dayStatus.toLowerCase()}`,
      {
        today: day.isToday,
        past: day.isPast,
        compact: compact,
        'iranian-holiday': day.iranianHoliday && !day.holiday,
        'friday-persian': isFridayInPersian
      }
    ]"
    @click="handleClick"
    role="button"
    tabindex="0"
    @keydown.enter="handleClick"
    @keydown.space.prevent="handleClick"
  >
    <!-- Day Header -->
    <div class="day-header">
      <span class="day-number">
        {{ dayNumber }}
      </span>
      <span v-if="!compact" class="day-name">
        {{ dayName }}
      </span>
    </div>

    <!-- Day Content -->
    <div class="day-content">
      <!-- Holiday Indicator -->
      <div v-if="day.holiday" class="day-indicator holiday-indicator">
        <span class="indicator-icon">🎉</span>
        <span v-if="!compact" class="indicator-text">{{ day.holiday.reason }}</span>
      </div>

      <!-- Iranian Public Holiday Indicator (show only if no custom holiday is set) -->
      <div v-else-if="day.iranianHoliday && !day.holiday" class="day-indicator iranian-holiday-indicator">
        <span class="indicator-icon">🇮🇷</span>
        <span v-if="!compact" class="indicator-text">
          {{ locale === 'fa' ? day.iranianHoliday.nameFa : day.iranianHoliday.nameEn }}
        </span>
      </div>

      <!-- Exception Hours -->
      <div v-else-if="day.exception && !day.exception.isClosed" class="exception-display">
        <div class="exception-badge">
          <span class="badge-icon">⚠️</span>
          <span class="badge-text">Modified Hours</span>
        </div>
        <div class="hours-text">
          {{ formatTime(day.exception.openTime!) }} - {{ formatTime(day.exception.closeTime!) }}
        </div>
      </div>

      <!-- Exception Closed -->
      <div v-else-if="day.exception && day.exception.isClosed" class="day-indicator exception-indicator">
        <span class="indicator-icon">🚫</span>
        <span v-if="!compact" class="indicator-text">Closed: {{ day.exception.reason }}</span>
      </div>

      <!-- Regular Hours -->
      <div v-else-if="day.baseHours" class="hours-display">
        <div class="hours-text">
          {{ formatTime(day.baseHours.openTime) }} - {{ formatTime(day.baseHours.closeTime) }}
        </div>
        <div v-if="day.breaks && day.breaks.length > 0 && !compact" class="breaks-indicator">
          <span class="indicator-icon">☕</span>
          <span class="breaks-count">{{ formatNumber(day.breaks.length) }} break{{ day.breaks.length > 1 ? 's' : '' }}</span>
        </div>
      </div>

      <!-- Closed -->
      <div v-else class="closed-display">
        <span class="closed-text">Closed</span>
      </div>

      <!-- Booking Count (if provided) -->
      <div v-if="day.bookingCount && day.bookingCount > 0 && !compact" class="booking-count">
        {{ formatNumber(day.bookingCount) }} booking{{ day.bookingCount > 1 ? 's' : '' }}
      </div>
    </div>

    <!-- Today Badge -->
    <div v-if="day.isToday" class="today-badge">Today</div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { CalendarDay } from '@/modules/provider/types/hours.types'
import { useI18n } from 'vue-i18n'
import { formatDayNumber, getShortDayName, formatTimeDisplay, toPersianNumber } from '@/modules/provider/utils/dateHelpers'

// Props
interface Props {
  day: CalendarDay
  compact?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  compact: false
})

// Emits
const emit = defineEmits<{
  click: []
}>()

// Composables
const { locale } = useI18n()

// Computed
const dayNumber = computed(() => {
  const date = new Date(props.day.date)
  return formatDayNumber(date, locale.value)
})

const dayName = computed(() => {
  const date = new Date(props.day.date)
  return getShortDayName(date, locale.value)
})

const isFridayInPersian = computed(() => {
  // Friday is day 5 in JavaScript (0=Sunday, 5=Friday)
  // Only apply red border for Friday when locale is Persian
  return locale.value === 'fa' && props.day.dayOfWeek === 5
})

// Methods
function formatTime(time: string): string {
  return formatTimeDisplay(time, locale.value)
}

function formatNumber(num: number): string {
  if (locale.value === 'fa') {
    return toPersianNumber(num)
  }
  return String(num)
}

function handleClick() {
  emit('click')
}
</script>

<style scoped>
.calendar-day-cell {
  position: relative;
  display: flex;
  flex-direction: column;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.375rem;
  cursor: pointer;
  transition: all 0.2s;
  padding: 0.75rem;
  min-height: 100px;
}

.calendar-day-cell:hover {
  border-color: #3b82f6;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  transform: translateY(-1px);
}

.calendar-day-cell:focus {
  outline: 2px solid #3b82f6;
  outline-offset: 2px;
}

/* Day Status Colors */
.calendar-day-cell.status-open {
  border-left: 3px solid #10b981;
}

.calendar-day-cell.status-closed {
  background-color: #f9fafb;
  border-left: 3px solid #9ca3af;
}

.calendar-day-cell.status-holiday {
  background-color: #fef2f2;
  border-left: 3px solid #dc2626; /* Red for custom holidays */
}

.calendar-day-cell.status-exception {
  background-color: #fffbeb;
  border-left: 3px solid #f59e0b;
}

/* Iranian Public Holiday - Green left border */
.calendar-day-cell.iranian-holiday {
  background-color: #f0fdf4;
  border-left: 3px solid #16a34a; /* Green for Iranian public holidays */
}

/* Friday in Persian locale - Red left border (weekend in Iran) */
.calendar-day-cell.friday-persian {
  border-left: 3px solid #dc2626; /* Red for Friday (Iranian weekend) */
}

/* Friday with open status in Persian locale - keep red border but with open background */
.calendar-day-cell.friday-persian.status-open {
  border-left: 3px solid #dc2626; /* Red overrides green for Friday */
}

.calendar-day-cell.today {
  box-shadow: 0 0 0 2px #3b82f6;
}

.calendar-day-cell.past {
  opacity: 0.6;
}

.calendar-day-cell.compact {
  padding: 0.5rem;
  min-height: 60px;
}

/* Day Header */
.day-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
}

.day-number {
  font-size: 1.125rem;
  font-weight: 700;
  color: #111827;
}

.day-name {
  font-size: 0.75rem;
  font-weight: 600;
  color: #6b7280;
  text-transform: uppercase;
}

.compact .day-header {
  margin-bottom: 0.25rem;
}

.compact .day-number {
  font-size: 0.875rem;
}

/* Day Content */
.day-content {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  flex: 1;
}

.day-indicator {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem;
  border-radius: 0.25rem;
  font-size: 0.875rem;
}

.holiday-indicator {
  background-color: #fee2e2;
  color: #991b1b;
}

.iranian-holiday-indicator {
  background-color: #dcfce7;
  color: #166534;
  border: 1px solid #bbf7d0;
}

.exception-indicator {
  background-color: #fef3c7;
  color: #92400e;
}

.exception-display {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.exception-badge {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.25rem 0.5rem;
  background-color: #fef3c7;
  border-radius: 0.25rem;
  width: fit-content;
}

.badge-icon {
  font-size: 0.875rem;
}

.badge-text {
  font-size: 0.75rem;
  font-weight: 600;
  color: #92400e;
  text-transform: uppercase;
  letter-spacing: 0.025em;
}

.indicator-icon {
  font-size: 1rem;
}

.indicator-text {
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.hours-display {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.hours-text {
  font-size: 0.875rem;
  font-weight: 600;
  color: #111827;
}

.breaks-indicator {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  font-size: 0.75rem;
  color: #6b7280;
}

.breaks-count {
  font-weight: 500;
}

.closed-display {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem;
}

.closed-text {
  font-size: 0.875rem;
  font-weight: 500;
  color: #9ca3af;
  font-style: italic;
}

.booking-count {
  margin-top: auto;
  padding-top: 0.5rem;
  border-top: 1px solid #e5e7eb;
  font-size: 0.75rem;
  color: #6b7280;
  text-align: center;
}

.today-badge {
  position: absolute;
  top: 0.5rem;
  right: 0.5rem;
  padding: 0.125rem 0.5rem;
  background: #3b82f6;
  color: white;
  font-size: 0.625rem;
  font-weight: 700;
  text-transform: uppercase;
  border-radius: 0.25rem;
  letter-spacing: 0.05em;
}

.compact .today-badge {
  font-size: 0.5rem;
  padding: 0.125rem 0.25rem;
}

/* RTL Support */
:dir(rtl) .calendar-day-cell {
  direction: rtl;
}

:dir(rtl) .today-badge {
  right: auto;
  left: 0.5rem;
}

/* Mobile Optimization */
@media (max-width: 768px) {
  .calendar-day-cell {
    padding: 0.5rem;
    min-height: 80px;
  }

  .day-number {
    font-size: 1rem;
  }

  .hours-text {
    font-size: 0.75rem;
  }

  .indicator-text {
    display: none;
  }

  .badge-text {
    font-size: 0.625rem;
  }

  .exception-badge {
    padding: 0.125rem 0.375rem;
  }
}
</style>
