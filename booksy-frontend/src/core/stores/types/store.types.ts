export interface StoreState {
  isLoading: boolean
  error: string | null
}

export interface PaginationState {
  page: number
  pageSize: number
  total: number
  totalPages: number
}

export interface FilterState {
  search: string
  sortBy: string
  sortOrder: 'asc' | 'desc'
  filters: Record<string, any>
}