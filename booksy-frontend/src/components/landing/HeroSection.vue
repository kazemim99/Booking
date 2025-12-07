<template>
  <section class="hero-section" dir="rtl">
    <div class="hero-background">
      <!-- Background Video -->
      <video
        class="hero-video"
        autoplay
        muted
        loop
        playsinline
        poster="https://images.unsplash.com/photo-1560066984-138dadb4c035?w=1920&q=80"
      >
        <source src="https://cdn.coverr.co/videos/coverr-beauty-salon-reflection-8122/1080p.mp4" type="video/mp4" />
        <!-- Fallback to gradient if video fails to load -->
      </video>
      <div class="hero-overlay"></div>
      <div class="hero-pattern"></div>
    </div>

    <div class="hero-content">
      <div class="hero-text">
        <h1 class="hero-title">
          کشف و رزرو
          <span class="highlight">خدمات زیبایی و سلامت</span>
          در نزدیکی شما
        </h1>
        <p class="hero-subtitle">
          بهترین سالن‌ها، اسپاها و متخصصان سلامتی را پیدا کنید. نوبت خود را در چند کلیک رزرو نمایید.
        </p>
      </div>

      <div class="hero-search">
        <div class="search-card">
          <div class="search-tabs">
            <button
              :class="['tab-btn', { active: searchType === 'service' }]"
              @click="searchType = 'service'"
            >
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              جستجوی خدمات
            </button>
            <button
              :class="['tab-btn', { active: searchType === 'location' }]"
              @click="searchType = 'location'"
            >
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
              </svg>
              نزدیک من
            </button>
          </div>

          <div class="search-inputs">
            <div class="input-group" v-if="searchType === 'service'">
              <SearchableDropdown
                v-model="selectedCategory"
                :options="categories"
                placeholder="به دنبال چه خدمتی هستید؟"
                @select="handleCategorySelect"
              />
            </div>

            <div class="input-group" v-else>
              <svg class="input-icon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              <input
                v-model="locationSearchQuery"
                type="text"
                placeholder="موقعیت مکانی خود را وارد کنید..."
                class="search-input"
                @keydown.enter="handleSearch"
              />
            </div>

            <div class="input-group location-group">
              <SearchableDropdown
                v-model="selectedCity"
                :options="cities"
                :loading="isLoadingCities"
                :min-search-length="2"
                placeholder="شهر"
                @search="searchCities"
                @select="handleCitySelect"
              />
            </div>

            <button class="search-btn" @click="handleSearch">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              جستجو
            </button>
          </div>

          <div class="popular-searches">
            <span class="label">محبوب‌ترین:</span>
            <button
              v-for="tag in popularSearches"
              :key="tag.slug"
              class="tag-btn"
              @click="quickSearch(tag.slug)"
            >
              {{ tag.name }}
            </button>
          </div>
        </div>
      </div>

      <div class="hero-stats">
        <div class="stat-item">
          <div class="stat-value">{{ statsDisplay.providers }}</div>
          <div class="stat-label">ارائه‌دهنده خدمات</div>
        </div>
        <div class="stat-divider"></div>
        <div class="stat-item">
          <div class="stat-value">{{ statsDisplay.customers }}</div>
          <div class="stat-label">مشتری راضی</div>
        </div>
        <div class="stat-divider"></div>
        <div class="stat-item">
          <div class="stat-value">{{ statsDisplay.rating }}</div>
          <div class="stat-label">میانگین امتیاز</div>
        </div>
      </div>

      <!-- Provider CTA Banner -->
      <div class="provider-cta-banner">
        <div class="provider-cta-content">
          <div class="provider-cta-text">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
            </svg>
            <span class="provider-cta-title">صاحب کسب‌وکار هستید؟</span>
            <span class="provider-cta-subtitle">کسب‌وکار خود را رشد دهید</span>
          </div>
          <router-link to="/provider/login" class="provider-cta-button">
            ورود به پنل کسب‌وکار
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
          </router-link>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import SearchableDropdown from '@/shared/components/ui/SearchableDropdown.vue'
import type { DropdownOption } from '@/shared/components/ui/SearchableDropdown.vue'
import { categoryService } from '@/core/api/services/category.service'
import { locationService } from '@/core/api/services/location.service'
import { platformService } from '@/core/api/services/platform.service'
import type { PlatformStatistics } from '@/core/api/services/platform.service'

const router = useRouter()

const searchType = ref<'service' | 'location'>('service')
const selectedCategory = ref<string | number>('')
const selectedCity = ref<string | number>('')
const locationSearchQuery = ref('')

const categories = ref<DropdownOption[]>([])
const cities = ref<DropdownOption[]>([])
const popularSearches = ref<Array<{ name: string; slug: string }>>([])
const isLoadingCities = ref(false)
const platformStats = ref<PlatformStatistics | null>(null)
const isLoadingStats = ref(false)

