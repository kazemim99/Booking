<template>
  <div class="business-hours-view" dir="rtl">
    <!-- Header -->
    <div class="page-header">
      <h2 class="page-title">ساعات کاری</h2>
      <p class="page-description">مدیریت ساعات کاری هفتگی و تنظیمات روزهای خاص</p>
    </div>

    <!-- Main Grid Layout -->
    <div class="hours-grid">
      <!-- Weekly Schedule Section (Left - 2/3 width) -->
      <div class="weekly-section">
        <!-- Weekly Schedule Card -->
        <div class="schedule-card">
          <div class="card-header">
            <div class="header-title">
              <svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <circle cx="12" cy="12" r="10"></circle>
                <polyline points="12 6 12 12 16 14"></polyline>
              </svg>
              <h3>برنامه هفتگی</h3>
            </div>
            <div class="header-actions">
              <button type="button" class="action-btn" @click="handleSetStandardTime">
                <svg class="btn-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                  <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                  <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                </svg>
                تنظیم استاندارد
              </button>
              <button type="button" class="action-btn" @click="handleClearAll">
                <svg class="btn-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                  <polyline points="3 6 5 6 21 6"></polyline>
                  <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                </svg>
                پاک کردن همه
              </button>
            </div>
          </div>

          <div class="card-content">
            <!-- Day Schedule Items -->
            <div class="schedule-days">
              <div
                v-for="(day, index) in schedule"
                :key="day.day"
                :class="['day-item', { 'day-disabled': !day.enabled }]"
              >
                <!-- Day Header -->
                <div class="day-row-header">
                  <div class="day-toggle-group">
                    <label class="switch">
                      <input
                        type="checkbox"
                        :checked="day.enabled"
                        @change="handleToggleDay(index)"
                      />
                      <span class="switch-slider"></span>
                    </label>
                    <span class="day-name">{{ day.day }}</span>
                  </div>
                  <span v-if="!day.enabled" class="closed-label">تعطیل</span>
                </div>

                <!-- Time Inputs (only when enabled) -->
                <div v-if="day.enabled" class="time-inputs">
                  <div class="input-group">
                    <label class="input-label">ساعت شروع</label>
                    <PersianTimePicker
                      :model-value="day.startTime"
                      @update:model-value="handleTimeChange(index, 'startTime', $event)"
                      placeholder="10:00"
                    />
                  </div>
                  <div class="input-group">
                    <label class="input-label">ساعت پایان</label>
                    <PersianTimePicker
                      :model-value="day.endTime"
                      @update:model-value="handleTimeChange(index, 'endTime', $event)"
                      placeholder="22:00"
                    />
                  </div>
                  <div class="input-group">
                    <label class="input-label">استراحت (اختیاری)</label>
                    <PersianTimePicker
                      :model-value="day.breakTime"
                      @update:model-value="handleTimeChange(index, 'breakTime', $event)"
                      placeholder="14:00"
                    />
                  </div>
                </div>
              </div>
            </div>

            <!-- Save Button -->
            <button type="button" class="save-btn" @click="handleSave">
              ذخیره تغییرات
            </button>
          </div>
        </div>

        <!-- Info Card -->
        <div class="info-card">
          <div class="info-content">
            <h4 class="info-title">💡 راهنما</h4>
            <ul class="info-list">
              <li>از تقویم برای تنظیم ساعات روزهای خاص استفاده کنید</li>
              <li>می‌توانید برای روزهای تعطیل دلیل تعطیلی ثبت کنید</li>
              <li>زمان استراحت باعث می‌شود در آن بازه رزرو ثبت نشود</li>
              <li>تنظیمات روزهای خاص بر برنامه هفتگی اولویت دارند</li>
            </ul>
          </div>
        </div>
      </div>

      <!-- Calendar Section (Right - 1/3 width) -->
      <div class="calendar-section">
        <!-- Calendar Card -->
        <div class="calendar-card">
          <div class="card-header">
            <div class="header-title">
              <svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
                <line x1="16" y1="2" x2="16" y2="6"></line>
                <line x1="8" y1="2" x2="8" y2="6"></line>
                <line x1="3" y1="10" x2="21" y2="10"></line>
              </svg>
              <h3>تقویم روزهای خاص</h3>
            </div>
          </div>
          <div class="card-content">
            <p class="calendar-hint">برای تنظیم ساعات کاری روز خاص، روی تاریخ کلیک کنید</p>

            <!-- Persian Calendar Component -->
            <PersianCalendar
              v-model="selectedDate"
              :custom-days="customDays"
              @day-click="handleDateSelect"
            />

            <!-- Selected Date Info -->
            <div v-if="selectedDateInfo" class="selected-info">
              <p class="info-label">تنظیمات ذخیره شده:</p>
              <p class="info-value">
                {{ selectedDateInfo.isClosed
                  ? `تعطیل - ${selectedDateInfo.closedReason}`
                  : `${selectedDateInfo.startTime} - ${selectedDateInfo.endTime}`
                }}
              </p>
            </div>
          </div>
        </div>

        <!-- Custom Days List -->
        <div v-if="customDays.size > 0" class="custom-days-card">
          <div class="card-header">
            <h3 class="header-title-simple">روزهای تنظیم شده</h3>
          </div>
          <div class="card-content">
            <div class="custom-days-list">
              <div
                v-for="[dateKey, data] in Array.from(customDays.entries())"
                :key="dateKey"
                class="custom-day-item"
              >
                <div class="custom-day-date">{{ formatPersianDate(dateKey) }}</div>
                <div class="custom-day-info">
                  {{ data.isClosed
                    ? `تعطیل - ${data.closedReason}`
                    : `${data.startTime} - ${data.endTime}`
                  }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Custom Day Modal -->
    <CustomDayModal
      :is-open="modalOpen"
      :selected-date="selectedDate"
      @close="modalOpen = false"
      @save="handleSaveCustomDay"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import PersianCalendar from '@/shared/components/calendar/PersianCalendar.vue'
import PersianTimePicker from '@/shared/components/calendar/PersianTimePicker.vue'
import CustomDayModal from './CustomDayModal.vue'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'

interface DaySchedule {
  day: string
  enabled: boolean
  startTime: string
  endTime: string
  breakTime: string
}

interface CustomDayData {
  date: Date
  startTime: string
  endTime: string
  breakTime: string
  isClosed: boolean
  closedReason: string
}

const initialSchedule: DaySchedule[] = [
  { day: 'شنبه', enabled: true, startTime: '10:00', endTime: '22:00', breakTime: '' },
  { day: 'یکشنبه', enabled: true, startTime: '10:00', endTime: '22:00', breakTime: '' },
  { day: 'دوشنبه', enabled: true, startTime: '10:00', endTime: '22:00', breakTime: '' },
  { day: 'سه‌شنبه', enabled: true, startTime: '10:00', endTime: '22:00', breakTime: '' },
  { day: 'چهارشنبه', enabled: true, startTime: '10:00', endTime: '22:00', breakTime: '' },
  { day: 'پنج‌شنبه', enabled: true, startTime: '10:00', endTime: '22:00', breakTime: '' },
  { day: 'جمعه', enabled: false, startTime: '', endTime: '', breakTime: '' },
]

const schedule = ref<DaySchedule[]>([...initialSchedule])
const selectedDate = ref<Date | null>(null)
const modalOpen = ref(false)
const customDays = ref<Map<string, CustomDayData>>(new Map())

const handleToggleDay = (index: number) => {
  const day = schedule.value[index]
  day.enabled = !day.enabled
  if (!day.enabled) {
    day.startTime = ''
    day.endTime = ''
    day.breakTime = ''
  }
}

const handleTimeChange = (index: number, field: 'startTime' | 'endTime' | 'breakTime', value: string) => {
  schedule.value[index][field] = value
}

const handleSetStandardTime = () => {
  schedule.value = schedule.value.map(day => ({
    ...day,
    enabled: true,
    startTime: '10:00',
    endTime: '22:00',
    breakTime: '',
  }))
}

const handleClearAll = () => {
  schedule.value = schedule.value.map(day => ({
    ...day,
    enabled: false,
    startTime: '',
    endTime: '',
    breakTime: '',
  }))
}

const handleSave = () => {
  // TODO: Save to backend
  console.log('Saving schedule:', schedule.value)
  alert('ساعات کاری با موفقیت ذخیره شد')
}

const handleDateSelect = (date: Date) => {
  // PersianCalendar emits Date object directly
  selectedDate.value = date
  modalOpen.value = true
}

const handleSaveCustomDay = (data: CustomDayData) => {
  const dateKey = data.date.toISOString().split('T')[0]
  customDays.value.set(dateKey, data)
  modalOpen.value = false
}

const selectedDateInfo = computed(() => {
  if (!selectedDate.value) return null
  const dateKey = selectedDate.value.toISOString().split('T')[0]
  return customDays.value.get(dateKey) || null
})

const formatPersianDate = (dateKey: string) => {
  const date = new Date(dateKey)
  return `${convertEnglishToPersianNumbers(date.getDate().toString())} / ${convertEnglishToPersianNumbers((date.getMonth() + 1).toString())} / ${convertEnglishToPersianNumbers(date.getFullYear().toString())}`
}
</script>

<style scoped lang="scss">
.business-hours-view {
  max-width: 90rem;
  margin: 0 auto;
  padding: 2rem;
}

/* Page Header */
.page-header {
  margin-bottom: 2rem;
}

.page-title {
  font-size: 2rem;
  font-weight: 600;
  color: #1a1a1a;
  margin-bottom: 0.5rem;
}

.page-description {
  font-size: 1rem;
  color: var(--color-gray-600);
}

/* Grid Layout */
.hours-grid {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: 1.5rem;
}

@media (max-width: 1024px) {
  .hours-grid {
    grid-template-columns: 1fr;
  }
}

/* Card Base Styles */
.schedule-card,
.info-card,
.calendar-card,
.custom-days-card {
  background: white;
  border: 1px solid var(--color-gray-300);
  border-radius: 0.75rem;
  overflow: hidden;
}

.card-header {
  padding: 1.5rem;
  border-bottom: 1px solid var(--color-gray-300);
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.header-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 1.125rem;
  font-weight: 600;
  color: #1a1a1a;
}

.header-title-simple {
  font-size: 1.125rem;
  font-weight: 600;
  color: #1a1a1a;
}

.icon {
  width: 1.25rem;
  height: 1.25rem;
  stroke-width: 2;
}

.header-actions {
  display: flex;
  gap: 0.5rem;
}

.action-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  background: white;
  border: 1px solid var(--color-gray-300);
  border-radius: 0.5rem;
  font-size: 0.875rem;
  color: var(--color-gray-800);
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: var(--color-gray-50);
    border-color: var(--color-gray-400);
  }
}

