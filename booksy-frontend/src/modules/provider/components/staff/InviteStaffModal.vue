<template>
  <Teleport to="body">
    <div class="modal-overlay" @click="handleClose">
      <div class="modal-container" @click.stop>
        <div class="modal-header">
          <h2 class="modal-title">دعوت کارمند جدید</h2>
          <button class="modal-close" @click="handleClose">
            <i class="icon-x"></i>
          </button>
        </div>

        <div class="modal-body">
          <form @submit.prevent="handleSubmit">
            <!-- Phone Number -->
            <div class="form-group">
              <label for="phoneNumber" class="form-label">
                شماره موبایل <span class="required">*</span>
              </label>
              <div class="phone-input-container" :class="{ 'has-error': errors.phoneNumber }">
                <!-- Clear Button (on the left for RTL) -->
                <button
                  v-if="formData.phoneNumber"
                  type="button"
                  class="clear-button"
                  @click="clearPhoneNumber"
                  tabindex="-1"
                >
                  <svg class="clear-icon" viewBox="0 0 20 20" fill="currentColor">
                    <path
                      fill-rule="evenodd"
                      d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                      clip-rule="evenodd"
                    />
                  </svg>
                </button>

                <!-- Phone Number Input -->
                <input
                  id="phoneNumber"
                  v-model="formData.phoneNumber"
                  type="tel"
                  dir="ltr"
                  class="phone-input"
                  placeholder="9123456789"
                  maxlength="10"
                  @input="validatePhoneNumber"
                  @blur="validateField('phoneNumber')"
                />

                <!-- Country Code (on the right for RTL) -->
                <div class="country-code-display">
                  +98
                </div>
              </div>
              <span v-if="errors.phoneNumber" class="form-error">{{ errors.phoneNumber }}</span>
              <span v-else class="form-hint">شماره موبایل کارمند جدید را وارد کنید</span>
            </div>

            <!-- First Name -->
            <div class="form-group">
              <label for="firstName" class="form-label">
                نام <span class="required">*</span>
              </label>
              <input
                id="firstName"
                v-model="formData.firstName"
                type="text"
                class="form-input"
                :class="{ 'form-input-error': errors.firstName }"
                placeholder="مثال: رضا"
                maxlength="50"
                @blur="validateField('firstName')"
              />
              <span v-if="errors.firstName" class="form-error">{{ errors.firstName }}</span>
              <span v-else class="form-hint">نام کارمند (حداقل 2 کاراکتر)</span>
            </div>

            <!-- Last Name -->
            <div class="form-group">
              <label for="lastName" class="form-label">
                نام خانوادگی <span class="required">*</span>
              </label>
              <input
                id="lastName"
                v-model="formData.lastName"
                type="text"
                class="form-input"
                :class="{ 'form-input-error': errors.lastName }"
                placeholder="مثال: احمدی"
                maxlength="50"
                @blur="validateField('lastName')"
              />
              <span v-if="errors.lastName" class="form-error">{{ errors.lastName }}</span>
              <span v-else class="form-hint">نام خانوادگی کارمند (حداقل 2 کاراکتر)</span>
            </div>

            <!-- Email (Optional) -->
            <div class="form-group">
              <label for="email" class="form-label">ایمیل (اختیاری)</label>
              <input
                id="email"
                v-model="formData.email"
                type="email"
                dir="ltr"
                class="form-input"
                :class="{ 'form-input-error': errors.email }"
                placeholder="example@email.com"
                @blur="validateField('email')"
              />
              <span v-if="errors.email" class="form-error">{{ errors.email }}</span>
              <span v-else class="form-hint">ایمیل برای اطلاع‌رسانی‌های آینده</span>
            </div>

            <!-- Message -->
            <div class="form-group">
              <label for="message" class="form-label">پیام دعوت (اختیاری)</label>
              <textarea
                id="message"
                v-model="formData.message"
                class="form-textarea"
                rows="4"
                placeholder="پیامی برای کارمند جدید بنویسید..."
                maxlength="500"
              ></textarea>
              <div class="textarea-footer">
                <span class="form-hint">پیام شخصی‌سازی شده برای دعوت</span>
                <span class="char-count">{{ messageLength }}/500</span>
              </div>
            </div>

            <!-- Info Box -->
            <div class="info-box">
              <i class="icon-info-circle"></i>
              <div class="info-content">
                <h4 class="info-title">نحوه عملکرد دعوت:</h4>
                <ul class="info-list">
                  <li>یک پیامک حاوی لینک دعوت به شماره موبایل ارسال می‌شود</li>
                  <li>کارمند با کلیک روی لینک می‌تواند دعوت را بپذیرد</li>
                  <li>پس از پذیرش، کارمند به تیم شما اضافه می‌شود</li>
                  <li>دعوت پس از 7 روز منقضی می‌شود</li>
                </ul>
              </div>
            </div>
          </form>
        </div>

        <div class="modal-footer">
          <AppButton variant="secondary" size="medium" @click="handleClose" :disabled="isSubmitting">
            انصراف
          </AppButton>
          <AppButton
            variant="primary"
            size="medium"
            @click="handleSubmit"
            :disabled="!isFormValid || isSubmitting"
            :loading="isSubmitting"
          >
            <i v-if="!isSubmitting" class="icon-send"></i>
            {{ isSubmitting ? 'در حال ارسال...' : 'ارسال دعوت' }}
          </AppButton>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, reactive } from 'vue'
