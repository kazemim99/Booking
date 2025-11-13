<template>
  <div class="registration-flow">
    <!-- Step 1: Business Info -->
    <BusinessInfoStep
      v-if="currentStep === 1"
      v-model="registrationData.businessInfo"
      @next="handleNext"
    />

    <!-- Step 2: Category Selection -->
    <CategorySelectionStep
      v-else-if="currentStep === 2"
      v-model="registrationData.categoryId"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 3: Location (NEW Figma Design) -->
    <LocationStep
      v-else-if="currentStep === 3"
      :address="registrationData.address"
      :location="registrationData.location"
      @update:address="(val) => setAddress(val)"
      @update:location="(val) => setLocation(val)"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 4: Services (NEW Figma Design) -->
    <ServicesStepNew
      v-else-if="currentStep === 4"
      v-model="registrationData.services"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 5: Staff/Team Members (NEW Figma Design) -->
    <StaffStepNew
      v-else-if="currentStep === 5"
      v-model="registrationData.teamMembers"
      :owner-name="ownerFullName"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 6: Working Hours (NEW Figma Design) -->
    <WorkingHoursStepNew
      v-else-if="currentStep === 6"
      v-model="registrationData.businessHours"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 7: Gallery (NEW) -->
    <GalleryStep v-else-if="currentStep === 7" @next="handleNext" @back="previousStep" />

    <!-- Step 8: Optional Feedback (NEW) -->
    <OptionalFeedbackStep
      v-else-if="currentStep === 8"
      @next="handleFinalSubmit"
      @back="previousStep"
    />

    <!-- Step 9: Completion (NEW) -->
    <CompletionStep v-else-if="currentStep === 9" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderRegistration } from '../../composables/useProviderRegistration'
import { toastService } from '@/core/services/toast.service'

// New Step Components (Figma Design)
import BusinessInfoStep from '../../components/registration/steps/BusinessInfoStep.vue'
import CategorySelectionStep from '../../components/registration/steps/CategorySelectionStep.vue'
import LocationStep from '../../components/registration/steps/LocationStep.vue'
import ServicesStepNew from '../../components/registration/steps/ServicesStepNew.vue'
import StaffStepNew from '../../components/registration/steps/StaffStepNew.vue'
import WorkingHoursStepNew from '../../components/registration/steps/WorkingHoursStepNew.vue'
import GalleryStep from '../../components/registration/steps/GalleryStep.vue'
import OptionalFeedbackStep from '../../components/registration/steps/OptionalFeedbackStep.vue'
import CompletionStep from '../../components/registration/steps/CompletionStep.vue'

const router = useRouter()

const {
  currentStep,
  registrationData,
  nextStep,
  previousStep,
  setAddress,
  setLocation,
  canProceedToNextStep,
  loadDraft,
  saveDraft,
  saveServices,
  saveStaff,
  saveWorkingHours,
  saveGallery,
  saveFeedback,
  completeRegistration,
  handleBeforeUnload,
  initialize,
  state,
} = useProviderRegistration()

const ownerFullName = computed(() => {
  const { ownerFirstName, ownerLastName } = registrationData.value.businessInfo
  return ownerFirstName && ownerLastName ? `${ownerFirstName} ${ownerLastName}` : 'Business Owner'
})

let draftProviderId: string | undefined = undefined

