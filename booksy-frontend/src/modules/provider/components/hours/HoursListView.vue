<template>
  <div class="hours-list-view">
    <div class="list-header">
      <h3 class="list-title">{{ t('provider.businessHours.listView.title') }}</h3>
      <div class="quick-actions">
        <button type="button" class="action-btn" @click="emit('copyMonday')" :title="t('provider.businessHours.listView.quickActions.copyMondayHint')">
          {{ t('provider.businessHours.listView.quickActions.copyMonday') }}
        </button>
        <button type="button" class="action-btn" @click="emit('setStandard')" :title="t('provider.businessHours.listView.quickActions.setStandardHint')">
          {{ t('provider.businessHours.listView.quickActions.setStandard') }}
        </button>
        <button type="button" class="action-btn danger" @click="emit('clearAll')" :title="t('provider.businessHours.listView.quickActions.clearAllHint')">
          {{ t('provider.businessHours.listView.quickActions.clearAll') }}
        </button>
      </div>
    </div>

    <div class="days-list">
      <div
        v-for="(day, index) in weekDays"
        :key="day.value"
        :class="['day-item', { 'day-closed': !scheduleData[index]?.isOpen }]"
      >
        <!-- Day Header -->
        <div class="day-item-header">
          <div class="day-info">
            <h4 class="day-name">{{ day.label }}</h4>
            <span class="day-status">
              {{ scheduleData[index]?.isOpen ? t('provider.businessHours.dayStatus.open') : t('provider.businessHours.dayStatus.closed') }}
            </span>
          </div>
          <label class="toggle">
            <input
              type="checkbox"
              :checked="scheduleData[index]?.isOpen"
              @change="handleToggleDay(index)"
            />
            <span class="toggle-slider"></span>
          </label>
        </div>

        <!-- Day Content -->
        <div v-if="scheduleData[index]?.isOpen" class="day-item-content">
          <!-- Hours -->
          <div class="hours-section">
            <div class="time-input-group">
              <div class="time-field">
                <label>{{ t('provider.businessHours.listView.openingLabel') }}</label>
                <select
                  :value="scheduleData[index]?.openTime"
                  @change="handleTimeChange(index, 'openTime', ($event.target as HTMLSelectElement).value)"
                >
                  <option v-for="time in timeOptions" :key="time.value" :value="time.value">
                    {{ time.label }}
                  </option>
                </select>
              </div>
              <span class="time-separator">—</span>
              <div class="time-field">
                <label>{{ t('provider.businessHours.listView.closingLabel') }}</label>
                <select
                  :value="scheduleData[index]?.closeTime"
                  @change="handleTimeChange(index, 'closeTime', ($event.target as HTMLSelectElement).value)"
                >
                  <option v-for="time in timeOptions" :key="time.value" :value="time.value">
                    {{ time.label }}
                  </option>
                </select>
              </div>
            </div>
          </div>

          <!-- Breaks -->
          <div class="breaks-section">
            <div class="breaks-header">
              <h5 class="breaks-title">{{ t('provider.businessHours.listView.breaksLabel') }}</h5>
              <button
                type="button"
                class="add-break-btn"
                @click="handleAddBreak(index)"
                :disabled="(scheduleData[index]?.breaks?.length || 0) >= 3"
              >
                {{ t('provider.businessHours.listView.addBreak') }}
              </button>
            </div>

            <div v-if="!scheduleData[index]?.breaks?.length" class="no-breaks">
              {{ t('provider.businessHours.listView.noBreaks') }}
            </div>

            <div v-else class="breaks-list">
              <div
                v-for="(breakItem, breakIndex) in scheduleData[index]?.breaks"
                :key="`${index}-${breakIndex}`"
                class="break-item"
              >
                <div class="time-input-group">
                  <div class="time-field">
                    <label>{{ t('provider.businessHours.breaks.from') }}</label>
                    <select
                      :value="breakItem.startTime"
                      @change="handleBreakChange(index, breakIndex, 'startTime', ($event.target as HTMLSelectElement).value)"
                    >
                      <option v-for="time in timeOptions" :key="time.value" :value="time.value">
                        {{ time.label }}
                      </option>
                    </select>
                  </div>
                  <span class="time-separator">—</span>
                  <div class="time-field">
                    <label>{{ t('provider.businessHours.breaks.to') }}</label>
                    <select
                      :value="breakItem.endTime"
                      @change="handleBreakChange(index, breakIndex, 'endTime', ($event.target as HTMLSelectElement).value)"
                    >
                      <option v-for="time in timeOptions" :key="time.value" :value="time.value">
                        {{ time.label }}
                      </option>
                    </select>
                  </div>
                  <button
                    type="button"
                    class="remove-btn"
                    @click="handleRemoveBreak(index, breakIndex)"
                    :title="t('provider.businessHours.breaks.remove')"
                  >
                    ×
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { BusinessHoursWithBreaks } from '@/modules/provider/types/hours.types'

