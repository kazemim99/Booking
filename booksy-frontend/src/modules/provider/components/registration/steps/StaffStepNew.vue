<template>
  <div class="registration-step">
    <ProgressIndicator :current-step="5" :total-steps="9" />

    <div class="step-card">
      <div class="step-header">
        <h2 class="step-title">پرسنل</h2>
        <p class="step-description">اطلاعات پرسنل و همکاران خود را اضافه کنید (اختیاری)</p>
      </div>

      <div class="step-content">
        <!-- Use ProfileStaffSection component -->
        <ProfileStaffSection v-model="staffMembers" :use-backend="false" />

        <!-- Navigation -->
        <div class="step-actions">
          <AppButton type="button" variant="outline" size="large" @click="$emit('back')">
            قبلی
          </AppButton>
          <AppButton type="button" variant="primary" size="large" @click="handleNext">
            بعدی
          </AppButton>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import ProgressIndicator from '../shared/ProgressIndicator.vue'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import ProfileStaffSection from '../../ProfileStaffSection.vue'
import type { TeamMember } from '@/modules/provider/types/registration.types'

interface Props {
  modelValue?: TeamMember[]
  ownerName?: string
}

interface Emits {
  (e: 'update:modelValue', value: TeamMember[]): void
  (e: 'next'): void
  (e: 'back'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

// State
const staffMembers = ref<TeamMember[]>(props.modelValue || [])

const handleNext = () => {
  // Staff is optional, so we can proceed without validation
  emit('update:modelValue', staffMembers.value)
  emit('next')
}
</script>

<style scoped>
.registration-step {
  min-height: 100vh;
  padding: 2rem 1rem;
  background: #f9fafb;
  direction: rtl;
}

.step-card {
  max-width: 42rem;
  margin: 0 auto;
  background: white;
  border-radius: 1rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  padding: 2rem;
}

.step-header {
  margin-bottom: 2rem;
}

.step-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin-bottom: 0.5rem;
}

.step-description {
  font-size: 0.875rem;
  color: #6b7280;
}

.step-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Navigation */
.step-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 1rem;
  padding-top: 1.5rem;
  border-top: 1px solid #e5e7eb;
}

.step-actions > * {
  flex: 1;
}

@media (max-width: 640px) {
  .step-card {
    padding: 1.5rem;
  }

  .step-title {
    font-size: 1.25rem;
  }
}
</style>
