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
        فیلترها
      </h3>

      <button v-if="hasActiveFilters" class="btn-clear-all" @click="handleClearAll">
        پاک کردن همه
      </button>
    </div>

    <div class="filters-body">
      <!-- Voice Search Button -->
      <div class="filter-section">
        <button class="voice-search-btn" @click="startVoiceSearch" :class="{ listening: isListening }">
          <svg v-if="!isListening" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11a7 7 0 01-7 7m0 0a7 7 0 01-7-7m7 7v4m0 0H8m4 0h4m-4-8a3 3 0 01-3-3V5a3 3 0 116 0v6a3 3 0 01-3 3z" />
          </svg>
          <div v-else class="pulse-animation">
            <span class="pulse-dot"></span>
          </div>
          <span>{{ isListening ? 'در حال گوش دادن...' : 'جستجوی صوتی' }}</span>
        </button>
        <p v-if="voiceSearchError" class="voice-error">{{ voiceSearchError }}</p>
      </div>

      <!-- Search Input -->
      <div class="filter-section">
        <label class="filter-label">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          جستجو
        </label>
        <div class="search-wrapper">
          <input
            v-model="localFilters.searchTerm"
            type="text"
            placeholder="نام کسب‌وکار..."
            class="search-input"
            @input="handleInputDebounced"
          />
          <button v-if="localFilters.searchTerm" class="clear-input-btn" @click="localFilters.searchTerm = ''; handleChange()">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </div>

      <!-- Service Category -->
      <div class="filter-section">
        <label class="filter-label">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 21a4 4 0 01-4-4V5a2 2 0 012-2h4a2 2 0 012 2v12a4 4 0 01-4 4zm0 0h12a2 2 0 002-2v-4a2 2 0 00-2-2h-2.343M11 7.343l1.657-1.657a2 2 0 012.828 0l2.829 2.829a2 2 0 010 2.828l-8.486 8.485M7 17h.01" />
          </svg>
          دسته‌بندی خدمات
        </label>
        <div class="custom-select-wrapper">
          <select
            v-model="localFilters.serviceCategory"
            class="filter-select"
            @change="handleChange"
          >
            <option :value="undefined">همه دسته‌ها</option>
            <option value="haircut">آرایشگری و مدل مو</option>
            <option value="coloring">رنگ مو</option>
            <option value="massage">ماساژ درمانی</option>
            <option value="spa">خدمات اسپا</option>
            <option value="facial">پاکسازی و مراقبت پوست</option>
            <option value="manicure">مانیکور و پدیکور</option>
            <option value="waxing">اپیلاسیون</option>
            <option value="makeup">آرایش و زیبایی</option>
            <option value="barbering">آرایشگری مردانه</option>
            <option value="tattoo">تاتو و پیرسینگ</option>
          </select>
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
          </svg>
        </div>
      </div>

      <!-- Price Range with Visual Indicators -->
      <div class="filter-section">
        <label class="filter-label">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          محدوده قیمت
        </label>
        <div class="price-range-buttons">
          <button
            v-for="range in priceRanges"
            :key="range.value"
            :class="['price-btn', { active: localFilters.priceRange === range.value }]"
            @click="selectPriceRange(range.value)"
          >
            <span class="price-icon">{{ range.icon }}</span>
            <span class="price-label">{{ range.label }}</span>
          </button>
        </div>
      </div>

      <!-- Location Filters with Icons -->
      <div class="filter-section">
        <label class="filter-label">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
          </svg>
          موقعیت مکانی
        </label>
        <div class="location-inputs">
          <button class="use-location-btn" @click="useCurrentLocation" :disabled="gettingLocation">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
            </svg>
            {{ gettingLocation ? 'در حال دریافت...' : 'موقعیت من' }}
          </button>
        </div>
      </div>

      <!-- Provider Hierarchy Type (Organization vs Individual) -->
      <div class="filter-section">
        <label class="filter-label">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
          </svg>
          نوع ارائه‌دهنده
        </label>
        <div class="hierarchy-type-btns">
          <button
            v-for="hType in providerHierarchyTypes"
            :key="hType.value"
            :class="['hierarchy-btn', { active: localFilters.hierarchyType === hType.value }]"
            @click="selectHierarchyType(hType.value)"
          >
            <span class="hierarchy-icon">{{ hType.icon }}</span>
            <span class="hierarchy-label">{{ hType.label }}</span>
          </button>
        </div>
      </div>

      <!-- Business Type (for more specific filtering) -->
      <div class="filter-section">
        <label class="filter-label">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
          </svg>
          نوع کسب‌وکار
        </label>
        <div class="type-chips">
          <button
            v-for="type in providerTypes"
            :key="type.value"
            :class="['chip-btn', { active: localFilters.type === type.value }]"
            @click="selectType(type.value)"
          >
            {{ type.label }}
          </button>
        </div>
      </div>

      <!-- Status Filter (if admin) -->
      <div v-if="showStatusFilter" class="filter-section">
        <label class="filter-label">وضعیت</label>
        <div class="custom-select-wrapper">
          <select v-model="localFilters.status" class="filter-select" @change="handleChange">
            <option :value="undefined">همه وضعیت‌ها</option>
            <option value="Active">فعال</option>
            <option value="Pending">در انتظار</option>
            <option value="Inactive">غیرفعال</option>
            <option value="Suspended">معلق</option>
          </select>
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
          </svg>
        </div>
      </div>

      <!-- Features with Toggle Switches -->
      <div class="filter-section">
        <label class="filter-label">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          امکانات
        </label>
        <div class="toggle-group">
          <label class="toggle-label">
            <input
              v-model="localFilters.allowOnlineBooking"
              type="checkbox"
              class="toggle-checkbox"
              @change="handleChange"
            />
            <span class="toggle-switch"></span>
            <span class="toggle-text">رزرو آنلاین</span>
          </label>
          <label class="toggle-label">
            <input
              v-model="localFilters.offersMobileServices"
              type="checkbox"
              class="toggle-checkbox"
              @change="handleChange"
            />
            <span class="toggle-switch"></span>
            <span class="toggle-text">خدمات سیار</span>
          </label>
        </div>
      </div>

      <!-- Rating Filter with Stars -->
      <div v-if="showRatingFilter" class="filter-section">
        <label class="filter-label">
          <svg xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 24 24">
            <path d="M10.788 3.21c.448-1.077 1.976-1.077 2.424 0l2.082 5.007 5.404.433c1.164.093 1.636 1.545.749 2.305l-4.117 3.527 1.257 5.273c.271 1.136-.964 2.033-1.96 1.425L12 18.354 7.373 21.18c-.996.608-2.231-.29-1.96-1.425l1.257-5.273-4.117-3.527c-.887-.76-.415-2.212.749-2.305l5.404-.433 2.082-5.006z" />
          </svg>
          حداقل امتیاز
        </label>
        <div class="rating-stars">
          <button
            v-for="rating in [5, 4, 3, 2, 1]"
            :key="rating"
            :class="['star-btn', { active: localFilters.minRating === rating }]"
            @click="setMinRating(rating)"
          >
            <div class="stars-display">
              <svg
                v-for="i in rating"
                :key="i"
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 24 24"
                fill="currentColor"
              >
                <path d="M10.788 3.21c.448-1.077 1.976-1.077 2.424 0l2.082 5.007 5.404.433c1.164.093 1.636 1.545.749 2.305l-4.117 3.527 1.257 5.273c.271 1.136-.964 2.033-1.96 1.425L12 18.354 7.373 21.18c-.996.608-2.231-.29-1.96-1.425l1.257-5.273-4.117-3.527c-.887-.76-.415-2.212.749-2.305l5.404-.433 2.082-5.006z" />
              </svg>
            </div>
            <span class="rating-text">و بالاتر</span>
          </button>
        </div>
      </div>

      <!-- Sort By -->
      <div class="filter-section">
        <label class="filter-label">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 4h13M3 8h9m-9 4h9m5-4v12m0 0l-4-4m4 4l4-4" />
          </svg>
          مرتب‌سازی بر اساس
        </label>
        <div class="custom-select-wrapper">
          <select v-model="localFilters.sortBy" class="filter-select" @change="handleChange">
            <option value="rating">بالاترین امتیاز</option>
            <option value="name">نام (الف-ی)</option>
            <option value="distance">فاصله (نزدیک‌ترین)</option>
            <option value="price">قیمت</option>
          </select>
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
          </svg>
        </div>
        <label class="toggle-label" style="margin-top: 0.75rem;">
          <input
            v-model="localFilters.sortDescending"
            type="checkbox"
            class="toggle-checkbox"
            @change="handleChange"
          />
          <span class="toggle-switch"></span>
          <span class="toggle-text">نزولی</span>
        </label>
      </div>
    </div>

    <div class="filters-footer">
      <button class="btn btn-primary btn-block" @click="handleApply">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
        </svg>
        اعمال فیلترها
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import type {
  ProviderSearchFilters,
  ProviderType,
  ProviderStatus,
  PriceRange,
} from '../types/provider.types'

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
  showRatingFilter: true,
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
  pageNumber: 1,
  pageSize: 20,
  searchTerm: '',
  city: '',
  state: '',
  country: '',
  serviceCategory: undefined,
  priceRange: undefined,
  type: undefined,
  hierarchyType: undefined,
  status: undefined,
  allowOnlineBooking: undefined,
  offersMobileServices: undefined,
  tags: [],
  radiusKm: 10,
  minRating: undefined,
  sortBy: 'rating',
  sortDescending: true,
})

