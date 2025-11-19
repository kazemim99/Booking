<template>
  <div class="review-card">
    <!-- Provider Info -->
    <div class="review-header">
      <img
        v-if="review.providerLogoUrl"
        :src="review.providerLogoUrl"
        :alt="review.providerName"
        class="provider-logo"
      />
      <div v-else class="provider-logo-placeholder">
        {{ review.providerName.charAt(0) }}
      </div>

      <div class="provider-info">
        <h3 class="provider-name">{{ review.providerName }}</h3>
        <p class="service-name">{{ review.serviceName }}</p>
      </div>

      <!-- Edit Button -->
      <button
        v-if="review.canEdit"
        @click="handleEdit"
        class="edit-button"
        title="ویرایش نظر"
      >
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
        </svg>
      </button>
    </div>

    <!-- Star Rating -->
    <div class="rating">
      <svg
        v-for="star in 5"
        :key="star"
        xmlns="http://www.w3.org/2000/svg"
        :fill="star <= review.rating ? 'currentColor' : 'none'"
        viewBox="0 0 24 24"
        stroke="currentColor"
        class="star"
        :class="{ filled: star <= review.rating }"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
      </svg>
    </div>

    <!-- Review Text -->
    <p v-if="review.text" class="review-text">{{ review.text }}</p>

    <!-- Review Date -->
    <div class="review-footer">
      <span class="review-date">{{ formattedDate }}</span>
      <span v-if="review.updatedAt" class="edited-badge">(ویرایش شده)</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { CustomerReview } from '../../types/customer.types'

interface Props {
  review: CustomerReview
}

const props = defineProps<Props>()

const emit = defineEmits<{
  edit: [review: CustomerReview]
}>()

const formattedDate = computed(() => {
  // TODO: Implement Jalali date formatting
  const date = new Date(props.review.createdAt)
  return date.toLocaleDateString('fa-IR', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
})

function handleEdit(): void {
  emit('edit', props.review)
}
</script>

<style scoped lang="scss">
.review-card {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.review-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.provider-logo {
  width: 48px;
  height: 48px;
  border-radius: 8px;
  object-fit: cover;
}

.provider-logo-placeholder {
  width: 48px;
  height: 48px;
  border-radius: 8px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 1.25rem;
}

.provider-info {
  flex: 1;
  min-width: 0;
}

.provider-name {
  font-size: 1rem;
  font-weight: 600;
  color: #111827;
  margin: 0 0 0.25rem 0;
}

.service-name {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

.edit-button {
  background: none;
  border: none;
  cursor: pointer;
  color: #667eea;
  padding: 0.5rem;
  border-radius: 6px;
  transition: all 0.2s;

  svg {
    width: 20px;
    height: 20px;
  }

  &:hover {
    background: #f3f4f6;
  }
}

.rating {
  display: flex;
  gap: 0.25rem;
}

.star {
  width: 20px;
  height: 20px;
  color: #d1d5db;
  transition: color 0.2s;

  &.filled {
    color: #f59e0b;
  }
}

.review-text {
  font-size: 0.875rem;
  line-height: 1.6;
  color: #374151;
  margin: 0;
}

.review-footer {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.75rem;
  color: #9ca3af;
}

.edited-badge {
  font-style: italic;
}
</style>
