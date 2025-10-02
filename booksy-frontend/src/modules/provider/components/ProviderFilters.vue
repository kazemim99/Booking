<template>
  <div class="provider-filters">
    <div class="filters-header">
      <h3 class="filters-title">
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
            d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z"
          />
        </svg>
        Filters
      </h3>

      <button v-if="hasActiveFilters" class="btn-clear-all" @click="handleClearAll">
        Clear All
      </button>
    </div>

    <div class="filters-body">
      <!-- Search Input -->
      <div class="filter-section">
        <label class="filter-label">Search</label>
        <div class="search-wrapper">
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
              d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
            />
          </svg>
          <input
            v-model="localFilters.searchTerm"
            type="text"
            placeholder="Business name..."
            class="search-input"
            @input="handleInputDebounced"
          />
        </div>
      </div>

      <!-- Location Filters -->
      <div class="filter-section">
        <label class="filter-label">Location</label>
        <input
          v-model="localFilters.city"
          type="text"
          placeholder="City"
          class="filter-input"
          @input="handleInputDebounced"
        />
        <input
          v-model="localFilters.state"
          type="text"
          placeholder="State/Province"
          class="filter-input"
          @input="handleInputDebounced"
        />
        <input
          v-model="localFilters.country"
          type="text"
          placeholder="Country"
          class="filter-input"
          @input="handleInputDebounced"
        />
      </div>

      <!-- Provider Type -->
      <div class="filter-section">
        <label class="filter-label">Provider Type</label>
        <select v-model="localFilters.type" class="filter-select" @change="handleChange">
          <option :value="undefined">All Types</option>
          <option value="Individual">Individual</option>
          <option value="Salon">Salon</option>
          <option value="Clinic">Clinic</option>
          <option value="Spa">Spa</option>
          <option value="Studio">Studio</option>
          <option value="Professional">Professional</option>
        </select>
      </div>

      <!-- Status Filter (if admin) -->
      <div v-if="showStatusFilter" class="filter-section">
        <label class="filter-label">Status</label>
        <select v-model="localFilters.status" class="filter-select" @change="handleChange">
          <option :value="undefined">All Statuses</option>
          <option value="Active">Active</option>
          <option value="Pending">Pending</option>
          <option value="Inactive">Inactive</option>
          <option value="Suspended">Suspended</option>
        </select>
      </div>

      <!-- Features -->
      <div class="filter-section">
        <label class="filter-label">Features</label>
        <div class="checkbox-group">
          <label class="checkbox-label">
            <input
              v-model="localFilters.allowOnlineBooking"
              type="checkbox"
              @change="handleChange"
            />
            <span>Online Booking</span>
          </label>
          <label class="checkbox-label">
            <input
              v-model="localFilters.offersMobileServices"
              type="checkbox"
              @change="handleChange"
            />
            <span>Mobile Services</span>
          </label>
        </div>
      </div>

      <!-- Tags Input -->
      <div class="filter-section">
        <label class="filter-label">Specializations</label>
        <div class="tags-input-wrapper">
          <div class="selected-tags">
            <span v-for="(tag, index) in localFilters.tags" :key="index" class="tag">
              {{ tag }}
              <button @click="removeTag(index)" class="tag-remove">×</button>
            </span>
          </div>
          <input
            v-model="tagInput"
            type="text"
            placeholder="Add specialization..."
            class="tag-input"
            @keydown.enter.prevent="addTag"
            @keydown.comma.prevent="addTag"
          />
        </div>
        <span class="input-hint">Press Enter or comma to add</span>
      </div>

      <!-- Location Radius (if geolocation available) -->
      <div v-if="showLocationRadius" class="filter-section">
        <label class="filter-label"> Distance: {{ localFilters.radiusKm || 10 }} km </label>
        <input
          v-model.number="localFilters.radiusKm"
          type="range"
          min="1"
          max="100"
          step="1"
          class="range-input"
          @change="handleChange"
        />
        <div class="range-labels">
          <span>1 km</span>
          <span>100 km</span>
        </div>
      </div>

      <!-- Rating Filter (future) -->
      <div v-if="showRatingFilter" class="filter-section">
        <label class="filter-label">Minimum Rating</label>
        <div class="rating-select">
          <button
            v-for="rating in [1, 2, 3, 4, 5]"
            :key="rating"
            class="rating-btn"
            :class="{ active: localFilters.minRating === rating }"
            @click="setMinRating(rating)"
          >
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path
                fill-rule="evenodd"
                d="M10.788 3.21c.448-1.077 1.976-1.077 2.424 0l2.082 5.007 5.404.433c1.164.093 1.636 1.545.749 2.305l-4.117 3.527 1.257 5.273c.271 1.136-.964 2.033-1.96 1.425L12 18.354 7.373 21.18c-.996.608-2.231-.29-1.96-1.425l1.257-5.273-4.117-3.527c-.887-.76-.415-2.212.749-2.305l5.404-.433 2.082-5.006z"
                clip-rule="evenodd"
              />
            </svg>
            {{ rating }}+
          </button>
        </div>
      </div>
    </div>

    <div class="filters-footer">
      <button class="btn btn-primary btn-block" @click="handleApply">Apply Filters</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import type { ProviderSearchFilters, ProviderType, ProviderStatus } from '../types/provider.types'

