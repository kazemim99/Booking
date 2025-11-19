<template>
  <div class="provider-search-view">
    <!-- Mobile Filter Toggle Button -->
    <button
      v-if="isMobile"
      class="mobile-filter-toggle"
      @click="toggleMobileFilters"
      :class="{ active: showMobileFilters }"
    >
      <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z"
        />
      </svg>
      <span>فیلترها</span>
      <span v-if="activeFiltersCount > 0" class="filter-badge">{{ activeFiltersCount }}</span>
    </button>

    <div class="search-container">
      <!-- Sidebar Filters with Mobile Drawer -->
      <aside class="search-sidebar" :class="{ 'mobile-open': showMobileFilters }">
        <div v-if="isMobile" class="mobile-filter-header">
          <h3>فیلترها</h3>
          <button @click="closeMobileFilters" class="close-btn">
            <svg
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>
        </div>

        <ProviderFilters
          :filters="currentFilters"
          :show-rating-filter="true"
          :show-location-radius="false"
          :show-status-filter="false"
          @apply="handleApplyFilters"
          @clear="handleClearFilters"
        />
      </aside>

      <!-- Backdrop for Mobile Filters -->
      <div
        v-if="isMobile && showMobileFilters"
        class="mobile-backdrop"
        @click="closeMobileFilters"
      ></div>

      <!-- Main Content -->
      <main class="search-main">
        <!-- Grid/List View -->
        <ProviderSearchResults
          v-if="viewMode === 'grid' || viewMode === 'list'"
          :providers="providers"
          :loading="isSearching"
          :current-page="currentPage"
          :total-pages="totalPages"
          :total-results="totalProviders"
          :view-mode="viewMode"
          results-title="یافتن بهترین ارائه‌دهنده خدمات"
          @page-change="handlePageChange"
          @view-mode-change="handleViewModeChange"
          @sort-change="handleSortChange"
          @provider-click="handleProviderClick"
          @book-click="handleBookClick"
          @clear-filters="handleClearFilters"
        />

        <!-- Map View -->
        <MapViewResults
          v-else-if="viewMode === 'map'"
          :providers="providers"
          :loading="isSearching"
          :map-key="neshanMapKey"
          :service-key="neshanServiceKey"
          @view-mode-change="handleViewModeChange"
          @provider-click="handleProviderClick"
          @book="handleBookClick"
        />
      </main>
    </div>

    <!-- Scroll to Top Button -->
    <transition name="fade">
      <button v-if="showScrollTop" class="scroll-to-top" @click="scrollToTop">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M5 10l7-7m0 0l7 7m-7-7v18"
          />
        </svg>
      </button>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, computed, watch, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useProviderStore } from '../stores/provider.store'
import ProviderFilters from '../components/ProviderFilters.vue'
import ProviderSearchResults from '../components/ProviderSearchResults.vue'
import MapViewResults from '../components/MapViewResults.vue'
import type { ProviderSearchFilters, ProviderSummary } from '../types/provider.types'

// Router
const route = useRoute()

// Store
const providerStore = useProviderStore()

// Neshan Maps Configuration
const neshanMapKey = import.meta.env.VITE_NESHAN_MAP_KEY
const neshanServiceKey = import.meta.env.VITE_NESHAN_SERVICE_KEY

// Mobile UI State
const isMobile = ref(false)
const showMobileFilters = ref(false)
const showScrollTop = ref(false)

// Computed
const providers = computed(() => providerStore.providers)
const isSearching = computed(() => providerStore.isSearching)
const currentFilters = computed(() => providerStore.currentFilters)
const currentPage = computed(() => providerStore.currentPage)
const totalPages = computed(() => providerStore.totalPages)
const totalProviders = computed(() => providerStore.totalProviders)
const viewMode = computed(() => providerStore.viewMode)
const activeFiltersCount = computed(() => providerStore.activeFiltersCount)

// Calculate average rating from providers
const averageRating = computed(() => {
  if (providers.value.length === 0) return null
  const ratings = providers.value
    .map((p) => p.averageRating)
    .filter((r): r is number => r !== undefined && r !== null)
  if (ratings.length === 0) return null
  const avg = ratings.reduce((sum, r) => sum + r, 0) / ratings.length
  return avg.toFixed(1)
})

// Methods
const handleApplyFilters = async (filters: ProviderSearchFilters) => {
  console.log('[ProviderSearchView] Applying filters:', filters)
  await providerStore.applyFilters(filters)
  if (isMobile.value) {
    closeMobileFilters()
  }
}

const handleClearFilters = async () => {
  console.log('[ProviderSearchView] Clearing filters')
  await providerStore.clearFilters()
}

