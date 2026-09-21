<template>
  <main class="notifications-page">
    <NotificationList :audience="audience" />
  </main>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import NotificationList from '../components/NotificationList.vue'
import type { InboxAudience } from '../utils/destination'

/** One page for everyone signed in; only where a tap leads differs between a salon and a customer. */
const auth = useAuthStore()
const audience = computed<InboxAudience>(() => (auth.hasRole('Provider') ? 'provider' : 'customer'))
</script>

<style scoped>
.notifications-page {
  max-width: 42rem;
  margin: 0 auto;
  padding: 1.5rem 1rem;
}
</style>
