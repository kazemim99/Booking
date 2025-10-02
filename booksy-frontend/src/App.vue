<template>
  <div id="app">
    <AppLayout v-if="isAuthenticated" />
    <RouterView v-else />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { RouterView } from 'vue-router'
import AppLayout from '@/shared/components/layout/AppLayout.vue'
import { useAuthStore } from '@/core/stores/modules/auth.store'

const authStore = useAuthStore()
const isAuthenticated = computed(() => authStore.isAuthenticated)
</script>
