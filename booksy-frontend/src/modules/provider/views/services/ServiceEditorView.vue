<template>
  <div class="service-editor-view">
    <!-- Header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">{{ isEditMode ? 'Edit Service' : 'Create Service' }}</h1>
        <p class="page-subtitle">{{ isEditMode ? 'Update service details' : 'Add a new service to your catalog' }}</p>
      </div>
      <Button variant="secondary" @click="handleCancel">← Back to Services</Button>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="loading-state">
      <Spinner />
      <p>{{ isEditMode ? 'Loading service...' : 'Initializing form...' }}</p>
    </div>

    <!-- Error State -->
    <Alert
      v-if="error"
      type="error"
      :message="error"
      @dismiss="error = null"
    />

    <!-- Success Message -->
    <Alert
      v-if="successMessage"
      type="success"
      :message="successMessage"
      @dismiss="successMessage = null"
    />

    <!-- Service Editor Form -->
    <form v-if="!isLoading" @submit.prevent="handleSubmit" class="service-form">
      <!-- Basic Information Section -->
      <Card class="form-section">
        <h2 class="section-title">Basic Information</h2>

        <div class="form-row">
          <div class="form-group full-width">
            <label for="name" class="form-label required">Service Name</label>
            <TextInput
              id="name"
              v-model="formData.name"
              placeholder="e.g., Women's Haircut"
              :error="validationErrors.name"
              @blur="validateField('name')"
            />
            <span v-if="validationErrors.name" class="error-message">{{ validationErrors.name }}</span>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group full-width">
            <label for="description" class="form-label required">Description</label>
            <TextArea
              id="description"
              v-model="formData.description"
              placeholder="Describe your service..."
              rows="4"
              :error="validationErrors.description"
              @blur="validateField('description')"
            />
            <span v-if="validationErrors.description" class="error-message">{{ validationErrors.description }}</span>
          </div>
        </div>

        <div class="form-row two-columns">
          <div class="form-group">
            <label for="category" class="form-label required">Category</label>
            <Select
              id="category"
              v-model="formData.category"
              :options="categoryOptions"
              placeholder="Select category"
              :error="validationErrors.category"
              @blur="validateField('category')"
            />
            <span v-if="validationErrors.category" class="error-message">{{ validationErrors.category }}</span>
          </div>

          <div class="form-group">
            <label for="type" class="form-label required">Service Type</label>
            <Select
              id="type"
              v-model="formData.type"
              :options="typeOptions"
              placeholder="Select type"
              :error="validationErrors.type"
              @blur="validateField('type')"
            />
            <span v-if="validationErrors.type" class="error-message">{{ validationErrors.type }}</span>
          </div>
        </div>
      </Card>

      <!-- Pricing Section -->
      <Card class="form-section">
        <h2 class="section-title">Pricing</h2>

        <div class="form-row two-columns">
          <div class="form-group">
            <label for="basePrice" class="form-label required">Base Price</label>
            <TextInput
              id="basePrice"
              v-model.number="formData.basePrice"
              type="number"
              min="0"
              step="0.01"
              placeholder="0.00"
              :error="validationErrors.basePrice"
              @blur="validateField('basePrice')"
            />
            <span v-if="validationErrors.basePrice" class="error-message">{{ validationErrors.basePrice }}</span>
          </div>

          <div class="form-group">
            <label for="currency" class="form-label required">Currency</label>
            <Select
              id="currency"
              v-model="formData.currency"
              :options="currencyOptions"
              placeholder="Select currency"
              :error="validationErrors.currency"
            />
            <span v-if="validationErrors.currency" class="error-message">{{ validationErrors.currency }}</span>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group full-width">
            <label class="checkbox-label">
              <input
                type="checkbox"
                v-model="formData.hasPriceTiers"
              />
              <span>Enable price tiers (different prices for staff levels, experience, etc.)</span>
            </label>
          </div>
        </div>
      </Card>

      <!-- Duration & Timing Section -->
      <Card class="form-section">
        <h2 class="section-title">Duration & Timing</h2>

        <div class="form-row two-columns">
          <div class="form-group">
            <label for="duration" class="form-label required">Service Duration (minutes)</label>
            <TextInput
              id="duration"
              v-model.number="formData.duration"
              type="number"
              min="5"
              step="5"
              placeholder="60"
              :error="validationErrors.duration"
              @blur="validateField('duration')"
            />
            <span v-if="validationErrors.duration" class="error-message">{{ validationErrors.duration }}</span>
          </div>

          <div class="form-group">
            <label for="bufferTime" class="form-label">Buffer Time (minutes)</label>
            <TextInput
              id="bufferTime"
              v-model.number="formData.bufferTime"
              type="number"
              min="0"
              step="5"
              placeholder="0"
            />
            <p class="help-text">Time between bookings for cleanup/preparation</p>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group full-width">
            <div class="info-box">
              <strong>Total Booking Slot:</strong> {{ totalDuration }} minutes
            </div>
          </div>
        </div>
      </Card>

      <!-- Availability Section -->
      <Card class="form-section">
        <h2 class="section-title">Availability</h2>

        <div class="form-row">
          <div class="form-group full-width">
            <label class="checkbox-label">
              <input
                type="checkbox"
                v-model="formData.isOnlineBookingEnabled"
              />
              <span>Enable online booking for this service</span>
            </label>
          </div>
        </div>

        <div class="form-row two-columns">
          <div class="form-group">
            <label for="maxAdvanceBookingDays" class="form-label">Max Advance Booking (days)</label>
            <TextInput
              id="maxAdvanceBookingDays"
              v-model.number="formData.maxAdvanceBookingDays"
              type="number"
              min="1"
              placeholder="90"
            />
            <p class="help-text">How far in advance customers can book</p>
          </div>

          <div class="form-group">
            <label for="minAdvanceBookingHours" class="form-label">Min Advance Booking (hours)</label>
            <TextInput
              id="minAdvanceBookingHours"
              v-model.number="formData.minAdvanceBookingHours"
              type="number"
              min="0"
              placeholder="2"
            />
            <p class="help-text">Minimum notice required for bookings</p>
          </div>
        </div>
      </Card>

      <!-- Tags & Image Section -->
      <Card class="form-section">
        <h2 class="section-title">Tags & Image</h2>

        <div class="form-row">
          <div class="form-group full-width">
            <label for="tags" class="form-label">Tags</label>
            <TextInput
              id="tags"
              v-model="tagsInput"
              placeholder="Enter tags separated by commas (e.g., popular, new, trending)"
            />
            <p class="help-text">Press Enter or use comma to separate tags</p>
          </div>
        </div>

        <div v-if="formData.tags.length > 0" class="tags-display">
          <span
            v-for="(tag, index) in formData.tags"
            :key="tag"
            class="tag-chip"
          >
            {{ tag }}
            <button type="button" class="tag-remove" @click="removeTag(index)">×</button>
          </span>
        </div>

        <div class="form-row">
          <div class="form-group full-width">
            <label for="imageUrl" class="form-label">Service Image URL</label>
            <TextInput
              id="imageUrl"
              v-model="formData.imageUrl"
              placeholder="https://example.com/image.jpg"
            />
            <p class="help-text">Image upload functionality coming soon</p>
          </div>
        </div>

        <div v-if="formData.imageUrl" class="image-preview">
          <img :src="formData.imageUrl" alt="Service preview" />
        </div>
      </Card>

      <!-- Form Actions -->
      <div class="form-actions">
        <Button
          type="button"
          variant="secondary"
          @click="handleCancel"
          :disabled="isSaving"
        >
          Cancel
        </Button>

        <Button
          v-if="!isEditMode"
          type="button"
          variant="secondary"
          @click="handleSaveDraft"
          :disabled="isSaving"
        >
          Save as Draft
        </Button>

        <Button
          type="submit"
          variant="primary"
          :disabled="isSaving"
          :loading="isSaving"
        >
          {{ isSaving ? 'Saving...' : (isEditMode ? 'Update Service' : 'Create Service') }}
        </Button>
      </div>
    </form>

    <!-- Unsaved Changes Confirmation -->
    <ConfirmModal
      v-if="showUnsavedConfirm"
      :is-open="showUnsavedConfirm"
      title="Unsaved Changes"
      message="You have unsaved changes. Are you sure you want to leave?"
      confirm-text="Leave"
      cancel-text="Stay"
      @confirm="confirmLeave"
      @cancel="cancelLeave"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useServiceStore } from '../../stores/service.store'
