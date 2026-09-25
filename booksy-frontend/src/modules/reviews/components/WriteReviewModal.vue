<template>
  <BaseModal :is-open="isOpen" title="نظر شما دربارهٔ این مراجعه" size="md" @close="emit('close')">
    <p v-if="subject" class="write-review__subject" data-test="review-subject">{{ subject }}</p>
    <ReviewForm :submitting="submitting" cancellable @submit="send" @cancel="emit('close')" />
  </BaseModal>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useNotification } from '@/core/composables/useNotification'
import { reviewsApi } from '../api/reviews.api'
import type { ReviewInput } from '../types/reviews.types'
import ReviewForm from './ReviewForm.vue'

/**
 * Writing a review for a visit — the web had the form and the endpoint but no way to reach them, so a customer could
 * not leave one at all (openspec/changes/_inline/customer-reviews-and-nahal-seed). A refusal keeps the form open with
 * what was typed; its reason is said by the API client's error toast, in the server's own Persian words.
 */
const props = defineProps<{ isOpen: boolean; bookingId: string; subject?: string }>()
const emit = defineEmits<{ close: []; saved: [reviewId: string] }>()

const { showSuccess } = useNotification()
const submitting = ref(false)

async function send(input: ReviewInput) {
  if (submitting.value) return
  submitting.value = true
  try {
    const saved = await reviewsApi.submit(props.bookingId, input)
    showSuccess('نظر شما ثبت شد', 'پس از تأیید برای دیگران نمایش داده می‌شود. ممنون از شما!')
    emit('saved', saved.reviewId)
  } catch {
    // Said by the client's error toast; the form stays as it was.
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.write-review__subject {
  margin: 0 0 1rem;
  color: var(--color-text-secondary, #6b7280);
  font-size: 0.9rem;
}
</style>
