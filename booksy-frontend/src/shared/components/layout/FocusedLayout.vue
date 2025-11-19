<template>
  <div class="focused-layout">
    <!-- Simple Header (Sticky) -->
    <SimpleHeader
      :show-back-button="showBackButton"
      :back-button-text="backButtonText"
      :back-button-title="backButtonTitle"
    >
      <template v-if="$slots.breadcrumbs" #breadcrumbs>
        <slot name="breadcrumbs"></slot>
      </template>
    </SimpleHeader>

    <!-- Full-Screen Main Content -->
    <main class="focused-main">
      <slot />
    </main>

    <!-- No Footer - Maximize Content Space -->
  </div>
</template>

<script setup lang="ts">
import SimpleHeader from './Header/SimpleHeader.vue'

interface Props {
  showBackButton?: boolean
  backButtonText?: string
  backButtonTitle?: string
}

withDefaults(defineProps<Props>(), {
  showBackButton: true,
  backButtonText: 'بازگشت',
  backButtonTitle: 'بازگشت به صفحه قبل',
})
</script>

<style scoped>
.focused-layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f9fafb;
}

.focused-main {
  flex: 1;
  width: 100%;
  min-height: calc(100vh - 72px); /* Full viewport minus header height */
  padding: 0;
  margin: 0;
}

/* Ensure no extra padding/margin on mobile */
@media (max-width: 768px) {
  .focused-main {
    min-height: calc(100vh - 60px); /* Smaller header on mobile */
  }
}

/* Dark mode support (future) */
@media (prefers-color-scheme: dark) {
  .focused-layout {
    background: #111827;
  }
}
</style>
