<template>
  <div v-if="hasErrors" class="validation-summary">
    <div class="summary-header">
      <span class="summary-icon">⚠️</span>
      <h3 class="summary-title">{{ title }}</h3>
    </div>
    <ul class="error-list">
      <li v-for="(error, index) in errorList" :key="index" class="error-item">
        {{ error }}
      </li>
    </ul>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  errors?: string[] | Record<string, string[]>
  title?: string
}

const props = withDefaults(defineProps<Props>(), {
  title: 'Please correct the following errors:',
})

const hasErrors = computed(() => {
  if (!props.errors) return false
  if (Array.isArray(props.errors)) {
    return props.errors.length > 0
  }
  return Object.keys(props.errors).length > 0
})

const errorList = computed(() => {
  if (!props.errors) return []

  if (Array.isArray(props.errors)) {
    return props.errors
  }

  // Flatten object errors
  return Object.values(props.errors).flat()
})
</script>

<style scoped lang="scss">
.validation-summary {
  padding: 1rem;
  background-color: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 8px;
  margin-bottom: 1.5rem;
}

.summary-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
}

.summary-icon {
  font-size: 1.25rem;
}

.summary-title {
  font-size: 0.875rem;
  font-weight: 600;
  color: #991b1b;
  margin: 0;
}

.error-list {
  list-style: none;
  padding: 0;
  margin: 0;
}

.error-item {
  font-size: 0.875rem;
  color: #991b1b;
  padding: 0.25rem 0;
  padding-left: 1.75rem;
  position: relative;

  &::before {
    content: '•';
    position: absolute;
    left: 0.75rem;
  }
}
</style>
