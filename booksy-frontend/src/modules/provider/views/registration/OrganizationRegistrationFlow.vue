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

    <!-- Validation Error Alert -->
    <div v-if="validationError.show" class="error-container">
      <ValidationAlert
        v-model="validationError.show"
        variant="error"
        :title="validationError.title"
        :message="validationError.message"
        :errors="validationError.errors"
        :dismissible="true"
        :auto-dismiss="true"
        :auto-dismiss-delay="8000"
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
import { useLocations } from '@/shared/composables/useLocations'
import { useProviderRegistration } from '../../composables/useProviderRegistration'
import { hierarchyService } from '../../services/hierarchy.service'
import { providerRegistrationService } from '../../services/provider-registration.service'
import type { RegisterOrganizationRequest } from '../../types/hierarchy.types'
import { parseApiError } from '@/shared/utils/validation/error-parser'

// Components
import RegistrationProgressIndicator from '../../components/registration/RegistrationProgressIndicator.vue'
import OrganizationBusinessInfoStep from '../../components/registration/steps/OrganizationBusinessInfoStep.vue'
import CategorySelectionStep from '../../components/registration/steps/CategorySelectionStep.vue'
import LocationStep from '../../components/registration/steps/LocationStep.vue'
import ServicesStep from '../../components/registration/steps/ServicesStep.vue'
import WorkingHoursStep from '../../components/registration/steps/WorkingHoursStep.vue'
import GalleryStep from '../../components/registration/steps/GalleryStep.vue'
import OrganizationPreviewStep from '../../components/registration/steps/OrganizationPreviewStep.vue'
import CompletionStep from '../../components/registration/steps/CompletionStep.vue'
import ValidationAlert from '@/shared/components/ui/Alert/ValidationAlert.vue'

// ============================================
// State
// ============================================

const router = useRouter()
const authStore = useAuthStore()
const locationStore = useLocations()
const registration = useProviderRegistration()

// Validation error state
const validationError = ref({
  show: false,
  title: 'خطا در ذخیره اطلاعات',
  message: undefined as string | string[] | undefined,
  errors: undefined as Record<string, string[]> | undefined,
})

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

  // Hide any previous validation errors
  validationError.value.show = false

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
        logoUrl: registrationData.value.businessInfo.logoUrl || undefined,
      }

      const response = await hierarchyService.registerOrganization(request)
      draftProviderId = response.data?.providerId
      console.log('✅ Organization draft created:', draftProviderId)
      toastService.success('اطلاعات شما ذخیره شد')
    } else if (currentStep.value === 4) {
      // Step 4: Save services
      if (!draftProviderId) {
        toastService.error('خطا: شناسه سازمان یافت نشد')
        return
      }
      console.log('✅ Step 4 - Saving services to backend...')
      await providerRegistrationService.saveStep4Services(
        draftProviderId,
        registrationData.value.services
      )
      toastService.success('خدمات ذخیره شد')
    } else if (currentStep.value === 5) {
      // Step 5: Save working hours
      if (!draftProviderId) {
        toastService.error('خطا: شناسه سازمان یافت نشد')
        return
      }
      console.log('✅ Step 5 - Saving working hours to backend...')
      await providerRegistrationService.saveStep6WorkingHours(
        draftProviderId,
        registrationData.value.businessHours
      )
      toastService.success('ساعات کاری ذخیره شد')
    } else if (currentStep.value === 6) {
      // Step 6: Save gallery images (optional)
      if (!draftProviderId) {
        toastService.error('خطا: شناسه سازمان یافت نشد')
        return
      }

      // Check if there are any images to upload from the composable
      const galleryImages = registration.registrationData.value.galleryImages
      console.log('🖼️ Gallery images in registration data (from composable):', galleryImages)

      if (galleryImages && galleryImages.length > 0) {
        console.log('✅ Step 6 - Found gallery images, checking for uploads...')

        // Filter images that have File objects (not yet uploaded)
        const filesToUpload = galleryImages
          .filter((img: any) => img.file instanceof File)
          .map((img: any) => img.file as File)

        console.log('📤 Files to upload:', filesToUpload.length)

        if (filesToUpload.length > 0) {
          console.log(`📤 Uploading ${filesToUpload.length} image(s) to backend...`)
          await providerRegistrationService.saveStep7Gallery(filesToUpload)
          console.log('✅ Upload successful!')
          toastService.success(`${filesToUpload.length} تصویر آپلود شد`)
        } else {
          console.log('✅ All images already uploaded (no File objects found)')
          toastService.success('گالری ذخیره شد')
        }
      } else {
        console.log('ℹ️ No gallery images to upload - skipping step 7')
        toastService.success('مرحله گالری رد شد')
      }
    }

    nextStep()
  } catch (error: any) {
    console.error('❌ Error in handleNext:', error)

    // Parse the error and display validation alert
    const parsedError = parseApiError(error)

    validationError.value = {
      show: true,
      title: parsedError.title,
      message: parsedError.message,
      errors: parsedError.errors,
    }

    // Also show a toast for quick feedback
    toastService.error(parsedError.message || parsedError.title)

    // Scroll to top to show the error alert
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
}

