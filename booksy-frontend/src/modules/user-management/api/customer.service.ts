/**
 * Customer Service
 * Handles customer/user data fetching with intelligent caching
 * Optimized for frequent name lookups in booking lists
 */

import { userManagementClient } from '@/core/api/client/http-client'
import type { ApiResponse } from '@/core/api/client/api-response'
import type { User } from '../types/user.types'

const API_VERSION = 'v1'
const API_BASE = `/${API_VERSION}/Users`

// ==================== Cache Configuration ====================

interface CacheEntry<T> {
  data: T
  timestamp: number
  expiresAt: number
}

class DataCache<T> {
  private cache = new Map<string, CacheEntry<T>>()
  private defaultTTL: number // Time to live in milliseconds

  constructor(ttlMinutes: number = 15) {
    this.defaultTTL = ttlMinutes * 60 * 1000
  }

  set(key: string, data: T, ttl?: number): void {
    const now = Date.now()
    this.cache.set(key, {
      data,
      timestamp: now,
      expiresAt: now + (ttl || this.defaultTTL),
    })
  }

  get(key: string): T | null {
    const entry = this.cache.get(key)
    if (!entry) return null

    // Check if expired
    if (Date.now() > entry.expiresAt) {
      this.cache.delete(key)
      return null
    }

    return entry.data
  }

  has(key: string): boolean {
    const entry = this.cache.get(key)
    if (!entry) return false

    if (Date.now() > entry.expiresAt) {
      this.cache.delete(key)
      return false
    }

    return true
  }

  invalidate(key: string): void {
    this.cache.delete(key)
  }

  clear(): void {
    this.cache.clear()
  }

  getSize(): number {
    // Clean expired entries first
    this.cleanExpired()
    return this.cache.size
  }

  private cleanExpired(): void {
    const now = Date.now()
    for (const [key, entry] of this.cache.entries()) {
      if (now > entry.expiresAt) {
        this.cache.delete(key)
      }
    }
  }
}

// ==================== Customer Name Cache ====================

interface CustomerNameData {
  id: string
  fullName: string
  firstName?: string
  lastName?: string
}

// ==================== Customer Service Class ====================

class CustomerService {
  // Customer name cache (15 minute TTL)
  private nameCache = new DataCache<CustomerNameData>(15)

  // Full customer data cache (5 minute TTL for detailed data)
  private customerCache = new DataCache<User>(5)

  // ============================================
  // Quick Name Lookup (Optimized for Booking Lists)
  // ============================================

  /**
   * Get customer name by ID (with caching)
   * Returns formatted name string or ID as fallback
   *
   * Usage: Perfect for booking lists where you just need the name
   */
  async getCustomerName(customerId: string): Promise<string> {
    try {
      // Check cache first
      const cached = this.nameCache.get(customerId)
      if (cached) {
        return cached.fullName || `${cached.firstName} ${cached.lastName}`.trim() || customerId
      }

      // Fetch from API
      const customer = await this.getCustomerById(customerId)

      // Cache the name data
      this.nameCache.set(customerId, {
        id: customer.id,
        fullName: customer.fullName || '',
        firstName: customer.firstName,
        lastName: customer.lastName,
      })

      return customer.fullName || `${customer.firstName} ${customer.lastName}`.trim() || customerId
    } catch (error) {
      console.warn(`[CustomerService] Could not fetch name for customer ${customerId}:`, error)
      // Return ID as fallback instead of throwing
      return customerId
    }
  }

  /**
   * Get multiple customer names in one call (batch lookup)
   * More efficient than multiple individual calls
   *
   * Returns a Map of customerId -> name
   */
  async getCustomerNames(customerIds: string[]): Promise<Map<string, string>> {
    const result = new Map<string, string>()
    const uncachedIds: string[] = []

    // First, check cache for each ID
    for (const id of customerIds) {
      const cached = this.nameCache.get(id)
      if (cached) {
        result.set(id, cached.fullName || `${cached.firstName} ${cached.lastName}`.trim() || id)
      } else {
        uncachedIds.push(id)
      }
    }

    // If all were cached, return immediately
    if (uncachedIds.length === 0) {
      return result
    }

    try {
      // Fetch uncached IDs
      // TODO: Implement batch endpoint if available: GET /api/v1/users/batch?ids=1,2,3
      // For now, fetch individually (could be optimized with Promise.allSettled)
      const fetchPromises = uncachedIds.map(id =>
        this.getCustomerById(id)
          .then(customer => {
            const name = customer.fullName || `${customer.firstName} ${customer.lastName}`.trim() || id

            // Cache the name
            this.nameCache.set(id, {
              id: customer.id,
              fullName: customer.fullName || '',
              firstName: customer.firstName,
              lastName: customer.lastName,
            })

            result.set(id, name)
          })
          .catch(err => {
            console.warn(`[CustomerService] Failed to fetch customer ${id}:`, err)
            result.set(id, id) // Use ID as fallback
          })
      )

      await Promise.all(fetchPromises)
    } catch (error) {
      console.error('[CustomerService] Error in batch name fetch:', error)
    }

    return result
  }

