<template>
  <ResponsiveModal
    :is-open="isOpen"
    @close="handleClose"
    title="ویرایش پروفایل"
    size="md"
    mobile-height="auto"
  >
    <form @submit.prevent="handleSubmit" class="profile-edit-form">
      <!-- First Name and Last Name in a row -->
      <div class="form-row">
        <div class="form-group">
          <label for="firstName" class="form-label">نام *</label>
          <input
            id="firstName"
            v-model="form.firstName"
            type="text"
            class="form-input"
            :class="{ error: errors.firstName }"
            placeholder="نام"
            :disabled="loading"
          />
          <span v-if="errors.firstName" class="error-message">{{ errors.firstName }}</span>
        </div>

        <div class="form-group">
          <label for="lastName" class="form-label">نام خانوادگی *</label>
          <input
            id="lastName"
            v-model="form.lastName"
            type="text"
            class="form-input"
            :class="{ error: errors.lastName }"
            placeholder="نام خانوادگی"
            :disabled="loading"
          />
          <span v-if="errors.lastName" class="error-message">{{ errors.lastName }}</span>
        </div>
      </div>

      <!-- Phone Number (Display Only) -->
      <div class="form-group">
        <label for="phoneNumber" class="form-label">شماره تلفن</label>
        <input
          id="phoneNumber"
          :value="profile?.phoneNumber"
          type="text"
          class="form-input"
          disabled
          readonly
        />
        <span class="help-text">برای تغییر شماره تلفن با پشتیبانی تماس بگیرید</span>
      </div>

      <!-- Email (Optional) -->
      <div class="form-group">
        <label for="email" class="form-label">ایمیل (اختیاری)</label>
        <input
          id="email"
          v-model="form.email"
          type="email"
          class="form-input"
          :class="{ error: errors.email }"
          placeholder="your@email.com"
          :disabled="loading"
        />
        <span v-if="errors.email" class="error-message">{{ errors.email }}</span>
      </div>

      <!-- Error Message -->
      <div v-if="serverError" class="server-error">
        {{ serverError }}
      </div>

      <!-- Buttons -->
      <div class="form-actions">
        <button type="button" @click="handleClose" class="btn btn-secondary" :disabled="loading">
          لغو
        </button>
        <button type="submit" class="btn btn-primary" :disabled="loading || !isFormValid">
          <span v-if="loading">در حال ذخیره...</span>
          <span v-else>ذخیره تغییرات</span>
        </button>
      </div>
    </form>
  </ResponsiveModal>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useCustomerStore } from '../../stores/customer.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useNotification } from '@/core/composables/useNotification'
import ResponsiveModal from '@/shared/components/ui/ResponsiveModal.vue'

interface Props {
  isOpen: boolean
}

const props = defineProps<Props>()
const emit = defineEmits<{
  close: []
}>()

const customerStore = useCustomerStore()
const authStore = useAuthStore()
const { success, error } = useNotification()

const profile = computed(() => customerStore.profile)
const loading = ref(false)
const serverError = ref<string | null>(null)

// Form state
const form = ref({
  firstName: '',
  lastName: '',
  email: ''
})

// Form errors
const errors = ref({
  firstName: '',
  lastName: '',
  email: ''
})

// Fetch profile when modal opens
watch(() => props.isOpen, async (open) => {
  if (open) {
    // If customerId is missing, try to refresh the token to get it
    if (!authStore.customerId) {
      console.warn('[ProfileEditModal] customerId is null, refreshing token...')
      try {
        await authStore.refresh()
      } catch (error) {
        console.error('[ProfileEditModal] Failed to refresh token:', error)
      }
    }

    // Now try to fetch profile if we have customerId
    if (authStore.customerId && !profile.value) {
      try {
        await customerStore.fetchProfile(authStore.customerId)
      } catch (err) {
        console.error('[ProfileEditModal] Error fetching profile:', err)
        error('خطا', 'خطا در بارگذاری اطلاعات پروفایل')
      }
    } else if (!authStore.customerId) {
      error('خطا', 'لطفاً دوباره وارد شوید')
    }
  }
}, { immediate: true })

