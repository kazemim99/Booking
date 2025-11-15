<template>
  <div class="search-filters" dir="rtl">
    <!-- Filter Header -->
    <div class="filter-header">
      <h3>فیلترها</h3>
      <button v-if="hasActiveFilters" class="clear-all-btn" @click="handleClearAll">
        پاک کردن همه
      </button>
    </div>

    <!-- Search Input -->
    <div class="filter-section">
      <label class="filter-label">جستجو</label>
      <input
        v-model="localFilters.searchTerm"
        type="text"
        class="search-input"
        placeholder="نام ارائه‌دهنده یا خدمت..."
        @input="debounceSearch"
      />
    </div>

    <!-- Category Filter -->
    <div class="filter-section">
      <label class="filter-label">دسته‌بندی</label>
      <div class="checkbox-group">
        <label
          v-for="type in providerTypes"
          :key="type.value"
          class="checkbox-label"
        >
          <input
            v-model="selectedCategories"
            type="checkbox"
            :value="type.value"
            @change="handleCategoryChange"
          />
          <span>{{ type.label }}</span>
        </label>
      </div>
    </div>

    <!-- Rating Filter -->
    <div class="filter-section">
      <label class="filter-label">
        حداقل امتیاز: {{ localFilters.minRating || 0 }} ⭐
      </label>
      <input
        v-model.number="localFilters.minRating"
        type="range"
        min="0"
        max="5"
        step="0.5"
        class="range-slider"
        @change="handleRatingChange"
      />
      <div class="range-labels">
        <span>0</span>
        <span>5</span>
      </div>
    </div>

    <!-- Distance Filter (if location is available) -->
    <div v-if="hasLocation" class="filter-section">
      <label class="filter-label">
        فاصله: {{ localFilters.radiusKm || 5 }} کیلومتر
      </label>
      <input
        v-model.number="localFilters.radiusKm"
        type="range"
        min="1"
        max="50"
        step="1"
        class="range-slider"
        @change="handleDistanceChange"
      />
      <div class="range-labels">
        <span>1 کم</span>
        <span>50 کم</span>
      </div>
    </div>

    <!-- Price Range Filter -->
    <div class="filter-section">
      <label class="filter-label">محدوده قیمت (تومان)</label>
      <div class="price-inputs">
        <input
          v-model.number="localFilters.priceMin"
          type="number"
          placeholder="از"
          class="price-input"
          @change="handlePriceChange"
        />
        <span class="price-separator">تا</span>
        <input
          v-model.number="localFilters.priceMax"
          type="number"
          placeholder="تا"
          class="price-input"
          @change="handlePriceChange"
        />
      </div>
    </div>

    <!-- Availability Filters -->
    <div class="filter-section">
      <label class="filter-label">در دسترس</label>
      <div class="checkbox-group">
        <label class="checkbox-label">
          <input
            v-model="localFilters.openNow"
            type="checkbox"
            @change="handleOpenNowChange"
          />
          <span>الان باز است</span>
        </label>
        <label class="checkbox-label">
          <input
            v-model="localFilters.hasAvailableSlots"
            type="checkbox"
            @change="handleAvailableChange"
          />
          <span>نوبت آزاد دارد</span>
        </label>
      </div>
    </div>

    <!-- Sorting -->
    <div class="filter-section">
      <label class="filter-label">مرتب‌سازی</label>
      <select
        v-model="localFilters.sortBy"
        class="sort-select"
        @change="handleSortChange"
      >
        <option value="">پیش‌فرض</option>
        <option value="distance">نزدیک‌ترین</option>
        <option value="rating">بالاترین امتیاز</option>
        <option value="popular">محبوب‌ترین</option>
        <option value="newest">جدیدترین</option>
      </select>
    </div>

    <!-- Apply Filters Button (Mobile) -->
    <button class="apply-filters-btn" @click="handleApply">
      اعمال فیلترها
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useSearchStore } from '../../stores/search.store'
import { ProviderType } from '@/modules/provider/types/provider.types'
import type { ProviderSearchFilters } from '../../types/search.types'

