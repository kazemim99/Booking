<template>
  <section class="review-list" dir="rtl">
    <p v-if="state === 'loading'" class="review-list__status">در حال بارگذاری نظرات…</p>

    <div v-else-if="state === 'error'" class="review-list__status" role="alert" data-test="reviews-error">
      نظرات بارگذاری نشد.
      <button type="button" class="link" @click="load">تلاش دوباره</button>
    </div>

    <div v-else-if="listing && listing.statistics.totalReviews === 0" class="review-list__empty" data-test="reviews-empty">
      <h3>هنوز نظری ثبت نشده است</h3>
      <p>این سالن هنوز نظر منتشرشده‌ای ندارد.</p>
    </div>

    <template v-else-if="listing">
      <div class="review-list__summary">
        <div class="review-list__overall">
          <span class="review-list__average" data-test="rating-average">
            {{ toPersianDigits(listing.statistics.averageRating.toFixed(1)) }}
          </span>
          <RatingStars :model-value="listing.statistics.averageRating" readonly label="میانگین امتیاز" />
          <span class="review-list__count" data-test="rating-count">
            بر اساس {{ toPersianDigits(listing.statistics.totalReviews) }} نظر
          </span>
        </div>

        <dl v-if="ratedDimensions.length" class="review-list__dimensions">
          <div v-for="d in ratedDimensions" :key="d" class="review-list__dimension" data-test="dimension-row">
            <dt>{{ DIMENSION_LABELS[d] }}</dt>
            <dd>
              <span class="bar"><span class="bar__fill" :style="{ width: `${(listing.statistics[d].average! / 5) * 100}%` }" /></span>
              {{ toPersianDigits(listing.statistics[d].average!.toFixed(1)) }}
            </dd>
          </div>
        </dl>
      </div>

      <p v-if="signInPrompt" class="review-list__prompt" role="status" data-test="sign-in-to-vote">
        برای ثبت رأی، ابتدا وارد حساب خود شوید.
      </p>

      <div class="review-list__items">
        <ReviewCard
          v-for="review in listing.reviews"
          :key="review.reviewId"
          :review="review"
          :provider-name="providerName"
          @vote="(isHelpful: boolean) => castVote(review, isHelpful)"
        />
      </div>
    </template>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { toPersianDigits } from '@/core/utils/persian.service'
import RatingStars from './RatingStars.vue'
import ReviewCard from './ReviewCard.vue'
import { reviewsApi } from '../api/reviews.api'
import { DIMENSIONS, DIMENSION_LABELS, type ProviderReview, type ProviderReviewListing } from '../types/reviews.types'

/**
 * A provider's published reviews and statistics, straight from the API.
 *
 * Three honest states: loading, failed, and loaded. "No reviews yet" is only ever shown when the API said so — a
 * failed request is not evidence that a salon has no reviews.
 */
const props = defineProps<{ providerId: string; providerName?: string | null }>()

const state = ref<'loading' | 'error' | 'loaded'>('loading')
const listing = ref<ProviderReviewListing | null>(null)
const signInPrompt = ref(false)

const ratedDimensions = computed(() =>
  listing.value ? DIMENSIONS.filter((d) => listing.value!.statistics[d].count > 0) : [],
)

async function load() {
  state.value = 'loading'
  try {
    listing.value = await reviewsApi.forProvider(props.providerId)
    state.value = 'loaded'
  } catch {
    state.value = 'error'
  }
}

async function castVote(review: ProviderReview, isHelpful: boolean) {
  signInPrompt.value = false
  try {
    const result = await reviewsApi.vote(review.reviewId, isHelpful)
    // What the server answered, not an optimistic guess: the server also knows about the legacy baseline.
    review.helpfulCount = result.helpfulCount
    review.notHelpfulCount = result.notHelpfulCount
    review.myVote = result.myVote
  } catch (error) {
    if ((error as { response?: { status?: number } })?.response?.status === 401) signInPrompt.value = true
  }
}

onMounted(load)
watch(() => props.providerId, load)
</script>

<style scoped>
.review-list { display: flex; flex-direction: column; gap: 1rem; }
.review-list__status, .review-list__empty { text-align: center; color: var(--color-text-secondary, #777); padding: 1.5rem 0; }
.review-list__empty h3 { margin: 0 0 0.25rem; color: var(--color-text, #222); }
.review-list__summary { display: flex; flex-wrap: wrap; gap: 1.5rem; align-items: center; }
.review-list__overall { display: flex; flex-direction: column; align-items: center; gap: 0.25rem; }
.review-list__average { font-size: 2.2rem; font-weight: 700; line-height: 1; }
.review-list__count { font-size: 0.85rem; color: var(--color-text-secondary, #777); }
.review-list__dimensions { display: grid; gap: 0.4rem; margin: 0; flex: 1; min-width: 14rem; }
.review-list__dimension { display: grid; grid-template-columns: 8rem 1fr; align-items: center; gap: 0.5rem; }
.review-list__dimension dt { font-size: 0.9rem; }
.review-list__dimension dd { margin: 0; display: flex; align-items: center; gap: 0.5rem; }
.bar { flex: 1; height: 0.45rem; border-radius: 999px; background: var(--color-surface-muted, #eee); overflow: hidden; }
.bar__fill { display: block; height: 100%; background: var(--color-warning, #f5a623); }
.review-list__prompt { background: var(--color-surface-muted, #f5f5f7); border-radius: 0.5rem; padding: 0.5rem 0.75rem; margin: 0; }
.review-list__items { display: flex; flex-direction: column; gap: 0.75rem; }
.link { background: none; border: 0; color: var(--color-primary, #6c5ce7); cursor: pointer; }
</style>
