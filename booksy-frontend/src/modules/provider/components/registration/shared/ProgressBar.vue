<template>
  <div class="progress-bar-container">
    <div class="progress-bar">
      <div class="progress-fill" :style="{ width: `${percentage}%` }">
        <div class="progress-indicator"></div>
      </div>
    </div>
    <div v-if="showLabel" class="progress-label">
      {{ $t('provider.registration.progress.step', { current: currentStep, total: totalSteps }) }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  currentStep: number
  totalSteps: number
  showLabel?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  showLabel: false,
})

const percentage = computed(() => {
  return Math.min(Math.max((props.currentStep / props.totalSteps) * 100, 0), 100)
})
</script>

<style scoped>
.progress-bar-container {
  width: 100%;
  padding: 1rem 0;
}

.progress-bar {
  position: relative;
  width: 100%;
  height: 0.5rem;
  background-color: #e5e7eb;
  border-radius: 9999px;
  overflow: hidden;
}

.progress-fill {
  position: absolute;
  left: 0;
  top: 0;
  height: 100%;
  background: linear-gradient(90deg, #10b981 0%, #34d399 100%);
  border-radius: 9999px;
  transition: width 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  display: flex;
  align-items: center;
  justify-content: flex-end;
}

.progress-indicator {
  width: 1rem;
  height: 1rem;
  background-color: #10b981;
  border: 3px solid #ffffff;
  border-radius: 50%;
  box-shadow: 0 2px 8px rgba(16, 185, 129, 0.4);
  transform: translateX(50%);
  animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}

@keyframes pulse {
  0%,
  100% {
    opacity: 1;
  }
  50% {
    opacity: 0.8;
  }
}

.progress-label {
  margin-top: 0.5rem;
  text-align: center;
  font-size: 0.875rem;
  font-weight: 500;
  color: #6b7280;
}

/* Responsive */
@media (max-width: 640px) {
  .progress-bar-container {
    padding: 0.75rem 0;
  }

  .progress-bar {
    height: 0.375rem;
  }

  .progress-indicator {
    width: 0.75rem;
    height: 0.75rem;
    border-width: 2px;
  }

  .progress-label {
    font-size: 0.75rem;
  }
}
</style>
