<template>
  <div
    :class="[
      'textarea-wrapper',
      { 'textarea-wrapper-error': error, 'textarea-wrapper-disabled': disabled },
    ]"
  >
    <label v-if="label" :for="textareaId" class="textarea-label">
      {{ label }}
      <span v-if="required" class="textarea-required">*</span>
    </label>

    <textarea
      :id="textareaId"
      ref="textareaRef"
      :value="modelValue"
      :placeholder="placeholder"
      :disabled="disabled"
      :readonly="readonly"
      :required="required"
      :maxlength="maxlength"
      :rows="rows"
      :class="textareaClasses"
      @input="handleInput"
      @blur="handleBlur"
      @focus="handleFocus"
    />

    <div v-if="showCount && maxlength" class="textarea-count">
      {{ characterCount }} / {{ maxlength }}
    </div>

    <span v-if="error" class="textarea-error">{{ error }}</span>
    <span v-else-if="hint" class="textarea-hint">{{ hint }}</span>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'

interface Props {
  modelValue?: string
  label?: string
  placeholder?: string
  hint?: string
  error?: string
  disabled?: boolean
  readonly?: boolean
  required?: boolean
  maxlength?: number
  rows?: number
  showCount?: boolean
  resize?: 'none' | 'both' | 'horizontal' | 'vertical'
  autofocus?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  rows: 4,
  showCount: false,
  resize: 'vertical',
  disabled: false,
  readonly: false,
  required: false,
  autofocus: false,
})

interface Emits {
  (e: 'update:modelValue', value: string): void
  (e: 'blur', event: FocusEvent): void
  (e: 'focus', event: FocusEvent): void
}

const emit = defineEmits<Emits>()

const textareaRef = ref<HTMLTextAreaElement | null>(null)
const textareaId = computed(() => `textarea-${Math.random().toString(36).substr(2, 9)}`)

const characterCount = computed(() => props.modelValue?.length || 0)

const textareaClasses = computed(() => [
  'textarea',
  `textarea-resize-${props.resize}`,
  {
    'textarea-error': props.error,
  },
])

function handleInput(event: Event) {
  const target = event.target as HTMLTextAreaElement
  emit('update:modelValue', target.value)
}

function handleBlur(event: FocusEvent) {
  emit('blur', event)
}

function handleFocus(event: FocusEvent) {
  emit('focus', event)
}

function focus() {
  textareaRef.value?.focus()
}

onMounted(() => {
  if (props.autofocus) {
    focus()
  }
})

defineExpose({
  focus,
  textareaRef,
})
</script>

<style scoped lang="scss">
.textarea-wrapper {
  margin-bottom: 1.5rem;

  &:last-child {
    margin-bottom: 0;
  }
}

.textarea-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
  margin-bottom: 0.5rem;
}

.textarea-required {
  color: #ef4444;
  margin-left: 0.25rem;
}

.textarea {
  width: 100%;
  padding: 0.75rem 1rem;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  background: white;
  color: #1f2937;
  font-family: inherit;
  font-size: 1rem;
  line-height: 1.5;
  transition: all 0.2s;
  outline: none;

  &::placeholder {
    color: #9ca3af;
  }

  &:hover:not(:disabled) {
    border-color: #9ca3af;
  }

  &:focus {
    border-color: #667eea;
    box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
  }

  &:disabled {
    background-color: #f3f4f6;
    cursor: not-allowed;
    opacity: 0.6;
  }

  &:readonly {
    background-color: #f9fafb;
  }

  &-error {
    border-color: #ef4444;

    &:focus {
      border-color: #ef4444;
      box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
    }
  }

  &-resize {
    &-none {
      resize: none;
    }
    &-both {
      resize: both;
    }
    &-horizontal {
      resize: horizontal;
    }
    &-vertical {
      resize: vertical;
    }
  }
}

.textarea-count {
  text-align: right;
  font-size: 0.75rem;
  color: #6b7280;
  margin-top: 0.25rem;
}

.textarea-error {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.75rem;
  color: #ef4444;
}

.textarea-hint {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.75rem;
  color: #6b7280;
}

.textarea-wrapper-disabled {
  opacity: 0.6;
}
</style>
