<template>
  <div class="staff-list-view">
    <!-- Header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">Staff</h1>
        <p class="page-subtitle">Manage your team members</p>
      </div>
      <Button variant="primary" @click="handleCreateStaff">
        + Add Staff Member
      </Button>
    </div>

    <!-- Search -->
    <Card class="filters-section">
      <div class="filters-row">
        <TextInput
          v-model="localSearchTerm"
          placeholder="Search staff..."
          class="search-input"
          @input="handleSearch"
        />

        <Button
          v-if="staffStore.filters.searchTerm"
          variant="secondary"
          size="small"
          @click="handleClearFilters"
        >
          Clear Search
        </Button>
      </div>
    </Card>

    <!-- Loading State -->
    <div v-if="staffStore.isLoading" class="loading-state">
      <Spinner />
      <p>Loading staff...</p>
    </div>

    <!-- Error State -->
    <Alert
      v-if="staffStore.error"
      type="error"
      :message="staffStore.error"
      @dismiss="staffStore.clearError()"
    />

    <!-- Empty State -->
    <EmptyState
      v-if="!staffStore.isLoading && !staffStore.hasStaff"
      title="No staff members yet"
      description="Add your first team member to get started"
      icon="👥"
    >
      <template #actions>
        <Button variant="primary" @click="handleCreateStaff">
          + Add Your First Staff Member
        </Button>
      </template>
    </EmptyState>

    <!-- Empty Filtered State -->
    <EmptyState
      v-else-if="!staffStore.isLoading && staffStore.filteredStaff.length === 0"
      title="No staff members found"
      description="Try adjusting your search term"
      icon="🔍"
      size="small"
    >
      <template #actions>
        <Button variant="secondary" @click="handleClearFilters">
          Clear Search
        </Button>
      </template>
    </EmptyState>

    <!-- Staff Grid -->
    <div v-else-if="!staffStore.isLoading" class="staff-grid">
      <StaffCard
        v-for="staff in staffStore.filteredStaff"
        :key="staff.id"
        :staff="staff"
        @click="handleStaffClick"
        @edit="handleEditStaff"
        @delete="handleDeleteStaff"
      />
    </div>

    <!-- Staff Editor Modal -->
    <Modal
      v-if="showStaffModal"
      :model-value="showStaffModal"
      :title="isEditMode ? 'Edit Staff Member' : 'Add Staff Member'"
      @update:model-value="closeStaffModal"
    >
      <form @submit.prevent="handleSaveStaff" class="staff-form">
        <div class="form-group">
          <label for="firstName" class="form-label required">First Name</label>
          <TextInput
            id="firstName"
            v-model="staffFormData.firstName"
            placeholder="Enter first name"
            :error="validationErrors.firstName"
            required
          />
          <span v-if="validationErrors.firstName" class="error-message">{{ validationErrors.firstName }}</span>
        </div>

        <div class="form-group">
          <label for="lastName" class="form-label required">Last Name</label>
          <TextInput
            id="lastName"
            v-model="staffFormData.lastName"
            placeholder="Enter last name"
            :error="validationErrors.lastName"
            required
          />
          <span v-if="validationErrors.lastName" class="error-message">{{ validationErrors.lastName }}</span>
        </div>

        <div class="form-group">
          <label for="phoneNumber" class="form-label">Phone Number</label>
          <TextInput
            id="phoneNumber"
            v-model="staffFormData.phoneNumber"
            type="tel"
            placeholder="+1 (555) 000-0000"
            :error="validationErrors.phoneNumber"
          />
          <span v-if="validationErrors.phoneNumber" class="error-message">{{ validationErrors.phoneNumber }}</span>
        </div>

        <div class="form-actions">
          <Button type="button" variant="secondary" @click="closeStaffModal" :disabled="staffStore.isCreating || staffStore.isUpdating">
            Cancel
          </Button>
          <Button type="submit" variant="primary" :loading="staffStore.isCreating || staffStore.isUpdating" :disabled="staffStore.isCreating || staffStore.isUpdating">
            {{ isEditMode ? 'Update' : 'Add' }} Staff Member
          </Button>
        </div>
      </form>
    </Modal>

    <!-- Delete Confirmation Modal -->
    <ConfirmModal
      v-if="showDeleteConfirm"
      :is-open="showDeleteConfirm"
      title="Remove Staff Member"
      message="Are you sure you want to remove this staff member? This action cannot be undone."
      confirm-text="Remove"
      cancel-text="Cancel"
      @confirm="confirmDelete"
      @cancel="cancelDelete"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useStaffStore } from '../../stores/staff.store'
import { useProviderStore } from '../../stores/provider.store'
import { Button, TextInput, Card, Alert, Spinner, EmptyState, ConfirmModal, Modal } from '@/shared/components'
import StaffCard from '../../components/staff/StaffCard.vue'
import type { CreateStaffRequest, UpdateStaffRequest } from '../../types/staff.types'