const tagInput = ref('')
let debounceTimeout: ReturnType<typeof setTimeout> | null = null

// Voice search state
const isListening = ref(false)
const voiceSearchError = ref('')
const recognition = ref<any>(null)

// Location state
const gettingLocation = ref(false)

// Price ranges
const priceRanges = [
  { value: 'Budget', label: 'اقتصادی', icon: '💰' },
  { value: 'Moderate', label: 'متوسط', icon: '💰💰' },
  { value: 'Premium', label: 'لوکس', icon: '💰💰💰' },
]

// Provider types - hierarchy types (Organization vs Individual)
const providerHierarchyTypes = [
  { value: 'Organization', label: 'سازمان / کسب‌وکار', icon: '🏢' },
  { value: 'Individual', label: 'متخصص فردی', icon: '👤' },
]

// Business types (legacy - for more specific filtering)
const providerTypes = [
  { value: 'Salon', label: 'سالن' },
  { value: 'Clinic', label: 'کلینیک' },
  { value: 'Spa', label: 'اسپا' },
  { value: 'Studio', label: 'استودیو' },
  { value: 'Barbershop', label: 'آرایشگاه' },
  { value: 'BeautySalon', label: 'سالن زیبایی' },
]

// Computed
const hasActiveFilters = computed(() => {
  return Object.entries(localFilters.value).some(([key, value]) => {
    if (key === 'tags') return Array.isArray(value) && value.length > 0
    if (key === 'pageNumber' || key === 'pageSize') return false
    if (key === 'sortBy' || key === 'sortDescending') return false
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
    pageNumber: 1,
    pageSize: 20,
    searchTerm: '',
    city: '',
    state: '',
    country: '',
    serviceCategory: undefined,
    priceRange: undefined,
    type: undefined,
    hierarchyType: undefined,
    status: undefined,
    allowOnlineBooking: undefined,
    offersMobileServices: undefined,
    tags: [],
    radiusKm: 10,
    minRating: undefined,
    sortBy: 'rating',
    sortDescending: true,
  }
  tagInput.value = ''
  emit('clear')

  if (props.autoApply) {
    emit('apply', {
      pageNumber: 1,
      pageSize: 20,
      sortBy: 'rating',
      sortDescending: true,
    })
  }
}

const selectPriceRange = (range: string) => {
  localFilters.value.priceRange = localFilters.value.priceRange === range ? undefined : (range as PriceRange)
  handleChange()
}

const selectType = (type: string) => {
  localFilters.value.type = localFilters.value.type === type ? undefined : (type as ProviderType)
  handleChange()
}

const selectHierarchyType = (hType: string) => {
  localFilters.value.hierarchyType = localFilters.value.hierarchyType === hType ? undefined : hType
  handleChange()
}

const setMinRating = (rating: number) => {
  localFilters.value.minRating = localFilters.value.minRating === rating ? undefined : rating
  handleChange()
}

// Voice Search
const startVoiceSearch = () => {
  if (!('webkitSpeechRecognition' in window) && !('SpeechRecognition' in window)) {
    voiceSearchError.value = 'مرورگر شما از جستجوی صوتی پشتیبانی نمی‌کند'
    return
  }

  const SpeechRecognition = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition
  recognition.value = new SpeechRecognition()
  recognition.value.lang = 'fa-IR'
  recognition.value.continuous = false
  recognition.value.interimResults = false

  recognition.value.onstart = () => {
    isListening.value = true
    voiceSearchError.value = ''
  }

  recognition.value.onresult = (event: any) => {
    const transcript = event.results[0][0].transcript
    localFilters.value.searchTerm = transcript
    handleApply()
    isListening.value = false
  }

  recognition.value.onerror = (event: any) => {
    voiceSearchError.value = 'خطا در جستجوی صوتی'
    isListening.value = false
  }

  recognition.value.onend = () => {
    isListening.value = false
  }

  recognition.value.start()
}

// Geolocation
const useCurrentLocation = () => {
  if (!navigator.geolocation) {
    alert('مرورگر شما از موقعیت یابی پشتیبانی نمی‌کند')
    return
  }

  gettingLocation.value = true

  navigator.geolocation.getCurrentPosition(
    (position) => {
      localFilters.value.latitude = position.coords.latitude
      localFilters.value.longitude = position.coords.longitude
      // Here you would typically reverse geocode to get city/state
      gettingLocation.value = false
      handleChange()
    },
    (error) => {
      alert('خطا در دریافت موقعیت مکانی')
      gettingLocation.value = false
    }
  )
}

const getCleanFilters = (): ProviderSearchFilters => {
  const filters: ProviderSearchFilters = {
    pageNumber: localFilters.value.pageNumber,
    pageSize: localFilters.value.pageSize,
  }

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
  if (localFilters.value.serviceCategory) {
    filters.serviceCategory = localFilters.value.serviceCategory
  }
  if (localFilters.value.priceRange) {
    filters.priceRange = localFilters.value.priceRange
  }
  if (localFilters.value.type) {
    filters.type = localFilters.value.type as ProviderType
  }
  if (localFilters.value.hierarchyType) {
    filters.hierarchyType = localFilters.value.hierarchyType
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
  if (localFilters.value.sortBy) {
    filters.sortBy = localFilters.value.sortBy
  }
  if (localFilters.value.sortDescending !== undefined) {
    filters.sortDescending = localFilters.value.sortDescending
  }

  return filters
}

// Cleanup
const cleanup = () => {
  if (debounceTimeout) {
    clearTimeout(debounceTimeout)
  }
  if (recognition.value) {
    recognition.value.stop()
  }
}

onUnmounted(() => {
  cleanup()
})

// Expose cleanup for parent components
defineExpose({
  cleanup,
  reset: handleClearAll,
})
</script>

<style scoped>
.provider-filters {
  background: white;
  border-radius: 16px;
  overflow: hidden;
  font-family: 'Vazir', 'IRANSans', sans-serif;
}

.filters-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid var(--color-border);
  background: linear-gradient(135deg, var(--color-bg-secondary) 0%, white 100%);
}

.filters-title {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-size: 1.25rem;
  font-weight: 700;
  margin: 0;
  color: var(--color-text-primary);
}

.filters-title svg {
  width: 22px;
  height: 22px;
  color: var(--color-primary);
}

.btn-clear-all {
  padding: 0.625rem 1.25rem;
  background: transparent;
  border: 1.5px solid var(--color-border);
  border-radius: 8px;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  font-family: 'Vazir', 'IRANSans', sans-serif;
}

.btn-clear-all:hover {
  background: var(--color-danger);
  border-color: var(--color-danger);
  color: white;
  transform: translateY(-1px);
}

.filters-body {
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.75rem;
  max-height: calc(100vh - 16rem);
  overflow-y: auto;
}

.filter-section {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.filter-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.95rem;
  font-weight: 700;
  color: var(--color-text-primary);
}

.filter-label svg {
  width: 18px;
  height: 18px;
  color: var(--color-primary);
}

/* Voice Search Button */
.voice-search-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  width: 100%;
  padding: 1rem;
  background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%);
  border: none;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  color: white;
  cursor: pointer;
  transition: all 0.3s ease;
  font-family: 'Vazir', 'IRANSans', sans-serif;
  box-shadow: 0 4px 12px rgba(139, 92, 246, 0.3);
}

