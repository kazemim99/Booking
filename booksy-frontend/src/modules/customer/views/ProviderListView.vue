<template>
  <div class="provider-list-view" dir="rtl">
    <div class="container">
      <!-- Header -->
      <div class="page-header">
        <div class="header-content">
          <h1 class="page-title">جستجوی ارائه‌دهندگان</h1>
          <p class="page-description">
            بهترین ارائه‌دهندگان خدمات را پیدا کنید
          </p>
        </div>

        <!-- View Mode Toggle -->
        <div class="view-controls">
          <div class="view-mode-buttons">
            <button
              :class="['view-btn', { active: searchStore.viewMode === 'grid' }]"
              @click="searchStore.setViewMode('grid')"
              title="نمایش شبکه‌ای"
            >
              <span class="icon">⊞</span>
            </button>
            <button
              :class="['view-btn', { active: searchStore.viewMode === 'list' }]"
              @click="searchStore.setViewMode('list')"
              title="نمایش لیستی"
            >
              <span class="icon">☰</span>
            </button>
          </div>
        </div>
      </div>

      <div class="content-layout">
        <!-- Sidebar - Filters -->
        <aside class="filters-sidebar">
          <SearchFilters />
        </aside>

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

// ============================================
// Router & Store
// ============================================

const router = useRouter()
const searchStore = useSearchStore()

// ============================================
// Local State
// ============================================

const initialLoad = ref(true)

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
  background: #f9fafb;
  padding: 2rem 0;
}

.container {
  max-width: 1400px;
  margin: 0 auto;
  padding: 0 1.5rem;
}

/* Header */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  flex-wrap: wrap;
  gap: 1.5rem;
}

.header-content {
  flex: 1;
  min-width: 250px;
}

.page-title {
  font-size: 2rem;
  font-weight: 800;
  color: #111827;
  margin: 0 0 0.5rem 0;
}

.page-description {
  font-size: 1.125rem;
  color: #6b7280;
  margin: 0;
}

.view-controls {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.view-mode-buttons {
  display: flex;
  gap: 0.5rem;
  background: white;
  padding: 0.25rem;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.08);
}

.view-btn {
  padding: 0.625rem 1rem;
  background: transparent;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
  color: #6b7280;
  font-size: 1.25rem;
}

.view-btn.active {
  background: #667eea;
  color: white;
}

.view-btn:hover:not(.active) {
  background: #f3f4f6;
}

.icon {
  display: block;
}

/* Layout */
.content-layout {
  display: grid;
  grid-template-columns: 300px 1fr;
  gap: 2rem;
  align-items: start;
}

.filters-sidebar {
  position: sticky;
  top: 2rem;
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
  padding: 1rem;
  background: white;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}

.results-count {
  font-size: 1rem;
  font-weight: 600;
  color: #374151;
}

.active-filters-badge {
  background: #667eea;
  color: white;
  padding: 0.375rem 0.875rem;
  border-radius: 16px;
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
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.loading-spinner {
  width: 48px;
  height: 48px;
  border: 4px solid #f3f4f6;
  border-top-color: #667eea;
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
  color: #6b7280;
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
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
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
  padding: 0.75rem 1.5rem;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.retry-btn:hover {
  background: #5568d3;
  transform: translateY(-1px);
}

/* Empty State */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 4rem 2rem;
  background: white;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  text-align: center;
}

.empty-icon {
  font-size: 4rem;
  margin-bottom: 1rem;
  opacity: 0.5;
}

.empty-state h3 {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1f2937;
  margin: 0 0 0.5rem 0;
}

.empty-state p {
  color: #6b7280;
  font-size: 1rem;
  margin: 0 0 1.5rem 0;
  max-width: 400px;
}

.clear-filters-btn {
  padding: 0.75rem 1.5rem;
  background: transparent;
  color: #667eea;
  border: 2px solid #667eea;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.clear-filters-btn:hover {
  background: #667eea;
  color: white;
  transform: translateY(-1px);
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
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.pagination-btn {
  padding: 0.75rem 1.25rem;
  background: white;
  color: #667eea;
  border: 2px solid #e5e7eb;
  border-radius: 8px;
  font-size: 0.9375rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.pagination-btn:not(:disabled):hover {
  border-color: #667eea;
  background: #f3f4f6;
}

.pagination-btn:disabled {
  opacity: 0.4;
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
  border: 2px solid #e5e7eb;
  border-radius: 8px;
  font-size: 0.9375rem;
  font-weight: 600;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.2s;
}

.page-btn.active {
  background: #667eea;
  border-color: #667eea;
  color: white;
}

.page-btn:not(.active):hover {
  border-color: #667eea;
  color: #667eea;
  background: #f3f4f6;
}

/* Responsive */
@media (max-width: 1024px) {
  .content-layout {
    grid-template-columns: 1fr;
  }

  .filters-sidebar {
    position: static;
  }

  .results-grid {
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
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
    font-size: 1.5rem;
  }

  .page-description {
    font-size: 1rem;
  }

  .content-layout {
    gap: 1.5rem;
  }

  .results-grid {
    grid-template-columns: 1fr;
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
