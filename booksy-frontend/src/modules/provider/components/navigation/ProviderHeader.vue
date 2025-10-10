<template>
  <header class="provider-header">
    <button class="sidebar-toggle" @click="$emit('toggle-sidebar')" aria-label="Toggle sidebar">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
        <path d="M3 6h18v2H3V6zm0 5h12v2H3v-2zm0 5h18v2H3v-2z" />
      </svg>
    </button>

    <div class="brand" v-if="provider">
      <img
        v-if="provider.profile.logoUrl"
        :src="provider.profile.logoUrl"
        :alt="provider.profile.businessName"
        class="brand-logo"
      />
      <div v-else class="brand-logo placeholder">{{ businessInitials }}</div>
      <div class="brand-info">
        <div class="brand-name">{{ provider.profile.businessName }}</div>
        <div v-if="provider.profile.description" class="brand-sub">
          {{ provider.profile.description }}
        </div>
      </div>
    </div>

    <div class="spacer" />

    <UserMenu :menu-items="providerMenuItems" />
  </header>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { Provider } from '@/modules/provider/types/provider.types'
import { UserMenu } from '@/shared/components'

const props = defineProps<{
  provider: Provider | null
}>()

defineEmits<{
  (e: 'toggle-sidebar'): void
}>()

const businessInitials = computed(() => {
  const name = props.provider?.profile.businessName?.trim()
  if (!name) return 'B'
  const parts = name.split(' ').filter(Boolean)
  if (parts.length >= 2) return `${parts[0][0]}${parts[1][0]}`.toUpperCase()
  return name[0].toUpperCase()
})

const providerMenuItems = [
  {
    name: 'provider-profile',
    label: 'Business Profile',
    to: { name: 'ProviderProfile' },
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>',
  },
  {
    name: 'provider-bookings',
    label: 'Bookings',
    to: { name: 'ProviderBookings' },
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" /></svg>',
  },
]
</script>

<style scoped>
.provider-header {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: 64px;
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0 1rem;
  background: #ffffff;
  border-bottom: 1px solid #e5e7eb;
  z-index: 50;
}

.sidebar-toggle {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border: none;
  border-radius: 8px;
  background: #f3f4f6;
  color: #374151;
  cursor: pointer;
}

.sidebar-toggle svg {
  width: 20px;
  height: 20px;
}

.brand {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.brand-logo {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  object-fit: cover;
  border: 1px solid #e5e7eb;
}

.brand-logo.placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  background: #eef2ff;
  color: #4f46e5;
  font-weight: 700;
}

.brand-info {
  display: flex;
  flex-direction: column;
}
.brand-name {
  font-weight: 600;
  color: #111827;
}
.brand-sub {
  font-size: 0.75rem;
  color: #6b7280;
  max-width: 40ch;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.spacer {
  flex: 1;
}
</style>
