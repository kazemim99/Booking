<template>
  <div class="provider-list-view" dir="rtl">
    <div class="container">
      <!-- Header - Booksy Style -->
      <div class="page-header">
        <div class="header-content">
          <h1 class="page-title">پیدا کردن متخصص Booksy شما</h1>
          <p class="page-description">
            رزرو آنلاین سریع و آسان برای خدمات زیبایی و سلامت
          </p>
        </div>
      </div>

      <!-- Service Categories - Booksy Style -->
      <div class="service-categories">
        <button
          v-for="category in serviceCategories"
          :key="category.value"
          :class="['category-card', { active: selectedCategory === category.value }]"
          @click="selectCategory(category.value)"
        >
          <span class="category-emoji">{{ category.emoji }}</span>
          <span class="category-name">{{ category.label }}</span>
        </button>
      </div>

      <!-- Search Bar - Simplified -->
      <div class="search-bar-container">
        <div class="search-bar">
          <span class="search-icon">🔍</span>
          <input
            v-model="searchQuery"
            type="text"
            class="search-input"
            placeholder="جستجوی خدمت یا ارائه‌دهنده..."
            @input="handleSearch"
          />
          <button
            v-if="searchStore.isFiltered"
            class="clear-search-btn"
            @click="clearAllFilters"
          >
            پاک کردن
          </button>
        </div>

        <!-- Quick Filters -->
        <button class="filter-toggle-btn" @click="showFilters = !showFilters">
          <span>فیلترها</span>
          <span class="filter-icon">{{ showFilters ? '▲' : '▼' }}</span>
        </button>
      </div>

      <!-- Collapsible Filters -->
      <div v-if="showFilters" class="filters-panel">
        <SearchFilters />
      </div>

      <div class="content-layout">

        <!-- Main Content -->
        <main class="results-section">
          <!-- Search Stats -->
          <div v-if="!searchStore.isSearching && searchStore.hasResults" class="search-stats">
            <span class="results-count">
              {{ searchStore.totalResults }} ارائه‌دهنده یافت شد
            </span>
            <span v-if="searchStore.isFiltered" class="active-filters-badge">
              {{ searchStore.activeFiltersCount }} فیلتر فعال
            </span>
          </div>

          <!-- Loading State -->
          <div v-if="searchStore.isSearching" class="loading-state">
            <div class="loading-spinner"></div>
            <p>در حال جستجو...</p>
          </div>

          <!-- Error State -->
          <div v-else-if="searchStore.error" class="error-state">
            <div class="error-icon">⚠️</div>
            <p class="error-message">{{ searchStore.error }}</p>
            <button class="retry-btn" @click="handleRetry">تلاش مجدد</button>
          </div>

          <!-- Empty State -->
          <div v-else-if="!searchStore.hasResults && !initialLoad" class="empty-state">
            <div class="empty-icon">🔍</div>
            <h3>ارائه‌دهنده‌ای یافت نشد</h3>
            <p>فیلترهای جستجو را تغییر دهید یا از جستجوی دیگری استفاده کنید</p>
            <button class="clear-filters-btn" @click="searchStore.clearFilters()">
              پاک کردن فیلترها
            </button>
          </div>

          <!-- Results Grid/List -->
          <div v-else-if="searchStore.hasResults" :class="resultsClass">
            <ProviderCard
              v-for="provider in searchStore.providers"
              :key="provider.id"
              :provider="provider"
              @click="handleProviderClick"
              @book-now="handleBookNow"
            />
          </div>

          <!-- Pagination -->
          <div v-if="searchStore.hasResults && searchStore.totalPages > 1" class="pagination">
            <button
              :disabled="!searchStore.hasPreviousPage"
              class="pagination-btn"
              @click="searchStore.previousPage()"
            >
              ← قبلی
            </button>

            <div class="page-numbers">
              <button
                v-for="page in visiblePages"
                :key="page"
                :class="['page-btn', { active: page === searchStore.currentPage }]"
                @click="searchStore.goToPage(page)"
              >
                {{ page }}
              </button>
            </div>

            <button
              :disabled="!searchStore.hasNextPage"
              class="pagination-btn"
              @click="searchStore.nextPage()"
            >
              بعدی →
            </button>
          </div>
        </main>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useSearchStore } from '../stores/search.store'
