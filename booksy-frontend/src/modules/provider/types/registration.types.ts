/**
 * Provider Registration Types
 * Types and interfaces for the multi-step provider registration workflow
 */

// Registration Steps
export type RegistrationStep = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8

// Business Categories
export interface BusinessCategory {
  id: string
  name: string
  icon: string
  description?: string
}

export const BUSINESS_CATEGORIES = {
  NAIL_SALON: 'nail_salon',
  HAIR_SALON: 'hair_salon',
  BROWS_LASHES: 'brows_lashes',
  BRAIDS_LOCS: 'braids_locs',
  MASSAGE: 'massage',
  BARBERSHOP: 'barbershop',
  AESTHETIC_MEDICINE: 'aesthetic_medicine',
  DENTAL_ORTHODONTICS: 'dental_orthodontics',
  HAIR_REMOVAL: 'hair_removal',
  HEALTH_FITNESS: 'health_fitness',
  HOME_SERVICES: 'home_services',
} as const

export type BusinessCategoryId = (typeof BUSINESS_CATEGORIES)[keyof typeof BUSINESS_CATEGORIES]

// Business Information
export interface BusinessInfo {
  businessName: string
  ownerFirstName: string
  ownerLastName: string
  phoneNumber: string // From phone verification
}

// Address & Location
export interface BusinessAddress {
  addressLine1: string
  addressLine2?: string
  city: string
  zipCode: string
  isShared: boolean
}

export interface BusinessLocation {
  latitude: number
  longitude: number
  formattedAddress: string
}

// Business Hours
export interface TimeSlot {
  hours: number
  minutes: number
}

export interface BreakTime {
  id: string
  start: TimeSlot
  end: TimeSlot
}

export interface DayHours {
  dayOfWeek: number // 0 = Sunday, 1 = Monday, etc.
  isOpen: boolean
  openTime: TimeSlot | null
  closeTime: TimeSlot | null
  breaks: BreakTime[]
}

// Services
export interface Service {
  id: string
  name: string
  durationHours: number
  durationMinutes: number
  price: number
  priceType: 'fixed' | 'variable'
}

// Assistance Options
export type AssistanceOption =
  | 'more_self_booked_clients'
  | 'selling_products'
  | 'less_canceled_appointments'
  | 'simplified_payment_processing'
  | 'tracking_business_statistics'
  | 'attract_new_clients'
  | 'engage_clients'
  | 'social_media_integration'
  | 'improve_financial_performance'
  | 'other'

// Team Members
export interface TeamMember {
  id: string
  name: string
  email: string
  phoneNumber: string
  countryCode: string
  position: string
  isOwner: boolean
}

// Complete Registration Data
export interface ProviderRegistrationData {
  step: RegistrationStep
  userId: string
  categoryId: BusinessCategoryId | null
  businessInfo: Partial<BusinessInfo>
  address: Partial<BusinessAddress>
  location: Partial<BusinessLocation>
  businessHours: DayHours[]
  services: Service[]
  assistanceOptions: AssistanceOption[]
  teamMembers: TeamMember[]
}

// Registration State
export interface RegistrationState {
  currentStep: RegistrationStep
  data: ProviderRegistrationData
  isLoading: boolean
  error: string | null
  isDirty: boolean // Track if there are unsaved changes
}

// Validation Results
export interface ValidationResult {
  isValid: boolean
  errors: Record<string, string>
}

// API Request/Response Types
export interface SaveRegistrationDraftRequest {
  step: RegistrationStep
  data: Partial<ProviderRegistrationData>
}

export interface SaveRegistrationDraftResponse {
  success: boolean
  message?: string
}

export interface CompleteRegistrationRequest {
  registrationData: ProviderRegistrationData
}

export interface CompleteRegistrationResponse {
  success: boolean
  providerId: string
  status: 'pending' | 'approved' | 'rejected'
  message: string
}

// Step Props
export interface StepProps {
  modelValue?: any
  disabled?: boolean
}

export interface StepEmits {
  (e: 'update:modelValue', value: any): void
  (e: 'next'): void
  (e: 'back'): void
  (e: 'complete'): void
}
