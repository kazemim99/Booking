import apiClient from '../utils/axios'
import type { User, PaginatedResponse } from '../types'

export interface UsersQuery {
  pageNumber?: number
  pageSize?: number
  search?: string
  role?: string
  isActive?: boolean
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
}

export interface CreateUserRequest {
  email: string
  password: string
  firstName?: string
  lastName?: string
  phoneNumber?: string
  role: 'Admin' | 'Provider' | 'Client'
}

export interface UpdateUserRequest {
  firstName?: string
  lastName?: string
  phoneNumber?: string
  role?: string
  isActive?: boolean
}

export const usersApi = {
  getUsers: async (query: UsersQuery = {}): Promise<PaginatedResponse<User>> => {
    const response = await apiClient.get<{ data: PaginatedResponse<User> }>('/Users/search', { params: query })
    return response.data.data
  },

  getUserById: async (id: string): Promise<User> => {
    const response = await apiClient.get<User>(`/Users/${id}`)
    return response.data
  },

  createUser: async (data: CreateUserRequest): Promise<User> => {
    const response = await apiClient.post<User>('/Users', data)
    return response.data
  },

  updateUser: async (id: string, data: UpdateUserRequest): Promise<User> => {
    const response = await apiClient.put<User>(`/Users/${id}`, data)
    return response.data
  },

  deleteUser: async (id: string): Promise<void> => {
    await apiClient.delete(`/Users/${id}`)
  },

  toggleUserStatus: async (id: string): Promise<User> => {
    const response = await apiClient.patch<User>(`/Users/${id}/toggle-status`)
    return response.data
  },

  resetPassword: async (id: string, newPassword: string): Promise<void> => {
    await apiClient.post(`/Users/${id}/reset-password`, { newPassword })
  },
}
