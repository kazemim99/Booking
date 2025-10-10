<template>
  <div class="navigation-buttons">
    <button
      v-if="showBack"
      type="button"
      class="btn btn-secondary"
      :disabled="disabled || loading"
      @click="$emit('back')"
    >
      <svg
        class="icon-left"
        viewBox="0 0 20 20"
        fill="currentColor"
        xmlns="http://www.w3.org/2000/svg"
      >
        <path
          fill-rule="evenodd"
          d="M9.707 16.707a1 1 0 01-1.414 0l-6-6a1 1 0 010-1.414l6-6a1 1 0 011.414 1.414L5.414 9H17a1 1 0 110 2H5.414l4.293 4.293a1 1 0 010 1.414z"
          clip-rule="evenodd"
        />
      </svg>
      {{ backLabel || $t('common.back') }}
    </button>

    <button
      v-if="showSaveDraft"
      type="button"
      class="btn btn-text"
      :disabled="disabled || loading || !isDirty"
      @click="$emit('save-draft')"
    >
      {{ $t('provider.registration.saveDraft') }}
    </button>

    <button
      type="button"
      class="btn btn-primary"
      :disabled="disabled || loading || !canContinue"
      @click="$emit('next')"
    >
      <span v-if="loading" class="loading-spinner"></span>
      <span>{{ nextLabel || $t('common.continue') }}</span>
      <svg
        v-if="!loading"
        class="icon-right"
        viewBox="0 0 20 20"
        fill="currentColor"
        xmlns="http://www.w3.org/2000/svg"
      >
        <path
          fill-rule="evenodd"
          d="M10.293 3.293a1 1 0 011.414 0l6 6a1 1 0 010 1.414l-6 6a1 1 0 01-1.414-1.414L14.586 11H3a1 1 0 110-2h11.586l-4.293-4.293a1 1 0 010-1.414z"
          clip-rule="evenodd"
        />
      </svg>
    </button>
  </div>
</template>

<script setup lang="ts">
interface Props {
  showBack?: boolean
  showSaveDraft?: boolean
  canContinue?: boolean
  isDirty?: boolean
  disabled?: boolean
  loading?: boolean
  backLabel?: string
  nextLabel?: string
}

withDefaults(defineProps<Props>(), {
  showBack: true,
  showSaveDraft: false,
  canContinue: true,
  isDirty: false,
  disabled: false,
  loading: false,
})

interface Emits {
  (e: 'back'): void
  (e: 'next'): void
  (e: 'save-draft'): void
}

defineEmits<Emits>()
</script>

<style scoped>
.navigation-buttons {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 1px solid #e5e7eb;
}

.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  font-size: 0.875rem;
  font-weight: 600;
  border-radius: 0.5rem;
  border: none;
  cursor: pointer;
  transition: all 0.2s ease;
  white-space: nowrap;
}

.btn-primary {
  background-color: #111827;
  color: #ffffff;
  flex: 1;
  max-width: 200px;
  margin-left: auto;
}

.btn-primary:hover:not(:disabled) {
  background-color: #1f2937;
  transform: translateY(-1px);
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.2);
}

.btn-primary:active:not(:disabled) {
  transform: translateY(0);
}

.btn-primary:disabled {
  background-color: #d1d5db;
  cursor: not-allowed;
  transform: none;
}

.btn-secondary {
  background-color: transparent;
  color: #6b7280;
  border: 1px solid #d1d5db;
}

.btn-secondary:hover:not(:disabled) {
  background-color: #f9fafb;
  border-color: #9ca3af;
  color: #111827;
}

.btn-secondary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-text {
  background: none;
  border: none;
  color: #6b7280;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  padding: 0.5rem 1rem;
}

.btn-text:hover:not(:disabled) {
  color: #111827;
  text-decoration: underline;
}

.btn-text:disabled {
  color: #d1d5db;
  cursor: not-allowed;
}

.icon-left,
.icon-right {
  width: 1.25rem;
  height: 1.25rem;
}

.loading-spinner {
  width: 1rem;
  height: 1rem;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: #ffffff;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* Responsive */
@media (max-width: 640px) {
  .navigation-buttons {
    flex-wrap: wrap;
    gap: 0.75rem;
  }

  .btn-primary {
    order: 1;
    max-width: none;
    flex: 1 1 100%;
  }

  .btn-secondary {
    order: 2;
    flex: 1;
  }

  .btn-text {
    order: 3;
    flex: 1;
  }
}
</style>
