<template>
  <teleport to="body">
    <transition name="bottomsheet">
      <div v-if="isOpen" class="bottomsheet-overlay" @click="handleOverlayClick">
        <div
          ref="sheetRef"
          class="bottomsheet"
          :class="[`height-${height}`, { dragging: isDragging }]"
          :style="sheetStyle"
          @click.stop
          @touchstart="handleTouchStart"
          @touchmove="handleTouchMove"
          @touchend="handleTouchEnd"
        >
          <!-- Drag Handle -->
          <div class="drag-handle-container" @mousedown="handleMouseDown">
            <div class="drag-handle"></div>
          </div>

          <!-- Header (optional) -->
          <div v-if="title" class="bottomsheet-header">
            <h3 class="bottomsheet-title">{{ title }}</h3>
            <button
              v-if="showCloseButton"
              @click="handleClose"
              class="close-button"
              aria-label="بستن"
            >
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <!-- Content -->
          <div class="bottomsheet-content" :class="{ 'has-header': title }">
            <slot />
          </div>
        </div>
      </div>
    </transition>
  </teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch, onBeforeUnmount } from 'vue'

interface Props {
  isOpen: boolean
  title?: string
  height?: 'half' | 'full' | 'auto'
  showCloseButton?: boolean
  closeOnOverlay?: boolean
  swipeToDismiss?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  height: 'auto',
  showCloseButton: true,
  closeOnOverlay: true,
  swipeToDismiss: true,
})

const emit = defineEmits<{
  close: []
}>()

const sheetRef = ref<HTMLElement | null>(null)
const isDragging = ref(false)
const startY = ref(0)
const currentY = ref(0)
const translateY = ref(0)

const sheetStyle = computed(() => {
  if (translateY.value > 0) {
    return {
      transform: `translateY(${translateY.value}px)`,
      transition: isDragging.value ? 'none' : 'transform 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    }
  }
  return {}
})

// Touch handlers for swipe-to-dismiss
function handleTouchStart(event: TouchEvent): void {
  if (!props.swipeToDismiss) return

  const touch = event.touches[0]
  startY.value = touch.clientY
  currentY.value = touch.clientY
  isDragging.value = true
}

function handleTouchMove(event: TouchEvent): void {
  if (!props.swipeToDismiss || !isDragging.value) return

  const touch = event.touches[0]
  currentY.value = touch.clientY
  const diff = currentY.value - startY.value

  // Only allow downward dragging
  if (diff > 0) {
    translateY.value = diff
  }
}

function handleTouchEnd(): void {
  if (!props.swipeToDismiss || !isDragging.value) return

  isDragging.value = false

  // If dragged down more than 100px, close the sheet
  if (translateY.value > 100) {
    handleClose()
  } else {
    // Snap back
    translateY.value = 0
  }
}

// Mouse handlers (for desktop testing)
function handleMouseDown(event: MouseEvent): void {
  if (!props.swipeToDismiss) return

  startY.value = event.clientY
  currentY.value = event.clientY
  isDragging.value = true

  document.addEventListener('mousemove', handleMouseMove)
  document.addEventListener('mouseup', handleMouseUp)
}

function handleMouseMove(event: MouseEvent): void {
  if (!isDragging.value) return

  currentY.value = event.clientY
  const diff = currentY.value - startY.value

  // Only allow downward dragging
  if (diff > 0) {
    translateY.value = diff
  }
}

function handleMouseUp(): void {
  if (!isDragging.value) return

  isDragging.value = false

  document.removeEventListener('mousemove', handleMouseMove)
  document.removeEventListener('mouseup', handleMouseUp)

  // If dragged down more than 100px, close the sheet
  if (translateY.value > 100) {
    handleClose()
  } else {
    // Snap back
    translateY.value = 0
  }
}

function handleOverlayClick(): void {
  if (props.closeOnOverlay) {
    handleClose()
  }
}

function handleClose(): void {
  translateY.value = 0
  emit('close')
}

// Handle ESC key
function handleEscKey(event: KeyboardEvent): void {
  if (event.key === 'Escape' && props.isOpen) {
    handleClose()
  }
}

// Lock body scroll when sheet is open
watch(() => props.isOpen, (isOpen) => {
  if (isOpen) {
    document.body.style.overflow = 'hidden'
    document.addEventListener('keydown', handleEscKey)
  } else {
    document.body.style.overflow = ''
    document.removeEventListener('keydown', handleEscKey)
    translateY.value = 0
  }
})

onBeforeUnmount(() => {
  document.body.style.overflow = ''
  document.removeEventListener('keydown', handleEscKey)
  document.removeEventListener('mousemove', handleMouseMove)
  document.removeEventListener('mouseup', handleMouseUp)
})
</script>

<style scoped lang="scss">
.bottomsheet-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  z-index: 9999;
  display: flex;
  align-items: flex-end;
  animation: fadeIn 0.3s ease-out;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

.bottomsheet {
  width: 100%;
  background: white;
  border-radius: 24px 24px 0 0;
  box-shadow: 0 -4px 24px rgba(0, 0, 0, 0.15);
  display: flex;
  flex-direction: column;
  max-height: 90vh;
  overflow: hidden;

  &.height-half {
    height: 50vh;
    min-height: 300px;
  }

  &.height-full {
    height: 90vh;
  }

  &.height-auto {
    max-height: 90vh;
  }

  &.dragging {
    transition: none !important;
  }
}

.drag-handle-container {
  padding: 0.75rem 0;
  display: flex;
  justify-content: center;
  cursor: grab;
  -webkit-tap-highlight-color: transparent;

  &:active {
    cursor: grabbing;
  }
}

.drag-handle {
  width: 40px;
  height: 4px;
  background: #d1d5db;
  border-radius: 2px;
  transition: background 0.2s;

  .drag-handle-container:hover & {
    background: #9ca3af;
  }
}

.bottomsheet-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 1.5rem 1rem 1.5rem;
  border-bottom: 1px solid #e5e7eb;
}

.bottomsheet-title {
  font-size: 1.25rem;
  font-weight: 600;
  color: #111827;
  margin: 0;
}

.close-button {
  background: none;
  border: none;
  cursor: pointer;
  color: #6b7280;
  padding: 0.5rem;
  border-radius: 8px;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;

  svg {
    width: 20px;
    height: 20px;
  }

  &:hover {
    background: #f3f4f6;
    color: #111827;
  }

  &:active {
    transform: scale(0.95);
  }
}

.bottomsheet-content {
  flex: 1;
  overflow-y: auto;
  padding: 1.5rem;
  -webkit-overflow-scrolling: touch;

  &.has-header {
    padding-top: 1rem;
  }

  /* Custom scrollbar */
  &::-webkit-scrollbar {
    width: 6px;
  }

  &::-webkit-scrollbar-track {
    background: transparent;
  }

  &::-webkit-scrollbar-thumb {
    background: #d1d5db;
    border-radius: 3px;

    &:hover {
      background: #9ca3af;
    }
  }
}

/* Transitions */
.bottomsheet-enter-active {
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.bottomsheet-leave-active {
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.bottomsheet-enter-from,
.bottomsheet-leave-to {
  opacity: 0;

  .bottomsheet {
    transform: translateY(100%);
  }
}

/* RTL Support */
[dir='rtl'] {
  .bottomsheet-header {
    direction: rtl;
  }

  .bottomsheet-content {
    direction: rtl;
  }
}

/* Landscape mobile optimization */
@media (max-height: 500px) and (orientation: landscape) {
  .bottomsheet.height-full {
    height: 95vh;
  }

  .bottomsheet-content {
    padding: 1rem;
  }
}
</style>
