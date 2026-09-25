<template>
  <span
    class="rating-summary"
    :class="`rating-summary--${size}`"
    :aria-label="label"
    role="img"
    data-test="rating-summary"
  >
    <template v-if="hasReviews">
      <span class="rating-summary__star" aria-hidden="true">★</span> <span class="rating-summary__value" data-test="rating-value">{{ value }}</span> <span class="rating-summary__sep" aria-hidden="true">·</span> <span class="rating-summary__count" data-test="rating-count">{{ countText }}</span>
    </template>
    <span v-else class="rating-summary__empty" data-test="rating-empty">هنوز نظری ثبت نشده</span>
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { toPersianDigits } from '@/core/utils/persian.service'

/**
 * A salon's rating and how many reviews it rests on, as two things: «★ ۴.۰ · ۱ نظر». Written side by side without a
 * separator they read as one number («۴.۰ ۱ نظر» → «۱۴», reviews-and-reschedule-round2 item 2). The count decides:
 * none means «هنوز نظری ثبت نشده», never "rated zero". Every rating-with-count on the web goes through this.
 */
const props = withDefaults(
  defineProps<{
    rating?: number | null
    count?: number | null
    size?: 'sm' | 'md'
  }>(),
  { rating: 0, count: 0, size: 'md' },
)

const hasReviews = computed(() => (props.count ?? 0) > 0)
const value = computed(() => toPersianDigits((props.rating ?? 0).toFixed(1)))
const countText = computed(() => `${toPersianDigits(props.count ?? 0)} نظر`)
const label = computed(() =>
  hasReviews.value ? `امتیاز ${value.value} از ۵، ${countText.value}` : 'هنوز نظری ثبت نشده',
)
</script>

<style scoped>
.rating-summary {
  display: inline-flex;
  align-items: baseline;
  gap: 0.3rem;
  white-space: nowrap;
  font-size: var(--font-size-sm);
  line-height: 1.4;
}
.rating-summary--sm { font-size: var(--font-size-xs); }
.rating-summary__star { color: var(--color-gold); }
.rating-summary__value { font-weight: var(--font-weight-semibold); color: var(--color-text-primary); }
.rating-summary__sep { color: var(--color-text-tertiary); font-weight: var(--font-weight-bold); }
.rating-summary__count,
.rating-summary__empty { color: var(--color-text-secondary); }
</style>
