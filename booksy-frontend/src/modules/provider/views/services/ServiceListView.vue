<template>
  <div class="service-list-view">
    <!-- Header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">Services</h1>
        <p class="page-subtitle">Manage your service catalog</p>
      </div>
      <Button variant="primary" @click="handleCreateService">
        + Create Service
      </Button>
    </div>

    <!-- Filters & Search -->
    <Card class="filters-section">
      <div class="filters-row">
        <TextInput
          v-model="localSearchTerm"
          placeholder="Search services..."
          class="search-input"
          @input="handleSearch"
        />

        <Select
          v-model="localSelectedCategory"
          :options="categoryOptions"
          placeholder="All Categories"
          class="filter-select"
          @update:model-value="handleCategoryChange"
        />

        <Select
          v-model="localSelectedStatus"
          :options="statusOptions"
          placeholder="All Statuses"
          class="filter-select"
          @update:model-value="handleStatusChange"
        />

        <Button
          v-if="serviceStore.hasActiveFilters"
          variant="secondary"
          size="small"
          @click="handleClearFilters"
        >
          Clear Filters
        </Button>
      </div>
    </Card>

    <!-- Loading State -->
    <div v-if="serviceStore.isLoading" class="loading-state">
      <Spinner />
      <p>Loading services...</p>
    </div>

    <!-- Error State -->
    <Alert
      v-if="serviceStore.error"
      type="error"
      :message="serviceStore.error"
      @dismiss="serviceStore.clearError()"
    />

    <!-- Success Message -->
    <Alert
      v-if="serviceStore.successMessage"
      type="success"
      :message="serviceStore.successMessage"
      @dismiss="serviceStore.clearSuccess()"
    />

    <!-- Empty State -->
    <EmptyState
      v-if="!serviceStore.isLoading && !serviceStore.hasServices"
      title="No services yet"
      description="Create your first service to start accepting bookings"
      icon="📦"
    >
      <template #actions>
        <Button variant="primary" @click="handleCreateService">
          + Create Your First Service
        </Button>
      </template>
    </EmptyState>

    <!-- Empty Filtered State -->
    <EmptyState
      v-else-if="!serviceStore.isLoading && serviceStore.filteredServices.length === 0"
      title="No services found"
      description="Try adjusting your filters or search term"
      icon="🔍"
      size="small"
    >
      <template #actions>
        <Button variant="secondary" @click="handleClearFilters">
          Clear Filters
        </Button>
      </template>
    </EmptyState>

    <!-- Service Grid -->
    <div v-else-if="!serviceStore.isLoading" class="services-grid">
      <ServiceCard
        v-for="service in serviceStore.filteredServices"
        :key="service.id"
        :service="service"
        @click="handleServiceClick"
        @edit="handleEditService"
        @delete="handleDeleteService"
        @activate="handleActivateService"
        @deactivate="handleDeactivateService"
      />
    </div>

    <!-- Delete Confirmation Modal -->
    <ConfirmModal
      v-if="showDeleteConfirm"
      :is-open="showDeleteConfirm"
      title="Delete Service"
      message="Are you sure you want to delete this service? This action cannot be undone."
      confirm-text="Delete"
      cancel-text="Cancel"
      @confirm="confirmDelete"
      @cancel="cancelDelete"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useServiceStore } from '../../stores/service.store'
import { useProviderStore } from '../../stores/provider.store'
import { Button, TextInput, Select, Card, Alert, Spinner, EmptyState, ConfirmModal } from '@/shared/components'
import ServiceCard from '../../components/services/ServiceCard.vue'
import { SERVICE_CATEGORY_LABELS, SERVICE_STATUS_LABELS, ServiceCategory, ServiceStatus } from '../../types/service.types'

const router = useRouter()
const serviceStore = useServiceStore()
const providerStore = useProviderStore()

// Local state for filters
const localSearchTerm = ref('')
const localSelectedCategory = ref<ServiceCategory | undefined>(undefined)
const localSelectedStatus = ref<ServiceStatus | undefined>(undefined)

// Delete confirmation
const showDeleteConfirm = ref(false)
const serviceToDelete = ref<string | null>(null)

// Filter options
const categoryOptions = Object.entries(SERVICE_CATEGORY_LABELS).map(([value, label]) => ({
  value,
  label,
}))

const statusOptions = Object.entries(SERVICE_STATUS_LABELS).map(([value, label]) => ({
  value,
  label,
}))

// Lifecycle
onMounted(async () => {
  await loadServices()
})

// Methods
async function loadServices() {
  try {
    if (!providerStore.currentProvider) {
      await providerStore.loadCurrentProvider()
    }

    const providerId = providerStore.currentProvider?.id
    if (providerId) {
      await serviceStore.loadServices(providerId)
    }
  } catch (error) {
    console.error('Error loading services:', error)
  }
}

function handleSearch() {
  serviceStore.setSearchTerm(localSearchTerm.value)
}

function handleCategoryChange() {
  serviceStore.setCategory(localSelectedCategory.value)
}

function handleStatusChange() {
  serviceStore.setStatus(localSelectedStatus.value)
}

function handleClearFilters() {
  localSearchTerm.value = ''
  localSelectedCategory.value = undefined
  localSelectedStatus.value = undefined
  serviceStore.clearFilters()
}

function handleCreateService() {
  router.push({ name: 'ServiceEditor', params: { mode: 'create' } })
}

function handleServiceClick(service: any) {
  router.push({ name: 'ServiceEditor', params: { id: service.id, mode: 'edit' } })
}

function handleEditService(serviceId: string) {
  router.push({ name: 'ServiceEditor', params: { id: serviceId, mode: 'edit' } })
}

function handleDeleteService(serviceId: string) {
  serviceToDelete.value = serviceId
  showDeleteConfirm.value = true
}

async function confirmDelete() {
  if (serviceToDelete.value) {
    try {
      await serviceStore.deleteService(serviceToDelete.value)
    } catch (error) {
      console.error('Error deleting service:', error)
    } finally {
      showDeleteConfirm.value = false
      serviceToDelete.value = null
    }
  }
}

function cancelDelete() {
  showDeleteConfirm.value = false
  serviceToDelete.value = null
}

async function handleActivateService(serviceId: string) {
  try {
    await serviceStore.activateService(serviceId)
  } catch (error) {
    console.error('Error activating service:', error)
  }
}

async function handleDeactivateService(serviceId: string) {
  try {
    await serviceStore.deactivateService(serviceId)
  } catch (error) {
    console.error('Error deactivating service:', error)
  }
}
</script>

<style scoped lang="scss">
.service-list-view {
  max-width: 1400px;
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

.filters-section {
  margin-bottom: 2rem;
}

.filters-row {
  display: flex;
  gap: 1rem;
  align-items: center;
  flex-wrap: wrap;
}

.search-input {
  flex: 1;
  min-width: 200px;
}

.filter-select {
  min-width: 180px;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  gap: 1rem;
}

.services-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

@media (max-width: 768px) {
  .service-list-view {
    padding: 1rem;
  }

  .page-header {
    flex-direction: column;
    align-items: stretch;
  }

  .filters-row {
    flex-direction: column;
    align-items: stretch;

    .search-input,
    .filter-select {
      width: 100%;
    }
  }

  .services-grid {
    grid-template-columns: 1fr;
  }
}
</style>