const handleNext = async () => {
  console.log('🚀 ProviderRegistrationFlow: handleNext called, current step:', currentStep.value)
  console.log('🚀 ProviderRegistrationFlow: Registration data:', registrationData.value)
  console.log('🚀 ProviderRegistrationFlow: draftProviderId:', draftProviderId)

  const canProceed = canProceedToNextStep()
  console.log('🚀 ProviderRegistrationFlow: Can proceed to next step:', canProceed)

  if (!canProceed) {
    console.error('🚀 ProviderRegistrationFlow: Cannot proceed - validation failed')
    toastService.error('لطفاً تمام فیلدهای الزامی را تکمیل کنید')
    return
  }

  console.log('🚀 ProviderRegistrationFlow: Validation passed, continuing...')

  try {
    // Step-specific save operations
    if (currentStep.value === 3) {
      // Step 3: Create provider draft with location
      console.log('✅ Step 3 complete - Creating provider draft...')
      const draftResult = await saveDraft()

      if (draftResult.success && draftResult.providerId) {
        draftProviderId = draftResult.providerId
        console.log('✅ Provider draft created:', draftProviderId)
        toastService.success('اطلاعات شما ذخیره شد')
      } else {
        console.error('❌ Failed to create draft:', draftResult.message)
        toastService.error(draftResult.message || 'خطا در ذخیره اطلاعات')
        return
      }
    } else if (currentStep.value === 4) {
      // Step 4: Save services
      // Try to get provider ID from draftProviderId or load from progress
      let providerId = draftProviderId
      if (!providerId) {
        console.log('⚠️ draftProviderId not found, fetching from registration progress...')
        const draftResult = await loadDraft()
        if (draftResult.success && draftResult.providerId) {
          providerId = draftResult.providerId
          draftProviderId = providerId
          console.log('✅ Provider ID loaded from progress:', providerId)
        }
      }

      if (!providerId) {
        console.error('❌ No provider ID found')
        toastService.error('خطا: شناسه ارائه‌دهنده یافت نشد. لطفاً از مرحله ۳ دوباره شروع کنید.')
        return
      }

      console.log('✅ Step 4 complete - Saving services...')
      const result = await saveServices(providerId)

      if (!result.success) {
        console.error('❌ Failed to save services:', result.message)
        toastService.error(result.message || 'خطا در ذخیره خدمات')
        return
      }
      toastService.success('خدمات ذخیره شد')
    } else if (currentStep.value === 5) {
      // Step 5: Save staff (optional)
      // Try to get provider ID from draftProviderId or load from progress
      let providerId = draftProviderId
      if (!providerId) {
        console.log('⚠️ draftProviderId not found, fetching from registration progress...')
        const draftResult = await loadDraft()
        if (draftResult.success && draftResult.providerId) {
          providerId = draftResult.providerId
          draftProviderId = providerId
          console.log('✅ Provider ID loaded from progress:', providerId)
        }
      }

      if (!providerId) {
        console.error('❌ No provider ID found')
        toastService.error('خطا: شناسه ارائه‌دهنده یافت نشد. لطفاً از مرحله ۳ دوباره شروع کنید.')
        return
      }

      console.log('✅ Step 5 complete - Saving staff...')
      const result = await saveStaff(providerId)

      if (!result.success) {
        console.error('❌ Failed to save staff:', result.message)
        toastService.error(result.message || 'خطا در ذخیره کارکنان')
        return
      }
      toastService.success('کارکنان ذخیره شد')
    } else if (currentStep.value === 6) {
      // Step 6: Save working hours
      // Try to get provider ID from draftProviderId or load from progress
      let providerId = draftProviderId
      if (!providerId) {
        console.log('⚠️ draftProviderId not found, fetching from registration progress...')
        const draftResult = await loadDraft()
        if (draftResult.success && draftResult.providerId) {
          providerId = draftResult.providerId
          draftProviderId = providerId
          console.log('✅ Provider ID loaded from progress:', providerId)
        }
      }

      if (!providerId) {
        console.error('❌ No provider ID found')
        toastService.error('خطا: شناسه ارائه‌دهنده یافت نشد. لطفاً از مرحله ۳ دوباره شروع کنید.')
        return
      }

      console.log('✅ Step 6 complete - Saving working hours...')
      const result = await saveWorkingHours(providerId)

      if (!result.success) {
        console.error('❌ Failed to save working hours:', result.message)
        toastService.error(result.message || 'خطا در ذخیره ساعات کاری')
        return
      }
      toastService.success('ساعات کاری ذخیره شد')
    } else if (currentStep.value === 7) {
      console.log('🚀 ProviderRegistrationFlow: Processing Step 7 (Gallery)')

      // Step 7: Save gallery (optional)
      // Try to get provider ID from draftProviderId or load from progress
      let providerId = draftProviderId
      if (!providerId) {
        console.log('⚠️ draftProviderId not found, fetching from registration progress...')
        const draftResult = await loadDraft()
        if (draftResult.success && draftResult.providerId) {
          providerId = draftResult.providerId
          draftProviderId = providerId
          console.log('✅ Provider ID loaded from progress:', providerId)
        }
      }

      if (!providerId) {
        console.error('❌ No provider ID found')
        toastService.error('خطا: شناسه ارائه‌دهنده یافت نشد. لطفاً از مرحله ۳ دوباره شروع کنید.')
        return
      }

      console.log('✅ Step 7 - Calling saveGallery with provider ID:', providerId)
      const result = await saveGallery(providerId)
      console.log('✅ Step 7 - saveGallery result:', result)

      if (!result.success) {
        console.error('❌ Failed to save gallery:', result.message)
        toastService.error(result.message || 'خطا در ذخیره گالری')
        return
      }
      console.log('✅ Step 7 - Showing success toast')
      toastService.success('گالری ذخیره شد')
      console.log('✅ Step 7 - Success toast shown')
    }

    console.log('🚀 ProviderRegistrationFlow: Proceeding to next step')
    nextStep()
    console.log('🚀 ProviderRegistrationFlow: nextStep() called successfully')
  } catch (error: any) {
    console.error('Error in handleNext:', error)
    toastService.error(error.message || 'خطا در ذخیره اطلاعات')
  }
}

