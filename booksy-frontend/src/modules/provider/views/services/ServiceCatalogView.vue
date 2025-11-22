<template>
  <div class="service-catalog-view">
    <!-- Header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">Service Catalog</h1>
        <p class="page-subtitle">
          Create and manage the services you offer to your customers
        </p>
      </div>
      <Button variant="secondary" @click="goBack">← Back to Onboarding</Button>
    </div>

    <!-- Loading State -->
    <div v-if="providerStore.isLoading" class="loading-state">
      <Spinner />
      <p>Loading service catalog...</p>
    </div>

    <!-- Error State -->
    <Alert
      v-if="errorMessage"
      type="error"
      :message="errorMessage"
      @dismiss="errorMessage = null"
    />

    <!-- Success Message -->
    <Alert
      v-if="successMessage"
      type="success"
      :message="successMessage"
      @dismiss="successMessage = null"
    />

    <!-- Content -->
    <div v-if="!providerStore.isLoading" class="catalog-content">
      <!-- Actions Bar -->
      <div class="actions-bar">
        <div class="search-filter">
          <input
            type="text"
            v-model="searchTerm"
            placeholder="Search services..."
            class="search-input"
          />
          <select v-model="categoryFilter" class="filter-select">
            <option value="">All Categories</option>
            <option v-for="cat in categories" :key="cat" :value="cat">{{ cat }}</option>
          </select>
          <Button nativeType="button" variant="secondary" size="small" @click="resetFilters">
            Reset Filters
          </Button>
        </div>
        <Button nativeType="button" variant="primary" @click="openAddServiceModal">
          + Add New Service
        </Button>
      </div>

      <!-- Service List -->
      <div v-if="filteredServices.length > 0" class="service-list">
        <Card v-for="service in filteredServices" :key="service.id" class="service-card">
          <div class="service-card-content">
            <div class="service-image">
              <img
                :src="service.imageUrl || '/placeholder-service.png'"
                :alt="service.name"
                class="service-thumbnail"
              />
            </div>
            <div class="service-details">
              <div class="service-header">
                <h3 class="service-name">{{ service.name }}</h3>
                <div class="service-status" :class="service.status.toLowerCase()">
                  {{ service.status }}
                </div>
              </div>
              <div class="service-meta">
                <div class="service-category">{{ service.category }}</div>
                <div class="service-price">
                  <strong>{{ formatCurrency(service.basePrice, service.currency) }}</strong>
                </div>
                <div class="service-duration">{{ formatDuration(service.duration) }}</div>
              </div>
              <p class="service-description">{{ service.description }}</p>
              <div class="service-tags">
                <span v-for="(tag, index) in service.tags" :key="index" class="tag">
                  {{ tag }}
                </span>
              </div>
            </div>
          </div>
          <div class="service-actions">
            <Button
              nativeType="button"
              variant="secondary"
              size="small"
              @click="editService(service)"
            >
              Edit
            </Button>
            <Button
              nativeType="button"
              variant="danger"
              size="small"
              @click="confirmDeleteService(service)"
            >
              Delete
            </Button>
          </div>
        </Card>
      </div>

      <!-- Empty State -->
      <div v-else-if="services.length === 0" class="empty-state">
        <div class="empty-icon">📋</div>
        <h3>No Services Yet</h3>
        <p>Your service catalog is empty. Start by adding your first service.</p>
        <Button nativeType="button" variant="primary" @click="openAddServiceModal">
          + Add Your First Service
        </Button>
      </div>

      <!-- No Results -->
      <div v-else class="empty-state">
        <div class="empty-icon">🔍</div>
        <h3>No Matching Services</h3>
        <p>No services match your current filters. Try adjusting your search criteria.</p>
        <Button nativeType="button" variant="secondary" @click="resetFilters">
          Reset Filters
        </Button>
      </div>
    </div>

    <!-- Service Form Modal -->
    <Modal
      v-if="showServiceModal"
      :modelValue="showServiceModal"
      :title="isEditing ? 'Edit Service' : 'Add New Service'"
      @update:modelValue="closeServiceModal"
    >
      <form class="service-form" @submit.prevent="saveService">
        <div class="form-grid">
          <div class="form-group full-width">
            <label for="serviceName">Service Name *</label>
            <input
              id="serviceName"
              type="text"
              v-model="serviceForm.name"
              placeholder="e.g., Haircut, Massage, Consultation"
              required
              class="form-input"
              :class="{ 'has-error': validationErrors.name }"
            />
            <div v-if="validationErrors.name" class="error-message">
              {{ validationErrors.name }}
            </div>
          </div>

          <div class="form-group">
            <label for="serviceCategory">Category *</label>
            <select
              id="serviceCategory"
              v-model="serviceForm.category"
              class="form-select"
              required
              :class="{ 'has-error': validationErrors.category }"
            >
              <option value="" disabled>Select a category</option>
              <option v-for="cat in categories" :key="cat" :value="cat">{{ cat }}</option>
              <option value="custom">+ Add New Category</option>
            </select>
            <div v-if="validationErrors.category" class="error-message">
              {{ validationErrors.category }}
            </div>
          </div>

          <div v-if="serviceForm.category === 'custom'" class="form-group">
            <label for="newCategory">New Category Name *</label>
            <input
              id="newCategory"
              type="text"
              v-model="newCategoryName"
              placeholder="Enter new category name"
              required
              class="form-input"
            />
          </div>

          <div class="form-group">
            <label for="serviceType">Service Type</label>
            <select id="serviceType" v-model="serviceForm.type" class="form-select">
              <option value="Standard">Standard</option>
              <option value="Package">Package</option>
              <option value="Addon">Add-on</option>
              <option value="Membership">Membership</option>
            </select>
          </div>

          <div class="form-group full-width">
            <label for="serviceDescription">Description *</label>
            <textarea
              id="serviceDescription"
              v-model="serviceForm.description"
              rows="3"
              placeholder="Describe your service in detail"
              required
              class="form-textarea"
              :class="{ 'has-error': validationErrors.description }"
            ></textarea>
            <div v-if="validationErrors.description" class="error-message">
              {{ validationErrors.description }}
            </div>
          </div>

          <div class="form-group">
            <label for="servicePrice">Price *</label>
            <div class="input-with-prefix">
              <span class="input-prefix">$</span>
              <input
                id="servicePrice"
                type="number"
                v-model.number="serviceForm.basePrice"
                min="0"
                step="0.01"
                required
                class="form-input price-input"
                :class="{ 'has-error': validationErrors.basePrice }"
              />
            </div>
            <div v-if="validationErrors.basePrice" class="error-message">
              {{ validationErrors.basePrice }}
            </div>
          </div>

          <div class="form-group">
            <label for="serviceDuration">Duration (minutes) *</label>
            <input
              id="serviceDuration"
              type="number"
              v-model.number="serviceForm.duration"
              min="1"
              required
              class="form-input"
              :class="{ 'has-error': validationErrors.duration }"
            />
            <div v-if="validationErrors.duration" class="error-message">
              {{ validationErrors.duration }}
            </div>
          </div>

          <div class="form-group">
            <label for="serviceCurrency">Currency</label>
            <select id="serviceCurrency" v-model="serviceForm.currency" class="form-select">
              <option value="USD">USD - US Dollar</option>
              <option value="EUR">EUR - Euro</option>
              <option value="GBP">GBP - British Pound</option>
              <option value="CAD">CAD - Canadian Dollar</option>
              <option value="AUD">AUD - Australian Dollar</option>
            </select>
          </div>

          <div class="form-group">
            <label for="serviceStatus">Status</label>
            <select id="serviceStatus" v-model="serviceForm.status" class="form-select">
              <option value="Active">Active</option>
              <option value="Inactive">Inactive</option>
              <option value="Seasonal">Seasonal</option>
            </select>
          </div>

          <div class="form-group full-width">
            <label for="serviceImage">Image URL</label>
            <input
              id="serviceImage"
              type="url"
              v-model="serviceForm.imageUrl"
              placeholder="https://example.com/image.jpg"
              class="form-input"
            />
            <small>URL to an image for this service</small>
          </div>

          <div class="form-group full-width">
            <label for="serviceTags">Tags</label>
            <input
              id="serviceTags"
              type="text"
              v-model="tagsInput"
              placeholder="e.g., hair, style, premium (comma separated)"
              class="form-input"
            />
            <small>Comma-separated tags to help customers find your service</small>
          </div>
        </div>

        <div class="modal-actions">
          <Button nativeType="button" variant="secondary" @click="closeServiceModal">
            Cancel
          </Button>
          <Button nativeType="submit" variant="primary" :disabled="isSaving">
            {{ isSaving ? 'Saving...' : (isEditing ? 'Update Service' : 'Add Service') }}
          </Button>
        </div>
      </form>
    </Modal>

    <!-- Confirmation Modal -->
    <ConfirmModal
      v-if="showDeleteModal"
      title="Delete Service"
      :message="`Are you sure you want to delete '${selectedService?.name}'? This action cannot be undone.`"
      confirmLabel="Delete"
      confirmVariant="danger"
      @confirm="deleteService"
      @cancel="cancelDelete"
    />

    <!-- Form Actions -->
    <div class="form-actions">
      <Button type="button" variant="secondary" @click="goBack">Cancel</Button>
      <Button type="button" variant="primary" @click="finishAndGoBack">
        Finish and Go Back
      </Button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import { ServiceSummary } from '@/modules/provider/types/provider.types'