.voice-search-btn svg {
  width: 24px;
  height: 24px;
  color: white;
  stroke: white;
}

.voice-search-btn:hover {
  background: linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%);
  box-shadow: 0 6px 16px rgba(139, 92, 246, 0.4);
  transform: translateY(-2px);
}

.voice-search-btn.listening {
  background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
  animation: pulse-bg 1.5s infinite;
  box-shadow: 0 4px 12px rgba(239, 68, 68, 0.4);
}

.voice-search-btn span {
  color: white;
  font-weight: 600;
}

@keyframes pulse-bg {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.8; }
}

.pulse-animation {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
}

.pulse-dot {
  width: 12px;
  height: 12px;
  background: white;
  border-radius: 50%;
  animation: pulse-dot 1.5s infinite;
}

@keyframes pulse-dot {
  0%, 100% {
    transform: scale(1);
    opacity: 1;
  }
  50% {
    transform: scale(1.5);
    opacity: 0.5;
  }
}

.voice-error {
  color: var(--color-danger);
  font-size: 0.875rem;
  margin: 0;
}

/* Search Input */
.search-wrapper {
  position: relative;
}

.search-input {
  width: 100%;
  padding: 0.875rem 3rem 0.875rem 1rem;
  border: 2px solid var(--color-border);
  border-radius: 12px;
  font-size: 0.95rem;
  transition: all 0.3s ease;
  font-family: 'Vazir', 'IRANSans', sans-serif;
  text-align: right;
}

