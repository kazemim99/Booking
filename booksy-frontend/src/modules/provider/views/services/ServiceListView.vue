<template>
  <div class="modern-service-catalog">
    <!-- Provider Not Found Warning -->
    <div
      v-if="!providerStore.currentProvider && !providerStore.isLoading"
      class="alert alert-warning"
    >
      <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
        <path
          fill-rule="evenodd"
          d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
          clip-rule="evenodd"
        />
      </svg>
      <div>
        <strong>Provider Not Found</strong>
        <p>You need to register as a provider before you can manage services.</p>
        <button class="alert-btn" @click="$router.push({ name: 'ProviderRegistration' })">
          Register as Provider
        </button>
      </div>
    </div>

    <!-- Modern Header with gradient -->
    <div class="catalog-header">
      <div class="header-content">
        <div class="header-text">
          <h1 class="catalog-title">{{ $t('services.title') }}</h1>
          <p class="catalog-subtitle">{{ $t('services.manageYourServices') }}</p>
        </div>
        <button class="create-btn" @click="handleCreateService">
          <span class="btn-icon">+</span>
          <span>{{ $t('services.addService') }}</span>
        </button>
      </div>
    </div>

    <!-- Search Bar -->
    <div class="search-container">
      <div class="search-wrapper">
        <svg class="search-icon" width="20" height="20" viewBox="0 0 20 20" fill="none">
          <path
            d="M9 17A8 8 0 1 0 9 1a8 8 0 0 0 0 16zM18 18l-4-4"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
          />
        </svg>
        <input
          v-model="searchTerm"
          type="text"
          :placeholder="$t('services.searchPlaceholder')"
          class="search-input"
          @input="handleSearch"
        />
        <button v-if="searchTerm" class="clear-search" @click="clearSearch">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <path
              d="M12 4L4 12M4 4l8 8"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
            />
          </svg>
        </button>
      </div>
    </div>

    <!-- Stats Cards -->
    <div v-if="serviceStore.hasServices" class="stats-grid">
      <div class="stat-card">
        <div class="stat-value">{{ serviceStore.totalServices }}</div>
        <div class="stat-label">{{ $t('services.totalServices') }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-value">{{ activeCount }}</div>
        <div class="stat-label">{{ $t('services.activeServices') }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-value">{{ draftCount }}</div>
        <div class="stat-label">{{ $t('services.draftServices') }}</div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="serviceStore.isLoading && !serviceStore.hasServices" class="loading-state">
      <div class="spinner"></div>
      <p>{{ $t('services.loadingServices') }}</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="!serviceStore.hasServices" class="empty-state">
      <div class="empty-icon">
        <svg width="64" height="64" viewBox="0 0 64 64" fill="none">
          <circle cx="32" cy="32" r="30" stroke="#e5e7eb" stroke-width="2" stroke-dasharray="4 4" />
          <path d="M32 20v24M20 32h24" stroke="#9ca3af" stroke-width="2" stroke-linecap="round" />
        </svg>
      </div>
      <h3 class="empty-title">{{ $t('services.noServicesYet') }}</h3>
      <p class="empty-text">{{ $t('services.noServicesMessage') }}</p>
      <button class="empty-action-btn" @click="handleCreateService">
        {{ $t('services.addFirstService') }}
      </button>
    </div>

    <!-- No Results -->
    <div v-else-if="!serviceStore.hasFilteredServices" class="empty-state">
      <div class="empty-icon">
        <svg width="64" height="64" viewBox="0 0 64 64" fill="none">
          <circle cx="25" cy="25" r="15" stroke="#9ca3af" stroke-width="2" />
          <path d="M36 36l10 10" stroke="#9ca3af" stroke-width="2" stroke-linecap="round" />
        </svg>
      </div>
      <h3 class="empty-title">{{ $t('services.noServicesFound') }}</h3>
      <p class="empty-text">{{ $t('services.noServicesFoundMessage') }}</p>
      <button class="empty-action-btn" @click="clearSearch">
        {{ $t('services.clearFilters') }}
      </button>
    </div>

    <!-- Services Grid -->
    <div v-else class="services-grid">
      <div
        v-for="service in serviceStore.filteredServices"
        :key="service.id"
        class="service-card"
        @click="handleEditService(service.id)"
      >
        <div class="service-card-header">
          <h3 class="service-name">{{ service.name }}</h3>
          <button class="service-menu-btn" @click.stop="toggleMenu(service.id)">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
              <circle cx="10" cy="5" r="1.5" />
              <circle cx="10" cy="10" r="1.5" />
              <circle cx="10" cy="15" r="1.5" />
            </svg>
          </button>
        </div>

        <div class="service-details">
          <div class="service-detail-item">
            <svg class="detail-icon" width="16" height="16" viewBox="0 0 16 16" fill="none">
              <circle cx="8" cy="8" r="7" stroke="currentColor" stroke-width="1.5" />
              <path
                d="M8 4v4l3 2"
                stroke="currentColor"
                stroke-width="1.5"
                stroke-linecap="round"
              />
            </svg>
            <span>{{ service.duration }} {{ $t('services.min') }}</span>
          </div>

          <div class="service-detail-item">
            <svg class="detail-icon" width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path
                d="M8 1v2M8 13v2M3 8H1m14 0h-2"
                stroke="currentColor"
                stroke-width="1.5"
                stroke-linecap="round"
              />
              <circle cx="8" cy="8" r="3" stroke="currentColor" stroke-width="1.5" />
            </svg>
            <span class="price">{{ formatPrice(service.basePrice, service.currency) }}</span>
          </div>
        </div>

        <!-- Menu Dropdown -->
        <div v-if="activeMenuId === service.id" class="service-menu" @click.stop>
          <button class="menu-item" @click="handleEditService(service.id)">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path
                d="M2 14h2l8-8-2-2-8 8v2zM14 4l-2-2"
                stroke="currentColor"
                stroke-width="1.5"
                stroke-linecap="round"
              />
            </svg>
            <span>{{ $t('common.edit') }}</span>
          </button>
          <button class="menu-item danger" @click="handleDeleteService(service.id)">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path
                d="M3 4h10M6 4V3a1 1 0 011-1h2a1 1 0 011 1v1M5 4v9a1 1 0 001 1h4a1 1 0 001-1V4"
                stroke="currentColor"
                stroke-width="1.5"
                stroke-linecap="round"
              />
            </svg>
            <span>{{ $t('common.delete') }}</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Service Form Modal -->
    <div v-if="showServiceModal" class="modal-overlay" @click="closeServiceModal">
      <div class="modal service-form-modal" @click.stop>
        <div class="modal-header">
          <h3 class="modal-title">
            {{ isEditMode ? $t('services.editService') : $t('services.createService') }}
          </h3>
          <button class="modal-close" @click="closeServiceModal">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
              <path
                d="M18 6L6 18M6 6l12 12"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
              />
            </svg>
          </button>
        </div>

        <!-- Error Alert -->
        <div v-if="formError" class="alert alert-error">
          <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
            <path
              fill-rule="evenodd"
              d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z"
              clip-rule="evenodd"
            />
          </svg>
          <span>{{ formError }}</span>
        </div>

        <form @submit.prevent="handleSubmitForm" class="modal-body">
          <!-- Service Name -->
          <div class="form-group">
            <label for="serviceName" class="form-label">
              {{ $t('services.serviceName') }}
              <span class="required">*</span>
            </label>
            <input
              id="serviceName"
              v-model="serviceForm.name"
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

          <!-- Duration and Price -->
          <div class="form-row">
            <!-- Duration -->
            <div class="form-group">
              <label for="duration" class="form-label">
                {{ $t('services.duration') }} ({{ $t('services.min') }})
                <span class="required">*</span>
              </label>
              <div class="input-with-icon">
                <svg class="input-icon" width="20" height="20" viewBox="0 0 20 20" fill="none">
                  <circle cx="10" cy="10" r="8" stroke="currentColor" stroke-width="1.5" />
                  <path
                    d="M10 6v4l3 2"
                    stroke="currentColor"
                    stroke-width="1.5"
                    stroke-linecap="round"
                  />
                </svg>
                <input
                  id="duration"
                  v-model.number="serviceForm.duration"
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
                  v-model.number="serviceForm.basePrice"
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

          <!-- Modal Actions -->
          <div class="modal-actions">
            <button
              type="button"
              class="modal-btn secondary"
              @click="closeServiceModal"
              :disabled="isSaving"
            >
              {{ $t('common.cancel') }}
            </button>
            <button type="submit" class="modal-btn primary" :disabled="isSaving">
              <span v-if="isSaving" class="btn-spinner"></span>
              <span v-else>
                {{ isEditMode ? $t('services.update') : $t('services.createNew') }}
              </span>
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Delete Confirmation Modal -->
    <div v-if="showDeleteModal" class="modal-overlay" @click="showDeleteModal = false">
      <div class="modal" @click.stop>
        <h3 class="modal-title">{{ $t('services.deleteService') }}</h3>
        <p class="modal-text">{{ $t('services.confirmDelete') }}</p>
        <div class="modal-actions">
          <button class="modal-btn secondary" @click="showDeleteModal = false">
            {{ $t('common.cancel') }}
          </button>
          <button class="modal-btn danger" @click="confirmDelete">
            {{ $t('common.delete') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useServiceStore } from '../../stores/service.store'
import { useProviderStore } from '../../stores/provider.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import {
  type CreateServiceRequest,
  type UpdateServiceRequest,
} from '../../types/service.types'

const _router = useRouter()
const { t } = useI18n()
const serviceStore = useServiceStore()
const providerStore = useProviderStore()
const authStore = useAuthStore()

const searchTerm = ref('')
const activeMenuId = ref<string | null>(null)
const showDeleteModal = ref(false)
const serviceToDelete = ref<string | null>(null)

// Service Form Modal State
const showServiceModal = ref(false)
const isEditMode = ref(false)
const isSaving = ref(false)
const formError = ref<string | null>(null)
const editingServiceId = ref<string | null>(null)

const serviceForm = reactive({
  name: '',
  duration: 60,
  basePrice: 0,
  currency: 'USD',
})

const validationErrors = reactive<Record<string, string>>({})

const activeCount = computed(
  () => serviceStore.services.filter((s) => s.status === 'Active').length,
)

const draftCount = computed(() => serviceStore.services.filter((s) => s.status === 'Draft').length)

const currencySymbol = computed(() => {
  const symbols: Record<string, string> = {
    USD: '$',
    EUR: '€',
    GBP: '£',
    IRR: '﷼',
  }
  return symbols[serviceForm.currency] || serviceForm.currency
})

onMounted(async () => {
  try {
    await providerStore.loadCurrentProvider()
    console.log(
      '[ServiceList] Provider load completed. Current provider:',
      providerStore.currentProvider,
    )
  } catch (err) {
    console.error('[ServiceList] Failed to load provider:', err)
  }
  // Check if provider is now available
  if (providerStore.currentProvider?.id) {
    console.log('[ServiceList] Loading services for provider:', providerStore.currentProvider.id)
    try {
      await serviceStore.loadServices(providerStore.currentProvider.id)
      console.log('[ServiceList] Services loaded:', serviceStore.services.length)
    } catch (err) {
      console.error('[ServiceList] Failed to load services:', err)
    }
  } else {
    console.error('[ServiceList] ⚠️ NO PROVIDER FOUND - User needs to register as provider')
    console.log('[ServiceList] Check these:')
    console.log('  - Is user logged in?')
    console.log('  - Does providerId exist in JWT token?', authStore.providerId)
    console.log('  - Does backend have provider record for this user?')
  }
})

function handleSearch() {
  serviceStore.setSearchTerm(searchTerm.value)
}

function clearSearch() {
  searchTerm.value = ''
  serviceStore.setSearchTerm('')
}

function handleCreateService() {
  isEditMode.value = false
  editingServiceId.value = null
  resetForm()
  showServiceModal.value = true
}

function handleEditService(serviceId: string) {
  isEditMode.value = true
  editingServiceId.value = serviceId

  const service = serviceStore.serviceById(serviceId)
  if (service) {
    serviceForm.name = service.name
    serviceForm.duration = service.duration
    serviceForm.basePrice = service.basePrice
    serviceForm.currency = service.currency
  }

  showServiceModal.value = true
}

function toggleMenu(serviceId: string) {
  activeMenuId.value = activeMenuId.value === serviceId ? null : serviceId
}

function handleDeleteService(serviceId: string) {
  serviceToDelete.value = serviceId
  showDeleteModal.value = true
  activeMenuId.value = null
}

async function confirmDelete() {
  if (serviceToDelete.value) {
    try {
      await serviceStore.deleteService(serviceToDelete.value)
      showDeleteModal.value = false
      serviceToDelete.value = null
    } catch (error) {
      console.error('Error deleting service:', error)
    }
  }
}

function formatPrice(price: number, currency: string): string {
  const symbols: Record<string, string> = {
    USD: '$',
    EUR: '€',
    GBP: '£',
    IRR: '﷼',
  }
  return `${symbols[currency] || currency} ${price.toFixed(2)}`
}

// Form handling functions
function resetForm() {
  serviceForm.name = ''
  serviceForm.duration = 60
  serviceForm.basePrice = 0
  serviceForm.currency = 'USD'
  Object.keys(validationErrors).forEach((key) => delete validationErrors[key])
  formError.value = null
}

function closeServiceModal() {
  showServiceModal.value = false
  resetForm()
  activeMenuId.value = null
}

function validateField(field: keyof typeof serviceForm): boolean {
  delete validationErrors[field]

  switch (field) {
    case 'name':
      if (!serviceForm.name.trim()) {
        validationErrors.name = t('services.errors.nameRequired') || 'Service name is required'
        return false
      }
      if (serviceForm.name.length < 3) {
        validationErrors.name =
          t('services.errors.nameMinLength') || 'Service name must be at least 3 characters'
        return false
      }
      break

    case 'duration':
      if (!serviceForm.duration || serviceForm.duration < 5) {
        validationErrors.duration =
          t('services.errors.durationMin') || 'Duration must be at least 5 minutes'
        return false
      }
      break

    case 'basePrice':
      if (serviceForm.basePrice < 0) {
        validationErrors.basePrice =
          t('services.errors.priceNegative') || 'Price cannot be negative'
        return false
      }
      break
  }

  return true
}

function validateForm(): boolean {
  const fields: Array<keyof typeof serviceForm> = ['name', 'duration', 'basePrice']

  let isValid = true
  fields.forEach((field) => {
    if (!validateField(field)) {
      isValid = false
    }
  })

  return isValid
}

async function handleSubmitForm() {
  formError.value = null

  if (!validateForm()) {
    formError.value =
      t('services.errors.fixValidation') || 'Please fix the validation errors before submitting'
    return
  }

  // Ensure provider is loaded
  if (!providerStore.currentProvider) {
    console.warn('[ServiceForm] Provider not loaded, attempting to load...')
    try {
      await providerStore.loadCurrentProvider()
    } catch (err) {
      console.error('[ServiceForm] Failed to load provider:', err)
    }
  }

  // Check again after attempting to load
  if (!providerStore.currentProvider?.id) {
    console.error(
      '[ServiceForm] No provider found. Current provider:',
      providerStore.currentProvider,
    )
    console.error('[ServiceForm] Auth providerId from token:', authStore.providerId)
    console.error('[ServiceForm] Auth user:', authStore.user)

    if (!authStore.providerId) {
      formError.value =
        'No provider registration found. Please complete provider registration first.'
    } else {
      formError.value =
        'Failed to load provider information. Please try refreshing the page or contact support.'
    }
    return
  }

  console.log('[ServiceForm] Submitting with provider ID:', providerStore.currentProvider.id)
  isSaving.value = true

  try {
    if (isEditMode.value && editingServiceId.value) {
      // Update existing service
      // Get providerId from the service being edited
      const serviceToEdit = serviceStore.serviceById(editingServiceId.value)
      const providerId = serviceToEdit?.providerId || providerStore.currentProvider.id

      const updateData: UpdateServiceRequest = {
        providerId,
        serviceName: serviceForm.name,
        durationHours: Math.floor(serviceForm.duration / 60),
        durationMinutes: serviceForm.duration % 60,
        price: serviceForm.basePrice,
        currency: serviceForm.currency,
      }

      console.log('[ServiceForm] Updating service:', editingServiceId.value, updateData)
      await serviceStore.updateService(editingServiceId.value, updateData)
    } else {
      // Create new service
      const createData: CreateServiceRequest = {
        providerId: providerStore.currentProvider.id,
        serviceName: serviceForm.name,
        durationHours: Math.floor(serviceForm.duration / 60),
        durationMinutes: serviceForm.duration % 60,
        price: serviceForm.basePrice,
        currency: serviceForm.currency,
        // Optional fields
        description: serviceForm.name,
        category: 'HairCare',
      }

      console.log('[ServiceForm] Creating service:', createData)
      await serviceStore.createService(createData)
    }

    closeServiceModal()
  } catch (err: unknown) {
    console.error('[ServiceForm] Error saving service:', err)

    // Handle validation errors
    if (err instanceof Error && (err as any).isValidationError && (err as any).validationErrors) {
      const validationErrs = (err as any).validationErrors as Record<string, string[]>

      // Map backend field names to form field names (case-insensitive)
      const fieldMap: Record<string, keyof typeof serviceForm> = {
        name: 'name',
        servicename: 'name',
        duration: 'duration',
        serviceduration: 'duration',
        price: 'basePrice',
        baseprice: 'basePrice',
        serviceprice: 'basePrice',
      }

      // Clear existing validation errors
      Object.keys(validationErrors).forEach((key) => delete validationErrors[key])

      // Map validation errors to form fields
      let hasFieldError = false
      for (const [field, messages] of Object.entries(validationErrs)) {
        const normalizedField = field.toLowerCase()
        const formField = fieldMap[normalizedField]

        if (formField && Array.isArray(messages) && messages.length > 0) {
          validationErrors[formField] = messages.join(', ')
          hasFieldError = true
        }
      }

      // Set general error message
      if (hasFieldError) {
        formError.value =
          t('services.errors.fixValidation') || 'Please fix the validation errors below'
      } else {
        // If we couldn't map the errors to fields, show them as general error
        formError.value = err.message
      }
    } else {
      // Handle non-validation errors
      formError.value =
        err instanceof Error
          ? err.message
          : isEditMode.value
            ? 'Failed to update service'
            : 'Failed to create service'
    }
  } finally {
    isSaving.value = false
  }
}
</script>

<style scoped lang="scss">
.modern-service-catalog {
  min-height: 100vh;
  background: linear-gradient(to bottom, #f9fafb 0%, #ffffff 100%);
  padding: 2rem;
}

// Header with gradient
.catalog-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 24px;
  padding: 3rem;
  margin-bottom: 2rem;
  box-shadow: 0 20px 60px rgba(102, 126, 234, 0.3);
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 2rem;
}

.header-text {
  flex: 1;
}

.catalog-title {
  font-size: 2.5rem;
  font-weight: 800;
  color: white;
  margin: 0 0 0.5rem 0;
  letter-spacing: -0.02em;
}

.catalog-subtitle {
  font-size: 1.125rem;
  color: rgba(255, 255, 255, 0.9);
  margin: 0;
}

.create-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 1rem 2rem;
  background: white;
  color: #667eea;
  border: none;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 8px 20px rgba(0, 0, 0, 0.15);
  }

  .btn-icon {
    font-size: 1.5rem;
    line-height: 1;
  }
}

