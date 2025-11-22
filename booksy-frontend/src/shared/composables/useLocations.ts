import { ref, computed, onMounted } from 'vue'
import { serviceCategoryClient } from '@/core/api/client/http-client'

export interface Location {
  id: number
  name: string
  provinceCode: number
  cityCode?: number
  parentId: number | null
  type: 'Province' | 'City'
}

const allLocations = ref<Location[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)

export function useLocations() {
  const provinces = computed(() =>
    allLocations.value.filter(loc => loc.type === 'Province')
  )

  const loadProvinces = async () => {
    if (provinces.value.length > 0) return // Already loaded

    isLoading.value = true
    error.value = null

    try {
      const response = await serviceCategoryClient.get<Location[]>('/v1/locations/provinces')
      const provincesData = response.data
      // Add provinces to allLocations
      allLocations.value = [...allLocations.value, ...provincesData]
    } catch (err: any) {
      error.value = err.message || 'Failed to load provinces'
      console.error('Error loading provinces:', err)
    } finally {
      isLoading.value = false
    }
  }

  const loadCitiesByProvinceId = async (provinceId: number) => {
    // Check if cities for this province are already loaded
    const existingCities = allLocations.value.filter(
      loc => loc.type === 'City' && loc.parentId === provinceId
    )

    if (existingCities.length > 0) return // Already loaded

    isLoading.value = true
    error.value = null

    try {
      const response = await serviceCategoryClient.get<Location[]>(`/v1/locations/provinces/${provinceId}/cities`)
      const citiesData = response.data
      // Add cities to allLocations
      allLocations.value = [...allLocations.value, ...citiesData]
    } catch (err: any) {
      error.value = err.message || 'Failed to load cities'
      console.error('Error loading cities:', err)
    } finally {
      isLoading.value = false
    }
  }

  const getCitiesByProvinceId = (provinceId: number): Location[] => {
    return allLocations.value.filter(
      loc => loc.type === 'City' && loc.parentId === provinceId
    )
  }

  const searchProvinces = (query: string): Location[] => {
    if (!query) return provinces.value

    const lowerQuery = query.toLowerCase()
    return provinces.value.filter(province =>
      province.name.toLowerCase().includes(lowerQuery)
    )
  }

  const searchCities = (provinceId: number, query: string): Location[] => {
    const cities = getCitiesByProvinceId(provinceId)

    if (!query) return cities

    const lowerQuery = query.toLowerCase()
    return cities.filter(city =>
      city.name.toLowerCase().includes(lowerQuery)
    )
  }

  const getLocationById = (id: number): Location | undefined => {
    return allLocations.value.find(loc => loc.id === id)
  }

  const getProvinceByName = (name: string): Location | undefined => {
    return provinces.value.find(p => p.name === name)
  }

  const getCityByName = (provinceId: number, cityName: string): Location | undefined => {
    const cities = getCitiesByProvinceId(provinceId)
    return cities.find(c => c.name === cityName)
  }

  // Load provinces on mount
  onMounted(async () => {
    await loadProvinces()
  })

  return {
    provinces,
    allLocations,
    isLoading,
    error,
    loadProvinces,
    loadCitiesByProvinceId,
    getCitiesByProvinceId,
    searchProvinces,
    searchCities,
    getLocationById,
    getProvinceByName,
    getCityByName,
  }
}