const { t, locale } = useI18n()

// Props
interface Props {
  scheduleData: BusinessHoursWithBreaks[]
}

const props = defineProps<Props>()

// Emits
const emit = defineEmits<{
  toggleDay: [index: number]
  timeChange: [index: number, field: 'openTime' | 'closeTime', value: string]
  addBreak: [index: number]
  removeBreak: [dayIndex: number, breakIndex: number]
  breakChange: [dayIndex: number, breakIndex: number, field: 'startTime' | 'endTime', value: string]
  copyMonday: []
  setStandard: []
  clearAll: []
}>()

// Constants - Week days ordered based on locale (Persian week starts Saturday)
const weekDays = computed(() => {
  const isPersian = locale.value === 'fa'

  if (isPersian) {
    // Persian week: Saturday to Friday
    return [
      { value: 5, label: t('provider.businessHours.weekDays.saturday') },
      { value: 6, label: t('provider.businessHours.weekDays.sunday') },
      { value: 0, label: t('provider.businessHours.weekDays.monday') },
      { value: 1, label: t('provider.businessHours.weekDays.tuesday') },
      { value: 2, label: t('provider.businessHours.weekDays.wednesday') },
      { value: 3, label: t('provider.businessHours.weekDays.thursday') },
      { value: 4, label: t('provider.businessHours.weekDays.friday') },
    ]
  } else {
    // Western week: Monday to Sunday
    return [
      { value: 0, label: t('provider.businessHours.weekDays.monday') },
      { value: 1, label: t('provider.businessHours.weekDays.tuesday') },
      { value: 2, label: t('provider.businessHours.weekDays.wednesday') },
      { value: 3, label: t('provider.businessHours.weekDays.thursday') },
      { value: 4, label: t('provider.businessHours.weekDays.friday') },
      { value: 5, label: t('provider.businessHours.weekDays.saturday') },
      { value: 6, label: t('provider.businessHours.weekDays.sunday') },
    ]
  }
})

// Time options (30-minute intervals)
const timeOptions = computed(() => {
  const options = []
  for (let i = 0; i < 24; i++) {
    const hour = i.toString().padStart(2, '0')
    options.push({
      value: `${hour}:00`,
      label: formatTimeForDisplay(`${hour}:00`)
    })
    options.push({
      value: `${hour}:30`,
      label: formatTimeForDisplay(`${hour}:30`)
    })
  }
  return options
})

// Methods
function formatTimeForDisplay(time: string): string {
  const [hours, minutes] = time.split(':')
  const hour = parseInt(hours, 10)
  const ampm = hour >= 12 ? 'PM' : 'AM'
  const displayHour = hour % 12 || 12
  return `${displayHour}:${minutes} ${ampm}`
}

function handleToggleDay(index: number) {
  emit('toggleDay', index)
}

function handleTimeChange(index: number, field: 'openTime' | 'closeTime', value: string) {
  emit('timeChange', index, field, value)
}

function handleAddBreak(index: number) {
  emit('addBreak', index)
}

function handleRemoveBreak(dayIndex: number, breakIndex: number) {
  emit('removeBreak', dayIndex, breakIndex)
}

function handleBreakChange(dayIndex: number, breakIndex: number, field: 'startTime' | 'endTime', value: string) {
  emit('breakChange', dayIndex, breakIndex, field, value)
}
</script>

<style scoped>
.hours-list-view {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  overflow: hidden;
}

.list-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  border-bottom: 1px solid #e5e7eb;
  background-color: #f9fafb;
}

.list-title {
  font-size: 1.125rem;
  font-weight: 600;
  margin: 0;
  color: #111827;
}

.quick-actions {
  display: flex;
  gap: 0.5rem;
}

.action-btn {
  padding: 0.5rem 0.75rem;
  border: 1px solid #d1d5db;
  background: white;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  font-weight: 500;
  color: #111827;
  cursor: pointer;
  transition: all 0.2s;
}

