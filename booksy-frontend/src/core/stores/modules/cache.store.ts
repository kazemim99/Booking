/**
 * Cache Store
 * Application-level caching with TTL support and memory management
 */

import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { CacheMetadata } from '@/core/types/api.types'

// ==================== Types ====================

interface CacheEntry<T = unknown> {
  key: string
  value: T
  cachedAt: number
  expiresAt: number
  ttl: number
  accessCount: number
  lastAccessedAt: number
  tags?: string[]
  metadata?: Record<string, unknown>
}

interface CacheOptions {
  ttl?: number // Time to live in milliseconds
  tags?: string[]
  metadata?: Record<string, unknown>
}

interface CacheStats {
  size: number
  totalEntries: number
  hitCount: number
  missCount: number
  hitRate: number
  memoryUsage: number
}

// ==================== Constants ====================

const DEFAULT_TTL = 5 * 60 * 1000 // 5 minutes
const MAX_CACHE_SIZE = 100 // Maximum number of entries
const CLEANUP_INTERVAL = 60 * 1000 // 1 minute

// ==================== Store ====================

export const useCacheStore = defineStore('cache', () => {
  // ==================== State ====================

  const entries = ref<Map<string, CacheEntry>>(new Map())
  const hitCount = ref(0)
  const missCount = ref(0)
  const cleanupInterval = ref<ReturnType<typeof setInterval> | null>(null)

  // ==================== Computed ====================

  const size = computed(() => entries.value.size)

  const hitRate = computed(() => {
    const total = hitCount.value + missCount.value
    return total === 0 ? 0 : (hitCount.value / total) * 100
  })

  const stats = computed<CacheStats>(() => {
    const entriesArray = Array.from(entries.value.values())
    const memoryUsage = estimateMemoryUsage(entriesArray)

    return {
      size: size.value,
      totalEntries: size.value,
      hitCount: hitCount.value,
      missCount: missCount.value,
      hitRate: hitRate.value,
      memoryUsage,
    }
  })

  // ==================== Actions ====================

  /**
   * Get value from cache
   */
  function get<T = unknown>(key: string): T | null {
    const entry = entries.value.get(key)

    if (!entry) {
      missCount.value++
      return null
    }

    // Check if expired
    if (isExpired(entry)) {
      entries.value.delete(key)
      missCount.value++
      return null
    }

    // Update access stats
    entry.accessCount++
    entry.lastAccessedAt = Date.now()
    hitCount.value++

    return entry.value as T
  }

  /**
   * Set value in cache
   */
  function set<T = unknown>(
    key: string,
    value: T,
    options: CacheOptions = {}
  ): void {
    const ttl = options.ttl || DEFAULT_TTL
    const now = Date.now()

    const entry: CacheEntry<T> = {
      key,
      value,
      cachedAt: now,
      expiresAt: now + ttl,
      ttl,
      accessCount: 0,
      lastAccessedAt: now,
      tags: options.tags,
      metadata: options.metadata,
    }

    // Check cache size limit
    if (entries.value.size >= MAX_CACHE_SIZE) {
      evictLRU()
    }

    entries.value.set(key, entry)

    if (import.meta.env.DEV) {
      console.log(`💾 Cache SET: ${key} (TTL: ${ttl}ms)`)
    }
  }

  /**
   * Check if key exists in cache
   */
  function has(key: string): boolean {
    const entry = entries.value.get(key)

    if (!entry) {
      return false
    }

    if (isExpired(entry)) {
      entries.value.delete(key)
      return false
    }

    return true
  }

  /**
   * Delete entry from cache
   */
  function del(key: string): boolean {
    return entries.value.delete(key)
  }

  /**
   * Clear all entries from cache
   */
  function clear(): void {
    entries.value.clear()
    hitCount.value = 0
    missCount.value = 0

    if (import.meta.env.DEV) {
      console.log('💾 Cache CLEARED')
    }
  }

  /**
   * Clear entries by pattern (regex)
   */
  function clearByPattern(pattern: RegExp): number {
    let cleared = 0

    for (const [key] of entries.value) {
      if (pattern.test(key)) {
        entries.value.delete(key)
        cleared++
      }
    }

    if (import.meta.env.DEV && cleared > 0) {
      console.log(`💾 Cache CLEARED ${cleared} entries matching pattern: ${pattern}`)
    }

    return cleared
  }

  /**
   * Clear entries by tag
   */
  function clearByTag(tag: string): number {
    let cleared = 0

    for (const [key, entry] of entries.value) {
      if (entry.tags?.includes(tag)) {
        entries.value.delete(key)
        cleared++
      }
    }

    if (import.meta.env.DEV && cleared > 0) {
      console.log(`💾 Cache CLEARED ${cleared} entries with tag: ${tag}`)
    }

    return cleared
  }

  /**
   * Clear expired entries
   */
  function clearExpired(): number {
    let cleared = 0

    for (const [key, entry] of entries.value) {
      if (isExpired(entry)) {
        entries.value.delete(key)
        cleared++
      }
    }

    if (import.meta.env.DEV && cleared > 0) {
      console.log(`💾 Cache CLEARED ${cleared} expired entries`)
    }

    return cleared
  }

  /**
   * Get cache metadata for a key
   */
  function getMetadata(key: string): CacheMetadata | null {
    const entry = entries.value.get(key)

    if (!entry || isExpired(entry)) {
      return null
    }

    return {
      key: entry.key,
      cachedAt: new Date(entry.cachedAt).toISOString(),
      expiresAt: new Date(entry.expiresAt).toISOString(),
      ttl: entry.ttl,
      hitCount: entry.accessCount,
    }
  }

  /**
   * Get all keys in cache
   */
  function keys(): string[] {
    clearExpired() // Clean up first
    return Array.from(entries.value.keys())
  }

  /**
   * Get all entries in cache (non-expired)
   */
  function getAllEntries<T = unknown>(): Array<CacheEntry<T>> {
    clearExpired()
    return Array.from(entries.value.values()) as Array<CacheEntry<T>>
  }

  /**
   * Update TTL for an entry
   */
  function updateTTL(key: string, ttl: number): boolean {
    const entry = entries.value.get(key)

    if (!entry || isExpired(entry)) {
      return false
    }

    entry.ttl = ttl
    entry.expiresAt = Date.now() + ttl

    return true
  }

  /**
   * Touch an entry to reset its expiry time
   */
  function touch(key: string): boolean {
    const entry = entries.value.get(key)

    if (!entry || isExpired(entry)) {
      return false
    }

    const now = Date.now()
    entry.expiresAt = now + entry.ttl
    entry.lastAccessedAt = now

    return true
  }

  /**
   * Remember function with cache
   * Executes function and caches result if not already cached
   */
  async function remember<T>(
    key: string,
    fn: () => T | Promise<T>,
    options: CacheOptions = {}
  ): Promise<T> {
    // Check cache first
    const cached = get<T>(key)
    if (cached !== null) {
      return cached
    }

    // Execute function
    const result = await fn()

    // Store in cache
    set(key, result, options)

    return result
  }

  /**
   * Remember function forever (no expiry)
   */
  async function rememberForever<T>(
    key: string,
    fn: () => T | Promise<T>,
    options: Omit<CacheOptions, 'ttl'> = {}
  ): Promise<T> {
    return remember(key, fn, {
      ...options,
      ttl: Number.MAX_SAFE_INTEGER,
    })
  }

  /**
   * Forget a key from cache
   */
  function forget(key: string): boolean {
    return del(key)
  }

  /**
   * Flush all cache entries
   */
  function flush(): void {
    clear()
  }

  /**
   * Start automatic cleanup interval
   */
  function startCleanup(): void {
    if (cleanupInterval.value) {
      return
    }

    cleanupInterval.value = setInterval(() => {
      const cleared = clearExpired()
      if (import.meta.env.DEV && cleared > 0) {
        console.log(`💾 Cache auto-cleanup: removed ${cleared} expired entries`)
      }
    }, CLEANUP_INTERVAL)

    if (import.meta.env.DEV) {
      console.log(`💾 Cache cleanup started (interval: ${CLEANUP_INTERVAL}ms)`)
    }
  }

  /**
   * Stop automatic cleanup interval
   */
  function stopCleanup(): void {
    if (cleanupInterval.value) {
      clearInterval(cleanupInterval.value)
      cleanupInterval.value = null

      if (import.meta.env.DEV) {
        console.log('💾 Cache cleanup stopped')
      }
    }
  }

  /**
   * Initialize cache store
   */
  function initialize(): void {
    startCleanup()
  }

  /**
   * Destroy cache store
   */
  function destroy(): void {
    stopCleanup()
    clear()
  }

  // ==================== Helper Functions ====================

  /**
   * Check if cache entry is expired
   */
  function isExpired(entry: CacheEntry): boolean {
    return Date.now() > entry.expiresAt
  }

  /**
   * Evict least recently used entry
   */
  function evictLRU(): void {
    let oldestKey: string | null = null
    let oldestTime = Date.now()

    for (const [key, entry] of entries.value) {
      if (entry.lastAccessedAt < oldestTime) {
        oldestTime = entry.lastAccessedAt
        oldestKey = key
      }
    }

    if (oldestKey) {
      entries.value.delete(oldestKey)

      if (import.meta.env.DEV) {
        console.log(`💾 Cache LRU eviction: ${oldestKey}`)
      }
    }
  }

  /**
   * Estimate memory usage of cache entries
   */
  function estimateMemoryUsage(entriesArray: CacheEntry[]): number {
    let size = 0

    for (const entry of entriesArray) {
      // Rough estimation: JSON string length as bytes
      size += JSON.stringify(entry.value).length
      size += entry.key.length
      size += 100 // Overhead for metadata
    }

    return size
  }

  // ==================== Return ====================

  return {
    // State
    entries,
    hitCount,
    missCount,

    // Computed
    size,
    hitRate,
    stats,

    // Actions
    get,
    set,
    has,
    del,
    clear,
    clearByPattern,
    clearByTag,
    clearExpired,
    getMetadata,
    keys,
    getAllEntries,
    updateTTL,
    touch,
    remember,
    rememberForever,
    forget,
    flush,
    startCleanup,
    stopCleanup,
    initialize,
    destroy,
  }
})

// ==================== Composable Helper ====================

/**
 * Use cache store with automatic initialization
 */
export function useCache() {
  const store = useCacheStore()

  // Auto-initialize if not already started
  if (!store.cleanupInterval) {
    store.initialize()
  }

  return store
}