import ProviderCard from '../components/browse/ProviderCard.vue'
import SearchFilters from '../components/browse/SearchFilters.vue'
import { ProviderType } from '@/modules/provider/types/provider.types'

// ============================================
// Router & Store
// ============================================

const router = useRouter()
const searchStore = useSearchStore()

// ============================================
// Local State
// ============================================

const initialLoad = ref(true)
const searchQuery = ref('')
const selectedCategory = ref<string | null>(null)
const showFilters = ref(false)

// Service Categories - Booksy Style with Emojis
const serviceCategories = [
  { value: ProviderType.Salon, label: 'آرایشگاه', emoji: '💇' },
  { value: ProviderType.Spa, label: 'اسپا', emoji: '💆' },
  { value: ProviderType.Clinic, label: 'کلینیک', emoji: '🏥' },
  { value: ProviderType.Studio, label: 'استودیو', emoji: '🎨' },
  { value: ProviderType.Professional, label: 'حرفه‌ای', emoji: '👔' },
]

// Debounce timer for search
let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null

// ============================================
// Computed
// ============================================

const resultsClass = computed(() => {
  return searchStore.viewMode === 'grid' ? 'results-grid' : 'results-list'
})

const visiblePages = computed(() => {
  const current = searchStore.currentPage
  const total = searchStore.totalPages
  const pages: number[] = []

  // Show max 7 page buttons
  const maxVisible = 7
  let start = Math.max(1, current - Math.floor(maxVisible / 2))
  let end = Math.min(total, start + maxVisible - 1)

  // Adjust start if we're near the end
  if (end - start < maxVisible - 1) {
    start = Math.max(1, end - maxVisible + 1)
  }

  for (let i = start; i <= end; i++) {
    pages.push(i)
  }

  return pages
})

// ============================================
// Methods
// ============================================

function handleProviderClick(providerId: string) {
  router.push(`/customer/provider/${providerId}`)
}

function handleBookNow(providerId: string) {
  router.push({
    name: 'customer-booking-wizard',
    query: { providerId },
  })
}

async function handleRetry() {
  await searchStore.searchProviders()
}

// Booksy-style category selection
function selectCategory(category: string) {
  if (selectedCategory.value === category) {
    // Deselect if clicking the same category
    selectedCategory.value = null
    searchStore.updateFilter('category', undefined)
  } else {
    selectedCategory.value = category
    searchStore.updateFilter('category', category)
  }
}

// Debounced search handler
function handleSearch() {
  if (searchDebounceTimer) {
    clearTimeout(searchDebounceTimer)
  }

  searchDebounceTimer = setTimeout(() => {
    searchStore.updateFilter('searchTerm', searchQuery.value)
  }, 500)
}

// Clear all filters
function clearAllFilters() {
  searchQuery.value = ''
  selectedCategory.value = null
  showFilters.value = false
  searchStore.clearFilters()
}

async function loadInitialData() {
  try {
    // Load featured providers or perform initial search
    if (searchStore.providers.length === 0) {
      await searchStore.searchProviders()
    }
  } catch (error) {
    console.error('[ProviderListView] Error loading data:', error)
  } finally {
    initialLoad.value = false
  }
}

// ============================================
// Lifecycle
// ============================================

onMounted(async () => {
  await loadInitialData()
})
</script>

<style scoped>
.provider-list-view {
  min-height: 100vh;
  background: #f5f7fa;
  padding: 2rem 0;
}

.container {
  max-width: 1400px;
  margin: 0 auto;
  padding: 0 1.5rem;
}

/* Header - Booksy Style */
.page-header {
  text-align: center;
  margin-bottom: 2.5rem;
}

.header-content {
  max-width: 700px;
  margin: 0 auto;
}

.page-title {
  font-size: 2.5rem;
  font-weight: 800;
  color: #1a1a1a;
  margin: 0 0 0.75rem 0;
  letter-spacing: -0.5px;
}

.page-description {
  font-size: 1.125rem;
  color: #666666;
  margin: 0;
  font-weight: 400;
}

