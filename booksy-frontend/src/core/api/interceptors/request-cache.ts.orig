/**
 * Request Cache
 *
 * Caches GET requests to reduce unnecessary API calls
 * Supports TTL (time-to-live) and cache invalidation
 */

import type { AxiosResponse, InternalAxiosRequestConfig } from 'axios'

interface CacheEntry {
  data: AxiosResponse
  timestamp: number
  ttl: number
}

interface CacheConfig {
  ttl: number // Time to live in milliseconds
  enabled: boolean
}

const DEFAULT_CACHE_CONFIG: CacheConfig = {
  ttl: 5 * 60 * 1000, // 5 minutes
  enabled: true,
}

/**
 * Enhanced config with cache metadata
 */
interface CacheableRequestConfig extends InternalAxiosRequestConfig {
  cache?: boolean | Partial<CacheConfig>
  cacheKey?: string
  __cacheConfig?: CacheConfig
}

/**
 * In-memory cache storage
 */
class RequestCache {
  private cache = new Map<string, CacheEntry>()

  /**
   * Generate cache key from request config
   */
  private generateKey(config: InternalAxiosRequestConfig): string {
    const { method, url, params } = config
    const paramsStr = params ? JSON.stringify(params) : ''
    return `${method}:${url}:${paramsStr}`
  }

  /**
   * Check if cache entry is still valid
   */
  private isValid(entry: CacheEntry): boolean {
    const now = Date.now()
    return now - entry.timestamp < entry.ttl
  }

  /**
   * Get cached response if available and valid
   */
  get(config: InternalAxiosRequestConfig): AxiosResponse | null {
    const key = this.generateKey(config)
    const entry = this.cache.get(key)

    if (!entry) {
      return null
    }

    if (!this.isValid(entry)) {
      this.cache.delete(key)
      return null
    }

    if (import.meta.env.DEV) {
      const method = config.method && typeof config.method === 'string' ? config.method.toUpperCase() : 'UNKNOWN'
      console.log(`💾 Cache HIT: ${method} ${config.url}`)
    }

    // Ensure the cached response has a valid config object with method
    const cachedResponse = { ...entry.data }

    // Always ensure config exists with all required properties
    // Use the current request config as the base to ensure method and url are always defined
    const cachedConfig = cachedResponse.config || {}
    cachedResponse.config = {
      ...config, // Use the current request config as base (has method, url, etc.)
      ...cachedConfig, // Override with cached config properties
      // Explicitly ensure method and url are never undefined
      method: config.method || cachedConfig.method || 'get',
      url: config.url || cachedConfig.url || '',
      // Preserve metadata from current config for logging interceptor
      metadata: config.metadata || cachedConfig.metadata,
    }

    return cachedResponse
  }

  /**
   * Store response in cache
   */
  set(config: InternalAxiosRequestConfig, response: AxiosResponse, ttl: number): void {
    // Don't cache responses without a valid config
    if (!response.config || !response.config.method) {
      if (import.meta.env.DEV) {
        console.warn('💾 Cache SKIP: Response missing valid config')
      }
      return
    }

    const key = this.generateKey(config)

    // Deep clone the config to prevent mutation issues
    // Use the original request config as base and preserve essential response config properties
    const clonedConfig = {
      ...config,
      method: response.config.method || config.method,
      url: response.config.url || config.url,
    }

    this.cache.set(key, {
      data: {
        ...response,
        config: clonedConfig,
      },
      timestamp: Date.now(),
      ttl,
    })

    if (import.meta.env.DEV) {
      const method = config.method && typeof config.method === 'string' ? config.method.toUpperCase() : 'UNKNOWN'
      console.log(`💾 Cache SET: ${method} ${config.url} (TTL: ${ttl}ms)`)
    }
  }

  /**
   * Clear specific cache entry
   */
  delete(config: InternalAxiosRequestConfig): void {
    const key = this.generateKey(config)
    this.cache.delete(key)
  }

  /**
   * Clear cache entries by URL pattern
   */
  deleteByPattern(pattern: string | RegExp): void {
    const regex = typeof pattern === 'string' ? new RegExp(pattern) : pattern

    for (const key of this.cache.keys()) {
      if (regex.test(key)) {
        this.cache.delete(key)
      }
    }
  }

  /**
   * Clear all cache
   */
  clear(): void {
    this.cache.clear()
    if (import.meta.env.DEV) {
      console.log('💾 Cache CLEARED')
    }
  }

