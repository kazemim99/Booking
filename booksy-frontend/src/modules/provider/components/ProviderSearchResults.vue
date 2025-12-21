<template>
  <div class="provider-search-results">
    <!-- Results Header -->
    <div class="results-header">
      <div class="results-info">
        <h2 class="results-title">
          {{ resultsTitle }}
        </h2>
        <p v-if="totalResults > 0" class="results-count">
          {{ totalResults }} provider{{ totalResults !== 1 ? 's' : '' }} found
        </p>
      </div>

      <div class="results-controls">
        <!-- Sort By Dropdown -->
        <div class="sort-dropdown">
          <label for="sort-select" class="sort-label">مرتب‌سازی:</label>
          <select id="sort-select" v-model="selectedSort" class="sort-select" @change="handleSortChange">
            <option value="rating-desc">بالاترین امتیاز</option>
            <option value="rating-asc">کمترین امتیاز</option>
            <option value="name-asc">نام (الف-ی)</option>
            <option value="name-desc">نام (ی-الف)</option>
            <option value="distance-asc">نزدیک‌ترین</option>
            <option value="distance-desc">دورترین</option>
          </select>
        </div>

        <!-- View Mode Toggle -->
        <div class="view-toggle">
          <button
            :class="['view-btn', { active: viewMode === 'grid' }]"
            @click="emit('viewModeChange', 'grid')"
            title="نمایش شبکه‌ای"
          >
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
            </svg>
          </button>
          <button
            :class="['view-btn', { active: viewMode === 'list' }]"
            @click="emit('viewModeChange', 'list')"
            title="نمایش لیستی"
          >
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
          <button
            :class="['view-btn', { active: viewMode === 'map' }]"
            @click="emit('viewModeChange', 'map')"
            title="نمایش نقشه"
          >
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
            </svg>
          </button>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="loading-state">
      <div class="loading-spinner"></div>
      <p>Searching providers...</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="!loading && providers.length === 0" class="empty-state">
      <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
      </svg>
      <h3>No providers found</h3>
      <p>Try adjusting your filters or search criteria</p>
      <button class="btn-clear-filters" @click="emit('clearFilters')">
        Clear All Filters
      </button>
    </div>

    <!-- Results Grid/List -->
    <div
      v-else
      :class="['results-container', `view-mode-${viewMode}`]"
    >
      <ProviderCard
        v-for="provider in providers"
        :key="provider.id"
        :provider="provider"
        :view-mode="cardViewMode"
        @click="handleProviderClick(provider)"
        @book="handleBookClick(provider)"
      />
    </div>

    <!-- Pagination -->
    <div v-if="!loading && totalPages > 1" class="pagination">
      <button
        class="pagination-btn"
        :disabled="currentPage === 1"
        @click="emit('pageChange', currentPage - 1)"
      >
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
        Previous
      </button>

      <div class="pagination-numbers">
        <button
          v-for="page in visiblePages"
          :key="page"
          :class="['page-btn', { active: page === currentPage }]"
          @click="emit('pageChange', page)"
        >
          {{ page }}
        </button>
      </div>

      <button
        class="pagination-btn"
        :disabled="currentPage === totalPages"
        @click="emit('pageChange', currentPage + 1)"
      >
        Next
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import ProviderCard from './ProviderCard.vue'
import type { ProviderSummary } from '../types/provider.types'

// Props
interface Props {
  providers: ProviderSummary[]
  loading?: boolean
  currentPage?: number
  totalPages?: number
  totalResults?: number
  viewMode?: 'grid' | 'list' | 'map'
  resultsTitle?: string
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
  currentPage: 1,
  totalPages: 1,
  totalResults: 0,
  viewMode: 'grid',
  resultsTitle: 'Search Results',
})

// Emits
const emit = defineEmits<{
  (e: 'pageChange', page: number): void
  (e: 'viewModeChange', mode: 'grid' | 'list' | 'map'): void
  (e: 'sortChange', sortBy: string, sortDescending: boolean): void
  (e: 'providerClick', provider: ProviderSummary): void
  (e: 'bookClick', provider: ProviderSummary): void
  (e: 'clearFilters'): void
}>()

// Router
const router = useRouter()

// State
const selectedSort = ref('rating-desc')

// Computed
const cardViewMode = computed(() => {
  return props.viewMode === 'map' ? 'grid' : (props.viewMode as 'grid' | 'list')
})

const visiblePages = computed(() => {
  const pages: number[] = []
  const maxVisible = 5
  let start = Math.max(1, props.currentPage - Math.floor(maxVisible / 2))
  const end = Math.min(props.totalPages, start + maxVisible - 1)

  if (end - start < maxVisible - 1) {
    start = Math.max(1, end - maxVisible + 1)
  }

  for (let i = start; i <= end; i++) {
    pages.push(i)
  }

  return pages
})

// Methods
const handleSortChange = () => {
  const [sortBy, direction] = selectedSort.value.split('-')
  const sortDescending = direction === 'desc'
  emit('sortChange', sortBy, sortDescending)
}

