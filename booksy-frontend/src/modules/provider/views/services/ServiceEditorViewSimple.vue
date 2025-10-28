<template>
  <div class="modern-service-editor">
    <!-- Header -->
    <div class="editor-header">
      <button class="back-btn" @click="handleCancel">
        <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
          <path d="M12 4L6 10l6 6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
        <span>{{ $t('services.backToServices') }}</span>
      </button>
      <h1 class="editor-title">
        {{ isEditMode ? $t('services.editService') : $t('services.createService') }}
      </h1>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="loading-container">
      <div class="spinner"></div>
      <p>{{ isEditMode ? 'Loading service...' : 'Initializing form...' }}</p>
    </div>

    <!-- Error Alert -->
    <div v-if="error" class="alert alert-error">
      <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
      </svg>
      <span>{{ error }}</span>
      <button @click="error = null" class="alert-close">×</button>
    </div>

    <!-- Success Alert -->
    <div v-if="successMessage" class="alert alert-success">
      <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
      </svg>
      <span>{{ successMessage }}</span>
    </div>

    <!-- Service Form -->
    <form v-if="!isLoading" @submit.prevent="handleSubmit" class="service-form">
      <!-- Service Name -->
      <div class="form-card">
        <div class="form-group">
          <label for="serviceName" class="form-label">
            {{ $t('services.serviceName') }}
            <span class="required">*</span>
          </label>
          <input
            id="serviceName"
            v-model="formData.name"
            type="text"
            class="form-input"
            :class="{ error: validationErrors.name }"
            :placeholder="$t('services.serviceNamePlaceholder')"
            @blur="validateField('name')"
          />
          <span v-if="validationErrors.name" class="error-message">
            {{ validationErrors.name }}
          </span>
        </div>
      </div>

      <!-- Duration and Price -->
      <div class="form-card">
        <div class="form-row">
          <!-- Duration -->
          <div class="form-group">
            <label for="duration" class="form-label">
              {{ $t('services.duration') }} ({{ $t('services.min') }})
              <span class="required">*</span>
            </label>
            <div class="input-with-icon">
              <svg class="input-icon" width="20" height="20" viewBox="0 0 20 20" fill="none">
                <circle cx="10" cy="10" r="8" stroke="currentColor" stroke-width="1.5"/>
                <path d="M10 6v4l3 2" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
              </svg>
              <input
                id="duration"
                v-model.number="formData.duration"
                type="number"
                min="5"
                step="5"
                class="form-input with-icon"
                :class="{ error: validationErrors.duration }"
                :placeholder="$t('services.durationPlaceholder')"
                @blur="validateField('duration')"
              />
            </div>
            <span v-if="validationErrors.duration" class="error-message">
              {{ validationErrors.duration }}
            </span>
          </div>

          <!-- Price -->
          <div class="form-group">
            <label for="price" class="form-label">
              {{ $t('services.price') }}
              <span class="required">*</span>
            </label>
            <div class="input-with-icon">
              <span class="currency-icon">{{ currencySymbol }}</span>
              <input
                id="price"
                v-model.number="formData.basePrice"
                type="number"
                min="0"
                step="0.01"
                class="form-input with-currency"
                :class="{ error: validationErrors.basePrice }"
                :placeholder="$t('services.pricePlaceholder')"
                @blur="validateField('basePrice')"
              />
            </div>
            <span v-if="validationErrors.basePrice" class="error-message">
              {{ validationErrors.basePrice }}
            </span>
          </div>
        </div>
      </div>

      <!-- Action Buttons -->
      <div class="form-actions">
        <button
          type="button"
          class="btn-secondary"
          @click="handleCancel"
          :disabled="isSaving"
        >
          {{ $t('common.cancel') }}
        </button>

        <button
          type="submit"
          class="btn-primary"
          :disabled="isSaving"
        >
          <span v-if="isSaving" class="btn-spinner"></span>
          <span v-else>
            {{ isEditMode ? $t('services.update') : $t('services.createNew') }}
          </span>
        </button>
      </div>
    </form>

    <!-- Unsaved Changes Modal -->
    <div v-if="showUnsavedModal" class="modal-overlay" @click="cancelLeave">
      <div class="modal" @click.stop>
        <h3 class="modal-title">Unsaved Changes</h3>
        <p class="modal-text">You have unsaved changes. Are you sure you want to leave?</p>
        <div class="modal-actions">
          <button class="modal-btn secondary" @click="cancelLeave">
            Stay
          </button>
          <button class="modal-btn danger" @click="confirmLeave">
            Leave
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useServiceStore } from '../../stores/service.store'
import { useProviderStore } from '../../stores/provider.store'
import { ServiceStatus, type CreateServiceRequest, type UpdateServiceRequest, type Service } from '../../types/service.types'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const serviceStore = useServiceStore()
const providerStore = useProviderStore()

