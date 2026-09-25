<!-- src/App.vue -->
<template>
  <div id="app" :dir="direction" :class="appClasses">
    <!-- Global Toast/Notification Container -->
    <AppToast />

    <!-- Customer Modals Container (Profile, Bookings, Favorites, Reviews, Settings) -->
    <CustomerModalsContainer />

    <!-- Mobile Bottom Navigation (only show for certain layouts) -->
    <BottomNavigation v-if="showBottomNav" />

    <!-- Dynamic Layout Rendering -->
    <component :is="currentLayout">
      <Suspense>
        <template #default>
          <RouterView />
        </template>

        <template #fallback>
          <div class="app-loading">
            <AppSpinner size="large" />
            <p>Loading...</p>
          </div>
        </template>
      </Suspense>
    </component>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { RouterView, useRoute } from 'vue-router'
import { useRTL } from '@/core/composables/useRTL'
import { Toast as AppToast, Spinner as AppSpinner } from '@/shared/components'
import CustomerModalsContainer from '@/modules/customer/components/CustomerModalsContainer.vue'
import BottomNavigation from '@/shared/components/layout/BottomNavigation.vue'

// Layout Components
import FocusedLayout from '@/shared/components/layout/FocusedLayout.vue'
import DefaultLayout from '@/shared/components/layout/DefaultLayout.vue'

// ============================================
// Composables & State
// ============================================

const { direction } = useRTL()
const route = useRoute()

// ============================================
// Layout System
// ============================================

// Available layouts
const layouts = {
  focused: FocusedLayout,
  default: DefaultLayout,
}

// Determine current layout from route meta
const currentLayout = computed(() => {
  const layoutName = (route.meta.layout as keyof typeof layouts) || 'default'
  return layouts[layoutName] || layouts.default
})

// Show bottom navigation only for default layout on mobile
const showBottomNav = computed(() => {
  const layoutName = route.meta.layout || 'default'
  return layoutName === 'default' && window.innerWidth < 768
})

// ============================================
// Computed Properties
// ============================================

const appClasses = computed(() => ({
  'app--rtl': direction.value === 'rtl',
  'app--ltr': direction.value === 'ltr',
}))

// ============================================
// Lifecycle
// ============================================

onMounted(() => {
  console.log('🚀 Booksy App Mounted')
  console.log('📍 Direction:', direction.value)
})
</script>

<style lang="scss">
// Import global styles
@import '@/assets/styles/main.scss';

// ============================================
// App Root Styles
// ============================================

#app {
  width: 100%;
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
}

// ============================================
// Loading State
// ============================================

.app-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  gap: 1rem;

  p {
    color: var(--color-text-secondary);
    font-size: 0.875rem;
  }
}
</style>
