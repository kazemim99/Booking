import apiClient from '../utils/axios'
import type { Provider, ProviderDetails, PaginatedResponse } from '../types'
import type { ProviderStatus } from '../constants/provider-status'

export interface ProvidersQuery {
  pageNumber?: number
  pageSize?: number
  search?: string
  status?: ProviderStatus
}

const emptyPage = (pageNumber: number, pageSize: number): PaginatedResponse<Provider> => ({
  items: [],
  totalCount: 0,
  pageNumber,
  pageSize,
  totalPages: 0,
})

export const providersApi = {
  /**
   * Two backend endpoints serve this, because only one of them can filter by status:
   *
   * - `/Providers/search` is paginated but accepts no status parameter. The admin used to pass
   *   `status` here, where it bound to nothing and was silently discarded — which is why every
   *   status tab rendered the full provider list.
   * - `/Providers/by-status/{status}` filters for real but returns a plain capped array, so its
   *   pagination is applied here.
   */
  getProviders: async (query: ProvidersQuery = {}): Promise<PaginatedResponse<Provider>> => {
    const pageNumber = query.pageNumber ?? 1
    const pageSize = query.pageSize ?? 10

    if (query.status) {
      const response = await apiClient.get<Provider[]>(`/Providers/by-status/${query.status}`, {
        params: { maxResults: 1000 },
      })

      const term = query.search?.trim().toLowerCase()
      const matches = term
        ? response.data.filter(p =>
            [p.businessName, p.description, p.city].some(f => f?.toLowerCase().includes(term)))
        : response.data

      const start = (pageNumber - 1) * pageSize
      return {
        items: matches.slice(start, start + pageSize),
        totalCount: matches.length,
        pageNumber,
        pageSize,
        totalPages: Math.ceil(matches.length / pageSize),
      }
    }

    const response = await apiClient.get<PaginatedResponse<Provider>>('/Providers/search', {
      params: {
        pageNumber,
        pageSize,
        // The backend parameter is `searchTerm`; sending `search` bound to nothing,
        // so the search box silently returned unfiltered results.
        searchTerm: query.search?.trim() || undefined,
        includeInactive: true,
      },
    })
    return response.data ?? emptyPage(pageNumber, pageSize)
  },

  /** Count of providers awaiting admin verification — drives the queue badge. */
  getPendingVerificationCount: async (): Promise<number> => {
    const response = await apiClient.get<Provider[]>('/Providers/by-status/PendingVerification', {
      params: { maxResults: 1000 },
    })
    return response.data.length
  },

  getProviderById: async (id: string): Promise<ProviderDetails> => {
    const response = await apiClient.get<ProviderDetails>(`/Providers/${id}`)
    return response.data
  },

  /**
   * Moves a provider to Active. This is the only provider lifecycle transition the backend
   * exposes over HTTP; Deactivate exists on the domain but has no endpoint, and Reject,
   * Suspend and Reactivate do not exist at all.
   */
  activateProvider: async (id: string): Promise<void> => {
    await apiClient.post(`/Providers/${id}/activate`)
  },
}