  // ============================================
  // Customer CRUD Operations
  // ============================================

  /**
   * Get customer by ID (with caching)
   * GET /api/v1/users/{id}
   */
  async getCustomerById(id: string): Promise<User> {
    try {
      // Check cache first
      const cached = this.customerCache.get(id)
      if (cached) {
        console.log(`[CustomerService] Customer ${id} retrieved from cache`)
        return cached
      }

      console.log(`[CustomerService] Fetching customer: ${id}`)

      const response = await userManagementClient.get<ApiResponse<User>>(
        `${API_BASE}/${id}`
      )

      console.log('[CustomerService] Customer retrieved:', response.data)

      // Handle wrapped response format
      const customer = response.data?.data || response.data

      if (!customer) {
        throw new Error(`Customer ${id} not found`)
      }

      // Cache the full customer data
      this.customerCache.set(id, customer as User)

      return customer as User
    } catch (error) {
      console.error(`[CustomerService] Error fetching customer ${id}:`, error)
      throw this.handleError(error)
    }
  }

  /**
   * Search customers
   * GET /api/v1/users/search
   */
  async searchCustomers(query: string, limit: number = 10): Promise<User[]> {
    try {
      console.log(`[CustomerService] Searching customers: "${query}"`)

      const response = await userManagementClient.get<ApiResponse<{ items: User[] }>>(
        `${API_BASE}/search`,
        {
          params: {
            searchTerm: query,
            userType: 'Client',
            pageNumber: 1,
            pageSize: limit,
          },
        }
      )

      // Handle wrapped response format
      const data = response.data?.data || response.data
      const customers = data?.items || []

      console.log(`[CustomerService] Found ${customers.length} customers`)

      // Cache the results
      customers.forEach(customer => {
        this.customerCache.set(customer.id, customer)
        this.nameCache.set(customer.id, {
          id: customer.id,
          fullName: customer.fullName || '',
          firstName: customer.firstName,
          lastName: customer.lastName,
        })
      })

      return customers
    } catch (error) {
      console.error('[CustomerService] Error searching customers:', error)
      throw this.handleError(error)
    }
  }

  // ============================================
  // Cache Management
  // ============================================

  /**
   * Invalidate cache for specific customer
   */
  invalidateCustomer(customerId: string): void {
    this.customerCache.invalidate(customerId)
    this.nameCache.invalidate(customerId)
    console.log(`[CustomerService] Cache invalidated for customer ${customerId}`)
  }

  /**
   * Clear all caches
   */
  clearCache(): void {
    this.customerCache.clear()
    this.nameCache.clear()
    console.log('[CustomerService] All caches cleared')
  }

  /**
   * Get cache statistics
   */
  getCacheStats(): { names: number; customers: number } {
    return {
      names: this.nameCache.getSize(),
      customers: this.customerCache.getSize(),
    }
  }

  // ============================================
  // Helper Methods
  // ============================================

  /**
   * Format customer display name
   * Handles various name formats gracefully
   */
  formatCustomerName(customer: Partial<User>): string {
    if (customer.fullName) {
      return customer.fullName
    }

    if (customer.firstName && customer.lastName) {
      return `${customer.firstName} ${customer.lastName}`.trim()
    }

    if (customer.firstName) {
      return customer.firstName
    }

    if (customer.lastName) {
      return customer.lastName
    }

    if (customer.email) {
      return customer.email.split('@')[0] // Use email username as fallback
    }

    return customer.id || 'مشتری'
  }

  // ============================================
  // Error Handling
  // ============================================

  private handleError(error: unknown): Error {
    if (error instanceof Error) {
      return error
    }
    return new Error('خطا در دریافت اطلاعات مشتری')
  }
}

// Export singleton instance
export const customerService = new CustomerService()
export default customerService