import { Button, Card, Modal, ConfirmModal, Alert, Spinner } from '@/shared/components'

const router = useRouter()
const providerStore = useProviderStore()

// State
const isSaving = ref(false)
const errorMessage = ref<string | null>(null)
const successMessage = ref<string | null>(null)
const services = ref<ServiceSummary[]>([])
const searchTerm = ref('')
const categoryFilter = ref('')

// Service modal state
const showServiceModal = ref(false)
const isEditing = ref(false)
const serviceForm = reactive({
  id: '',
  providerId: '',
  name: '',
  description: '',
  category: '',
  type: 'Standard',
  basePrice: 0,
  currency: 'USD',
  duration: 30,
  status: 'Active',
  imageUrl: '',
  tags: [] as string[],
})
const newCategoryName = ref('')
const tagsInput = ref('')

// Delete confirmation modal
const showDeleteModal = ref(false)
const selectedService = ref<ServiceSummary | null>(null)

// Validation
const validationErrors = reactive<Record<string, string>>({})

// Computed
const categories = computed(() => {
  // Extract all unique categories from services
  const categorySet = new Set<string>()
  services.value.forEach((service) => {
    if (service.category) {
      categorySet.add(service.category)
    }
  })
  return Array.from(categorySet)
})

const filteredServices = computed(() => {
  return services.value.filter((service) => {
    // Filter by search term
    const searchLower = searchTerm.value.toLowerCase()
    const matchesSearch =
      searchTerm.value === '' ||
      service.name.toLowerCase().includes(searchLower) ||
      service.description.toLowerCase().includes(searchLower) ||
      service.tags.some((tag) => tag.toLowerCase().includes(searchLower))

    // Filter by category
    const matchesCategory = categoryFilter.value === '' || service.category === categoryFilter.value

    return matchesSearch && matchesCategory
  })
})