.search-input:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(var(--color-primary-rgb), 0.1);
}

.clear-input-btn {
  position: absolute;
  left: 0.75rem;
  top: 50%;
  transform: translateY(-50%);
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  padding: 0;
  background: var(--color-bg-tertiary);
  border: none;
  border-radius: 50%;
  cursor: pointer;
  transition: all 0.2s;
}

.clear-input-btn svg {
  width: 16px;
  height: 16px;
  color: var(--color-text-tertiary);
}

.clear-input-btn:hover {
  background: var(--color-danger);
}

.clear-input-btn:hover svg {
  color: white;
}

/* Custom Select */
.custom-select-wrapper {
  position: relative;
}

.custom-select-wrapper select {
  width: 100%;
  padding: 0.875rem 3rem 0.875rem 1rem;
  border: 2px solid var(--color-border);
  border-radius: 12px;
  font-size: 0.95rem;
  background: white;
  cursor: pointer;
  transition: all 0.3s ease;
  appearance: none;
  font-family: 'Vazir', 'IRANSans', sans-serif;
  text-align: right;
}

.custom-select-wrapper select:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(var(--color-primary-rgb), 0.1);
}

.custom-select-wrapper svg {
  position: absolute;
  left: 1rem;
  top: 50%;
  transform: translateY(-50%);
  width: 20px;
  height: 20px;
  color: var(--color-text-tertiary);
  pointer-events: none;
}

