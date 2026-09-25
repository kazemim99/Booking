<template>
  <form @submit.prevent="onFormSubmit" class="reset-password-form">
    <h2 class="form-title">Reset Password</h2>
    <p class="form-subtitle">Enter your new password below</p>

    <div class="form-group">
      <label for="password" class="form-label">New Password</label>
      <div class="password-input-wrapper">
        <input
          id="password"
          v-model="formData.password"
          :type="showPassword ? 'text' : 'password'"
          class="form-input"
          :class="{ 'form-input-error': getFieldError('password') }"
          placeholder="Create a strong password"
          @blur="setFieldTouched('password')"
        />
        <button type="button" class="password-toggle" @click="showPassword = !showPassword">
          {{ showPassword ? '👁️' : '👁️‍🗨️' }}
        </button>
      </div>
      <span v-if="getFieldError('password')" class="form-error">
        {{ getFieldError('password') }}
      </span>
    </div>

    <div class="form-group">
      <label for="confirmPassword" class="form-label">Confirm New Password</label>
      <input
        id="confirmPassword"
        v-model="formData.confirmPassword"
        :type="showPassword ? 'text' : 'password'"
        class="form-input"
        :class="{ 'form-input-error': getFieldError('confirmPassword') }"
        placeholder="Confirm your password"
        @blur="setFieldTouched('confirmPassword')"
      />
      <span v-if="getFieldError('confirmPassword')" class="form-error">
        {{ getFieldError('confirmPassword') }}
      </span>
    </div>

    <button type="submit" class="btn btn-primary btn-block" :disabled="!isValid || isSubmitting">
      <span v-if="isSubmitting">Resetting...</span>
      <span v-else>Reset Password</span>
    </button>
  </form>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRoute } from 'vue-router'
import { useForm } from '@/core/composables/useForm'
import { usePasswordReset } from '../composables/usePasswordReset'
import { VALIDATION_RULES, ERROR_MESSAGES } from '@/core/constants/validation.constants'

type ResetPasswordFormData = Record<string, unknown> & {
  password: string
  confirmPassword: string
}

const route = useRoute()
const { resetPassword, isLoading: isSubmitting } = usePasswordReset()
const showPassword = ref(false)

const token = route.params.token as string

const initialValues: ResetPasswordFormData = {
  password: '',
  confirmPassword: '',
}

const validations = {
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
}

const { formData, isValid, setFieldTouched, getFieldError, handleSubmit } =
  useForm<ResetPasswordFormData>(initialValues, validations)

async function onSubmit(values: ResetPasswordFormData) {
  await resetPassword({
    token,
    password: values.password,
    confirmPassword: values.confirmPassword,
  })
}

// Wrapper function for form submit
async function onFormSubmit() {
  await handleSubmit(onSubmit)
}
</script>

<style scoped lang="scss">
@import './auth-form-styles.scss';
</style>
