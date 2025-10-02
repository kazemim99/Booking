import { ref, computed } from 'vue'
import type { PaginationParams } from '@/core/types/pagination.types'

export function usePagination(initialPageSize = 20) {
  const currentPage = ref(1)
  const pageSize = ref(initialPageSize)
  const total = ref(0)

  const totalPages = computed(() => Math.ceil(total.value / pageSize.value))
  const hasNextPage = computed(() => currentPage.value < totalPages.value)
  const hasPreviousPage = computed(() => currentPage.value > 1)

  function setPage(page: number): void {
    if (page >= 1 && page <= totalPages.value) {
      currentPage.value = page
    }
  }

  function nextPage(): void {
    if (hasNextPage.value) {
      currentPage.value++
    }
  }

  function previousPage(): void {
    if (hasPreviousPage.value) {
      currentPage.value--
    }
  }

  function setPageSize(size: number): void {
    pageSize.value = size
    currentPage.value = 1
  }

  function setTotal(totalItems: number): void {
    total.value = totalItems
  }

  function reset(): void {
    currentPage.value = 1
    total.value = 0
  }

  const params = computed<PaginationParams>(() => ({
    page: currentPage.value,
    pageSize: pageSize.value,
  }))

  return {
    currentPage,
    pageSize,
    total,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    params,
    setPage,
    nextPage,
    previousPage,
    setPageSize,
    setTotal,
    reset,
  }
}