const handleFinalSubmit = async (feedback?: string) => {
  try {
    // Try to get provider ID from draftProviderId or load from progress
    let providerId = draftProviderId
    if (!providerId) {
      console.log('⚠️ draftProviderId not found, fetching from registration progress...')
      const draftResult = await loadDraft()
      if (draftResult.success && draftResult.providerId) {
        providerId = draftResult.providerId
        draftProviderId = providerId
        console.log('✅ Provider ID loaded from progress:', providerId)
      }
    }

    if (!providerId) {
      console.error('❌ No provider ID found')
      toastService.error('خطا: شناسه ارائه‌دهنده یافت نشد. لطفاً از مرحله ۳ دوباره شروع کنید.')
      return
    }

    // Step 8: Save feedback (optional)
    console.log('✅ Step 8 complete - Saving feedback...')
    const feedbackResult = await saveFeedback(providerId)

    if (!feedbackResult.success) {
      console.error('❌ Failed to save feedback:', feedbackResult.message)
      toastService.error(feedbackResult.message || 'خطا در ذخیره بازخورد')
      return
    }

    console.log('✅ Completing registration with provider ID:', providerId)

    // Step 9: Complete registration
    const result = await completeRegistration(providerId)

    if (result.success) {
      // Move to completion step
      nextStep()
      toastService.success('ثبت‌نام شما با موفقیت تکمیل شد!')
    } else {
      // Show error message
      toastService.error(result.message || 'خطا در تکمیل ثبت‌نام')
    }
  } catch (error: any) {
    console.error('Error in handleFinalSubmit:', error)
    toastService.error(error.message || 'خطا در تکمیل ثبت‌نام')
  }
}

// Initialize on mount
onMounted(async () => {
  initialize()

  // Check provider status before loading draft
  // Only call progress API if status is Drafted or no provider exists
  try {
    const { providerService } = await import('../../services/provider.service')
    const { ProviderStatus } = await import('@/core/types/enums.types')

    const statusData = await providerService.getCurrentProviderStatus()
    debugger
    // If no provider exists or status is Drafted, load the draft
    if (!statusData || statusData.status === ProviderStatus.Drafted) {
      console.log('[RegistrationFlow] Provider status allows draft loading, calling progress API')

      const draftResult = await loadDraft()
      if (draftResult.success && draftResult.providerId) {
        draftProviderId = draftResult.providerId
        console.log('✅ Existing draft loaded with provider ID:', draftProviderId)

        if (state.value.data.step >= 3) {
          toastService.info('ثبت‌نام شما از مرحله قبل ادامه می‌یابد')
        }
      }
    } else {
      // User has completed registration, shouldn't be here
      // Route guard should have prevented this, but handle it gracefully
      console.warn(
        '[RegistrationFlow] Provider status is',
        statusData.status,
        '- registration already complete',
      )
      console.warn('[RegistrationFlow] Skipping progress API call')
    }
  } catch (error: any) {
    console.error('[RegistrationFlow] Error checking provider status:', error)

    // If error is due to no provider existing (404 or similar), proceed with draft loading
    // This is expected for brand new users starting registration
    const isNoProviderError =
      error?.statusCode === 404 ||
      error?.response?.status === 404 ||
      error?.message?.includes('not found') ||
      error?.message?.includes('404')

    if (isNoProviderError) {
      console.log('[RegistrationFlow] No provider found (404) - proceeding with draft loading for new registration')
    } else {
      console.log('[RegistrationFlow] Error occurred, proceeding with draft loading as fallback')
    }

    // On any error, try to load draft anyway (fail open for new users)
    try {
      const draftResult = await loadDraft()
      if (draftResult.success && draftResult.providerId) {
        draftProviderId = draftResult.providerId
        console.log('✅ Existing draft loaded with provider ID:', draftProviderId)

        if (state.value.data.step >= 3) {
          toastService.info('ثبت‌نام شما از مرحله قبل ادامه می‌یابد')
        }
      }
    } catch (draftError) {
      console.error('[RegistrationFlow] Error loading draft:', draftError)
      // Continue anyway - user can start fresh registration
    }
  }

  window.addEventListener('beforeunload', handleBeforeUnload)
})

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload)
})
</script>

<style scoped>
.registration-flow {
  min-height: 100vh;
  background: #f9fafb;
}
</style>
