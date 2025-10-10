<template>
  <div class="security-tab">
    <div class="tab-header">
      <div>
        <h2 class="tab-title">Security Settings</h2>
        <p class="tab-description">Manage your password and account security</p>
      </div>
    </div>

    <!-- Change Password Section -->
    <div class="form-section">
      <h3 class="section-title">Change Password</h3>
      <p class="section-description">Choose a strong password to keep your account secure</p>

      <form @submit.prevent="handleChangePassword" class="password-form">
        <div class="form-grid">
          <!-- Current Password -->
          <div class="form-group form-group-full">
            <label for="currentPassword" class="form-label required"> Current Password </label>
            <div class="password-input-wrapper">
              <input
                id="currentPassword"
                v-model="passwordForm.currentPassword"
                :type="showCurrentPassword ? 'text' : 'password'"
                class="form-input"
                :class="{ 'input-error': errors.currentPassword }"
                placeholder="Enter your current password"
                autocomplete="current-password"
              />
              <button
                type="button"
                @click="showCurrentPassword = !showCurrentPassword"
                class="password-toggle"
                tabindex="-1"
              >
                <svg
                  v-if="showCurrentPassword"
                  width="20"
                  height="20"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                >
                  <path
                    d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"
                  ></path>
                  <line x1="1" y1="1" x2="23" y2="23"></line>
                </svg>
                <svg
                  v-else
                  width="20"
                  height="20"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                >
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                  <circle cx="12" cy="12" r="3"></circle>
                </svg>
              </button>
            </div>
            <span v-if="errors.currentPassword" class="error-message">
              {{ errors.currentPassword }}
            </span>
          </div>

          <!-- New Password -->
          <div class="form-group">
            <label for="newPassword" class="form-label required"> New Password </label>
            <div class="password-input-wrapper">
              <input
                id="newPassword"
                v-model="passwordForm.newPassword"
                :type="showNewPassword ? 'text' : 'password'"
                class="form-input"
                :class="{ 'input-error': errors.newPassword }"
                placeholder="Enter new password"
                autocomplete="new-password"
                @input="validatePasswordStrength"
              />
              <button
                type="button"
                @click="showNewPassword = !showNewPassword"
                class="password-toggle"
                tabindex="-1"
              >
                <svg
                  v-if="showNewPassword"
                  width="20"
                  height="20"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                >
                  <path
                    d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"
                  ></path>
                  <line x1="1" y1="1" x2="23" y2="23"></line>
                </svg>
                <svg
                  v-else
                  width="20"
                  height="20"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                >
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                  <circle cx="12" cy="12" r="3"></circle>
                </svg>
              </button>
            </div>
            <span v-if="errors.newPassword" class="error-message">
              {{ errors.newPassword }}
            </span>

            <!-- Password Strength Indicator -->
            <div v-if="passwordForm.newPassword" class="password-strength">
              <div class="strength-bar">
                <div
                  class="strength-fill"
                  :class="passwordStrength.class"
                  :style="{ width: passwordStrength.percentage + '%' }"
                ></div>
              </div>
              <span class="strength-label" :class="passwordStrength.class">
                {{ passwordStrength.label }}
              </span>
            </div>

            <!-- Password Requirements -->
            <div class="password-requirements">
              <div class="requirement" :class="{ met: requirements.minLength }">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M9 16.2L4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z" />
                </svg>
                <span>At least 8 characters</span>
              </div>
              <div class="requirement" :class="{ met: requirements.hasUpperCase }">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M9 16.2L4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z" />
                </svg>
                <span>One uppercase letter</span>
              </div>
              <div class="requirement" :class="{ met: requirements.hasLowerCase }">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M9 16.2L4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z" />
                </svg>
                <span>One lowercase letter</span>
              </div>
              <div class="requirement" :class="{ met: requirements.hasNumber }">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M9 16.2L4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z" />
                </svg>
                <span>One number</span>
              </div>
              <div class="requirement" :class="{ met: requirements.hasSpecialChar }">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M9 16.2L4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z" />
                </svg>
                <span>One special character (!@#$%)</span>
              </div>
            </div>
          </div>

          <!-- Confirm Password -->
          <div class="form-group">
            <label for="confirmPassword" class="form-label required"> Confirm New Password </label>
            <div class="password-input-wrapper">
              <input
                id="confirmPassword"
                v-model="passwordForm.confirmPassword"
                :type="showConfirmPassword ? 'text' : 'password'"
                class="form-input"
                :class="{ 'input-error': errors.confirmPassword }"
                placeholder="Confirm new password"
                autocomplete="new-password"
              />
              <button
                type="button"
                @click="showConfirmPassword = !showConfirmPassword"
                class="password-toggle"
                tabindex="-1"
              >
                <svg
                  v-if="showConfirmPassword"
                  width="20"
                  height="20"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                >
                  <path
                    d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"
                  ></path>
                  <line x1="1" y1="1" x2="23" y2="23"></line>
                </svg>
                <svg
                  v-else
                  width="20"
                  height="20"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                >
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                  <circle cx="12" cy="12" r="3"></circle>
                </svg>
              </button>
            </div>
            <span v-if="errors.confirmPassword" class="error-message">
              {{ errors.confirmPassword }}
            </span>
          </div>
        </div>

        <!-- Form Actions -->
        <div class="form-actions">
          <button
            type="button"
            class="btn btn-secondary"
            @click="resetPasswordForm"
            :disabled="isSaving"
          >
            Cancel
          </button>
          <button
            type="submit"
            class="btn btn-primary"
            :disabled="isSaving || !isPasswordFormValid"
          >
            <span v-if="isSaving" class="btn-spinner"></span>
            {{ isSaving ? 'Changing Password...' : 'Change Password' }}
          </button>
        </div>
      </form>
    </div>

    <!-- Security Tips -->
    <div class="security-tips">
      <div class="tip-header">
        <svg
          width="20"
          height="20"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
        >
          <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path>
        </svg>
        <span>Security Best Practices</span>
      </div>
      <ul class="tip-list">
        <li>Use a unique password that you don't use for other websites</li>
        <li>Consider using a password manager to generate and store strong passwords</li>
        <li>Never share your password with anyone</li>
        <li>Change your password regularly (every 3-6 months)</li>
        <li>If you suspect unauthorized access, change your password immediately</li>
      </ul>
    </div>

    <!-- Recent Activity -->
    <div class="form-section">
      <h3 class="section-title">Recent Activity</h3>
      <div class="activity-list">
        <div class="activity-item">
          <div class="activity-icon">
            <svg
              width="20"
              height="20"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
              <circle cx="12" cy="7" r="4"></circle>
            </svg>
          </div>
          <div class="activity-details">
            <div class="activity-title">Profile Updated</div>
            <div class="activity-time">2 hours ago</div>
          </div>
        </div>
        <div class="activity-item">
          <div class="activity-icon">
            <svg
              width="20"
              height="20"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4"></path>
              <polyline points="10 17 15 12 10 7"></polyline>
              <line x1="15" y1="12" x2="3" y2="12"></line>
            </svg>
          </div>
          <div class="activity-details">
            <div class="activity-title">Logged in from Chrome</div>
            <div class="activity-time">Today at 8:15 AM</div>
          </div>
        </div>
        <div class="activity-item">
          <div class="activity-icon">
            <svg
              width="20"
              height="20"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
              <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
            </svg>
          </div>
          <div class="activity-details">
            <div class="activity-title">Password Changed</div>
            <div class="activity-time">3 days ago</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import type {
  ChangePasswordRequest,
  ValidationErrors,
} from '@/modules/user-management/types/user-profile.types'

// Props
defineProps<{
  isSaving: boolean
}>()

// Emits
const emit = defineEmits<{
  changePassword: [data: ChangePasswordRequest]
}>()

// Form state
const passwordForm = ref<ChangePasswordRequest>({
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
})

const errors = ref<ValidationErrors>({})
const showCurrentPassword = ref(false)
const showNewPassword = ref(false)
const showConfirmPassword = ref(false)

// Password requirements state
const requirements = ref({
  minLength: false,
  hasUpperCase: false,
  hasLowerCase: false,
  hasNumber: false,
  hasSpecialChar: false,
})

// Computed
const passwordStrength = computed(() => {
  const password = passwordForm.value.newPassword
  if (!password) {
    return { percentage: 0, label: '', class: '' }
  }

  let strength = 0

  if (requirements.value.minLength) strength += 20
  if (requirements.value.hasUpperCase) strength += 20
  if (requirements.value.hasLowerCase) strength += 20
  if (requirements.value.hasNumber) strength += 20
  if (requirements.value.hasSpecialChar) strength += 20

  if (strength <= 40) {
    return { percentage: strength, label: 'Weak', class: 'weak' }
  } else if (strength <= 60) {
    return { percentage: strength, label: 'Fair', class: 'fair' }
  } else if (strength <= 80) {
    return { percentage: strength, label: 'Good', class: 'good' }
  } else {
    return { percentage: strength, label: 'Strong', class: 'strong' }
  }
})

const isPasswordFormValid = computed(() => {
  return (
    passwordForm.value.currentPassword.length > 0 &&
    passwordForm.value.newPassword.length >= 8 &&
    passwordForm.value.confirmPassword.length > 0 &&
    passwordForm.value.newPassword === passwordForm.value.confirmPassword &&
    Object.values(requirements.value).every((req) => req === true)
  )
})

// Methods
function validatePasswordStrength() {
  const password = passwordForm.value.newPassword

  requirements.value = {
    minLength: password.length >= 8,
    hasUpperCase: /[A-Z]/.test(password),
    hasLowerCase: /[a-z]/.test(password),
    hasNumber: /\d/.test(password),
    hasSpecialChar: /[!@#$%^&*(),.?":{}|<>]/.test(password),
  }
}

function validatePasswordForm(): boolean {
  errors.value = {}

  if (!passwordForm.value.currentPassword) {
    errors.value.currentPassword = 'Current password is required'
  }

  if (!passwordForm.value.newPassword) {
    errors.value.newPassword = 'New password is required'
  } else if (passwordForm.value.newPassword.length < 8) {
    errors.value.newPassword = 'Password must be at least 8 characters'
  } else if (!Object.values(requirements.value).every((req) => req === true)) {
    errors.value.newPassword = 'Password does not meet all requirements'
  }

  if (!passwordForm.value.confirmPassword) {
    errors.value.confirmPassword = 'Please confirm your new password'
  } else if (passwordForm.value.newPassword !== passwordForm.value.confirmPassword) {
    errors.value.confirmPassword = 'Passwords do not match'
  }

  return Object.keys(errors.value).length === 0
}

function handleChangePassword() {
  if (!validatePasswordForm()) {
    return
  }

  emit('changePassword', {
    currentPassword: passwordForm.value.currentPassword,
    newPassword: passwordForm.value.newPassword,
    confirmPassword: passwordForm.value.confirmPassword,
  })

  // Reset form on successful submission
  resetPasswordForm()
}

function resetPasswordForm() {
  passwordForm.value = {
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  }
  errors.value = {}
  requirements.value = {
    minLength: false,
    hasUpperCase: false,
    hasLowerCase: false,
    hasNumber: false,
    hasSpecialChar: false,
  }
}
</script>

<style scoped lang="scss">
.security-tab {
  max-width: 900px;
}

.tab-header {
  margin-bottom: 2rem;
  padding-bottom: 1.5rem;
  border-bottom: 2px solid var(--color-border);
}

.tab-title {
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--color-text-primary);
  margin: 0 0 0.5rem;
}

.tab-description {
  color: var(--color-text-secondary);
  margin: 0;
}

.form-section {
  margin-bottom: 2.5rem;
}

.section-title {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--color-text-primary);
  margin: 0 0 1.5rem;
  padding-bottom: 0.75rem;
  border-bottom: 1px solid var(--color-border);
}

