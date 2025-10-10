<template>
  <form @submit.prevent="onFormSubmit" class="auth-form">
    <h2 class="form-title">{{ $t('auth.createAccount') }}</h2>
    <p class="form-subtitle">{{ $t('auth.joinToday') }}</p>

    <!-- Error Alert -->
    <Alert
      v-if="authStore.error && authStore.error !== 'Please correct the validation errors'"
      type="error"
      :message="authStore.error"
      @dismiss="authStore.clearError"
    />

    <!-- Validation Summary -->
    <ValidationSummary
      v-if="hasDescriptiveValidationErrors"
      :errors="allErrors"
      :title="$t('validation.checkFollowing')"
    />

    <!-- Name Row -->
    <div class="form-row">
      <AppTextInput
        v-model="formData.firstName"
        type="text"
        :label="$t('auth.firstName')"
        :placeholder="$t('auth.firstNamePlaceholder')"
        :error="getFieldError('firstName') ?? undefined"
        :disabled="isSubmitting"
        required
        autocomplete="given-name"
        @blur="setFieldTouched('firstName')"
        @input="() => getFieldError('firstName') && validateField('firstName')"
      />

      <AppTextInput
        v-model="formData.lastName"
        type="text"
        :label="$t('auth.lastName')"
        :placeholder="$t('auth.lastNamePlaceholder')"
        :error="getFieldError('lastName')"
        :disabled="isSubmitting"
        required
        autocomplete="family-name"
        @blur="setFieldTouched('lastName')"
        @input="() => getFieldError('lastName') && validateField('lastName')"
      />
    </div>

    <!-- Email Field -->
    <AppTextInput
      v-model="formData.email"
      type="email"
      :label="$t('auth.emailAddress')"
      :placeholder="$t('auth.emailPlaceholder')"
      :error="getFieldError('email') ?? undefined"
      :disabled="isSubmitting"
      required
      autocomplete="email"
      @blur="setFieldTouched('email')"
      @input="() => getFieldError('email') && validateField('email')"
    />

    <!-- Phone Number Field (Optional) -->
    <AppTextInput
      v-model="formData.phoneNumber"
      type="tel"
      :label="$t('auth.phoneNumber')"
      :placeholder="$t('auth.phonePlaceholder')"
      :error="getFieldError('phoneNumber') ?? undefined"
      :disabled="isSubmitting"
      :hint="$t('auth.optional')"
      autocomplete="tel"
      @blur="setFieldTouched('phoneNumber')"
      @input="() => getFieldError('phoneNumber') && validateField('phoneNumber')"
    />

    <!-- Password Field -->
    <AppTextInput
      v-model="formData.password"
      :type="showPassword ? 'text' : 'password'"
      :label="$t('auth.password')"
      :placeholder="$t('auth.createPasswordPlaceholder')"
      :error="getFieldError('password') ?? undefined"
      :disabled="isSubmitting"
      required
      autocomplete="new-password"
      @blur="setFieldTouched('password')"
      @input="() => getFieldError('password') && validateField('password')"
    >
      <template #suffix>
        <button
          type="button"
          class="password-toggle-btn"
          @click="showPassword = !showPassword"
          :disabled="isSubmitting"
        >
          {{ showPassword ? '👁️' : '👁️‍🗨️' }}
        </button>
      </template>
    </AppTextInput>

    <!-- Confirm Password Field -->
    <AppTextInput
      v-model="formData.confirmPassword"
      :type="showPassword ? 'text' : 'password'"
      :label="$t('auth.confirmPassword')"
      :placeholder="$t('auth.confirmPasswordPlaceholder')"
      :error="getFieldError('confirmPassword') ?? undefined"
      :disabled="isSubmitting"
      required
      autocomplete="new-password"
      @blur="setFieldTouched('confirmPassword')"
      @input="() => getFieldError('confirmPassword') && validateField('confirmPassword')"
    />

    <!-- User Type Selection -->
    <div class="form-group">
      <label class="form-label">{{ $t('auth.userType') }}</label>
      <div class="user-type-options">
        <label class="radio-option" :class="{ active: formData.userType === UserType.Client }">
          <input
            v-model="formData.userType"
            type="radio"
            value="Customer"
            class="radio-input"
            :disabled="isSubmitting"
          />
          <div class="radio-content">
            <span class="radio-icon">👤</span>
            <span class="radio-label">{{ $t('auth.customer') }}</span>
          </div>
        </label>

        <label class="radio-option" :class="{ active: formData.userType === 'Provider' }">
          <input
            v-model="formData.userType"
            type="radio"
            value="Provider"
            class="radio-input"
            :disabled="isSubmitting"
          />
          <div class="radio-content">
            <span class="radio-icon">💼</span>
            <span class="radio-label">{{ $t('auth.provider') }}</span>
          </div>
        </label>
      </div>
    </div>

    <!-- Terms Acceptance -->
    <label class="checkbox-wrapper terms-wrapper">
      <input
        v-model="formData.acceptTerms"
        type="checkbox"
        class="form-checkbox"
        :disabled="isSubmitting"
      />
      <span>
        {{ $t('auth.iAccept') }}
        <router-link to="/terms" class="link-primary" target="_blank">
          {{ $t('auth.termsConditions') }}
        </router-link>
      </span>
    </label>
    <ValidationError :error="getFieldError('acceptTerms') ?? undefined" />

    <!-- Submit Button -->
    <AppButton
      type="submit"
      variant="primary"
      size="large"
      :loading="isSubmitting"
      :disabled="!isValid || isSubmitting"
      block
    >
      {{ isSubmitting ? $t('auth.creatingAccount') : $t('auth.createAccount') }}
    </AppButton>

    <!-- Footer -->
    <div class="form-footer">
      <p>
        {{ $t('auth.alreadyHaveAccount') }}
        <router-link to="/login" class="link-primary">
          {{ $t('auth.signIn') }}
        </router-link>
      </p>
    </div>
  </form>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useForm } from '../../../core/composables/useForm'