.btn-icon {
  width: 1rem;
  height: 1rem;
  stroke-width: 2;
}

.card-content {
  padding: 1.5rem;
}

/* Weekly Section */
.weekly-section {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.schedule-days {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.day-item {
  padding: 1rem;
  border: 1px solid var(--color-gray-300);
  border-radius: 0.5rem;
  background: white;
  transition: all 0.2s;

  &.day-disabled {
    background: rgba(229, 231, 235, 0.3);
  }
}

.day-row-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1rem;
}

.day-toggle-group {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.day-name {
  font-size: 1rem;
  font-weight: 500;
  color: #1a1a1a;
}

.closed-label {
  font-size: 0.875rem;
  color: var(--color-gray-600);
}

/* Switch Toggle */
.switch {
  position: relative;
  display: inline-block;
  width: 3rem;
  height: 1.75rem;
}

.switch input {
  opacity: 0;
  width: 0;
  height: 0;
}

.switch-slider {
  position: absolute;
  cursor: pointer;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: var(--color-gray-300);
  transition: 0.3s;
  border-radius: 1.75rem;
}

.switch-slider:before {
  position: absolute;
  content: '';
  height: 1.25rem;
  width: 1.25rem;
  left: 0.25rem;
  bottom: 0.25rem;
  background-color: white;
  transition: 0.3s;
  border-radius: 50%;
}

.switch input:checked + .switch-slider {
  background-color: var(--color-primary-500);
}

.switch input:checked + .switch-slider:before {
  transform: translateX(1.25rem);
}

/* Time Inputs */
.time-inputs {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0.75rem;
}

@media (max-width: 640px) {
  .time-inputs {
    grid-template-columns: 1fr;
  }
}

.input-group {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.input-label {
  font-size: 0.75rem;
  color: var(--color-gray-600);
  font-weight: 500;
}

.time-input {
  width: 100%;
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--color-gray-300);
  border-radius: 0.5rem;
  font-size: 0.875rem;
  transition: all 0.2s;

  &:focus {
    outline: none;
    border-color: var(--color-primary-500);
    box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
  }

  &::placeholder {
    color: var(--color-gray-500);
  }
}

/* Save Button */
.save-btn {
  width: 100%;
  margin-top: 1.5rem;
  padding: 0.75rem 1.5rem;
  background: var(--color-primary-500);
  color: white;
  border: none;
  border-radius: 0.5rem;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: var(--color-primary-700);
  }

  &:active {
    transform: scale(0.98);
  }
}

/* Info Card */
.info-card {
  background: rgba(139, 92, 246, 0.05);
  border-color: rgba(139, 92, 246, 0.2);
}

.info-content {
  padding: 1.5rem;
}

.info-title {
  font-size: 1rem;
  font-weight: 500;
  color: #1a1a1a;
  margin-bottom: 0.75rem;
}

.info-list {
  list-style: none;
  padding: 0;
  margin: 0;
  margin-right: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.info-list li {
  font-size: 0.875rem;
  color: var(--color-gray-600);
  position: relative;
  padding-right: 1rem;

  &:before {
    content: '•';
    position: absolute;
    right: 0;
    color: var(--color-primary-500);
  }
}

/* Calendar Section */
.calendar-section {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.calendar-hint {
  font-size: 0.875rem;
  color: var(--color-gray-600);
  margin-bottom: 1rem;
}

.selected-info {
  margin-top: 1rem;
  padding: 0.75rem;
  background: rgba(139, 92, 246, 0.05);
  border-radius: 0.5rem;
}

.info-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #1a1a1a;
  margin-bottom: 0.25rem;
}

.info-value {
  font-size: 0.75rem;
  color: var(--color-gray-600);
}

/* Custom Days List */
.custom-days-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.custom-day-item {
  padding: 0.75rem;
  border: 1px solid var(--color-gray-300);
  border-radius: 0.5rem;
  font-size: 0.875rem;
}

.custom-day-date {
  font-weight: 500;
  color: #1a1a1a;
  margin-bottom: 0.25rem;
}

.custom-day-info {
  font-size: 0.75rem;
  color: var(--color-gray-600);
}
</style>