.section-description {
  color: var(--color-text-secondary);
  margin: 0 0 1.5rem;
  font-size: 0.9375rem;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1.5rem;

  @media (max-width: 768px) {
    grid-template-columns: 1fr;
  }
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;

  &.form-group-full {
    grid-column: 1 / -1;
  }
}

.form-label {
  font-weight: 500;
  color: var(--color-text-primary);
  font-size: 0.875rem;

  &.required::after {
    content: ' *';
    color: var(--color-danger);
  }
}

.password-input-wrapper {
  position: relative;
}

.form-input {
  width: 100%;
  padding: 0.75rem 3rem 0.75rem 1rem;
  border: 1px solid var(--color-border);
  border-radius: 0.375rem;
  font-size: 0.9375rem;
  transition: all 0.2s;
  font-family: inherit;

  &:focus {
    outline: none;
    border-color: var(--color-primary);
    box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
  }

  &.input-error {
    border-color: var(--color-danger);

    &:focus {
      box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
    }
  }

  &::placeholder {
    color: var(--color-text-placeholder);
  }
}

.password-toggle {
  position: absolute;
  right: 0.75rem;
  top: 50%;
  transform: translateY(-50%);
  background: none;
  border: none;
  color: var(--color-text-secondary);
  cursor: pointer;
  padding: 0.25rem;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: color 0.2s;

  &:hover {
    color: var(--color-primary);
  }
}