import { useProviderStore } from '../../stores/provider.store'
import {
  Button,
  Card,
  TextInput,
  TextArea,
  Select,
  Alert,
  Spinner,
  ConfirmModal,
} from '@/shared/components'
import {
  ServiceCategory,
  ServiceType,
  ServiceStatus,
  SERVICE_CATEGORY_LABELS,
  SERVICE_TYPE_LABELS,
  type CreateServiceRequest,
  type UpdateServiceRequest,
} from '../../types/service.types'

const router = useRouter()
const route = useRoute()
const serviceStore = useServiceStore()
const providerStore = useProviderStore()

// State
const isLoading = ref(false)
const isSaving = ref(false)
const error = ref<string | null>(null)
const successMessage = ref<string | null>(null)
const showUnsavedConfirm = ref(false)
const hasUnsavedChanges = ref(false)

// Form data
const formData = reactive({
  name: '',
  description: '',
  category: '' as ServiceCategory | '',
  type: '' as ServiceType | '',
  basePrice: 0,
  currency: 'USD',
  duration: 60,
  bufferTime: 0,
  isOnlineBookingEnabled: true,
  maxAdvanceBookingDays: 90,
  minAdvanceBookingHours: 2,
  tags: [] as string[],
  imageUrl: '',
  hasPriceTiers: false,
  status: ServiceStatus.Active,
})

