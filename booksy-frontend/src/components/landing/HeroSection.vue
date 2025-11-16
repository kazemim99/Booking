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
            <div class="input-group">
              <svg class="input-icon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              <input
                v-model="searchQuery"
                type="text"
                :placeholder="searchType === 'service' ? 'به دنبال چه خدمتی هستید؟' : 'موقعیت مکانی خود را وارد کنید...'"
                class="search-input"
                @keydown.enter="handleSearch"
              />
            </div>

            <div class="input-group location-group">
              <svg class="input-icon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
              </svg>
              <input
                v-model="location"
                type="text"
                placeholder="شهر، استان"
                class="search-input"
                @keydown.enter="handleSearch"
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
          <div class="stat-value">۱۰,۰۰۰+</div>
          <div class="stat-label">ارائه‌دهنده خدمات</div>
        </div>
        <div class="stat-divider"></div>
        <div class="stat-item">
          <div class="stat-value">۵۰,۰۰۰+</div>
          <div class="stat-label">مشتری راضی</div>
        </div>
        <div class="stat-divider"></div>
        <div class="stat-item">
          <div class="stat-value">★۴.۸</div>
          <div class="stat-label">میانگین امتیاز</div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

const searchType = ref<'service' | 'location'>('service')
const searchQuery = ref('')
const location = ref('')

const popularSearches = [
  { name: 'کوتاهی مو', slug: 'haircut' },
  { name: 'ماساژ', slug: 'massage' },
  { name: 'پاکسازی پوست', slug: 'facial' },
  { name: 'مانیکور', slug: 'manicure' },
  { name: 'اسپا', slug: 'spa' },
]

const handleSearch = () => {
  const filters: any = {
    pageNumber: 1,
    pageSize: 12,
  }

  if (searchQuery.value) {
    if (searchType.value === 'service') {
      filters.serviceCategory = searchQuery.value.toLowerCase()
    } else {
      filters.searchTerm = searchQuery.value
    }
  }

  if (location.value) {
    const [city, state] = location.value.split('،').map(s => s.trim())
    if (city) filters.city = city
    if (state) filters.state = state
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
}
</style>
