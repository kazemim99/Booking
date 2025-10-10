<template>
  <StepContainer
    :title="$t('provider.registration.hours.title')"
    :subtitle="$t('provider.registration.hours.subtitle')"
  >
    <!-- TODO: Implement full business hours management with modals -->
    <!-- For now, simplified day toggle list -->
    <div class="hours-list">
      <div v-for="day in businessHours" :key="day.dayOfWeek" class="day-row">
        <label class="toggle-container">
          <input
            v-model="day.isOpen"
            type="checkbox"
            class="toggle-input"
          />
          <span class="toggle-slider"></span>
        </label>
        <span class="day-name">{{ getDayName(day.dayOfWeek) }}</span>
        <span class="day-hours">
          {{ day.isOpen ? '10:00 AM - 7:00 PM' : 'Closed' }}
        </span>
      </div>
    </div>

    <p class="note">{{ $t('provider.registration.hours.note') }}</p>

    <NavigationButtons
      :show-back="true"
      :can-continue="hasAtLeastOneOpenDay"
      @back="$emit('back')"
      @next="handleNext"
    />
  </StepContainer>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import StepContainer from '../shared/StepContainer.vue'
import NavigationButtons from '../shared/NavigationButtons.vue'
import type { DayHours } from '@/modules/provider/types/registration.types'

interface Props {
  modelValue?: DayHours[]
}

interface Emits {
  (e: 'update:modelValue', value: DayHours[]): void
  (e: 'next'): void
  (e: 'back'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const businessHours = ref<DayHours[]>(props.modelValue || Array.from({ length: 7 }, (_, i) => ({
  dayOfWeek: i,
  isOpen: i >= 1 && i <= 5, // Monday-Friday open by default
  openTime: { hours: 10, minutes: 0 },
  closeTime: { hours: 19, minutes: 0 },
  breaks: [],
})))

const getDayName = (dayOfWeek: number): string => {
  const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
  return days[dayOfWeek]
}

const hasAtLeastOneOpenDay = computed(() => businessHours.value.some(day => day.isOpen))

const handleNext = () => {
  emit('update:modelValue', businessHours.value)
  emit('next')
}
</script>

<style scoped>
.hours-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-bottom: 2rem;
}

.day-row {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  background: #ffffff;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
}

.toggle-container {
  position: relative;
  display: inline-block;
  width: 3rem;
  height: 1.75rem;
}

.toggle-input {
  opacity: 0;
  width: 0;
  height: 0;
}

.toggle-slider {
  position: absolute;
  cursor: pointer;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: #d1d5db;
  transition: 0.3s;
  border-radius: 9999px;
}

.toggle-slider:before {
  position: absolute;
  content: "";
  height: 1.25rem;
  width: 1.25rem;
  left: 0.25rem;
  bottom: 0.25rem;
  background-color: white;
  transition: 0.3s;
  border-radius: 50%;
}

.toggle-input:checked + .toggle-slider {
  background-color: #10b981;
}

.toggle-input:checked + .toggle-slider:before {
  transform: translateX(1.25rem);
}

.day-name {
  flex: 0 0 7rem;
  font-weight: 600;
  color: #111827;
}

.day-hours {
  flex: 1;
  color: #6b7280;
  font-size: 0.875rem;
}

.note {
  font-size: 0.875rem;
  color: #6b7280;
  text-align: center;
  font-style: italic;
}
</style>
