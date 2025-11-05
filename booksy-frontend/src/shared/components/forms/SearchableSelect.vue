<template>
  <div class="searchable-select" :class="{ disabled, error: !!error }" ref="containerRef">
    <label v-if="label" class="select-label">
      {{ label }}
      <span v-if="required" class="required">*</span>
    </label>

    <div class="select-wrapper" @click="toggleDropdown">
      <input
        autocomplete="off"
        autocorrect="off"
        autocapitalize="off"
        spellcheck="false"
        name="searchable-select-input"
        :id="`searchable-select-${Math.random()}`"
        ref="inputRef"
        v-model="searchQuery"
        type="text"
        class="select-input"
        :placeholder="placeholder"
        :disabled="disabled"
        :readonly="true"
        @focus="openDropdown"
        @keydown="handleKeydown"
      />
      <svg class="select-icon" :class="{ open: isOpen }" viewBox="0 0 20 20" fill="currentColor">
        <path
          fill-rule="evenodd"
          d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z"
          clip-rule="evenodd"
        />
      </svg>
    </div>

    <Transition name="dropdown">
      <div v-if="isOpen" class="dropdown">
        <div v-if="filteredOptions.length === 0" class="no-results">
          {{ noResultsText }}
        </div>
        <div
          v-for="(option, index) in filteredOptions"
          :key="option.value"
          class="dropdown-item"
          :class="{ selected: option.value === modelValue, focused: index === focusedIndex }"
          @click="selectOption(option)"
          @mouseenter="focusedIndex = index"
        >
          {{ option.label }}
        </div>
      </div>
    </Transition>

    <div v-if="error" class="error-message">{{ error }}</div>
    <div v-else-if="hint" class="hint-message">{{ hint }}</div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'

export interface SelectOption {
  label: string
  value: string | number
}

interface Props {
  modelValue: string | number | null
  options: SelectOption[]
  label?: string
  placeholder?: string
  error?: string
  hint?: string
  disabled?: boolean
  required?: boolean
  noResultsText?: string
}

const props = withDefaults(defineProps<Props>(), {
  placeholder: 'Search...',
  noResultsText: 'No results found',
})

const emit = defineEmits<{
  'update:modelValue': [value: string | number | null]
}>()

const containerRef = ref<HTMLElement>()
const inputRef = ref<HTMLInputElement>()
const isOpen = ref(false)
const searchQuery = ref('')
const focusedIndex = ref(0)

const filteredOptions = computed(() => {
  // Always return all options (no filtering/search)
  return props.options
})

const selectedOption = computed(() => props.options.find((opt) => opt.value === props.modelValue))

// Update search query when selection changes from outside
watch(
  () => props.modelValue,
  (newValue) => {
    if (newValue) {
      const option = props.options.find((opt) => opt.value === newValue)
      if (option) {
        searchQuery.value = option.label
      }
    } else {
      searchQuery.value = ''
    }
  },
  { immediate: true },
)

function toggleDropdown() {
  if (props.disabled) return
  isOpen.value = !isOpen.value
  if (isOpen.value) {
    focusedIndex.value = 0
  }
}

function openDropdown() {
  if (props.disabled) return
  isOpen.value = true
  focusedIndex.value = 0
}

function closeDropdown() {
  isOpen.value = false
  if (selectedOption.value) {
    searchQuery.value = selectedOption.value.label
  } else {
    searchQuery.value = ''
  }
}

function selectOption(option: SelectOption) {
  emit('update:modelValue', option.value)
  searchQuery.value = option.label
  closeDropdown()
}

function handleKeydown(event: KeyboardEvent) {
  if (!isOpen.value && (event.key === 'ArrowDown' || event.key === 'ArrowUp')) {
    openDropdown()
    event.preventDefault()
    return
  }

  if (!isOpen.value) return

  switch (event.key) {
    case 'ArrowDown':
      event.preventDefault()
      focusedIndex.value = Math.min(focusedIndex.value + 1, filteredOptions.value.length - 1)
      break
    case 'ArrowUp':
      event.preventDefault()
      focusedIndex.value = Math.max(focusedIndex.value - 1, 0)
      break
    case 'Enter':
      event.preventDefault()
      if (filteredOptions.value[focusedIndex.value]) {
        selectOption(filteredOptions.value[focusedIndex.value])
      }
      break
    case 'Escape':
      event.preventDefault()
      closeDropdown()
      break
  }
}

function handleClickOutside(event: MouseEvent) {
  if (containerRef.value && !containerRef.value.contains(event.target as Node)) {
    closeDropdown()
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>

<style scoped>
.searchable-select {
  position: relative;
  width: 100%;
  isolation: isolate;
}

.select-label {
  display: block;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
  margin-bottom: var(--spacing-xs);
}

.required {
  color: var(--color-danger-500);
  margin-inline-start: 2px;
}

.select-wrapper {
  position: relative;
  cursor: pointer;
}

.select-input {
  width: 100%;
  padding: var(--spacing-sm) var(--spacing-md);
  padding-inline-end: 40px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  font-size: var(--font-size-sm);
  color: var(--color-text-primary);
  background: var(--color-background);
  transition: all var(--transition-fast);
  cursor: pointer;
}

.select-input:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px var(--color-primary-100);
}

.select-input:disabled {
  background: var(--color-gray-50);
  cursor: not-allowed;
  opacity: 0.6;
}

.searchable-select.error .select-input {
  border-color: var(--color-danger-500);
}

.searchable-select.error .select-input:focus {
  box-shadow: 0 0 0 3px var(--color-danger-100);
}

.select-icon {
  position: absolute;
  inset-inline-end: var(--spacing-sm);
  top: 50%;
  transform: translateY(-50%);
  width: 20px;
  height: 20px;
  color: var(--color-text-tertiary);
  transition: transform var(--transition-fast);
  pointer-events: none;
}

.select-icon.open {
  transform: translateY(-50%) rotate(180deg);
}

.dropdown {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  right: 0;
  max-height: 250px;
  overflow-y: auto;
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-lg);
  z-index: 1000;
  margin-top: 0;
}

.dropdown-item {
  padding: var(--spacing-sm) var(--spacing-md);
  cursor: pointer;
  transition: background-color var(--transition-fast);
}

.dropdown-item:hover,
.dropdown-item.focused {
  background: var(--color-gray-50);
}

.dropdown-item.selected {
  background: var(--color-primary-50);
  color: var(--color-primary-600);
  font-weight: var(--font-weight-medium);
}

.no-results {
  padding: var(--spacing-md);
  text-align: center;
  color: var(--color-text-tertiary);
  font-size: var(--font-size-sm);
}

.error-message {
  margin-top: var(--spacing-xs);
  font-size: var(--font-size-xs);
  color: var(--color-danger-500);
}

.hint-message {
  margin-top: var(--spacing-xs);
  font-size: var(--font-size-xs);
  color: var(--color-text-tertiary);
}

.dropdown-enter-active,
.dropdown-leave-active {
  transition: all var(--transition-fast);
}

.dropdown-enter-from {
  opacity: 0;
  transform: translateY(-10px);
}

.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

/* Custom scrollbar */
.dropdown::-webkit-scrollbar {
  width: 6px;
}

.dropdown::-webkit-scrollbar-track {
  background: var(--color-gray-50);
}

.dropdown::-webkit-scrollbar-thumb {
  background: var(--color-gray-300);
  border-radius: var(--radius-full);
}

.dropdown::-webkit-scrollbar-thumb:hover {
  background: var(--color-gray-400);
}
</style>
