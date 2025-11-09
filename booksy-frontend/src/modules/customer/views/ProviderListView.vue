<template>
  <div class="provider-list" dir="rtl">
    <h2>ارائه‌دهندگان</h2>
    <div class="providers-grid">
      <div v-for="provider in providers" :key="provider.id" class="provider-card">
        <div class="card-content" @click="viewProvider(provider.id)">
          <div class="provider-image">
            <img :src="provider.logo || '/placeholder.jpg'" :alt="provider.name" />
          </div>
          <h3>{{ provider.name }}</h3>
          <p class="provider-type">{{ provider.type }}</p>
          <div class="provider-rating">⭐ {{ provider.rating }}</div>
        </div>
        <div class="card-actions" @click.stop>
          <FavoriteButton
            :provider-id="provider.id"
            @favorited="handleFavorited"
            @unfavorited="handleUnfavorited"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import FavoriteButton from '../components/favorites/FavoriteButton.vue'

const router = useRouter()
const providers = ref([
  { id: '1', name: 'آرایشگاه زیبا', type: 'آرایشگاه', rating: 4.8, logo: null },
  { id: '2', name: 'اسپا رویا', type: 'اسپا', rating: 4.5, logo: null },
])

function viewProvider(id: string) {
  router.push(`/customer/provider/${id}`)
}

function handleFavorited(providerId: string): void {
  console.log('[ProviderListView] Provider favorited:', providerId)
}

function handleUnfavorited(providerId: string): void {
  console.log('[ProviderListView] Provider unfavorited:', providerId)
}
</script>

<style scoped>
.providers-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 1.5rem;
}

.provider-card {
  background: white;
  padding: 1.5rem;
  border-radius: 0.75rem;
  text-align: center;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  transition: box-shadow 0.2s;
}

.provider-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.card-content {
  cursor: pointer;
  flex: 1;
}

.provider-image {
  width: 100px;
  height: 100px;
  margin: 0 auto 1rem;
  border-radius: 50%;
  overflow: hidden;
  background: #f7fafc;
}

.provider-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.provider-card h3 {
  margin: 0.5rem 0;
  color: #2d3748;
  font-size: 1.125rem;
}

.provider-type {
  color: #718096;
  font-size: 0.875rem;
  margin: 0.25rem 0;
}

.provider-rating {
  color: #f59e0b;
  font-weight: 600;
  margin-top: 0.5rem;
}

.card-actions {
  display: flex;
  justify-content: center;
  padding-top: 0.75rem;
  border-top: 1px solid #e2e8f0;
}
</style>