import { useHierarchyStore } from '../../stores/hierarchy.store'
import type { SendInvitationRequest } from '../../types/hierarchy.types'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import { useNotification } from '@/core/composables/useNotification'
import { isValidIranianMobile } from '@/core/utils'

// ============================================
// Props & Emits
// ============================================

interface Props {
  organizationId: string
}

const props = defineProps<Props>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'invited'): void
}>()

// ============================================
// Composables
// ============================================

const hierarchyStore = useHierarchyStore()
const { success, error } = useNotification()

// ============================================
// State
// ============================================

const formData = reactive({
  phoneNumber: '',
  firstName: '',
  lastName: '',
  email: '',
  message: '',
})

const errors = reactive({
  phoneNumber: '',
  firstName: '',
  lastName: '',
  email: '',
  message: '',
})

const isSubmitting = ref(false)

// ============================================
// Computed
// ============================================

const messageLength = computed(() => formData.message.length)

const isFormValid = computed(() => {
  return (
    formData.phoneNumber.length === 10 &&
    formData.firstName.trim().length >= 2 &&
    formData.lastName.trim().length >= 2 &&
    !errors.phoneNumber &&
    !errors.firstName &&
    !errors.lastName &&
    !errors.email &&
    !isSubmitting.value
  )
})

// ============================================
// Validation Methods
// ============================================

function validatePhoneNumber(): void {
  // Remove any non-digit characters
  formData.phoneNumber = formData.phoneNumber.replace(/\D/g, '')

  // Limit to 10 digits
  if (formData.phoneNumber.length > 10) {
    formData.phoneNumber = formData.phoneNumber.slice(0, 10)
  }
}