.error-message {
  font-size: 0.8125rem;
  color: var(--color-danger);
  display: flex;
  align-items: center;
  gap: 0.25rem;

  &::before {
    content: '⚠';
  }
}

// Password Strength
.password-strength {
  margin-top: 0.5rem;
}

.strength-bar {
  height: 6px;
  background: var(--color-background);
  border-radius: 3px;
  overflow: hidden;
  margin-bottom: 0.5rem;
}

.strength-fill {
  height: 100%;
  transition: all 0.3s ease;
  border-radius: 3px;

  &.weak {
    background: #ef4444;
  }

  &.fair {
    background: #f59e0b;
  }

  &.good {
    background: #3b82f6;
  }

  &.strong {
    background: #10b981;
  }
}

.strength-label {
  font-size: 0.8125rem;
  font-weight: 500;

  &.weak {
    color: #ef4444;
  }

  &.fair {
    color: #f59e0b;
  }

  &.good {
    color: #3b82f6;
  }

  &.strong {
    color: #10b981;
  }
}

// Password Requirements
.password-requirements {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-top: 1rem;
  padding: 1rem;
  background: var(--color-background);
  border-radius: 0.375rem;
}

.requirement {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: var(--color-text-secondary);
  font-size: 0.875rem;

  svg {
    color: #cbd5e1;
    flex-shrink: 0;
  }

  &.met {
    color: #10b981;

    svg {
      color: #10b981;
    }
  }
}