// State
const isLoading = ref(false)
const isSaving = ref(false)
const error = ref<string | null>(null)
const successMessage = ref<string | null>(null)
const showUnsavedModal = ref(false)
const hasUnsavedChanges = ref(false)
const loadedService = ref<Service | null>(null) // Store loaded service to access providerId

// Form data - SIMPLIFIED: only name, duration, price
const formData = reactive({
  name: '',
  duration: 60,
  basePrice: 0,
  currency: 'USD',
})

const validationErrors = reactive<Record<string, string>>({})

// Computed
const isEditMode = computed(() => !!route.params.id)

const currencySymbol = computed(() => {
  const symbols: Record<string, string> = {
    USD: '$',
    EUR: '€',
    GBP: '£',
    IRR: '﷼'
  }
  return symbols[formData.currency] || formData.currency
})

// Lifecycle
onMounted(async () => {
  if (isEditMode.value) {
    await loadService()
  } else {
    if (!providerStore.currentProvider) {
      await providerStore.loadCurrentProvider()
    }
  }
})

// Watch for unsaved changes
watch(
  formData,
  () => {
    hasUnsavedChanges.value = true
  },
  { deep: true }
)

// Methods
async function loadService() {
  isLoading.value = true
  error.value = null

  try {
    const serviceId = route.params.id as string
    const service = await serviceStore.getServiceById(serviceId)

    if (!service) {
      throw new Error('Service not found')
    }

    // Store the loaded service to access providerId later
    loadedService.value = service

    formData.name = service.name
    formData.duration = service.duration
    formData.basePrice = service.basePrice
    formData.currency = service.currency

    hasUnsavedChanges.value = false
  } catch (err: unknown) {
    error.value = err instanceof Error ? err.message : 'Failed to load service'
  } finally {
    isLoading.value = false
  }
}

function validateField(field: keyof typeof formData): boolean {
  delete validationErrors[field]

  switch (field) {
    case 'name':
      if (!formData.name.trim()) {
        validationErrors.name = 'Service name is required'
        return false
      }
      if (formData.name.length < 3) {
        validationErrors.name = 'Service name must be at least 3 characters'
        return false
      }
      break

    case 'duration':
      if (!formData.duration || formData.duration < 5) {
        validationErrors.duration = 'Duration must be at least 5 minutes'
        return false
      }
      break

    case 'basePrice':
      if (formData.basePrice < 0) {
        validationErrors.basePrice = 'Price cannot be negative'
        return false
      }
      break
  }

  return true
}

function validateForm(): boolean {
  const fields: Array<keyof typeof formData> = ['name', 'duration', 'basePrice']

  let isValid = true
  fields.forEach((field) => {
    if (!validateField(field)) {
      isValid = false
    }
  })

  return isValid
}

async function handleSubmit() {
  error.value = null
  successMessage.value = null

  if (!validateForm()) {
    error.value = 'Please fix the validation errors before submitting'
    return
  }

  isSaving.value = true

  try {
    if (!providerStore.currentProvider?.id) {
      throw new Error('Provider information not available')
    }

    if (isEditMode.value) {
      // Update existing service - SIMPLIFIED
      // Get providerId from loaded service or fallback to current provider
      const providerId = loadedService.value?.providerId || providerStore.currentProvider.id

      const updateData: UpdateServiceRequest = {
        providerId,
        serviceName: formData.name,
        durationHours: Math.floor(formData.duration / 60),
        durationMinutes: formData.duration % 60,
        price: formData.basePrice,
        currency: formData.currency,
      }

      await serviceStore.updateService(route.params.id as string, updateData)
      successMessage.value = t('services.serviceUpdatedSuccess')
    } else {
      // Create new service - SIMPLIFIED
      const createData: CreateServiceRequest = {
        providerId: providerStore.currentProvider.id,
        serviceName: formData.name,
        durationHours: Math.floor(formData.duration / 60),
        durationMinutes: formData.duration % 60,
        price: formData.basePrice,
        currency: formData.currency,
        // Optional fields
        description: formData.name,
        category: 'HairCare',
      }

      await serviceStore.createService(createData)
      successMessage.value = t('services.serviceCreatedSuccess')
    }

    hasUnsavedChanges.value = false

    // Redirect after short delay
    setTimeout(() => {
      router.push({ name: 'ProviderServices' })
    }, 1500)
  } catch (err: unknown) {
    error.value = err instanceof Error ? err.message : 'Failed to save service'
  } finally {
    isSaving.value = false
  }
}

function handleCancel() {
  if (hasUnsavedChanges.value) {
    showUnsavedModal.value = true
  } else {
    router.push({ name: 'ProviderServices' })
  }
}