// Load services
onMounted(async () => {
  try {
    if (!providerStore.currentProvider) {
      await providerStore.loadCurrentProvider()
    }

    const provider = providerStore.currentProvider
    if (!provider) {
      errorMessage.value = 'Provider profile not found. Please register as a provider first.'
      return
    }

    // Load services
    services.value = provider.services || []
  } catch (error) {
    console.error('Error loading services:', error)
    errorMessage.value = 'Failed to load services. Please try again.'
  }
})

// Format helpers
function formatCurrency(amount: number, currency: string = 'USD'): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(amount)
}

function formatDuration(minutes: number): string {
  if (minutes < 60) {
    return `${minutes} min`
  }
  const hours = Math.floor(minutes / 60)
  const mins = minutes % 60
  if (mins === 0) {
    return `${hours} hr${hours > 1 ? 's' : ''}`
  }
  return `${hours} hr${hours > 1 ? 's' : ''} ${mins} min`
}

// Filter actions
function resetFilters() {
  searchTerm.value = ''
  categoryFilter.value = ''
}

// Service Modal
function openAddServiceModal() {
  isEditing.value = false
  resetServiceForm()
  showServiceModal.value = true
}

function editService(service: ServiceSummary) {
  isEditing.value = true
  resetServiceForm()

  // Fill form with service data
  serviceForm.id = service.id
  serviceForm.providerId = service.providerId
  serviceForm.name = service.name
  serviceForm.description = service.description
  serviceForm.category = service.category
  serviceForm.type = service.type
  serviceForm.basePrice = service.basePrice
  serviceForm.currency = service.currency
  serviceForm.duration = service.duration
  serviceForm.status = service.status
  serviceForm.imageUrl = service.imageUrl || ''
  serviceForm.tags = [...service.tags]

  // Convert tags array to comma-separated string
  tagsInput.value = service.tags.join(', ')

  showServiceModal.value = true
}

