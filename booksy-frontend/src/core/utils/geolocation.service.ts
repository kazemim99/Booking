/**
 * Geolocation Service
 * Handles browser geolocation API and reverse geocoding using Neshan Maps
 */

// ==================== Types ====================

export interface GeolocationPosition {
  latitude: number
  longitude: number
  accuracy: number
  timestamp: number
}

export interface IPGeolocationResult {
  latitude: number
  longitude: number
  city: string
  country: string
  countryCode: string
  timezone: string
  accuracy: number
}

export interface ReverseGeocodeResult {
  city: string
  province: string
  district?: string
  neighbourhood?: string
  formatted_address: string
  route_name?: string
}

export interface GeolocationError {
  code: 'PERMISSION_DENIED' | 'POSITION_UNAVAILABLE' | 'TIMEOUT' | 'NOT_SUPPORTED' | 'GEOCODING_FAILED'
  message: string
}

export interface GeolocationOptions {
  enableHighAccuracy?: boolean
  timeout?: number
  maximumAge?: number
}

// ==================== Geolocation Service Class ====================

class GeolocationService {
  private readonly NESHAN_REVERSE_GEOCODE_URL = 'https://api.neshan.org/v5/reverse'
  private readonly NESHAN_GEOCODING_URL = 'https://api.neshan.org/geocoding'
  private readonly DEFAULT_TIMEOUT = 10000 // 10 seconds
  private readonly CACHE_MAX_AGE = 300000 // 5 minutes

  // IP-based geolocation APIs (fallback only, not accurate)
  private readonly IP_GEOLOCATION_APIS = [
    'https://ipapi.co/json/',
    'https://ip-api.com/json/',
    'https://geolocation-db.com/json/',
  ]

  private cachedPosition: GeolocationPosition | null = null
  private cachedAddress: ReverseGeocodeResult | null = null
  private cachedIPLocation: IPGeolocationResult | null = null

  /**
   * Check if browser supports geolocation
   */
  isSupported(): boolean {
    return 'geolocation' in navigator
  }

  /**
   * Get user's current position using browser Geolocation API
   *
   * @param options - Geolocation options
   * @returns Promise with latitude and longitude
   *
   * @example
   * ```typescript
   * const position = await geolocationService.getCurrentPosition()
   * console.log(position.latitude, position.longitude)
   * ```
   */
  async getCurrentPosition(options?: GeolocationOptions): Promise<GeolocationPosition> {
    if (!this.isSupported()) {
      throw this.createError('NOT_SUPPORTED', 'مرورگر شما از موقعیت‌یابی پشتیبانی نمی‌کند')
    }

    // Return cached position if still valid
    if (this.cachedPosition && this.isCacheValid(this.cachedPosition.timestamp, options?.maximumAge)) {
      console.log('[GeolocationService] Returning cached position')
      return this.cachedPosition
    }

    const geoOptions: PositionOptions = {
      enableHighAccuracy: options?.enableHighAccuracy ?? true,
      timeout: options?.timeout ?? this.DEFAULT_TIMEOUT,
      maximumAge: options?.maximumAge ?? this.CACHE_MAX_AGE,
    }

    try {
      console.log('[GeolocationService] Requesting current position...')

      const position = await new Promise<GeolocationPosition>((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(
          (pos) => {
            const result: GeolocationPosition = {
              latitude: pos.coords.latitude,
              longitude: pos.coords.longitude,
              accuracy: pos.coords.accuracy,
              timestamp: pos.timestamp,
            }
            resolve(result)
          },
          (error) => {
            reject(this.handleGeolocationError(error))
          },
          geoOptions
        )
      })

      // Cache the result
      this.cachedPosition = position
      console.log('[GeolocationService] Position retrieved:', position)

      return position
    } catch (error) {
      console.error('[GeolocationService] Error getting position:', error)
      throw error
    }
  }