// Search Container
.search-container {
  margin-bottom: 2rem;
}

.search-wrapper {
  position: relative;
  max-width: 600px;
  margin: 0 auto;
}

.search-icon {
  position: absolute;
  left: 1.25rem;
  top: 50%;
  transform: translateY(-50%);
  color: #9ca3af;
  pointer-events: none;
}

.search-input {
  width: 100%;
  padding: 1.25rem 3.5rem 1.25rem 3.5rem;
  border: 2px solid #e5e7eb;
  border-radius: 16px;
  font-size: 1rem;
  transition: all 0.2s;
  background: white;

  &:focus {
    outline: none;
    border-color: #667eea;
    box-shadow: 0 0 0 4px rgba(102, 126, 234, 0.1);
  }

  &::placeholder {
    color: #9ca3af;
  }
}

.clear-search {
  position: absolute;
  right: 1rem;
  top: 50%;
  transform: translateY(-50%);
  padding: 0.5rem;
  background: #f3f4f6;
  border: none;
  border-radius: 8px;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: #e5e7eb;
  }
}

// Stats Grid
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.stat-card {
  background: white;
  padding: 2rem;
  border-radius: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  transition: all 0.2s;

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 8px 20px rgba(0, 0, 0, 0.1);
  }
}

.stat-value {
  font-size: 2.5rem;
  font-weight: 700;
  color: #111827;
  line-height: 1;
  margin-bottom: 0.5rem;
}

