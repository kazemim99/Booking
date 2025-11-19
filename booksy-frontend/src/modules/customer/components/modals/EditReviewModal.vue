<template>
  <BaseModal :is-open="isOpen" @close="handleClose" title="ویرایش نظر" size="md">
    <form @submit.prevent="handleSubmit" class="edit-review-form">
      <!-- Rating -->
      <div class="form-group">
        <label class="form-label">امتیاز شما *</label>
        <div class="star-rating">
          <button
            v-for="star in 5"
            :key="star"
            type="button"
            @click="rating = star"
            @mouseenter="hoverRating = star"
            @mouseleave="hoverRating = 0"
            class="star-button"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              :fill="star <= (hoverRating || rating) ? 'currentColor' : 'none'"
              viewBox="0 0 24 24"
              stroke="currentColor"
              class="star"
              :class="{ filled: star <= (hoverRating || rating) }"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
            </svg>
          </button>
        </div>
        <span v-if="errors.rating" class="error-message">{{ errors.rating }}</span>
      </div>

      <!-- Review Text -->
      <div class="form-group">
        <label for="reviewText" class="form-label">
          نظر شما (اختیاری)
          <span class="char-count">{{ text.length }} / 500</span>
        </label>
        <textarea
          id="reviewText"
          v-model="text"
          class="form-textarea"
          :class="{ error: errors.text }"
          placeholder="تجربه خود را با ما به اشتراک بگذارید..."
          rows="5"
          maxlength="500"
        />
        <span v-if="errors.text" class="error-message">{{ errors.text }}</span>
      </div>

      <!-- Buttons -->
      <div class="form-actions">
        <button type="button" @click="handleClose" class="btn btn-secondary">
          لغو
        </button>
        <button type="submit" class="btn btn-primary" :disabled="!isFormValid">
          ذخیره تغییرات
        </button>
      </div>
    </form>
  </BaseModal>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import BaseModal from '@/shared/components/ui/BaseModal.vue'
import type { CustomerReview, UpdateReviewRequest } from '../../types/customer.types'

interface Props {
  isOpen: boolean
  review: CustomerReview
}

const props = defineProps<Props>()
const emit = defineEmits<{
  close: []
  save: [reviewId: string, data: UpdateReviewRequest]
}>()

const rating = ref(0)
const text = ref('')
const hoverRating = ref(0)

const errors = ref({
  rating: '',
  text: ''
})

// Initialize form when modal opens
watch(() => props.isOpen, (isOpen) => {
  if (isOpen && props.review) {
    rating.value = props.review.rating
    text.value = props.review.text || ''
    clearErrors()
  }
}, { immediate: true })

const isFormValid = computed(() => {
  return rating.value > 0 && !errors.value.rating && !errors.value.text
})

function clearErrors(): void {
  errors.value.rating = ''
  errors.value.text = ''
}

function validateRating(): boolean {
  if (rating.value < 1 || rating.value > 5) {
    errors.value.rating = 'لطفاً امتیاز خود را انتخاب کنید'
    return false
  }
  errors.value.rating = ''
  return true
}

function validateText(): boolean {
  if (text.value.length > 500) {
    errors.value.text = 'نظر شما نباید بیشتر از 500 کاراکتر باشد'
    return false
  }
  errors.value.text = ''
  return true
}

function handleSubmit(): void {
  clearErrors()

  if (!validateRating() || !validateText()) {
    return
  }

  const data: UpdateReviewRequest = {
    rating: rating.value,
    text: text.value.trim() || undefined
  }

  emit('save', props.review.id, data)
}

function handleClose(): void {
  clearErrors()
  emit('close')
}
</script>

<style scoped lang="scss">
.edit-review-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-label {
  font-weight: 500;
  color: #374151;
  font-size: 0.875rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.char-count {
  font-size: 0.75rem;
  color: #9ca3af;
  font-weight: 400;
}

.star-rating {
  display: flex;
  gap: 0.5rem;
}

.star-button {
  background: none;
  border: none;
  cursor: pointer;
  padding: 0;
  transition: transform 0.2s;

  &:hover {
    transform: scale(1.1);
  }
}

.star {
  width: 32px;
  height: 32px;
  color: #d1d5db;
  transition: color 0.2s;

  &.filled {
    color: #f59e0b;
  }
}

.form-textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 1rem;
  font-family: inherit;
  resize: vertical;
  transition: border-color 0.2s, box-shadow 0.2s;

  &:focus {
    outline: none;
    border-color: #667eea;
    box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
  }

  &.error {
    border-color: #ef4444;

    &:focus {
      box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
    }
  }
}

.error-message {
  font-size: 0.75rem;
  color: #ef4444;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  margin-top: 1rem;
}

.btn {
  padding: 0.75rem 1.5rem;
  border-radius: 8px;
  font-weight: 500;
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s;
  border: none;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

.btn-secondary {
  background-color: white;
  color: #374151;
  border: 1px solid #d1d5db;

  &:hover:not(:disabled) {
    background-color: #f9fafb;
  }
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;

  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
  }
}
</style>
