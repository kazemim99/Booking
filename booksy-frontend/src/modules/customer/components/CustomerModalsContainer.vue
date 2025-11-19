<template>
  <teleport to="body">
    <!-- Profile Edit Modal -->
    <ProfileEditModal
      v-if="activeModal === 'profile'"
      :is-open="activeModal === 'profile'"
      @close="customerStore.closeModal()"
    />

    <!-- Bookings Sidebar -->
    <BookingsSidebar
      v-if="activeModal === 'bookings'"
      :is-open="activeModal === 'bookings'"
      @close="customerStore.closeModal()"
    />

    <!-- Favorites Modal -->
    <FavoritesModal
      v-if="activeModal === 'favorites'"
      :is-open="activeModal === 'favorites'"
      @close="customerStore.closeModal()"
    />

    <!-- Reviews Modal -->
    <ReviewsModal
      v-if="activeModal === 'reviews'"
      :is-open="activeModal === 'reviews'"
      @close="customerStore.closeModal()"
    />

    <!-- Settings Modal -->
    <SettingsModal
      v-if="activeModal === 'settings'"
      :is-open="activeModal === 'settings'"
      @close="customerStore.closeModal()"
    />
  </teleport>
</template>

<script setup lang="ts">
import { computed, defineAsyncComponent } from 'vue'
import { useCustomerStore } from '../stores/customer.store'

const customerStore = useCustomerStore()

// Lazy load modal components for better performance
const ProfileEditModal = defineAsyncComponent(() => import('./modals/ProfileEditModal.vue'))
const BookingsSidebar = defineAsyncComponent(() => import('./modals/BookingsSidebar.vue'))
const FavoritesModal = defineAsyncComponent(() => import('./modals/FavoritesModal.vue'))
const ReviewsModal = defineAsyncComponent(() => import('./modals/ReviewsModal.vue'))
const SettingsModal = defineAsyncComponent(() => import('./modals/SettingsModal.vue'))

const activeModal = computed(() => customerStore.activeModal)
</script>
