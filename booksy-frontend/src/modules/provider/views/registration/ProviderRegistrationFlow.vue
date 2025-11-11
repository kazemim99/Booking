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
    <GalleryStep
      v-else-if="currentStep === 7"
      @next="handleNext"
      @back="previousStep"
    />

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
import { computed, onMounted, onBeforeUnmount, watch } from 'vue'
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
  console.log('handleNext called, current step:', currentStep.value)
  console.log('Registration data:', registrationData.value)

  const canProceed = canProceedToNextStep()
  console.log('Can proceed to next step:', canProceed)

  if (!canProceed) {
    console.error('Cannot proceed - validation failed')
    toastService.error('لطفاً تمام فیلدهای الزامی را تکمیل کنید')
    return
  }

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
      if (!draftProviderId) {
        toastService.error('خطا: شناسه ارائه‌دهنده یافت نشد')
        return
      }
      console.log('✅ Step 4 complete - Saving services...')
      const result = await saveServices(draftProviderId)

      if (!result.success) {
        console.error('❌ Failed to save services:', result.message)
        toastService.error(result.message || 'خطا در ذخیره خدمات')
        return
      }
      toastService.success('خدمات ذخیره شد')
    } else if (currentStep.value === 5) {
      // Step 5: Save staff (optional)
      if (!draftProviderId) {
        toastService.error('خطا: شناسه ارائه‌دهنده یافت نشد')
        return
      }
      console.log('✅ Step 5 complete - Saving staff...')
      const result = await saveStaff(draftProviderId)

      if (!result.success) {
        console.error('❌ Failed to save staff:', result.message)
        toastService.error(result.message || 'خطا در ذخیره کارکنان')
        return
      }
      toastService.success('کارکنان ذخیره شد')
    } else if (currentStep.value === 6) {
      // Step 6: Save working hours
      if (!draftProviderId) {
        toastService.error('خطا: شناسه ارائه‌دهنده یافت نشد')
        return
      }
      console.log('✅ Step 6 complete - Saving working hours...')
      const result = await saveWorkingHours(draftProviderId)

      if (!result.success) {
        console.error('❌ Failed to save working hours:', result.message)
        toastService.error(result.message || 'خطا در ذخیره ساعات کاری')
        return
      }
      toastService.success('ساعات کاری ذخیره شد')
    } else if (currentStep.value === 7) {
      // Step 7: Save gallery (optional)
      if (!draftProviderId) {
        toastService.error('خطا: شناسه ارائه‌دهنده یافت نشد')
        return
      }
      console.log('✅ Step 7 complete - Saving gallery...')
      const result = await saveGallery(draftProviderId)

      if (!result.success) {
        console.error('❌ Failed to save gallery:', result.message)
        toastService.error(result.message || 'خطا در ذخیره گالری')
        return
      }
      toastService.success('گالری ذخیره شد')
    }

    console.log('Proceeding to next step')
    nextStep()
  } catch (error: any) {
    console.error('Error in handleNext:', error)
    toastService.error(error.message || 'خطا در ذخیره اطلاعات')
  }
}

const handleFinalSubmit = async (feedback?: string) => {
  try {
    if (!draftProviderId) {
      toastService.error('خطا: شناسه ارائه‌دهنده یافت نشد')
      return
    }

    // Step 8: Save feedback (optional)
    console.log('✅ Step 8 complete - Saving feedback...')
    const feedbackResult = await saveFeedback(draftProviderId)

    if (!feedbackResult.success) {
      console.error('❌ Failed to save feedback:', feedbackResult.message)
      toastService.error(feedbackResult.message || 'خطا در ذخیره بازخورد')
      return
    }

    console.log('✅ Completing registration with provider ID:', draftProviderId)

    // Step 9: Complete registration
    const result = await completeRegistration(draftProviderId)

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

// Auto-save staff when leaving step 5
watch(currentStep, async (newStep, oldStep) => {
  // If we're leaving step 5 (staff), auto-save the staff data
  if (oldStep === 5 && newStep !== 5 && draftProviderId) {
    console.log('🔄 Auto-saving staff before leaving step 5...')
    try {
      const result = await saveStaff(draftProviderId)
      if (result.success) {
        console.log('✅ Staff auto-saved successfully')
      } else {
        console.warn('⚠️ Failed to auto-save staff:', result.message)
      }
    } catch (error) {
      console.error('❌ Error auto-saving staff:', error)
    }
  }
})

// Initialize on mount
onMounted(async () => {
  initialize()

  // Try to load existing draft
  const draftResult = await loadDraft()
  if (draftResult.success && draftResult.providerId) {
    draftProviderId = draftResult.providerId
    console.log('✅ Existing draft loaded with provider ID:', draftProviderId)

    if (state.value.data.step >= 3) {
      toastService.info('ثبت‌نام شما از مرحله قبل ادامه می‌یابد')
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
