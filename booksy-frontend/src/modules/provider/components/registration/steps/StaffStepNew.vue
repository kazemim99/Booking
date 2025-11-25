<template>
  <div class="registration-step">

    <div class="step-card">
      <div class="step-header">
        <h2 class="step-title">پرسنل</h2>
        <p class="step-description">اطلاعات پرسنل و همکاران خود را اضافه کنید (اختیاری)</p>
      </div>

      <div class="step-content">
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
              <h4 class="staff-name">{{ member.name }}</h4>
              <p class="staff-details">
                {{ member.position || 'همکار' }} • {{ member.phoneNumber || 'بدون شماره' }}
              </p>
            </div>
            <div class="staff-actions">
              <button
                type="button"
                class="btn-icon"
                @click="handleEdit(member)"
                title="ویرایش"
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
                title="حذف"
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
            <label for="staffName" class="form-label">نام و نام خانوادگی</label>
            <input
              id="staffName"
              v-model="formData.name"
              type="text"
              class="form-input"
              placeholder="مثال: سارا محمدی"
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
              dir="ltr"
              class="form-input"
              placeholder="09123456789"
            />
          </div>

          <div class="form-actions">
            <AppButton type="button" variant="primary" size="medium" @click="handleAddStaff">
              {{ editingId ? 'ویرایش' : 'افزودن' }}
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
          @click="isAdding = true"
        >
          <svg class="icon-plus" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M12 4v16m8-8H4"
            />
          </svg>
          افزودن پرسنل جدید
        </AppButton>

        <!-- Navigation -->
        <div class="step-actions">
          <AppButton type="button" variant="outline" size="large" @click="$emit('back')">
            قبلی
          </AppButton>
          <AppButton type="button" variant="primary" size="large" @click="handleNext">
            بعدی
          </AppButton>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import ProgressIndicator from '../shared/ProgressIndicator.vue'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import type { TeamMember } from '@/modules/provider/types/registration.types'

interface Props {
  modelValue?: TeamMember[]
  ownerName?: string
}

interface Emits {
  (e: 'update:modelValue', value: TeamMember[]): void
  (e: 'next'): void
  (e: 'back'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

// State
const staffMembers = ref<TeamMember[]>(props.modelValue || [])
const isAdding = ref(false)
const editingId = ref<string | null>(null)
const formData = ref({
  name: '',
  position: '',
  phone: '',
})
const error = ref('')

// Methods
const handleAddStaff = () => {
  if (!formData.value.name || !formData.value.phone || !formData.value.position) {
    error.value = 'لطفاً تمام فیلدها را پر کنید'
    return
  }

  const newMember: TeamMember = {
    id: editingId.value || Date.now().toString(),
    name: formData.value.name.trim(),
    email: '',
    phoneNumber: formData.value.phone,
    countryCode: '+98',
    position: formData.value.position,
    isOwner: false,
  }

  if (editingId.value) {
    // Edit existing staff member
    staffMembers.value = staffMembers.value.map((s) =>
      s.id === editingId.value ? newMember : s
    )
    editingId.value = null
  } else {
    // Add new staff member
    staffMembers.value.push(newMember)
  }

  // Reset form
  formData.value = { name: '', position: '', phone: '' }
  isAdding.value = false
  error.value = ''

  // Emit update
  emit('update:modelValue', staffMembers.value)
}

const handleEdit = (member: TeamMember) => {
  formData.value = {
    name: member.name || '',
    position: member.position || '',
    phone: member.phoneNumber || '',
  }
  editingId.value = member.id
  isAdding.value = true
}

const handleDelete = (id: string) => {
  staffMembers.value = staffMembers.value.filter((s) => s.id !== id)
  emit('update:modelValue', staffMembers.value)
}

const handleCancelAdd = () => {
  isAdding.value = false
  editingId.value = null
  formData.value = { name: '', position: '', phone: '' }
  error.value = ''
}

const handleNext = () => {
  // Staff is optional, so we can proceed without validation
  error.value = ''
  emit('next')
}
</script>

<style scoped>
.registration-step {
  min-height: 100vh;
  padding: 2rem 1rem;
  background: #f9fafb;
  direction: rtl;
}

.step-card {
  max-width: 42rem;
  margin: 0 auto;
  background: white;
  border-radius: 1rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  padding: 2rem;
}

.step-header {
  margin-bottom: 2rem;
}

.step-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin-bottom: 0.5rem;
}

.step-description {
  font-size: 0.875rem;
  color: #6b7280;
}

.step-content {
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

/* Navigation */
.step-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 1rem;
  padding-top: 1.5rem;
  border-top: 1px solid #e5e7eb;
}

.step-actions > * {
  flex: 1;
}

@media (max-width: 640px) {
  .step-card {
    padding: 1.5rem;
  }

  .step-title {
    font-size: 1.25rem;
  }
}
</style>
