/**
 * Simple in-memory cache with TTL (Time To Live)
 * Used for caching API responses to reduce server load
 */

interface CacheEntry<T> {
  data: T
  timestamp: number
  ttl: number // milliseconds
}

class Cache {
  private cache: Map<string, CacheEntry<unknown>> = new Map()

  /**
   * Set a value in cache with TTL
   * @param key Cache key
   * @param data Data to cache
   * @param ttl Time to live in milliseconds (default: 5 minutes)
   */
  set<T>(key: string, data: T, ttl: number = 5 * 60 * 1000): void {
    this.cache.set(key, {
      data,
      timestamp: Date.now(),
      ttl,
    })
  }

  /**
   * Get a value from cache
   * Returns null if not found or expired
   * @param key Cache key
   */
  get<T>(key: string): T | null {
    const entry = this.cache.get(key)

    if (!entry) {
      return null
    }

    const now = Date.now()
    const age = now - entry.timestamp

    // Check if expired
    if (age > entry.ttl) {
      this.cache.delete(key)
      return null
    }

    return entry.data as T
  }

  /**
   * Check if a key exists and is not expired
   * @param key Cache key
   */
  has(key: string): boolean {
    const data = this.get(key)
    return data !== null
  }

  /**
   * Invalidate (delete) a cache entry
   * @param key Cache key
   */
  invalidate(key: string): void {
    this.cache.delete(key)
  }

  /**
   * Invalidate all cache entries matching a pattern
   * @param pattern RegExp or string to match against keys
   */
  invalidatePattern(pattern: string | RegExp): void {
    const regex = typeof pattern === 'string' ? new RegExp(pattern) : pattern

    for (const key of this.cache.keys()) {
      if (regex.test(key)) {
        this.cache.delete(key)
      }
    }
  }

  /**
   * Clear all cache entries
   */
  clear(): void {
    this.cache.clear()
  }

  /**
   * Get cache statistics
   */
  stats(): { size: number; keys: string[] } {
    return {
      size: this.cache.size,
      keys: Array.from(this.cache.keys()),
    }
  }

  /**
   * Clean up expired entries
   */
  cleanup(): void {
    const now = Date.now()

    for (const [key, entry] of this.cache.entries()) {
      const age = now - entry.timestamp

      if (age > entry.ttl) {
        this.cache.delete(key)
      }
    }
  }
}

// Export singleton instance
export const cache = new Cache()

// Auto-cleanup every 5 minutes
setInterval(() => {
  cache.cleanup()
}, 5 * 60 * 1000)

export default cache

/**
 * Cache key generators
 */
export const CacheKeys = {
  // Customer profile
  customerProfile: (customerId: string) => `customer:profile:${customerId}`,

  // Bookings
  upcomingBookings: (customerId: string) => `customer:bookings:upcoming:${customerId}`,
  bookingHistory: (customerId: string, page: number) => `customer:bookings:history:${customerId}:${page}`,

  // Favorites
  favorites: (customerId: string) => `customer:favorites:${customerId}`,

  // Reviews
  reviews: (customerId: string) => `customer:reviews:${customerId}`,

  // Preferences
  preferences: (customerId: string) => `customer:preferences:${customerId}`,

  // Provider data
  providerDetails: (providerId: string) => `provider:details:${providerId}`,
  providerReviews: (providerId: string, page: number) => `provider:reviews:${providerId}:${page}`,
}

/**
 * Cache TTL constants (in milliseconds)
 */
export const CacheTTL = {
  SHORT: 2 * 60 * 1000,      // 2 minutes
  MEDIUM: 5 * 60 * 1000,     // 5 minutes
  LONG: 15 * 60 * 1000,      // 15 minutes
  VERY_LONG: 60 * 60 * 1000, // 1 hour
}
