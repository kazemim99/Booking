<template>
  <div class="registration-progress-indicator" :dir="direction">
    <div class="progress-wrapper">
      <div class="progress-steps">
        <div
          v-for="(label, index) in stepLabels"
          :key="index"
          class="progress-step"
          :class="{
            active: index + 1 === currentStep,
            completed: index + 1 < currentStep,
            future: index + 1 > currentStep,
          }"
        >
          <div class="step-indicator">
            <div class="step-circle">
              <i v-if="index + 1 < currentStep" class="icon-check"></i>
              <span v-else>{{ index + 1 }}</span>
            </div>
            <div v-if="index < stepLabels.length - 1" class="step-line"></div>
          </div>
          <div class="step-label">{{ label }}</div>
        </div>
      </div>

      <!-- Progress Bar -->
      <div class="progress-bar-container">
        <div class="progress-bar">
          <div class="progress-bar-fill" :style="{ width: progressPercentage + '%' }"></div>
        </div>
        <div class="progress-text">
          <span>مرحله {{ currentStep }} از {{ totalSteps }}</span>
          <span class="progress-percentage">{{ Math.round(progressPercentage) }}% تکمیل شده</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRTL } from '@/core/composables/useRTL'

// ============================================
// Props
// ============================================

interface Props {
  currentStep: number
  totalSteps: number
  stepLabels: string[]
}

const props = defineProps<Props>()

// ============================================
// Composables
// ============================================

const { direction } = useRTL()

// ============================================
// Computed
// ============================================

const progressPercentage = computed(() => {
  return ((props.currentStep - 1) / (props.totalSteps - 1)) * 100
})
</script>

<style scoped lang="scss">
.registration-progress-indicator {
  padding: 1.5rem 2rem;
  background: #fff;
}

.progress-wrapper {
  max-width: 1200px;
  margin: 0 auto;
}

.progress-steps {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1.5rem;
  position: relative;
}

.progress-step {
  display: flex;
  flex-direction: column;
  align-items: center;
  flex: 1;
  position: relative;

  &:last-child .step-line {
    display: none;
  }
}

.step-indicator {
  display: flex;
  align-items: center;
  width: 100%;
  position: relative;
}

.step-circle {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 1rem;
  transition: all 0.3s ease;
  position: relative;
  z-index: 2;
  background: #fff;

  .progress-step.future & {
    border: 2px solid #d1d5db;
    color: #9ca3af;
  }

  .progress-step.active & {
    border: 3px solid #7c3aed;
    color: #7c3aed;
    box-shadow: 0 0 0 4px rgba(124, 58, 237, 0.1);
    transform: scale(1.1);
  }

  .progress-step.completed & {
    background: linear-gradient(135deg, #10b981 0%, #059669 100%);
    border: none;
    color: #fff;

    i {
      font-size: 1.2rem;
    }
  }
}

.step-line {
  flex: 1;
  height: 2px;
  background: #d1d5db;
  margin: 0 0.5rem;
  position: relative;
  z-index: 1;
  transition: all 0.3s ease;

  .progress-step.completed & {
    background: linear-gradient(90deg, #10b981 0%, #059669 100%);
  }

  .progress-step.active & {
    background: linear-gradient(90deg, #10b981 0%, #d1d5db 100%);
  }
}

.step-label {
  margin-top: 0.75rem;
  font-size: 0.75rem;
  font-weight: 500;
  text-align: center;
  color: #6b7280;
  transition: all 0.3s ease;
  max-width: 100px;

  .progress-step.active & {
    color: #7c3aed;
    font-weight: 600;
  }

  .progress-step.completed & {
    color: #10b981;
  }
}

.progress-bar-container {
  margin-top: 1rem;
}

.progress-bar {
  width: 100%;
  height: 8px;
  background: #e5e7eb;
  border-radius: 999px;
  overflow: hidden;
  position: relative;
}

.progress-bar-fill {
  height: 100%;
  background: linear-gradient(90deg, #10b981 0%, #059669 100%);
  border-radius: 999px;
  transition: width 0.4s ease;
  position: relative;

  &::after {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: linear-gradient(
      90deg,
      rgba(255, 255, 255, 0) 0%,
      rgba(255, 255, 255, 0.3) 50%,
      rgba(255, 255, 255, 0) 100%
    );
    animation: shimmer 2s infinite;
  }
}

@keyframes shimmer {
  0% {
    transform: translateX(-100%);
  }
  100% {
    transform: translateX(100%);
  }
}

.progress-text {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 0.75rem;
  font-size: 0.875rem;
  color: #6b7280;
  font-weight: 500;
}

.progress-percentage {
  color: #10b981;
  font-weight: 600;
}

// Responsive
@media (max-width: 1024px) {
  .step-label {
    font-size: 0.7rem;
    max-width: 80px;
  }

  .step-circle {
    width: 36px;
    height: 36px;
    font-size: 0.9rem;
  }
}

@media (max-width: 768px) {
  .registration-progress-indicator {
    padding: 1rem;
  }

  .progress-steps {
    margin-bottom: 1rem;
  }

  .step-label {
    font-size: 0.65rem;
    max-width: 60px;
  }

  .step-circle {
    width: 32px;
    height: 32px;
    font-size: 0.85rem;
  }

  .step-line {
    margin: 0 0.25rem;
  }
}

@media (max-width: 480px) {
  .step-label {
    display: none;
  }

  .progress-text {
    font-size: 0.8rem;
  }
}
</style>