// Props
interface Props {
  filters?: ProviderSearchFilters
  showStatusFilter?: boolean
  showLocationRadius?: boolean
  showRatingFilter?: boolean
  autoApply?: boolean
  debounceMs?: number
}

const props = withDefaults(defineProps<Props>(), {
  showStatusFilter: false,
  showLocationRadius: false,
  showRatingFilter: false,
  autoApply: false,
  debounceMs: 500,
})

// Emits
const emit = defineEmits<{
  (e: 'apply', filters: ProviderSearchFilters): void
  (e: 'clear'): void
  (e: 'change', filters: ProviderSearchFilters): void
}>()

// Local state
const localFilters = ref<ProviderSearchFilters>({
  searchTerm: '',
  city: '',
  state: '',
  country: '',
  type: undefined,
  status: undefined,
  allowOnlineBooking: undefined,
  offersMobileServices: undefined,
  tags: [],
  radiusKm: 10,
  minRating: undefined,
})

const tagInput = ref('')
let debounceTimeout: ReturnType<typeof setTimeout> | null = null

// Computed
const hasActiveFilters = computed(() => {
  return Object.entries(localFilters.value).some(([key, value]) => {
    if (key === 'tags') return Array.isArray(value) && value.length > 0
    return value !== undefined && value !== '' && value !== null
  })
})

// Watch for external filter changes
watch(
  () => props.filters,
  (newFilters) => {
    if (newFilters) {
      localFilters.value = { ...localFilters.value, ...newFilters }
    }
  },
  { immediate: true, deep: true },
)

// Methods
const handleInputDebounced = () => {
  if (debounceTimeout) {
    clearTimeout(debounceTimeout)
  }

  debounceTimeout = setTimeout(() => {
    if (props.autoApply) {
      handleApply()
    } else {
      emit('change', getCleanFilters())
    }
  }, props.debounceMs)
}

const handleChange = () => {
  if (props.autoApply) {
    handleApply()
  } else {
    emit('change', getCleanFilters())
  }
}

const handleApply = () => {
  emit('apply', getCleanFilters())
}

const handleClearAll = () => {
  localFilters.value = {
    searchTerm: '',
    city: '',
    state: '',
    country: '',
    type: undefined,
    status: undefined,
    allowOnlineBooking: undefined,
    offersMobileServices: undefined,
    tags: [],
    radiusKm: 10,
    minRating: undefined,
  }
  tagInput.value = ''
  emit('clear')

  if (props.autoApply) {
    emit('apply', {})
  }
}

const addTag = () => {
  const tag = tagInput.value.trim().replace(',', '')
  if (tag && !localFilters.value.tags?.includes(tag)) {
    if (!localFilters.value.tags) {
      localFilters.value.tags = []
    }
    localFilters.value.tags.push(tag)
    tagInput.value = ''

    if (props.autoApply) {
      handleApply()
    }
  }
}

const removeTag = (index: number) => {
  localFilters.value.tags?.splice(index, 1)

  if (props.autoApply) {
    handleApply()
  }
}

const setMinRating = (rating: number) => {
  localFilters.value.minRating = localFilters.value.minRating === rating ? undefined : rating
  handleChange()
}

const getCleanFilters = (): ProviderSearchFilters => {
  const filters: ProviderSearchFilters = {}

  if (localFilters.value.searchTerm?.trim()) {
    filters.searchTerm = localFilters.value.searchTerm.trim()
  }
  if (localFilters.value.city?.trim()) {
    filters.city = localFilters.value.city.trim()
  }
  if (localFilters.value.state?.trim()) {
    filters.state = localFilters.value.state.trim()
  }
  if (localFilters.value.country?.trim()) {
    filters.country = localFilters.value.country.trim()
  }
  if (localFilters.value.type) {
    filters.type = localFilters.value.type as ProviderType
  }
  if (localFilters.value.status) {
    filters.status = localFilters.value.status as ProviderStatus
  }
  if (localFilters.value.allowOnlineBooking !== undefined) {
    filters.allowOnlineBooking = localFilters.value.allowOnlineBooking
  }
  if (localFilters.value.offersMobileServices !== undefined) {
    filters.offersMobileServices = localFilters.value.offersMobileServices
  }
  if (localFilters.value.tags && localFilters.value.tags.length > 0) {
    filters.tags = [...localFilters.value.tags]
  }
  if (localFilters.value.radiusKm) {
    filters.radiusKm = localFilters.value.radiusKm
  }
  if (localFilters.value.minRating) {
    filters.minRating = localFilters.value.minRating
  }
  if (localFilters.value.latitude && localFilters.value.longitude) {
    filters.latitude = localFilters.value.latitude
    filters.longitude = localFilters.value.longitude
  }

  return filters
}

// Cleanup
const cleanup = () => {
  if (debounceTimeout) {
    clearTimeout(debounceTimeout)
  }
}

