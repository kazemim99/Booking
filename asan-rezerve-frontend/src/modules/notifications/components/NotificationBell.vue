<template>
  <RouterLink :to="{ name: 'Notifications' }" class="notification-bell" :aria-label="label">
    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="icon">
      <path
        stroke-linecap="round"
        stroke-linejoin="round"
        stroke-width="2"
        d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
      />
    </svg>
    <span v-if="inbox.unreadCount > 0" class="badge" data-test="unread-badge">{{ badgeText }}</span>
  </RouterLink>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { useInboxStore } from '@/core/stores/modules/inbox.store'

/**
 * The unread badge. Reads the count on its own endpoint so the header never has to load a page of the inbox to
 * show a number. It replaces a header bell that displayed a hardcoded "3" with a comment saying it should come
 * from a store.
 */
const inbox = useInboxStore()

onMounted(() => {
  // A failed count leaves the badge hidden. It is a hint, not a record, and a wrong number is worse than none.
  inbox.refreshCount().catch(() => undefined)
})

const badgeText = computed(() => (inbox.unreadCount > 99 ? '۹۹+' : inbox.unreadCount.toLocaleString('fa-IR')))

const label = computed(() =>
  inbox.unreadCount > 0 ? `اعلان‌ها، ${inbox.unreadCount} خوانده نشده` : 'اعلان‌ها',
)
</script>

<style scoped>
.notification-bell {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.5rem;
  height: 2.5rem;
  border-radius: 50%;
  color: var(--color-gray-600);
}

.notification-bell:hover {
  background: var(--color-gray-100);
}

.icon {
  width: 1.5rem;
  height: 1.5rem;
}

.badge {
  position: absolute;
  top: 0.125rem;
  inset-inline-end: 0.125rem;
  min-width: 1.125rem;
  padding: 0 0.25rem;
  border-radius: 999px;
  background: var(--color-primary-500);
  color: white;
  font-size: 0.6875rem;
  line-height: 1.125rem;
  text-align: center;
}
</style>
