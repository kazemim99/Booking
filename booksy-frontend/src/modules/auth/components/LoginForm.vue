<template>
  <form @submit.prevent="onFormSubmit" class="auth-form">
    <h2 class="form-title">{{ $t('auth.welcomeBack') }}</h2>
    <p class="form-subtitle">{{ $t('auth.signInToAccount') }}</p>

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

    <!-- Email Field using AppTextInput -->
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

    <!-- Password Field using AppTextInput -->
    <AppTextInput
      v-model="formData.password"
      :type="showPassword ? 'text' : 'password'"
      :label="$t('auth.password')"
      :placeholder="$t('auth.passwordPlaceholder')"
      :error="getFieldError('password') ?? undefined"
      :disabled="isSubmitting"
      required
      autocomplete="current-password"
      @blur="setFieldTouched('password')"
      @input="() => getFieldError('password') && validateField('password')"
    >
      <template #suffix>
        <button
          type="button"
          class="password-toggle-btn"
          @click="showPassword = !showPassword"
          :disabled="isSubmitting"
          :aria-label="showPassword ? 'Hide password' : 'Show password'"
        >
          {{ showPassword ? '👁️' : '👁️‍🗨️' }}
        </button>
      </template>
    </AppTextInput>

    <!-- Remember Me & Forgot Password Row -->
    <div class="form-row">
      <label class="checkbox-wrapper">
        <input
          v-model="formData.rememberMe"
          type="checkbox"
          class="form-checkbox"
          :disabled="isSubmitting"
        />
        <span>{{ $t('auth.rememberMe') }}</span>
      </label>
      <router-link to="/forgot-password" class="link-primary">
        {{ $t('auth.forgotPassword') }}
      </router-link>
    </div>

    <!-- Submit Button using AppButton -->
    <AppButton
      type="submit"
      variant="primary"
      size="large"
      :loading="isSubmitting"
      :disabled="!isValid || isSubmitting"
      block
    >
      {{ isSubmitting ? $t('auth.signingIn') : $t('auth.signIn') }}
    </AppButton>

    <!-- Footer -->
    <div class="form-footer">
      <p>
        {{ $t('auth.noAccount') }}
        <router-link to="/register" class="link-primary">
          {{ $t('auth.signUp') }}
        </router-link>
      </p>
    </div>
  </form>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useForm } from '@/core/composables/useForm'
import { useLogin } from '../composables/useLogin'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { VALIDATION_RULES, ERROR_MESSAGES } from '@/core/constants/validation.constants'
import AppTextInput from '@/shared/components/ui/Input/AppTextInput.vue'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import ValidationSummary from '@/shared/components/ui/ValidationSummary.vue'
import Alert from '@/shared/components/ui/AppAlert.vue'
import type { LoginFormData } from '../types/login.types'

const { login } = useLogin()
const authStore = useAuthStore()
const showPassword = ref(false)

const initialValues: LoginFormData = {
  email: '',
  password: '',
  rememberMe: false,
}

const validations = {
  email: (value: unknown): string | null => {
    const email = value as string
    if (!email) return ERROR_MESSAGES.REQUIRED
    if (!VALIDATION_RULES.EMAIL.PATTERN.test(email)) {
      return VALIDATION_RULES.EMAIL.MESSAGE
    }
    return null
  },
  password: (value: unknown): string | null => {
    const password = value as string
    if (!password) return ERROR_MESSAGES.REQUIRED
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
} = useForm<LoginFormData>(initialValues, validations)

const hasDescriptiveValidationErrors = computed(() => {
  const errors = { ...serverErrors.value, ...authStore.validationErrors }
  if (Object.keys(errors).length === 0) return false

  return Object.values(errors).some((errorArray) =>
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

async function onSubmit(values: LoginFormData) {
  const success = await login({
    email: values.email,
    password: values.password,
    rememberMe: values.rememberMe,
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
  max-width: 400px;
  margin: 0 auto;
}

.form-title {
  font-size: 1.875rem;
  font-weight: 700;
  color: #1f2937;
  margin-bottom: 0.5rem;
  text-align: center;

  @include rtl {
    font-family: 'Cairo', 'Tajawal', sans-serif; // RTL fonts
  }
}

.form-subtitle {
  font-size: 0.875rem;
  color: #6b7280;
  margin-bottom: 2rem;
  text-align: center;
}

.form-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin: 1rem 0 1.5rem;
  gap: 1rem;

  @include flex-direction-row;
}

.checkbox-wrapper {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #374151;
  cursor: pointer;
}

.form-checkbox {
  width: 1rem;
  height: 1rem;
  cursor: pointer;
  accent-color: #667eea;

  &:disabled {
    cursor: not-allowed;
  }
}

.link-primary {
  font-size: 0.875rem;
  color: #667eea;
  text-decoration: none;
  font-weight: 500;
  white-space: nowrap;

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
  display: flex;
  align-items: center;

  &:hover:not(:disabled) {
    opacity: 1;
  }

  &:disabled {
    cursor: not-allowed;
    opacity: 0.3;
  }
}
</style>
