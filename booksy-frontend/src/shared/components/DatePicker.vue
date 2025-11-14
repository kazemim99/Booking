<template>
  <div class="date-picker">
    <div class="date-picker-input" @click="togglePicker">
      <svg class="calendar-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
      </svg>
      <input
        type="text"
        :value="displayValue"
        :placeholder="placeholder"
        readonly
        class="date-input"
      />
    </div>

    <transition name="picker">
      <div v-if="isOpen" class="date-picker-dropdown" @click.stop>
        <div class="picker-header">
          <button class="nav-btn" @click="previousMonth">
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
          </button>
          <div class="current-month">{{ currentMonthName }} {{ currentYear }}</div>
          <button class="nav-btn" @click="nextMonth">
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </button>
        </div>

        <div class="picker-weekdays">
          <div v-for="day in weekDays" :key="day" class="weekday">{{ day }}</div>
        </div>

        <div class="picker-days">
          <div
            v-for="(day, index) in calendarDays"
            :key="index"
            :class="[
              'day',
              {
                'other-month': !day.isCurrentMonth,
                'selected': day.isSelected,
                'today': day.isToday,
                'disabled': day.isDisabled
              }
            ]"
            @click="selectDate(day)"
          >
            {{ day.day }}
          </div>
        </div>

        <div v-if="showTime" class="time-picker">
          <div class="time-inputs">
            <div class="time-input-group">
              <label>ساعت</label>
              <input
                type="number"
                v-model.number="selectedHour"
                min="0"
                max="23"
                class="time-input"
              />
            </div>
            <span class="time-separator">:</span>
            <div class="time-input-group">
              <label>دقیقه</label>
              <input
                type="number"
                v-model.number="selectedMinute"
                min="0"
                max="59"
                class="time-input"
              />
            </div>
          </div>
        </div>

        <div class="picker-footer">
          <button class="btn-clear" @click="clear">پاک کردن</button>
          <button class="btn-confirm" @click="confirm">تایید</button>
        </div>
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'

interface Props {
  modelValue?: string
  placeholder?: string
  showTime?: boolean
  minDate?: Date
  maxDate?: Date
}

