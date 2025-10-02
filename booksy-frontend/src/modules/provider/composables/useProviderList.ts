// src/modules/provider/composables/useProviderList.ts
import { ref, computed } from 'vue'
import { providerService } from '../services/provider.service'
import { usePagination } from '@/core/composables/usePagination'
import type {
  ProviderSummary,
  ProviderSearchFilters,
  ProviderType,
  ProviderStatus,
} from '../types/provider.types'
import type { PagedResult } from '@/core/types/common.types'

export function useProviderList() {
  const loading = ref(false)
  const error = ref<string | null>(null)
  const providers = ref<ProviderSummary[]>([])

  const {
    currentPage,
    pageSize,
    total,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    setPage,
    nextPage,
    previousPage,
    setPageSize,
    setTotal,
  } = usePagination(12)

  // Filters
  const filters = ref<ProviderSearchFilters>({
    searchTerm: '',
    type: undefined,
    status: undefined,
    city: '',
    minRating: undefined,
    pageNumber: 1,
    pageSize: pageSize.value,
  })

  // Sorting
  const sortBy = ref<string>('')
  const sortOrder = ref<'asc' | 'desc'>('asc')

  // Computed
  const hasFilters = computed(() => {
    return (
      filters.value.searchTerm ||
      filters.value.type ||
      filters.value.status ||
      filters.value.city ||
      filters.value.minRating
    )
  })

  // Methods
  async function fetchProviders(): Promise<void> {
    loading.value = true
    error.value = null

    try {
      const result: PagedResult<ProviderSummary> = await providerService.searchProviders({
        ...filters.value,
        pageNumber: currentPage.value,
        pageSize: pageSize.value,
        sortBy: sortBy.value,
        sortOrder: sortOrder.value,
      })

      providers.value = result.items
      setTotal(result.totalCount)
    } catch (err: unknown) {
      if (err instanceof Error) {
        error.value = err.message
      } else {
        error.value = 'Failed to load providers'
      }
      providers.value = []
    } finally {
      loading.value = false
    }
  }

  async function searchProviders(searchTerm: string): Promise<void> {
    filters.value.searchTerm = searchTerm
    currentPage.value = 1
    await fetchProviders()
  }

  async function filterByType(type: ProviderType | undefined): Promise<void> {
    filters.value.type = type
    currentPage.value = 1
    await fetchProviders()
  }

  async function filterByStatus(status: ProviderStatus | undefined): Promise<void> {
    filters.value.status = status
    currentPage.value = 1
    await fetchProviders()
  }

  async function filterByCity(city: string): Promise<void> {
    filters.value.city = city
    currentPage.value = 1
    await fetchProviders()
  }

  function clearFilters(): void {
    filters.value = {
      searchTerm: '',
      type: undefined,
      status: undefined,
      city: '',
      minRating: undefined,
      pageNumber: 1,
      pageSize: pageSize.value,
    }
    currentPage.value = 1
    fetchProviders()
  }

  function handleSort(data: { column: string; direction: 'asc' | 'desc' }): void {
    sortBy.value = data.column
    sortOrder.value = data.direction
    fetchProviders()
  }

  function handlePageChange(page: number): void {
    setPage(page)
    fetchProviders()
  }

  function handlePageSizeChange(size: number): void {
    setPageSize(size)
    fetchProviders()
  }

  // Auto-fetch on mount
  // fetchProviders()

  return {
    // State
    loading,
    error,
    providers,

    // Pagination
    currentPage,
    pageSize,
    total,
    totalPages,
    hasNextPage,
    hasPreviousPage,

    // Filters
    filters,
    hasFilters,

    // Methods
    fetchProviders,
    searchProviders,
    filterByType,
    filterByStatus,
    filterByCity,
    clearFilters,
    handleSort,
    handlePageChange,
    handlePageSizeChange,
    nextPage,
    previousPage,
  }
}