const tagsInput = ref('')
const validationErrors = reactive<Record<string, string>>({})

// Computed
const isEditMode = computed(() => !!route.params.id)

const totalDuration = computed(() => {
  return (formData.duration || 0) + (formData.bufferTime || 0)
})

// Options
const categoryOptions = Object.entries(SERVICE_CATEGORY_LABELS).map(([value, label]) => ({
  value,
  label,
}))

const typeOptions = Object.entries(SERVICE_TYPE_LABELS).map(([value, label]) => ({
  value,
  label,
}))

const currencyOptions = [
  { value: 'USD', label: 'USD ($)' },
  { value: 'EUR', label: 'EUR (€)' },
  { value: 'GBP', label: 'GBP (£)' },
  { value: 'IRR', label: 'IRR (﷼)' },
]

// Lifecycle
onMounted(async () => {
  if (isEditMode.value) {
    await loadService()
  } else {
    // Load provider for providerId
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

// Watch tags input for comma or Enter
watch(tagsInput, (newValue) => {
  if (newValue.includes(',')) {
    const tags = newValue.split(',').map((t) => t.trim()).filter((t) => t)
    tags.forEach((tag) => {
      if (!formData.tags.includes(tag)) {
        formData.tags.push(tag)
      }
    })
    tagsInput.value = ''
  }
})

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

    // Populate form
    formData.name = service.name
    formData.description = service.description
    formData.category = service.category
    formData.type = service.type
    formData.basePrice = service.basePrice
    formData.currency = service.currency
    formData.duration = service.duration
    formData.bufferTime = service.bufferTime || 0
    formData.isOnlineBookingEnabled = service.isOnlineBookingEnabled
    formData.maxAdvanceBookingDays = service.maxAdvanceBookingDays || 90
    formData.minAdvanceBookingHours = service.minAdvanceBookingHours || 2
    formData.tags = service.tags || []
    formData.imageUrl = service.imageUrl || ''
    formData.hasPriceTiers = !!service.priceTiers?.length
    formData.status = service.status

    hasUnsavedChanges.value = false
  } catch (err: unknown) {
    error.value = err instanceof Error ? err.message : 'Failed to load service'
    console.error('Error loading service:', err)
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

    case 'description':
      if (!formData.description.trim()) {
        validationErrors.description = 'Description is required'
        return false
      }
      if (formData.description.length < 10) {
        validationErrors.description = 'Description must be at least 10 characters'
        return false
      }
      break

    case 'category':
      if (!formData.category) {
        validationErrors.category = 'Category is required'
        return false
      }
      break

    case 'type':
      if (!formData.type) {
        validationErrors.type = 'Service type is required'
        return false
      }
      break

    case 'basePrice':
      if (formData.basePrice < 0) {
        validationErrors.basePrice = 'Price cannot be negative'
        return false
      }
      break

    case 'duration':
      if (!formData.duration || formData.duration < 5) {
        validationErrors.duration = 'Duration must be at least 5 minutes'
        return false
      }
      break

    case 'currency':
      if (!formData.currency) {
        validationErrors.currency = 'Currency is required'
        return false
      }
      break
  }

  return true
}

function validateForm(): boolean {
  const fields: Array<keyof typeof formData> = [
    'name',
    'description',
    'category',
    'type',
    'basePrice',
    'currency',
    'duration',
  ]

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
      // Update existing service
      const updateData: UpdateServiceRequest = {
        name: formData.name,
        description: formData.description,
        category: formData.category as ServiceCategory,
        type: formData.type as ServiceType,
        basePrice: formData.basePrice,
        currency: formData.currency,
        duration: formData.duration,
        bufferTime: formData.bufferTime || 0,
        isOnlineBookingEnabled: formData.isOnlineBookingEnabled,
        maxAdvanceBookingDays: formData.maxAdvanceBookingDays,
        minAdvanceBookingHours: formData.minAdvanceBookingHours,
        tags: formData.tags,
        imageUrl: formData.imageUrl || undefined,
        status: formData.status,
      }

      await serviceStore.updateService(route.params.id as string, updateData)
      successMessage.value = 'Service updated successfully!'
    } else {
      // Create new service
      const createData: CreateServiceRequest = {
        providerId: providerStore.currentProvider.id,
        name: formData.name,
        description: formData.description,
        category: formData.category as ServiceCategory,
        type: formData.type as ServiceType,
        basePrice: formData.basePrice,
        currency: formData.currency,
        duration: formData.duration,
        bufferTime: formData.bufferTime || 0,
        isOnlineBookingEnabled: formData.isOnlineBookingEnabled,
        maxAdvanceBookingDays: formData.maxAdvanceBookingDays,
        minAdvanceBookingHours: formData.minAdvanceBookingHours,
        tags: formData.tags,
        imageUrl: formData.imageUrl || undefined,
        status: ServiceStatus.Active,
      }

      await serviceStore.createService(createData)
      successMessage.value = 'Service created successfully!'
    }

    hasUnsavedChanges.value = false

    // Redirect after short delay
    setTimeout(() => {
      router.push({ name: 'ProviderServices' })
    }, 1500)
  } catch (err: unknown) {
    error.value = err instanceof Error ? err.message : 'Failed to save service'
    console.error('Error saving service:', err)
  } finally {
    isSaving.value = false
  }
}

