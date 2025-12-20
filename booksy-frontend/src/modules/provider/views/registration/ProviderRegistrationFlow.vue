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
    <ServicesStep
      v-else-if="currentStep === 4"
      v-model="registrationData.services"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 5: Staff/Team Members (NEW Figma Design) -->
<!-- <StaffStep currentStep={currentStep} onUpdate={updateFormData} /> -->

    <!-- Step 6: Working Hours (NEW Figma Design) -->
    <WorkingHoursStep
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
import { useProviderRegistration } from '../../composables/useProviderRegistration'
import { toastService } from '@/core/services/toast.service'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { ProviderStatus } from '../../types/provider.types'

// New Step Components (Figma Design)
import BusinessInfoStep from '../../components/registration/steps/BusinessInfoStep.vue'
import CategorySelectionStep from '../../components/registration/steps/CategorySelectionStep.vue'
import LocationStep from '../../components/registration/steps/LocationStep.vue'
import ServicesStep from '../../components/registration/steps/ServicesStep.vue'
// import StaffStep from '../../components/registration/steps/StaffStep.vue' // Staff/Team Members Step - Component not found
import WorkingHoursStep from '../../components/registration/steps/WorkingHoursStep.vue'
import GalleryStep from '../../components/registration/steps/GalleryStep.vue'
import OptionalFeedbackStep from '../../components/registration/steps/OptionalFeedbackStep.vue'
import CompletionStep from '../../components/registration/steps/CompletionStep.vue'

const authStore = useAuthStore()

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
  } catch (error) {
    console.error('Error in handleNext:', error)
    toastService.error((error as Error).message || 'خطا در ذخیره اطلاعات')
  }
}

const handleFinalSubmit = async () => {
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
  } catch (error) {
    console.error('Error in handleFinalSubmit:', error)
    toastService.error((error as Error).message || 'خطا در تکمیل ثبت‌نام')
  }
}

// Initialize on mount
onMounted(async () => {
  initialize()

  // Check provider status from token (no API call needed)
  const tokenProviderStatus = authStore.providerStatus
  const tokenProviderId = authStore.providerId

  console.log('[RegistrationFlow] Provider status from token:', tokenProviderStatus)
  console.log('[RegistrationFlow] Provider ID from token:', tokenProviderId)

  // Only load draft if status is null (new user) or Drafted (incomplete registration)
  if (tokenProviderStatus === null || tokenProviderStatus === ProviderStatus.Drafted) {
    console.log('[RegistrationFlow] Provider status allows draft loading')

    try {
      const draftResult = await loadDraft()
      if (draftResult.success && draftResult.providerId) {
        draftProviderId = draftResult.providerId
        console.log('✅ Existing draft loaded with provider ID:', draftProviderId)

        if (state.value.data.step >= 3) {
          toastService.info('ثبت‌نام شما از مرحله قبل ادامه می‌یابد')
        }
      } else {
        // No existing draft - new user starting fresh
        console.log('[RegistrationFlow] No existing draft found, starting fresh registration')
      }
    } catch (error) {
      console.error('[RegistrationFlow] Error loading draft:', error)
      // Continue anyway - user can start fresh registration
    }
  } else {
    // User has already completed registration (Active, PendingVerification, etc.)
    console.warn('[RegistrationFlow] Provider status is', tokenProviderStatus, '- registration already complete')
    console.warn('[RegistrationFlow] Route guard should have prevented access')
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