const handlePageChange = async (page: number) => {
  console.log('[ProviderSearchView] Page change:', page)
  await providerStore.goToPage(page)
  // Scroll to top on page change
  scrollToTop()
}

const handleViewModeChange = (mode: 'grid' | 'list' | 'map') => {
  console.log('[ProviderSearchView] View mode change:', mode)
  providerStore.setViewMode(mode)
}

const handleSortChange = async (sortBy: string, sortDescending: boolean) => {
  console.log('[ProviderSearchView] Sort change:', sortBy, sortDescending)
  await providerStore.applyFilters({
    ...currentFilters.value,
    sortBy,
    sortDescending,
    pageNumber: 1, // Reset to first page on sort change
  })
}

const handleProviderClick = (provider: ProviderSummary) => {
  console.log('[ProviderSearchView] Provider clicked:', provider.id)
  // Navigation is handled in ProviderSearchResults component
}

const handleBookClick = (provider: ProviderSummary) => {
  console.log('[ProviderSearchView] Book clicked for provider:', provider.id)
  // Navigation is handled in ProviderSearchResults component
}

// Mobile Filter Methods
const toggleMobileFilters = () => {
  showMobileFilters.value = !showMobileFilters.value
  // Prevent body scroll when filters are open
  if (showMobileFilters.value) {
    document.body.style.overflow = 'hidden'
  } else {
    document.body.style.overflow = ''
  }
}

const closeMobileFilters = () => {
  showMobileFilters.value = false
  document.body.style.overflow = ''
}

// Scroll Methods
const scrollToTop = () => {
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

const handleScroll = () => {
  showScrollTop.value = window.scrollY > 300
}

// Responsive Methods
const checkMobile = () => {
  isMobile.value = window.innerWidth < 768
  // Close mobile filters if window is resized to desktop
  if (!isMobile.value && showMobileFilters.value) {
    closeMobileFilters()
  }
}

// Parse query parameters from URL (coming from HeroSection)
function parseQueryParams(): ProviderSearchFilters {
  const filters: ProviderSearchFilters = {
    pageNumber: Number(route.query.pageNumber) || 1,
    pageSize: Number(route.query.pageSize) || 12,
    sortBy: 'rating',
    sortDescending: true,
  }

  // Service category from quick search or search input
  if (route.query.serviceCategory) {
    filters.serviceCategory = String(route.query.serviceCategory)
  }

  // Search term (general search)
  if (route.query.searchTerm) {
    filters.searchTerm = String(route.query.searchTerm)
  }

  // Location filters
  if (route.query.city) {
    filters.city = String(route.query.city)
  }
  if (route.query.state) {
    filters.state = String(route.query.state)
  }

  return filters
}

// Lifecycle
onMounted(async () => {
  console.log('[ProviderSearchView] Component mounted')
  console.log('[ProviderSearchView] Query params:', route.query)

  // Initialize mobile detection
  checkMobile()
  window.addEventListener('resize', checkMobile)
  window.addEventListener('scroll', handleScroll)

  // Parse filters from URL query parameters
  const filters = parseQueryParams()
  console.log('[ProviderSearchView] Parsed filters:', filters)

  // Load providers with filters from URL
  await providerStore.searchProviders(filters)
})

onUnmounted(() => {
  // Clean up event listeners
  window.removeEventListener('resize', checkMobile)
  window.removeEventListener('scroll', handleScroll)
  // Reset body overflow in case filters were open
  document.body.style.overflow = ''
})

// Watch for query parameter changes (when user navigates from HeroSection)
watch(
  () => route.query,
  async (newQuery) => {
    console.log('[ProviderSearchView] Query params changed:', newQuery)

    const filters = parseQueryParams()
    console.log('[ProviderSearchView] Applying new filters:', filters)

    await providerStore.searchProviders(filters)
  },
)
</script>

<style scoped>
.provider-search-view {
  min-height: 100vh;
  background: linear-gradient(135deg, #f5f7fa 0%, #e8ecf1 100%);
  padding: 2rem 0;
  position: relative;
}

/* Mobile Filter Toggle Button */
.mobile-filter-toggle {
  position: fixed;
  bottom: 2rem;
  right: 2rem;
  z-index: 999;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem 1.5rem;
  background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%);
  color: white;
  border: none;
  border-radius: 50px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 8px 24px rgba(139, 92, 246, 0.4);
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  font-family: 'Vazir', 'IRANSans', sans-serif;
}

.mobile-filter-toggle svg {
  width: 20px;
  height: 20px;
  color: white;
  stroke: white;
}

.mobile-filter-toggle span {
  color: white;
  font-weight: 600;
}

.mobile-filter-toggle:hover {
  background: linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%);
  transform: translateY(-2px);
  box-shadow: 0 12px 32px rgba(139, 92, 246, 0.5);
}