  /**
   * Get cache statistics
   */
  stats(): { size: number; entries: string[] } {
    return {
      size: this.cache.size,
      entries: Array.from(this.cache.keys()),
    }
  }

  /**
   * Clean expired entries
   */
  cleanExpired(): void {
    const now = Date.now()
    let cleaned = 0

    for (const [key, entry] of this.cache.entries()) {
      if (now - entry.timestamp >= entry.ttl) {
        this.cache.delete(key)
        cleaned++
      }
    }

    if (import.meta.env.DEV && cleaned > 0) {
      console.log(`💾 Cache cleaned ${cleaned} expired entries`)
    }
  }
}

// Singleton cache instance
const cache = new RequestCache()

// Auto-clean expired entries every 5 minutes
if (typeof window !== 'undefined') {
  setInterval(() => {
    cache.cleanExpired()
  }, 5 * 60 * 1000)
}

/**
 * Request interceptor - Check cache before making request
 * Uses a custom adapter to properly short-circuit axios when cache hits
 */
export function cacheRequestInterceptor(
  config: CacheableRequestConfig,
): InternalAxiosRequestConfig {
  // Only cache GET requests
  if (config.method?.toLowerCase() !== 'get') {
    return config
  }

  // Check if caching is disabled for this request
  if (config.cache === false) {
    return config
  }

  // Get cache config
  const cacheConfig =
    typeof config.cache === 'object'
      ? { ...DEFAULT_CACHE_CONFIG, ...config.cache }
      : DEFAULT_CACHE_CONFIG

  if (!cacheConfig.enabled) {
    return config
  }

  // Try to get from cache with error handling
  try {
    const cachedResponse = cache.get(config)

    if (cachedResponse) {
      // Validate cached response has required structure
      if (!cachedResponse.config || !cachedResponse.config.method) {
        if (import.meta.env.DEV) {
          console.warn('💾 Cache INVALID: Response missing valid config, fetching fresh data')
        }
        // Invalidate this cache entry and proceed with fresh request
        cache.delete(config)
        ;(config as CacheableRequestConfig).__cacheConfig = cacheConfig
        return config
      }

      // Store original adapter and replace with cache adapter
      const originalAdapter = config.adapter
      config.adapter = () => {
        // Restore original adapter for future requests
        config.adapter = originalAdapter
        // Return cached response as a resolved promise
        return Promise.resolve(cachedResponse)
      }

      return config
    }
  } catch (e) {
    // If cache retrieval fails, log and proceed with fresh request
    if (import.meta.env.DEV) {
      console.warn('💾 Cache ERROR: Failed to retrieve cached response:', e)
    }
  }

  // Store cache config in request for response interceptor
  ;(config as CacheableRequestConfig).__cacheConfig = cacheConfig

  return config
}

/**
 * Response interceptor - Store successful GET responses in cache
 */
export function cacheResponseInterceptor(response: AxiosResponse): AxiosResponse {
  const config = response.config as CacheableRequestConfig

  // Only cache GET requests
  if (config.method?.toLowerCase() !== 'get') {
    return response
  }

  // Check if caching is disabled
  if (config.cache === false) {
    return response
  }

  // Get cache config from request
  const cacheConfig = (config as CacheableRequestConfig).__cacheConfig

  if (cacheConfig?.enabled) {
    cache.set(config, response, cacheConfig.ttl)
  }

  return response
}

/**
 * Helper: Configure cache for specific request
 */
export function withCache(
  config: InternalAxiosRequestConfig,
  cacheConfig?: Partial<CacheConfig>,
): CacheableRequestConfig {
  return {
    ...config,
    cache: cacheConfig ? { ...DEFAULT_CACHE_CONFIG, ...cacheConfig } : true,
  } as CacheableRequestConfig
}

/**
 * Helper: Disable cache for specific request
 */
export function withoutCache(config: InternalAxiosRequestConfig): CacheableRequestConfig {
  return {
    ...config,
    cache: false,
  } as CacheableRequestConfig
}

/**
 * Helper: Clear cache by URL pattern
 */
export function clearCache(pattern?: string | RegExp): void {
  if (pattern) {
    cache.deleteByPattern(pattern)
  } else {
    cache.clear()
  }
}

/**
 * Helper: Get cache statistics
 */
export function getCacheStats(): { size: number; entries: string[] } {
  return cache.stats()
}

/**
 * Export cache instance for advanced usage
 */
export { cache as requestCache }