// Security Tips
.security-tips {
  background: linear-gradient(135deg, rgba(16, 185, 129, 0.05) 0%, rgba(5, 150, 105, 0.05) 100%);
  border: 1px solid rgba(16, 185, 129, 0.2);
  border-radius: 0.75rem;
  padding: 1.5rem;
  margin-bottom: 2rem;
}

.tip-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 600;
  color: #059669;
  margin-bottom: 1rem;

  svg {
    color: #059669;
  }
}

.tip-list {
  margin: 0;
  padding-left: 1.5rem;
  color: var(--color-text-secondary);

  li {
    margin-bottom: 0.5rem;
    line-height: 1.6;

    &:last-child {
      margin-bottom: 0;
    }
  }
}

// Recent Activity
.activity-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.activity-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
}

.activity-icon {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: white;
  border-radius: 0.5rem;
  border: 1px solid var(--color-border);
  color: var(--color-primary);
  flex-shrink: 0;
}

.activity-details {
  flex: 1;
}

.activity-title {
  font-weight: 500;
  color: var(--color-text-primary);
  margin-bottom: 0.25rem;
}

.activity-time {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
}

// Form Actions
.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  padding-top: 2rem;
  border-top: 1px solid var(--color-border);
  margin-top: 2rem;

  @media (max-width: 640px) {
    flex-direction: column-reverse;
  }
}

.btn {
  padding: 0.75rem 2rem;
  border-radius: 0.375rem;
  font-weight: 500;
  font-size: 0.9375rem;
  cursor: pointer;
  transition: all 0.2s;
  border: none;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }

  &.btn-primary {
    background: var(--color-primary);
    color: white;

    &:hover:not(:disabled) {
      background: var(--color-primary-dark);
      transform: translateY(-1px);
      box-shadow: 0 4px 12px rgba(99, 102, 241, 0.4);
    }
  }

  &.btn-secondary {
    background: var(--color-background);
    color: var(--color-text-primary);
    border: 1px solid var(--color-border);

    &:hover:not(:disabled) {
      background: var(--color-background-hover);
      border-color: var(--color-primary);
    }
  }
}

.btn-spinner {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
