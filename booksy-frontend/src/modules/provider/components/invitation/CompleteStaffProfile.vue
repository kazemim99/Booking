<template>
  <div class="complete-staff-profile">
    <div class="profile-wizard">
      <h3 class="wizard-title">تکمیل پروفایل کارمند</h3>
      <p class="wizard-description">لطفاً اطلاعات خود را وارد کنید تا به تیم بپیوندید.</p>

      <form @submit.prevent="handleSubmit" class="profile-form">
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
            placeholder="نام خود را وارد کنید"
            @blur="validateField('firstName')"
          />
          <span v-if="errors.firstName" class="form-error">{{ errors.firstName }}</span>
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
            placeholder="نام خانوادگی خود را وارد کنید"
            @blur="validateField('lastName')"
          />
          <span v-if="errors.lastName" class="form-error">{{ errors.lastName }}</span>
        </div>

        <!-- Email -->
        <div class="form-group">
          <label for="email" class="form-label">
            ایمیل <span class="required">*</span>
          </label>
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
        </div>

        <!-- Bio -->
        <div class="form-group">
          <label for="bio" class="form-label">بیوگرافی (اختیاری)</label>
          <textarea
            id="bio"
            v-model="formData.bio"
            class="form-textarea"
            rows="4"
            placeholder="درباره خودتان و تخصص‌هایتان بنویسید..."
            maxlength="500"
          ></textarea>
          <div class="textarea-footer">
            <span class="form-hint">معرفی مختصری از خودتان</span>
            <span class="char-count">{{ bioLength }}/500</span>
          </div>
        </div>

        <!-- Avatar URL (optional - usually would be file upload) -->
        <div class="form-group">
          <label for="avatarUrl" class="form-label">آدرس تصویر پروفایل (اختیاری)</label>
          <input
            id="avatarUrl"
            v-model="formData.avatarUrl"
            type="url"
            dir="ltr"
            class="form-input"
            placeholder="https://example.com/avatar.jpg"
          />
          <span class="form-hint">لینک تصویر پروفایل خود را وارد کنید</span>
        </div>

        <!-- Info Box -->
        <div class="info-box">
          <i class="icon-info-circle"></i>
          <div class="info-content">
            <p>
              با تکمیل این فرم، شما به عنوان کارمند به سازمان
              <strong>{{ organizationName }}</strong> اضافه خواهید شد.
            </p>
          </div>
        </div>

        <!-- Submit Button -->
        <div class="form-actions">
          <AppButton
            type="submit"
            variant="primary"
            size="large"
            :disabled="!isFormValid || isSubmitting"
            :loading="isSubmitting"
            full-width
          >
            {{ isSubmitting ? 'در حال ثبت...' : 'تکمیل و پذیرش دعوت' }}
          </AppButton>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive } from 'vue'
import { useHierarchyStore } from '../../stores/hierarchy.store'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'

interface Props {
  invitationId: string
  organizationId?: string
  organizationName?: string
}

