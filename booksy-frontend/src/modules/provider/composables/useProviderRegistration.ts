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
  GalleryImageData,
} from '../types/registration.types'

const TOTAL_STEPS = 9 // Updated for new Figma flow: BusinessInfo, Category, Location, Services, Staff, Hours, Gallery, Feedback, Complete

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
    galleryImages: [],
    feedbackText: undefined,
  },
  isLoading: false,
  error: null,
  isDirty: false,
})

// Track if draft is currently being loaded to prevent duplicate calls
let isDraftLoading = false

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

  const addGalleryImage = (image: GalleryImageData) => {
    registrationState.value.data.galleryImages.push(image)
    registrationState.value.isDirty = true
  }

  const removeGalleryImage = (imageId: string) => {
    registrationState.value.data.galleryImages = registrationState.value.data.galleryImages.filter(
      (img) => img.id !== imageId,
    )
    registrationState.value.isDirty = true
  }

  const setGalleryImages = (images: GalleryImageData[]) => {
    registrationState.value.data.galleryImages = images
    registrationState.value.isDirty = true
  }

  const setFeedbackText = (text: string) => {
    registrationState.value.data.feedbackText = text
    registrationState.value.isDirty = true
  }

  // Validation
  const validateStep = (step: RegistrationStep): ValidationResult => {
    const errors: Record<string, string> = {}

    console.log('Validating step:', step)

    switch (step) {
      case 1: // Business Info (NEW STEP 1)
        const { businessName, ownerFirstName, ownerLastName } =
          registrationState.value.data.businessInfo
        console.log('Step 1 validation - BusinessInfo:', { businessName, ownerFirstName, ownerLastName })
        if (!businessName?.trim()) errors.businessName = 'Business name is required'
        if (!ownerFirstName?.trim()) errors.ownerFirstName = 'First name is required'
        if (!ownerLastName?.trim()) errors.ownerLastName = 'Last name is required'
        break

      case 2: // Category Selection (NEW STEP 2)
        console.log('Step 2 validation - Category:', registrationState.value.data.categoryId)
        if (!registrationState.value.data.categoryId) {
          errors.category = 'Please select a business category'
        }
        break

      case 3: // Address & Location
        const { addressLine1, city, province } = registrationState.value.data.address
        console.log('Step 3 validation - Address:', { addressLine1, city, province })
        if (!addressLine1?.trim()) errors.addressLine1 = 'Street address is required'
        if (!city?.trim()) errors.city = 'City is required'
        if (!province?.trim()) errors.province = 'Province is required'
        break

      case 4: // Services - At least one required
        console.log('Step 4 validation - Services count:', registrationState.value.data.services.length)
        if (registrationState.value.data.services.length === 0) {
          errors.services = 'Please add at least one service'
        }
        break

      case 5: // Team Members - Optional
        console.log('Step 5 validation - Team Members (optional)')
        break

      case 6: // Business Hours
        const hasOpenDay = registrationState.value.data.businessHours.some((h) => h.isOpen)
        console.log('Step 6 validation - Business Hours, has open day:', hasOpenDay)
        if (!hasOpenDay) {
          errors.businessHours = 'Please select at least one working day'
        }
        break

      case 7: // Gallery - Optional (NEW)
        console.log('Step 7 validation - Gallery (optional)')
        break

      case 8: // Feedback - Optional (NEW)
        console.log('Step 8 validation - Feedback (optional)')
        break

      case 9: // Completion Screen - No validation needed
        console.log('Step 9 validation - Completion (no validation)')
        break
    }

    console.log('Validation result:', { isValid: Object.keys(errors).length === 0, errors })

    return {
      isValid: Object.keys(errors).length === 0,
      errors,
    }
  }

  const canProceedToNextStep = (): boolean => {
    const result = validateStep(registrationState.value.currentStep).isValid
    console.log('canProceedToNextStep result:', result)
    return result
  }

  // Save draft - Creates provider draft at Step 3 (after location)
  const saveDraft = async (): Promise<{ success: boolean; message?: string; providerId?: string }> => {
    registrationState.value.isLoading = true
    registrationState.value.error = null

    try {
      // Only save draft at step 3 or later (after business info, category, location)
      if (registrationState.value.currentStep < 3) {
        return { success: true, message: 'No draft to save yet' }
      }

      const { businessInfo, address, location, categoryId } = registrationState.value.data

      // Validate required data for draft creation
      if (!businessInfo.businessName || !categoryId || !address.addressLine1) {
        return { success: false, message: 'Missing required information for draft' }
      }

      // Create draft provider request
      const draftRequest = {
        ownerFirstName: businessInfo.ownerFirstName || '',
        ownerLastName: businessInfo.ownerLastName || '',
        businessName: businessInfo.businessName,
        businessDescription: businessInfo.businessDescription || '',
        category: categoryId,
        phoneNumber: businessInfo.phoneNumber || '',
        email: authStore.user?.email || '',
        addressLine1: address.addressLine1,
        addressLine2: address.addressLine2,
        city: address.city || '',
        province: address.province || '',
        postalCode: address.zipCode || '',
        latitude: location.latitude || 0,
        longitude: location.longitude || 0,
      }

      const response = await providerRegistrationService.saveStep3Location(draftRequest)

      // Update tokens if returned (after provider aggregate is created)
      if (response.accessToken && response.refreshToken) {
        authStore.setToken(response.accessToken)
        authStore.setRefreshToken(response.refreshToken)
        console.log('✅ Updated tokens with provider claims (providerId now included)')
      }

      registrationState.value.isDirty = false

      return {
        success: true,
        message: response.message,
        providerId: response.providerId
      }
    } catch (error: any) {
      const message = error.response?.data?.message || 'Failed to save draft'
      registrationState.value.error = message
      return { success: false, message }
    } finally {
      registrationState.value.isLoading = false
    }
  }

  // Save services - Calls Step 4 endpoint
  const saveServices = async (providerId: string): Promise<{ success: boolean; message?: string }> => {
    registrationState.value.isLoading = true
    registrationState.value.error = null

    try {
      const services = registrationState.value.data.services.map((service) => ({
        name: service.name,
        durationHours: service.durationHours,
        durationMinutes: service.durationMinutes,
        price: service.price,
        priceType: service.priceType || 'fixed',
      }))

      if (services.length === 0) {
        throw new Error('Please add at least one service')
      }

      const response = await providerRegistrationService.saveStep4Services(providerId, services)

      console.log('✅ Services saved:', response)

      registrationState.value.isDirty = false

      return {
        success: true,
        message: response.message || 'Services saved successfully',
      }
    } catch (error: any) {
      console.error('❌ Failed to save services:', error)

      const message = error.response?.data?.message || error.message || 'Failed to save services'
      registrationState.value.error = message

      return { success: false, message }
    } finally {
      registrationState.value.isLoading = false
    }
  }

  // Save staff - Calls Step 5 endpoint
  const saveStaff = async (providerId: string): Promise<{ success: boolean; message?: string }> => {
    registrationState.value.isLoading = true
    registrationState.value.error = null

    try {
      const staffMembers = registrationState.value.data.teamMembers.map((member) => ({
        name: member.name,
        email: member.email || '',
        phoneNumber: member.phoneNumber || '',
        position: member.position || 'stylist',
      }))

      // Staff is optional, allow proceeding even without staff members
      const response = await providerRegistrationService.saveStep5Staff(providerId, staffMembers)

      console.log('✅ Staff saved:', response)

      registrationState.value.isDirty = false

      return {
        success: true,
        message: response.message || 'Staff saved successfully',
      }
    } catch (error: any) {
      console.error('❌ Failed to save staff:', error)

      const message = error.response?.data?.message || error.message || 'Failed to save staff'
      registrationState.value.error = message

      return { success: false, message }
    } finally {
      registrationState.value.isLoading = false
    }
  }

  // Save working hours - Calls Step 6 endpoint
  const saveWorkingHours = async (providerId: string): Promise<{ success: boolean; message?: string }> => {
    registrationState.value.isLoading = true
    registrationState.value.error = null

    try {
      const businessHours = registrationState.value.data.businessHours.map((day) => ({
        dayOfWeek: day.dayOfWeek,
        isOpen: day.isOpen,
        openTime: day.openTime
          ? { hours: day.openTime.hours, minutes: day.openTime.minutes }
          : { hours: 0, minutes: 0 },
        closeTime: day.closeTime
          ? { hours: day.closeTime.hours, minutes: day.closeTime.minutes }
          : { hours: 0, minutes: 0 },
        breaks: day.breaks?.map((b) => ({
          start: { hours: b.start.hours, minutes: b.start.minutes },
          end: { hours: b.end.hours, minutes: b.end.minutes },
        })) || [],
      }))

      const hasOpenDays = businessHours.some((day) => day.isOpen)
      if (!hasOpenDays) {
        throw new Error('Please set at least one open day')
      }

      const response = await providerRegistrationService.saveStep6WorkingHours(providerId, businessHours)

      console.log('✅ Working hours saved:', response)

      registrationState.value.isDirty = false

      return {
        success: true,
        message: response.message || 'Working hours saved successfully',
      }
    } catch (error: any) {
      console.error('❌ Failed to save working hours:', error)

      const message = error.response?.data?.message || error.message || 'Failed to save working hours'
      registrationState.value.error = message

      return { success: false, message }
    } finally {
      registrationState.value.isLoading = false
    }
  }

  // Save gallery - Gallery images are uploaded in real-time in GalleryStep.vue
  // via galleryStore.uploadImages(), which auto-updates registration step to 7
  // This function just returns success since the work is already done
  const saveGallery = async (providerId: string): Promise<{ success: boolean; message?: string }> => {
    registrationState.value.isLoading = true
    registrationState.value.error = null

    try {
      const imagesCount = registrationState.value.data.galleryImages.length

      console.log('✅ Gallery step already complete (images uploaded in real-time):', {
        providerId,
        imagesCount
      })

      registrationState.value.isDirty = false

      return {
        success: true,
        message: imagesCount > 0
          ? `تصاویر گالری ذخیره شد (${imagesCount} تصویر)`
          : 'مرحله گالری تکمیل شد',
      }
    } catch (error: any) {
      console.error('❌ Failed to complete gallery step:', error)

      const message = error.response?.data?.message || error.message || 'Failed to complete gallery step'
      registrationState.value.error = message

      return { success: false, message }
    } finally {
      registrationState.value.isLoading = false
    }
  }

  // Save feedback - Calls Step 8 endpoint
  const saveFeedback = async (providerId: string): Promise<{ success: boolean; message?: string }> => {
    registrationState.value.isLoading = true
    registrationState.value.error = null

    try {
      const feedbackText = registrationState.value.data.feedbackText || ''

      // Feedback is optional
      const response = await providerRegistrationService.saveStep8Feedback(providerId, feedbackText)

      console.log('✅ Feedback saved:', response)

      registrationState.value.isDirty = false

      return {
        success: true,
        message: response.message || 'Feedback saved successfully',
      }
    } catch (error: any) {
      console.error('❌ Failed to save feedback:', error)

      const message = error.response?.data?.message || error.message || 'Failed to save feedback'
      registrationState.value.error = message

      return { success: false, message }
    } finally {
      registrationState.value.isLoading = false
    }
  }

  // Complete registration - Calls the complete endpoint (Step 9)
  const completeRegistration = async (providerId?: string): Promise<{
    success: boolean
    message?: string
    providerId?: string
  }> => {
    registrationState.value.isLoading = true
    registrationState.value.error = null

    try {
      // Get provider ID from parameter or draft response
      if (!providerId) {
        // Try to get from draft first
        const draftResponse = await providerRegistrationService.getRegistrationProgress()
        if (draftResponse.hasDraft && draftResponse.draftData) {
          providerId = draftResponse.draftData.providerId
        } else {
          throw new Error('No provider draft found. Please complete steps 1-3 first.')
        }
      }

      // Validate critical steps before completion (Steps 4 & 6 are required)
      const servicesValidation = validateStep(4) // Services
      const hoursValidation = validateStep(6) // Business Hours

      if (!servicesValidation.isValid) {
        throw new Error('Please add at least one service before completing registration')
      }

      if (!hoursValidation.isValid) {
        throw new Error('Please set business hours before completing registration')
      }

      console.log('✅ Validation passed. Completing registration for provider:', providerId)

      // Call backend API to complete registration (Step 9)
      const response = await providerRegistrationService.saveStep9Complete(providerId)

      console.log('✅ Provider registration completed:', response)

      // If registration returned new tokens with provider claims, update auth store
      if (response.accessToken && response.refreshToken) {
        console.log('✅ Updating authentication tokens with provider claims')
        authStore.setToken(response.accessToken)
        authStore.setRefreshToken(response.refreshToken)

        // Update provider status in auth store
        authStore.setProviderStatus(response.status as any, response.providerId)
      }

      registrationState.value.isDirty = false

      return {
        success: true,
        message: response.message || 'Registration completed successfully. Pending admin approval.',
        providerId: response.providerId,
      }
    } catch (error: any) {
      console.error('❌ Registration completion failed:', error)

      // Check for validation errors from backend
      let message = 'Failed to complete registration. Please try again.'

      if (error.response?.data?.error) {
        const errorData = error.response.data.error

        // If there are detailed validation errors, format them
        if (errorData.errors && typeof errorData.errors === 'object') {
          const validationErrors = Object.entries(errorData.errors)
            .map(([field, messages]) => {
              const fieldMessages = Array.isArray(messages) ? messages : [messages]
              return `${field}: ${fieldMessages.join(', ')}`
            })
            .join('\n')

          message = `خطای اعتبارسنجی:\n${validationErrors}`
        } else if (errorData.message) {
          message = errorData.message
        }
      } else if (error.response?.data?.message) {
        message = error.response.data.message
      } else if (error.message) {
        message = error.message
      }

      registrationState.value.error = message
      return { success: false, message }
    } finally {
      registrationState.value.isLoading = false
    }
  }

  // Load existing draft on initialization
  const loadDraft = async (): Promise<{ success: boolean; message?: string; providerId?: string }> => {
    // Prevent duplicate concurrent calls
    if (isDraftLoading) {
      console.log('⏭️  Draft already loading, skipping duplicate call')
      return { success: false, message: 'Draft load already in progress' }
    }

    isDraftLoading = true
    registrationState.value.isLoading = true

    try {
      // Don't try to load draft if user is not authenticated
      // (e.g., new users coming from phone verification)
      if (!authStore.isAuthenticated) {
        console.log('⏭️  Skipping draft load - user not authenticated (new registration)')
        return { success: true, message: 'No authentication - starting fresh registration' }
      }

      const response = await providerRegistrationService.getRegistrationProgress()

      if (response.hasDraft && response.draftData) {
        // Populate registration state with draft data
        const draft = response.draftData

        // Business info and category
        registrationState.value.data.categoryId = draft.businessInfo.category
        registrationState.value.data.businessInfo = {
          ...registrationState.value.data.businessInfo,
          businessName: draft.businessInfo.businessName,
          businessDescription: draft.businessInfo.businessDescription,
          phoneNumber: draft.businessInfo.phoneNumber,
        }

        // Address and location
        registrationState.value.data.address = {
          ...registrationState.value.data.address,
          addressLine1: draft.location.addressLine1,
          addressLine2: draft.location.addressLine2 || undefined,
          city: draft.location.city,
          province: draft.location.province,
          zipCode: draft.location.postalCode,
        }
        registrationState.value.data.location = {
          latitude: draft.location.latitude,
          longitude: draft.location.longitude,
        }

        // Services
        registrationState.value.data.services = draft.services.map((s) => ({
          id: s.id,
          name: s.name,
          durationHours: s.durationHours,
          durationMinutes: s.durationMinutes,
          price: s.price,
          priceType: s.priceType,
        }))

        // Staff
        registrationState.value.data.teamMembers = draft.staff.map((s) => ({
          id: s.id,
          name: s.name,
          email: s.email || '',
          phoneNumber: s.phoneNumber,
          countryCode: '+98',
          position: s.position,
          isOwner: false,
        }))

        // Business hours
        registrationState.value.data.businessHours = draft.businessHours.map((h) => ({
          dayOfWeek: h.dayOfWeek,
          isOpen: h.isOpen,
          openTime:
            h.openTimeHours !== null && h.openTimeMinutes !== null
              ? { hours: h.openTimeHours, minutes: h.openTimeMinutes }
              : null,
          closeTime:
            h.closeTimeHours !== null && h.closeTimeMinutes !== null
              ? { hours: h.closeTimeHours, minutes: h.closeTimeMinutes }
              : null,
          breaks: h.breaks?.map((br) => ({
            id: `break-${br.startTimeHours}-${br.startTimeMinutes}`,
            start: { hours: br.startTimeHours, minutes: br.startTimeMinutes },
            end: { hours: br.endTimeHours, minutes: br.endTimeMinutes },
          })) || [],
        }))

        // Gallery images
        registrationState.value.data.galleryImages = draft.galleryImages.map((img) => ({
          id: img.displayOrder.toString(),
          url: img.imageUrl,
          thumbnailUrl: img.thumbnailUrl || undefined,
        }))

        // Set current step from draft
        registrationState.value.currentStep = draft.registrationStep as RegistrationStep
        registrationState.value.data.step = draft.registrationStep as RegistrationStep

        console.log('✅ Draft loaded successfully:', {
          step: draft.registrationStep,
          services: draft.services.length,
          staff: draft.staff.length,
          businessHours: draft.businessHours.filter((h) => h.isOpen).length,
          gallery: draft.galleryImages.length,
        })

        return { success: true, message: 'Draft loaded successfully', providerId: draft.providerId }
      }

      return { success: true, message: 'No draft found' }
    } catch (error: any) {
      console.error('Failed to load draft:', error)
      return { success: false, message: 'Failed to load draft' }
    } finally {
      registrationState.value.isLoading = false
      isDraftLoading = false
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
        galleryImages: [],
        feedbackText: undefined,
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
    addGalleryImage,
    removeGalleryImage,
    setGalleryImages,
    setFeedbackText,
    validateStep,
    canProceedToNextStep,
    loadDraft,
    saveDraft,
    saveServices,
    saveStaff,
    saveWorkingHours,
    saveGallery,
    saveFeedback,
    completeRegistration,
    resetRegistration,
    handleBeforeUnload,
  }
}