  /**
   * Reverse geocode coordinates to address using Neshan Maps API
   *
   * @param latitude - Latitude coordinate
   * @param longitude - Longitude coordinate
   * @returns Promise with address details
   *
   * @example
   * ```typescript
   * const address = await geolocationService.reverseGeocode(35.6892, 51.3890)
   * console.log(address.city) // "تهران"
   * ```
   */
  async reverseGeocode(latitude: number, longitude: number): Promise<ReverseGeocodeResult> {
    const neshanApiKey = import.meta.env.VITE_NESHAN_SERVICE_KEY

    if (!neshanApiKey) {
      throw this.createError('GEOCODING_FAILED', 'کلید API نشان تنظیم نشده است')
    }

    try {
      console.log(`[GeolocationService] Reverse geocoding: ${latitude}, ${longitude}`)

      const url = `${this.NESHAN_REVERSE_GEOCODE_URL}?lat=${latitude}&lng=${longitude}`

      const response = await fetch(url, {
        method: 'GET',
        headers: {
          'Api-Key': neshanApiKey,
        },
      })

      if (!response.ok) {
        throw new Error(`Neshan API error: ${response.status} ${response.statusText}`)
      }

      const data = await response.json()
      console.log('[GeolocationService] Reverse geocode result:', data)

      // Neshan API response structure - handle nested data
      // Sometimes the response is wrapped in a data property
      const responseData = data.data || data

      // Extract city name with multiple fallbacks
      // Try: city -> municipal_area -> district -> locality -> neighbourhood
      const cityName =
        responseData.city ||
        responseData.municipal_area ||
        responseData.district ||
        responseData.locality ||
        responseData.neighbourhood ||
        'نامشخص'

      const result: ReverseGeocodeResult = {
        city: cityName,
        province: responseData.state || responseData.province || 'نامشخص',
        district: responseData.district,
        neighbourhood: responseData.neighbourhood,
        formatted_address: responseData.formatted_address || responseData.address || '',
        route_name: responseData.route_name,
      }

      // Cache the result
      this.cachedAddress = result

      return result
    } catch (error) {
      console.error('[GeolocationService] Reverse geocoding failed:', error)
      throw this.createError('GEOCODING_FAILED', 'خطا در تبدیل موقعیت به آدرس')
    }
  }

  /**
   * Get location using IP-based geolocation (works in Iran without Google services)
   * This is a fallback when browser geolocation is blocked or unavailable
   *
   * @returns Promise with position from IP geolocation
   *
   * @example
   * ```typescript
   * const location = await geolocationService.getLocationByIP()
   * console.log(location.city) // "Tehran"
   * console.log(location.latitude, location.longitude)
   * ```
   */
  async getLocationByIP(): Promise<IPGeolocationResult> {
    // Return cached IP location if still valid
    if (this.cachedIPLocation) {
      console.log('[GeolocationService] Returning cached IP location')
      return this.cachedIPLocation
    }

    console.log('[GeolocationService] Getting location by IP...')

    // Try multiple IP geolocation APIs until one works
    for (const apiUrl of this.IP_GEOLOCATION_APIS) {
      try {
        console.log(`[GeolocationService] Trying IP API: ${apiUrl}`)

        const response = await fetch(apiUrl, {
          method: 'GET',
          headers: {
            'Accept': 'application/json',
          },
        })

        if (!response.ok) {
          console.warn(`[GeolocationService] IP API failed: ${response.status}`)
          continue // Try next API
        }

        const data = await response.json()
        console.log('[GeolocationService] IP geolocation result:', data)

        // Parse different API response formats
        let result: IPGeolocationResult

        if (apiUrl.includes('ipapi.co')) {
          // ipapi.co format
          result = {
            latitude: parseFloat(data.latitude),
            longitude: parseFloat(data.longitude),
            city: data.city || 'نامشخص',
            country: data.country_name || data.country || 'Iran',
            countryCode: data.country_code || 'IR',
            timezone: data.timezone || 'Asia/Tehran',
            accuracy: 5000, // IP-based is less accurate (~5km)
          }
        } else if (apiUrl.includes('ip-api.com')) {
          // ip-api.com format
          result = {
            latitude: data.lat,
            longitude: data.lon,
            city: data.city || 'نامشخص',
            country: data.country || 'Iran',
            countryCode: data.countryCode || 'IR',
            timezone: data.timezone || 'Asia/Tehran',
            accuracy: 5000,
          }
        } else {
          // geolocation-db.com format
          result = {
            latitude: parseFloat(data.latitude),
            longitude: parseFloat(data.longitude),
            city: data.city || 'نامشخص',
            country: data.country_name || 'Iran',
            countryCode: data.country_code || 'IR',
            timezone: 'Asia/Tehran',
            accuracy: 5000,
          }
        }

        // Cache the result
        this.cachedIPLocation = result
        console.log('[GeolocationService] IP location detected:', result)

        return result
      } catch (error) {
        console.warn(`[GeolocationService] IP API error: ${apiUrl}`, error)
        // Continue to next API
      }
    }

    // All APIs failed
    throw this.createError('POSITION_UNAVAILABLE', 'امکان تشخیص موقعیت از طریق IP وجود ندارد')
  }

