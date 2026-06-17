<template>
  <div class="staff-section">
    <!-- Hierarchy Type Check: Independent Individual Cannot Add Staff -->
    <div v-if="isIndependentIndividual" class="info-card">
      <div class="info-icon-wrapper">ℹ️</div>
      <div class="info-content">
        <h4 class="info-title">حساب فردی</h4>
        <p class="info-text">
          شما به عنوان یک متخصص فردی ثبت‌نام کرده‌اید. برای اضافه کردن پرسنل، باید ابتدا
          <router-link to="/provider/convert-to-organization" class="convert-link">
            به سازمان تبدیل شوید
          </router-link>.
        </p>
      </div>
    </div>

    <!-- Hierarchy Type Check: Organization Should Use Advanced Dashboard -->
    <div v-else-if="isOrganization" class="info-card info-card-primary">
      <div class="info-icon-wrapper">👥</div>
      <div class="info-content">
        <h4 class="info-title">مدیریت پیشرفته پرسنل</h4>
        <p class="info-text">
          برای مدیریت کامل پرسنل سازمان، از داشبورد مدیریت پرسنل استفاده کنید.
          در آنجا می‌توانید دعوت‌نامه ارسال کنید، درخواست‌های پیوستن را بررسی کنید و پرسنل را مدیریت نمایید.
        </p>
        <router-link to="/provider/staff" class="dashboard-link-btn">
          <svg class="icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 8l4 4m0 0l-4 4m4-4H3" />
          </svg>
          رفتن به داشبورد مدیریت پرسنل
        </router-link>

        <!-- Show staff list for organizations (read-only preview) -->
        <div v-if="staffMembers.length > 0" class="staff-preview">
          <h5 class="preview-title">پرسنل فعلی ({{ staffMembers.length }} نفر)</h5>
          <div class="staff-list-compact">
            <div v-for="member in staffMembers.slice(0, 5)" :key="member.id" class="staff-item-compact">
              <div class="staff-avatar-small">
                <svg class="avatar-icon-small" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                </svg>
              </div>
              <div class="staff-info-compact">
                <span class="staff-name-compact">{{ member.firstName }} {{ member.lastName }}</span>
                <span class="staff-role-compact">{{ member.role || 'پرسنل' }}</span>
              </div>
            </div>
            <div v-if="staffMembers.length > 5" class="more-indicator">
              و {{ staffMembers.length - 5 }} نفر دیگر...
            </div>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useStaffStore } from '../stores/staff.store'
import { useProviderStore } from '../stores/provider.store'
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

// Computed: Check provider hierarchy type
const currentProvider = computed(() => {
  const provider = providerStore.currentProvider

  // Debug logging to check if hierarchy fields are present
  if (provider) {
    const hasHierarchyFields = provider.hierarchyType !== undefined || provider.isIndependent !== undefined

    console.log('[ProfileStaffSection] Current provider:', {
      id: provider.id,
      businessName: provider.profile?.businessName,
      hierarchyType: provider.hierarchyType,
      isIndependent: provider.isIndependent,
      hasStaff: provider.staff?.length || 0,
      hasHierarchyFields
    })

    if (!hasHierarchyFields) {
      console.warn(
        '[ProfileStaffSection] ⚠️ Backend is not returning hierarchyType and isIndependent fields.\n' +
        'This means the provider hierarchy system is not fully integrated with the backend.\n' +
        'The legacy staff management interface will be shown.\n\n' +
        'TO FIX: Update the backend Provider API (GetProviderById endpoint) to include:\n' +
        '- hierarchyType: "Organization" | "Individual"\n' +
        '- isIndependent: boolean\n' +
        '- parentProviderId: string | null'
      )
    }
  }

  return provider
})

const isIndependentIndividual = computed(() => {
  // Check if hierarchy type is explicitly set to Individual with isIndependent flag
  if (currentProvider.value?.hierarchyType === 'Individual' && currentProvider.value?.isIndependent === true) {
    return true
  }
  return false
})

const isOrganization = computed(() => {
  // Check if hierarchy type is explicitly set to Organization
  if (currentProvider.value?.hierarchyType === 'Organization') {
    return true
  }
  return false
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
  border: 1px solid var(--color-gray-300);
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
  color: var(--color-primary-500);
}

.staff-info {
  flex: 1;
}

.staff-name {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-gray-900);
  margin-bottom: 0.25rem;
}

.staff-details {
  font-size: 0.875rem;
  color: var(--color-gray-600);
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
  color: var(--color-gray-600);
}

.btn-delete .icon {
  color: var(--color-danger-500);
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
  color: var(--color-gray-800);
}

.form-input {
  width: 100%;
  padding: 0.75rem 1rem;
  font-size: 1rem;
  border: 1px solid var(--color-gray-400);
  border-radius: 0.5rem;
  background: white;
  transition: all 0.2s ease;
}

.form-input:focus {
  outline: none;
  border-color: var(--color-primary-500);
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
  color: var(--color-danger-500);
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

/* Info Cards for Hierarchy Type Messages */
.info-card {
  display: flex;
  gap: 1rem;
  padding: 1.5rem;
  background: rgba(59, 130, 246, 0.05);
  border: 1px solid rgba(59, 130, 246, 0.2);
  border-radius: 0.75rem;
  margin-bottom: 1rem;
}

.info-card-primary {
  background: rgba(139, 92, 246, 0.05);
  border-color: rgba(139, 92, 246, 0.2);
}

.info-icon-wrapper {
  font-size: 2rem;
  flex-shrink: 0;
}

.info-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.info-title {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-gray-900);
  margin: 0;
}

.info-text {
  font-size: 0.875rem;
  color: var(--color-gray-600);
  line-height: 1.6;
  margin: 0;
}

.convert-link {
  color: var(--color-primary-500);
  font-weight: 600;
  text-decoration: underline;
  transition: color 0.2s;
}

.convert-link:hover {
  color: var(--color-primary-700);
}

.dashboard-link-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.25rem;
  background: var(--color-primary-500);
  color: white;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-weight: 600;
  text-decoration: none;
  transition: all 0.2s;
  width: fit-content;
}

.dashboard-link-btn:hover {
  background: var(--color-primary-700);
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(139, 92, 246, 0.3);
}

.dashboard-link-btn .icon {
  width: 1rem;
  height: 1rem;
}

/* Staff Preview (for Organizations) */
.staff-preview {
  margin-top: 1.5rem;
  padding-top: 1.5rem;
  border-top: 1px solid rgba(139, 92, 246, 0.2);
}

.preview-title {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-gray-800);
  margin: 0 0 0.75rem 0;
}

.staff-list-compact {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.staff-item-compact {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.625rem;
  background: white;
  border: 1px solid var(--color-gray-300);
  border-radius: 0.5rem;
}

.staff-avatar-small {
  width: 2rem;
  height: 2rem;
  border-radius: 50%;
  background: rgba(139, 92, 246, 0.1);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.avatar-icon-small {
  width: 1rem;
  height: 1rem;
  color: var(--color-primary-500);
}

.staff-info-compact {
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
  flex: 1;
}

.staff-name-compact {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-gray-900);
}

.staff-role-compact {
  font-size: 0.75rem;
  color: var(--color-gray-600);
}

.more-indicator {
  font-size: 0.8125rem;
  color: var(--color-gray-600);
  text-align: center;
  padding: 0.5rem;
  background: var(--color-gray-50);
  border-radius: 0.375rem;
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

  .info-card {
    padding: 1rem;
  }

  .dashboard-link-btn {
    width: 100%;
    justify-content: center;
  }
}
</style>
