<template>
  <Teleport to="body">
    <div class="confirmation-overlay" @click="handleCancel">
      <div class="confirmation-modal" :class="`variant-${variant}`" @click.stop>
        <!-- Icon or Image Preview -->
        <div class="modal-icon" :class="`icon-${variant}`">
          <!-- Image Preview -->
          <img v-if="imageUrl" :src="imageUrl" :alt="title" class="modal-image" />
          <!-- Icons (when no image) -->
          <template v-else>
            <!-- Alert Triangle (Danger) -->
            <svg
              v-if="variant === 'danger'"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M12 9v2m0 4v2m0 0a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            <!-- Alert Circle (Warning) -->
            <svg
              v-else-if="variant === 'warning'"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M12 8v4m0 4v2m0 0a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            <!-- Info (Info) -->
            <svg
              v-else-if="variant === 'info'"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            <!-- Check Circle (Success) -->
            <svg
              v-else-if="variant === 'success'"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
          </template>
        </div>

        <!-- Content -->
        <div class="modal-content">
          <h3 class="modal-title">{{ title }}</h3>
          <p class="modal-message">{{ message }}</p>
        </div>

        <!-- Actions -->
        <div class="modal-actions">
          <AppButton
            variant="secondary"
            size="medium"
            @click="handleCancel"
            :disabled="isProcessing"
          >
            {{ cancelText }}
          </AppButton>

          <AppButton
            :variant="confirmVariant"
            size="medium"
            @click="handleConfirm"
            :disabled="isProcessing"
            :loading="isProcessing"
          >
            {{ confirmText }}
          </AppButton>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import AppButton from './ui/Button/AppButton.vue'

// ============================================
// Props & Emits
// ============================================

interface Props {
  title: string
  message: string
  confirmText?: string
  cancelText?: string
  variant?: 'danger' | 'warning' | 'info' | 'success'
  processing?: boolean
  imageUrl?: string
}

const props = withDefaults(defineProps<Props>(), {
  confirmText: 'تأیید',
  cancelText: 'انصراف',
  variant: 'danger',
  processing: false,
})

const emit = defineEmits<{
  (e: 'confirm'): void
  (e: 'cancel'): void
}>()

// ============================================
// State
// ============================================

const isProcessing = ref(props.processing)

// ============================================
// Computed
// ============================================

const confirmVariant = computed(() => {
  switch (props.variant) {
    case 'danger':
      return 'danger'
    case 'warning':
      return 'warning'
    case 'info':
      return 'primary'
    case 'success':
      return 'success'
    default:
      return 'primary'
  }
})

// ============================================
// Methods
// ============================================

function handleConfirm(): void {
  emit('confirm')
}

function handleCancel(): void {
  if (isProcessing.value) return
  emit('cancel')
}
</script>

<style scoped lang="scss">
.confirmation-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 1rem;
  backdrop-filter: blur(4px);
  animation: fadeIn 0.2s ease-out;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

.confirmation-modal {
  background: #fff;
  border-radius: 16px;
  max-width: 480px;
  width: 100%;
  padding: 2rem;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  animation: slideUp 0.3s ease-out;
  text-align: center;
}

@keyframes slideUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.modal-icon {
  width: 80px;
  height: 80px;
  margin: 0 auto 1.5rem;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  border: 3px solid transparent;

  svg {
    width: 2.5rem;
    height: 2.5rem;
  }

  .modal-image {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  &.icon-danger {
    background: #fee2e2;
    border-color: #dc2626;

    svg {
      color: #dc2626;
    }
  }

  &.icon-warning {
    background: #fef3c7;
    border-color: #f59e0b;

    svg {
      color: #f59e0b;
    }
  }

  &.icon-info {
    background: #dbeafe;
    border-color: #3b82f6;

    svg {
      color: #3b82f6;
    }
  }

  &.icon-success {
    background: #d1fae5;
    border-color: #10b981;

    svg {
      color: #10b981;
    }
  }
}

.modal-content {
  margin-bottom: 2rem;
}

.modal-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1a1a1a;
  margin-bottom: 0.75rem;
}

.modal-message {
  font-size: 1rem;
  color: #6b7280;
  line-height: 1.6;
  margin: 0;
}

.modal-actions {
  display: flex;
  gap: 1rem;
  justify-content: center;

  @media (max-width: 480px) {
    flex-direction: column-reverse;
  }
}

// Responsive
@media (max-width: 480px) {
  .confirmation-modal {
    padding: 1.5rem;
  }

  .modal-icon {
    width: 60px;
    height: 60px;

    svg {
      width: 2rem;
      height: 2rem;
    }
  }

  .modal-title {
    font-size: 1.25rem;
  }

  .modal-message {
    font-size: 0.95rem;
  }
}
</style>