function confirmLeave() {
  showUnsavedModal.value = false
  hasUnsavedChanges.value = false
  router.push({ name: 'ProviderServices' })
}

function cancelLeave() {
  showUnsavedModal.value = false
}
</script>

<style scoped lang="scss">
.modern-service-editor {
  min-height: 100vh;
  background: linear-gradient(to bottom, #f9fafb 0%, #ffffff 100%);
  padding: 2rem;
  max-width: 800px;
  margin: 0 auto;
}

// Header
.editor-header {
  margin-bottom: 2rem;
}

.back-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  font-size: 0.938rem;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.2s;
  margin-bottom: 1rem;

  &:hover {
    background: #f9fafb;
    border-color: #d1d5db;
  }
}

.editor-title {
  font-size: 2rem;
  font-weight: 700;
  color: #111827;
  margin: 0;
}

// Loading
.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem;
  gap: 1rem;
}

.spinner {
  width: 48px;
  height: 48px;
  border: 4px solid #e5e7eb;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

// Alerts
.alert {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem 1.25rem;
  border-radius: 12px;
  margin-bottom: 1.5rem;
  animation: slideIn 0.3s ease;

  &-error {
    background: #fee2e2;
    color: #991b1b;
  }

  &-success {
    background: #d1fae5;
    color: #065f46;
  }
}

.alert-close {
  margin-left: auto;
  padding: 0.25rem 0.5rem;
  background: transparent;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  opacity: 0.6;
  transition: opacity 0.2s;

  &:hover {
    opacity: 1;
  }
}

@keyframes slideIn {
  from {
    transform: translateY(-10px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

// Form
.service-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-card {
  background: white;
  padding: 2rem;
  border-radius: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.5rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-label {
  font-size: 0.938rem;
  font-weight: 600;
  color: #374151;

  .required {
    color: #ef4444;
    margin-left: 0.25rem;
  }
}

.form-input {
  padding: 0.875rem 1rem;
  border: 2px solid #e5e7eb;
  border-radius: 12px;
  font-size: 1rem;
  transition: all 0.2s;
  background: white;

  &:focus {
    outline: none;
    border-color: #667eea;
    box-shadow: 0 0 0 4px rgba(102, 126, 234, 0.1);
  }

  &.error {
    border-color: #ef4444;
  }

  &.with-icon {
    padding-left: 3rem;
  }

  &.with-currency {
    padding-left: 3rem;
  }
}

.input-with-icon {
  position: relative;
}

.input-icon {
  position: absolute;
  left: 1rem;
  top: 50%;
  transform: translateY(-50%);
  color: #9ca3af;
  pointer-events: none;
}

.currency-icon {
  position: absolute;
  left: 1rem;
  top: 50%;
  transform: translateY(-50%);
  color: #6b7280;
  font-weight: 600;
  pointer-events: none;
}

.error-message {
  font-size: 0.813rem;
  color: #ef4444;
}

// Actions
.form-actions {
  display: flex;
  gap: 1rem;
  padding-top: 1rem;
}

.btn-secondary,
.btn-primary {
  flex: 1;
  padding: 1rem 2rem;
  border: none;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

.btn-secondary {
  background: #f3f4f6;
  color: #374151;

  &:hover:not(:disabled) {
    background: #e5e7eb;
  }
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);

  &:hover:not(:disabled) {
    transform: translateY(-2px);
    box-shadow: 0 8px 20px rgba(102, 126, 234, 0.4);
  }
}

.btn-spinner {
  display: inline-block;
  width: 20px;
  height: 20px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

// Modal
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  backdrop-filter: blur(4px);
}

.modal {
  background: white;
  border-radius: 20px;
  padding: 2rem;
  max-width: 400px;
  width: 90%;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}

.modal-title {
  font-size: 1.5rem;
  font-weight: 600;
  color: #111827;
  margin: 0 0 1rem 0;
}

.modal-text {
  font-size: 1rem;
  color: #6b7280;
  margin: 0 0 2rem 0;
  line-height: 1.6;
}

.modal-actions {
  display: flex;
  gap: 1rem;
}

.modal-btn {
  flex: 1;
  padding: 0.875rem;
  border: none;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;

  &.secondary {
    background: #f3f4f6;
    color: #374151;

    &:hover {
      background: #e5e7eb;
    }
  }

  &.danger {
    background: #ef4444;
    color: white;

    &:hover {
      background: #dc2626;
    }
  }
}

// Responsive
@media (max-width: 768px) {
  .modern-service-editor {
    padding: 1rem;
  }

  .form-card {
    padding: 1.5rem;
  }

  .form-row {
    grid-template-columns: 1fr;
  }

  .form-actions {
    flex-direction: column-reverse;
  }
}
</style>
