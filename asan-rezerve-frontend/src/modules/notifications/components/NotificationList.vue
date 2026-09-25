<template>
  <section class="notification-list" dir="rtl">
    <header class="list-header">
      <h2 class="title">اعلان‌ها</h2>
      <button
        v-if="inbox.unreadCount > 0"
        type="button"
        class="mark-all"
        data-test="mark-all-read"
        :disabled="busy"
        @click="markAll"
      >
        خواندن همه
      </button>
    </header>

    <p v-if="inbox.loading && !inbox.loaded" class="state">در حال بارگذاری…</p>

    <!-- A failure is reported as a failure. It is never allowed to read as "you have no notifications". -->
    <div v-else-if="inbox.error" class="state error" data-test="inbox-error">
      <p>اعلان‌ها بارگذاری نشد.</p>
      <button type="button" class="retry" @click="inbox.load()">تلاش دوباره</button>
    </div>

    <p v-else-if="inbox.isEmpty" class="state" data-test="inbox-empty">هنوز اعلانی ندارید.</p>

    <div v-else class="rows">
      <NotificationItem v-for="item in inbox.items" :key="item.id" :item="item" @open="open" />
    </div>

    <p v-if="actionError" class="action-error" role="alert">{{ actionError }}</p>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useInboxStore } from '@/core/stores/modules/inbox.store'
import type { InboxItem } from '@/core/api/services/notification-inbox.service'
import NotificationItem from './NotificationItem.vue'
import { destinationRoute, type InboxAudience } from '../utils/destination'

const props = defineProps<{ audience: InboxAudience }>()

const inbox = useInboxStore()
const router = useRouter()
const busy = ref(false)
const actionError = ref<string | null>(null)

onMounted(() => inbox.load())

/**
 * Reading comes first and navigation second, but a failed read does not trap the person here: they asked to
 * see the thing, and the badge is already put back by the store if the server refused.
 */
async function open(item: InboxItem): Promise<void> {
  actionError.value = null

  try {
    await inbox.markRead(item.id)
  } catch {
    actionError.value = 'علامت‌گذاری اعلان انجام نشد.'
  }

  const target = destinationRoute(item, props.audience)
  if (target) await router.push(target)
}

async function markAll(): Promise<void> {
  busy.value = true
  actionError.value = null

  try {
    await inbox.markAllRead()
  } catch {
    actionError.value = 'علامت‌گذاری اعلان‌ها انجام نشد.'
  } finally {
    busy.value = false
  }
}
</script>

<style scoped>
.notification-list {
  background: white;
  border-radius: 0.75rem;
  overflow: hidden;
}

.list-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem;
  border-bottom: 1px solid var(--color-gray-100);
}

.title {
  margin: 0;
  font-size: 1.125rem;
  color: var(--color-gray-900);
}

.mark-all,
.retry {
  border: none;
  background: none;
  color: var(--color-primary-600);
  font: inherit;
  cursor: pointer;
}

.mark-all:disabled {
  opacity: 0.5;
  cursor: default;
}

.state {
  padding: 2rem 1rem;
  text-align: center;
  color: var(--color-gray-500);
}

.error {
  color: var(--color-gray-800);
}

.action-error {
  margin: 0;
  padding: 0.75rem 1rem;
  color: var(--color-warning-500);
}
</style>