function closeServiceModal() {
  showServiceModal.value = false
  resetServiceForm()
}

function resetServiceForm() {
  serviceForm.id = ''
  serviceForm.providerId = ''
  serviceForm.name = ''
  serviceForm.description = ''
  serviceForm.category = ''
  serviceForm.type = 'Standard'
  serviceForm.basePrice = 0
  serviceForm.currency = 'USD'
  serviceForm.duration = 30
  serviceForm.status = 'Active'
  serviceForm.imageUrl = ''
  serviceForm.tags = []
  tagsInput.value = ''
  newCategoryName.value = ''
  validationErrors.name = ''
  validationErrors.description = ''
  validationErrors.category = ''
  validationErrors.basePrice = ''
  validationErrors.duration = ''
}

// Validate service form
function validateServiceForm(): boolean {
  let isValid = true

  // Clear previous errors
  Object.keys(validationErrors).forEach(key => {
    validationErrors[key] = ''
  })

  // Validate name
  if (!serviceForm.name.trim()) {
    validationErrors.name = 'Service name is required'
    isValid = false
  }

  // Validate description
  if (!serviceForm.description.trim()) {
    validationErrors.description = 'Description is required'
    isValid = false
  } else if (serviceForm.description.length < 10) {
    validationErrors.description = 'Description should be at least 10 characters'
    isValid = false
  }

  // Validate category
  if (!serviceForm.category) {
    validationErrors.category = 'Category is required'
    isValid = false
  } else if (serviceForm.category === 'custom' && !newCategoryName.value.trim()) {
    validationErrors.category = 'Please enter a new category name'
    isValid = false
  }

  // Validate price
  if (serviceForm.basePrice <= 0) {
    validationErrors.basePrice = 'Price must be greater than 0'
    isValid = false
  }

  // Validate duration
  if (serviceForm.duration <= 0) {
    validationErrors.duration = 'Duration must be greater than 0'
    isValid = false
  }

  return isValid
}