// Expose cleanup for parent components
defineExpose({
  cleanup,
  reset: handleClearAll,
})
</script>

<style scoped>
.provider-filters {
  background: white;
  border-radius: 12px;
  border: 1px solid var(--color-border);
  overflow: hidden;
}

.filters-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.25rem;
  border-bottom: 1px solid var(--color-border);
  background: var(--color-bg-secondary);
}

.filters-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 1.1rem;
  font-weight: 600;
  margin: 0;
  color: var(--color-text-primary);
}

.filters-title svg {
  width: 20px;
  height: 20px;
}

.btn-clear-all {
  padding: 0.5rem 1rem;
  background: transparent;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  font-size: 0.9rem;
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: all 0.2s;
}

.btn-clear-all:hover {
  background: var(--color-bg-tertiary);
  border-color: var(--color-text-secondary);
  color: var(--color-text-primary);
}

.filters-body {
  padding: 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  max-height: 70vh;
  overflow-y: auto;
}

.filter-section {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.filter-label {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--color-text-primary);
  margin-bottom: 0.25rem;
}

.search-wrapper {
  position: relative;
}

.search-wrapper svg {
  position: absolute;
  left: 1rem;
  top: 50%;
  transform: translateY(-50%);
  width: 18px;
  height: 18px;
  color: var(--color-text-tertiary);
}

.search-input {
  width: 100%;
  padding: 0.75rem 1rem 0.75rem 2.75rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  font-size: 0.95rem;
  transition: border-color 0.2s;
}

.search-input:focus {
  outline: none;
  border-color: var(--color-primary);
}

.filter-input,
.filter-select {
  padding: 0.75rem 1rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  font-size: 0.95rem;
  background: white;
  transition: border-color 0.2s;
}

.filter-input:focus,
.filter-select:focus {
  outline: none;
  border-color: var(--color-primary);
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
  font-size: 0.95rem;
  color: var(--color-text-primary);
  cursor: pointer;
}

.checkbox-label input[type='checkbox'] {
  width: 18px;
  height: 18px;
  cursor: pointer;
}

.tags-input-wrapper {
  border: 1px solid var(--color-border);
  border-radius: 8px;
  padding: 0.5rem;
  min-height: 100px;
  background: white;
}

.selected-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.tag {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.375rem 0.75rem;
  background: var(--color-primary-light);
  color: var(--color-primary-dark);
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 500;
}

.tag-remove {
  background: none;
  border: none;
  color: currentColor;
  font-size: 1.25rem;
  line-height: 1;
  cursor: pointer;
  padding: 0;
  margin-left: 0.25rem;
}

.tag-remove:hover {
  color: var(--color-danger);
}

.tag-input {
  width: 100%;
  border: none;
  padding: 0.5rem;
  font-size: 0.95rem;
}

.tag-input:focus {
  outline: none;
}

.input-hint {
  font-size: 0.8rem;
  color: var(--color-text-tertiary);
  margin-top: 0.25rem;
  font-style: italic;
}

.range-input {
  width: 100%;
  height: 6px;
  border-radius: 3px;
  background: var(--color-border);
  outline: none;
  cursor: pointer;
}

.range-input::-webkit-slider-thumb {
  appearance: none;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: var(--color-primary);
  cursor: pointer;
}

.range-input::-moz-range-thumb {
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: var(--color-primary);
  cursor: pointer;
  border: none;
}

.range-labels {
  display: flex;
  justify-content: space-between;
  font-size: 0.8rem;
  color: var(--color-text-tertiary);
  margin-top: 0.25rem;
}

.rating-select {
  display: flex;
  gap: 0.5rem;
}

.rating-btn {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--color-border);
  background: white;
  border-radius: 6px;
  font-size: 0.9rem;
  cursor: pointer;
  transition: all 0.2s;
}

.rating-btn svg {
  width: 16px;
  height: 16px;
  color: var(--color-text-tertiary);
}

.rating-btn:hover {
  border-color: var(--color-primary);
  background: var(--color-bg-secondary);
}

.rating-btn.active {
  border-color: var(--color-primary);
  background: var(--color-primary);
  color: white;
}

.rating-btn.active svg {
  color: #fbbf24;
}

.filters-footer {
  padding: 1.25rem;
  border-top: 1px solid var(--color-border);
  background: var(--color-bg-secondary);
}

.btn {
  padding: 0.875rem 1.5rem;
  border: none;
  border-radius: 8px;
  font-size: 0.95rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-primary {
  background: var(--color-primary);
  color: white;
}

.btn-primary:hover {
  background: var(--color-primary-dark);
}

.btn-block {
  width: 100%;
}

/* Scrollbar styling */
.filters-body::-webkit-scrollbar {
  width: 6px;
}

.filters-body::-webkit-scrollbar-track {
  background: var(--color-bg-secondary);
}

.filters-body::-webkit-scrollbar-thumb {
  background: var(--color-border);
  border-radius: 3px;
}

.filters-body::-webkit-scrollbar-thumb:hover {
  background: var(--color-text-tertiary);
}
</style>
