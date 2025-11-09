<template>
  <div class="my-favorites" dir="rtl">
    <h2>علاقه‌مندی‌های من</h2>
    <div v-if="favorites.length > 0" class="favorites-grid">
      <div v-for="item in favorites" :key="item.id" class="favorite-card">
        <button @click="removeFavorite(item.id)" class="btn-remove">❌</button>
        <div class="favorite-image">
          <img :src="item.image || '/placeholder.jpg'" :alt="item.name" />
        </div>
        <h3>{{ item.name }}</h3>
        <p class="favorite-type">{{ item.type }}</p>
        <div class="favorite-rating">⭐ {{ item.rating }}</div>
        <button @click="viewDetails(item)" class="btn-view">مشاهده</button>
      </div>
    </div>
    <div v-else class="empty-state">
      <p>شما هنوز هیچ علاقه‌مندی‌ای اضافه نکرده‌اید</p>
      <button @click="$router.push('/customer/browse')" class="btn-browse">
        مرور خدمات
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

const favorites = ref([
  { id: '1', name: 'آرایشگاه زیبا', type: 'ارائه‌دهنده', rating: 4.8, image: null },
  { id: '2', name: 'کوتاهی مو', type: 'خدمت', rating: 4.5, image: null },
])

function removeFavorite(id: string) {
  favorites.value = favorites.value.filter(f => f.id !== id)
}

function viewDetails(item: any) {
  if (item.type === 'ارائه‌دهنده') {
    router.push(`/customer/provider/${item.id}`)
  }
}
</script>

<style scoped>
.favorites-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 1.5rem;
  margin-top: 2rem;
}

.favorite-card {
  background: white;
  padding: 1.5rem;
  border-radius: 0.75rem;
  text-align: center;
  position: relative;
}

.btn-remove {
  position: absolute;
  top: 0.5rem;
  left: 0.5rem;
  background: none;
  border: none;
  cursor: pointer;
}

.favorite-image {
  width: 100px;
  height: 100px;
  margin: 0 auto 1rem;
  border-radius: 50%;
  overflow: hidden;
}

.favorite-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.empty-state {
  text-align: center;
  padding: 4rem;
}

.btn-browse {
  padding: 0.75rem 1.5rem;
  background: var(--color-primary);
  color: white;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
  margin-top: 1rem;
}
</style>