.mobile-filter-toggle:active {
  transform: scale(0.95);
}

.mobile-filter-toggle.active {
  background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
  box-shadow: 0 8px 24px rgba(239, 68, 68, 0.4);
}

.filter-badge {
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 24px;
  height: 24px;
  padding: 0 0.5rem;
  background: rgba(255, 255, 255, 0.3);
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 700;
}

.search-container {
  max-width: 1400px;
  margin: 0 auto;
  padding: 0 1.5rem;
  display: grid;
  grid-template-columns: 320px 1fr;
  gap: 2rem;
  align-items: start;
  position: relative;
}

/* Sidebar Filters */
.search-sidebar {
  position: sticky;
  top: 2rem;
  max-height: calc(100vh - 4rem);
  overflow-y: auto;
  border-radius: 16px;
  background: white;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
  transition: all 0.3s ease;
}

.search-sidebar:hover {
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
}

/* Mobile Drawer for Filters */
@media (max-width: 768px) {
  .search-sidebar {
    position: fixed;
    top: 0;
    right: -100%;
    width: 85%;
    max-width: 380px;
    height: 100vh;
    max-height: 100vh;
    z-index: 1000;
    border-radius: 0;
    box-shadow: -4px 0 16px rgba(0, 0, 0, 0.2);
    transition: right 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    overflow-y: auto;
  }

  .search-sidebar.mobile-open {
    right: 0;
  }
}

.mobile-filter-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid var(--color-border);
  background: var(--color-bg-secondary);
  position: sticky;
  top: 0;
  z-index: 10;
}

.mobile-filter-header h3 {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--color-text-primary);
  font-family: 'Vazir', 'IRANSans', sans-serif;
}

.close-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  padding: 0;
  background: transparent;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}

.close-btn svg {
  width: 20px;
  height: 20px;
  color: var(--color-text-secondary);
}

.close-btn:hover {
  background: var(--color-bg-tertiary);
  border-color: var(--color-text-secondary);
}

/* Mobile Backdrop */
.mobile-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  z-index: 999;
  backdrop-filter: blur(4px);
  animation: fadeIn 0.3s ease;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

/* Main Content */
.search-main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Quick Stats Bar (Persian Style) */
.quick-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
  gap: 1rem;
  padding: 1.5rem;
  background: white;
  border-radius: 16px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.06);
  animation: slideUp 0.4s ease;
}

@keyframes slideUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.stat-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding: 0.75rem;
  border-radius: 12px;
  background: linear-gradient(135deg, var(--color-bg-secondary) 0%, var(--color-bg-primary) 100%);
  transition: all 0.3s ease;
}

.stat-item:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
}

.stat-value {
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--color-primary);
  line-height: 1.2;
  font-family: 'Vazir', 'IRANSans', sans-serif;
}

.stat-label {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
  margin-top: 0.25rem;
  font-family: 'Vazir', 'IRANSans', sans-serif;
}

/* Scroll to Top Button */
.scroll-to-top {
  position: fixed;
  bottom: 2rem;
  left: 2rem;
  z-index: 998;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 48px;
  height: 48px;
  padding: 0;
  background: white;
  border: 2px solid var(--color-primary);
  border-radius: 50%;
  cursor: pointer;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.scroll-to-top svg {
  width: 24px;
  height: 24px;
  color: var(--color-primary);
}

.scroll-to-top:hover {
  background: var(--color-primary);
  transform: translateY(-4px);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15);
}

.scroll-to-top:hover svg {
  color: white;
}

/* Fade Transition */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
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
    gap: 1rem;
    padding: 0 1rem;
  }

  .mobile-filter-toggle {
    bottom: 1.5rem;
    right: 1.5rem;
    padding: 0.875rem 1.25rem;
  }

  .scroll-to-top {
    bottom: 5rem;
    left: 1.5rem;
    width: 44px;
    height: 44px;
  }

  .quick-stats {
    grid-template-columns: repeat(3, 1fr);
    gap: 0.75rem;
    padding: 1rem;
  }

  .stat-value {
    font-size: 1.5rem;
  }

  .stat-label {
    font-size: 0.75rem;
  }
}

@media (max-width: 480px) {
  .quick-stats {
    grid-template-columns: 1fr;
  }

  .stat-item {
    flex-direction: row;
    justify-content: space-between;
    text-align: right;
  }

  .stat-label {
    margin-top: 0;
  }
}
</style>