// ============================================
// Store
// ============================================

const searchStore = useSearchStore()

// ============================================
// Local State
// ============================================

const localFilters = ref<{
  searchTerm?: string
  minRating?: number
  radiusKm?: number
  priceMin?: number
  priceMax?: number
  openNow?: boolean
  hasAvailableSlots?: boolean
  sortBy?: string
}>({
  radiusKm: 5,
})

const selectedCategories = ref<ProviderType[]>([])

// Debounce timer for search input
let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null

// ============================================
// Provider Types
// ============================================

const providerTypes = [
  { value: ProviderType.Salon, label: 'آرایشگاه' },
  { value: ProviderType.Clinic, label: 'کلینیک' },
  { value: ProviderType.Spa, label: 'اسپا' },
  { value: ProviderType.Studio, label: 'استودیو' },
  { value: ProviderType.Professional, label: 'حرفه‌ای' },
  { value: ProviderType.Individual, label: 'فردی' },
]

// ============================================
// Computed
// ============================================

const hasActiveFilters = computed(() => {
  return (
    (localFilters.value.searchTerm && localFilters.value.searchTerm.length > 0) ||
    selectedCategories.value.length > 0 ||
    (localFilters.value.minRating && localFilters.value.minRating > 0) ||
    localFilters.value.priceMin !== undefined ||
    localFilters.value.priceMax !== undefined ||
    localFilters.value.openNow ||
    localFilters.value.hasAvailableSlots ||
    (localFilters.value.sortBy && localFilters.value.sortBy.length > 0)
  )
})

const hasLocation = computed(() => {
  return (
    searchStore.currentFilters.latitude !== undefined &&
    searchStore.currentFilters.longitude !== undefined
  )
})

// ============================================
// Methods
// ============================================

function debounceSearch() {
  if (searchDebounceTimer) {
    clearTimeout(searchDebounceTimer)
  }

  searchDebounceTimer = setTimeout(() => {
    handleApply()
  }, 500) // Wait 500ms after user stops typing
}

function handleCategoryChange() {
  handleApply()
}

function handleRatingChange() {
  handleApply()
}

function handleDistanceChange() {
  if (hasLocation.value) {
    handleApply()
  }
}

function handlePriceChange() {
  handleApply()
}

function handleOpenNowChange() {
  handleApply()
}

function handleAvailableChange() {
  handleApply()
}

function handleSortChange() {
  handleApply()
}

async function handleApply() {
  const filters: Partial<ProviderSearchFilters> = {
    searchTerm: localFilters.value.searchTerm || undefined,
    minRating: localFilters.value.minRating || undefined,
    openNow: localFilters.value.openNow || undefined,
    hasAvailableSlots: localFilters.value.hasAvailableSlots || undefined,
    sortBy: localFilters.value.sortBy as any,
  }

  // Categories
  if (selectedCategories.value.length > 0) {
    filters.providerTypes = selectedCategories.value
  }

  // Price range
  if (localFilters.value.priceMin !== undefined || localFilters.value.priceMax !== undefined) {
    filters.priceRange = {
      min: localFilters.value.priceMin || 0,
      max: localFilters.value.priceMax || 99999999,
    }
  }

  // Distance (if location available)
  if (hasLocation.value && localFilters.value.radiusKm) {
    filters.radiusKm = localFilters.value.radiusKm
  }

  await searchStore.applyFilters(filters)
}

async function handleClearAll() {
  // Reset local filters
  localFilters.value = {
    radiusKm: 5,
  }
  selectedCategories.value = []

  // Clear store filters
  await searchStore.clearFilters()
}

// ============================================
// Lifecycle
// ============================================

