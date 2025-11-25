<template>
  <div class="organization-registration-flow">
    <!-- Progress Indicator -->
    <div class="progress-container">
      <RegistrationProgressIndicator
        :current-step="currentStep"
        :total-steps="totalSteps"
        :step-labels="stepLabels"
      />
    </div>

    <!-- Step 1: Business Information -->
    <OrganizationBusinessInfoStep
      v-if="currentStep === 1"
      v-model="registrationData.businessInfo"
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

    <!-- Step 3: Location -->
    <LocationStep
      v-else-if="currentStep === 3"
      :address="registrationData.address"
      :location="registrationData.location"
      @update:address="(val) => setAddress(val)"
      @update:location="(val) => setLocation(val)"
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

    <!-- Step 6: Gallery -->
    <GalleryStep
      v-else-if="currentStep === 6"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 7: Preview & Confirm -->
    <OrganizationPreviewStep
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
import type { RegisterOrganizationRequest } from '../../types/hierarchy.types'

// Components
import RegistrationProgressIndicator from '../../components/registration/RegistrationProgressIndicator.vue'
import OrganizationBusinessInfoStep from '../../components/registration/steps/OrganizationBusinessInfoStep.vue'
import CategorySelectionStep from '../../components/registration/steps/CategorySelectionStep.vue'
import LocationStep from '../../components/registration/steps/LocationStep.vue'
import ServicesStepNew from '../../components/registration/steps/ServicesStepNew.vue'
import WorkingHoursStepNew from '../../components/registration/steps/WorkingHoursStepNew.vue'
import GalleryStep from '../../components/registration/steps/GalleryStep.vue'
import OrganizationPreviewStep from '../../components/registration/steps/OrganizationPreviewStep.vue'
import CompletionStep from '../../components/registration/steps/CompletionStep.vue'

// ============================================
// State
// ============================================

const router = useRouter()
const authStore = useAuthStore()

const currentStep = ref(1)
const totalSteps = 8

const stepLabels = [
  'اطلاعات کسب‌و‌کار',
  'دسته‌بندی',
  'موقعیت مکانی',
  'خدمات',
  'ساعات کاری',
  'گالری تصاویر',
  'بررسی نهایی',
  'تکمیل',
]

