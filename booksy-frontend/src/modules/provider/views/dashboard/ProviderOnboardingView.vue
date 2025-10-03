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
      <AppButton variant="primary" :disabled="!canFinish" @click="finishOnboarding">
        Finish Setup
      </AppButton>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import OnboardingProgress from '../../components/onboarding/OnboardingProgress.vue'

const router = useRouter()

const currentStep = ref(0)

const onboardingSteps = ref([
  {
    id: 'business-info',
    title: 'Business Information',
    description: 'Complete your business details and contact information',
    icon: 'building',
    completed: false,
    route: 'ProviderBusinessInfo',
  },
  {
    id: 'business-hours',
    title: 'Business Hours',
    description: 'Set your operating hours and availability',
    icon: 'clock',
    completed: false,
    route: 'ProviderBusinessHours',
  },
  {
    id: 'services',
    title: 'Add Services',
    description: 'Create your service catalog with pricing',
    icon: 'services',
    completed: false,
    route: 'ProviderServices',
  },
  {
    id: 'gallery',
    title: 'Photo Gallery',
    description: 'Upload photos of your work and business',
    icon: 'image',
    completed: false,
    route: 'ProviderGallery',
  },
])

const canFinish = computed(() => {
  return onboardingSteps.value.filter((s) => s.completed).length >= 2
})

const startStep = (index: number) => {
  const step = onboardingSteps.value[index]
  router.push({ name: step.route })
}

const skipForNow = () => {
  router.push({ name: 'ProviderDashboard' })
}

const finishOnboarding = () => {
  // Mark onboarding as complete
  localStorage.setItem('providerOnboardingComplete', 'true')
  router.push({ name: 'ProviderDashboard' })
}
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
