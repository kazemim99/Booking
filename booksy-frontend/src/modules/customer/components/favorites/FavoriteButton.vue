<template>
  <button
    @click="handleToggle"
    :disabled="loading"
    :class="['favorite-btn', { active: isFavorite, loading }]"
    :title="isFavorite ? 'حذف از علاقه‌مندی‌ها' : 'افزودن به علاقه‌مندی‌ها'"
  >
    <!-- Heart Icon -->
    <svg v-if="!loading" viewBox="0 0 20 20" :fill="isFavorite ? 'currentColor' : 'none'" stroke="currentColor">
      <path
        fill-rule="evenodd"
        d="M3.172 5.172a4 4 0 015.656 0L10 6.343l1.172-1.171a4 4 0 115.656 5.656L10 17.657l-6.828-6.829a4 4 0 010-5.656z"
        clip-rule="evenodd"
      />
    </svg>

    <!-- Loading Spinner -->
    <div v-else class="spinner"></div>

    <!-- Label (optional) -->
    <span v-if="showLabel" class="label">
      {{ isFavorite ? 'علاقه‌مندی' : 'افزودن' }}
    </span>
  </button>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { favoritesService } from '../../services/favorites.service'

// ============================================================================
// Props & Emits
// ============================================================================

interface Props {
  providerId: string
  showLabel?: boolean
  size?: 'sm' | 'md' | 'lg'
}

const props = withDefaults(defineProps<Props>(), {
  showLabel: false,
  size: 'md',
})

const emit = defineEmits<{
  (e: 'favorited', providerId: string): void
  (e: 'unfavorited', providerId: string): void
  (e: 'error', error: Error): void
}>()

// ============================================================================
// Store & State
// ============================================================================

const authStore = useAuthStore()
const customerId = computed(() => authStore.user?.id || '')

const loading = ref(false)
const isFavorite = ref(false)

// ============================================================================
// Lifecycle
// ============================================================================

onMounted(() => {
  checkFavoriteStatus()
})

// ============================================================================
// Methods
// ============================================================================

function checkFavoriteStatus(): void {
  isFavorite.value = favoritesService.isFavorite(props.providerId)
}

async function handleToggle(): Promise<void> {
  if (!customerId.value) {
    console.warn('[FavoriteButton] No customer ID available')
    return
  }

  loading.value = true

  try {
    const result = await favoritesService.toggleFavorite(customerId.value, props.providerId)

    isFavorite.value = result.isFavorite

    if (result.isFavorite) {
      emit('favorited', props.providerId)
    } else {
      emit('unfavorited', props.providerId)
    }
  } catch (error) {
    console.error('[FavoriteButton] Error toggling favorite:', error)
    emit('error', error as Error)
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.favorite-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.375rem;
  padding: 0.5rem;
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.2s;
  color: #718096;
}

.favorite-btn:hover:not(:disabled) {
  background: #f7fafc;
  border-color: #cbd5e0;
  color: #e53e3e;
}

.favorite-btn.active {
  color: #e53e3e;
  border-color: #e53e3e;
  background: #fff5f5;
}

.favorite-btn.active:hover {
  background: #fed7d7;
}

.favorite-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.favorite-btn svg {
  width: 1.25rem;
  height: 1.25rem;
  stroke-width: 2;
}

.spinner {
  width: 1.25rem;
  height: 1.25rem;
  border: 2px solid #e2e8f0;
  border-top-color: #e53e3e;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.label {
  font-size: 0.875rem;
  font-weight: 500;
}
</style>
