<template>
  <div class="favorite-card">
    <!-- Heart Button (Remove) -->
    <button @click="handleRemove" class="heart-button filled" aria-label="حذف از علاقه‌مندی‌ها">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
        <path d="M11.645 20.91l-.007-.003-.022-.012a15.247 15.247 0 01-.383-.218 25.18 25.18 0 01-4.244-3.17C4.688 15.36 2.25 12.174 2.25 8.25 2.25 5.322 4.714 3 7.688 3A5.5 5.5 0 0112 5.052 5.5 5.5 0 0116.313 3c2.973 0 5.437 2.322 5.437 5.25 0 3.925-2.438 7.111-4.739 9.256a25.175 25.175 0 01-4.244 3.17 15.247 15.247 0 01-.383.219l-.022.012-.007.004-.003.001a.752.752 0 01-.704 0l-.003-.001z" />
      </svg>
    </button>

    <!-- Provider Logo -->
    <img
      v-if="favorite.providerLogoUrl"
      :src="favorite.providerLogoUrl"
      :alt="favorite.providerName"
      class="provider-logo"
    />
    <div v-else class="provider-logo-placeholder">
      {{ favorite.providerName.charAt(0) }}
    </div>

    <!-- Provider Info -->
    <div class="provider-info">
      <h3 class="provider-name">{{ favorite.providerName }}</h3>
      <p v-if="favorite.providerCategory" class="provider-category">{{ favorite.providerCategory }}</p>

      <!-- Rating -->
      <div v-if="favorite.providerRating" class="rating">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="star-icon">
          <path fill-rule="evenodd" d="M10.788 3.21c.448-1.077 1.976-1.077 2.424 0l2.082 5.007 5.404.433c1.164.093 1.636 1.545.749 2.305l-4.117 3.527 1.257 5.273c.271 1.136-.964 2.033-1.96 1.425L12 18.354 7.373 21.18c-.996.608-2.231-.29-1.96-1.425l1.257-5.273-4.117-3.527c-.887-.76-.415-2.212.749-2.305l5.404-.433 2.082-5.006z" clip-rule="evenodd" />
        </svg>
        <span class="rating-value">{{ formatRating(favorite.providerRating) }}</span>
      </div>
    </div>

    <!-- Notes (if any) -->
    <p v-if="favorite.notes" class="notes">{{ favorite.notes }}</p>

    <!-- Quick Book Button -->
    <button @click="handleQuickBook" class="quick-book-button">
      <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
      </svg>
      <span>رزرو سریع</span>
    </button>
  </div>
</template>

<script setup lang="ts">
import type { FavoriteProvider } from '../../types/favorites.types'

interface Props {
  favorite: FavoriteProvider
}

const props = defineProps<Props>()

const emit = defineEmits<{
  remove: [providerId: string, providerName: string]
  'quick-book': [providerId: string, providerName: string]
}>()

function formatRating(rating: number): string {
  return rating.toFixed(1)
}

function handleRemove(): void {
  emit('remove', props.favorite.providerId, props.favorite.providerName)
}

function handleQuickBook(): void {
  emit('quick-book', props.favorite.providerId, props.favorite.providerName)
}
</script>

<style scoped lang="scss">
.favorite-card {
  position: relative;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 1rem;
  transition: all 0.2s;

  &:hover {
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
    transform: translateY(-2px);
  }
}

.heart-button {
  position: absolute;
  top: 0.75rem;
  right: 0.75rem;
  background: white;
  border: none;
  border-radius: 50%;
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  z-index: 10;

  svg {
    width: 20px;
    height: 20px;
  }

  &.filled {
    color: #ef4444;

    &:hover {
      background: #fef2f2;
      transform: scale(1.1);
    }
  }
}

.provider-logo {
  width: 80px;
  height: 80px;
  border-radius: 12px;
  object-fit: cover;
  border: 2px solid #e5e7eb;
}

.provider-logo-placeholder {
  width: 80px;
  height: 80px;
  border-radius: 12px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 2rem;
  border: 2px solid #e5e7eb;
}

.provider-info {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  width: 100%;
}

.provider-name {
  font-size: 1.125rem;
  font-weight: 600;
  color: #111827;
  margin: 0;
}

.provider-category {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

.rating {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  color: #f59e0b;
}

.star-icon {
  width: 16px;
  height: 16px;
}

.rating-value {
  font-size: 0.875rem;
  font-weight: 600;
  color: #374151;
}

.notes {
  font-size: 0.875rem;
  color: #6b7280;
  line-height: 1.5;
  margin: 0;
  width: 100%;
  text-align: right;
  padding: 0.75rem;
  background: #f9fafb;
  border-radius: 8px;
}

.quick-book-button {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 8px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;

  svg {
    width: 18px;
    height: 18px;
  }

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
  }
}
</style>
