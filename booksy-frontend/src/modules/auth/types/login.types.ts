export interface LoginFormData extends Record<string, unknown> {
  email: string
  password: string
  rememberMe: boolean
}

export interface LoginCredentials {
  email: string
  password: string
  rememberMe?: boolean
}
