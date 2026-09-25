<template>
  <header class="provider-header">
    <button class="sidebar-toggle" @click="$emit('toggle-sidebar')" :aria-label="$t('provider.navigation.toggleSidebar')">
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
import { useI18n } from 'vue-i18n'
import type { Provider } from '@/modules/provider/types/provider.types'
import { UserMenu } from '@/shared/components'

const { t } = useI18n()

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
    label: t('provider.menu.businessProfile'),
    to: { name: 'ProviderProfile' },
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>',
  },
  {
    name: 'provider-bookings',
    label: t('provider.menu.bookings'),
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
  height: var(--header-height);
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: 0 var(--spacing-lg);
  background: var(--color-background);
  border-bottom: 1px solid var(--color-border);
  box-shadow: var(--shadow-sm);
  z-index: var(--z-index-fixed);
  transition: box-shadow var(--transition-base);
}

.provider-header:hover {
  box-shadow: var(--shadow-md);
}

.sidebar-toggle {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border: none;
  border-radius: var(--radius-md);
  background: var(--color-gray-100);
  color: var(--color-gray-700);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.sidebar-toggle:hover {
  background: var(--color-primary-50);
  color: var(--color-primary-600);
  transform: scale(1.05);
}

.sidebar-toggle:active {
  transform: scale(0.95);
}

.sidebar-toggle svg {
  width: 22px;
  height: 22px;
}

.brand {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  flex-shrink: 1;
  min-width: 0;
  overflow: hidden;
}

.brand-logo {
  width: 40px;
  height: 40px;
  border-radius: var(--radius-md);
  object-fit: cover;
  border: 2px solid var(--color-border-light);
  box-shadow: var(--shadow-sm);
  transition: all var(--transition-fast);
}

.brand-logo:hover {
  border-color: var(--color-primary-200);
  box-shadow: var(--shadow-primary);
}

.brand-logo.placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, var(--color-primary-50) 0%, var(--color-primary-100) 100%);
  color: var(--color-primary-600);
  font-weight: var(--font-weight-bold);
  font-size: var(--font-size-lg);
}

.brand-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.brand-name {
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-base);
  color: var(--color-text-primary);
  line-height: var(--line-height-tight);
}

.brand-sub {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  max-width: 40ch;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  line-height: var(--line-height-tight);
}

.spacer {
  flex: 1;
}

/* User Menu - prevent overflow */
.provider-header :deep(.user-menu) {
  flex-shrink: 0;
  margin-inline-end: 0;
}

/* Mobile responsive */
@media (max-width: 768px) {
  .provider-header {
    padding: 0 var(--spacing-md);
    gap: var(--spacing-sm);
  }

  .brand-sub {
    display: none;
  }
}

@media (max-width: 640px) {
  .provider-header {
    padding: 0 var(--spacing-sm);
  }

  .brand-info {
    display: none;
  }

  .brand {
    gap: var(--spacing-xs);
  }
}

@media (max-width: 480px) {
  .sidebar-toggle {
    width: 36px;
    height: 36px;
  }

  .brand-logo {
    width: 36px;
    height: 36px;
  }
}
</style>
