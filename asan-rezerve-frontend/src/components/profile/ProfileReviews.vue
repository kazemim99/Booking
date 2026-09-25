<template>
  <section class="profile-reviews" dir="rtl">
    <header class="reviews-header">
      <h2 class="section-title">نظرات مشتریان</h2>
      <p class="section-subtitle">تجربه مشتریانی که واقعاً در این سالن نوبت داشته‌اند</p>
    </header>
    <ReviewList :provider-id="provider.id" :provider-name="provider.businessName" />
  </section>
</template>

<script setup lang="ts">
import type { Provider } from '@/modules/provider/types/provider.types'
import ReviewList from '@/modules/reviews/components/ReviewList.vue'

/**
 * The reviews tab on a provider's page.
 *
 * This component used to render six hardcoded Persian reviews, a hardcoded rating distribution and the constants
 * 4.8 / 127, with a like button that only logged to the console — on every provider's page, whatever their real
 * reviews were. It now shows the provider's published reviews from the API, and nothing it did not get from there.
 */
defineProps<{ provider: Provider }>()
</script>

<style scoped>
.profile-reviews { display: flex; flex-direction: column; gap: 1rem; }
.reviews-header { display: flex; flex-direction: column; gap: 0.25rem; }
.section-title { margin: 0; font-size: 1.25rem; }
.section-subtitle { margin: 0; color: var(--color-text-secondary, #777); font-size: 0.9rem; }
</style>
