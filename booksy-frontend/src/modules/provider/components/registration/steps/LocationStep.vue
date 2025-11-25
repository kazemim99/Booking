<template>
  <div class="location-step">
    <div class="step-container">
      <div class="step-header">
        <h2 class="step-title">موقعیت مکانی</h2>
        <p class="step-description">آدرس و موقعیت کسب‌و‌کار خود را مشخص کنید</p>
      </div>

      <form class="step-content" @submit.prevent="handleSubmit">
        <!-- City Search -->
        <div class="form-group">
          <label class="form-label required">شهر</label>
          <SearchableSelect
            v-model="formData.cityId"
            :options="allCityOptions"
            label=""
            placeholder="جستجوی شهر..."
            :error="errors.city"
            :required="true"
            @update:model-value="handleCityChange"
          />
          <span class="form-hint">برای یافتن شهر مورد نظر تایپ کنید</span>
        </div>

        <!-- Neshan Map Picker -->
        <div class="form-group">
          <label class="form-label">موقعیت روی نقشه</label>
          <p class="form-hint">روی نقشه کلیک کنید تا موقعیت دقیق کسب‌وکار خود را انتخاب کنید</p>
          <NeshanMapPicker
            v-model="formData.coordinates"
            :map-key="neshanMapKey"
            :service-key="neshanServiceKey"
            height="450px"
            @location-selected="handleLocationSelected"
          />
        </div>

        <!-- Address -->
        <div class="form-group">
          <label for="address" class="form-label required">آدرس دقیق</label>
          <input
            id="address"
            v-model="formData.address"
            type="text"
            class="form-input"
            placeholder="مثال: خیابان ولیعصر، کوچه پنجم، پلاک ۱۲"
            @blur="validateField('address')"
          />
          <span v-if="errors.address" class="form-error">{{ errors.address }}</span>
        </div>

        <!-- Postal Code -->
        <div class="form-group">
          <label for="postalCode" class="form-label">کد پستی (اختیاری)</label>
          <input
            id="postalCode"
            v-model="formData.postalCode"
            type="text"
            dir="ltr"
            class="form-input"
            placeholder="1234567890"
            maxlength="10"
          />
        </div>

        <!-- Actions -->
        <div class="step-actions">
          <AppButton type="button" variant="secondary" size="large" @click="$emit('back')">
            ← بازگشت
          </AppButton>
          <AppButton type="submit" variant="primary" size="large" :disabled="!isFormValid">
            ادامه →
          </AppButton>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import NeshanMapPicker from '@/shared/components/map/NeshanMapPicker.vue'
import SearchableSelect, { type SelectOption } from '@/shared/components/forms/SearchableSelect.vue'
import type { BusinessAddress, BusinessLocation } from '@/modules/provider/types/registration.types'
import { useLocations } from '@/shared/composables/useLocations'

interface Props {
  address?: Partial<BusinessAddress>
  location?: Partial<BusinessLocation>
}