import { useRegister } from '../composables/useRegister'
import { useAuthStore } from '../../../core/stores/modules/auth.store'
import { VALIDATION_RULES, ERROR_MESSAGES } from '../../../core/constants/validation.constants'
import AppTextInput from '../../../shared/components/ui/Input/AppTextInput.vue'
import AppButton from '../../../shared/components/ui/Button/AppButton.vue'
import ValidationError from '../../../shared/components/ui/ValidationError.vue'
import ValidationSummary from '../../../shared/components/ui/ValidationSummary.vue'
import Alert from '../../../shared/components/ui/AppAlert.vue'
import type { RegisterFormData } from '../types/register.types'
import { UserType } from '@/modules/user-management/types/user.types'

const { t: $t } = useI18n()
const { register } = useRegister()
const authStore = useAuthStore()
const showPassword = ref(false)

const initialValues: RegisterFormData = {
  email: '',
  password: '',
  confirmPassword: '',
  firstName: '',
  lastName: '',
  phoneNumber: '',
  userType: UserType.Client,
  acceptTerms: false,
}

const validations = {
  firstName: (value: unknown): string | null => {
    const name = value as string
    if (!name) return ERROR_MESSAGES.REQUIRED
    if (name.length < 2) return ERROR_MESSAGES.MIN_LENGTH(2)
    return null
  },
  lastName: (value: unknown): string | null => {
    const name = value as string
    if (!name) return ERROR_MESSAGES.REQUIRED
    if (name.length < 2) return ERROR_MESSAGES.MIN_LENGTH(2)
    return null
  },
  email: (value: unknown): string | null => {
    const email = value as string
    if (!email) return ERROR_MESSAGES.REQUIRED
    if (!VALIDATION_RULES.EMAIL.PATTERN.test(email)) {
      return VALIDATION_RULES.EMAIL.MESSAGE
    }
    return null
  },
  phoneNumber: (value: unknown): string | null => {
    const phone = value as string
    if (phone && !VALIDATION_RULES.PHONE.PATTERN.test(phone)) {
      return VALIDATION_RULES.PHONE.MESSAGE
    }
    return null
  },
  password: (value: unknown): string | null => {
    const password = value as string
    if (!password) return ERROR_MESSAGES.REQUIRED
    if (password.length < VALIDATION_RULES.PASSWORD.MIN_LENGTH) {
      return ERROR_MESSAGES.MIN_LENGTH(VALIDATION_RULES.PASSWORD.MIN_LENGTH)
    }
    if (!VALIDATION_RULES.PASSWORD.PATTERN.test(password)) {
      return VALIDATION_RULES.PASSWORD.MESSAGE
    }
    return null
  },
  confirmPassword: (value: unknown): string | null => {
    const confirmPassword = value as string
    if (!confirmPassword) return ERROR_MESSAGES.REQUIRED
    if (confirmPassword !== formData.password) {
      return ERROR_MESSAGES.PASSWORDS_MISMATCH
    }
    return null
  },
  acceptTerms: (value: unknown): string | null => {
    const accepted = value as boolean
    if (!accepted) return 'You must accept the terms and conditions'
    return null
  },
}

