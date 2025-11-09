<template>
  <div class="provider-list" dir="rtl">
    <h2>ارائه‌دهندگان</h2>
    <div class="providers-grid">
      <div v-for="provider in providers" :key="provider.id" class="provider-card" @click="viewProvider(provider.id)">
        <div class="provider-image">
          <img :src="provider.logo || '/placeholder.jpg'" :alt="provider.name" />
        </div>
        <h3>{{ provider.name }}</h3>
        <p class="provider-type">{{ provider.type }}</p>
        <div class="provider-rating">⭐ {{ provider.rating }}</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const providers = ref([
  { id: '1', name: 'آرایشگاه زیبا', type: 'آرایشگاه', rating: 4.8, logo: null },
  { id: '2', name: 'اسپا رویا', type: 'اسپا', rating: 4.5, logo: null },
])

function viewProvider(id: string) {
  router.push(`/customer/provider/${id}`)
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
  cursor: pointer;
  text-align: center;
}

.provider-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.provider-image {
  width: 100px;
  height: 100px;
  margin: 0 auto 1rem;
  border-radius: 50%;
  overflow: hidden;
}

.provider-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
</style>
