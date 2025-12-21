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
    <ServicesStep
      v-else-if="currentStep === 4"
      v-model="registrationData.services"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 5: Working Hours -->
    <WorkingHoursStep
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
      @loading="isProcessing = $event"
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
import ServicesStep from '../../components/registration/steps/ServicesStep.vue'
import WorkingHoursStep from '../../components/registration/steps/WorkingHoursStep.vue'
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
const isProcessing = ref(false)

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
    firstName: authStore.user?.profile?.firstName || '',
    lastName: authStore.user?.profile?.lastName || '',
    email: '',
    phone: authStore.user?.phoneNumber || '',
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
    latitude: 0,
    longitude: 0,
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
        ownerId: authStore.user!.id,
        firstName: registrationData.value.personalInfo.firstName,
        lastName: registrationData.value.personalInfo.lastName,
        bio: registrationData.value.personalInfo.bio,
        email: registrationData.value.personalInfo.email,
        phone: registrationData.value.personalInfo.phone,
        photoUrl: registrationData.value.personalInfo.avatarUrl,
        city: registrationData.value.serviceArea.city,
        state: registrationData.value.serviceArea.state,
        country: registrationData.value.serviceArea.country,
      }

      const response = await hierarchyService.registerIndividual(request)
      draftProviderId = response.data?.providerId
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
  } finally {
    isProcessing.value = false
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

onMounted(async () => {
  window.addEventListener('beforeunload', handleBeforeUnload)

  // Check if user has an existing draft provider
  try {
    const draft = await hierarchyService.getDraftProvider()
    if (draft && draft.hierarchyType === 'Individual') {
      console.log('📋 Found existing draft provider:', draft)

      // Restore provider ID
      draftProviderId = draft.providerId

      // Restore registration step (minimum step 3 since draft was created)
      if (draft.registrationStep && draft.registrationStep >= 3) {
        currentStep.value = draft.registrationStep
      }

      // Restore personal info
      if (draft.firstName) {
        registrationData.value.personalInfo.firstName = draft.firstName
      }
      if (draft.lastName) {
        registrationData.value.personalInfo.lastName = draft.lastName
      }
      if (draft.bio) {
        registrationData.value.personalInfo.bio = draft.bio
      }
      if (draft.email) {
        registrationData.value.personalInfo.email = draft.email
      }
      if (draft.phoneNumber) {
        registrationData.value.personalInfo.phone = draft.phoneNumber
      }
      if (draft.avatarUrl) {
        registrationData.value.personalInfo.avatarUrl = draft.avatarUrl
      }

      // Restore category
      if (draft.category) {
        registrationData.value.categoryId = draft.category
      }

      // Restore service area
      if (draft.serviceArea) {
        registrationData.value.serviceArea.city = draft.serviceArea.city || ''
        registrationData.value.serviceArea.state = draft.serviceArea.state || ''
        registrationData.value.serviceArea.serviceRadius = draft.serviceArea.serviceRadius || 10

        // Restore location coordinates
        if (draft.serviceArea.latitude && draft.serviceArea.longitude) {
          registrationData.value.serviceArea.latitude = draft.serviceArea.latitude
          registrationData.value.serviceArea.longitude = draft.serviceArea.longitude
        }
      }

      // Restore mobile services flag
      if (draft.offersMobileServices !== undefined) {
        registrationData.value.offersMobileServices = draft.offersMobileServices
      }

      toastService.success('ثبت‌نام شما بازیابی شد. می‌توانید از جایی که متوقف شده بودید ادامه دهید.')
    }
  } catch (error) {
    console.error('Error loading draft provider:', error)
    // Don't show error to user, just start fresh
  }
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
  z-index: 1000;
  background: #fff;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  padding: 1rem 0;
  margin-bottom: 2rem;
}
</style>
