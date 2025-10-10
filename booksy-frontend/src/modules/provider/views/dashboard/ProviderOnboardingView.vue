<template>
  <div class="provider-onboarding">
    <div class="onboarding-header">
      <h1>Complete Your Provider Profile</h1>
      <p>Fill in the missing information to start receiving bookings</p>
    </div>

    <OnboardingProgress :steps="onboardingSteps" :current-step="currentStep" />

    <div class="onboarding-content">
      <!-- Step cards -->
      <OnboardingStepCard
        v-for="(step, index) in onboardingSteps"
        :key="step.id"
        :step="step"
        :is-completed="step.completed"
        :is-current="index === currentStep"
        @start="startStep(index)"
      />
    </div>

    <div class="onboarding-footer">
      <AppButton variant="secondary" @click="skipForNow"> Skip for Now </AppButton>
      <AppButton variant="primary" :disabled="cannotFinish" @click="finishOnboarding">
        Finish Setup
      </AppButton>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import OnboardingProgress from '../../components/onboarding/OnboardingProgress.vue'
import OnboardingStepCard from '../../components/onboarding/OnboardingStepCard.vue'
import { Button as AppButton } from '@/shared/components'
import { useProviderStore } from '@/modules/provider/stores/provider.store'

const router = useRouter()

const currentStep = ref(0)
const providerStore = useProviderStore()

const onboardingSteps = computed(() => {
  const p = providerStore.currentProvider
  const businessInfoComplete = !!(
    p &&
    p.profile.businessName &&
    p.profile.description &&
    p.contactInfo.email &&
    p.contactInfo.primaryPhone &&
    p.address.addressLine1
  )

  const businessHoursComplete = !!(p && p.businessHours && p.businessHours.length > 0)
  const servicesComplete = !!(p && p.services && p.services.length > 0)
  const galleryComplete = !!(p && (p.profile.logoUrl || p.profile.coverImageUrl))

  return [
    {
      id: 'business-info',
      title: 'Business Information',
      description: 'Complete your business details and contact information',
      icon: 'building',
      completed: businessInfoComplete,
      route: 'ProviderBusinessInfo',
    },
    {
      id: 'business-hours',
      title: 'Business Hours',
      description: 'Set your operating hours and availability',
      icon: 'clock',
      completed: businessHoursComplete,
      route: 'ProviderBusinessHours',
    },
    {
      id: 'services',
      title: 'Add Services',
      description: 'Create your service catalog with pricing',
      icon: 'services',
      completed: servicesComplete,
      route: 'ProviderServices',
    },
    {
      id: 'gallery',
      title: 'Photo Gallery',
      description: 'Upload photos of your work and business',
      icon: 'image',
      completed: galleryComplete,
      route: 'ProviderGallery',
    },
  ]
})

const canFinish = computed(() => {
  const completed = new Set(onboardingSteps.value.filter((s) => s.completed).map((s) => s.id))
  return completed.has('business-info') && completed.has('business-hours')
})

const cannotFinish = computed(() => !canFinish.value)

const startStep = (index: number) => {
  const step = onboardingSteps.value[index]
  router.push({ name: step.route })
}

const skipForNow = () => {
  router.push({ name: 'ProviderDashboard' })
}

const finishOnboarding = () => {
  // After finishing, go to Provider Profile to allow editing
  router.push({ name: 'ProviderProfile' })
}

onMounted(async () => {
  try {
    if (!providerStore.currentProvider) {
      await providerStore.loadCurrentProvider()
    }

    // If still no provider after loading, redirect to registration
    if (!providerStore.currentProvider) {
      console.warn('No provider found - redirecting to registration')
      router.push({
        name: 'ProviderRegistration'
      })
    }
  } catch (error) {
    console.error('Error loading provider for onboarding:', error)
    // On error, also redirect to registration
    router.push({
      name: 'ProviderRegistration'
    })
  }
})
</script>

<style scoped>
.provider-onboarding {
  max-width: 900px;
  margin: 0 auto;
}

.onboarding-header {
  text-align: center;
  margin-bottom: 3rem;
}

.onboarding-header h1 {
  font-size: 2rem;
  font-weight: 700;
  margin: 0 0 0.5rem 0;
}

.onboarding-header p {
  font-size: 1.1rem;
  color: var(--color-text-secondary);
  margin: 0;
}

.onboarding-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin: 2rem 0;
}

.onboarding-footer {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  padding-top: 2rem;
  border-top: 1px solid var(--color-border);
}
</style>