async function handleSaveDraft() {
  if (!validateForm()) {
    error.value = 'Please fix the validation errors before saving'
    return
  }

  isSaving.value = true
  error.value = null

  try {
    if (!providerStore.currentProvider?.id) {
      throw new Error('Provider information not available')
    }

    const createData: CreateServiceRequest = {
      providerId: providerStore.currentProvider.id,
      name: formData.name,
      description: formData.description,
      category: formData.category as ServiceCategory,
      type: formData.type as ServiceType,
      basePrice: formData.basePrice,
      currency: formData.currency,
      duration: formData.duration,
      bufferTime: formData.bufferTime || 0,
      isOnlineBookingEnabled: formData.isOnlineBookingEnabled,
      maxAdvanceBookingDays: formData.maxAdvanceBookingDays,
      minAdvanceBookingHours: formData.minAdvanceBookingHours,
      tags: formData.tags,
      imageUrl: formData.imageUrl || undefined,
      status: ServiceStatus.Draft,
    }

    await serviceStore.createService(createData)
    successMessage.value = 'Draft saved successfully!'
    hasUnsavedChanges.value = false

    setTimeout(() => {
      router.push({ name: 'ProviderServices' })
    }, 1500)
  } catch (err: unknown) {
    error.value = err instanceof Error ? err.message : 'Failed to save draft'
    console.error('Error saving draft:', err)
  } finally {
    isSaving.value = false
  }
}