const handleProviderClick = (provider: ProviderSummary) => {
  emit('providerClick', provider)
  router.push(`/providers/${provider.id}`)
}

const handleBookClick = (provider: ProviderSummary) => {
  emit('bookClick', provider)
  router.push(`/book/${provider.id}`)
}
</script>

<style scoped>
.provider-search-results {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.results-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  flex-wrap: wrap;
}

.results-info {
  flex: 1;
}

.results-title {
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--color-text-primary);
  margin: 0 0 0.5rem 0;
}

.results-count {
  font-size: 1rem;
  color: var(--color-text-secondary);
  margin: 0;
}

.results-controls {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  flex-wrap: wrap;
}

/* Sort Dropdown */
.sort-dropdown {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.sort-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
  font-family: 'Vazir', 'IRANSans', sans-serif;
  white-space: nowrap;
}

.sort-select {
  padding: 0.5rem 2.5rem 0.5rem 1rem;
  background: white;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 0.875rem;
  font-family: 'Vazir', 'IRANSans', sans-serif;
  color: #111827;
  cursor: pointer;
  transition: all 0.2s;
  appearance: none;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 24 24' stroke='%236b7280'%3E%3Cpath stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M19 9l-7 7-7-7'/%3E%3C/svg%3E");
  background-repeat: no-repeat;
  background-position: left 0.5rem center;
  background-size: 1.25rem;
  direction: rtl;
}

.sort-select:hover {
  border-color: #8b5cf6;
  box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
}

.sort-select:focus {
  outline: none;
  border-color: #8b5cf6;
  box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.2);
}

.view-toggle {
  display: flex;
  gap: 0.5rem;
  background: var(--color-bg-secondary);
  padding: 0.25rem;
  border-radius: 8px;
}

.view-btn {
  padding: 0.5rem;
  background: transparent;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
}

.view-btn svg {
  width: 20px;
  height: 20px;
  color: var(--color-text-secondary);
}

.view-btn:hover {
  background: white;
}

.view-btn.active {
  background: var(--color-primary);
}

.view-btn.active svg {
  color: white;
}

/* Loading State */
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  text-align: center;
}

.loading-spinner {
  width: 48px;
  height: 48px;
  border: 4px solid var(--color-border);
  border-top-color: var(--color-primary);
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
  color: var(--color-text-secondary);
  font-size: 1rem;
}

/* Empty State */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  text-align: center;
  background: var(--color-bg-secondary);
  border-radius: 12px;
}

.empty-state svg {
  width: 64px;
  height: 64px;
  color: var(--color-text-tertiary);
  margin-bottom: 1.5rem;
}

.empty-state h3 {
  font-size: 1.5rem;
  font-weight: 600;
  color: var(--color-text-primary);
  margin: 0 0 0.5rem 0;
}

.empty-state p {
  font-size: 1rem;
  color: var(--color-text-secondary);
  margin: 0 0 1.5rem 0;
}

.btn-clear-filters {
  padding: 0.75rem 1.5rem;
  background: var(--color-primary);
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 0.95rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-clear-filters:hover {
  background: var(--color-primary-dark);
  transform: scale(1.05);
}

/* Results Container */
.results-container {
  display: grid;
  gap: 1.5rem;
}

.results-container.view-mode-grid {
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
}

.results-container.view-mode-list {
  grid-template-columns: 1fr;
}

/* Pagination */
.pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 2rem 0;
}

.pagination-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.25rem;
  background: white;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  font-size: 0.95rem;
  font-weight: 500;
  color: var(--color-text-primary);
  cursor: pointer;
  transition: all 0.2s;
}

.pagination-btn svg {
  width: 16px;
  height: 16px;
}

.pagination-btn:hover:not(:disabled) {
  background: var(--color-bg-secondary);
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.pagination-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.pagination-numbers {
  display: flex;
  gap: 0.25rem;
}

.page-btn {
  min-width: 40px;
  height: 40px;
  padding: 0.5rem;
  background: white;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  font-size: 0.95rem;
  font-weight: 500;
  color: var(--color-text-primary);
  cursor: pointer;
  transition: all 0.2s;
}

.page-btn:hover {
  background: var(--color-bg-secondary);
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.page-btn.active {
  background: var(--color-primary);
  border-color: var(--color-primary);
  color: white;
}

/* Responsive */
@media (max-width: 768px) {
  .results-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
  }

  .results-controls {
    width: 100%;
    flex-direction: column;
    align-items: stretch;
    gap: 1rem;
  }

  .sort-dropdown {
    width: 100%;
    justify-content: space-between;
  }

  .sort-select {
    flex: 1;
    min-width: 0;
  }

  .view-toggle {
    width: 100%;
    justify-content: center;
  }

  .results-container.view-mode-grid {
    grid-template-columns: 1fr;
  }

  .pagination {
    flex-wrap: wrap;
  }

  .pagination-numbers {
    order: 3;
    width: 100%;
    justify-content: center;
    margin-top: 0.5rem;
  }
}
</style>
