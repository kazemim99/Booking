<template>
  <Teleport to="body">
    <div class="confirmation-overlay" @click="handleCancel">
      <div class="confirmation-modal" :class="`variant-${variant}`" @click.stop>
        <!-- Icon -->
        <div class="modal-icon" :class="`icon-${variant}`">
          <i :class="iconClass"></i>
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
import AppButton from './ui/AppButton.vue'

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

const iconClass = computed(() => {
  switch (props.variant) {
    case 'danger':
      return 'icon-alert-triangle'
    case 'warning':
      return 'icon-alert-circle'
    case 'info':
      return 'icon-info'
    case 'success':
      return 'icon-check-circle'
    default:
      return 'icon-alert-triangle'
  }
})

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

  i {
    font-size: 2.5rem;
  }

  &.icon-danger {
    background: #fee2e2;

    i {
      color: #dc2626;
    }
  }

  &.icon-warning {
    background: #fef3c7;

    i {
      color: #f59e0b;
    }
  }

  &.icon-info {
    background: #dbeafe;

    i {
      color: #3b82f6;
    }
  }

  &.icon-success {
    background: #d1fae5;

    i {
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

    i {
      font-size: 2rem;
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