  /**
   * Get current position and reverse geocode it in one call
   * Falls back to IP-based geolocation if browser geolocation fails (useful in Iran)
   *
   * @param options - Geolocation options
   * @param useIPFallback - Use IP geolocation if browser geolocation fails
   * @returns Promise with position and address
   *
   * @example
   * ```typescript
   * const { position, address } = await geolocationService.getCurrentLocationAndAddress()
   * console.log(address.city) // Auto-detected city
   * ```
   */
  async getCurrentLocationAndAddress(
    options?: GeolocationOptions,
    useIPFallback: boolean = true
  ): Promise<{
    position: GeolocationPosition
    address: ReverseGeocodeResult
  }> {
    try {
      // Try browser geolocation first
      const position = await this.getCurrentPosition(options)
      const address = await this.reverseGeocode(position.latitude, position.longitude)

      return { position, address }
    } catch (error) {
      console.warn('[GeolocationService] Browser geolocation failed, trying IP fallback...')

      if (!useIPFallback) {
        throw error
      }

      // Fallback to IP-based geolocation
      try {
        const ipLocation = await this.getLocationByIP()

        // Convert IP location to position format
        const position: GeolocationPosition = {
          latitude: ipLocation.latitude,
          longitude: ipLocation.longitude,
          accuracy: ipLocation.accuracy,
          timestamp: Date.now(),
        }

        // Reverse geocode the IP-based coordinates
        const address = await this.reverseGeocode(position.latitude, position.longitude)

        console.log('[GeolocationService] Using IP-based location:', { position, address })

        return { position, address }
      } catch (ipError) {
        console.error('[GeolocationService] IP fallback also failed:', ipError)
        throw error // Throw original error
      }
    }
  }

