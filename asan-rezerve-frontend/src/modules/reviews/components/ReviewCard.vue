<template>
  <article class="review-card" dir="rtl" data-test="review-card">
    <header class="review-card__header">
      <span class="review-card__avatar" aria-hidden="true">{{ initial }}</span>
      <strong class="review-card__author" data-test="review-author">{{ author }}</strong>
      <RatingStars :model-value="review.rating" readonly size="sm" label="امتیاز کلی" />
      <time class="review-card__date" :datetime="review.createdAt">{{ date }}</time>
      <span v-if="review.isVerified" class="review-card__verified">مراجعه تأییدشده</span>
    </header>

    <ul v-if="rated.length" class="review-card__dimensions">
      <li v-for="d in rated" :key="d" data-test="dimension-chip">
        {{ DIMENSION_LABELS[d] }} {{ toPersianDigits(review.dimensions[d]!) }}
      </li>
    </ul>

    <p v-if="review.comment" class="review-card__comment">{{ review.comment }}</p>

    <blockquote v-if="review.providerResponse" class="review-card__reply" data-test="reply">
      <strong>پاسخ سالن</strong>
      <p>{{ review.providerResponse }}</p>
    </blockquote>

    <footer class="review-card__votes">
      <span>این نظر مفید بود؟</span>
      <button
        type="button"
        class="vote"
        :class="{ 'vote--active': review.myVote === 'helpful' }"
        :aria-pressed="review.myVote === 'helpful'"
        data-test="vote-helpful"
        @click="emit('vote', true)"
      >
        بله ({{ toPersianDigits(review.helpfulCount) }})
      </button>
      <button
        type="button"
        class="vote"
        :class="{ 'vote--active': review.myVote === 'notHelpful' }"
        :aria-pressed="review.myVote === 'notHelpful'"
        data-test="vote-not-helpful"
        @click="emit('vote', false)"
      >
        خیر ({{ toPersianDigits(review.notHelpfulCount) }})
      </button>
    </footer>
  </article>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { toPersianDigits } from '@/core/utils/persian.service'
import RatingStars from './RatingStars.vue'
import { DIMENSIONS, DIMENSION_LABELS, type ProviderReview } from '../types/reviews.types'

/**
 * One published review. Shows only the dimensions the customer actually rated, the salon's reply when the API sent
 * one (it sends a reply only once approved), and the reader's own vote as pressed. Voting itself is the parent's.
 */
const props = defineProps<{ review: ProviderReview }>()
const emit = defineEmits<{ vote: [isHelpful: boolean] }>()

const rated = computed(() => DIMENSIONS.filter((d) => typeof props.review.dimensions[d] === 'number'))

/** The name the listing gives; a review from an older listing without one is still someone's. */
const author = computed(() => props.review.customerName?.trim() || 'مشتری')

/** Its first letter — a face for the review, never a photo of a person. */
const initial = computed(() => Array.from(author.value)[0])

const date = computed(() => {
  const d = new Date(props.review.createdAt)
  return Number.isNaN(d.getTime()) ? '' : d.toLocaleDateString('fa-IR')
})
</script>

<style scoped>
.review-card {
  border: 1px solid var(--color-border, #eee);
  border-radius: 0.75rem;
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
  background: var(--color-surface, #fff);
}
.review-card__header { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; }
.review-card__date { color: var(--color-text-secondary, #777); font-size: 0.85rem; }
.review-card__avatar {
  width: 2rem;
  height: 2rem;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: var(--color-primary-light, #ede9fe);
  color: var(--color-primary, #6d28d9);
  font-weight: 700;
}
.review-card__author { font-weight: 600; }
.review-card__verified {
  font-size: 0.75rem;
  color: var(--color-success, #00b894);
  border: 1px solid currentColor;
  border-radius: 999px;
  padding: 0.05rem 0.5rem;
}
.review-card__dimensions { display: flex; gap: 0.4rem; flex-wrap: wrap; list-style: none; padding: 0; margin: 0; }
.review-card__dimensions li {
  font-size: 0.8rem;
  background: var(--color-surface-muted, #f5f5f7);
  border-radius: 999px;
  padding: 0.15rem 0.6rem;
}
.review-card__comment { margin: 0; line-height: 1.8; }
.review-card__reply {
  margin: 0;
  border-inline-start: 3px solid var(--color-primary, #6c5ce7);
  padding: 0.4rem 0.8rem;
  background: var(--color-surface-muted, #f5f5f7);
  border-radius: 0.5rem;
}
.review-card__reply p { margin: 0.25rem 0 0; }
.review-card__votes { display: flex; align-items: center; gap: 0.5rem; font-size: 0.85rem; }
.vote {
  border: 1px solid var(--color-border, #ddd);
  background: none;
  border-radius: 999px;
  padding: 0.15rem 0.7rem;
  cursor: pointer;
}
.vote--active { border-color: var(--color-primary, #6c5ce7); color: var(--color-primary, #6c5ce7); font-weight: 600; }
</style>
