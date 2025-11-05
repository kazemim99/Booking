<template>
  <div class="staff-section">
    <!-- Staff List -->
    <div v-if="staffMembers.length > 0" class="staff-list">
      <div v-for="member in staffMembers" :key="member.id" class="staff-item">
        <div class="staff-avatar">
          <svg class="avatar-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
            />
          </svg>
        </div>
        <div class="staff-info">
          <h4 class="staff-name">{{ member.firstName }} {{ member.lastName }}</h4>
          <p class="staff-details">
            {{ member.role || 'Staff' }} • {{ member.phoneNumber || 'No phone' }}
          </p>
        </div>
        <div class="staff-actions">
          <button
            type="button"
            class="btn-icon"
            @click="handleEdit(member)"
            title="Edit"
          >
            <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
              />
            </svg>
          </button>
          <button
            type="button"
            class="btn-icon btn-delete"
            @click="handleDelete(member.id)"
            title="Delete"
          >
            <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
              />
            </svg>
          </button>
        </div>
      </div>
    </div>

    <!-- Add/Edit Form -->
    <div v-if="isAdding" class="staff-form">
      <div class="form-group">
        <label for="staffName" class="form-label">Full Name</label>
        <input
          id="staffName"
          v-model="formData.name"
          type="text"
          class="form-input"
          placeholder="e.g., John Doe"
        />
      </div>

      <div class="form-group">
        <label for="staffPosition" class="form-label">Position</label>
        <input
          id="staffPosition"
          v-model="formData.position"
          type="text"
          class="form-input"
          placeholder="e.g., Stylist"
        />
      </div>

      <div class="form-group">
        <label for="staffPhone" class="form-label">Phone Number</label>
        <input
          id="staffPhone"
          v-model="formData.phone"
          type="tel"
          class="form-input"
          placeholder="+1 (555) 000-0000"
        />
      </div>

      <div class="form-actions">
        <AppButton type="button" variant="primary" size="medium" @click="handleSaveStaff">
          {{ editingId ? 'Update' : 'Add' }}
        </AppButton>
        <AppButton type="button" variant="outline" size="medium" @click="handleCancelAdd">
          Cancel
        </AppButton>
      </div>

      <p v-if="error" class="form-error">{{ error }}</p>
    </div>

    <!-- Add Staff Button -->
    <AppButton
      v-else
      type="button"
      variant="outline"
      size="large"
      class="btn-add-staff"
      @click="handleAddClick"
    >
      <svg class="icon-plus" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M12 4v16m8-8H4"
        />
      </svg>
      Add Staff Member
    </AppButton>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useStaffStore } from '../stores/staff.store'
import { useProviderStore } from '../stores/provider.store'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import type { Staff } from '../types/staff.types'

const staffStore = useStaffStore()
const providerStore = useProviderStore()

// State
const staffMembers = ref<Staff[]>([])
const isAdding = ref(false)
const editingId = ref<string | null>(null)
const error = ref('')

const formData = ref({
  name: '',
  position: '',
  phone: '',
})

// Load staff on mount
onMounted(async () => {
  await loadStaff()
})

async function loadStaff() {
  try {
    if (!providerStore.currentProvider) {
      await providerStore.loadCurrentProvider()
    }

    const providerId = providerStore.currentProvider?.id
    if (providerId) {
      await staffStore.loadStaffByProvider(providerId)
      staffMembers.value = staffStore.staff
    }
  } catch (err) {
    console.error('Error loading staff:', err)
  }
}

function handleAddClick() {
  isAdding.value = true
  editingId.value = null
  formData.value = {
    name: '',
    position: '',
    phone: '',
  }
  error.value = ''
}

function handleEdit(member: Staff) {
  formData.value = {
    name: `${member.firstName} ${member.lastName}`.trim(),
    position: member.role || '',
    phone: member.phoneNumber || '',
  }
  editingId.value = member.id
  isAdding.value = true
  error.value = ''
}

