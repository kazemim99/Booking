<template>
  <ResponsiveModal
    :is-open="isOpen"
    @close="handleClose"
    title="علاقه‌مندی‌های من"
    size="lg"
    mobile-height="full"
  >
    <div class="favorites-content">
      <!-- Loading State -->
      <div v-if="loading" class="loading-state">
        <div class="spinner"></div>
        <p>در حال بارگذاری...</p>
      </div>

      <!-- Empty State -->
      <div v-else-if="favorites.length === 0" class="empty-state">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="empty-icon">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
        </svg>
        <p class="empty-title">شما هنوز ارائه‌دهنده‌ای را به علاقه‌مندی‌ها اضافه نکرده‌اید</p>
        <p class="empty-description">با افزودن ارائه‌دهندگان مورد علاقه‌تان، می‌توانید به راحتی آن‌ها را پیدا کرده و رزرو کنید</p>
      </div>

      <!-- Favorites Grid -->
      <div v-else class="favorites-grid">
        <FavoriteProviderCard
          v-for="favorite in favorites"
          :key="favorite.id"
          :favorite="favorite"
          @remove="handleRemoveFavorite"
          @quick-book="handleQuickBook"
        />
      </div>
    </div>
  </ResponsiveModal>
</template>

<script setup lang="ts">
import { computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useCustomerStore } from '../../stores/customer.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useToast } from '@/core/composables/useToast'
import ResponsiveModal from '@/shared/components/ui/ResponsiveModal.vue'
import FavoriteProviderCard from './FavoriteProviderCard.vue'

interface Props {
  isOpen: boolean
}

const props = defineProps<Props>()
const emit = defineEmits<{
  close: []
}>()

const router = useRouter()
const customerStore = useCustomerStore()
const authStore = useAuthStore()
const { showSuccess, showError, showConfirm } = useToast()

const favorites = computed(() => customerStore.favorites)
const loading = computed(() => customerStore.loading.favorites)

// Fetch favorites when modal opens
watch(() => props.isOpen, async (isOpen) => {
  if (isOpen && authStore.customerId) {
    try {
      await customerStore.fetchFavorites(authStore.customerId)
    } catch (error) {
      console.error('[FavoritesModal] Error fetching favorites:', error)
      showError('خطا در بارگذاری علاقه‌مندی‌ها')
    }
  }
}, { immediate: true })

async function handleRemoveFavorite(providerId: string, providerName: string): Promise<void> {
  if (!authStore.customerId) return

  const confirmed = await showConfirm(
    `آیا می‌خواهید "${providerName}" را از علاقه‌مندی‌ها حذف کنید؟`,
    'حذف از علاقه‌مندی‌ها'
  )

  if (!confirmed) return

  try {
    await customerStore.removeFavorite(authStore.customerId, providerId)
    showSuccess(`"${providerName}" از علاقه‌مندی‌ها حذف شد`)
  } catch (error) {
    console.error('[FavoritesModal] Error removing favorite:', error)
    showError('خطا در حذف از علاقه‌مندی‌ها')
  }
}

function handleQuickBook(providerId: string, providerName: string): void {
  handleClose()
  router.push(`/booking/${providerId}`)
  showSuccess(`در حال انتقال به صفحه رزرو ${providerName}...`)
}

function handleClose(): void {
  emit('close')
}
</script>

<style scoped lang="scss">
.favorites-content {
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

.favorites-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1.5rem;
  padding: 1rem 0;
}

@media (max-width: 640px) {
  .favorites-grid {
    grid-template-columns: 1fr;
  }
}
</style>
