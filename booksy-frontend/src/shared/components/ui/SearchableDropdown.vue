<template>
  <div class="searchable-dropdown" v-click-outside="closeDropdown" dir="rtl">
    <div class="dropdown-input-wrapper" :class="{ focused: isOpen }">
      <svg class="input-icon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
      </svg>
      <input
        v-model="searchQuery"
        type="text"
        :placeholder="placeholder"
        class="dropdown-input"
        @focus="openDropdown"
        @input="handleInput"
        @keydown.down.prevent="navigateDown"
        @keydown.up.prevent="navigateUp"
        @keydown.enter.prevent="selectHighlighted"
        @keydown.esc="closeDropdown"
      />
      <svg
        v-if="searchQuery && clearable"
        @click="clearInput"
        class="clear-icon"
        xmlns="http://www.w3.org/2000/svg"
        fill="none"
        viewBox="0 0 24 24"
        stroke="currentColor"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
      </svg>
    </div>

    <transition name="dropdown-fade">
      <div v-if="isOpen" class="dropdown-menu">
        <!-- Loading state -->
        <div v-if="loading" class="dropdown-loading">
          <div class="spinner"></div>
          <span>در حال جستجو...</span>
        </div>

        <!-- Minimum search length message -->
        <div v-else-if="minSearchLength > 0 && searchQuery.length < minSearchLength" class="dropdown-empty">
          لطفاً حداقل {{ minSearchLength }} حرف وارد کنید
        </div>

        <!-- Results -->
        <div v-else-if="filteredOptions.length > 0" class="dropdown-scroll">
          <div
            v-for="(option, index) in filteredOptions"
            :key="option.value"
            :class="['dropdown-item', { highlighted: index === highlightedIndex }]"
            @click="selectOption(option)"
            @mouseenter="highlightedIndex = index"
          >
            <span class="option-label">{{ option.label }}</span>
            <span v-if="option.description" class="option-description">{{ option.description }}</span>
          </div>
        </div>

        <!-- No results -->
        <div v-else-if="searchQuery && searchQuery.length >= minSearchLength" class="dropdown-empty">
          نتیجه‌ای یافت نشد
        </div>
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'

export interface DropdownOption {
  label: string
  value: string | number
  description?: string
}

interface Props {
  placeholder?: string
  options: DropdownOption[]
  modelValue?: string | number
  clearable?: boolean
  loading?: boolean
  minSearchLength?: number
}

interface Emits {
  (e: 'update:modelValue', value: string | number): void
  (e: 'select', option: DropdownOption): void
  (e: 'search', query: string): void
}

const props = withDefaults(defineProps<Props>(), {
  placeholder: 'جستجو...',
  clearable: true,
  loading: false,
  minSearchLength: 0,
})

const emit = defineEmits<Emits>()

const searchQuery = ref('')
const isOpen = ref(false)
const highlightedIndex = ref(0)

const filteredOptions = computed(() => {
  if (!searchQuery.value) {
    return props.options
  }

  const query = searchQuery.value.toLowerCase()
  return props.options.filter(option => {
    const valueStr = String(option.value).toLowerCase()
    return (
      option.label.toLowerCase().includes(query) ||
      valueStr.includes(query) ||
      option.description?.toLowerCase().includes(query)
    )
  })
})

const openDropdown = () => {
  isOpen.value = true
  highlightedIndex.value = 0
}

const closeDropdown = () => {
  isOpen.value = false
}

const handleInput = () => {
  if (!isOpen.value) {
    isOpen.value = true
  }
  highlightedIndex.value = 0

  // Emit search event for parent to handle
  emit('search', searchQuery.value)
  emit('update:modelValue', searchQuery.value)
}

const selectOption = (option: DropdownOption) => {
  searchQuery.value = option.label
  emit('update:modelValue', option.value)
  emit('select', option)
  closeDropdown()
}

const selectHighlighted = () => {
  if (filteredOptions.value.length > 0 && highlightedIndex.value >= 0) {
    selectOption(filteredOptions.value[highlightedIndex.value])
  }
}