const props = withDefaults(defineProps<Props>(), {
  placeholder: 'تاریخ را انتخاب کنید',
  showTime: false,
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
}>()

const isOpen = ref(false)
const currentMonth = ref(new Date().getMonth() + 1)
const currentYear = ref(new Date().getFullYear())
const selectedDate = ref<Date | null>(null)
const selectedHour = ref(9)
const selectedMinute = ref(0)

const weekDays = ['ش', 'ی', 'د', 'س', 'چ', 'پ', 'ج']
const monthNames = [
  'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
  'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
]

const currentMonthName = computed(() => monthNames[currentMonth.value - 1])

const displayValue = computed(() => {
  if (!selectedDate.value) return ''

  const date = selectedDate.value
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')

  if (props.showTime) {
    const hour = String(selectedHour.value).padStart(2, '0')
    const minute = String(selectedMinute.value).padStart(2, '0')
    return `${year}/${month}/${day} - ${hour}:${minute}`
  }

  return `${year}/${month}/${day}`
})

const calendarDays = computed(() => {
  const days: any[] = []
  const year = currentYear.value
  const month = currentMonth.value

  // Get first day of month
  const firstDay = new Date(year, month - 1, 1)
  const lastDay = new Date(year, month, 0)

  // Get day of week for first day (0 = Sunday, 6 = Saturday)
  // Adjust for Persian week (Saturday = 0)
  let startDay = firstDay.getDay()
  startDay = startDay === 6 ? 0 : startDay + 1

  // Previous month days
  const prevMonthDays = new Date(year, month - 1, 0).getDate()
  for (let i = startDay - 1; i >= 0; i--) {
    days.push({
      day: prevMonthDays - i,
      isCurrentMonth: false,
      isSelected: false,
      isToday: false,
      isDisabled: true,
      date: new Date(year, month - 2, prevMonthDays - i)
    })
  }

  // Current month days
  const today = new Date()
  today.setHours(0, 0, 0, 0)

  for (let day = 1; day <= lastDay.getDate(); day++) {
    const date = new Date(year, month - 1, day)
    date.setHours(0, 0, 0, 0)

    const isToday = date.getTime() === today.getTime()
    const isSelected = selectedDate.value &&
      date.getTime() === new Date(selectedDate.value.getFullYear(), selectedDate.value.getMonth(), selectedDate.value.getDate()).getTime()

    let isDisabled = false
    if (props.minDate) {
      const minDate = new Date(props.minDate)
      minDate.setHours(0, 0, 0, 0)
      isDisabled = date < minDate
    }
    if (props.maxDate && !isDisabled) {
      const maxDate = new Date(props.maxDate)
      maxDate.setHours(0, 0, 0, 0)
      isDisabled = date > maxDate
    }

    days.push({
      day,
      isCurrentMonth: true,
      isSelected,
      isToday,
      isDisabled,
      date
    })
  }

  // Next month days
  const remainingDays = 42 - days.length
  for (let day = 1; day <= remainingDays; day++) {
    days.push({
      day,
      isCurrentMonth: false,
      isSelected: false,
      isToday: false,
      isDisabled: true,
      date: new Date(year, month, day)
    })
  }

  return days
})

const togglePicker = () => {
  isOpen.value = !isOpen.value
}

const previousMonth = () => {
  if (currentMonth.value === 1) {
    currentMonth.value = 12
    currentYear.value--
  } else {
    currentMonth.value--
  }
}

const nextMonth = () => {
  if (currentMonth.value === 12) {
    currentMonth.value = 1
    currentYear.value++
  } else {
    currentMonth.value++
  }
}

const selectDate = (day: any) => {
  if (day.isDisabled || !day.isCurrentMonth) return

  selectedDate.value = day.date

  if (!props.showTime) {
    confirm()
  }
}

const clear = () => {
  selectedDate.value = null
  emit('update:modelValue', '')
  isOpen.value = false
}

const confirm = () => {
  if (selectedDate.value) {
    const date = selectedDate.value
    const year = date.getFullYear()
    const month = String(date.getMonth() + 1).padStart(2, '0')
    const day = String(date.getDate()).padStart(2, '0')

    let value = `${year}-${month}-${day}`

    if (props.showTime) {
      const hour = String(selectedHour.value).padStart(2, '0')
      const minute = String(selectedMinute.value).padStart(2, '0')
      value += `T${hour}:${minute}:00`
    }

    emit('update:modelValue', value)
  }

  isOpen.value = false
}

const handleClickOutside = (event: MouseEvent) => {
  const target = event.target as HTMLElement
  if (!target.closest('.date-picker')) {
    isOpen.value = false
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)

  // Initialize from modelValue
  if (props.modelValue) {
    const date = new Date(props.modelValue)
    selectedDate.value = date
    currentMonth.value = date.getMonth() + 1
    currentYear.value = date.getFullYear()

    if (props.showTime) {
      selectedHour.value = date.getHours()
      selectedMinute.value = date.getMinutes()
    }
  }
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>

<style scoped>
.date-picker {
  position: relative;
  width: 100%;
}

.date-picker-input {
  position: relative;
  display: flex;
  align-items: center;
  cursor: pointer;
}

.calendar-icon {
  position: absolute;
  right: 12px;
  width: 20px;
  height: 20px;
  color: rgba(0, 0, 0, 0.54);
  pointer-events: none;
}

.date-input {
  width: 100%;
  padding: 10px 16px 10px 44px;
  border: 1px solid rgba(0, 0, 0, 0.23);
  border-radius: 4px;
  font-size: 14px;
  background: white;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  color: rgba(0, 0, 0, 0.87);
  cursor: pointer;
}

.date-input:hover {
  border-color: rgba(0, 0, 0, 0.87);
}

.date-input:focus {
  outline: none;
  border-color: #1976d2;
  border-width: 2px;
  padding: 9px 15px 9px 43px;
}

.date-picker-dropdown {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  right: 0;
  background: white;
  border-radius: 4px;
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.15);
  z-index: 1000;
  padding: 16px;
}

.picker-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.nav-btn {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  background: transparent;
  border-radius: 50%;
  cursor: pointer;
  color: rgba(0, 0, 0, 0.6);
  transition: all 0.2s;
}

.nav-btn:hover {
  background: rgba(0, 0, 0, 0.04);
  color: rgba(0, 0, 0, 0.87);
}

.nav-btn svg {
  width: 20px;
  height: 20px;
}

.current-month {
  font-size: 16px;
  font-weight: 500;
  color: rgba(0, 0, 0, 0.87);
}

.picker-weekdays {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 4px;
  margin-bottom: 8px;
}

.weekday {
  text-align: center;
  font-size: 12px;
  font-weight: 500;
  color: rgba(0, 0, 0, 0.6);
  padding: 8px 0;
}

.picker-days {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 4px;
}

.day {
  aspect-ratio: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  border-radius: 50%;
  cursor: pointer;
  transition: all 0.2s;
  color: rgba(0, 0, 0, 0.87);
}

.day:hover:not(.disabled):not(.other-month) {
  background: rgba(25, 118, 210, 0.08);
}

.day.other-month {
  color: rgba(0, 0, 0, 0.38);
  cursor: default;
}

.day.today {
  border: 1px solid #1976d2;
}

.day.selected {
  background: #1976d2;
  color: white;
}

.day.selected:hover {
  background: #1565c0;
}

.day.disabled {
  color: rgba(0, 0, 0, 0.26);
  cursor: not-allowed;
}

.time-picker {
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid rgba(0, 0, 0, 0.12);
}

.time-inputs {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.time-input-group {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
}

.time-input-group label {
  font-size: 12px;
  color: rgba(0, 0, 0, 0.6);
}

.time-input {
  width: 60px;
  padding: 8px;
  border: 1px solid rgba(0, 0, 0, 0.23);
  border-radius: 4px;
  text-align: center;
  font-size: 16px;
}

.time-input:focus {
  outline: none;
  border-color: #1976d2;
  border-width: 2px;
  padding: 7px;
}

.time-separator {
  font-size: 24px;
  font-weight: 600;
  color: rgba(0, 0, 0, 0.87);
  margin-top: 20px;
}

.picker-footer {
  display: flex;
  justify-content: space-between;
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid rgba(0, 0, 0, 0.12);
}

.btn-clear,
.btn-confirm {
  padding: 8px 16px;
  border-radius: 4px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  border: none;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.btn-clear {
  background: transparent;
  color: rgba(0, 0, 0, 0.6);
}

.btn-clear:hover {
  background: rgba(0, 0, 0, 0.04);
}

.btn-confirm {
  background: #1976d2;
  color: white;
}

.btn-confirm:hover {
  background: #1565c0;
}

/* Animations */
.picker-enter-active,
.picker-leave-active {
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}

.picker-enter-from {
  opacity: 0;
  transform: translateY(-8px);
}

.picker-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}

@media (max-width: 768px) {
  .date-picker-dropdown {
    position: fixed;
    top: 50%;
    left: 50%;
    right: auto;
    transform: translate(-50%, -50%);
    width: 90%;
    max-width: 320px;
  }
}
</style>
