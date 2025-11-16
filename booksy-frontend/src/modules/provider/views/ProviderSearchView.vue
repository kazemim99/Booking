<template>
  <div class="provider-search-view">
    <div class="search-container">
      <!-- Sidebar Filters -->
      <aside class="search-sidebar">
        <ProviderFilters
          :filters="currentFilters"
          :show-rating-filter="true"
          :show-location-radius="false"
          :show-status-filter="false"
          @apply="handleApplyFilters"
          @clear="handleClearFilters"
        />
      </aside>

      <!-- Main Content -->
      <main class="search-main">
        <ProviderSearchResults
          :providers="providers"
          :loading="isSearching"
          :current-page="currentPage"
          :total-pages="totalPages"
          :total-results="totalProviders"
          :view-mode="viewMode"
          results-title="Find Your Perfect Provider"
          @page-change="handlePageChange"
          @view-mode-change="handleViewModeChange"
          @provider-click="handleProviderClick"
          @book-click="handleBookClick"
          @clear-filters="handleClearFilters"
        />
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useProviderStore } from '../stores/provider.store'
import ProviderFilters from '../components/ProviderFilters.vue'
import ProviderSearchResults from '../components/ProviderSearchResults.vue'
import type { ProviderSearchFilters, ProviderSummary } from '../types/provider.types'

// Store
const providerStore = useProviderStore()

// Computed
const providers = computed(() => providerStore.providers)
const isSearching = computed(() => providerStore.isSearching)
const currentFilters = computed(() => providerStore.currentFilters)
const currentPage = computed(() => providerStore.currentPage)
const totalPages = computed(() => providerStore.totalPages)
const totalProviders = computed(() => providerStore.totalProviders)
const viewMode = computed(() => providerStore.viewMode)

// Methods
const handleApplyFilters = async (filters: ProviderSearchFilters) => {
  console.log('[ProviderSearchView] Applying filters:', filters)
  await providerStore.applyFilters(filters)
}

const handleClearFilters = async () => {
  console.log('[ProviderSearchView] Clearing filters')
  await providerStore.clearFilters()
}

const handlePageChange = async (page: number) => {
  console.log('[ProviderSearchView] Page change:', page)
  await providerStore.goToPage(page)
  // Scroll to top on page change
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

const handleViewModeChange = (mode: 'grid' | 'list') => {
  console.log('[ProviderSearchView] View mode change:', mode)
  providerStore.setViewMode(mode)
}

const handleProviderClick = (provider: ProviderSummary) => {
  console.log('[ProviderSearchView] Provider clicked:', provider.id)
  // Navigation is handled in ProviderSearchResults component
}

const handleBookClick = (provider: ProviderSummary) => {
  console.log('[ProviderSearchView] Book clicked for provider:', provider.id)
  // Navigation is handled in ProviderSearchResults component
}

// Lifecycle
onMounted(async () => {
  console.log('[ProviderSearchView] Component mounted, loading providers')

  // Load providers with default filters (sorted by rating, descending)
  await providerStore.searchProviders({
    pageNumber: 1,
    pageSize: 12,
    sortBy: 'rating',
    sortDescending: true,
  })
})
</script>

<style scoped>
.provider-search-view {
  min-height: 100vh;
  background: var(--color-bg-primary, #f9fafb);
  padding: 2rem 0;
}

.search-container {
  max-width: 1400px;
  margin: 0 auto;
  padding: 0 1.5rem;
  display: grid;
  grid-template-columns: 320px 1fr;
  gap: 2rem;
  align-items: start;
}

.search-sidebar {
  position: sticky;
  top: 2rem;
  max-height: calc(100vh - 4rem);
  overflow-y: auto;
}

.search-main {
  flex: 1;
  min-width: 0;
}

/* Responsive */
@media (max-width: 1024px) {
  .search-container {
    grid-template-columns: 280px 1fr;
    gap: 1.5rem;
  }
}

@media (max-width: 768px) {
  .provider-search-view {
    padding: 1rem 0;
  }

  .search-container {
    grid-template-columns: 1fr;
    gap: 1.5rem;
    padding: 0 1rem;
  }

  .search-sidebar {
    position: relative;
    top: 0;
    max-height: none;
  }
}

/* Scrollbar styling for sidebar */
.search-sidebar::-webkit-scrollbar {
  width: 6px;
}

.search-sidebar::-webkit-scrollbar-track {
  background: transparent;
}

.search-sidebar::-webkit-scrollbar-thumb {
  background: var(--color-border, #e5e7eb);
  border-radius: 3px;
}

.search-sidebar::-webkit-scrollbar-thumb:hover {
  background: var(--color-text-tertiary, #9ca3af);
}
</style>
