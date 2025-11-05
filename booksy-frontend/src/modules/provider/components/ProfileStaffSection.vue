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
            {{ member.role || 'پرسنل' }} • {{ member.phoneNumber || 'بدون شماره' }}
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
        <label for="staffFirstName" class="form-label">نام</label>
        <input
          id="staffFirstName"
          v-model="formData.firstName"
          type="text"
          class="form-input"
          placeholder="مثال: علی"
        />
      </div>

      <div class="form-group">
        <label for="staffLastName" class="form-label">نام خانوادگی</label>
        <input
          id="staffLastName"
          v-model="formData.lastName"
          type="text"
          class="form-input"
          placeholder="مثال: احمدی"
        />
      </div>

      <div class="form-group">
        <label for="staffPosition" class="form-label">سمت</label>
        <input
          id="staffPosition"
          v-model="formData.position"
          type="text"
          class="form-input"
          placeholder="مثال: آرایشگر"
        />
      </div>

      <div class="form-group">
        <label for="staffPhone" class="form-label">شماره تماس</label>
        <input
          id="staffPhone"
          v-model="formData.phone"
          type="tel"
          class="form-input"
          placeholder="09123456789"
        />
      </div>

      <div class="form-actions">
        <AppButton type="button" variant="primary" size="medium" @click="handleSaveStaff">
          {{ editingId ? 'بروزرسانی' : 'افزودن' }}
        </AppButton>
        <AppButton type="button" variant="outline" size="medium" @click="handleCancelAdd">
          لغو
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
      افزودن پرسنل
    </AppButton>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useStaffStore } from '../stores/staff.store'
import { useProviderStore } from '../stores/provider.store'
import { useToast } from '@/shared/composables/useToast'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import type { Staff } from '../types/staff.types'
import type { TeamMember } from '../types/registration.types'

// Internal staff member type (unified structure)
interface StaffMember {
  id: string
  firstName: string
  lastName: string
  phoneNumber?: string
  role?: string
}

interface Props {
  // For registration flow: pass local staff array (TeamMember[])
  modelValue?: TeamMember[]
  // For profile mode: load from backend
  useBackend?: boolean
}

interface Emits {
  (e: 'update:modelValue', value: TeamMember[]): void
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: () => [],
  useBackend: true,
})

const emit = defineEmits<Emits>()

const staffStore = useStaffStore()
const providerStore = useProviderStore()
const toast = useToast()

// State - use unified internal structure
const staffMembers = ref<StaffMember[]>([])
const isAdding = ref(false)
const editingId = ref<string | null>(null)
const error = ref('')

const formData = ref({
  firstName: '',
  lastName: '',
  position: '',
  phone: '',
})

// Helper functions to convert between types
function teamMemberToStaff(member: TeamMember): StaffMember {
  const nameParts = member.name.trim().split(' ')
  return {
    id: member.id,
    firstName: nameParts[0] || '',
    lastName: nameParts.slice(1).join(' ') || '',
    phoneNumber: member.phoneNumber,
    role: member.position,
  }
}

function staffToTeamMember(member: StaffMember): TeamMember {
  return {
    id: member.id,
    name: `${member.firstName} ${member.lastName}`.trim(),
    email: '',
    phoneNumber: member.phoneNumber || '',
    countryCode: '',
    position: member.role || '',
    isOwner: false,
  }
}

function backendStaffToInternal(staff: Staff): StaffMember {
  return {
    id: staff.id,
    firstName: staff.firstName,
    lastName: staff.lastName,
    phoneNumber: staff.phoneNumber,
    role: staff.role,
  }
}

// Watch modelValue for changes (registration mode)
watch(() => props.modelValue, (newValue) => {
  if (!props.useBackend && newValue) {
    staffMembers.value = newValue.map(teamMemberToStaff)
  }
}, { immediate: true })

// Load staff on mount (profile mode)
onMounted(async () => {
  if (props.useBackend) {
    await loadStaff()
  } else {
    staffMembers.value = props.modelValue.map(teamMemberToStaff)
  }
})

