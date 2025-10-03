<template>
  <div class="provider-register-view">
    <div class="register-container">
      <Alert
        v-if="providerStore.error"
        type="error"
        :message="providerStore.error"
        dismissible
        @dismiss="clearError"
        class="error-alert"
      />
      <!-- Header -->
      <div class="register-header">
        <h1 class="register-title">Become a Provider</h1>
        <p class="register-subtitle">Join our platform and start accepting bookings from clients</p>
      </div>

      <!-- Progress Steps -->
      <div class="progress-steps">
        <div
          v-for="(step, index) in steps"
          :key="index"
          class="progress-step"
          :class="{
            active: currentStep === index,
            completed: currentStep > index,
          }"
          @click="goToStep(index)"
        >
          <div class="step-number">
            <span v-if="currentStep > index">✓</span>
            <span v-else>{{ index + 1 }}</span>
          </div>
          <div class="step-label">{{ step.label }}</div>
        </div>
        <div class="progress-line" :style="progressLineStyle"></div>
      </div>

      <!-- Form Card -->
      <Card class="register-form-card">
        <!-- Error Alert -->
        <Alert
          v-if="error"
          type="error"
          :message="error"
          @dismiss="clearError"
          class="form-alert"
        />

        <!-- Validation Summary -->
        <ValidationSummary
          v-if="Object.keys(validationErrors).length > 0"
          :errors="
            Object.fromEntries(
              Object.entries(validationErrors).map(([key, value]) => [key, [value]]),
            )
          "
          class="form-alert"
        />

        <!-- Step 1: Business Information -->
        <div v-if="currentStep === 0" class="form-step">
          <h2 class="step-title">Business Information</h2>
          <p class="step-description">Tell us about your business</p>

          <div class="dev-tools">
            <button @click="fillMock(0)">Fill: Hair Salon</button>
            <AppButton @click="fillMock(1)">Fill: Tech Repair</AppButton>
            <AppButton @click="fillMock(2)">Fill: Fitness</AppButton>
            <AppButton @click="fillRandomMock">Fill: `Random</AppButton>
          </div>
          <div class="form-grid">
            <div class="form-group span-2">
              <label class="form-label required">Business Name</label>
              <TextInput
                v-model="formData.businessName"
                placeholder="Enter your business name"
                :error="getFieldError('businessName')"
                @blur="validateField('businessName')"
              />
              <ValidationError :error="getFieldError('businessName')" />
            </div>

            <div class="form-group span-2">
              <label class="form-label required">Provider Type</label>
              <Select
                v-model="formData.type"
                :options="providerTypes"
                placeholder="Select provider type"
                :error="getFieldError('type')"
                @change="validateField('type')"
              />
              <ValidationError :error="getFieldError('type')" />
            </div>

            <div class="form-group span-2">
              <label class="form-label required">Description</label>
              <TextArea
                v-model="formData.description"
                placeholder="Describe your business, services, and what makes you unique..."
                :rows="4"
                :error="getFieldError('description')"
                @blur="validateField('description')"
              />
              <ValidationError :error="getFieldError('description')" />
              <span class="field-hint">{{ formData.description.length }}/500 characters</span>
            </div>

            <div class="form-group">
              <label class="form-label">Website</label>
              <TextInput
                v-model="formData.websiteUrl"
                placeholder="https://www.example.com"
                type="url"
                :error="getFieldError('websiteUrl')"
              />
              <ValidationError :error="getFieldError('websiteUrl')" />
            </div>
          </div>
        </div>

        <!-- Step 2: Contact Information -->
        <div v-if="currentStep === 1" class="form-step">
          <h2 class="step-title">Contact Information</h2>
          <p class="step-description">How can clients reach you?</p>

          <div class="form-grid">
            <div class="form-group">
              <label class="form-label required">Email</label>
              <TextInput
                v-model="formData.email"
                type="email"
                placeholder="contact@example.com"
                :error="getFieldError('email')"
                @blur="validateField('email')"
              />
              <ValidationError :error="getFieldError('email')" />
            </div>

            <div class="form-group">
              <label class="form-label required">Primary Phone</label>
              <TextInput
                v-model="formData.primaryPhone"
                type="tel"
                placeholder="(555) 123-4567"
                :error="getFieldError('primaryPhone')"
                @blur="validateField('primaryPhone')"
              />
              <ValidationError :error="getFieldError('primaryPhone')" />
            </div>

            <div class="form-group">
              <label class="form-label">Secondary Phone</label>
              <TextInput
                v-model="formData.secondaryPhone"
                type="tel"
                placeholder="(555) 987-6543"
              />
            </div>
          </div>
        </div>

        <!-- Step 3: Location -->
        <div v-if="currentStep === 2" class="form-step">
          <h2 class="step-title">Business Location</h2>
          <p class="step-description">Where is your business located?</p>

          <div class="form-grid">
            <div class="form-group span-2">
              <label class="form-label required">Street Address</label>
              <TextInput
                v-model="formData.addressLine1"
                placeholder="123 Main Street"
                :error="getFieldError('addressLine1')"
                @blur="validateField('addressLine1')"
              />
              <ValidationError :error="getFieldError('addressLine1')" />
            </div>

            <div class="form-group span-2">
              <label class="form-label">Address Line 2</label>
              <TextInput v-model="formData.addressLine2" placeholder="Suite 100, Building B" />
            </div>

            <div class="form-group">
              <label class="form-label required">City</label>
              <TextInput
                v-model="formData.city"
                placeholder="New York"
                :error="getFieldError('city')"
                @blur="validateField('city')"
              />
              <ValidationError :error="getFieldError('city')" />
            </div>

            <div class="form-group">
              <label class="form-label required">State/Province</label>
              <TextInput
                v-model="formData.state"
                placeholder="NY"
                :error="getFieldError('state')"
                @blur="validateField('state')"
              />
              <ValidationError :error="getFieldError('state')" />
            </div>

            <div class="form-group">
              <label class="form-label required">Postal Code</label>
              <TextInput
                v-model="formData.postalCode"
                placeholder="10001"
                :error="getFieldError('postalCode')"
                @blur="validateField('postalCode')"
              />
              <ValidationError :error="getFieldError('postalCode')" />
            </div>

            <div class="form-group">
              <label class="form-label required">Country</label>
              <Select
                v-model="formData.country"
                :options="countries"
                placeholder="Select country"
                :error="getFieldError('country')"
                @change="validateField('country')"
              />
              <ValidationError :error="getFieldError('country')" />
            </div>
          </div>
        </div>

        <!-- Step 4: Service Settings -->
        <div v-if="currentStep === 3" class="form-step">
          <h2 class="step-title">Service Settings</h2>
          <p class="step-description">Configure how you'll work with clients</p>

          <div class="settings-grid">
            <div class="setting-card">
              <div class="setting-icon">📅</div>
              <div class="setting-content">
                <h3 class="setting-title">Online Booking</h3>
                <p class="setting-description">Allow clients to book appointments online</p>
                <label class="toggle-switch">
                  <input v-model="formData.allowOnlineBooking" type="checkbox" />
                  <span class="toggle-slider"></span>
                </label>
              </div>
            </div>

            <div class="setting-card">
              <div class="setting-icon">🚗</div>
              <div class="setting-content">
                <h3 class="setting-title">Mobile Services</h3>
                <p class="setting-description">Offer services at client locations</p>
                <label class="toggle-switch">
                  <input v-model="formData.offersMobileServices" type="checkbox" />
                  <span class="toggle-slider"></span>
                </label>
              </div>
            </div>

            <div class="setting-card">
              <div class="setting-icon">✅</div>
              <div class="setting-content">
                <h3 class="setting-title">Booking Approval</h3>
                <p class="setting-description">Manually approve each booking request</p>
                <label class="toggle-switch">
                  <input v-model="formData.requiresApproval" type="checkbox" />
                  <span class="toggle-slider"></span>
                </label>
              </div>
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Specializations (Tags)</label>
            <div class="tags-input-wrapper">
              <div v-if="formData.tags && formData.tags.length > 0" class="selected-tags">
                <span v-for="(tag, index) in formData.tags" :key="index" class="tag">
                  {{ tag }}
                  <button @click="removeTag(index)" class="tag-remove">×</button>
                </span>
              </div>
              <input
                v-model="tagInput"
                type="text"
                placeholder="Add specialization (e.g., 'Hair Styling', 'Massage')..."
                class="tag-input"
                @keydown.enter.prevent="addTag"
                @keydown="handleKeydown"
              />
            </div>
            <span class="field-hint">Press Enter or comma to add tags</span>
          </div>
        </div>

        <!-- Step 5: Review & Submit -->
        <div v-if="currentStep === 4" class="form-step">
          <h2 class="step-title">Review & Submit</h2>
          <p class="step-description">Please review your information before submitting</p>

          <div class="review-sections">
            <div class="review-section">
              <h3 class="review-section-title">Business Information</h3>
              <div class="review-grid">
                <div class="review-item">
                  <span class="review-label">Business Name:</span>
                  <span class="review-value">{{ formData.businessName }}</span>
                </div>
                <div class="review-item">
                  <span class="review-label">Type:</span>
                  <span class="review-value">{{ formData.type }}</span>
                </div>
                <div class="review-item span-2">
                  <span class="review-label">Description:</span>
                  <span class="review-value">{{ formData.description }}</span>
                </div>
              </div>
            </div>

            <div class="review-section">
              <h3 class="review-section-title">Contact Information</h3>
              <div class="review-grid">
                <div class="review-item">
                  <span class="review-label">Email:</span>
                  <span class="review-value">{{ formData.email }}</span>
                </div>
                <div class="review-item">
                  <span class="review-label">Phone:</span>
                  <span class="review-value">{{ formData.primaryPhone }}</span>
                </div>
              </div>
            </div>

            <div class="review-section">
              <h3 class="review-section-title">Location</h3>
              <div class="review-grid">
                <div class="review-item span-2">
                  <span class="review-label">Address:</span>
                  <span class="review-value">
                    {{ formData.addressLine1 }}
                    <span v-if="formData.addressLine2">, {{ formData.addressLine2 }}</span>
                  </span>
                </div>
                <div class="review-item">
                  <span class="review-label">City:</span>
                  <span class="review-value">{{ formData.city }}, {{ formData.state }}</span>
                </div>
                <div class="review-item">
                  <span class="review-label">Postal Code:</span>
                  <span class="review-value">{{ formData.postalCode }}</span>
                </div>
              </div>
            </div>

            <div class="review-section">
              <h3 class="review-section-title">Service Settings</h3>
              <div class="review-features">
                <Badge v-if="formData.allowOnlineBooking" variant="success"
                  >Online Booking Enabled</Badge
                >
                <Badge v-if="formData.offersMobileServices" variant="info">Mobile Services</Badge>
                <Badge v-if="formData.requiresApproval" variant="warning">Requires Approval</Badge>
              </div>
              <div v-if="formData.tags && formData.tags.length > 0" class="review-tags">
                <span class="review-label">Specializations:</span>
                <div class="tags-list">
                  <span v-for="tag in formData.tags" :key="tag" class="tag">{{ tag }}</span>
                </div>
              </div>
            </div>
          </div>

          <div class="terms-agreement">
            <label class="checkbox-label">
              <input v-model="agreedToTerms" type="checkbox" />
              <span>
                I agree to the
                <a href="/terms" target="_blank">Terms of Service</a> and
                <a href="/privacy" target="_blank">Privacy Policy</a>
              </span>
            </label>
            <ValidationError
              v-if="showTermsError"
              error="You must agree to the terms to continue"
            />
          </div>
        </div>

        <!-- Navigation Buttons -->
        <div class="form-actions">
          <button
            v-if="currentStep > 0"
            class="btn btn-secondary"
            :disabled="isSubmitting"
            @click="previousStep"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M15 19l-7-7 7-7"
              />
            </svg>
            Previous
          </button>

          <div class="spacer"></div>

          <button v-if="currentStep < steps.length - 1" class="btn btn-primary" @click="nextStep">
            Next
            <svg
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M9 5l7 7-7 7"
              />
            </svg>
          </button>

          <button
            v-if="currentStep === steps.length - 1"
            class="btn btn-primary btn-large"
            :disabled="isSubmitting || !agreedToTerms"
            @click="handleSubmit"
          >
            <Spinner v-if="isSubmitting" size="small" />
            <span v-else>Submit Application</span>
          </button>
        </div>
      </Card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '../stores/provider.store'
import {
  Card,
  Alert,
  Badge,
  Spinner,
  TextInput,
  TextArea,
  Select,
  ValidationError,
  ValidationSummary,
} from '../../../shared/components'
import type { RegisterProviderRequest, ProviderType } from '../types/provider.types'
import { mockProviders, getRandomMockProvider } from '../mocks/provider.mock'

// Fill with random mock data
function fillRandomMock() {
  Object.assign(formData, getRandomMockProvider())
}

// Fill with specific mock data
function fillMock(index: number) {
  Object.assign(formData, mockProviders[index])
}

// Router & Store
const router = useRouter()
const providerStore = useProviderStore()

// State
const currentStep = ref(0)
const isSubmitting = ref(false)
const agreedToTerms = ref(false)
const showTermsError = ref(false)
const tagInput = ref('')
const validationErrors = ref<Record<string, string>>({})

const steps = [
  { label: 'Business Info', key: 'business' },
  { label: 'Contact', key: 'contact' },
  { label: 'Location', key: 'location' },
  { label: 'Settings', key: 'settings' },
  { label: 'Review', key: 'review' },
]

const formData = reactive<RegisterProviderRequest>({
  businessName: '',
  description: '',
  type: '' as ProviderType,
  email: '',
  primaryPhone: '',
  secondaryPhone: '',
  websiteUrl: '',
  addressLine1: '',
  addressLine2: '',
  city: '',
  state: '',
  postalCode: '',
  country: 'USA',
  requiresApproval: false,
  allowOnlineBooking: true,
  offersMobileServices: false,
  tags: [],
  ownerId: '',
  latitude: 0,
  longitude: 0,
})

// Options
const providerTypes = [
  { value: 'Individual', label: 'Individual' },
  { value: 'Salon', label: 'Salon' },
  { value: 'Clinic', label: 'Clinic' },
  { value: 'Spa', label: 'Spa' },
  { value: 'Studio', label: 'Studio' },
  { value: 'Professional', label: 'Professional' },
]

const countries = [
  { value: 'USA', label: 'United States' },
  { value: 'Canada', label: 'Canada' },
  { value: 'UK', label: 'United Kingdom' },
  { value: 'Australia', label: 'Australia' },
]

// Computed
const error = computed(() => providerStore.error)

const progressLineStyle = computed(() => {
  const progress = (currentStep.value / (steps.length - 1)) * 100
  return { width: `${progress}%` }
})

// Validation
const validateField = (field: keyof RegisterProviderRequest): boolean => {
  delete validationErrors.value[field]

  if (field === 'businessName' && !formData.businessName.trim()) {
    validationErrors.value[field] = 'Business name is required'
    return false
  }

  if (field === 'type' && !formData.type) {
    validationErrors.value[field] = 'Provider type is required'
    return false
  }

  if (field === 'description') {
    if (!formData.description.trim()) {
      validationErrors.value[field] = 'Description is required'
      return false
    }
    if (formData.description.length < 50) {
      validationErrors.value[field] = 'Description must be at least 50 characters'
      return false
    }
  }

  if (field === 'email') {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!formData.email.trim()) {
      validationErrors.value[field] = 'Email is required'
      return false
    }
    if (!emailRegex.test(formData.email)) {
      validationErrors.value[field] = 'Invalid email format'
      return false
    }
  }

  if (field === 'primaryPhone' && !formData.primaryPhone.trim()) {
    validationErrors.value[field] = 'Primary phone is required'
    return false
  }

  if (field === 'addressLine1' && !formData.addressLine1.trim()) {
    validationErrors.value[field] = 'Street address is required'
    return false
  }

  if (field === 'city' && !formData.city.trim()) {
    validationErrors.value[field] = 'City is required'
    return false
  }

  if (field === 'state' && !formData.state.trim()) {
    validationErrors.value[field] = 'State is required'
    return false
  }

  if (field === 'postalCode' && !formData.postalCode.trim()) {
    validationErrors.value[field] = 'Postal code is required'
    return false
  }

  if (field === 'country' && !formData.country) {
    validationErrors.value[field] = 'Country is required'
    return false
  }

  return true
}

const validateStep = (step: number): boolean => {
  validationErrors.value = {}

  if (step === 0) {
    return validateField('businessName') && validateField('type') && validateField('description')
  }

  if (step === 1) {
    return validateField('email') && validateField('primaryPhone')
  }

  if (step === 2) {
    return (
      validateField('addressLine1') &&
      validateField('city') &&
      validateField('state') &&
      validateField('postalCode') &&
      validateField('country')
    )
  }

  return true
}

const getFieldError = (field: string): string | undefined => {
  return validationErrors.value[field]
}

// Navigation
const goToStep = (step: number) => {
  if (step < currentStep.value) {
    currentStep.value = step
  }
}

const nextStep = () => {
  if (validateStep(currentStep.value)) {
    currentStep.value++
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
}

const handleKeydown = (event: KeyboardEvent) => {
  if (event.key === ',') {
    event.preventDefault()
    addTag()
  }
}

const previousStep = () => {
  if (currentStep.value > 0) {
    currentStep.value--
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
}

// Tags
const addTag = () => {
  const tag = tagInput.value.trim().replace(',', '')
  if (tag && !formData.tags?.includes(tag)) {
    if (!formData.tags) {
      formData.tags = []
    }
    formData.tags.push(tag)
    tagInput.value = ''
  }
}

const removeTag = (index: number) => {
  formData.tags?.splice(index, 1)
}

// Submit
const handleSubmit = async () => {
  showTermsError.value = false

  if (!agreedToTerms.value) {
    showTermsError.value = true
    return
  }

  isSubmitting.value = true // ✅ Move here - set BEFORE the async call

  try {
    const result = await providerStore.registerProvider(formData)
    debugger
    if (result && !providerStore.error) {
      router.push({
        name: 'ProviderOnboarding',
        query: { welcome: 'true' },
      })
    } else {
      window.scrollTo({ top: 0, behavior: 'smooth' })
    }
  } catch (err: unknown) {
    console.error('Registration failed:', err)
    // Error is already set in the store, just scroll to show it
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    isSubmitting.value = false
  }
}
const clearError = () => {
  providerStore.clearError()
}
</script>

<style scoped>
.provider-register-view {
  min-height: 100vh;
  background: var(--color-bg-secondary);
  padding: 2rem;
}

.register-container {
  max-width: 900px;
  margin: 0 auto;
}

.register-header {
  text-align: center;
  margin-bottom: 3rem;
}

.register-title {
  font-size: 2.5rem;
  font-weight: 700;
  color: var(--color-text-primary);
  margin: 0 0 0.5rem 0;
}

.register-subtitle {
  font-size: 1.1rem;
  color: var(--color-text-secondary);
  margin: 0;
}

/* Progress Steps */
.progress-steps {
  position: relative;
  display: flex;
  justify-content: space-between;
  margin-bottom: 3rem;
  padding: 0 2rem;
}

.progress-line {
  position: absolute;
  top: 20px;
  left: 0;
  right: 0;
  height: 3px;
  background: var(--color-primary);
  transition: width 0.3s ease;
  z-index: 0;
}

.progress-step {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  z-index: 1;
}

.step-number {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: white;
  border: 3px solid var(--color-border);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  color: var(--color-text-tertiary);
  transition: all 0.3s;
}

.progress-step.active .step-number {
  border-color: var(--color-primary);
  background: var(--color-primary);
  color: rgb(238, 25, 25);
  transform: scale(1.1);
}

.progress-step.completed .step-number {
  border-color: var(--color-primary);
  background: var(--color-primary);
  color: rgb(27, 224, 43);
}

.step-label {
  font-size: 0.85rem;
  font-weight: 500;
  color: var(--color-text-secondary);
  white-space: nowrap;
}

.progress-step.active .step-label {
  color: var(--color-primary);
  font-weight: 600;
}

/* Form */
.register-form-card {
  padding: 2.5rem;
}

.form-alert {
  margin-bottom: 2rem;
}

.form-step {
  animation: fadeIn 0.3s ease;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.step-title {
  font-size: 1.75rem;
  font-weight: 600;
  margin: 0 0 0.5rem 0;
  color: var(--color-text-primary);
}

.step-description {
  font-size: 1rem;
  color: var(--color-text-secondary);
  margin: 0 0 2rem 0;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1.5rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group.span-2 {
  grid-column: span 2;
}

.form-label {
  font-size: 0.95rem;
  font-weight: 500;
  color: var(--color-text-primary);
}

.form-label.required::after {
  content: '*';
  color: var(--color-danger);
  margin-left: 0.25rem;
}

.field-hint {
  font-size: 0.85rem;
  color: var(--color-text-tertiary);
  font-style: italic;
}

/* Settings */
.settings-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.setting-card {
  padding: 1.5rem;
  background: var(--color-bg-secondary);
  border-radius: 12px;
  border: 2px solid var(--color-border);
  transition: all 0.2s;
}

.setting-card:hover {
  border-color: var(--color-primary);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
}

.setting-icon {
  font-size: 2rem;
  margin-bottom: 1rem;
}

.setting-title {
  font-size: 1.1rem;
  font-weight: 600;
  margin: 0 0 0.5rem 0;
}

.setting-description {
  font-size: 0.9rem;
  color: var(--color-text-secondary);
  margin: 0 0 1rem 0;
}

.toggle-switch {
  position: relative;
  display: inline-block;
  width: 50px;
  height: 26px;
}

.toggle-switch input {
  opacity: 0;
  width: 0;
  height: 0;
}

.toggle-slider {
  position: absolute;
  cursor: pointer;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: var(--color-border);
  transition: 0.3s;
  border-radius: 26px;
}

.toggle-slider:before {
  position: absolute;
  content: '';
  height: 18px;
  width: 18px;
  left: 4px;
  bottom: 4px;
  background-color: white;
  transition: 0.3s;
  border-radius: 50%;
}

input:checked + .toggle-slider {
  background-color: var(--color-primary);
}

input:checked + .toggle-slider:before {
  transform: translateX(24px);
}

/* Tags Input */
.tags-input-wrapper {
  border: 1px solid var(--color-border);
  border-radius: 8px;
  padding: 0.75rem;
  background: white;
  min-height: 80px;
}

.selected-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.tag {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.375rem 0.75rem;
  background: var(--color-primary-light);
  color: var(--color-primary-dark);
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 500;
}

.tag-remove {
  background: none;
  border: none;
  color: currentColor;
  font-size: 1.25rem;
  line-height: 1;
  cursor: pointer;
  padding: 0;
  margin-left: 0.25rem;
}

.tag-input {
  width: 100%;
  border: none;
  padding: 0.5rem 0;
  font-size: 0.95rem;
}

.tag-input:focus {
  outline: none;
}

/* Review */
.review-sections {
  display: flex;
  flex-direction: column;
  gap: 2rem;
  margin-bottom: 2rem;
}

.review-section {
  padding: 1.5rem;
  background: var(--color-bg-secondary);
  border-radius: 12px;
}

.review-section-title {
  font-size: 1.1rem;
  font-weight: 600;
  margin: 0 0 1rem 0;
  color: var(--color-text-primary);
}

.review-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1rem;
}

.review-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.review-item.span-2 {
  grid-column: span 2;
}

.review-label {
  font-size: 0.85rem;
  color: var(--color-text-secondary);
  font-weight: 500;
}

.review-value {
  font-size: 0.95rem;
  color: var(--color-text-primary);
}

.review-features {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.review-tags {
  margin-top: 1rem;
}

.tags-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-top: 0.5rem;
}

.terms-agreement {
  padding: 1.5rem;
  background: var(--color-bg-secondary);
  border-radius: 12px;
  margin-top: 2rem;
}

.checkbox-label {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  font-size: 0.95rem;
  cursor: pointer;
}

.checkbox-label input[type='checkbox'] {
  margin-top: 0.25rem;
  cursor: pointer;
}

.checkbox-label a {
  color: var(--color-primary);
  text-decoration: none;
}

.checkbox-label a:hover {
  text-decoration: underline;
}

/* Actions */
.form-actions {
  display: flex;
  gap: 1rem;
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 1px solid var(--color-border);
}

.spacer {
  flex: 1;
}

.btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.875rem 1.75rem;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn svg {
  width: 18px;
  height: 18px;
}

.btn-primary {
  background: var(--color-primary);
}

.btn-primary:hover:not(:disabled) {
  background: var(--color-primary-dark);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.3);
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-secondary {
  background: var(--color-bg-tertiary);
  color: var(--color-text-primary);
}

.btn-secondary:hover {
  background: var(--color-border);
}

.btn-large {
  padding: 1rem 2rem;
  font-size: 1.1rem;
}

@media (max-width: 768px) {
  .provider-register-view {
    padding: 1rem;
  }

  .register-title {
    font-size: 1.75rem;
  }

  .progress-steps {
    padding: 0;
    overflow-x: auto;
  }

  .step-label {
    font-size: 0.75rem;
  }

  .register-form-card {
    padding: 1.5rem;
  }

  .form-grid {
    grid-template-columns: 1fr;
  }

  .form-group.span-2 {
    grid-column: span 1;
  }

  .settings-grid {
    grid-template-columns: 1fr;
  }

  .review-grid {
    grid-template-columns: 1fr;
  }

  .review-item.span-2 {
    grid-column: span 1;
  }

  .form-actions {
    flex-direction: column;
  }

  .btn {
    width: 100%;
    justify-content: center;
  }
}
</style>
