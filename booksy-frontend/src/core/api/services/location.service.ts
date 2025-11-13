/**
 * Location Service
 * Handles Iranian location data (provinces and cities)
 */

import { httpClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'

const API_VERSION = 'v1'
const API_BASE = `/${API_VERSION}/locations`

// ==================== Types ====================

/**
 * Iranian Province
 */
export interface Province {
  id: string
  name: string
  nameEn?: string
  code?: string
  latitude?: number
  longitude?: number
  population?: number
}

/**
 * Iranian City
 */
export interface City {
  id: string
  provinceId: string
  name: string
  nameEn?: string
  code?: string
  latitude?: number
  longitude?: number
  population?: number
  isCapital?: boolean
}

/**
 * Location autocomplete result
 */
export interface LocationSearchResult {
  id: string
  name: string
  type: 'province' | 'city'
  provinceId?: string
  provinceName?: string
}

// ==================== Location Service Class ====================

class LocationService {
  // Cache for provinces and cities to reduce API calls
  private provincesCache: Province[] | null = null
  private citiesCache: Map<string, City[]> = new Map()

  // ============================================
  // Provinces
  // ============================================

  /**
   * Get all Iranian provinces
   * GET /api/v1/locations/provinces
   *
   * Example response:
   * [
   *   {
   *     "id": "province-1",
   *     "name": "تهران",
   *     "nameEn": "Tehran",
   *     "code": "08",
   *     "latitude": 35.6892,
   *     "longitude": 51.3890
   *   },
   *   ...
   * ]
   */
  async getProvinces(useCache = true): Promise<Province[]> {
    try {
      // Return cached data if available
      if (useCache && this.provincesCache) {
        console.log('[LocationService] Returning cached provinces')
        return this.provincesCache
      }

      console.log('[LocationService] Fetching provinces from API')

      const response = await httpClient.get<ApiResponse<Province[]>>(
        `${API_BASE}/provinces`
      )

      console.log('[LocationService] Provinces retrieved:', response.data)

      // Handle wrapped response format
      const provinces = response.data?.data || response.data
      const provincesList = provinces as Province[]

      // Cache the results
      this.provincesCache = provincesList

      return provincesList
    } catch (error) {
      console.error('[LocationService] Error fetching provinces:', error)
      throw this.handleError(error)
    }
  }

  /**
   * Get province by ID
   */
  async getProvinceById(id: string): Promise<Province | null> {
    const provinces = await this.getProvinces()
    return provinces.find(p => p.id === id) || null
  }

  /**
   * Get province by name
   */
  async getProvinceByName(name: string): Promise<Province | null> {
    const provinces = await this.getProvinces()
    return provinces.find(p =>
      p.name === name || p.nameEn?.toLowerCase() === name.toLowerCase()
    ) || null
  }

  // ============================================
  // Cities
  // ============================================

  /**
   * Get cities for a specific province
   * GET /api/v1/locations/provinces/{provinceId}/cities
   *
   * Example response:
   * [
   *   {
   *     "id": "city-1",
   *     "provinceId": "province-1",
   *     "name": "تهران",
   *     "nameEn": "Tehran",
   *     "code": "0801",
   *     "latitude": 35.6892,
   *     "longitude": 51.3890,
   *     "isCapital": true
   *   },
   *   ...
   * ]
   */
  async getCitiesByProvince(provinceId: string, useCache = true): Promise<City[]> {
    try {
      // Return cached data if available
      if (useCache && this.citiesCache.has(provinceId)) {
        console.log(`[LocationService] Returning cached cities for province: ${provinceId}`)
        return this.citiesCache.get(provinceId)!
      }

      console.log(`[LocationService] Fetching cities for province: ${provinceId}`)

      const response = await httpClient.get<ApiResponse<City[]>>(
        `${API_BASE}/provinces/${provinceId}/cities`
      )

      console.log('[LocationService] Cities retrieved:', response.data)

      // Handle wrapped response format
      const cities = response.data?.data || response.data
      const citiesList = cities as City[]

      // Cache the results
      this.citiesCache.set(provinceId, citiesList)

      return citiesList
    } catch (error) {
      console.error(`[LocationService] Error fetching cities for province ${provinceId}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Get all cities (from all provinces)
   * Warning: This may be a large dataset
   */
  async getAllCities(): Promise<City[]> {
    const provinces = await this.getProvinces()
    const allCities: City[] = []

    for (const province of provinces) {
      const cities = await this.getCitiesByProvince(province.id)
      allCities.push(...cities)
    }

    return allCities
  }

  /**
   * Get city by ID
   */
  async getCityById(cityId: string, provinceId?: string): Promise<City | null> {
    if (provinceId) {
      const cities = await this.getCitiesByProvince(provinceId)
      return cities.find(c => c.id === cityId) || null
    }

    // Search all provinces
    const allCities = await this.getAllCities()
    return allCities.find(c => c.id === cityId) || null
  }

  /**
   * Search cities by name
   */
  async searchCities(query: string, provinceId?: string): Promise<City[]> {
    const cities = provinceId
      ? await this.getCitiesByProvince(provinceId)
      : await this.getAllCities()

    const lowerQuery = query.toLowerCase()
    return cities.filter(city =>
      city.name.includes(query) ||
      city.nameEn?.toLowerCase().includes(lowerQuery)
    )
  }

  // ============================================
  // Search & Autocomplete
  // ============================================

  /**
   * Search locations (provinces and cities)
   * Useful for autocomplete functionality
   */
  async searchLocations(query: string): Promise<LocationSearchResult[]> {
    const results: LocationSearchResult[] = []

    // Search provinces
    const provinces = await this.getProvinces()
    const matchingProvinces = provinces.filter(p =>
      p.name.includes(query) ||
      p.nameEn?.toLowerCase().includes(query.toLowerCase())
    )

    results.push(...matchingProvinces.map(p => ({
      id: p.id,
      name: p.name,
      type: 'province' as const,
    })))

    // Search cities (limited to first 20 results for performance)
    const allCities = await this.getAllCities()
    const matchingCities = allCities
      .filter(c =>
        c.name.includes(query) ||
        c.nameEn?.toLowerCase().includes(query.toLowerCase())
      )
      .slice(0, 20)

    for (const city of matchingCities) {
      const province = await this.getProvinceById(city.provinceId)
      results.push({
        id: city.id,
        name: city.name,
        type: 'city',
        provinceId: city.provinceId,
        provinceName: province?.name,
      })
    }

    return results
  }

  // ============================================
  // Utility Methods
  // ============================================

  /**
   * Get capital cities
   */
  async getCapitalCities(): Promise<City[]> {
    const allCities = await this.getAllCities()
    return allCities.filter(city => city.isCapital)
  }

  /**
   * Clear cache
   */
  clearCache(): void {
    this.provincesCache = null
    this.citiesCache.clear()
    console.log('[LocationService] Cache cleared')
  }

  /**
   * Get formatted address string
   */
  formatAddress(city: City, province: Province, addressLine?: string): string {
    const parts = []

    if (addressLine) parts.push(addressLine)
    parts.push(city.name)
    parts.push(province.name)

    return parts.join('، ')
  }

  /**
   * Get formatted address from IDs
   */
  async formatAddressFromIds(
    provinceId: string,
    cityId: string,
    addressLine?: string
  ): Promise<string> {
    const province = await this.getProvinceById(provinceId)
    const city = await this.getCityById(cityId, provinceId)

    if (!province || !city) {
      throw new Error('Invalid province or city ID')
    }

    return this.formatAddress(city, province, addressLine)
  }

  // ============================================
  // Error Handling
  // ============================================

  /**
   * Centralized error handling
   */
  private handleError(error: unknown): Error {
    if (error instanceof Error) {
      return error
    }
    return new Error('خطا در بارگذاری اطلاعات مکانی')
  }
}

// Export singleton instance
export const locationService = new LocationService()
export default locationService
