export interface RegisterFormData extends Record<string, unknown> {
  email: string
  password: string
  confirmPassword: string
  firstName: string
  lastName: string
  phoneNumber: string
  userType: 'Customer' | 'Provider'
  acceptTerms: boolean
}

export interface RegisterData {
  email: string
  password: string
  firstName: string
  lastName: string
  phoneNumber?: string
  userType: 'Customer' | 'Provider'
}
