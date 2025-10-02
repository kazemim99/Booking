<template>
  <form @submit.prevent="onFormSubmit" class="forgot-password-form">
    <div v-if="!emailSent">
      <h2 class="form-title">Forgot Password?</h2>
      <p class="form-subtitle">
        Enter your email address and we'll send you instructions to reset your password.
      </p>

      <div class="form-group">
        <label for="email" class="form-label">Email Address</label>
        <input
          id="email"
          v-model="formData.email"
          type="email"
          class="form-input"
          :class="{ 'form-input-error': getFieldError('email') }"
          placeholder="you@example.com"
          @blur="setFieldTouched('email')"
        />
        <span v-if="getFieldError('email')" class="form-error">
          {{ getFieldError('email') }}
        </span>
      </div>

      <button type="submit" class="btn btn-primary btn-block" :disabled="!isValid || isSubmitting">
        <span v-if="isSubmitting">Sending...</span>
        <span v-else>Send Reset Link</span>
      </button>

      <div class="form-footer">
        <router-link to="/login" class="link-primary"> ← Back to login </router-link>
      </div>
    </div>

    <div v-else class="success-message">
      <div class="success-icon">✉️</div>
      <h2 class="form-title">Check Your Email</h2>
      <p class="form-subtitle">
        We've sent password reset instructions to <strong>{{ formData.email }}</strong>
      </p>
      <p class="help-text">
        Didn't receive the email? Check your spam folder or
        <button type="button" @click="handleResend" class="link-button">resend</button>
      </p>
      <router-link to="/login" class="btn btn-secondary btn-block"> Back to Login </router-link>
    </div>
  </form>
</template>

<script setup lang="ts">
import { useForm } from '@/core/composables/useForm'
import { usePasswordReset } from '../composables/usePasswordReset'
import { VALIDATION_RULES, ERROR_MESSAGES } from '@/core/constants/validation.constants'

type ForgotPasswordFormData = Record<string, unknown> & {
  email: string
}

const { requestPasswordReset, isLoading: isSubmitting, emailSent } = usePasswordReset()

const initialValues: ForgotPasswordFormData = {
  email: '',
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
}

const { formData, isValid, setFieldTouched, getFieldError, handleSubmit } =
  useForm<ForgotPasswordFormData>(initialValues, validations)

async function onSubmit(values: ForgotPasswordFormData) {
  await requestPasswordReset({ email: values.email })
}

async function handleResend() {
  await requestPasswordReset({ email: formData.email })
}

// Wrapper function for form submit
async function onFormSubmit() {
  await handleSubmit(onSubmit)
}
</script>

<style scoped lang="scss">
@import './auth-form-styles.scss';

.success-message {
  text-align: center;
}

.success-icon {
  font-size: 4rem;
  margin-bottom: 1rem;
}

.help-text {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 1.5rem 0;
}

.link-button {
  background: none;
  border: none;
  color: #667eea;
  font-weight: 600;
  cursor: pointer;
  text-decoration: underline;

  &:hover {
    color: #764ba2;
  }
}

.btn-secondary {
  background: #e5e7eb;
  color: #374151;

  &:hover:not(:disabled) {
    background: #d1d5db;
  }
}
</style>