.stat-label {
  font-size: 0.875rem;
  color: #6b7280;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  font-weight: 600;
}

// Loading State
.loading-state {
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
  to {
    transform: rotate(360deg);
  }
}

// Empty State
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  text-align: center;
}

.empty-icon {
  margin-bottom: 1.5rem;
  opacity: 0.6;
}

.empty-title {
  font-size: 1.5rem;
  font-weight: 600;
  color: #111827;
  margin: 0 0 0.5rem 0;
}

.empty-text {
  font-size: 1rem;
  color: #6b7280;
  margin: 0 0 2rem 0;
  max-width: 400px;
}

.empty-action-btn {
  padding: 0.875rem 2rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 12px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 8px 20px rgba(102, 126, 234, 0.3);
  }
}

// Services Grid
.services-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1.5rem;
}

.service-card {
  position: relative;
  background: white;
  padding: 1.75rem;
  border-radius: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    transform: translateY(-4px);
    box-shadow: 0 12px 28px rgba(0, 0, 0, 0.12);
  }
}

.service-card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1.5rem;
}

.service-name {
  font-size: 1.25rem;
  font-weight: 600;
  color: #111827;
  margin: 0;
  flex: 1;
  line-height: 1.4;
}

.service-menu-btn {
  padding: 0.5rem;
  background: transparent;
  border: none;
  color: #9ca3af;
  cursor: pointer;
  border-radius: 8px;
  transition: all 0.2s;

  &:hover {
    background: #f3f4f6;
    color: #111827;
  }
}

