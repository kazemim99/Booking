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

    <!-- The salon's answer to THIS review: nested under it, joined by a line, tinted, and signed with the salon. -->
    <section v-if="review.providerResponse" class="review-card__reply" :aria-label="`${replyTitle} به این نظر`" data-test="reply">
      <header class="review-card__reply-header">
        <span class="review-card__reply-avatar" aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M3 9l1.5-5h15L21 9" />
            <path d="M3 9a3 3 0 006 0 3 3 0 006 0 3 3 0 006 0" />
            <path d="M5 12v8h14v-8" />
            <path d="M10 20v-5h4v5" />
          </svg>
        </span>
        <strong class="review-card__reply-author" data-test="reply-author">{{ replyTitle }}</strong>
        <svg class="review-card__reply-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="M9 14L4 9l5-5" />
          <path d="M4 9h10a6 6 0 016 6v5" />
        </svg>
      </header>
      <p class="review-card__reply-text">{{ review.providerResponse }}</p>
    </section>

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
const props = defineProps<{
  review: ProviderReview
  /** The salon the reviews are about, to sign its reply «پاسخ سالن نهال». */
  providerName?: string | null
}>()
const emit = defineEmits<{ vote: [isHelpful: boolean] }>()

const rated = computed(() => DIMENSIONS.filter((d) => typeof props.review.dimensions[d] === 'number'))

/** The name the listing gives; a review from an older listing without one is still someone's. */
const author = computed(() => props.review.customerName?.trim() || 'مشتری')

/** Its first letter — a face for the review, never a photo of a person. */
const initial = computed(() => Array.from(author.value)[0])

const replyTitle = computed(() => {
  const name = props.providerName?.trim()
  return name ? `پاسخ ${name}` : 'پاسخ سالن'
})

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
/* The reply hangs off the review: indented from the inline start, a primary connector line leading into it. */
.review-card__reply {
  --reply-accent: var(--color-primary, var(--color-primary-500));
  position: relative;
  margin: 0.25rem 0 0;
  margin-inline-start: 1.5rem;
  padding: 0.6rem 0.8rem;
  border-radius: 0.75rem;
  border-start-start-radius: 0.25rem;
  background: color-mix(in srgb, var(--reply-accent) 8%, transparent);
  border: 1px solid color-mix(in srgb, var(--reply-accent) 20%, transparent);
}
.review-card__reply::before {
  content: '';
  position: absolute;
  inset-inline-start: -1rem;
  top: -0.6rem;
  width: 0.85rem;
  height: 1.6rem;
  border-inline-start: 2px solid var(--reply-accent);
  border-bottom: 2px solid var(--reply-accent);
  border-end-start-radius: 0.6rem;
}
.review-card__reply-header { display: flex; align-items: center; gap: 0.5rem; }
.review-card__reply-avatar {
  width: 1.75rem;
  height: 1.75rem;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  background: var(--reply-accent);
  color: var(--color-text-inverse);
}
.review-card__reply-avatar svg { width: 1rem; height: 1rem; }
.review-card__reply-author { font-size: 0.9rem; color: var(--reply-accent); }
.review-card__reply-icon { width: 1rem; height: 1rem; color: var(--reply-accent); margin-inline-start: auto; }
[dir='rtl'] .review-card__reply-icon { transform: scaleX(-1); }
.review-card__reply-text { margin: 0.4rem 0 0; line-height: 1.8; }
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