const navigateDown = () => {
  if (highlightedIndex.value < filteredOptions.value.length - 1) {
    highlightedIndex.value++
  }
}

const navigateUp = () => {
  if (highlightedIndex.value > 0) {
    highlightedIndex.value--
  }
}

const clearInput = () => {
  searchQuery.value = ''
  emit('update:modelValue', '')
  isOpen.value = false
}

// Watch for external changes to modelValue
watch(() => props.modelValue, (newValue) => {
  if (newValue) {
    const option = props.options.find(opt => opt.value === newValue)
    if (option) {
      searchQuery.value = option.label
    }
  } else {
    searchQuery.value = ''
  }
})

// Custom directive for click outside
const vClickOutside = {
  mounted(el: HTMLElement, binding: any) {
    el.clickOutsideEvent = (event: MouseEvent) => {
      if (!(el === event.target || el.contains(event.target as Node))) {
        binding.value()
      }
    }
    document.addEventListener('click', el.clickOutsideEvent)
  },
  unmounted(el: HTMLElement) {
    document.removeEventListener('click', el.clickOutsideEvent)
  },
}
</script>

<style scoped>
.searchable-dropdown {
  position: relative;
  width: 100%;
}

.dropdown-input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.dropdown-input-wrapper.focused .dropdown-input {
  border-color: #667eea;
  box-shadow: 0 0 0 4px rgba(102, 126, 234, 0.1);
}

.input-icon {
  position: absolute;
  right: 1rem;
  width: 20px;
  height: 20px;
  color: #94a3b8;
  pointer-events: none;
  z-index: 1;
}

.clear-icon {
  position: absolute;
  left: 1rem;
  width: 20px;
  height: 20px;
  color: #94a3b8;
  cursor: pointer;
  transition: color 0.2s;
  z-index: 1;
}

.clear-icon:hover {
  color: #64748b;
}

.dropdown-input {
  width: 100%;
  padding: 1rem 3rem;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  font-size: 1rem;
  transition: all 0.3s;
  text-align: right;
  background: white;
}

.dropdown-input:focus {
  outline: none;
}

.dropdown-menu {
  position: absolute;
  top: calc(100% + 0.5rem);
  left: 0;
  right: 0;
  background: white;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.1);
  z-index: 1000;
  max-height: 300px;
  overflow: hidden;
}

.dropdown-scroll {
  max-height: 300px;
  overflow-y: auto;
}

.dropdown-item {
  padding: 0.875rem 1rem;
  cursor: pointer;
  transition: all 0.2s;
  border-bottom: 1px solid #f1f5f9;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.dropdown-item:last-child {
  border-bottom: none;
}

.dropdown-item:hover,
.dropdown-item.highlighted {
  background: #f8fafc;
}

.option-label {
  font-size: 0.95rem;
  color: #1e293b;
  font-weight: 500;
}

.option-description {
  font-size: 0.8rem;
  color: #64748b;
}

.dropdown-empty {
  padding: 1.5rem;
  text-align: center;
  color: #94a3b8;
  font-size: 0.9rem;
}

.dropdown-loading {
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  color: #64748b;
  font-size: 0.9rem;
}

.spinner {
  width: 24px;
  height: 24px;
  border: 3px solid #e2e8f0;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.dropdown-fade-enter-active,
.dropdown-fade-leave-active {
  transition: all 0.2s ease;
}

.dropdown-fade-enter-from,
.dropdown-fade-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

/* Scrollbar styling */
.dropdown-scroll::-webkit-scrollbar {
  width: 6px;
}

.dropdown-scroll::-webkit-scrollbar-track {
  background: #f1f5f9;
  border-radius: 3px;
}

.dropdown-scroll::-webkit-scrollbar-thumb {
  background: #cbd5e1;
  border-radius: 3px;
}

.dropdown-scroll::-webkit-scrollbar-thumb:hover {
  background: #94a3b8;
}
</style>