function handleCancel() {
  if (hasUnsavedChanges.value) {
    showUnsavedConfirm.value = true
  } else {
    router.push({ name: 'ProviderServices' })
  }
}

function confirmLeave() {
  showUnsavedConfirm.value = false
  hasUnsavedChanges.value = false
  router.push({ name: 'ProviderServices' })
}

function cancelLeave() {
  showUnsavedConfirm.value = false
}

function removeTag(index: number) {
  formData.tags.splice(index, 1)
}
</script>

<style scoped lang="scss">
.service-editor-view {
  max-width: 1000px;
  margin: 0 auto;
  padding: 2rem;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 2rem;
  gap: 2rem;
}

.page-title {
  font-size: 2rem;
  font-weight: 700;
  margin: 0 0 0.5rem 0;
  color: #111827;
}

.page-subtitle {
  font-size: 1rem;
  color: #6b7280;
  margin: 0;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  gap: 1rem;
}

.service-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-section {
  padding: 2rem;
}

.section-title {
  font-size: 1.25rem;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 1.5rem 0;
  padding-bottom: 0.75rem;
  border-bottom: 2px solid #e5e7eb;
}

.form-row {
  display: flex;
  gap: 1.5rem;
  margin-bottom: 1.5rem;

  &:last-child {
    margin-bottom: 0;
  }

  &.two-columns {
    .form-group {
      flex: 1;
    }
  }
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;

  &.full-width {
    flex: 1;
    width: 100%;
  }
}

.form-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;

  &.required::after {
    content: ' *';
    color: #ef4444;
  }
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #374151;
  cursor: pointer;

  input[type='checkbox'] {
    width: 1rem;
    height: 1rem;
    cursor: pointer;
  }
}

.error-message {
  font-size: 0.75rem;
  color: #ef4444;
  margin-top: -0.25rem;
}

.help-text {
  font-size: 0.75rem;
  color: #6b7280;
  margin: 0;
}

.info-box {
  padding: 0.75rem 1rem;
  background: #f0f9ff;
  border: 1px solid #bfdbfe;
  border-radius: 6px;
  font-size: 0.875rem;
  color: #1e40af;
}

.tags-display {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.tag-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.375rem 0.75rem;
  background: #e5e7eb;
  color: #1f2937;
  border-radius: 9999px;
  font-size: 0.875rem;

  .tag-remove {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 1rem;
    height: 1rem;
    padding: 0;
    background: transparent;
    border: none;
    color: #6b7280;
    font-size: 1.25rem;
    line-height: 1;
    cursor: pointer;
    transition: color 0.2s;

    &:hover {
      color: #ef4444;
    }
  }
}

.image-preview {
  margin-top: 1rem;
  border-radius: 8px;
  overflow: hidden;
  max-width: 400px;

  img {
    width: 100%;
    height: auto;
    display: block;
  }
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  padding-top: 1.5rem;
  border-top: 2px solid #e5e7eb;
}

@media (max-width: 768px) {
  .service-editor-view {
    padding: 1rem;
  }

  .page-header {
    flex-direction: column;
    align-items: stretch;
  }

  .form-section {
    padding: 1.5rem;
  }

  .form-row {
    flex-direction: column;

    &.two-columns {
      .form-group {
        width: 100%;
      }
    }
  }

  .form-actions {
    flex-direction: column-reverse;

    button {
      width: 100%;
    }
  }
}
</style>
