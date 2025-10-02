<template>
  <div class="dropdown" ref="dropdownRef">
    <!-- Trigger -->
    <div
      class="dropdown-trigger"
      @click="toggle"
      @keydown.enter="toggle"
      @keydown.space.prevent="toggle"
      @keydown.esc="close"
      :aria-expanded="isOpen"
      :aria-haspopup="true"
      role="button"
      tabindex="0"
    >
      <slot name="trigger">
        <button type="button" class="dropdown-button">
          <span>{{ label }}</span>
          <svg
            class="dropdown-icon"
            :class="{ rotated: isOpen }"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M19 9l-7 7-7-7"
            />
          </svg>
        </button>
      </slot>
    </div>

    <!-- Dropdown Menu -->
    <Transition name="dropdown-fade">
      <div
        v-if="isOpen"
        v-click-outside="handleClickOutside"
        :class="dropdownMenuClasses"
        :style="dropdownMenuStyles"
        role="menu"
      >
        <slot />
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { DropdownEmits, DropdownProps } from '@/core/types/AppDropdown.types'
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'

const props = withDefaults(defineProps<DropdownProps>(), {
  position: 'bottom-left',
  offset: 8,
  closeOnSelect: true,
  disabled: false,
  width: 'auto',
})

const emit = defineEmits<DropdownEmits>()

const dropdownRef = ref<HTMLElement | null>(null)
const isOpen = ref(false)

const dropdownMenuClasses = computed(() => {
  const [vertical, horizontal] = props.position.split('-')
  return [
    'dropdown-menu',
    `dropdown-menu-${vertical}`,
    `dropdown-menu-${horizontal}`,
    { 'dropdown-menu-full-width': props.width === 'full' },
  ]
})

const dropdownMenuStyles = computed(() => {
  const styles: Record<string, string> = {}

  if (typeof props.width === 'number') {
    styles.width = `${props.width}px`
  } else if (props.width === 'trigger') {
    styles.width = '100%'
  } else if (props.width !== 'auto' && props.width !== 'full') {
    styles.width = props.width
  }

  return styles
})

function toggle(): void {
  if (props.disabled) return

  if (isOpen.value) {
    close()
  } else {
    open()
  }
}

function open(): void {
  if (props.disabled) return

  isOpen.value = true
  emit('open')
  emit('update:modelValue', true)
}

function close(): void {
  isOpen.value = false
  emit('close')
  emit('update:modelValue', false)
}

function handleClickOutside(): void {
  if (isOpen.value) {
    close()
  }
}

function handleEscapeKey(event: KeyboardEvent): void {
  if (event.key === 'Escape' && isOpen.value) {
    close()
  }
}

// Close on select if enabled
function handleMenuClick(event: Event): void {
  if (props.closeOnSelect) {
    const target = event.target as HTMLElement
    // Check if clicked element is an interactive element
    if (
      target.tagName === 'A' ||
      target.tagName === 'BUTTON' ||
      target.closest('a') ||
      target.closest('button')
    ) {
      close()
    }
  }
}

// Watch modelValue prop for external control
watch(
  () => props.modelValue,
  (newValue) => {
    isOpen.value = newValue ?? isOpen.value
  },
)

onMounted(() => {
  document.addEventListener('keydown', handleEscapeKey)
  dropdownRef.value?.addEventListener('click', handleMenuClick)
})

onUnmounted(() => {
  document.removeEventListener('keydown', handleEscapeKey)
  dropdownRef.value?.removeEventListener('click', handleMenuClick)
})

defineExpose({
  open,
  close,
  toggle,
  isOpen,
})
</script>

<style scoped lang="scss">
.dropdown {
  position: relative;
  display: inline-block;
}

.dropdown-trigger {
  display: inline-block;
  cursor: pointer;

  &:focus {
    outline: none;
  }
}

.dropdown-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 1rem;
  background: white;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  color: #374151;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    border-color: #9ca3af;
    background: #f9fafb;
  }

  &:focus {
    outline: none;
    border-color: #667eea;
    box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
  }
}

.dropdown-icon {
  width: 16px;
  height: 16px;
  color: #6b7280;
  transition: transform 0.2s;

  &.rotated {
    transform: rotate(180deg);
  }
}

.dropdown-menu {
  position: absolute;
  background: white;
  border-radius: 8px;
  box-shadow:
    0 10px 15px -3px rgba(0, 0, 0, 0.1),
    0 4px 6px -2px rgba(0, 0, 0, 0.05);
  border: 1px solid #e5e7eb;
  z-index: 100;
  min-width: 180px;
  max-width: 400px;
  overflow: hidden;

  // Vertical positioning
  &.dropdown-menu-bottom {
    top: calc(100% + v-bind('props.offset + "px"'));
  }

  &.dropdown-menu-top {
    bottom: calc(100% + v-bind('props.offset + "px"'));
  }

  // Horizontal positioning
  &.dropdown-menu-left {
    left: 0;
  }

  &.dropdown-menu-right {
    right: 0;
  }

  &.dropdown-menu-center {
    left: 50%;
    transform: translateX(-50%);
  }

  &.dropdown-menu-full-width {
    width: 100%;
    max-width: none;
  }
}

// Dropdown transitions
.dropdown-fade-enter-active,
.dropdown-fade-leave-active {
  transition: all 0.2s ease;
}

.dropdown-fade-enter-from {
  opacity: 0;
  transform: translateY(-8px);

  &.dropdown-menu-top {
    transform: translateY(8px);
  }

  &.dropdown-menu-center {
    transform: translateX(-50%) translateY(-8px);
  }
}

.dropdown-fade-leave-to {
  opacity: 0;
}

// Responsive adjustments
@media (max-width: 640px) {
  .dropdown-menu {
    max-width: calc(100vw - 2rem);
  }
}

// Dark theme support
[data-theme='dark'] {
  .dropdown-button {
    background: #1f2937;
    border-color: #374151;
    color: #f9fafb;

    &:hover {
      border-color: #4b5563;
      background: #374151;
    }
  }

  .dropdown-menu {
    background: #1f2937;
    border-color: #374151;
  }
}
</style>
