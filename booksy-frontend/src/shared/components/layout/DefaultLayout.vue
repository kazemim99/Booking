<template>
  <div class="default-layout">
    <!-- Just render the content as-is -->
    <slot />
  </div>
</template>

<script setup lang="ts">
// Default layout - no wrapper, renders content directly
// Used for pages that manage their own layout (like HomeView with LandingHeader)
</script>

<style scoped>
.default-layout {
  width: 100%;
  min-height: 100vh;
}
</style>