const staffStore = useStaffStore()
const providerStore = useProviderStore()

// Local state for filters
const localSearchTerm = ref('')

// Staff modal state
const showStaffModal = ref(false)
const isEditMode = ref(false)
const editingStaffId = ref<string | null>(null)

const staffFormData = reactive<CreateStaffRequest>({
  firstName: '',
  lastName: '',
  phoneNumber: '',
})

const validationErrors = reactive<Record<string, string>>({})

// Delete confirmation
const showDeleteConfirm = ref(false)
const staffToDelete = ref<string | null>(null)

// Lifecycle
onMounted(async () => {
  await loadStaff()
})

// Methods
async function loadStaff() {
  try {
    if (!providerStore.currentProvider) {
      await providerStore.loadCurrentProvider()
    }

    const providerId = providerStore.currentProvider?.id
    if (providerId) {
      await staffStore.loadStaffByProvider(providerId)
    }
  } catch (error) {
    console.error('Error loading staff:', error)
  }
}

function handleSearch() {
  staffStore.setSearchTerm(localSearchTerm.value)
}

function handleClearFilters() {
  localSearchTerm.value = ''
  staffStore.clearFilters()
}

function handleCreateStaff() {
  isEditMode.value = false
  editingStaffId.value = null
  staffFormData.firstName = ''
  staffFormData.lastName = ''
  staffFormData.phoneNumber = ''
  Object.keys(validationErrors).forEach(key => delete validationErrors[key])
  showStaffModal.value = true
}

function handleStaffClick(staff: any) {
  handleEditStaff(staff.id)
}

function handleEditStaff(staffId: string) {
  const staff = staffStore.staff.find(s => s.id === staffId)
  if (staff) {
    isEditMode.value = true
    editingStaffId.value = staffId
    staffFormData.firstName = staff.firstName
    staffFormData.lastName = staff.lastName
    staffFormData.phoneNumber = staff.phoneNumber || ''
    Object.keys(validationErrors).forEach(key => delete validationErrors[key])
    showStaffModal.value = true
  }
}

function closeStaffModal() {
  showStaffModal.value = false
  isEditMode.value = false
  editingStaffId.value = null
  staffFormData.firstName = ''
  staffFormData.lastName = ''
  staffFormData.phoneNumber = ''
  Object.keys(validationErrors).forEach(key => delete validationErrors[key])
}

function validateForm(): boolean {
  Object.keys(validationErrors).forEach(key => delete validationErrors[key])
  let isValid = true

  if (!staffFormData.firstName.trim()) {
    validationErrors.firstName = 'First name is required'
    isValid = false
  }

  if (!staffFormData.lastName.trim()) {
    validationErrors.lastName = 'Last name is required'
    isValid = false
  }

  return isValid
}

async function handleSaveStaff() {
  if (!validateForm()) {
    return
  }

  if (!providerStore.currentProvider?.id) {
    return
  }

  try {
    if (isEditMode.value && editingStaffId.value) {
      const updateData: UpdateStaffRequest = {
        firstName: staffFormData.firstName,
        lastName: staffFormData.lastName,
        phoneNumber: staffFormData.phoneNumber || undefined,
      }
      await staffStore.updateStaff(providerStore.currentProvider.id, editingStaffId.value, updateData)
    } else {
      await staffStore.createStaff(providerStore.currentProvider.id, staffFormData)
    }
    closeStaffModal()
  } catch (error) {
    console.error('Error saving staff:', error)
  }
}

function handleDeleteStaff(staffId: string) {
  staffToDelete.value = staffId
  showDeleteConfirm.value = true
}

async function confirmDelete() {
  if (staffToDelete.value && providerStore.currentProvider?.id) {
    try {
      await staffStore.deleteStaff(providerStore.currentProvider.id, staffToDelete.value)
    } catch (error) {
      console.error('Error deleting staff:', error)
    } finally {
      showDeleteConfirm.value = false
      staffToDelete.value = null
    }
  }
}

function cancelDelete() {
  showDeleteConfirm.value = false
  staffToDelete.value = null
}
</script>

<style scoped lang="scss">
.staff-list-view {
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

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  gap: 1rem;
}

.staff-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.staff-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  padding: 1rem 0;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
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

.error-message {
  font-size: 0.75rem;
  color: #ef4444;
  margin-top: -0.25rem;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  padding-top: 1rem;
  margin-top: 0.5rem;
  border-top: 1px solid #e5e7eb;
}

@media (max-width: 768px) {
  .staff-list-view {
    padding: 1rem;
  }

  .page-header {
    flex-direction: column;
    align-items: stretch;
  }

  .filters-row {
    flex-direction: column;
    align-items: stretch;

    .search-input {
      width: 100%;
    }
  }

  .staff-grid {
    grid-template-columns: 1fr;
  }

  .form-actions {
    flex-direction: column-reverse;

    button {
      width: 100%;
    }
  }
}
</style>
