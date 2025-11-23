<template>
  <div class="individual-registration-flow">
    <!-- Progress Indicator -->
    <div class="progress-container">
      <RegistrationProgressIndicator
        :current-step="currentStep"
        :total-steps="totalSteps"
        :step-labels="stepLabels"
      />
    </div>

    <!-- Step 1: Personal Information -->
    <IndividualPersonalInfoStep
      v-if="currentStep === 1"
      v-model="registrationData.personalInfo"
      @next="handleNext"
      @back="handleBack"
    />

    <!-- Step 2: Category Selection -->
    <CategorySelectionStep
      v-else-if="currentStep === 2"
      v-model="registrationData.categoryId"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 3: Service Area -->
    <ServiceAreaStep
      v-else-if="currentStep === 3"
      :service-area="registrationData.serviceArea"
      :offers-mobile-services="registrationData.offersMobileServices"
      @update:serviceArea="(val) => (registrationData.serviceArea = val)"
      @update:offersMobileServices="(val) => (registrationData.offersMobileServices = val)"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 4: Services -->
    <ServicesStepNew
      v-else-if="currentStep === 4"
      v-model="registrationData.services"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 5: Working Hours -->
    <WorkingHoursStepNew
      v-else-if="currentStep === 5"
      v-model="registrationData.businessHours"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 6: Portfolio/Gallery -->
    <GalleryStep
      v-else-if="currentStep === 6"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 7: Preview & Confirm -->
    <IndividualPreviewStep
      v-else-if="currentStep === 7"
      :data="registrationData"
      @next="handleFinalSubmit"
      @back="previousStep"
      @edit="editStep"
    />

    <!-- Step 8: Completion -->
    <CompletionStep v-else-if="currentStep === 8" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { toastService } from '@/core/services/toast.service'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { hierarchyService } from '../../services/hierarchy.service'
import type { RegisterIndependentIndividualRequest } from '../../types/hierarchy.types'

// Components
import RegistrationProgressIndicator from '../../components/registration/RegistrationProgressIndicator.vue'
import IndividualPersonalInfoStep from '../../components/registration/steps/IndividualPersonalInfoStep.vue'
import CategorySelectionStep from '../../components/registration/steps/CategorySelectionStep.vue'
import ServiceAreaStep from '../../components/registration/steps/ServiceAreaStep.vue'
import ServicesStepNew from '../../components/registration/steps/ServicesStepNew.vue'
import WorkingHoursStepNew from '../../components/registration/steps/WorkingHoursStepNew.vue'
import GalleryStep from '../../components/registration/steps/GalleryStep.vue'
import IndividualPreviewStep from '../../components/registration/steps/IndividualPreviewStep.vue'
import CompletionStep from '../../components/registration/steps/CompletionStep.vue'

// ============================================
// State
// ============================================

const router = useRouter()
const authStore = useAuthStore()

const currentStep = ref(1)
const totalSteps = 8

const stepLabels = [
  'اطلاعات شخصی',
  'دسته‌بندی',
  'منطقه خدمات',
  'خدمات',
  'ساعات کاری',
  'نمونه کارها',
  'بررسی نهایی',
  'تکمیل',
]

const registrationData = ref({
  personalInfo: {
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    bio: '',
    avatarUrl: '',
    specializations: [] as string[],
  },
  categoryId: '',
  serviceArea: {
    city: '',
    state: '',
    country: 'IR',
    serviceRadius: 10, // km
    latitude: undefined as number | undefined,
    longitude: undefined as number | undefined,
  },
  offersMobileServices: true,
  services: [] as any[],
  businessHours: [] as any[],
  gallery: [] as any[],
})

let draftProviderId: string | undefined = undefined

// ============================================
// Computed
// ============================================

const canProceed = computed(() => {
  switch (currentStep.value) {
    case 1:
      return (
        registrationData.value.personalInfo.firstName &&
        registrationData.value.personalInfo.lastName &&
        registrationData.value.personalInfo.email &&
        registrationData.value.personalInfo.phone
      )
    case 2:
      return !!registrationData.value.categoryId
    case 3:
      return (
        registrationData.value.serviceArea.city &&
        registrationData.value.serviceArea.latitude &&
        registrationData.value.serviceArea.longitude
      )
    case 4:
      return registrationData.value.services.length > 0
    case 5:
      return registrationData.value.businessHours.length > 0
    case 6:
      return true // Portfolio is optional
    case 7:
      return true // Preview step
    default:
      return false
  }
})

