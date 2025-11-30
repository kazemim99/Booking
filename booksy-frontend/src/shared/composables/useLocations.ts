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

export interface City {
  id: number
  name: string
  cityCode?: number
}

export interface ProvinceHierarchy {
  id: number
  name: string
  provinceCode: number
  cities: City[]
}

const allLocations = ref<Location[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)
const hierarchyLoaded = ref(false)

export function useLocations() {
  const provinces = computed(() =>
    allLocations.value.filter(loc => loc.type === 'Province')
  )

  /**
   * ✅ OPTIMIZED: Load all provinces with their cities in ONE API call
   * Replaces the old approach of 32 separate API calls
   */
  const loadProvinces = async () => {
    if (hierarchyLoaded.value) return // Already loaded

    isLoading.value = true
    error.value = null

    try {
      // ✅ Single API call to get all provinces with nested cities
      const response = await serviceCategoryClient.get<ProvinceHierarchy[]>('/v1/locations/hierarchy')
      const hierarchy = response.data

      // Transform hierarchy into flat array of locations
      const locations: Location[] = []

      hierarchy.forEach(province => {
        // Add province
        locations.push({
          id: province.id,
          name: province.name,
          provinceCode: province.provinceCode,
          parentId: null,
          type: 'Province'
        })

        // Add all cities for this province
        province.cities.forEach(city => {
          locations.push({
            id: city.id,
            name: city.name,
            provinceCode: province.provinceCode,
            cityCode: city.cityCode,
            parentId: province.id,
            type: 'City'
          })
        })
      })

      allLocations.value = locations
      hierarchyLoaded.value = true

      console.log(`✅ Loaded ${hierarchy.length} provinces with ${locations.filter(l => l.type === 'City').length} cities in 1 API call`)
    } catch (err: any) {
      error.value = err.message || 'Failed to load locations'
      console.error('Error loading locations:', err)
    } finally {
      isLoading.value = false
    }
  }

  /**
   * ✅ OPTIMIZED: No API call needed - data already loaded from hierarchy
   * This is now a no-op since all cities are loaded with loadProvinces()
   */
  const loadCitiesByProvinceId = async (provinceId: number) => {
    // Check if cities for this province are already loaded
    const existingCities = allLocations.value.filter(
      loc => loc.type === 'City' && loc.parentId === provinceId
    )

    if (existingCities.length > 0) {
      console.log(`✅ Cities for province ${provinceId} already loaded (${existingCities.length} cities)`)
      return // Already loaded
    }

    // If hierarchy not loaded yet, load it
    if (!hierarchyLoaded.value) {
      await loadProvinces()
      return
    }

    // If we get here and still no cities, log warning
    console.warn(`No cities found for province ${provinceId}`)
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

  // ❌ REMOVED: onMounted auto-loading causes multiple API calls
  // Components should explicitly call loadProvinces() when needed

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
