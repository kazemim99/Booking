<template>
  <label class="checkbox-wrapper" :class="{ disabled }">
    <input
      type="checkbox"
      :checked="modelValue"
      :disabled="disabled"
      @change="handleChange"
      class="checkbox-input"
    />
    <span class="checkbox-label">{{ label }}</span>
  </label>
</template>

<script setup lang="ts">
interface Props {
  modelValue: boolean
  label?: string
  disabled?: boolean
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void
}

withDefaults(defineProps<Props>(), {
  modelValue: false,
  disabled: false,
})

const emit = defineEmits<Emits>()

function handleChange(event: Event) {
  const target = event.target as HTMLInputElement
  emit('update:modelValue', target.checked)
}
</script>

<style scoped>
.checkbox-wrapper {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  user-select: none;
}

.checkbox-wrapper.disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.checkbox-input {
  width: 1.125rem;
  height: 1.125rem;
  cursor: pointer;
  accent-color: var(--color-primary);
}

.checkbox-wrapper.disabled .checkbox-input {
  cursor: not-allowed;
}

.checkbox-label {
  font-size: 0.9375rem;
  color: var(--color-text-primary);
  line-height: 1.5;
}
</style>