/* Service Categories - Booksy Style */
.service-categories {
  display: flex;
  justify-content: center;
  gap: 1rem;
  margin-bottom: 2rem;
  flex-wrap: wrap;
}

.category-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 1.25rem 1.5rem;
  background: white;
  border: 2px solid #e8e8e8;
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.3s ease;
  min-width: 120px;
}

.category-card:hover {
  border-color: #1e96fc;
  box-shadow: 0 4px 12px rgba(30, 150, 252, 0.15);
  transform: translateY(-2px);
}

.category-card.active {
  background: #1e96fc;
  border-color: #1e96fc;
  box-shadow: 0 4px 16px rgba(30, 150, 252, 0.25);
}

.category-emoji {
  font-size: 2rem;
  line-height: 1;
}

.category-name {
  font-size: 0.9375rem;
  font-weight: 600;
  color: #333333;
  transition: color 0.2s;
}

.category-card.active .category-name {
  color: white;
}

/* Search Bar - Simplified Booksy Style */
.search-bar-container {
  max-width: 800px;
  margin: 0 auto 2rem;
  display: flex;
  gap: 1rem;
  align-items: center;
}

.search-bar {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  background: white;
  border: 2px solid #e8e8e8;
  border-radius: 12px;
  padding: 0.875rem 1.25rem;
  transition: all 0.3s;
}

.search-bar:focus-within {
  border-color: #1e96fc;
  box-shadow: 0 0 0 4px rgba(30, 150, 252, 0.1);
}

.search-icon {
  font-size: 1.25rem;
  color: #999999;
}

.search-input {
  flex: 1;
  border: none;
  outline: none;
  font-size: 1rem;
  color: #333333;
  font-family: inherit;
}

.search-input::placeholder {
  color: #999999;
}

.clear-search-btn {
  padding: 0.5rem 1rem;
  background: #f5f5f5;
  border: none;
  border-radius: 6px;
  font-size: 0.875rem;
  font-weight: 600;
  color: #666666;
  cursor: pointer;
  transition: all 0.2s;
}

.clear-search-btn:hover {
  background: #e8e8e8;
  color: #333333;
}

.filter-toggle-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.875rem 1.5rem;
  background: white;
  border: 2px solid #e8e8e8;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  color: #333333;
  cursor: pointer;
  transition: all 0.3s;
  white-space: nowrap;
}

.filter-toggle-btn:hover {
  border-color: #1e96fc;
  background: #f8fbff;
}

.filter-icon {
  font-size: 0.875rem;
  color: #666666;
}

/* Collapsible Filters Panel */
.filters-panel {
  max-width: 800px;
  margin: 0 auto 2rem;
  animation: slideDown 0.3s ease-out;
}

@keyframes slideDown {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Layout - Simplified */
.content-layout {
  max-width: 1200px;
  margin: 0 auto;
}

.results-section {
  min-height: 400px;
}

/* Search Stats */
.search-stats {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1.5rem;
  padding: 1rem 1.5rem;
  background: white;
  border-radius: 12px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.06);
}

.results-count {
  font-size: 1rem;
  font-weight: 600;
  color: #333333;
}

.active-filters-badge {
  background: #1e96fc;
  color: white;
  padding: 0.375rem 0.875rem;
  border-radius: 20px;
  font-size: 0.875rem;
  font-weight: 600;
}

/* Loading State */
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  background: white;
  border-radius: 16px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}

.loading-spinner {
  width: 48px;
  height: 48px;
  border: 4px solid #f0f0f0;
  border-top-color: #1e96fc;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-bottom: 1rem;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.loading-state p {
  color: #666666;
  font-size: 1.125rem;
  margin: 0;
}

/* Error State */
.error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 4rem 2rem;
  background: white;
  border-radius: 16px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}

.error-icon {
  font-size: 4rem;
  margin-bottom: 1rem;
}

.error-message {
  color: #ef4444;
  font-size: 1.125rem;
  font-weight: 600;
  margin: 0 0 1.5rem 0;
  text-align: center;
}