.service-details {
  display: flex;
  gap: 1.5rem;
}

.service-detail-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #6b7280;
  font-size: 0.938rem;
}

.detail-icon {
  color: #9ca3af;
}

.price {
  font-weight: 600;
  color: #10b981;
  font-size: 1.125rem;
}

// Service Menu Dropdown
.service-menu {
  position: absolute;
  top: 3.5rem;
  right: 1rem;
  background: white;
  border-radius: 12px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15);
  padding: 0.5rem;
  z-index: 10;
  min-width: 160px;

  [dir='rtl'] & {
    right: auto;
    left: 1rem;
  }
}

.menu-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
  padding: 0.75rem 1rem;
  background: transparent;
  border: none;
  border-radius: 8px;
  font-size: 0.938rem;
  color: #374151;
  cursor: pointer;
  transition: all 0.2s;
  text-align: left;

  &:hover {
    background: #f3f4f6;
  }

  &.danger {
    color: #ef4444;

    &:hover {
      background: #fee2e2;
    }
  }
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

.service-form-modal {
  max-width: 600px;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.modal-close {
  padding: 0.5rem;
  background: transparent;
  border: none;
  color: #9ca3af;
  cursor: pointer;
  border-radius: 8px;
  transition: all 0.2s;

  &:hover {
    background: #f3f4f6;
    color: #111827;
  }
}

.modal-title {
  font-size: 1.5rem;
  font-weight: 600;
  color: #111827;
  margin: 0;
}

.modal-text {
  font-size: 1rem;
  color: #6b7280;
  margin: 0 0 2rem 0;
  line-height: 1.6;
}

.modal-body {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

// Alert
.alert {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 1rem 1.25rem;
  border-radius: 12px;
  margin-bottom: 1.5rem;

  svg {
    flex-shrink: 0;
    margin-top: 0.125rem;
  }

  div {
    flex: 1;
  }

  strong {
    display: block;
    margin-bottom: 0.25rem;
    font-weight: 600;
  }

  p {
    margin: 0 0 0.75rem 0;
    font-size: 0.938rem;
    line-height: 1.5;
  }

  &-error {
    background: #fee2e2;
    color: #991b1b;
  }

  &-warning {
    background: #fef3c7;
    color: #92400e;
    border: 1px solid #fbbf24;
  }
}

.alert-btn {
  padding: 0.5rem 1rem;
  background: #f59e0b;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: #d97706;
    transform: translateY(-1px);
  }
}

// Form Elements
.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.5rem;
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

.modal-actions {
  display: flex;
  gap: 1rem;
  margin-top: 0.5rem;
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

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }

  &.secondary {
    background: #f3f4f6;
    color: #374151;

    &:hover:not(:disabled) {
      background: #e5e7eb;
    }
  }

  &.primary {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);

    &:hover:not(:disabled) {
      transform: translateY(-2px);
      box-shadow: 0 8px 20px rgba(102, 126, 234, 0.4);
    }
  }

  &.danger {
    background: #ef4444;
    color: white;

    &:hover:not(:disabled) {
      background: #dc2626;
    }
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

// Responsive
@media (max-width: 768px) {
  .modern-service-catalog {
    padding: 1rem;
  }

  .catalog-header {
    padding: 2rem;
    border-radius: 16px;
  }

  .header-content {
    flex-direction: column;
    align-items: stretch;
  }

  .catalog-title {
    font-size: 2rem;
  }

  .create-btn {
    width: 100%;
    justify-content: center;
  }

  .services-grid {
    grid-template-columns: 1fr;
  }

  .stats-grid {
    grid-template-columns: 1fr;
  }

  .service-form-modal {
    max-width: 95%;
  }

  .form-row {
    grid-template-columns: 1fr;
  }

  .modal-actions {
    flex-direction: column-reverse;
  }
}
</style>
