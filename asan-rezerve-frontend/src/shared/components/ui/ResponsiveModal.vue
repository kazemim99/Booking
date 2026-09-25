<template>
  <!-- Desktop: Regular Modal -->
  <BaseModal
    v-if="!isMobile"
    :is-open="isOpen"
    :title="title"
    :size="size"
    @close="handleClose"
  >
    <slot />
  </BaseModal>

  <!-- Mobile: Bottom Sheet -->
  <BottomSheet
    v-else
    :is-open="isOpen"
    :title="title"
    :height="mobileHeight"
    :show-close-button="showCloseButton"
    :close-on-overlay="closeOnOverlay"
    :swipe-to-dismiss="swipeToDismiss"
    @close="handleClose"
  >
    <slot />
  </BottomSheet>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import BaseModal from './BaseModal.vue'
import BottomSheet from './BottomSheet.vue'

interface Props {
  isOpen: boolean
  title?: string
  size?: 'sm' | 'md' | 'lg' | 'xl'
  mobileHeight?: 'half' | 'full' | 'auto'
  showCloseButton?: boolean
  closeOnOverlay?: boolean
  swipeToDismiss?: boolean
}

withDefaults(defineProps<Props>(), {
  size: 'md',
  mobileHeight: 'auto',
  showCloseButton: true,
  closeOnOverlay: true,
  swipeToDismiss: true,
})

const emit = defineEmits<{
  close: []
}>()

const windowWidth = ref(window.innerWidth)

const isMobile = computed(() => windowWidth.value < 768)

function handleResize(): void {
  windowWidth.value = window.innerWidth
}

function handleClose(): void {
  emit('close')
}

onMounted(() => {
  window.addEventListener('resize', handleResize)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', handleResize)
})
</script>
