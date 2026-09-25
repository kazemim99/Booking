<template>
  <BaseModal :is-open="isOpen" :title="reviewId ? 'ویرایش نظر' : 'نظر شما دربارهٔ این مراجعه'" size="md" @close="emit('close')">
    <p v-if="subject" class="write-review__subject" data-test="review-subject">{{ subject }}</p>
    <template v-if="!reviewId">
      <ReviewForm :submitting="submitting" cancellable @submit="send" @cancel="emit('close')" />
    </template>
    <p v-else-if="loading" class="write-review__status" data-test="review-loading">در حال بارگذاری نظر شما...</p>
    <p v-else-if="!existing" class="write-review__status" data-test="review-missing">
      این نظر پیدا نشد یا دیگر قابل ویرایش نیست.
    </p>
    <ReviewForm
      v-else
      :initial="{
        rating: existing.rating,
        comment: existing.comment ?? '',
        dimensions: existing.dimensions,
        showName: existing.showName,
      }"
      :submitting="submitting"
      submit-label="ذخیره تغییرات"
      cancellable
      @submit="send"
      @cancel="emit('close')"
    />
  </BaseModal>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useNotification } from '@/core/composables/useNotification'
import { reviewsApi } from '../api/reviews.api'
import type { MyReview, ReviewInput } from '../types/reviews.types'
import ReviewForm from './ReviewForm.vue'

/**
 * Writing a review for a visit — or, given `reviewId`, editing the customer's review of that salon from the booking
 * card (reviews-and-reschedule-round2: one review per salon, editable while the server says so). The edit opens with
 * the review as it stands, read from «نظرهای من». A refusal keeps the form open with what was typed; its reason is
 * said by the API client's error toast, in the server's own Persian words.
 */
const props = defineProps<{ isOpen: boolean; bookingId: string; subject?: string; reviewId?: string | null }>()
const emit = defineEmits<{ close: []; saved: [reviewId: string] }>()

const { showSuccess } = useNotification()
const submitting = ref(false)
const loading = ref(false)
const existing = ref<MyReview | null>(null)

onMounted(async () => {
  if (!props.reviewId) return
  loading.value = true
  try {
    existing.value = (await reviewsApi.mine()).find((r) => r.reviewId === props.reviewId) ?? null
  } catch {
    existing.value = null
  } finally {
    loading.value = false
  }
})

async function send(input: ReviewInput) {
  if (submitting.value) return
  submitting.value = true
  try {
    if (props.reviewId) {
      const saved = await reviewsApi.edit(props.reviewId, input)
      showSuccess('نظر شما ویرایش شد', 'پس از تأیید دوباره برای دیگران نمایش داده می‌شود.')
      emit('saved', saved.reviewId)
    } else {
      const saved = await reviewsApi.submit(props.bookingId, input)
      showSuccess('نظر شما ثبت شد', 'پس از تأیید برای دیگران نمایش داده می‌شود. ممنون از شما!')
      emit('saved', saved.reviewId)
    }
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
  color: var(--color-text-secondary);
  font-size: 0.9rem;
}
.write-review__status {
  margin: 0;
  color: var(--color-text-secondary);
}
</style>