async function handleSaveStaff() {
  if (!formData.value.name || !formData.value.phone || !formData.value.position) {
    error.value = 'Please fill in all fields'
    return
  }

  if (!providerStore.currentProvider?.id) {
    error.value = 'Provider not found'
    return
  }

  // Split name into first and last name
  const nameParts = formData.value.name.trim().split(' ')
  const firstName = nameParts[0] || ''
  const lastName = nameParts.slice(1).join(' ') || ''

  try {
    if (editingId.value) {
      // Update existing staff
      await staffStore.updateStaff(providerStore.currentProvider.id, editingId.value, {
        firstName,
        lastName,
        phoneNumber: formData.value.phone || undefined,
        role: formData.value.position || undefined,
      })
    } else {
      // Create new staff
      await staffStore.createStaff(providerStore.currentProvider.id, {
        firstName,
        lastName,
        phoneNumber: formData.value.phone,
        role: formData.value.position,
      })
    }

    // Reload staff list
    await loadStaff()

    // Reset form
    isAdding.value = false
    editingId.value = null
    formData.value = {
      name: '',
      position: '',
      phone: '',
    }
    error.value = ''
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to save staff member'
    console.error('Error saving staff:', err)
  }
}

async function handleDelete(id: string) {
  if (!confirm('Are you sure you want to delete this staff member?')) {
    return
  }

  if (!providerStore.currentProvider?.id) {
    return
  }

  try {
    await staffStore.deleteStaff(providerStore.currentProvider.id, id)
    await loadStaff()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to delete staff member'
    console.error('Error deleting staff:', err)
  }
}

function handleCancelAdd() {
  isAdding.value = false
  editingId.value = null
  formData.value = {
    name: '',
    position: '',
    phone: '',
  }
  error.value = ''
}
</script>

<style scoped>
.staff-section {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Staff List */
.staff-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.staff-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem;
  background: rgba(139, 92, 246, 0.05);
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
}

.staff-avatar {
  width: 3rem;
  height: 3rem;
  border-radius: 50%;
  background: rgba(139, 92, 246, 0.1);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.avatar-icon {
  width: 1.5rem;
  height: 1.5rem;
  color: #8b5cf6;
}

.staff-info {
  flex: 1;
}

.staff-name {
  font-size: 1rem;
  font-weight: 600;
  color: #111827;
  margin-bottom: 0.25rem;
}

.staff-details {
  font-size: 0.875rem;
  color: #6b7280;
}

.staff-actions {
  display: flex;
  gap: 0.5rem;
}

.btn-icon {
  padding: 0.5rem;
  background: none;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-icon:hover {
  background: rgba(0, 0, 0, 0.05);
}

.btn-icon .icon {
  width: 1.25rem;
  height: 1.25rem;
  color: #6b7280;
}

.btn-delete .icon {
  color: #ef4444;
}

/* Staff Form */
.staff-form {
  padding: 1rem;
  background: rgba(139, 92, 246, 0.05);
  border: 1px solid rgba(139, 92, 246, 0.2);
  border-radius: 0.5rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
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
}

.form-input {
  width: 100%;
  padding: 0.75rem 1rem;
  font-size: 1rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  background: white;
  transition: all 0.2s ease;
}

.form-input:focus {
  outline: none;
  border-color: #8b5cf6;
  box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
}

.form-actions {
  display: flex;
  gap: 0.5rem;
}

.form-actions > * {
  flex: 1;
}

.form-error {
  font-size: 0.875rem;
  color: #ef4444;
}

/* Add Staff Button */
.btn-add-staff {
  width: 100%;
  border-style: dashed !important;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
}

.icon-plus {
  width: 1.25rem;
  height: 1.25rem;
}

/* Responsive */
@media (max-width: 640px) {
  .staff-item {
    flex-direction: column;
    align-items: flex-start;
  }

  .staff-actions {
    width: 100%;
    justify-content: flex-end;
  }

  .form-actions {
    flex-direction: column;
  }

  .form-actions > * {
    width: 100%;
  }
}
</style>
