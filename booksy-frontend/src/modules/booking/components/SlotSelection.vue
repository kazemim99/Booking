<!--
  REFACTORED: Now uses ProviderSelection component with two-step modal approach
  Old implementation: Combined calendar + time slots in one view
  New implementation: Provider selection → Modal with calendar + time slots
-->
<template>
  <ProviderSelection
    :provider-id="providerId"
    :service-id="serviceId"
    @slot-selected="handleSlotSelected"
  />
</template>

<script setup lang="ts">
import ProviderSelection from './ProviderSelection.vue'

interface Props {
  providerId: string
  serviceId: string | null
  selectedDate: string | null
  selectedTime: string | null
  selectedStaffId: string | null
}

const props = defineProps<Props>()

const emit = defineEmits<{
  (e: 'slot-selected', slot: { date: string; startTime: string; endTime: string; staffId: string; staffName: string }): void
}>()

// Handle slot selection from ProviderSelection component
const handleSlotSelected = (slot: { date: string; startTime: string; endTime: string; staffId: string; staffName: string }) => {
  emit('slot-selected', slot)
}
</script>

<style scoped>
/* Styles moved to ProviderSelection.vue and TimeSlotModal.vue */
</style>