onMounted(() => {
  // Initialize from store's current filters
  const storeFilters = searchStore.currentFilters

  if (storeFilters.searchTerm) {
    localFilters.value.searchTerm = storeFilters.searchTerm
  }
  if (storeFilters.minRating) {
    localFilters.value.minRating = storeFilters.minRating
  }
  if (storeFilters.radiusKm) {
    localFilters.value.radiusKm = storeFilters.radiusKm
  }
  if (storeFilters.openNow) {
    localFilters.value.openNow = storeFilters.openNow
  }
  if (storeFilters.hasAvailableSlots) {
    localFilters.value.hasAvailableSlots = storeFilters.hasAvailableSlots
  }
  if (storeFilters.sortBy) {
    localFilters.value.sortBy = storeFilters.sortBy
  }
  if (storeFilters.priceRange) {
    localFilters.value.priceMin = storeFilters.priceRange.min
    localFilters.value.priceMax = storeFilters.priceRange.max
  }
  if (storeFilters.providerTypes) {
    selectedCategories.value = storeFilters.providerTypes
  }
})
</script>

<style scoped>
.search-filters {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.filter-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  padding-bottom: 1rem;
  border-bottom: 2px solid #f3f4f6;
}

.filter-header h3 {
  font-size: 1.25rem;
  font-weight: 700;
  color: #1f2937;
  margin: 0;
}

.clear-all-btn {
  background: none;
  border: none;
  color: #ef4444;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  padding: 0.25rem 0.5rem;
  transition: color 0.2s;
}

.clear-all-btn:hover {
  color: #dc2626;
  text-decoration: underline;
}

.filter-section {
  margin-bottom: 1.5rem;
}

.filter-label {
  display: block;
  font-size: 0.9375rem;
  font-weight: 600;
  color: #374151;
  margin-bottom: 0.75rem;
}

.search-input {
  width: 100%;
  padding: 0.75rem 1rem;
  border: 2px solid #e5e7eb;
  border-radius: 8px;
  font-size: 1rem;
  transition: all 0.2s;
  font-family: inherit;
}

.search-input:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.checkbox-group {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  font-size: 0.9375rem;
  color: #4b5563;
  user-select: none;
}

.checkbox-label input[type='checkbox'] {
  width: 18px;
  height: 18px;
  cursor: pointer;
  accent-color: #667eea;
}

.checkbox-label:hover {
  color: #1f2937;
}

.range-slider {
  width: 100%;
  height: 6px;
  border-radius: 3px;
  background: #e5e7eb;
  outline: none;
  -webkit-appearance: none;
  appearance: none;
  cursor: pointer;
}

.range-slider::-webkit-slider-thumb {
  -webkit-appearance: none;
  appearance: none;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: #667eea;
  cursor: pointer;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}

.range-slider::-moz-range-thumb {
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: #667eea;
  cursor: pointer;
  border: none;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}

.range-labels {
  display: flex;
  justify-content: space-between;
  margin-top: 0.5rem;
  font-size: 0.8125rem;
  color: #6b7280;
}

.price-inputs {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.price-input {
  flex: 1;
  padding: 0.625rem 0.75rem;
  border: 2px solid #e5e7eb;
  border-radius: 6px;
  font-size: 0.9375rem;
  transition: all 0.2s;
  font-family: inherit;
}

.price-input:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.price-separator {
  color: #6b7280;
  font-size: 0.875rem;
  font-weight: 500;
}

.sort-select {
  width: 100%;
  padding: 0.75rem 1rem;
  border: 2px solid #e5e7eb;
  border-radius: 8px;
  font-size: 0.9375rem;
  background: white;
  cursor: pointer;
  transition: all 0.2s;
  font-family: inherit;
}

.sort-select:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.apply-filters-btn {
  width: 100%;
  padding: 0.875rem 1.25rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
  margin-top: 1.5rem;
}

.apply-filters-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.apply-filters-btn:active {
  transform: translateY(0);
}

/* Responsive */
@media (max-width: 768px) {
  .search-filters {
    padding: 1.25rem;
  }

  .filter-section {
    margin-bottom: 1.25rem;
  }
}
</style>
