<template>
  <div class="registration-flow">
    <!-- Progress Bar -->
    <ProgressBar :current-step="currentStep" :total-steps="8" />

    <!-- Step 1: Business Category -->
    <BusinessCategoryStep
      v-if="currentStep === 1"
      v-model="registrationData.categoryId"
      @next="handleNext"
    />

    <!-- Step 2: Business Information -->
    <BusinessInfoStep
      v-else-if="currentStep === 2"
      v-model="registrationData.businessInfo"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 3: Business Location & Address -->
    <BusinessLocationStep
      v-else-if="currentStep === 3"
      :address="registrationData.address"
      :location="registrationData.location"
      @update:address="(val) => setAddress(val)"
      @update:location="(val) => setLocation(val)"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 4: Business Hours -->
    <BusinessHoursStep
      v-else-if="currentStep === 4"
      v-model="registrationData.businessHours"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 5: Services -->
    <ServicesStep
      v-else-if="currentStep === 5"
      v-model="registrationData.services"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 6: Assistance Options -->
    <AssistanceOptionsStep
      v-else-if="currentStep === 6"
      v-model="registrationData.assistanceOptions"
      @next="handleNext"
      @back="previousStep"
    />

    <!-- Step 7: Team Members -->
    <TeamMembersStep
      v-else-if="currentStep === 7"
      v-model="registrationData.teamMembers"
      :owner-name="ownerFullName"
      :is-submitting="isSubmitting"
      @next="handleFinalSubmit"
      @back="previousStep"
    />

    <!-- Step 8: Registration Complete -->
    <RegistrationCompleteStep v-else-if="currentStep === 8" @complete="handleComplete" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderRegistration } from '../../composables/useProviderRegistration'
import { toastService } from '@/core/services/toast.service'
import { errorHandlerService } from '@/core/services/error-handler.service'

// Shared Components
import ProgressBar from '../../components/registration/shared/ProgressBar.vue'

// Step Components
import BusinessCategoryStep from '../../components/registration/steps/BusinessCategoryStep.vue'
import BusinessInfoStep from '../../components/registration/steps/BusinessInfoStep.vue'
import BusinessLocationStep from '../../components/registration/steps/BusinessLocationStep.vue'
import BusinessHoursStep from '../../components/registration/steps/BusinessHoursStep.vue'
import ServicesStep from '../../components/registration/steps/ServicesStep.vue'
import AssistanceOptionsStep from '../../components/registration/steps/AssistanceOptionsStep.vue'
import TeamMembersStep from '../../components/registration/steps/TeamMembersStep.vue'
import RegistrationCompleteStep from '../../components/registration/steps/RegistrationCompleteStep.vue'

const router = useRouter()
const isSubmitting = ref(false)

const {
  currentStep,
  registrationData,
  nextStep,
  previousStep,
  setAddress,
  setLocation,
  canProceedToNextStep,
  completeRegistration,
  handleBeforeUnload,
  initialize,
} = useProviderRegistration()

const ownerFullName = computed(() => {
  const { ownerFirstName, ownerLastName } = registrationData.value.businessInfo
  return ownerFirstName && ownerLastName ? `${ownerFirstName} ${ownerLastName}` : 'Business Owner'
})

const handleNext = () => {
  if (canProceedToNextStep()) {
    nextStep()
  } else {
    toastService.error('Please complete all required fields')
  }
}

/**
 * Handle final submission (at step 7 - Team Members)
 * Submit all data to backend and show step 8 on success
 */
const handleFinalSubmit = async () => {
  isSubmitting.value = true

  try {
    // Submit registration to backend
    const result = await completeRegistration()

    if (result.success) {
      // Show success toast
      toastService.success(
        result.message || 'Registration submitted successfully! Pending admin approval.',
        'Success'
      )
      // Move to step 8 (completion screen)
      nextStep()
    }
  } catch (error: any) {
    // Handle errors
    const errorResult = errorHandlerService.handleError(error)

    if (errorResult.isServerError) {
      // 5xx errors: Show as toasts
      toastService.error(
        errorResult.generalMessage,
        'Server Error'
      )
    } else if (errorResult.isValidationError) {
      // 4xx validation errors: Show field-specific errors
      toastService.error(
        'Please fix the validation errors and try again',
        'Validation Error'
      )

      // TODO: Display field errors inline on forms
      // This would require passing errors down to step components
      console.error('Validation errors:', errorResult.fieldErrors)
    } else {
      // Generic errors
      toastService.error(errorResult.generalMessage, 'Error')
    }
  } finally {
    isSubmitting.value = false
  }
}

/**
 * Handle navigation from completion screen
 * Since provider registration requires admin approval, redirect to home page
 * Users will be able to access ProviderDashboard once their registration is approved
 */
const handleComplete = () => {
  // Navigate to home page since provider needs admin approval before accessing dashboard
  router.push({ name: 'Home' })
}

// Initialize on mount
onMounted(() => {
  initialize()

  // Add beforeunload listener to warn about unsaved changes
  window.addEventListener('beforeunload', handleBeforeUnload)
})

// Cleanup
onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload)
})
</script>

<style scoped>
.registration-flow {
  width: 100%;
  max-width: 48rem;
  margin: 0 auto;
  padding: 1.5rem;
}

@media (max-width: 768px) {
  .registration-flow {
    padding: 1rem;
  }
}
</style>
