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
              <div class="phone-input-wrapper">
                <span class="country-code">+98</span>
                <input
                  id="phoneNumber"
                  v-model="formData.phoneNumber"
                  type="tel"
                  dir="ltr"
                  class="form-input phone-input"
                  :class="{ 'form-input-error': errors.phoneNumber }"
                  placeholder="9123456789"
                  maxlength="10"
                  @input="validatePhoneNumber"
                  @blur="validateField('phoneNumber')"
                />
              </div>
              <span v-if="errors.phoneNumber" class="form-error">{{ errors.phoneNumber }}</span>
              <span v-else class="form-hint">شماره موبایل کارمند جدید را وارد کنید</span>
            </div>

            <!-- Invitee Name -->
            <div class="form-group">
              <label for="inviteeName" class="form-label">نام کارمند (اختیاری)</label>
              <input
                id="inviteeName"
                v-model="formData.inviteeName"
                type="text"
                class="form-input"
                placeholder="مثال: رضا احمدی"
              />
              <span class="form-hint">نام کارمند برای شناسایی آسان‌تر دعوت</span>
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
import AppButton from '@/shared/components/AppButton.vue'
import { useToast } from '@/core/composables/useToast'

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
const { showSuccess, showError } = useToast()

// ============================================
// State
// ============================================

const formData = reactive({
  phoneNumber: '',
  inviteeName: '',
  message: '',
})

const errors = reactive({
  phoneNumber: '',
  inviteeName: '',
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
    !errors.phoneNumber &&
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
    }
  }
}

function validateForm(): boolean {
  validateField('phoneNumber')
  return !errors.phoneNumber
}

// ============================================
// Action Methods
// ============================================

async function handleSubmit(): Promise<void> {
  if (!validateForm() || isSubmitting.value) return

  isSubmitting.value = true

  try {
    const request: SendInvitationRequest = {
      organizationId: props.organizationId,
      inviteePhoneNumber: `+98${formData.phoneNumber}`,
      inviteeName: formData.inviteeName || undefined,
      message: formData.message || undefined,
    }

    await hierarchyStore.sendInvitation(props.organizationId, request)

    showSuccess('دعوت با موفقیت ارسال شد')
    emit('invited')
    handleClose()
  } catch (error) {
    console.error('Error sending invitation:', error)
    showError('خطا در ارسال دعوت. لطفاً دوباره تلاش کنید.')
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

.phone-input-wrapper {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.country-code {
  padding: 0.75rem 1rem;
  background: #f3f4f6;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 0.95rem;
  font-weight: 600;
  color: #374151;
  direction: ltr;
}

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

.phone-input {
  flex: 1;
  direction: ltr;
  text-align: left;
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
