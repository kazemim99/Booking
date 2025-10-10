<template>
  <StepContainer
    :title="$t('provider.registration.assistance.title')"
    :subtitle="$t('provider.registration.assistance.subtitle')"
  >
    <div class="options-grid">
      <button
        v-for="option in availableOptions"
        :key="option.id"
        type="button"
        class="option-chip"
        :class="{ selected: selectedOptions.includes(option.id), disabled: isMaxReached && !selectedOptions.includes(option.id) }"
        :disabled="isMaxReached && !selectedOptions.includes(option.id)"
        @click="toggleOption(option.id)"
      >
        {{ option.label }}
      </button>
    </div>

    <p class="selection-hint">
      {{ $t('provider.registration.assistance.selected', { count: selectedOptions.length, max: 5 }) }}
    </p>

    <NavigationButtons
      :show-back="true"
      @back="$emit('back')"
      @next="handleNext"
    />
  </StepContainer>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import StepContainer from '../shared/StepContainer.vue'
import NavigationButtons from '../shared/NavigationButtons.vue'
import type { AssistanceOption } from '@/modules/provider/types/registration.types'

interface Props {
  modelValue?: AssistanceOption[]
}

interface Emits {
  (e: 'update:modelValue', value: AssistanceOption[]): void
  (e: 'next'): void
  (e: 'back'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const selectedOptions = ref<AssistanceOption[]>(props.modelValue || [])

const availableOptions = [
  { id: 'more_self_booked_clients' as AssistanceOption, label: 'More self-booked clients' },
  { id: 'selling_products' as AssistanceOption, label: 'Selling products' },
  { id: 'less_canceled_appointments' as AssistanceOption, label: 'Less canceled or missed appointments' },
  { id: 'simplified_payment_processing' as AssistanceOption, label: 'Simplified payment processing' },
  { id: 'tracking_business_statistics' as AssistanceOption, label: 'Tracking business statistics' },
  { id: 'attract_new_clients' as AssistanceOption, label: 'Attract New Clients' },
  { id: 'engage_clients' as AssistanceOption, label: 'Engage clients' },
  { id: 'social_media_integration' as AssistanceOption, label: 'Social media integration' },
  { id: 'improve_financial_performance' as AssistanceOption, label: 'Improve financial performance' },
  { id: 'other' as AssistanceOption, label: 'Other' },
]

const isMaxReached = computed(() => selectedOptions.value.length >= 5)

const toggleOption = (option: AssistanceOption) => {
  const index = selectedOptions.value.indexOf(option)
  if (index > -1) {
    selectedOptions.value.splice(index, 1)
  } else if (!isMaxReached.value) {
    selectedOptions.value.push(option)
  }
}

const handleNext = () => {
  emit('update:modelValue', selectedOptions.value)
  emit('next')
}
</script>

<style scoped>
.options-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  margin-bottom: 2rem;
}

.option-chip {
  padding: 0.75rem 1.25rem;
  background: #ffffff;
  border: 2px solid #e5e7eb;
  border-radius: 9999px;
  font-size: 0.875rem;
  font-weight: 500;
  color: #111827;
  cursor: pointer;
  transition: all 0.2s ease;
}

.option-chip:hover:not(.disabled) {
  border-color: #10b981;
  background-color: #f0fdf4;
}

.option-chip.selected {
  border-color: #10b981;
  background-color: #10b981;
  color: #ffffff;
}

.option-chip.disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.selection-hint {
  text-align: center;
  font-size: 0.875rem;
  color: #6b7280;
  margin-bottom: 1rem;
}
</style>