// Computed statistics with Persian numbers
const statsDisplay = computed(() => {
  if (!platformStats.value) {
    // Fallback to mock data while loading
    return {
      providers: '۱۰,۰۰۰+',
      customers: '۵۰,۰۰۰+',
      rating: '★۴.۸'
    }
  }

  const formatted = platformService.formatForDisplay(platformStats.value)
  return {
    providers: formatted.providers,
    customers: formatted.customers,
    rating: `★${formatted.rating}`
  }
})

// Load categories and platform statistics on mount
onMounted(async () => {
  try {
    // Load platform statistics
    isLoadingStats.value = true
    platformStats.value = await platformService.getStatistics()
    console.log('Platform stats loaded:', platformStats.value)
  } catch (error) {
    console.error('Error loading platform statistics:', error)
    // Stats will fallback to mock data
  } finally {
    isLoadingStats.value = false
  }

  try {
    // Load categories
    const categoriesData = await categoryService.getCategories()
    categories.value = categoriesData.map(cat => ({
      label: cat.name,
      value: cat.slug,
      description: cat.description,
    }))

    // Load popular categories
    const popularData = await categoryService.getPopularCategories(5)
    popularSearches.value = popularData.map(cat => ({
      name: cat.name,
      slug: cat.slug,
    }))
  } catch (error) {
    console.error('Error loading data:', error)
  }
})

// Search cities based on user input (minimum 2 characters)
const searchCities = async (query: string) => {
  if (!query || query.length < 2) {
    cities.value = []
    return
  }

  try {
    isLoadingCities.value = true
    const results = await locationService.searchLocations(query)

    // Filter only cities (not provinces)
    const cityResults = results.filter(loc => loc.type === 'city')

    cities.value = cityResults.map(city => ({
      label: city.name,
      value: city.id,
      description: city.provinceName ? `${city.provinceName}` : undefined,
    }))
  } catch (error) {
    console.error('Error searching cities:', error)
    cities.value = []
  } finally {
    isLoadingCities.value = false
  }
}

const handleSearch = () => {
  const filters: Record<string, string | number> = {
    pageNumber: 1,
    pageSize: 12,
  }

  if (searchType.value === 'service' && selectedCategory.value) {
    filters.serviceCategory = selectedCategory.value
  } else if (searchType.value === 'location' && locationSearchQuery.value) {
    filters.searchTerm = locationSearchQuery.value
  }

  if (selectedCity.value) {
    const city = cities.value.find(c => c.value === selectedCity.value)
    if (city) {
      filters.city = city.label
    }
  }

  // Navigate to provider search with filters
  router.push({
    path: '/providers/search',
    query: filters
  })
}

const quickSearch = (category: string) => {
  router.push({
    path: '/providers/search',
    query: {
      serviceCategory: category,
      pageNumber: 1,
      pageSize: 12,
    }
  })
}

const handleCategorySelect = (option: DropdownOption) => {
  selectedCategory.value = option.value
}

const handleCitySelect = (option: DropdownOption) => {
  selectedCity.value = option.value
}
</script>

<style scoped>
.hero-section {
  position: relative;
  min-height: 700px;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  padding: 4rem 2rem;
}

.hero-background {
  position: absolute;
  inset: 0;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.hero-video {
  position: absolute;
  top: 50%;
  left: 50%;
  min-width: 100%;
  min-height: 100%;
  width: auto;
  height: auto;
  transform: translate(-50%, -50%);
  object-fit: cover;
  z-index: 0;
  opacity: 0.3;
  filter: brightness(1.1) contrast(0.9);
  animation: slowZoom 30s ease-in-out infinite alternate;
}

@keyframes slowZoom {
  0% {
    transform: translate(-50%, -50%) scale(1);
  }
  100% {
    transform: translate(-50%, -50%) scale(1.1);
  }
}

.hero-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(
    135deg,
    rgba(102, 126, 234, 0.85) 0%,
    rgba(118, 75, 162, 0.8) 100%
  );
  z-index: 1;
}

.hero-pattern {
  position: absolute;
  inset: 0;
  background-image: url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='0.05'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E");
  opacity: 0.4;
  z-index: 2;
}

.hero-content {
  position: relative;
  max-width: 900px;
  width: 100%;
  z-index: 3;
}

.hero-text {
  text-align: center;
  margin-bottom: 3rem;
}

.hero-title {
  font-size: clamp(2.5rem, 5vw, 3.5rem);
  font-weight: 800;
  color: white;
  line-height: 1.4;
  margin: 0 0 1.5rem 0;
  text-shadow: 0 2px 20px rgba(0, 0, 0, 0.2);
}