const {
  formData,
  isValid,
  isSubmitting,
  serverErrors,
  setFieldTouched,
  getFieldError,
  validateField,
  handleSubmit,
  setServerErrors,
} = useForm<RegisterFormData>(initialValues, validations)

const hasDescriptiveValidationErrors = computed(() => {
  const errors = { ...serverErrors.value, ...authStore.validationErrors }
  if (Object.keys(errors).length === 0) return false

  return Object.values(errors).some(
    (errorArray) =>
      Array.isArray(errorArray) &&
      errorArray.some((msg) => {
        if (/@/.test(msg) && !msg.includes(' ')) return false
        if (/^https?:\/\//.test(msg)) return false
        if (!msg.includes(' ') && msg.length < 30) return false
        return msg.includes(' ') || msg.length > 50
      }),
  )
})

const allErrors = computed(() => ({
  ...serverErrors.value,
  ...authStore.validationErrors,
}))

async function onSubmit(values: RegisterFormData) {
  const success = await register({
    email: values.email,
    password: values.password,
    firstName: values.firstName,
    lastName: values.lastName,
    phoneNumber: values.phoneNumber || undefined,
    userType: values.userType,
    confirmPassword: values.confirmPassword,
    acceptTerms: values.acceptTerms,
  })

  if (!success && Object.keys(authStore.validationErrors).length > 0) {
    setServerErrors(authStore.validationErrors)
  }
}

async function onFormSubmit() {
  try {
    await handleSubmit(onSubmit)
  } catch (err) {
    console.error('Form submission error:', err)
  }
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/rtl';

.auth-form {
  width: 100%;
  max-width: 500px;
  margin: 0 auto;
}

.form-title {
  font-size: 1.875rem;
  font-weight: 700;
  color: #1f2937;
  margin-bottom: 0.5rem;
  text-align: center;

  @include rtl {
    font-family: 'Cairo', 'Tajawal', sans-serif;
  }
}

.form-subtitle {
  font-size: 0.875rem;
  color: #6b7280;
  margin-bottom: 2rem;
  text-align: center;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  margin-bottom: 0;

  @media (max-width: 640px) {
    grid-template-columns: 1fr;
  }
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
  margin-bottom: 0.5rem;

  @include text-align-start;
}

.user-type-options {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  margin-top: 0.5rem;
}

.radio-option {
  position: relative;
  cursor: pointer;

  &.active .radio-content {
    border-color: #667eea;
    background: rgba(102, 126, 234, 0.05);
  }

  &:hover .radio-content {
    border-color: #667eea;
  }
}

.radio-input {
  position: absolute;
  opacity: 0;
  width: 0;
  height: 0;
}

.radio-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 1.5rem 1rem;
  border: 2px solid #d1d5db;
  border-radius: 8px;
  transition: all 0.2s;
}

.radio-icon {
  font-size: 2rem;
  margin-bottom: 0.5rem;
}

.radio-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}

.checkbox-wrapper {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #374151;
  cursor: pointer;

  &.terms-wrapper {
    margin: 1rem 0;
  }
}

.form-checkbox {
  width: 1rem;
  height: 1rem;
  margin-top: 0.125rem;
  cursor: pointer;
  accent-color: #667eea;
  flex-shrink: 0;

  &:disabled {
    cursor: not-allowed;
  }
}

.link-primary {
  color: #667eea;
  text-decoration: none;
  font-weight: 500;

  &:hover {
    color: #764ba2;
    text-decoration: underline;
  }
}

.form-footer {
  margin-top: 1.5rem;
  text-align: center;
  font-size: 0.875rem;
  color: #6b7280;

  p {
    margin: 0;
  }
}

.password-toggle-btn {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 1.25rem;
  opacity: 0.6;
  transition: opacity 0.2s;
  padding: 0;

  &:hover:not(:disabled) {
    opacity: 1;
  }

  &:disabled {
    cursor: not-allowed;
    opacity: 0.3;
  }
}
</style>
