<template>
  <div class="personal-info-tab">
    <div class="tab-header">
      <div>
        <h2 class="tab-title">Personal Information</h2>
        <p class="tab-description">Update your personal details and contact information</p>
      </div>
    </div>

    <form @submit.prevent="handleSubmit" class="profile-form">
      <!-- Basic Information Section -->
      <div class="form-section">
        <h3 class="section-title">Basic Information</h3>

        <div class="form-grid">
          <!-- First Name -->
          <div class="form-group">
            <label for="firstName" class="form-label required"> First Name </label>
            <input
              id="firstName"
              v-model="form.firstName"
              type="text"
              class="form-input"
              :class="{ 'input-error': errors.firstName }"
              placeholder="Enter your first name"
              required
            />
            <span v-if="errors.firstName" class="error-message">
              {{ errors.firstName }}
            </span>
          </div>

          <!-- Last Name -->
          <div class="form-group">
            <label for="lastName" class="form-label required"> Last Name </label>
            <input
              id="lastName"
              v-model="form.lastName"
              type="text"
              class="form-input"
              :class="{ 'input-error': errors.lastName }"
              placeholder="Enter your last name"
              required
            />
            <span v-if="errors.lastName" class="error-message">
              {{ errors.lastName }}
            </span>
          </div>

          <!-- Phone Number -->
          <div class="form-group">
            <label for="phoneNumber" class="form-label"> Phone Number </label>
            <input
              id="phoneNumber"
              v-model="form.phoneNumber"
              type="tel"
              class="form-input"
              :class="{ 'input-error': errors.phoneNumber }"
              placeholder="+1 (555) 123-4567"
            />
            <span v-if="errors.phoneNumber" class="error-message">
              {{ errors.phoneNumber }}
            </span>
            <span v-else class="help-text"> Include country code for international numbers </span>
          </div>

          <!-- Date of Birth -->
          <div class="form-group">
            <label for="dateOfBirth" class="form-label"> Date of Birth </label>
            <input
              id="dateOfBirth"
              v-model="form.dateOfBirth"
              type="date"
              class="form-input"
              :max="maxDate"
            />
            <span class="help-text"> Your age must be at least 13 years </span>
          </div>
        </div>

        <!-- Bio (Full width) -->
        <div class="form-group">
          <label for="bio" class="form-label"> Bio </label>
          <textarea
            id="bio"
            v-model="form.bio"
            class="form-textarea"
            rows="4"
            :maxlength="500"
            placeholder="Tell us a little about yourself..."
          ></textarea>
          <span class="help-text"> {{ bioLength }} / 500 characters </span>
        </div>
      </div>

      <!-- Address Section -->
      <div class="form-section">
        <h3 class="section-title">Address Information</h3>

        <div class="form-grid">
          <!-- Formatted Address -->
          <div class="form-group form-group-full">
            <label for="formattedAddress" class="form-label"> Address </label>
            <input
              id="formattedAddress"
              v-model="form.address.formattedAddress"
              type="text"
              class="form-input"
              placeholder="123 Main Street, Apt 4B, City, State"
            />
          </div>

          <!-- City -->
          <div class="form-group">
            <label for="city" class="form-label"> City </label>
            <input
              id="city"
              v-model="form.address.city"
              type="text"
              class="form-input"
              placeholder="New York"
            />
          </div>

          <!-- State/Province -->
          <div class="form-group">
            <label for="state" class="form-label"> State / Province </label>
            <input
              id="state"
              v-model="form.address.state"
              type="text"
              class="form-input"
              placeholder="NY"
            />
          </div>

          <!-- Postal Code -->
          <div class="form-group">
            <label for="postalCode" class="form-label"> Postal Code </label>
            <input
              id="postalCode"
              v-model="form.address.postalCode"
              type="text"
              class="form-input"
              placeholder="10001"
            />
          </div>

          <!-- Country -->
          <div class="form-group">
            <label for="country" class="form-label"> Country </label>
            <select id="country" v-model="form.address.country" class="form-select">
              <option value="">Select a country</option>
              <option value="United States">United States</option>
              <option value="Canada">Canada</option>
              <option value="United Kingdom">United Kingdom</option>
              <option value="Australia">Australia</option>
              <option value="Germany">Germany</option>
              <option value="France">France</option>
              <option value="Spain">Spain</option>
              <option value="Italy">Italy</option>
              <option value="Netherlands">Netherlands</option>
              <option value="Belgium">Belgium</option>
              <option value="Other">Other</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Form Actions -->
      <div class="form-actions">
        <button
          type="button"
          class="btn btn-secondary"
          @click="handleReset"
          :disabled="isSaving || !hasChanges"
        >
          Reset
        </button>
        <button
          type="submit"
          class="btn btn-primary"
          :disabled="isSaving || !hasChanges || !isValid"
        >
          <span v-if="isSaving" class="btn-spinner"></span>
          {{ isSaving ? 'Saving...' : 'Save Changes' }}
        </button>
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import type {
  UserProfile,
  UpdateProfileRequest,
  ValidationErrors,
} from '@/modules/user-management/types/user-profile.types'
import { Address } from '@/modules/user-management/types/user.types'
import { isValidPhoneNumber } from '@/core/utils'