// Save service
async function saveService() {
  // If category is 'custom', use the newCategoryName
  if (serviceForm.category === 'custom') {
    serviceForm.category = newCategoryName.value.trim()
  }

  // Validate form
  if (!validateServiceForm()) {
    return
  }

  // Parse tags from comma-separated string
  serviceForm.tags = tagsInput.value
    .split(',')
    .map(tag => tag.trim())
    .filter(tag => tag !== '')

  isSaving.value = true

  try {
    const provider = providerStore.currentProvider
    if (!provider) {
      throw new Error('Provider not found. Please try refreshing the page.')
    }

    if (isEditing.value) {
      // Update existing service
      const existingIndex = services.value.findIndex(s => s.id === serviceForm.id)
      if (existingIndex >= 0) {
        const updatedService = { ...services.value[existingIndex], ...serviceForm }
        services.value.splice(existingIndex, 1, updatedService)
      }
    } else {
      // Add new service
      // In a real implementation, the backend would generate the ID
      const newId = `temp-${Date.now()}`
      const newService: ServiceSummary = {
        id: newId,
        providerId: provider.id,
        name: serviceForm.name,
        description: serviceForm.description,
        category: serviceForm.category,
        type: serviceForm.type,
        basePrice: serviceForm.basePrice,
        currency: serviceForm.currency,
        duration: serviceForm.duration,
        status: serviceForm.status,
        imageUrl: serviceForm.imageUrl,
        tags: serviceForm.tags,
      }
      services.value.push(newService)
    }

    // Update provider with new services array
    const updateData = {
      services: services.value,
    }

    const updatedProvider = await providerStore.updateProvider(provider.id, updateData)

    // Check if update was successful
    if (!updatedProvider) {
      // Check if there's an error in the store
      if (providerStore.error) {
        throw new Error(providerStore.error)
      }
      throw new Error('Failed to save service. Please try again.')
    }

    successMessage.value = isEditing.value ? 'Service updated successfully!' : 'Service added successfully!'
    closeServiceModal()

  } catch (error) {
    console.error('Error saving service:', error)
    errorMessage.value = error instanceof Error ? error.message : 'Failed to save service'
  } finally {
    isSaving.value = false
  }
}

// Delete service
function confirmDeleteService(service: ServiceSummary) {
  selectedService.value = service
  showDeleteModal.value = true
}

function cancelDelete() {
  selectedService.value = null
  showDeleteModal.value = false
}

async function deleteService() {
  if (!selectedService.value) return

  isSaving.value = true

  try {
    const provider = providerStore.currentProvider
    if (!provider) {
      throw new Error('Provider not found. Please try refreshing the page.')
    }

    // Remove service from services array
    services.value = services.value.filter(s => s.id !== selectedService.value?.id)

    // Update provider with new services array
    const updateData = {
      services: services.value,
    }

    const updatedProvider = await providerStore.updateProvider(provider.id, updateData)

    // Check if update was successful
    if (!updatedProvider) {
      // Check if there's an error in the store
      if (providerStore.error) {
        throw new Error(providerStore.error)
      }
      throw new Error('Failed to delete service. Please try again.')
    }

    successMessage.value = 'Service deleted successfully!'

  } catch (error) {
    console.error('Error deleting service:', error)
    errorMessage.value = error instanceof Error ? error.message : 'Failed to delete service'

    // Restore services from provider if there was an error
    if (providerStore.currentProvider?.services) {
      services.value = providerStore.currentProvider.services
    }
  } finally {
    isSaving.value = false
    selectedService.value = null
    showDeleteModal.value = false
  }
}

// Navigation
function goBack() {
  router.push({ name: 'ProviderRegistration' })
}

function finishAndGoBack() {
  successMessage.value = 'Service catalog saved successfully!'
  setTimeout(() => {
    router.push({ name: 'ProviderRegistration' })
  }, 1500)
}
</script>

