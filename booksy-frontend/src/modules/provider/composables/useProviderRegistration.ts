import { ref, computed, watch } from 'vue'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { providerRegistrationService } from '../services/provider-registration.service'
import type {
  RegistrationStep,
  RegistrationState,
  BusinessCategoryId,
  BusinessInfo,
  BusinessAddress,
  BusinessLocation,
  DayHours,
  Service,
  AssistanceOption,
  TeamMember,
  ValidationResult,
} from '../types/registration.types'

const TOTAL_STEPS = 8

// Persistent state across component re-renders
const registrationState = ref<RegistrationState>({
  currentStep: 1,
  data: {
    step: 1,
    userId: '',
    categoryId: null,
    businessInfo: {},
    address: {},
    location: {},
    businessHours: initializeBusinessHours(),
    services: [],
    assistanceOptions: [],
    teamMembers: [],
  },
  isLoading: false,
  error: null,
  isDirty: false,
})

// Initialize business hours with default closed for all days
function initializeBusinessHours(): DayHours[] {
  return Array.from({ length: 7 }, (_, i) => ({
    dayOfWeek: i,
    isOpen: false,
    openTime: null,
    closeTime: null,
    breaks: [],
  }))
}

export function useProviderRegistration() {
  const authStore = useAuthStore()

  // Computed
  const currentStep = computed(() => registrationState.value.currentStep)
  const progressPercentage = computed(
    () => (registrationState.value.currentStep / TOTAL_STEPS) * 100,
  )
  const canGoBack = computed(() => registrationState.value.currentStep > 1)
  const canGoNext = computed(() => registrationState.value.currentStep < TOTAL_STEPS)
  const isFirstStep = computed(() => registrationState.value.currentStep === 1)
  const isLastStep = computed(() => registrationState.value.currentStep === TOTAL_STEPS)

  // Initialize with user data
  const initialize = () => {
    const user = authStore.user
    if (user) {
      registrationState.value.data.userId = user.id
      registrationState.value.data.businessInfo.phoneNumber = user.phoneNumber || ''
    }
  }

  // Navigation
  const goToStep = (step: RegistrationStep) => {
    if (step >= 1 && step <= TOTAL_STEPS) {
      registrationState.value.currentStep = step
      registrationState.value.data.step = step
    }
  }

  const nextStep = () => {
    if (canGoNext.value) {
      registrationState.value.currentStep = (registrationState.value.currentStep +
        1) as RegistrationStep
      registrationState.value.data.step = registrationState.value.currentStep
    }
  }

  const previousStep = () => {
    if (canGoBack.value) {
      registrationState.value.currentStep = (registrationState.value.currentStep -
        1) as RegistrationStep
      registrationState.value.data.step = registrationState.value.currentStep
    }
  }

  // Data setters
  const setCategory = (categoryId: BusinessCategoryId) => {
    registrationState.value.data.categoryId = categoryId
    registrationState.value.isDirty = true
  }

  const setBusinessInfo = (info: Partial<BusinessInfo>) => {
    registrationState.value.data.businessInfo = {
      ...registrationState.value.data.businessInfo,
      ...info,
    }
    registrationState.value.isDirty = true
  }

  const setAddress = (address: Partial<BusinessAddress>) => {
    registrationState.value.data.address = {
      ...registrationState.value.data.address,
      ...address,
    }
    registrationState.value.isDirty = true
  }

  const setLocation = (location: Partial<BusinessLocation>) => {
    registrationState.value.data.location = {
      ...registrationState.value.data.location,
      ...location,
    }
    registrationState.value.isDirty = true
  }

  const setBusinessHours = (hours: DayHours[]) => {
    registrationState.value.data.businessHours = hours
    registrationState.value.isDirty = true
  }

  const updateDayHours = (dayOfWeek: number, hours: Partial<DayHours>) => {
    const index = registrationState.value.data.businessHours.findIndex(
      (h) => h.dayOfWeek === dayOfWeek,
    )
    if (index !== -1) {
      registrationState.value.data.businessHours[index] = {
        ...registrationState.value.data.businessHours[index],
        ...hours,
      }
      registrationState.value.isDirty = true
    }
  }

  const copyHoursToOtherDays = (sourceDayOfWeek: number, targetDays: number[]) => {
    const sourceHours = registrationState.value.data.businessHours.find(
      (h) => h.dayOfWeek === sourceDayOfWeek,
    )
    if (sourceHours) {
      targetDays.forEach((dayOfWeek) => {
        updateDayHours(dayOfWeek, {
          isOpen: sourceHours.isOpen,
          openTime: sourceHours.openTime ? { ...sourceHours.openTime } : null,
          closeTime: sourceHours.closeTime ? { ...sourceHours.closeTime } : null,
          breaks: sourceHours.breaks.map((b) => ({ ...b })),
        })
      })
    }
  }

  const addService = (service: Service) => {
    registrationState.value.data.services.push(service)
    registrationState.value.isDirty = true
  }

  const removeService = (serviceId: string) => {
    registrationState.value.data.services = registrationState.value.data.services.filter(
      (s) => s.id !== serviceId,
    )
    registrationState.value.isDirty = true
  }

  const updateService = (serviceId: string, updates: Partial<Service>) => {
    const index = registrationState.value.data.services.findIndex((s) => s.id === serviceId)
    if (index !== -1) {
      registrationState.value.data.services[index] = {
        ...registrationState.value.data.services[index],
        ...updates,
      }
      registrationState.value.isDirty = true
    }
  }

  const setAssistanceOptions = (options: AssistanceOption[]) => {
    registrationState.value.data.assistanceOptions = options
    registrationState.value.isDirty = true
  }

  const addTeamMember = (member: TeamMember) => {
    registrationState.value.data.teamMembers.push(member)
    registrationState.value.isDirty = true
  }

  const removeTeamMember = (memberId: string) => {
    registrationState.value.data.teamMembers = registrationState.value.data.teamMembers.filter(
      (m) => m.id !== memberId,
    )
    registrationState.value.isDirty = true
  }

  // Validation
  const validateStep = (step: RegistrationStep): ValidationResult => {
    const errors: Record<string, string> = {}

    switch (step) {
      case 1: // Category
        if (!registrationState.value.data.categoryId) {
          errors.category = 'Please select a business category'
        }
        break

      case 2: // Business Info
        const { businessName, ownerFirstName, ownerLastName } =
          registrationState.value.data.businessInfo
        if (!businessName?.trim()) errors.businessName = 'Business name is required'
        if (!ownerFirstName?.trim()) errors.ownerFirstName = 'First name is required'
        if (!ownerLastName?.trim()) errors.ownerLastName = 'Last name is required'
        break

      case 3: // Address & Location
        const { addressLine1, city, zipCode } = registrationState.value.data.address
        if (!addressLine1?.trim()) errors.addressLine1 = 'Street address is required'
        if (!city?.trim()) errors.city = 'City is required'
        if (!zipCode?.trim()) errors.zipCode = 'Zip code is required'
        break

      case 4: // Business Hours
        const hasOpenDay = registrationState.value.data.businessHours.some((h) => h.isOpen)
        if (!hasOpenDay) {
          errors.businessHours = 'Please select at least one working day'
        }
        break

      case 5: // Services - At least one required
        if (registrationState.value.data.services.length === 0) {
          errors.services = 'Please add at least one service'
        }
        break

      case 6: // Assistance Options - Optional
        break

      case 7: // Team Members - Optional
        break

      case 8: // Complete - All previous validations
        break
    }

    return {
      isValid: Object.keys(errors).length === 0,
      errors,
    }
  }

  const canProceedToNextStep = (): boolean => {
    return validateStep(registrationState.value.currentStep).isValid
  }

  // Save draft
  const saveDraft = async (): Promise<{ success: boolean; message?: string }> => {
    registrationState.value.isLoading = true
    registrationState.value.error = null

    try {
      // TODO: Implement API call to save draft
      // const response = await api.post('/api/providers/registration/draft', {
      //   step: registrationState.value.currentStep,
      //   data: registrationState.value.data,
      // })

      // Simulate API call
      await new Promise((resolve) => setTimeout(resolve, 500))

      registrationState.value.isDirty = false
      return { success: true, message: 'Draft saved successfully' }
    } catch (error) {
      const message = 'Failed to save draft'
      registrationState.value.error = message
      return { success: false, message }
    } finally {
      registrationState.value.isLoading = false
    }
  }

  // Complete registration
  const completeRegistration = async (): Promise<{
    success: boolean
    message?: string
    providerId?: string
  }> => {
    registrationState.value.isLoading = true
    registrationState.value.error = null

    try {
      // Validate all steps before submission (excluding step 8 which is complete screen)
      for (let step = 1; step <= TOTAL_STEPS - 1; step++) {
        const validation = validateStep(step as RegistrationStep)
        if (!validation.isValid) {
          const errorMessages = Object.entries(validation.errors)
            .map(([field, msg]) => `${field}: ${msg}`)
            .join(', ')
          throw new Error(`Step ${step} validation failed: ${errorMessages}`)
        }
      }

      console.log('✅ All steps validated. Submitting registration...', registrationState.value.data)

      // Call backend API to register provider with all data
      const response = await providerRegistrationService.registerProviderFull(
        registrationState.value.data,
      )

      console.log('✅ Provider registered successfully:', response)

      registrationState.value.isDirty = false

      return {
        success: true,
        message: response.message || 'Registration completed successfully. Pending admin approval.',
        providerId: response.providerId,
      }
    } catch (error: any) {
      console.error('❌ Registration failed:', error)

      const message =
        error.response?.data?.message ||
        error.message ||
        'Failed to complete registration. Please try again.'

      registrationState.value.error = message
      return { success: false, message }
    } finally {
      registrationState.value.isLoading = false
    }
  }

  // Reset registration
  const resetRegistration = () => {
    registrationState.value = {
      currentStep: 1,
      data: {
        step: 1,
        userId: authStore.user?.id || '',
        categoryId: null,
        businessInfo: {
          phoneNumber: authStore.user?.phoneNumber || '',
        },
        address: {},
        location: {},
        businessHours: initializeBusinessHours(),
        services: [],
        assistanceOptions: [],
        teamMembers: [],
      },
      isLoading: false,
      error: null,
      isDirty: false,
    }
  }

  // Warn before leaving with unsaved changes
  const handleBeforeUnload = (e: BeforeUnloadEvent) => {
    if (registrationState.value.isDirty) {
      e.preventDefault()
      e.returnValue = ''
    }
  }

  // Watch for changes and mark as dirty
  watch(
    () => registrationState.value.data,
    () => {
      registrationState.value.isDirty = true
    },
    { deep: true },
  )

  return {
    // State
    state: registrationState,
    currentStep,
    progressPercentage,
    canGoBack,
    canGoNext,
    isFirstStep,
    isLastStep,

    // Data
    registrationData: computed(() => registrationState.value.data),

    // Methods
    initialize,
    goToStep,
    nextStep,
    previousStep,
    setCategory,
    setBusinessInfo,
    setAddress,
    setLocation,
    setBusinessHours,
    updateDayHours,
    copyHoursToOtherDays,
    addService,
    removeService,
    updateService,
    setAssistanceOptions,
    addTeamMember,
    removeTeamMember,
    validateStep,
    canProceedToNextStep,
    saveDraft,
    completeRegistration,
    resetRegistration,
    handleBeforeUnload,
  }
}
