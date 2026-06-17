import apiClient from '../utils/axios'

export interface LoginRequest {
  email: string
  password: string
  rememberMe?: boolean
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
  tokenType: string
  userInfo: {
    id: string
    email: string
    displayName: string
    roles: string[]
  }
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export const authApi = {
  login: async (credentials: LoginRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<{ data: LoginResponse }>('/Auth/login', {
      email: credentials.email,
      password: credentials.password,
      rememberMe: credentials.rememberMe || false
    })
    // Backend wraps response in { data: {...} }
    return response.data.data
  },

  logout: async (): Promise<void> => {
    await apiClient.post('/Auth/logout')
    localStorage.removeItem('admin_token')
    localStorage.removeItem('admin_refresh_token')
  },

  getCurrentUser: async () => {
    const response = await apiClient.get('/Users/me')
    return response.data
  },

  refreshToken: async (refreshToken: string): Promise<LoginResponse> => {
    const response = await apiClient.post<{ data: LoginResponse }>('/Auth/refresh', {
      refreshToken
    })
    // Backend wraps response in { data: {...} }
    return response.data.data
  },
}