  /**
   * Calculate distance between two coordinates (Haversine formula)
   *
   * @param lat1 - First latitude
   * @param lon1 - First longitude
   * @param lat2 - Second latitude
   * @param lon2 - Second longitude
   * @returns Distance in kilometers
   *
   * @example
   * ```typescript
   * const distance = geolocationService.calculateDistance(35.6892, 51.3890, 35.7219, 51.3347)
   * console.log(`${distance.toFixed(2)} km`)
   * ```
   */
  calculateDistance(lat1: number, lon1: number, lat2: number, lon2: number): number {
    const R = 6371 // Earth's radius in kilometers
    const dLat = this.toRad(lat2 - lat1)
    const dLon = this.toRad(lon2 - lon1)

    const a =
      Math.sin(dLat / 2) * Math.sin(dLat / 2) +
      Math.cos(this.toRad(lat1)) * Math.cos(this.toRad(lat2)) *
      Math.sin(dLon / 2) * Math.sin(dLon / 2)

    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a))
    return R * c
  }

  /**
   * Watch user's position and get real-time updates
   *
   * @param callback - Function to call on position update
   * @param errorCallback - Function to call on error
   * @param options - Geolocation options
   * @returns Watch ID that can be used to clear the watch
   *
   * @example
   * ```typescript
   * const watchId = geolocationService.watchPosition(
   *   (position) => console.log('Position updated:', position),
   *   (error) => console.error('Error:', error)
   * )
   *
   * // Later, stop watching
   * geolocationService.clearWatch(watchId)
   * ```
   */
  watchPosition(
    callback: (position: GeolocationPosition) => void,
    errorCallback?: (error: GeolocationError) => void,
    options?: GeolocationOptions
  ): number {
    if (!this.isSupported()) {
      const error = this.createError('NOT_SUPPORTED', 'مرورگر شما از موقعیت‌یابی پشتیبانی نمی‌کند')
      errorCallback?.(error)
      return -1
    }

    const geoOptions: PositionOptions = {
      enableHighAccuracy: options?.enableHighAccuracy ?? true,
      timeout: options?.timeout ?? this.DEFAULT_TIMEOUT,
      maximumAge: options?.maximumAge ?? this.CACHE_MAX_AGE,
    }

    return navigator.geolocation.watchPosition(
      (pos) => {
        const position: GeolocationPosition = {
          latitude: pos.coords.latitude,
          longitude: pos.coords.longitude,
          accuracy: pos.coords.accuracy,
          timestamp: pos.timestamp,
        }
        callback(position)
      },
      (error) => {
        const geoError = this.handleGeolocationError(error)
        errorCallback?.(geoError)
      },
      geoOptions
    )
  }

  /**
   * Clear position watch
   */
  clearWatch(watchId: number): void {
    if (this.isSupported()) {
      navigator.geolocation.clearWatch(watchId)
    }
  }

  /**
   * Request permission for geolocation (for browsers that support Permissions API)
   *
   * @returns Promise with permission state ('granted', 'denied', 'prompt')
   */
  async requestPermission(): Promise<PermissionState | 'unsupported'> {
    if (!('permissions' in navigator)) {
      return 'unsupported'
    }

    try {
      const result = await navigator.permissions.query({ name: 'geolocation' })
      return result.state
    } catch (error) {
      console.warn('[GeolocationService] Permission query failed:', error)
      return 'unsupported'
    }
  }

  /**
   * Clear cached position and address
   */
  clearCache(): void {
    this.cachedPosition = null
    this.cachedAddress = null
    this.cachedIPLocation = null
    console.log('[GeolocationService] Cache cleared')
  }

  // ============================================
  // Private Helper Methods
  // ============================================

  private toRad(degrees: number): number {
    return degrees * (Math.PI / 180)
  }

  private isCacheValid(timestamp: number, maxAge?: number): boolean {
    if (!maxAge) maxAge = this.CACHE_MAX_AGE
    return Date.now() - timestamp < maxAge
  }

  private handleGeolocationError(error: GeolocationPositionError): GeolocationError {
    switch (error.code) {
      case error.PERMISSION_DENIED:
        return this.createError(
          'PERMISSION_DENIED',
          'دسترسی به موقعیت مکانی رد شد. لطفاً دسترسی را در تنظیمات مرورگر فعال کنید.'
        )
      case error.POSITION_UNAVAILABLE:
        return this.createError(
          'POSITION_UNAVAILABLE',
          'موقعیت مکانی در دسترس نیست. لطفاً اتصال اینترنت و GPS خود را بررسی کنید.'
        )
      case error.TIMEOUT:
        return this.createError(
          'TIMEOUT',
          'زمان درخواست موقعیت مکانی به پایان رسید. لطفاً دوباره تلاش کنید.'
        )
      default:
        return this.createError(
          'POSITION_UNAVAILABLE',
          'خطای نامشخص در دریافت موقعیت مکانی'
        )
    }
  }

  private createError(code: GeolocationError['code'], message: string): GeolocationError {
    return { code, message }
  }
}

// Export singleton instance
export const geolocationService = new GeolocationService()
export default geolocationService