/* Price Range Buttons */
.price-range-buttons {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0.75rem;
}

.price-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 1rem 0.75rem;
  background: var(--color-bg-secondary);
  border: 2px solid var(--color-border);
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.3s ease;
}

.price-btn:hover {
  background: var(--color-primary-light);
  border-color: var(--color-primary);
  transform: translateY(-2px);
}

.price-btn.active {
  background: var(--color-primary);
  border-color: var(--color-primary);
  color: white;
  box-shadow: 0 4px 12px rgba(var(--color-primary-rgb), 0.3);
}

.price-icon {
  font-size: 1.5rem;
}

.price-label {
  font-size: 0.875rem;
  font-weight: 600;
  font-family: 'Vazir', 'IRANSans', sans-serif;
}

/* Location Inputs */
.location-inputs {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.filter-input {
  width: 100%;
  padding: 0.875rem 1rem;
  border: 2px solid var(--color-border);
  border-radius: 12px;
  font-size: 0.95rem;
  transition: all 0.3s ease;
  font-family: 'Vazir', 'IRANSans', sans-serif;
  text-align: right;
}

.filter-input:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(var(--color-primary-rgb), 0.1);
}

.use-location-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem;
  background: var(--color-bg-secondary);
  border: 2px dashed var(--color-border);
  border-radius: 12px;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: all 0.3s ease;
  font-family: 'Vazir', 'IRANSans', sans-serif;
}

.use-location-btn svg {
  width: 18px;
  height: 18px;
}