.highlight {
  display: block;
  background: linear-gradient(90deg, #ffd700 0%, #ffed4e 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.hero-subtitle {
  font-size: 1.25rem;
  color: rgba(255, 255, 255, 0.95);
  line-height: 1.8;
  max-width: 600px;
  margin: 0 auto;
  text-shadow: 0 1px 10px rgba(0, 0, 0, 0.1);
}

.hero-search {
  margin-bottom: 3rem;
}

.search-card {
  background: white;
  border-radius: 20px;
  padding: 2rem;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  animation: slideUp 0.6s ease-out;
}

@keyframes slideUp {
  from {
    opacity: 0;
    transform: translateY(30px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.search-tabs {
  display: flex;
  gap: 1rem;
  margin-bottom: 1.5rem;
  padding: 0.5rem;
  background: #f7fafc;
  border-radius: 12px;
}

.tab-btn {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  background: transparent;
  border: none;
  border-radius: 8px;
  font-size: 0.95rem;
  font-weight: 600;
  color: #64748b;
  cursor: pointer;
  transition: all 0.3s;
}

.tab-btn svg {
  width: 18px;
  height: 18px;
}

.tab-btn.active {
  background: white;
  color: #667eea;
  box-shadow: 0 2px 8px rgba(102, 126, 234, 0.15);
}

.search-inputs {
  display: grid;
  grid-template-columns: 1fr 1fr auto;
  gap: 1rem;
  margin-bottom: 1rem;
}

.input-group {
  position: relative;
  display: flex;
  align-items: center;
}

.input-icon {
  position: absolute;
  right: 1rem;
  width: 20px;
  height: 20px;
  color: #94a3b8;
  pointer-events: none;
}

.search-input {
  width: 100%;
  padding: 1rem 3rem 1rem 1rem;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  font-size: 1rem;
  transition: all 0.3s;
  text-align: right;
}

.search-input:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 4px rgba(102, 126, 234, 0.1);
}

.search-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 1rem 2rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
  white-space: nowrap;
}

.search-btn svg {
  width: 20px;
  height: 20px;
}

.search-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(102, 126, 234, 0.4);
}

.popular-searches {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.label {
  font-size: 0.875rem;
  font-weight: 600;
  color: #64748b;
}

.tag-btn {
  padding: 0.5rem 1rem;
  background: #f1f5f9;
  border: 1px solid #e2e8f0;
  border-radius: 20px;
  font-size: 0.875rem;
  color: #475569;
  cursor: pointer;
  transition: all 0.2s;
}

.tag-btn:hover {
  background: #667eea;
  color: white;
  border-color: #667eea;
  transform: translateY(-1px);
}

.hero-stats {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 2rem;
  padding: 2rem;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(10px);
  border-radius: 16px;
  border: 1px solid rgba(255, 255, 255, 0.2);
}

.stat-item {
  text-align: center;
}

.stat-value {
  font-size: 2rem;
  font-weight: 800;
  color: white;
  margin-bottom: 0.25rem;
}

.stat-label {
  font-size: 0.875rem;
  color: rgba(255, 255, 255, 0.9);
  font-weight: 500;
}

.stat-divider {
  width: 1px;
  height: 40px;
  background: rgba(255, 255, 255, 0.2);
}

/* Provider CTA Banner */
.provider-cta-banner {
  margin-top: 2rem;
  animation: slideUp 0.8s ease-out 0.3s both;
}

.provider-cta-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1.5rem;
  padding: 1.25rem 2rem;
  background: rgba(255, 255, 255, 0.15);
  backdrop-filter: blur(10px);
  border-radius: 16px;
  border: 1px solid rgba(255, 255, 255, 0.25);
  transition: all 0.3s ease;

  &:hover {
    background: rgba(255, 255, 255, 0.2);
    border-color: rgba(255, 255, 255, 0.4);
  }
}

.provider-cta-text {
  display: flex;
  align-items: center;
  gap: 1rem;
  color: white;

  svg {
    width: 28px;
    height: 28px;
    flex-shrink: 0;
  }
}

.provider-cta-title {
  font-size: 1rem;
  font-weight: 700;
  display: block;
}

.provider-cta-subtitle {
  font-size: 0.875rem;
  opacity: 0.9;
  display: block;
  margin-top: 0.125rem;
}

.provider-cta-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  background: white;
  color: #667eea;
  text-decoration: none;
  border-radius: 10px;
  font-weight: 600;
  font-size: 0.9375rem;
  white-space: nowrap;
  transition: all 0.3s ease;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);

  svg {
    width: 18px;
    height: 18px;
    transition: transform 0.3s ease;
  }

  &:hover {
    background: #f8f9fa;
    transform: translateY(-2px);
    box-shadow: 0 8px 20px rgba(0, 0, 0, 0.15);

    svg {
      transform: translateX(-3px);
    }
  }

  &:active {
    transform: translateY(0);
  }
}

/* Responsive */
@media (max-width: 768px) {
  .hero-section {
    min-height: 600px;
    padding: 2rem 1rem;
  }

  .hero-video {
    display: none; /* Hide video on mobile to save bandwidth */
  }

  .search-inputs {
    grid-template-columns: 1fr;
  }

  .search-btn {
    width: 100%;
    justify-content: center;
  }

  .hero-stats {
    flex-direction: column;
    gap: 1.5rem;
  }

  .stat-divider {
    width: 60px;
    height: 1px;
  }

  .search-card {
    padding: 1.5rem;
  }

  .provider-cta-content {
    flex-direction: column;
    text-align: center;
    padding: 1.5rem 1rem;
  }

  .provider-cta-text {
    flex-direction: column;
    text-align: center;
    gap: 0.75rem;
  }

  .provider-cta-button {
    width: 100%;
    justify-content: center;
  }
}
</style>
