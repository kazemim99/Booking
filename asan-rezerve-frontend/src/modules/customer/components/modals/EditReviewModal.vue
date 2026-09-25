<template>
  <BaseModal :is-open="isOpen" @close="emit('close')" title="ویرایش نظر" size="md">
    <ReviewForm
      :initial="{ rating: review.rating, comment: review.text ?? '', dimensions: review.dimensions, showName: review.showName }"
      submit-label="ذخیره تغییرات"
      cancellable
      @submit="save"
      @cancel="emit('close')"
    />
  </BaseModal>
</template>

<script setup lang="ts">
import ReviewForm from '@/modules/reviews/components/ReviewForm.vue'
import type { ReviewInput } from '@/modules/reviews/types/reviews.types'
import type { CustomerReview, UpdateReviewRequest } from '../../types/customer.types'

/**
 * Edit one of the customer's reviews, with the same form used to write one — the four aspects (the overall derived
 * from them), the name choice, and a 10–2000 character comment. (This modal used to carry its own form with a 500-character limit and no dimensions,
 * contradicting the rules the backend enforces.) Saving returns the review to moderation; the form says so.
 */
const props = defineProps<{ isOpen: boolean; review: CustomerReview }>()
const emit = defineEmits<{ close: []; save: [reviewId: string, data: UpdateReviewRequest] }>()

function save(input: ReviewInput) {
  emit('save', props.review.id, {
    rating: input.rating,
    text: input.comment,
    dimensions: input.dimensions,
    showName: input.showName,
  })
}
</script>
