<template>
  <div class="step-container">
    <div class="step-header">
      <h2 class="step-title">{{ title }}</h2>
      <p v-if="subtitle" class="step-subtitle">{{ subtitle }}</p>
    </div>
    <div class="step-content">
      <slot />
    </div>
  </div>
</template>

<script setup lang="ts">
interface Props {
  title: string
  subtitle?: string
}

defineProps<Props>()
</script>

<style scoped>
.step-container {
  width: 100%;
  animation: fadeInSlide 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}

@keyframes fadeInSlide {
  from {
    opacity: 0;
    transform: translateY(1rem);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.step-header {
  text-align: center;
  margin-bottom: 2rem;
}

.step-title {
  font-size: 1.875rem;
  font-weight: 700;
  color: #111827;
  margin-bottom: 0.5rem;
  line-height: 1.2;
}

.step-subtitle {
  font-size: 0.875rem;
  color: #6b7280;
  line-height: 1.5;
  max-width: 32rem;
  margin: 0 auto;
}

.step-content {
  width: 100%;
}

/* Responsive */
@media (max-width: 640px) {
  .step-header {
    margin-bottom: 1.5rem;
  }

  .step-title {
    font-size: 1.5rem;
  }

  .step-subtitle {
    font-size: 0.8125rem;
  }
}
</style>
