<template>
  <div class="progress-indicator">
    <div class="progress-header">
      <span class="progress-label">مرحله {{ currentStep }} از {{ totalSteps }}</span>
      <span class="progress-percentage">{{ progressPercentage }}%</span>
    </div>
    <div class="progress-bar">
      <div class="progress-fill" :style="{ width: `${progressPercentage}%` }"></div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  currentStep: number
  totalSteps: number
}

const props = defineProps<Props>()

const progressPercentage = computed(() => Math.round((props.currentStep / props.totalSteps) * 100))
</script>

<style scoped>
.progress-indicator {
  width: 100%;
  margin-bottom: 2rem;
}

.progress-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.5rem;
}

.progress-label,
.progress-percentage {
  font-size: 0.875rem;
  color: #6b7280;
}

.progress-bar {
  width: 100%;
  height: 0.5rem;
  background-color: #e5e7eb;
  border-radius: 9999px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 9999px;
  transition: width 0.3s ease;
}
</style>
