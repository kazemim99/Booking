<template>
  <form class="review-form" dir="rtl" @submit.prevent="send">
    <fieldset class="review-form__overall">
      <legend>امتیاز کلی شما</legend>
      <RatingStars v-model="rating" size="lg" label="امتیاز کلی" data-test="overall" />
    </fieldset>

    <button
      type="button"
      class="review-form__toggle"
      :aria-expanded="showDimensions"
      data-test="dimensions-toggle"
      @click="showDimensions = !showDimensions"
    >
      {{ showDimensions ? 'بستن جزئیات' : 'جزئیات بیشتر (اختیاری)' }}
    </button>

    <div v-if="showDimensions" class="review-form__dimensions">
      <div v-for="d in DIMENSIONS" :key="d" class="review-form__dimension">
        <span>{{ DIMENSION_LABELS[d] }}</span>
        <RatingStars
          :model-value="dimensions[d] ?? null"
          :label="DIMENSION_LABELS[d]"
          :data-test="`dimension-${d}`"
          @update:model-value="(v: number) => (dimensions[d] = v)"
        />
      </div>
    </div>

    <label class="review-form__comment">
      <span>نظر شما (اختیاری)</span>
      <textarea v-model="comment" rows="4" maxlength="2000" data-test="comment" />
      <small :class="{ 'is-error': commentTooShort }" data-test="comment-hint">
        {{ commentTooShort ? `نظر باید دست‌کم ${toPersianDigits(10)} نویسه باشد` : `${toPersianDigits(trimmed.length)} / ${toPersianDigits(2000)}` }}
      </small>
    </label>

    <p class="review-form__note">نظر شما پس از بررسی نمایش داده می‌شود.</p>

    <div class="review-form__actions">
      <button type="submit" class="btn btn--primary" :disabled="!canSend || submitting" data-test="submit">
        {{ submitLabel }}
      </button>
      <button v-if="cancellable" type="button" class="btn btn--ghost" @click="emit('cancel')">انصراف</button>
    </div>
  </form>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { toPersianDigits } from '@/core/utils/persian.service'
import RatingStars from './RatingStars.vue'
import { DIMENSIONS, DIMENSION_LABELS, type Dimension, type ReviewInput } from '../types/reviews.types'

/**
 * Write or edit a review. The overall star is the only required step; the four dimensions sit behind a disclosure
 * because every extra required row measurably costs completion. The overall is never computed from the
 * dimensions — it is the customer's own verdict.
 */
const props = withDefaults(
  defineProps<{
    initial?: { rating?: number; comment?: string | null; dimensions?: Partial<Record<Dimension, number | null>> }
    submitting?: boolean
    submitLabel?: string
    cancellable?: boolean
  }>(),
  { submitting: false, submitLabel: 'ثبت نظر', cancellable: false },
)

const emit = defineEmits<{ submit: [input: ReviewInput]; cancel: [] }>()

const rating = ref<number | null>(props.initial?.rating ?? null)
const comment = ref(props.initial?.comment ?? '')
const dimensions = reactive<Partial<Record<Dimension, number>>>(
  Object.fromEntries(
    Object.entries(props.initial?.dimensions ?? {}).filter(([, v]) => typeof v === 'number'),
  ) as Partial<Record<Dimension, number>>,
)
const showDimensions = ref(Object.keys(dimensions).length > 0)

const trimmed = computed(() => comment.value.trim())
const commentTooShort = computed(() => trimmed.value.length > 0 && trimmed.value.length < 10)
const canSend = computed(() => (rating.value ?? 0) >= 1 && !commentTooShort.value)

function send() {
  if (!canSend.value || rating.value === null) return
  emit('submit', {
    rating: rating.value,
    comment: trimmed.value || undefined,
    dimensions: { ...dimensions },
  })
}
</script>

<style scoped>
.review-form { display: flex; flex-direction: column; gap: 1rem; }
.review-form fieldset { border: 0; padding: 0; margin: 0; }
.review-form legend { font-weight: 600; margin-bottom: 0.5rem; }
.review-form__toggle {
  align-self: flex-start;
  background: none;
  border: 0;
  color: var(--color-primary, #6c5ce7);
  cursor: pointer;
  padding: 0;
  font-weight: 500;
}
.review-form__dimensions { display: grid; gap: 0.5rem; }
.review-form__dimension { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
.review-form__comment { display: flex; flex-direction: column; gap: 0.35rem; }
.review-form__comment textarea {
  resize: vertical;
  border: 1px solid var(--color-border, #d9d9d9);
  border-radius: 0.5rem;
  padding: 0.6rem;
  font: inherit;
}
.review-form__comment small { color: var(--color-text-secondary, #777); }
.review-form__comment small.is-error { color: var(--color-danger, #d63031); }
.review-form__note { color: var(--color-text-secondary, #777); font-size: 0.85rem; margin: 0; }
.review-form__actions { display: flex; gap: 0.5rem; }
</style>
