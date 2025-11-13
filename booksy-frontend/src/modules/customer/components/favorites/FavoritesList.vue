<template>
  <div class="favorites-list" dir="rtl">
    <!-- Header -->
    <div class="list-header">
      <h2>علاقه‌مندی‌های من</h2>
      <p>{{ favorites.length }} ارائه‌دهنده</p>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="loading">
      <div class="spinner"></div>
      <p>در حال بارگذاری...</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="favorites.length === 0" class="empty">
      <svg viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M3.172 5.172a4 4 0 015.656 0L10 6.343l1.172-1.171a4 4 0 115.656 5.656L10 17.657l-6.828-6.829a4 4 0 010-5.656z" clip-rule="evenodd" />
      </svg>
      <p>هنوز هیچ ارائه‌دهنده‌ای را به علاقه‌مندی‌ها اضافه نکرده‌اید</p>
    </div>

    <!-- Favorites Grid -->
    <div v-else class="favorites-grid">
      <div v-for="favorite in favorites" :key="favorite.id" class="favorite-card">
        <div class="card-image">
          <img
            v-if="favorite.provider?.logoUrl"
            :src="favorite.provider.logoUrl"
            :alt="favorite.provider.businessName"
          />
          <div v-else class="placeholder">
            {{ getInitials(favorite.provider?.businessName) }}
          </div>
        </div>

        <div class="card-content">
          <h3>{{ favorite.provider?.businessName || 'نامشخص' }}</h3>
          <div class="rating">
            <span class="stars">★ {{ favorite.provider?.rating?.toFixed(1) || '0.0' }}</span>
            <span class="reviews">({{ favorite.provider?.reviewCount || 0 }} نظر)</span>
          </div>
          <p class="category">{{ getCategoryLabel(favorite.provider?.category || '') }}</p>
          <p v-if="favorite.lastBookedAt" class="last-booked">
            آخرین رزرو: {{ formatTimeAgo(favorite.lastBookedAt) }}
          </p>
        </div>

        <div class="card-actions">
          <button @click="handleBook(favorite)" class="btn-primary">رزرو کنید</button>
          <FavoriteButton :provider-id="favorite.providerId" @unfavorited="handleUnfavorited" />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { favoritesService } from '../../services/favorites.service'
import FavoriteButton from './FavoriteButton.vue'
import type { FavoriteProvider } from '../../types/favorites.types'
import { formatTimeAgo, getCategoryLabel } from '../../types/favorites.types'

const authStore = useAuthStore()
const customerId = computed(() => authStore.user?.id || '')

const loading = ref(false)
const favorites = ref<FavoriteProvider[]>([])

onMounted(() => {
  loadFavorites()
})

async function loadFavorites(): Promise<void> {
  if (!customerId.value) return

  loading.value = true

  try {
    favorites.value = await favoritesService.getFavorites({
      customerId: customerId.value,
      includeProviderDetails: true,
    })
  } catch (error) {
    console.error('[FavoritesList] Error:', error)
  } finally {
    loading.value = false
  }
}

function handleBook(favorite: FavoriteProvider): void {
  // Navigate to booking page
  window.location.href = `/providers/${favorite.providerId}`
}

function handleUnfavorited(): void {
  loadFavorites()
}

function getInitials(name?: string): string {
  if (!name) return '?'
  return name.charAt(0).toUpperCase()
}
</script>

<style scoped>
.favorites-list {
  padding: 1.5rem;
}

.list-header {
  margin-bottom: 2rem;
}

.list-header h2 {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1a202c;
  margin: 0 0 0.5rem 0;
}

.list-header p {
  color: #718096;
  margin: 0;
}

.loading,
.empty {
  text-align: center;
  padding: 4rem 2rem;
  color: #718096;
}

.spinner {
  width: 3rem;
  height: 3rem;
  border: 4px solid #e2e8f0;
  border-top-color: #3182ce;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin: 0 auto 1rem;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.empty svg {
  width: 4rem;
  height: 4rem;
  color: #cbd5e0;
  margin-bottom: 1rem;
}

.favorites-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1.5rem;
}

.favorite-card {
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 0.75rem;
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  transition: box-shadow 0.2s;
}

.favorite-card:hover {
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.card-image {
  width: 4rem;
  height: 4rem;
  border-radius: 0.5rem;
  overflow: hidden;
  background: #f7fafc;
}

.card-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
  font-weight: 600;
  color: #718096;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.card-content h3 {
  font-size: 1.125rem;
  font-weight: 600;
  color: #2d3748;
  margin: 0 0 0.5rem 0;
}

.rating {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.stars {
  color: #f59e0b;
  font-weight: 600;
}

.reviews {
  color: #718096;
  font-size: 0.875rem;
}

.category,
.last-booked {
  font-size: 0.875rem;
  color: #718096;
  margin: 0.25rem 0;
}

.card-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: auto;
}

.btn-primary {
  flex: 1;
  padding: 0.625rem 1rem;
  background: #3182ce;
  color: white;
  border: none;
  border-radius: 0.5rem;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-primary:hover {
  background: #2c5aa0;
}
</style>
