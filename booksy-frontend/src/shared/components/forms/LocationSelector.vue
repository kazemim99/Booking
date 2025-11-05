<template>
  <div class="location-selector">
    <SearchableSelect
      v-model="selectedProvinceId"
      :options="provinceOptions"
      :label="provinceLabel"
      :placeholder="provincePlaceholder"
      :error="provinceError"
      :required="required"
      :disabled="disabled"
      @update:model-value="onProvinceChange"
    />

    <SearchableSelect
      v-model="selectedCityId"
      :options="cityOptions"
      :label="cityLabel"
      :placeholder="cityPlaceholder"
      :error="cityError"
      :required="required"
      :disabled="disabled || !selectedProvinceId"
      class="city-select"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import SearchableSelect, { type SelectOption } from './SearchableSelect.vue'
import { useLocations } from '@/shared/composables/useLocations'

interface Props {
  provinceId?: number | null
  cityId?: number | null
  provinceLabel?: string
  cityLabel?: string
  provincePlaceholder?: string
  cityPlaceholder?: string
  provinceError?: string
  cityError?: string
  required?: boolean
  disabled?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  provinceLabel: 'Province',
  cityLabel: 'City',
  provincePlaceholder: 'Select province...',
  cityPlaceholder: 'Select city...',
})

const emit = defineEmits<{
  'update:provinceId': [value: number | null]
  'update:cityId': [value: number | null]
}>()

const { provinces, getCitiesByProvinceId, loadCitiesByProvinceId, loadProvinces } = useLocations()

const selectedProvinceId = ref<number | null>(props.provinceId || null)
const selectedCityId = ref<number | null>(props.cityId || null)

// Province options for dropdown
const provinceOptions = computed<SelectOption[]>(() =>
  provinces.value.map(province => ({
    label: province.name,
    value: province.id,
  }))
)

// City options based on selected province
const cityOptions = computed<SelectOption[]>(() => {
  if (!selectedProvinceId.value) return []

  const cities = getCitiesByProvinceId(selectedProvinceId.value)
  return cities.map(city => ({
    label: city.name,
    value: city.id,
  }))
})

// Load provinces on mount
onMounted(async () => {
  await loadProvinces()

  // If provinceId is provided, load cities for that province
  if (props.provinceId) {
    selectedProvinceId.value = props.provinceId
    await loadCitiesByProvinceId(props.provinceId)

    // After cities are loaded, set cityId if provided
    if (props.cityId) {
      selectedCityId.value = props.cityId
    }
  }
})

// Watch for external changes
watch(() => props.provinceId, async (newValue, oldValue) => {
  selectedProvinceId.value = newValue || null

  // Load cities when province changes externally
  if (newValue) {
    await loadCitiesByProvinceId(newValue)

    // After cities are loaded, restore cityId if it was provided
    if (props.cityId) {
      selectedCityId.value = props.cityId
    }
  }
})

watch(() => props.cityId, (newValue) => {
  // Only update if cities are loaded (cityOptions has items)
  if (cityOptions.value.length > 0 || !newValue) {
    selectedCityId.value = newValue || null
  }
})

// Watch for internal changes and emit
watch(selectedProvinceId, (newValue) => {
  emit('update:provinceId', newValue)
})

watch(selectedCityId, (newValue) => {
  emit('update:cityId', newValue)
})

async function onProvinceChange(value: number | string | null) {
  selectedProvinceId.value = value as number | null
  // Reset city when province changes
  selectedCityId.value = null
  emit('update:cityId', null)

  // Load cities for the selected province
  if (value) {
    await loadCitiesByProvinceId(value as number)
  }
}
</script>

<style scoped>
.location-selector {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.city-select {
  /* Additional city-specific styles if needed */
}

@media (min-width: 768px) {
  .location-selector {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: var(--spacing-lg);
  }
}
</style>