function validateField(field: keyof typeof formData): void {
  errors[field] = ''

  if (field === 'phoneNumber') {
    if (!formData.phoneNumber) {
      errors.phoneNumber = 'شماره موبایل الزامی است'
    } else if (formData.phoneNumber.length !== 10) {
      errors.phoneNumber = 'شماره موبایل باید 10 رقم باشد'
    } else if (!formData.phoneNumber.startsWith('9')) {
      errors.phoneNumber = 'شماره موبایل باید با 9 شروع شود'
    } else if (!isValidIranianMobile(formData.phoneNumber)) {
      errors.phoneNumber = 'شماره موبایل معتبر نیست (اپراتور نامعتبر)'
    }
  } else if (field === 'firstName') {
    if (!formData.firstName.trim()) {
      errors.firstName = 'نام الزامی است'
    } else if (formData.firstName.trim().length < 2) {
      errors.firstName = 'نام باید حداقل 2 کاراکتر باشد'
    } else if (formData.firstName.trim().length > 50) {
      errors.firstName = 'نام نباید بیشتر از 50 کاراکتر باشد'
    }
  } else if (field === 'lastName') {
    if (!formData.lastName.trim()) {
      errors.lastName = 'نام خانوادگی الزامی است'
    } else if (formData.lastName.trim().length < 2) {
      errors.lastName = 'نام خانوادگی باید حداقل 2 کاراکتر باشد'
    } else if (formData.lastName.trim().length > 50) {
      errors.lastName = 'نام خانوادگی نباید بیشتر از 50 کاراکتر باشد'
    }
  } else if (field === 'email') {
    if (formData.email && formData.email.trim()) {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
      if (!emailRegex.test(formData.email)) {
        errors.email = 'فرمت ایمیل معتبر نیست'
      }
    }
  }
}

function clearPhoneNumber(): void {
  formData.phoneNumber = ''
  errors.phoneNumber = ''
}

function validateForm(): boolean {
  validateField('phoneNumber')
  validateField('firstName')
  validateField('lastName')
  validateField('email')
  return !errors.phoneNumber && !errors.firstName && !errors.lastName && !errors.email
}

// ============================================
// Action Methods
// ============================================

async function handleSubmit(): Promise<void> {
  if (!validateForm() || isSubmitting.value) return

  // Validate organization ID
  if (!props.organizationId || props.organizationId.trim() === '') {
    error('خطا', 'شناسه سازمان نامعتبر است. لطفاً دوباره وارد شوید.')
    return
  }

  isSubmitting.value = true

  try {
    const request: SendInvitationRequest = {
      organizationId: props.organizationId,
      inviteePhoneNumber: `+98${formData.phoneNumber}`,
      inviteeName: `${formData.firstName.trim()} ${formData.lastName.trim()}`,
      firstName: formData.firstName.trim(),
      lastName: formData.lastName.trim(),
      email: formData.email.trim() || undefined,
      message: formData.message || undefined,
    }

    await hierarchyStore.sendInvitation(props.organizationId, request)

    success('موفقیت', 'دعوت با موفقیت ارسال شد')
    emit('invited')
    handleClose()
  } catch (err) {
    console.error('Error sending invitation:', err)
    error('خطا', 'خطا در ارسال دعوت. لطفاً دوباره تلاش کنید.')
  } finally {
    isSubmitting.value = false
  }
}

function handleClose(): void {
  if (isSubmitting.value) return
  emit('close')
}
</script>

<style scoped lang="scss">
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 1rem;
  backdrop-filter: blur(4px);
}

.modal-container {
  background: #fff;
  border-radius: 16px;
  max-width: 600px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  animation: modalSlideIn 0.3s ease-out;
}

@keyframes modalSlideIn {
  from {
    opacity: 0;
    transform: translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem 2rem;
  border-bottom: 1px solid #e5e7eb;
}

.modal-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1a1a1a;
}

.modal-close {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  background: #f3f4f6;
  border-radius: 8px;
  cursor: pointer;
  color: #6b7280;
  transition: all 0.2s;

  &:hover {
    background: #e5e7eb;
    color: #1a1a1a;
  }

  i {
    font-size: 1.25rem;
  }
}

