<template>
  <div v-if="error" class="validation-error">
    <span class="error-icon">⚠️</span>
    <span class="error-text">{{ error }}</span>
  </div>
</template>

<script setup lang="ts">
interface Props {
  error: string | undefined
}

defineProps<Props>()
</script>

<style scoped lang="scss">
.validation-error {
  display: flex;
  align-items: flex-start;
  gap: 0.375rem;
  margin-top: 0.25rem;
  font-size: 0.75rem;
  color: #ef4444;
  line-height: 1.4;
}

.error-icon {
  font-size: 0.875rem;
  flex-shrink: 0;
  margin-top: 0.0625rem;
}

.error-text {
  flex: 1;
}
</style>