async function loadStaff() {
  try {
    if (!providerStore.currentProvider) {
      await providerStore.loadCurrentProvider()
    }

    const providerId = providerStore.currentProvider?.id
    if (providerId) {
      await staffStore.loadStaffByProvider(providerId)
      staffMembers.value = staffStore.staff.map(backendStaffToInternal)
    }
  } catch (err) {
    console.error('Error loading staff:', err)
  }
}

function handleAddClick() {
  isAdding.value = true
  editingId.value = null
  formData.value = {
    firstName: '',
    lastName: '',
    position: '',
    phone: '',
  }
  error.value = ''
}

function handleEdit(member: StaffMember) {
  formData.value = {
    firstName: member.firstName,
    lastName: member.lastName,
    position: member.role || '',
    phone: member.phoneNumber || '',
  }
  editingId.value = member.id
  isAdding.value = true
  error.value = ''
}

async function handleSaveStaff() {
  if (!formData.value.firstName || !formData.value.lastName || !formData.value.phone || !formData.value.position) {
    error.value = 'لطفاً تمام فیلدها را پر کنید'
    return
  }

  const newMember: StaffMember = {
    id: editingId.value || Date.now().toString(),
    firstName: formData.value.firstName,
    lastName: formData.value.lastName,
    phoneNumber: formData.value.phone,
    role: formData.value.position,
  }

  if (props.useBackend) {
    // Backend mode (Profile)
    if (!providerStore.currentProvider?.id) {
      error.value = 'ارائه‌دهنده یافت نشد'
      return
    }

    try {
      if (editingId.value) {
        // Update existing staff
        await staffStore.updateStaff(providerStore.currentProvider.id, editingId.value, {
          firstName: formData.value.firstName,
          lastName: formData.value.lastName,
          phoneNumber: formData.value.phone || undefined,
          role: formData.value.position || undefined,
        })
      } else {
        // Create new staff
        await staffStore.createStaff(providerStore.currentProvider.id, {
          firstName: formData.value.firstName,
          lastName: formData.value.lastName,
          phoneNumber: formData.value.phone,
          role: formData.value.position,
        })
      }

      // Reload staff list
      await loadStaff()
      toast.success(editingId.value ? 'پرسنل با موفقیت بروزرسانی شد' : 'پرسنل با موفقیت اضافه شد')
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'خطا در ذخیره اطلاعات پرسنل'
      error.value = errorMessage
      toast.error(errorMessage)
      console.error('Error saving staff:', err)
      return
    }
  } else {
    // Local mode (Registration)
    if (editingId.value) {
      // Edit existing staff member
      staffMembers.value = staffMembers.value.map((s) =>
        s.id === editingId.value ? newMember : s
      )
    } else {
      // Add new staff member
      staffMembers.value.push(newMember)
    }

    // Emit update (convert to TeamMember[] for registration mode)
    emit('update:modelValue', staffMembers.value.map(staffToTeamMember))
  }

  // Reset form
  isAdding.value = false
  editingId.value = null
  formData.value = {
    firstName: '',
    lastName: '',
    position: '',
    phone: '',
  }
  error.value = ''
}

async function handleDelete(id: string) {
  if (!confirm('آیا از حذف این پرسنل اطمینان دارید؟')) {
    return
  }

  if (props.useBackend) {
    // Backend mode (Profile)
    if (!providerStore.currentProvider?.id) {
      return
    }

    try {
      await staffStore.deleteStaff(providerStore.currentProvider.id, id)
      await loadStaff()
      toast.success('پرسنل با موفقیت حذف شد')
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'خطا در حذف پرسنل'
      error.value = errorMessage
      toast.error(errorMessage)
      console.error('Error deleting staff:', err)
    }
  } else {
    // Local mode (Registration)
    staffMembers.value = staffMembers.value.filter((s) => s.id !== id)
    emit('update:modelValue', staffMembers.value.map(staffToTeamMember))
    toast.success('پرسنل حذف شد')
  }
}

function handleCancelAdd() {
  isAdding.value = false
  editingId.value = null
  formData.value = {
    firstName: '',
    lastName: '',
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