.modal-body {
  padding: 2rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-label {
  display: block;
  font-size: 0.95rem;
  font-weight: 600;
  color: #374151;
  margin-bottom: 0.5rem;

  .required {
    color: #dc2626;
  }
}

// Phone Input Container (RTL layout)
.phone-input-container {
  display: flex;
  align-items: center;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  background-color: #ffffff;
  transition: all 0.2s;
  direction: rtl;
  overflow: hidden;

  &:focus-within {
    border-color: #7c3aed;
    box-shadow: 0 0 0 3px rgba(124, 58, 237, 0.1);
  }

  &.has-error {
    border-color: #dc2626;

    &:focus-within {
      border-color: #dc2626;
      box-shadow: 0 0 0 3px rgba(220, 38, 38, 0.1);
    }
  }
}

// Country Code Display (on the right for RTL)
.country-code-display {
  padding: 0.75rem 1rem;
  font-size: 0.95rem;
  font-weight: 600;
  color: #374151;
  background-color: #f9fafb;
  border-left: 1px solid #e5e7eb;
  user-select: none;
  flex-shrink: 0;
  direction: ltr;
}

// Phone Input Field
.phone-input {
  flex: 1;
  padding: 0.75rem 1rem;
  border: none;
  background: none;
  font-size: 0.95rem;
  color: #111827;
  outline: none;
  direction: ltr;
  text-align: left;

  &::placeholder {
    color: #9ca3af;
  }
}

// Clear Button (on the left for RTL)
.clear-button {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem;
  border: none;
  background: none;
  cursor: pointer;
  color: #9ca3af;
  transition: color 0.2s ease;
  flex-shrink: 0;

  &:hover {
    color: #6b7280;
  }

  &:focus {
    outline: none;
  }
}

.clear-icon {
  width: 1.25rem;
  height: 1.25rem;
}

// General Form Inputs
.form-input,
.form-textarea {
  width: 100%;
  padding: 0.75rem 1rem;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 0.95rem;
  transition: all 0.2s;

  &:focus {
    outline: none;
    border-color: #7c3aed;
    box-shadow: 0 0 0 3px rgba(124, 58, 237, 0.1);
  }

  &.form-input-error {
    border-color: #dc2626;

    &:focus {
      box-shadow: 0 0 0 3px rgba(220, 38, 38, 0.1);
    }
  }
}

.form-textarea {
  resize: vertical;
  min-height: 100px;
  font-family: inherit;
}

.textarea-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 0.5rem;
}

.form-hint {
  display: block;
  font-size: 0.875rem;
  color: #6b7280;
  margin-top: 0.25rem;
}

.form-error {
  display: block;
  font-size: 0.875rem;
  color: #dc2626;
  margin-top: 0.25rem;
}

.char-count {
  font-size: 0.875rem;
  color: #9ca3af;
}

.info-box {
  background: linear-gradient(135deg, #eff6ff 0%, #f0f9ff 100%);
  border: 1px solid #bfdbfe;
  border-radius: 12px;
  padding: 1.25rem;
  display: flex;
  gap: 1rem;
  margin-top: 1.5rem;

  i {
    color: #3b82f6;
    font-size: 1.5rem;
    flex-shrink: 0;
  }
}

.info-content {
  flex: 1;
}

.info-title {
  font-size: 1rem;
  font-weight: 600;
  color: #1e40af;
  margin-bottom: 0.5rem;
}

.info-list {
  list-style: none;
  padding: 0;
  margin: 0;

  li {
    padding: 0.375rem 0;
    padding-right: 1.25rem;
    position: relative;
    font-size: 0.875rem;
    color: #1e3a8a;
    line-height: 1.5;

    &::before {
      content: '•';
      position: absolute;
      right: 0;
      color: #3b82f6;
      font-weight: bold;
      font-size: 1.25rem;
    }
  }
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  padding: 1.5rem 2rem;
  border-top: 1px solid #e5e7eb;
  background: #f9fafb;
}

// Responsive
@media (max-width: 640px) {
  .modal-container {
    max-height: 95vh;
  }

  .modal-header,
  .modal-body,
  .modal-footer {
    padding: 1.25rem;
  }

  .modal-footer {
    flex-direction: column;

    button {
      width: 100%;
    }
  }

  .phone-input-wrapper {
    flex-direction: column;
    align-items: stretch;

    .country-code {
      text-align: center;
    }
  }
}
</style>