.use-location-btn:hover:not(:disabled) {
  background: var(--color-primary);
  border-color: var(--color-primary);
  color: white;
}

.use-location-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* Hierarchy Type Buttons */
.hierarchy-type-btns {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 0.75rem;
}

.hierarchy-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 1rem;
  background: var(--color-bg-secondary);
  border: 2px solid var(--color-border);
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.3s ease;
}

.hierarchy-btn:hover {
  background: var(--color-primary-light);
  border-color: var(--color-primary);
  transform: translateY(-2px);
}

.hierarchy-btn.active {
  background: var(--color-primary);
  border-color: var(--color-primary);
  color: white;
  box-shadow: 0 4px 12px rgba(var(--color-primary-rgb), 0.3);
}

.hierarchy-icon {
  font-size: 1.75rem;
}

.hierarchy-label {
  font-size: 0.875rem;
  font-weight: 600;
  font-family: 'Vazir', 'IRANSans', sans-serif;
  text-align: center;
}

/* Type Chips */
.type-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.chip-btn {
  padding: 0.625rem 1rem;
  background: var(--color-bg-secondary);
  border: 2px solid var(--color-border);
  border-radius: 50px;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: all 0.3s ease;
  font-family: 'Vazir', 'IRANSans', sans-serif;
}

.chip-btn:hover {
  background: var(--color-primary-light);
  border-color: var(--color-primary);
  color: var(--color-primary-dark);
}

.chip-btn.active {
  background: var(--color-primary);
  border-color: var(--color-primary);
  color: white;
}

/* Toggle Switches */
.toggle-group {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.toggle-label {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  cursor: pointer;
  user-select: none;
}

.toggle-checkbox {
  display: none;
}

.toggle-switch {
  position: relative;
  width: 48px;
  height: 26px;
  background: var(--color-border);
  border-radius: 13px;
  transition: all 0.3s ease;
}

.toggle-switch::before {
  content: '';
  position: absolute;
  top: 3px;
  left: 3px;
  width: 20px;
  height: 20px;
  background: white;
  border-radius: 50%;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}

.toggle-checkbox:checked + .toggle-switch {
  background: var(--color-primary);
}

.toggle-checkbox:checked + .toggle-switch::before {
  transform: translateX(22px);
}

.toggle-text {
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--color-text-primary);
  font-family: 'Vazir', 'IRANSans', sans-serif;
}

/* Rating Stars */
.rating-stars {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.star-btn {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem 1rem;
  background: var(--color-bg-secondary);
  border: 2px solid var(--color-border);
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.3s ease;
}

.star-btn:hover {
  background: var(--color-primary-light);
  border-color: var(--color-primary);
  transform: translateX(-4px);
}

.star-btn.active {
  background: var(--color-primary);
  border-color: var(--color-primary);
}

.stars-display {
  display: flex;
  gap: 0.25rem;
}

.stars-display svg {
  width: 18px;
  height: 18px;
  color: #fbbf24;
}

.star-btn.active .stars-display svg {
  color: white;
}

.rating-text {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-text-secondary);
  font-family: 'Vazir', 'IRANSans', sans-serif;
}

.star-btn.active .rating-text {
  color: white;
}

/* Footer */
.filters-footer {
  padding: 1.5rem;
  border-top: 1px solid var(--color-border);
  background: var(--color-bg-secondary);
}

.btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 1rem 1.5rem;
  border: none;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  font-family: 'Vazir', 'IRANSans', sans-serif;
}

.btn svg {
  width: 20px;
  height: 20px;
}

.btn-primary {
  background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark) 100%);
  color: white;
  box-shadow: 0 4px 12px rgba(var(--color-primary-rgb), 0.3);
}

.btn-primary:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(var(--color-primary-rgb), 0.4);
}

.btn-block {
  width: 100%;
}

/* Scrollbar */
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

/* Mobile Optimizations */
@media (max-width: 768px) {
  .filters-header {
    padding: 1.25rem;
  }

  .filters-body {
    padding: 1.25rem;
    max-height: calc(100vh - 14rem);
  }

  .price-range-buttons {
    grid-template-columns: 1fr;
  }

  .type-chips {
    justify-content: stretch;
  }

  .chip-btn {
    flex: 1;
    min-width: 0;
    text-align: center;
  }
}
</style>