.retry-btn {
  padding: 0.875rem 1.75rem;
  background: #1e96fc;
  color: white;
  border: none;
  border-radius: 10px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.retry-btn:hover {
  background: #0d7de3;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(30, 150, 252, 0.25);
}

/* Empty State */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 4rem 2rem;
  background: white;
  border-radius: 16px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
  text-align: center;
}

.empty-icon {
  font-size: 4rem;
  margin-bottom: 1rem;
  opacity: 0.4;
}

.empty-state h3 {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1a1a1a;
  margin: 0 0 0.5rem 0;
}

.empty-state p {
  color: #666666;
  font-size: 1rem;
  margin: 0 0 1.5rem 0;
  max-width: 450px;
  line-height: 1.6;
}

.clear-filters-btn {
  padding: 0.875rem 1.75rem;
  background: transparent;
  color: #1e96fc;
  border: 2px solid #1e96fc;
  border-radius: 10px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.clear-filters-btn:hover {
  background: #1e96fc;
  color: white;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(30, 150, 252, 0.25);
}

/* Results Grid */
.results-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1.5rem;
}

.results-list {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

/* Pagination */
.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 0.75rem;
  margin-top: 3rem;
  padding: 1.5rem;
  background: white;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}

.pagination-btn {
  padding: 0.75rem 1.25rem;
  background: white;
  color: #1e96fc;
  border: 2px solid #e8e8e8;
  border-radius: 10px;
  font-size: 0.9375rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.pagination-btn:not(:disabled):hover {
  border-color: #1e96fc;
  background: #f8fbff;
}

.pagination-btn:disabled {
  opacity: 0.3;
  cursor: not-allowed;
}

.page-numbers {
  display: flex;
  gap: 0.5rem;
}

.page-btn {
  width: 40px;
  height: 40px;
  background: white;
  border: 2px solid #e8e8e8;
  border-radius: 10px;
  font-size: 0.9375rem;
  font-weight: 600;
  color: #666666;
  cursor: pointer;
  transition: all 0.2s;
}

.page-btn.active {
  background: #1e96fc;
  border-color: #1e96fc;
  color: white;
  box-shadow: 0 2px 8px rgba(30, 150, 252, 0.2);
}

.page-btn:not(.active):hover {
  border-color: #1e96fc;
  color: #1e96fc;
  background: #f8fbff;
}

/* Responsive */
@media (max-width: 1024px) {
  .results-grid {
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  }

  .service-categories {
    gap: 0.75rem;
  }

  .category-card {
    min-width: 100px;
    padding: 1rem 1.25rem;
  }
}

@media (max-width: 768px) {
  .provider-list-view {
    padding: 1.5rem 0;
  }

  .page-title {
    font-size: 2rem;
  }

  .page-description {
    font-size: 1rem;
  }

  .service-categories {
    gap: 0.5rem;
  }

  .category-card {
    min-width: 90px;
    padding: 0.875rem 1rem;
  }

  .category-emoji {
    font-size: 1.75rem;
  }

  .category-name {
    font-size: 0.875rem;
  }

  .search-bar-container {
    flex-direction: column;
    gap: 0.75rem;
  }

  .filter-toggle-btn {
    width: 100%;
    justify-content: center;
  }
}

@media (max-width: 640px) {
  .provider-list-view {
    padding: 1rem 0;
  }

  .container {
    padding: 0 1rem;
  }

  .page-header {
    margin-bottom: 1.5rem;
  }

  .page-title {
    font-size: 1.75rem;
  }

  .page-description {
    font-size: 0.9375rem;
  }

  .service-categories {
    justify-content: flex-start;
    overflow-x: auto;
    flex-wrap: nowrap;
    padding-bottom: 0.5rem;
  }

  .category-card {
    min-width: 85px;
    padding: 0.75rem 0.875rem;
  }

  .results-grid {
    grid-template-columns: 1fr;
    gap: 1rem;
  }

  .pagination {
    padding: 1rem;
    flex-wrap: wrap;
  }

  .pagination-btn {
    padding: 0.625rem 1rem;
    font-size: 0.875rem;
  }

  .page-numbers {
    order: -1;
    width: 100%;
    justify-content: center;
    margin-bottom: 0.75rem;
  }
}
</style>
