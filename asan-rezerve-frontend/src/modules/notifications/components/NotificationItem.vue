<template>
  <button
    type="button"
    class="notification-row"
    :class="{ unread: !item.readAt, inert: !item.isActionable }"
    data-test="notification-row"
    @click="$emit('open', item)"
  >
    <span v-if="!item.readAt" class="unread-dot" data-test="unread-dot" aria-label="خوانده نشده" />
    <span class="content">
      <span class="subject">{{ item.subject }}</span>
      <span class="body">{{ item.body }}</span>
      <time class="when" :datetime="item.createdAt">{{ when }}</time>
    </span>
  </button>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { InboxItem } from '@/core/api/services/notification-inbox.service'

const props = defineProps<{ item: InboxItem }>()
defineEmits<{ open: [item: InboxItem] }>()

/**
 * The Persian calendar, in the browser's own locale data. The subject and body already carry the appointment's
 * date in the salon's wall-clock time (the backend writes them); this is only when the notice itself arrived.
 */
const when = computed(() => {
  const date = new Date(props.item.createdAt)
  if (Number.isNaN(date.getTime())) return ''
  return new Intl.DateTimeFormat('fa-IR-u-ca-persian', {
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date)
})
</script>

<style scoped>
.notification-row {
  display: flex;
  gap: 0.75rem;
  align-items: flex-start;
  width: 100%;
  padding: 0.875rem 1rem;
  border: none;
  border-bottom: 1px solid var(--color-gray-100);
  background: transparent;
  text-align: start;
  cursor: pointer;
  font: inherit;
}

.notification-row:hover {
  background: var(--color-gray-50);
}

.notification-row.unread {
  background: color-mix(in srgb, var(--color-primary-500) 6%, transparent);
}

/* A row whose target is gone still reads normally; it just does not look like a link. */
.notification-row.inert {
  cursor: default;
}

.unread-dot {
  flex: none;
  width: 0.5rem;
  height: 0.5rem;
  margin-top: 0.4rem;
  border-radius: 50%;
  background: var(--color-primary-500);
}

.content {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  min-width: 0;
}

.subject {
  font-weight: 600;
  color: var(--color-gray-900);
}

.body {
  color: var(--color-gray-600);
  line-height: 1.6;
}

.when {
  font-size: 0.75rem;
  color: var(--color-gray-400);
}
</style>