async function handleFinalSubmit() {
  try {
    if (!draftProviderId) {
      toastService.error('خطا: شناسه سازمان یافت نشد')
      return
    }

    console.log('✅ Completing organization registration with ID:', draftProviderId)

    // Step 9: Complete the registration
    await providerRegistrationService.saveStep9Complete(draftProviderId)

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
    if (draft) {
      console.log('📋 Found existing draft provider:', draft)

      // Restore provider ID
      draftProviderId = draft.providerId

      // Restore registration step (minimum step 3 since draft was created)
      if (draft.registrationStep && draft.registrationStep >= 1) {
        currentStep.value = draft.registrationStep
        console.log('✅ Restored to step:', draft.registrationStep)
      }

      // Restore business info from nested businessInfo object
      if (draft.businessInfo) {
        if (draft.businessInfo.businessName) {
          registrationData.value.businessInfo.businessName = draft.businessInfo.businessName
        }
        if (draft.businessInfo.businessDescription) {
          registrationData.value.businessInfo.description = draft.businessInfo.businessDescription
        }
        if (draft.businessInfo.email) {
          registrationData.value.businessInfo.email = draft.businessInfo.email
        }
        if (draft.businessInfo.phoneNumber) {
          registrationData.value.businessInfo.phone = draft.businessInfo.phoneNumber
        }

        // Restore owner names directly from businessInfo
        if (draft.businessInfo.ownerFirstName) {
          registrationData.value.businessInfo.ownerFirstName = draft.businessInfo.ownerFirstName
        }
        if (draft.businessInfo.ownerLastName) {
          registrationData.value.businessInfo.ownerLastName = draft.businessInfo.ownerLastName
        }

        // Restore logo URL from businessInfo
        if (draft.businessInfo.logoUrl) {
          registrationData.value.businessInfo.logoUrl = draft.businessInfo.logoUrl
        }

        // Restore category from businessInfo
        if (draft.businessInfo.category) {
          registrationData.value.categoryId = draft.businessInfo.category
        }
      }

      // Restore address and location from nested location object
      if (draft.location) {
        registrationData.value.address.addressLine1 = draft.location.addressLine1 || ''
        registrationData.value.address.addressLine2 = draft.location.addressLine2 || ''
        registrationData.value.address.city = draft.location.city || ''
        registrationData.value.address.state = draft.location.province || ''
        registrationData.value.address.postalCode = draft.location.postalCode || ''

        // Resolve province ID from name
        if (draft.location.province) {
          const province = locationStore.getProvinceByName(draft.location.province)
          if (province) {
            registrationData.value.address.provinceId = province.id
            console.log('✅ Resolved provinceId:', province.id, 'for', draft.location.province)

            // Load cities for this province
            await locationStore.loadCitiesByProvinceId(province.id)

            // Resolve city ID from name
            if (draft.location.city) {
              const cities = locationStore.getCitiesByProvinceId(province.id)
              const city = cities.find(c => c.name === draft.location.city)
              if (city) {
                registrationData.value.address.cityId = city.id
                console.log('✅ Resolved cityId:', city.id, 'for', draft.location.city)
              }
            }
          }
        }

        // Restore location coordinates
        if (draft.location.latitude && draft.location.longitude) {
          registrationData.value.location.latitude = draft.location.latitude
          registrationData.value.location.longitude = draft.location.longitude
        }
      }

      // Restore services
      if (draft.services && draft.services.length > 0) {
        registrationData.value.services = draft.services
        console.log('✅ Restored services:', draft.services.length)
      }

      // Restore business hours
      if (draft.businessHours && draft.businessHours.length > 0) {
        registrationData.value.businessHours = draft.businessHours.map((bh: any) => {
          // Map breaks from API format to component format
          let breaks = []
          if (bh.breaks && bh.breaks.length > 0) {
            breaks = bh.breaks.map((brk: any, index: number) => ({
              id: (index + 1).toString(),
              start: {
                hours: brk.startTimeHours ?? 0,
                minutes: brk.startTimeMinutes ?? 0
              },
              end: {
                hours: brk.endTimeHours ?? 0,
                minutes: brk.endTimeMinutes ?? 0
              }
            }))
            console.log(`✅ Restored ${breaks.length} break(s) for day ${bh.dayOfWeek}`)
          } else if (bh.isOpen) {
            // Add default break 14:00-17:00 for open days that don't have breaks
            breaks = [{
              id: '1',
              start: { hours: 14, minutes: 0 },
              end: { hours: 17, minutes: 0 }
            }]
            console.log(`🔄 Auto-migrated day ${bh.dayOfWeek}: Added default break 14:00-17:00`)
          }

          return {
            dayOfWeek: bh.dayOfWeek,
            isOpen: bh.isOpen,
            openTime: bh.openTimeHours !== null && bh.openTimeMinutes !== null
              ? { hours: bh.openTimeHours, minutes: bh.openTimeMinutes }
              : null,
            closeTime: bh.closeTimeHours !== null && bh.closeTimeMinutes !== null
              ? { hours: bh.closeTimeHours, minutes: bh.closeTimeMinutes }
              : null,
            breaks: breaks
          }
        })
        console.log('✅ Restored business hours:', draft.businessHours.length)
      }

      // Restore gallery images to composable
      if (draft.galleryImages && draft.galleryImages.length > 0) {
        // Map API format to component format
        registration.registrationData.value.galleryImages = draft.galleryImages.map((img: any) => ({
          id: img.id,
          url: img.imageUrl || img.mediumUrl || img.thumbnailUrl,
          thumbnailUrl: img.thumbnailUrl,
          mediumUrl: img.mediumUrl,
          imageUrl: img.imageUrl,
          altText: `تصویر گالری ${img.displayOrder + 1}`,
          displayOrder: img.displayOrder,
          isPrimary: img.isPrimary,
          uploadedAt: img.uploadedAt,
        }))
        console.log('✅ Restored gallery images to composable:', draft.galleryImages.length)
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

.error-container {
  max-width: 48rem;
  margin: 0 auto 2rem;
  padding: 0 1rem;
}
</style>
