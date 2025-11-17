<template>
  <div class="booking-view" dir="rtl">
    <BookingWizard />
  </div>
</template>

<script setup lang="ts">
import BookingWizard from '../components/BookingWizard.vue'
</script>

<style scoped>
.booking-view {
  min-height: 100vh;
}
</style>
