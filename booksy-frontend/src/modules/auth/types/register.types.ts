import { UserType } from "@/modules/user-management/types/user.types"

export interface RegisterFormData extends Record<string, unknown> {
  email: string
  password: string
  confirmPassword: string
  firstName: string
  lastName: string
  phoneNumber: string
  userType: UserType
  acceptTerms: boolean
}

export interface RegisterData {
  email: string
  password: string
  firstName: string
  lastName: string
  phoneNumber?: string
  userType: UserType
}