// Props
const props = defineProps<{
  profile: UserProfile
  isSaving: boolean
}>()

// Emits
const emit = defineEmits<{
  update: [data: UpdateProfileRequest]
}>()

// Form state
const form = ref({
  firstName: '',
  lastName: '',
  phoneNumber: '',
  dateOfBirth: '',
  bio: '',
  address: {
    formattedAddress: '',
    city: '',
    state: '',
    postalCode: '',
    country: '',
  } as Address,
})

const errors = ref<ValidationErrors>({})
const initialForm = ref<string>('')

// Computed
const bioLength = computed(() => form.value.bio?.length || 0)

const maxDate = computed(() => {
  const date = new Date()
  date.setFullYear(date.getFullYear() - 13) // Minimum age 13
  return date.toISOString().split('T')[0]
})

const hasChanges = computed(() => {
  return JSON.stringify(form.value) !== initialForm.value
})

const isValid = computed(() => {
  return form.value.firstName.trim() !== '' && form.value.lastName.trim() !== ''
})

// Methods
function initializeForm() {
  form.value = {
    firstName: props.profile.firstName || '',
    lastName: props.profile.lastName || '',
    phoneNumber: props.profile.phoneNumber || '',
    dateOfBirth: props.profile.dateOfBirth || '',
    bio: props.profile.bio || '',
    address: {
      formattedAddress: props.profile.address?.formattedAddress || '',
      city: props.profile.address?.city || '',
      state: props.profile.address?.state || '',
      postalCode: props.profile.address?.postalCode || '',
      country: props.profile.address?.country || '',
      latitude: props.profile.address?.latitude || undefined,
      longitude: props.profile.address?.longitude || undefined,
    },
  }

  // Store initial state for change detection
  initialForm.value = JSON.stringify(form.value)
  errors.value = {}
}

function validateForm(): boolean {
  errors.value = {}

  if (!form.value.firstName.trim()) {
    errors.value.firstName = 'First name is required'
  }

  if (!form.value.lastName.trim()) {
    errors.value.lastName = 'Last name is required'
  }

  if (form.value.phoneNumber && !isValidPhoneNumber(form.value.phoneNumber)) {
    errors.value.phoneNumber = 'Please enter a valid phone number'
  }

  if (form.value.dateOfBirth) {
    const birthDate = new Date(form.value.dateOfBirth)
    const today = new Date()
    const age = today.getFullYear() - birthDate.getFullYear()

    if (age < 13) {
      errors.value.dateOfBirth = 'You must be at least 13 years old'
    }
  }

  return Object.keys(errors.value).length === 0
}

function handleSubmit() {
  if (!validateForm()) {
    return
  }

  const updateData: UpdateProfileRequest = {
    firstName: form.value.firstName.trim(),
    lastName: form.value.lastName.trim(),
    phoneNumber: form.value.phoneNumber || undefined,
    dateOfBirth: form.value.dateOfBirth || undefined,
    bio: form.value.bio || undefined,
    address: hasAddress() ? form.value.address : undefined,
  }

  emit('update', updateData)
}

function hasAddress(): boolean {
  return !!(
    form.value.address.formattedAddress ||
    form.value.address.city ||
    form.value.address.state ||
    form.value.address.postalCode ||
    form.value.address.country
  )
}

function handleReset() {
  initializeForm()
}

// Watchers
watch(
  () => props.profile,
  () => {
    initializeForm()
  },
  { deep: true },
)

// Lifecycle
onMounted(() => {
  initializeForm()
})
</script>

<style scoped lang="scss">
.personal-info-tab {
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

.form-input,
.form-select,
.form-textarea {
  padding: 0.75rem 1rem;
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

.form-textarea {
  resize: vertical;
  min-height: 100px;
}

.help-text {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
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

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  padding-top: 2rem;
  border-top: 2px solid var(--color-border);

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