<style scoped>
.service-catalog-view {
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

.catalog-content {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.actions-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
  margin-bottom: 1rem;
}

.search-filter {
  display: flex;
  gap: 1rem;
  align-items: center;
  flex-wrap: wrap;
  flex: 1;
}

.search-input {
  flex: 1;
  min-width: 200px;
  padding: 0.625rem 1rem;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  font-size: 0.875rem;
}

.filter-select {
  padding: 0.625rem;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  min-width: 150px;
}

.service-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.service-card {
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.service-card-content {
  display: flex;
  gap: 1.5rem;
}

.service-image {
  flex-shrink: 0;
  width: 100px;
  height: 100px;
  border-radius: 0.375rem;
  overflow: hidden;
  background-color: #f3f4f6;
  display: flex;
  align-items: center;
  justify-content: center;
}

.service-thumbnail {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.service-details {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.service-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
}

.service-name {
  font-size: 1.125rem;
  font-weight: 600;
  margin: 0;
  color: #111827;
}

.service-status {
  font-size: 0.75rem;
  font-weight: 600;
  padding: 0.25rem 0.5rem;
  border-radius: 9999px;
  text-transform: uppercase;
}

.service-status.active {
  background-color: #d1fae5;
  color: #065f46;
}

.service-status.inactive {
  background-color: #f3f4f6;
  color: #6b7280;
}

.service-status.seasonal {
  background-color: #dbeafe;
  color: #1e40af;
}

.service-meta {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 0.5rem;
}

.service-category {
  font-size: 0.875rem;
  color: #6b7280;
  padding: 0.125rem 0.5rem;
  background-color: #f3f4f6;
  border-radius: 0.25rem;
}

.service-price {
  font-size: 0.875rem;
  color: #111827;
}

.service-duration {
  font-size: 0.875rem;
  color: #6b7280;
}

.service-description {
  font-size: 0.875rem;
  color: #4b5563;
  margin: 0;
  line-height: 1.5;
}

.service-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-top: 0.5rem;
}

.tag {
  font-size: 0.75rem;
  color: #4b5563;
  background-color: #f3f4f6;
  padding: 0.125rem 0.5rem;
  border-radius: 9999px;
}

.service-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  padding-top: 1rem;
  border-top: 1px solid #e5e7eb;
}

.empty-state {
  text-align: center;
  padding: 4rem 2rem;
  background-color: #f9fafb;
  border-radius: 0.5rem;
  border: 1px dashed #d1d5db;
}

.empty-icon {
  font-size: 3rem;
  margin-bottom: 1rem;
}

.empty-state h3 {
  font-size: 1.25rem;
  font-weight: 600;
  margin: 0 0 0.5rem 0;
  color: #111827;
}

.empty-state p {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0 0 1.5rem 0;
}

/* Service Form */
.service-form {
  padding: 1rem 0;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1.5rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group.full-width {
  grid-column: 1 / -1;
}

.form-group label {
  font-size: 0.875rem;
  font-weight: 600;
  color: #111827;
}

.form-group small {
  font-size: 0.75rem;
  color: #6b7280;
}

.form-input,
.form-select,
.form-textarea {
  padding: 0.625rem;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  color: #111827;
}

.form-input:focus,
.form-select:focus,
.form-textarea:focus {
  border-color: #8b5cf6;
  outline: none;
  box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
}

.form-input.has-error,
.form-select.has-error,
.form-textarea.has-error {
  border-color: #ef4444;
}

.input-with-prefix {
  display: flex;
  align-items: stretch;
}

.input-prefix {
  display: flex;
  align-items: center;
  padding: 0 0.75rem;
  background-color: #f3f4f6;
  border: 1px solid #d1d5db;
  border-right: none;
  border-radius: 0.375rem 0 0 0.375rem;
  font-size: 0.875rem;
  color: #6b7280;
}

.price-input {
  border-top-left-radius: 0;
  border-bottom-left-radius: 0;
}

.error-message {
  font-size: 0.75rem;
  color: #ef4444;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  margin-top: 2rem;
}

/* Form Actions */
.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  padding-top: 2rem;
  border-top: 1px solid #e5e7eb;
  margin-top: 2rem;
}

@media (max-width: 768px) {
  .service-catalog-view {
    padding: 1rem;
  }

  .page-header {
    flex-direction: column;
  }

  .actions-bar {
    flex-direction: column;
    align-items: stretch;
  }

  .search-filter {
    flex-direction: column;
    align-items: stretch;
  }

  .service-card-content {
    flex-direction: column;
  }

  .service-image {
    width: 100%;
    height: 150px;
  }

  .form-grid {
    grid-template-columns: 1fr;
  }

  .modal-actions,
  .form-actions {
    flex-direction: column;
  }

  .modal-actions button,
  .form-actions button {
    width: 100%;
  }
}
</style>