interface Emits {
  (e: 'update:address', value: Partial<BusinessAddress>): void
  (e: 'update:location', value: Partial<BusinessLocation>): void
  (e: 'next'): void
  (e: 'back'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

// Location composable for province/city data
const locationStore = useLocations()

// Neshan Map API keys - same as ProfileManager
const neshanMapKey = import.meta.env.VITE_NESHAN_MAP_KEY || 'web.741ff28152504624a0b3942d3621b56d'
const neshanServiceKey =
  import.meta.env.VITE_NESHAN_SERVICE_KEY || 'service.qBDJpu7hKVBEAzERghfm9JM7vqGKXoNNNTdtrGy7'

// Flag to prevent circular updates between map and form
const isUpdatingFromMap = ref(false)

// Form data
const formData = ref({
  provinceId: props.address?.provinceId || null as number | null,
  cityId: props.address?.cityId || null as number | null,
  address: props.address?.addressLine1 || '',
  postalCode: props.address?.zipCode || '',
  coordinates: props.location?.latitude && props.location?.longitude
    ? { lat: props.location.latitude, lng: props.location.longitude }
    : null as { lat: number; lng: number } | null,
  formattedAddress: props.address?.formattedAddress || '',
})

const errors = ref<Record<string, string>>({})

// Get all cities from all provinces for searchable dropdown
const allCityOptions = computed<SelectOption[]>(() => {
  const allCities: SelectOption[] = []

  // Get all provinces
  const provinces = locationStore.provinces.value

  // For each province, get its cities
  provinces.forEach(province => {
    const cities = locationStore.getCitiesByProvinceId(province.id)
    cities.forEach(city => {
      allCities.push({
        label: `${city.name} (${province.name})`,
        value: city.id,
      })
    })
  })

  return allCities
})

// Handle location selection from map
const handleLocationSelected = async (data: {
  lat: number
  lng: number
  address?: string
  addressDetails?: {
    formattedAddress: string
    neighbourhood: string
    city: string
    state: string
    address: string
    route: string
    district: string
    village: string
    county: string
    postalCode: string
  } | null
}) => {
  console.log('Location selected:', data)
  formData.value.coordinates = { lat: data.lat, lng: data.lng }

  // Auto-fill address if available
  if (data.addressDetails) {
    // Always update address from formatted_address when clicking map
    formData.value.formattedAddress = data.addressDetails.formattedAddress || data.address || ''
    formData.value.address = data.addressDetails.formattedAddress || data.addressDetails.address || ''

    if (data.addressDetails.postalCode) {
      formData.value.postalCode = data.addressDetails.postalCode
    }

    // Auto-select province from reverse geocoded state name
    if (data.addressDetails.state) {
      const province = locationStore.getProvinceByName(data.addressDetails.state)
      if (province) {
        // Temporarily disable watchers to prevent circular updates
        isUpdatingFromMap.value = true
        formData.value.provinceId = province.id
        errors.value.province = ''

        // Load cities for this province
        await locationStore.loadCitiesByProvinceId(province.id)

        // Auto-select city from reverse geocoded city name
        if (data.addressDetails.city) {
          const cities = locationStore.getCitiesByProvinceId(province.id)
          const city = cities.find(c => c.name === data.addressDetails.city)
          if (city) {
            formData.value.cityId = city.id
            errors.value.city = ''
          }
        }

        // Re-enable watchers after a delay
        setTimeout(() => {
          isUpdatingFromMap.value = false
        }, 100)
      }
    }
  } else if (data.address) {
    formData.value.formattedAddress = data.address
    formData.value.address = data.address
  }
}

// Handle city change
const handleCityChange = (cityId: string | number | null) => {
  if (typeof cityId === 'string') {
    formData.value.cityId = parseInt(cityId, 10)
  } else {
    formData.value.cityId = cityId
  }

  // Auto-set province when city is selected
  if (formData.value.cityId) {
    const city = locationStore.getLocationById(formData.value.cityId)
    if (city?.parentId) {
      formData.value.provinceId = city.parentId
    }
  }

  errors.value.city = ''
}

// Helper function to geocode location name using Neshan Search API
const geocodeLocationName = async (locationName: string): Promise<{ lat: number; lng: number } | null> => {
  if (!neshanServiceKey || !locationName) return null

  try {
    const response = await fetch(
      `https://api.neshan.org/v1/search?term=${encodeURIComponent(locationName)}&lat=35.6892&lng=51.389`,
      {
        headers: {
          'Api-Key': neshanServiceKey,
        },
      }
    )

    if (!response.ok) {
      console.error('Neshan search failed:', response.statusText)
      return null
    }

    const data = await response.json()

    if (data.items && data.items.length > 0) {
      const firstResult = data.items[0]
      return {
        lat: firstResult.location.y,
        lng: firstResult.location.x,
      }
    }

    return null
  } catch (error) {
    console.error('Geocoding error:', error)
    return null
  }
}

// Watch province changes to update map
watch(
  () => formData.value.provinceId,
  async (newProvinceId) => {
    // Skip if update is coming from map click
    if (isUpdatingFromMap.value || !newProvinceId) return

    // Get province name and geocode it
    const province = locationStore.getLocationById(newProvinceId)
    if (province) {
      const coordinates = await geocodeLocationName(province.name)
      if (coordinates) {
        formData.value.coordinates = coordinates
      }
    }
  }
)

// Watch city changes to update map
watch(
  () => formData.value.cityId,
  async (newCityId) => {
    // Skip if update is coming from map click
    if (isUpdatingFromMap.value || !newCityId) return

    // Get city and province names for better geocoding accuracy
    const city = locationStore.getLocationById(newCityId)
    const province = formData.value.provinceId ? locationStore.getLocationById(formData.value.provinceId) : null

    if (city) {
      // Combine city and province name for more accurate results
      const searchTerm = province ? `${city.name}, ${province.name}` : city.name
      const coordinates = await geocodeLocationName(searchTerm)
      if (coordinates) {
        formData.value.coordinates = coordinates
      }
    }
  }
)

// Validation
const validateField = (field: keyof typeof formData.value) => {
  errors.value = { ...errors.value }

  switch (field) {
    case 'address':
      if (!formData.value.address?.trim()) {
        errors.value.address = 'آدرس الزامی است'
      } else {
        delete errors.value.address
      }
      break
  }
}

const validateForm = (): boolean => {
  errors.value = {}

  if (!formData.value.cityId) {
    errors.value.city = 'شهر الزامی است'
  }

  if (!formData.value.address?.trim()) {
    errors.value.address = 'آدرس الزامی است'
  }

  return Object.keys(errors.value).length === 0
}

const isFormValid = computed(() => {
  return (
    formData.value.cityId !== null &&
    formData.value.address?.trim() !== ''
  )
})

// Update parent when form data changes
watch(
  formData,
  (newValue) => {
    // Get province and city names from locationStore
    const provinceName = newValue.provinceId
      ? locationStore.getLocationById(newValue.provinceId)?.name
      : undefined
    const cityName = newValue.cityId
      ? locationStore.getLocationById(newValue.cityId)?.name
      : undefined

    // Emit address update
    emit('update:address', {
      addressLine1: newValue.address,
      addressLine2: undefined,
      city: cityName,
      state: provinceName,
      postalCode: newValue.postalCode,
      country: 'IR',
      formattedAddress: newValue.formattedAddress,
      provinceId: newValue.provinceId || undefined,
      cityId: newValue.cityId || undefined,
    })

    // Emit location update
    if (newValue.coordinates) {
      emit('update:location', {
        latitude: newValue.coordinates.lat,
        longitude: newValue.coordinates.lng,
      })
    }
  },
  { deep: true }
)

const handleSubmit = () => {
  if (validateForm()) {
    emit('next')
  }
}

// Initialize from props on mount
onMounted(async () => {
  // Load all provinces and their cities for the searchable dropdown
  await locationStore.loadProvinces()

  // Load cities for all provinces
  const provinces = locationStore.provinces.value
  for (const province of provinces) {
    await locationStore.loadCitiesByProvinceId(province.id)
  }

  if (props.address?.provinceId) {
    formData.value.provinceId = props.address.provinceId
  }
  if (props.address?.cityId) {
    formData.value.cityId = props.address.cityId
  }
})
</script>

<style scoped lang="scss">
@import './steps-common.scss';
</style>
