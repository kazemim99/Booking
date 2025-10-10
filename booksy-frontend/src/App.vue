<!-- src/App.vue -->
<template>
  <div id="app" :dir="direction" :class="appClasses">
    <!-- Global Toast/Notification Container -->
    <AppToast />

    <!-- Router View - Layouts are applied at route level -->
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
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { RouterView } from 'vue-router'
import { useRTL } from '@/core/composables/useRTL'
import { Toast as AppToast, Spinner as AppSpinner } from '@/shared/components'

// ============================================
// Composables & State
// ============================================

const { direction } = useRTL()

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
