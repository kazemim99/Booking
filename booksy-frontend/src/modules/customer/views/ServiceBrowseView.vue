<template>
  <div class="service-browse" dir="rtl">
    <div class="search-header">
      <h2>جستجوی خدمات</h2>
      <div class="search-box">
        <input
          v-model="searchQuery"
          type="text"
          placeholder="جستجوی خدمات..."
          class="search-input"
        />
        <button class="search-btn">🔍</button>
      </div>
    </div>

    <div class="browse-content">
      <aside class="filters-sidebar">
        <h3>فیلترها</h3>
        <div class="filter-section">
          <h4>دسته‌بندی</h4>
          <label v-for="cat in categories" :key="cat.id">
            <input type="checkbox" :value="cat.id" v-model="selectedCategories" />
            {{ cat.name }}
          </label>
        </div>
        <div class="filter-section">
          <h4>محدوده قیمت</h4>
          <input v-model="priceMin" type="number" placeholder="از" />
          <input v-model="priceMax" type="number" placeholder="تا" />
        </div>
      </aside>

      <div class="services-grid">
        <div v-for="service in filteredServices" :key="service.id" class="service-card">
          <div class="service-image">
            <img :src="service.image || '/placeholder.jpg'" :alt="service.name" />
          </div>
          <div class="service-info">
            <h3>{{ service.name }}</h3>
            <p class="provider">{{ service.providerName }}</p>
            <p class="description">{{ service.description }}</p>
            <div class="service-footer">
              <span class="price">{{ formatPrice(service.price) }} تومان</span>
              <button @click="bookService(service)" class="btn-book">رزرو</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const searchQuery = ref('')
const selectedCategories = ref<string[]>([])
const priceMin = ref<number>()
const priceMax = ref<number>()

const categories = ref([
  { id: '1', name: 'آرایشگاه' },
  { id: '2', name: 'ماساژ' },
  { id: '3', name: 'پوست و مو' },
])

const services = ref([
  {
    id: '1',
    name: 'کوتاهی مو',
    description: 'کوتاهی و اصلاح مو توسط آرایشگر حرفه‌ای',
    price: 150000,
    providerName: 'آرایشگاه زیبا',
    categoryId: '1',
    image: null,
  },
  {
    id: '2',
    name: 'ماساژ سوئدی',
    description: 'ماساژ آرامش‌بخش بدن',
    price: 300000,
    providerName: 'اسپا رویا',
    categoryId: '2',
    image: null,
  },
])

const filteredServices = computed(() => {
  return services.value.filter(service => {
    if (searchQuery.value && !service.name.includes(searchQuery.value)) return false
    if (selectedCategories.value.length > 0 && !selectedCategories.value.includes(service.categoryId)) return false
    if (priceMin.value && service.price < priceMin.value) return false
    if (priceMax.value && service.price > priceMax.value) return false
    return true
  })
})

function formatPrice(price: number) {
  return new Intl.NumberFormat('fa-IR').format(price)
}

function bookService(service: any) {
  router.push({ name: 'NewBooking', query: { serviceId: service.id } })
}
</script>

<style scoped>
.service-browse {
  max-width: 1400px;
  margin: 0 auto;
}

.search-header {
  margin-bottom: 2rem;
}

.search-box {
  display: flex;
  gap: 0.5rem;
  margin-top: 1rem;
  max-width: 500px;
}

.search-input {
  flex: 1;
  padding: 0.75rem;
  border: 1px solid var(--color-gray-300);
  border-radius: 0.5rem;
}

.search-btn {
  padding: 0.75rem 1.5rem;
  background: var(--color-primary);
  color: white;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
}

.browse-content {
  display: grid;
  grid-template-columns: 250px 1fr;
  gap: 2rem;
}

.filters-sidebar {
  background: white;
  padding: 1.5rem;
  border-radius: 0.75rem;
  height: fit-content;
}

.filter-section {
  margin-bottom: 1.5rem;
}

.filter-section h4 {
  margin-bottom: 0.75rem;
}

.filter-section label {
  display: block;
  margin-bottom: 0.5rem;
}

.services-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
}

.service-card {
  background: white;
  border-radius: 0.75rem;
  overflow: hidden;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.service-image {
  height: 200px;
  background: var(--color-gray-200);
}

.service-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.service-info {
  padding: 1rem;
}

.service-info h3 {
  margin: 0 0 0.5rem;
}

.provider {
  color: var(--color-gray-600);
  font-size: 0.875rem;
  margin: 0 0 0.5rem;
}

.description {
  color: var(--color-gray-700);
  margin: 0 0 1rem;
}

.service-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.price {
  font-weight: 600;
  color: var(--color-primary);
}

.btn-book {
  padding: 0.5rem 1rem;
  background: var(--color-primary);
  color: white;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
}
</style>
