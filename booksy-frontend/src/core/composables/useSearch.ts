/**
 * Search Composable
 * Search and filter functionality with debouncing
 */

import { ref, computed, watch } from 'vue'
import type { Ref } from 'vue'
import type { FilterParams, SearchParams } from '@/core/types/common.types'
import { useDebounce } from './useDebounce'

// ==================== Types ====================

export interface UseSearchOptions {
  debounceMs?: number
  minSearchLength?: number
  caseSensitive?: boolean
  trimQuery?: boolean
}

// ==================== Composable ====================

export function useSearch<T = unknown>(options: UseSearchOptions = {}) {
  const {
    debounceMs = 300,
    minSearchLength = 2,
    caseSensitive = false,
    trimQuery = true,
  } = options

  // ==================== State ====================

  const query = ref('')
  const filters = ref<FilterParams>({})
  const results = ref<T[]>([])
  const isSearching = ref(false)

  // ==================== Computed ====================

  const hasQuery = computed(() => {
    const q = trimQuery ? query.value.trim() : query.value
    return q.length >= minSearchLength
  })

  const hasFilters = computed(() => Object.keys(filters.value).length > 0)

  const hasActiveSearch = computed(() => hasQuery.value || hasFilters.value)

  const resultCount = computed(() => results.value.length)

  const searchParams = computed<SearchParams>(() => ({
    pageNumber: 1,
    pageSize: 20,
    query: query.value,
    filters: filters.value,
  }))

  // ==================== Debounced Query ====================

  const debouncedQuery = useDebounce(query, debounceMs)

  // ==================== Actions ====================

  /**
   * Set search query
   */
  function setQuery(q: string): void {
    query.value = q
  }

  /**
   * Clear search query
   */
  function clearQuery(): void {
    query.value = ''
  }

  /**
   * Set a filter
   */
  function setFilter(key: string, value: unknown): void {
    filters.value[key] = value
  }

  /**
   * Remove a filter
   */
  function removeFilter(key: string): void {
    delete filters.value[key]
  }

  /**
   * Set multiple filters
   */
  function setFilters(newFilters: FilterParams): void {
    filters.value = { ...newFilters }
  }

  /**
   * Clear all filters
   */
  function clearFilters(): void {
    filters.value = {}
  }

  /**
   * Clear query and filters
   */
  function clearAll(): void {
    clearQuery()
    clearFilters()
  }

  /**
   * Set search results
   */
  function setResults(items: T[]): void {
    results.value = items
  }

  /**
   * Clear search results
   */
  function clearResults(): void {
    results.value = []
  }

  /**
   * Perform search with callback
   */
  async function search(
    searchFn: (params: SearchParams) => Promise<T[]>
  ): Promise<void> {
    if (!hasQuery.value && !hasFilters.value) {
      clearResults()
      return
    }

    isSearching.value = true

    try {
      const items = await searchFn(searchParams.value)
      setResults(items)
    } finally {
      isSearching.value = false
    }
  }

  /**
   * Filter items locally
   */
  function filterItems(items: T[], searchFn: (item: T, query: string) => boolean): T[] {
    const q = trimQuery ? query.value.trim() : query.value

    if (!hasQuery.value) {
      return items
    }

    const searchQuery = caseSensitive ? q : q.toLowerCase()

    return items.filter((item) => searchFn(item, searchQuery))
  }

  /**
   * Highlight matches in text
   */
  function highlightMatches(text: string): string {
    if (!hasQuery.value) {
      return text
    }

    const q = trimQuery ? query.value.trim() : query.value

    if (!q) {
      return text
    }

    const regex = new RegExp(
      `(${q.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`,
      caseSensitive ? 'g' : 'gi'
    )

    return text.replace(regex, '<mark>$1</mark>')
  }

  /**
   * Reset search state
   */
  function reset(): void {
    clearAll()
    clearResults()
    isSearching.value = false
  }

  return {
    // State
    query,
    filters,
    results,
    isSearching,

    // Computed
    hasQuery,
    hasFilters,
    hasActiveSearch,
    resultCount,
    searchParams,
    debouncedQuery,

    // Actions
    setQuery,
    clearQuery,
    setFilter,
    removeFilter,
    setFilters,
    clearFilters,
    clearAll,
    setResults,
    clearResults,
    search,
    filterItems,
    highlightMatches,
    reset,
  }
}

// ==================== Helper Functions ====================

/**
 * Simple text search function
 */
export function searchInText(text: string, query: string, caseSensitive = false): boolean {
  if (!query) return true

  const searchText = caseSensitive ? text : text.toLowerCase()
  const searchQuery = caseSensitive ? query : query.toLowerCase()

  return searchText.includes(searchQuery)
}

/**
 * Search in object fields
 */
export function searchInObject(
  obj: Record<string, unknown>,
  query: string,
  fields: string[],
  caseSensitive = false
): boolean {
  if (!query) return true

  return fields.some((field) => {
    const value = obj[field]
    if (value === null || value === undefined) return false

    return searchInText(String(value), query, caseSensitive)
  })
}