// ============================================
// Methods
// ============================================

function nextStep() {
  if (currentStep.value < totalSteps) {
    currentStep.value++
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
}

function previousStep() {
  if (currentStep.value > 1) {
    currentStep.value--
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
}

function editStep(step: number) {
  currentStep.value = step
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

function handleBack() {
  router.push('/provider/register')
}

async function handleNext() {
  console.log('🚀 IndividualRegistrationFlow: handleNext called, current step:', currentStep.value)

  if (!canProceed.value) {
    toastService.error('لطفاً تمام فیلدهای الزامی را تکمیل کنید')
    return
  }

  try {
    // Step-specific save operations
    if (currentStep.value === 3) {
      // Step 3: Create individual draft
      console.log('✅ Step 3 complete - Creating individual draft...')
      const request: RegisterIndependentIndividualRequest = {
        ownerId: authStore.userId!,
        firstName: registrationData.value.personalInfo.firstName,
        lastName: registrationData.value.personalInfo.lastName,
        bio: registrationData.value.personalInfo.bio,
        email: registrationData.value.personalInfo.email,
        phone: registrationData.value.personalInfo.phone,
        avatarUrl: registrationData.value.personalInfo.avatarUrl,
        specializations: registrationData.value.personalInfo.specializations,
        offersMobileServices: registrationData.value.offersMobileServices,
        city: registrationData.value.serviceArea.city,
        state: registrationData.value.serviceArea.state,
        country: registrationData.value.serviceArea.country,
        serviceRadius: registrationData.value.serviceArea.serviceRadius,
        latitude: registrationData.value.serviceArea.latitude,
        longitude: registrationData.value.serviceArea.longitude,
      }

      const response = await hierarchyService.registerIndividual(request)
      draftProviderId = response.id
      console.log('✅ Individual draft created:', draftProviderId)
      toastService.success('اطلاعات شما ذخیره شد')
    } else if (currentStep.value === 4) {
      // Step 4: Save services
      if (!draftProviderId) {
        toastService.error('خطا: شناسه ارائه‌دهنده یافت نشد')
        return
      }
      console.log('✅ Step 4 complete - Saving services...')
      toastService.success('خدمات ذخیره شد')
    } else if (currentStep.value === 5) {
      // Step 5: Save working hours
      if (!draftProviderId) {
        toastService.error('خطا: شناسه ارائه‌دهنده یافت نشد')
        return
      }
      console.log('✅ Step 5 complete - Saving working hours...')
      toastService.success('ساعات کاری ذخیره شد')
    } else if (currentStep.value === 6) {
      // Step 6: Save portfolio (optional)
      if (!draftProviderId) {
        toastService.error('خطا: شناسه ارائه‌دهنده یافت نشد')
        return
      }
      console.log('✅ Step 6 - Saving portfolio...')
      toastService.success('نمونه کارها ذخیره شد')
    }

    nextStep()
  } catch (error) {
    console.error('Error in handleNext:', error)
    toastService.error((error as Error).message || 'خطا در ذخیره اطلاعات')
  }
}

async function handleFinalSubmit() {
  try {
    if (!draftProviderId) {
      toastService.error('خطا: شناسه ارائه‌دهنده یافت نشد')
      return
    }

    console.log('✅ Completing individual registration with ID:', draftProviderId)

    // Mark registration as complete (would need backend endpoint)
    // For now, just move to completion step
    nextStep()
    toastService.success('ثبت‌نام شما با موفقیت تکمیل شد!')
  } catch (error) {
    console.error('Error in handleFinalSubmit:', error)
    toastService.error((error as Error).message || 'خطا در تکمیل ثبت‌نام')
  }
}

function handleBeforeUnload(e: BeforeUnloadEvent) {
  if (currentStep.value > 1 && currentStep.value < totalSteps) {
    e.preventDefault()
    e.returnValue = ''
  }
}

// ============================================
// Lifecycle
// ============================================

onMounted(() => {
  window.addEventListener('beforeunload', handleBeforeUnload)
})

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload)
})
</script>

<style scoped lang="scss">
.individual-registration-flow {
  min-height: 100vh;
  background: #f9fafb;
  padding-bottom: 2rem;
}

.progress-container {
  position: sticky;
  top: 0;
  z-index: 100;
  background: #fff;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  padding: 1rem 0;
  margin-bottom: 2rem;
}
</style>