.action-btn:hover {
  border-color: #3b82f6;
  background: #eff6ff;
  color: #3b82f6;
}

.action-btn.danger:hover {
  border-color: #ef4444;
  background: #fef2f2;
  color: #ef4444;
}

.days-list {
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.day-item {
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  overflow: hidden;
  transition: all 0.2s;
}

.day-item:hover {
  border-color: #d1d5db;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.day-item.day-closed {
  background-color: #f9fafb;
}

.day-item-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  background-color: #f3f4f6;
  border-bottom: 1px solid #e5e7eb;
}

.day-closed .day-item-header {
  border-bottom: none;
}

.day-info {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.day-name {
  font-size: 1rem;
  font-weight: 600;
  margin: 0;
  color: #111827;
}

.day-status {
  padding: 0.25rem 0.5rem;
  background-color: #e5e7eb;
  border-radius: 0.25rem;
  font-size: 0.75rem;
  font-weight: 500;
  color: #6b7280;
}

.day-item:not(.day-closed) .day-status {
  background-color: #d1fae5;
  color: #065f46;
}

.toggle {
  position: relative;
  display: inline-block;
  width: 3rem;
  height: 1.5rem;
  cursor: pointer;
}

.toggle input {
  opacity: 0;
  width: 0;
  height: 0;
}

.toggle-slider {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: #e5e7eb;
  border-radius: 1.5rem;
  transition: all 0.2s;
}

.toggle-slider:before {
  position: absolute;
  content: "";
  height: 1.25rem;
  width: 1.25rem;
  left: 0.125rem;
  bottom: 0.125rem;
  background-color: white;
  border-radius: 50%;
  transition: all 0.2s;
}

.toggle input:checked + .toggle-slider {
  background-color: #10b981;
}

.toggle input:checked + .toggle-slider:before {
  transform: translateX(1.5rem);
}

.day-item-content {
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.hours-section {
  padding-bottom: 1rem;
  border-bottom: 1px dashed #e5e7eb;
}

.time-input-group {
  display: flex;
  align-items: flex-end;
  gap: 0.75rem;
}

.time-field {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.time-field label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #6b7280;
}

.time-field select {
  padding: 0.5rem;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  color: #111827;
  background-color: white;
  cursor: pointer;
}

.time-separator {
  padding-bottom: 0.5rem;
  color: #9ca3af;
  font-weight: 500;
}

.breaks-section {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.breaks-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.breaks-title {
  font-size: 0.875rem;
  font-weight: 600;
  margin: 0;
  color: #4b5563;
}

.add-break-btn {
  padding: 0.375rem 0.75rem;
  border: 1px solid #d1d5db;
  background: white;
  border-radius: 0.375rem;
  font-size: 0.75rem;
  font-weight: 500;
  color: #111827;
  cursor: pointer;
  transition: all 0.2s;
}

.add-break-btn:hover:not(:disabled) {
  border-color: #3b82f6;
  background: #eff6ff;
  color: #3b82f6;
}

.add-break-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.no-breaks {
  text-align: center;
  padding: 0.75rem;
  color: #9ca3af;
  font-style: italic;
  background-color: #f9fafb;
  border-radius: 0.375rem;
  font-size: 0.875rem;
}

.breaks-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.break-item {
  padding: 0.75rem;
  background-color: #f9fafb;
  border-radius: 0.375rem;
  border: 1px solid #e5e7eb;
}

.remove-btn {
  width: 2rem;
  height: 2rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid #fca5a5;
  background: #fef2f2;
  border-radius: 0.375rem;
  font-size: 1.5rem;
  color: #ef4444;
  cursor: pointer;
  transition: all 0.2s;
  flex-shrink: 0;
  align-self: flex-end;
  margin-bottom: 0.125rem;
}

.remove-btn:hover {
  background: #ef4444;
  color: white;
}

/* Mobile Responsive */
@media (max-width: 768px) {
  .list-header {
    flex-direction: column;
    align-items: stretch;
    gap: 0.75rem;
  }

  .quick-actions {
    flex-direction: column;
  }

  .action-btn {
    width: 100%;
  }

  .time-input-group {
    flex-direction: column;
    align-items: stretch;
  }

  .time-separator {
    text-align: center;
    padding: 0.25rem 0;
  }

  .remove-btn {
    width: 100%;
    margin-top: 0.5rem;
  }
}
</style>
