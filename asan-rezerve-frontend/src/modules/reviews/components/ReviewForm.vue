<template>
  <form class="review-form" dir="rtl" @submit.prevent="send">
    <fieldset class="review-form__aspects">
      <legend>به هر مورد امتیاز دهید</legend>
      <div v-for="d in DIMENSIONS" :key="d" class="review-form__dimension">
        <span>{{ DIMENSION_LABELS[d] }}</span>
        <RatingStars
          :model-value="dimensions[d] ?? null"
          :label="DIMENSION_LABELS[d]"
          :data-test="`dimension-${d}`"
          @update:model-value="(v: number) => (dimensions[d] = v)"
        />
      </div>
      <p v-if="overall !== null" class="review-form__overall" aria-live="polite" data-test="overall-live">
        امتیاز کلی: {{ toPersianDigits(overall.toFixed(1)) }}
      </p>
    </fieldset>

    <label class="review-form__comment">
      <span>نظر شما (اختیاری)</span>
      <textarea v-model="comment" rows="4" maxlength="2000" data-test="comment" />
      <small :class="{ 'is-error': commentTooShort }" data-test="comment-hint">
        {{ commentTooShort ? `نظر باید دست‌کم ${toPersianDigits(10)} نویسه باشد` : `${toPersianDigits(trimmed.length)} / ${toPersianDigits(2000)}` }}
      </small>
    </label>

    <label class="review-form__hide-name" data-test="hide-name">
      <input v-model="hideName" type="checkbox" />
      <span>نامم در نظر نمایش داده نشود</span>
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
import {
  DIMENSIONS,
  DIMENSION_LABELS,
  overallFromAspects,
  type Dimension,
  type ReviewInput,
} from '../types/reviews.types'

/**
 * Write or edit a review (reviews-and-reschedule-round2, D1). The customer rates the four aspects — all required —
 * and the overall is their average to the nearest half star, shown live as «امتیاز کلی: ۴.۵» and sent with them.
 * A review written before D1 may carry only an overall: its missing aspects open at that overall, rounded, so the
 * edit starts where the customer left it.
 */
const props = withDefaults(
  defineProps<{
    initial?: {
      rating?: number
      comment?: string | null
      dimensions?: Partial<Record<Dimension, number | null>>
      showName?: boolean
    }
    submitting?: boolean
    submitLabel?: string
    cancellable?: boolean
  }>(),
  { submitting: false, submitLabel: 'ثبت نظر', cancellable: false },
)

const emit = defineEmits<{ submit: [input: ReviewInput]; cancel: [] }>()

function initialAspects(): Partial<Record<Dimension, number>> {
  const given = props.initial?.dimensions ?? {}
  const fallback = typeof props.initial?.rating === 'number' && props.initial.rating >= 1
    ? Math.min(5, Math.round(props.initial.rating))
    : undefined
  const out: Partial<Record<Dimension, number>> = {}
  for (const d of DIMENSIONS) {
    const value = given[d]
    if (typeof value === 'number') out[d] = value
    else if (fallback !== undefined) out[d] = fallback
  }
  return out
}

const dimensions = reactive<Partial<Record<Dimension, number>>>(initialAspects())
const comment = ref(props.initial?.comment ?? '')
/** Ticked = the public review reads «مشتری». Unticked unless the author chose it before. */
const hideName = ref(props.initial?.showName === false)

const overall = computed(() => overallFromAspects(dimensions))
const trimmed = computed(() => comment.value.trim())
const commentTooShort = computed(() => trimmed.value.length > 0 && trimmed.value.length < 10)
const canSend = computed(() => overall.value !== null && !commentTooShort.value)

function send() {
  if (!canSend.value || overall.value === null) return
  emit('submit', {
    rating: overall.value,
    comment: trimmed.value || undefined,
    dimensions: { ...dimensions },
    showName: !hideName.value,
  })
}
</script>

<style scoped>
.review-form { display: flex; flex-direction: column; gap: 1rem; }
.review-form fieldset { border: 0; padding: 0; margin: 0; }
.review-form legend { font-weight: 600; margin-bottom: 0.5rem; }
.review-form__aspects { display: grid; gap: 0.5rem; }
.review-form__overall {
  margin: 0.25rem 0 0;
  font-weight: 700;
  color: var(--color-primary, var(--color-primary-500));
}
.review-form__hide-name { display: inline-flex; align-items: center; gap: 0.5rem; cursor: pointer; font-size: 0.9rem; }
.review-form__hide-name input { accent-color: var(--color-primary, var(--color-primary-500)); }
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