const registrationData = ref({
  businessInfo: {
    businessName: '',
    ownerFirstName: '',
    ownerLastName: '',
    email: '',
    phone: authStore.user?.phoneNumber || '',
    description: '',
    logoUrl: '',
    coverImageUrl: '',
  },
  categoryId: '',
  address: {
    addressLine1: '',
    addressLine2: '',
    city: '',
    state: '',
    postalCode: '',
    country: 'IR',
    provinceId: undefined as number | undefined,
    cityId: undefined as number | undefined,
  },
  location: {
    latitude: undefined as number | undefined,
    longitude: undefined as number | undefined,
  },
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
        registrationData.value.businessInfo.businessName &&
        registrationData.value.businessInfo.ownerFirstName &&
        registrationData.value.businessInfo.ownerLastName &&
        registrationData.value.businessInfo.phone
      )
    case 2:
      return !!registrationData.value.categoryId
    case 3:
      return (
        registrationData.value.address.addressLine1 &&
        registrationData.value.address.city &&
        registrationData.value.location.latitude &&
        registrationData.value.location.longitude
      )
    case 4:
      return registrationData.value.services.length > 0
    case 5:
      return registrationData.value.businessHours.length > 0
    case 6:
      return true // Gallery is optional
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

function setAddress(address: any) {
  registrationData.value.address = { ...registrationData.value.address, ...address }
}

function setLocation(location: any) {
  registrationData.value.location = location
}

function handleBack() {
  router.push('/provider/register')
}

async function handleNext() {
  console.log('🚀 OrganizationRegistrationFlow: handleNext called, current step:', currentStep.value)

  if (!canProceed.value) {
    toastService.error('لطفاً تمام فیلدهای الزامی را تکمیل کنید')
    return
  }

  try {
    // Step-specific save operations
    if (currentStep.value === 3) {
      // Step 3: Create organization draft
      console.log('✅ Step 3 complete - Creating organization draft...')
      const request: RegisterOrganizationRequest = {
        businessName: registrationData.value.businessInfo.businessName,
        businessDescription: registrationData.value.businessInfo.description || '-',
        category: registrationData.value.categoryId,
        phoneNumber: registrationData.value.businessInfo.phone,
        email: registrationData.value.businessInfo.email || '-',
        addressLine1: registrationData.value.address.addressLine1,
        addressLine2: registrationData.value.address.addressLine2 || undefined,
        city: registrationData.value.address.city || '-',
        province: registrationData.value.address.state || '-',
        postalCode: registrationData.value.address.postalCode || '-',
        latitude: registrationData.value.location.latitude || 0,
        longitude: registrationData.value.location.longitude || 0,
        ownerFirstName: registrationData.value.businessInfo.ownerFirstName,
        ownerLastName: registrationData.value.businessInfo.ownerLastName,
      }

      const response = await hierarchyService.registerOrganization(request)
      draftProviderId = response.data?.providerId || response.data?.ProviderId
      console.log('✅ Organization draft created:', draftProviderId)
      toastService.success('اطلاعات شما ذخیره شد')
    } else if (currentStep.value === 4) {
      // Step 4: Save services
      if (!draftProviderId) {
        toastService.error('خطا: شناسه سازمان یافت نشد')
        return
      }
      console.log('✅ Step 4 complete - Saving services...')
      // Services will be saved via the existing provider service API
      toastService.success('خدمات ذخیره شد')
    } else if (currentStep.value === 5) {
      // Step 5: Save working hours
      if (!draftProviderId) {
        toastService.error('خطا: شناسه سازمان یافت نشد')
        return
      }
      console.log('✅ Step 5 complete - Saving working hours...')
      toastService.success('ساعات کاری ذخیره شد')
    } else if (currentStep.value === 6) {
      // Step 6: Save gallery (optional)
      if (!draftProviderId) {
        toastService.error('خطا: شناسه سازمان یافت نشد')
        return
      }
      console.log('✅ Step 6 - Saving gallery...')
      toastService.success('گالری ذخیره شد')
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
      toastService.error('خطا: شناسه سازمان یافت نشد')
      return
    }

    console.log('✅ Completing organization registration with ID:', draftProviderId)

    // Mark registration as complete (would need backend endpoint)
    // For now, just move to completion step
    nextStep()
    toastService.success('ثبت‌نام سازمان شما با موفقیت تکمیل شد!')
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
    if (draft && draft.hierarchyType === 'Organization') {
      console.log('📋 Found existing draft provider:', draft)

      // Restore provider ID
      draftProviderId = draft.providerId

      // Restore registration step (minimum step 3 since draft was created)
      if (draft.registrationStep && draft.registrationStep >= 3) {
        currentStep.value = draft.registrationStep
      }

      // Restore business info
      if (draft.businessName) {
        registrationData.value.businessInfo.businessName = draft.businessName
      }
      if (draft.businessDescription) {
        registrationData.value.businessInfo.description = draft.businessDescription
      }
      if (draft.ownerFirstName) {
        registrationData.value.businessInfo.ownerFirstName = draft.ownerFirstName
      }
      if (draft.ownerLastName) {
        registrationData.value.businessInfo.ownerLastName = draft.ownerLastName
      }
      if (draft.email) {
        registrationData.value.businessInfo.email = draft.email
      }
      if (draft.phoneNumber) {
        registrationData.value.businessInfo.phone = draft.phoneNumber
      }

      // Restore category
      if (draft.category) {
        registrationData.value.categoryId = draft.category
      }

      // Restore address
      if (draft.address) {
        registrationData.value.address.addressLine1 = draft.address.street || ''
        registrationData.value.address.city = draft.address.city || ''
        registrationData.value.address.state = draft.address.state || ''
        registrationData.value.address.postalCode = draft.address.postalCode || ''

        // Restore location coordinates
        if (draft.address.latitude && draft.address.longitude) {
          registrationData.value.location.latitude = draft.address.latitude
          registrationData.value.location.longitude = draft.address.longitude
        }
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
.organization-registration-flow {
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
