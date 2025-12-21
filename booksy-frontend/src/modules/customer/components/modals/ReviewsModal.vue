<template>
  <ResponsiveModal :is-open="isOpen" @close="handleClose" title="نظرات من" size="lg" mobile-height="full">
    <div class="reviews-content">
      <!-- Loading State -->
      <div v-if="loading" class="loading-state">
        <div class="spinner"></div>
        <p>در حال بارگذاری...</p>
      </div>

      <!-- Empty State -->
      <div v-else-if="reviews.length === 0" class="empty-state">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="empty-icon">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
        </svg>
        <p class="empty-title">شما هنوز نظری ثبت نکرده‌اید</p>
        <p class="empty-description">پس از دریافت خدمات، می‌توانید نظر خود را درباره ارائه‌دهندگان ثبت کنید</p>
      </div>

      <!-- Reviews List -->
      <div v-else class="reviews-list">
        <ReviewCard
          v-for="review in reviews"
          :key="review.id"
          :review="review"
          @edit="handleEditReview"
        />
      </div>
    </div>

    <!-- Edit Review Modal (nested) -->
    <teleport to="body">
      <EditReviewModal
        v-if="editingReview"
        :is-open="!!editingReview"
        :review="editingReview"
        @close="editingReview = null"
        @save="handleSaveReview"
      />
    </teleport>
  </ResponsiveModal>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useCustomerStore } from '../../stores/customer.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useNotification } from '@/core/composables/useNotification'
import ResponsiveModal from '@/shared/components/ui/ResponsiveModal.vue'
import ReviewCard from './ReviewCard.vue'
import EditReviewModal from './EditReviewModal.vue'
import type { CustomerReview, UpdateReviewRequest } from '../../types/customer.types'

interface Props {
  isOpen: boolean
}

const props = defineProps<Props>()
const emit = defineEmits<{
  close: []
}>()

const customerStore = useCustomerStore()
const authStore = useAuthStore()
const { showSuccess, showError } = useNotification()

const reviews = computed(() => customerStore.reviews)
const loading = computed(() => customerStore.loading.reviews)

const editingReview = ref<CustomerReview | null>(null)

// Fetch reviews when modal opens
watch(() => props.isOpen, async (isOpen) => {
  if (isOpen && authStore.user?.id) {
    try {
      await customerStore.fetchReviews(authStore.user.id)
    } catch (error) {
      console.error('[ReviewsModal] Error fetching reviews:', error)
      showError('خطا', 'خطا در بارگذاری نظرات')
    }
  }
}, { immediate: true })

function handleEditReview(review: CustomerReview): void {
  if (!review.canEdit) {
    showError('خطا', 'فقط می‌توانید نظرات خود را تا ۷ روز بعد از ثبت ویرایش کنید')
    return
  }

  editingReview.value = review
}

async function handleSaveReview(reviewId: string, data: UpdateReviewRequest): Promise<void> {
  if (!authStore.user?.id) return

  try {
    await customerStore.updateReview(authStore.user.id, reviewId, data)
    showSuccess('موفقیت', 'نظر شما با موفقیت بهروزرسانی شد')
    editingReview.value = null
  } catch (error) {
    console.error('[ReviewsModal] Error updating review:', error)
    showError('خطا', 'خطا در بهروزرسانی نظر')
  }
}

function handleClose(): void {
  emit('close')
}
</script>

<style scoped lang="scss">
.reviews-content {
  min-height: 300px;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem 1rem;
  color: #6b7280;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 3px solid #e5e7eb;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-bottom: 1rem;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem 1rem;
  text-align: center;
}

.empty-icon {
  width: 64px;
  height: 64px;
  color: #d1d5db;
  margin-bottom: 1rem;
}

.empty-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: #374151;
  margin-bottom: 0.5rem;
}

.empty-description {
  font-size: 0.875rem;
  color: #6b7280;
  max-width: 400px;
}

.reviews-list {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  padding: 1rem 0;
}
</style>