interface Emits {
  (e: 'completed'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const hierarchyStore = useHierarchyStore()

const formData = reactive({
  firstName: '',
  lastName: '',
  email: '',
  bio: '',
  avatarUrl: '',
})

const errors = reactive({
  firstName: '',
  lastName: '',
  email: '',
})

const isSubmitting = ref(false)

const bioLength = computed(() => formData.bio.length)

const isFormValid = computed(() => {
  return (
    formData.firstName.trim().length > 0 &&
    formData.lastName.trim().length > 0 &&
    formData.email.trim().length > 0 &&
    isValidEmail(formData.email) &&
    !errors.firstName &&
    !errors.lastName &&
    !errors.email
  )
})

function validateField(field: keyof typeof errors) {
  switch (field) {
    case 'firstName':
      if (!formData.firstName.trim()) {
        errors.firstName = 'نام الزامی است'
      } else if (formData.firstName.length < 2) {
        errors.firstName = 'نام باید حداقل 2 کاراکتر باشد'
      } else {
        errors.firstName = ''
      }
      break

    case 'lastName':
      if (!formData.lastName.trim()) {
        errors.lastName = 'نام خانوادگی الزامی است'
      } else if (formData.lastName.length < 2) {
        errors.lastName = 'نام خانوادگی باید حداقل 2 کاراکتر باشد'
      } else {
        errors.lastName = ''
      }
      break

    case 'email':
      if (!formData.email.trim()) {
        errors.email = 'ایمیل الزامی است'
      } else if (!isValidEmail(formData.email)) {
        errors.email = 'فرمت ایمیل نامعتبر است'
      } else {
        errors.email = ''
      }
      break
  }
}

function isValidEmail(email: string): boolean {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email)
}

async function handleSubmit() {
  // Validate all fields
  validateField('firstName')
  validateField('lastName')
  validateField('email')

  if (!isFormValid.value || !props.organizationId) return

  isSubmitting.value = true

  try {
    await hierarchyStore.acceptInvitation(props.organizationId, props.invitationId, {
      invitationId: props.invitationId,
      firstName: formData.firstName,
      lastName: formData.lastName,
      email: formData.email,
      bio: formData.bio || undefined,
      avatarUrl: formData.avatarUrl || undefined,
    })

    emit('completed')
  } catch (error) {
    console.error('Error completing staff profile:', error)
    alert('خطا در ثبت اطلاعات. لطفاً دوباره تلاش کنید.')
  } finally {
    isSubmitting.value = false
  }
}
</script>

<style scoped lang="scss">
.complete-staff-profile {
  margin-top: 2rem;
}

.profile-wizard {
  .wizard-title {
    font-size: 1.25rem;
    color: #1a202c;
    margin-bottom: 0.5rem;
    font-weight: 600;
  }

  .wizard-description {
    color: #718096;
    margin-bottom: 2rem;
    line-height: 1.6;
  }
}

.profile-form {
  .form-group {
    margin-bottom: 1.5rem;

    .form-label {
      display: block;
      font-weight: 500;
      margin-bottom: 0.5rem;
      color: #2d3748;
      font-size: 0.95rem;

      .required {
        color: #f56565;
        margin-right: 0.25rem;
      }
    }

    .form-input,
    .form-textarea {
      width: 100%;
      padding: 0.75rem 1rem;
      border: 1px solid #cbd5e0;
      border-radius: 8px;
      font-size: 1rem;
      transition: all 0.2s;
      font-family: inherit;

      &:focus {
        outline: none;
        border-color: #667eea;
        box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
      }

      &::placeholder {
        color: #a0aec0;
      }

      &.form-input-error {
        border-color: #f56565;
      }
    }

    .form-textarea {
      resize: vertical;
      min-height: 100px;
    }

    .form-error {
      display: block;
      color: #f56565;
      font-size: 0.875rem;
      margin-top: 0.25rem;
    }

    .form-hint {
      display: block;
      color: #a0aec0;
      font-size: 0.875rem;
      margin-top: 0.25rem;
    }

    .textarea-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-top: 0.25rem;

      .char-count {
        font-size: 0.875rem;
        color: #a0aec0;
      }
    }
  }

  .info-box {
    background: #ebf8ff;
    border: 1px solid #90cdf4;
    border-radius: 8px;
    padding: 1rem;
    margin-bottom: 1.5rem;
    display: flex;
    align-items: flex-start;
    gap: 0.75rem;

    i {
      color: #3182ce;
      font-size: 1.25rem;
      margin-top: 0.125rem;
    }

    .info-content {
      flex: 1;
      color: #2c5282;
      font-size: 0.95rem;
      line-height: 1.6;

      p {
        margin: 0;
      }

      strong {
        font-weight: 600;
      }
    }
  }

  .form-actions {
    margin-top: 2rem;
  }
}

@media (max-width: 640px) {
  .profile-form {
    .form-group {
      margin-bottom: 1.25rem;
    }
  }
}
</style>
