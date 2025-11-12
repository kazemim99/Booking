<template>
  <DashboardLayout>
    <!-- Booking Statistics -->
    <BookingStatsCard :provider-id="currentProvider?.id" />

    <!-- Booking List -->
    <BookingListCard :provider-id="currentProvider?.id" />
  </DashboardLayout>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useProviderStore } from '../stores/provider.store'
import DashboardLayout from '../components/dashboard/DashboardLayout.vue'
import BookingStatsCard from '../components/dashboard/BookingStatsCard.vue'
import BookingListCard from '../components/dashboard/BookingListCard.vue'

const providerStore = useProviderStore()

const currentProvider = computed(() => providerStore.currentProvider)

onMounted(async () => {
  try {
    if (!currentProvider.value) {
      await providerStore.loadCurrentProvider()
    }
  } catch (error) {
    console.error('Failed to load provider data:', error)
  }
})
</script>