// Initialize form when profile changes
watch([() => props.isOpen, profile], ([open, profileData]) => {
  if (open && profileData) {
    form.value.firstName = profileData.firstName || ''
    form.value.lastName = profileData.lastName || ''
    form.value.email = profileData.email || ''
    clearErrors()
    serverError.value = null
  }
}, { immediate: true })

const isFormValid = computed(() => {
  return form.value.firstName.length >= 2 &&
         form.value.lastName.length >= 2 &&
         !errors.value.firstName &&
         !errors.value.lastName &&
         !errors.value.email
})

function validateFirstName(): boolean {
  if (!form.value.firstName || form.value.firstName.trim().length < 2) {
    errors.value.firstName = 'نام باید حداقل 2 کاراکتر باشد'
    return false
  }
  if (form.value.firstName.length > 50) {
    errors.value.firstName = 'نام نمی‌تواند بیشتر از 50 کاراکتر باشد'
    return false
  }
  errors.value.firstName = ''
  return true
}

function validateLastName(): boolean {
  if (!form.value.lastName || form.value.lastName.trim().length < 2) {
    errors.value.lastName = 'نام خانوادگی باید حداقل 2 کاراکتر باشد'
    return false
  }
  if (form.value.lastName.length > 50) {
    errors.value.lastName = 'نام خانوادگی نمی‌تواند بیشتر از 50 کاراکتر باشد'
    return false
  }
  errors.value.lastName = ''
  return true
}

function validateEmail(): boolean {
  const email = form.value.email.trim()
  if (!email) {
    errors.value.email = ''
    return true // Email is optional
  }

  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  if (!emailRegex.test(email)) {
    errors.value.email = 'فرمت ایمیل معتبر نیست'
    return false
  }

  errors.value.email = ''
  return true
}

function clearErrors(): void {
  errors.value.firstName = ''
  errors.value.lastName = ''
  errors.value.email = ''
  serverError.value = null
}

async function handleSubmit(): Promise<void> {
  clearErrors()

  if (!validateFirstName() || !validateLastName() || !validateEmail()) {
    return
  }

  if (!authStore.customerId) {
    error('خطا', 'کاربر احراز هویت نشده است')
    return
  }

  loading.value = true

  try {
    await customerStore.updateProfile(authStore.customerId, {
      firstName: form.value.firstName.trim(),
      lastName: form.value.lastName.trim(),
      email: form.value.email.trim() || undefined
    })

    success('موفقیت', 'پروفایل با موفقیت بهروزرسانی شد')
    handleClose()
  } catch (err) {
    console.error('[ProfileEditModal] Error updating profile:', err)
    serverError.value = err instanceof Error ? err.message : 'خطا در بهروزرسانی پروفایل'
    error('خطا', serverError.value)
  } finally {
    loading.value = false
  }
}

function handleClose(): void {
  clearErrors()
  emit('close')
}
</script>

<style scoped lang="scss">
.profile-edit-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;

  @media (max-width: 640px) {
    grid-template-columns: 1fr;
  }
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-label {
  font-weight: 500;
  color: #374151;
  font-size: 0.875rem;
}

.form-input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 1rem;
  transition: border-color 0.2s, box-shadow 0.2s;

  &:focus {
    outline: none;
    border-color: #667eea;
    box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
  }

  &:disabled {
    background-color: #f3f4f6;
    color: #6b7280;
    cursor: not-allowed;
  }

  &.error {
    border-color: #ef4444;

    &:focus {
      box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
    }
  }
}

.help-text {
  font-size: 0.75rem;
  color: #6b7280;
}

.error-message {
  font-size: 0.75rem;
  color: #ef4444;
}

.server-error {
  padding: 0.75rem;
  background-color: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 8px;
  color: #ef4444;
  font-size: 0.875rem;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  margin-top: 1rem;
}

.btn {
  padding: 0.75rem 1.5rem;
  border-radius: 8px;
  font-weight: 500;
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s;
  border: none;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

.btn-secondary {
  background-color: white;
  color: #374151;
  border: 1px solid #d1d5db;

  &:hover:not(:disabled) {
    background-color: #f9fafb;
  }
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;

  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
  }
}
</style>
