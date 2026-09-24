<template>
  <div class="reschedule-action">
    <button
      class="btn-reschedule"
      data-testid="booking-reschedule-button"
      :disabled="blockedReason !== null"
      @click="emit('reschedule')"
    >
      تغییر زمان
    </button>
    <p v-if="blockedReason" class="reschedule-reason" data-testid="booking-reschedule-reason">
      {{ blockedReason }}
    </p>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

/**
 * «تغییر زمان» for a booking that is still ahead. Inside the salon's reschedule window it is disabled and says why,
 * right here — the customer used to choose a slot and only then be told the window had closed (QA 2026-09-24).
 */
const props = defineProps<{
  /** Why the booking cannot be moved now (Persian, from the server); null/blank when it can. */
  rescheduleBlockedReason?: string | null
}>()

const emit = defineEmits<{ (e: 'reschedule'): void }>()

const blockedReason = computed(() => {
  const reason = props.rescheduleBlockedReason?.trim()
  return reason ? reason : null
})
</script>

<style scoped lang="scss">
.reschedule-action {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.25rem;
}

.btn-reschedule {
  background: #e0e7ff;
  color: var(--color-primary-700);
  border: none;
  border-radius: 8px;
  padding: 0.5rem 1rem;
  cursor: pointer;

  &:hover:not(:disabled) {
    background: #c7d2fe;
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
}

.reschedule-reason {
  margin: 0;
  font-size: 0.8rem;
  color: var(--color-gray-600);
}
</style>
