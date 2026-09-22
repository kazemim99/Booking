<template>
  <div
    class="rating-stars"
    :class="[`rating-stars--${size}`, { 'rating-stars--input': !readonly }]"
    :role="readonly ? 'img' : 'radiogroup'"
    :aria-label="readonly ? `${label}: ${toPersianDigits(modelValue ?? 0)} از ۵` : label"
    dir="ltr"
  >
    <template v-if="readonly">
      <span v-for="star in 5" :key="star" class="star" :class="fill(star)" aria-hidden="true">★</span>
    </template>
    <template v-else>
      <button
        v-for="star in 5"
        :key="star"
        type="button"
        class="star"
        :class="fill(star)"
        role="radio"
        :aria-checked="modelValue === star"
        :aria-label="`${toPersianDigits(star)} ستاره`"
        :data-test="`star-${star}`"
        @click="emit('update:modelValue', star)"
      >
        ★
      </button>
    </template>
  </div>
</template>

<script setup lang="ts">
import { toPersianDigits } from '@/core/utils/persian.service'

/**
 * Stars for showing a rating (half stars rendered) or choosing one. Choosing is in whole stars — a valid subset of
 * the API's half-star rule, and far easier to hit on a phone than ten half-width targets.
 */
const props = withDefaults(
  defineProps<{
    modelValue: number | null
    readonly?: boolean
    label?: string
    size?: 'sm' | 'md' | 'lg'
  }>(),
  { readonly: false, label: 'امتیاز', size: 'md' },
)

const emit = defineEmits<{ 'update:modelValue': [value: number] }>()

function fill(star: number): string {
  const value = props.modelValue ?? 0
  if (value >= star) return 'star--full'
  if (value >= star - 0.5) return 'star--half'
  return 'star--empty'
}
</script>

<style scoped>
.rating-stars {
  display: inline-flex;
  gap: 0.125rem;
  color: var(--color-warning, #f5a623);
}
.star {
  background: none;
  border: 0;
  padding: 0;
  line-height: 1;
  color: inherit;
}
.rating-stars--sm .star { font-size: 0.95rem; }
.rating-stars--md .star { font-size: 1.25rem; }
.rating-stars--lg .star { font-size: 1.9rem; }
.rating-stars--input .star { cursor: pointer; padding: 0.15rem; }
.rating-stars--input .star:focus-visible { outline: 2px solid currentColor; border-radius: 4px; }
.star--empty { color: var(--color-border, #d9d9d9); }
.star--half {
  background: linear-gradient(90deg, currentColor 50%, var(--color-border, #d9d9d9) 50%);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}
</style>
