<template>
  <BaseModal :is-open="isOpen" title="با کدام برنامه مسیریابی شود؟" size="sm" @close="emit('close')">
    <ul ref="list" class="directions-chooser" dir="rtl" data-test="directions-options">
      <li v-for="option in options" :key="option.id">
        <a
          class="directions-chooser__option"
          :class="{ 'directions-chooser__option--device': option.id === 'device' }"
          :href="option.url"
          :target="option.id === 'device' ? undefined : '_blank'"
          rel="noopener noreferrer"
          :data-test="`directions-${option.id}`"
          @click="emit('close')"
        >
          <span class="directions-chooser__label">{{ option.label }}</span>
          <small v-if="option.id === 'device'" class="directions-chooser__hint">برنامه‌ای که روی گوشی دارید</small>
        </a>
      </li>
    </ul>
  </BaseModal>
</template>

<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import BaseModal from './BaseModal.vue'
import { directionsOptions } from '@/core/utils/directions'

/**
 * «با کدام برنامه مسیریابی شود؟» (reviews-and-reschedule-round2 item 5): on a phone the phone's own chooser of
 * installed map apps comes first, then نشان / بلد / گوگل‌مپ / ویز. A dialog (BaseModal: role=dialog, Esc closes);
 * the first option takes focus when it opens.
 */
const props = defineProps<{
  isOpen: boolean
  latitude: number
  longitude: number
  /** The browser's user agent; injectable for tests. */
  userAgent?: string
}>()
const emit = defineEmits<{ close: [] }>()

const list = ref<HTMLElement | null>(null)

const options = computed(() =>
  directionsOptions(
    props.latitude,
    props.longitude,
    props.userAgent ?? (typeof navigator === 'undefined' ? '' : navigator.userAgent),
  ),
)

watch(
  () => props.isOpen,
  async (open) => {
    if (!open) return
    await nextTick()
    list.value?.querySelector<HTMLElement>('a')?.focus()
  },
  { immediate: true },
)
</script>

<style scoped>
.directions-chooser {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
.directions-chooser__option {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  padding: 0.75rem 1rem;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  color: var(--color-text-primary);
  text-decoration: none;
  font-weight: var(--font-weight-medium);
  transition: background-color 0.15s ease, border-color 0.15s ease;
}
.directions-chooser__option:hover,
.directions-chooser__option:focus-visible {
  border-color: var(--color-primary, var(--color-primary-500));
  background: var(--color-primary-50);
  outline: none;
}
.directions-chooser__option--device {
  border-color: var(--color-primary, var(--color-primary-500));
  background: var(--color-primary-50);
}
.directions-chooser__hint {
  color: var(--color-text-secondary);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-normal);
}
</style>
