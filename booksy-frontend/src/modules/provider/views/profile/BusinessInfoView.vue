<template>
  <div class="business-info-view">
    <!-- Header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">Business Information</h1>
        <p class="page-subtitle">
          Complete your business profile to help customers find and connect with you
        </p>
      </div>
      <Button variant="secondary" @click="goBack">← Back to Onboarding</Button>
    </div>

    <!-- Loading State -->
    <div v-if="providerStore.isLoading" class="loading-state">
      <Spinner />
      <p>Loading business information...</p>
    </div>

    <!-- Error State -->
    <Alert
      v-if="errorMessage"
      type="error"
      :message="errorMessage"
      @dismiss="errorMessage = null"
    />

    <!-- Success Message -->
    <Alert
      v-if="successMessage"
      type="success"
      :message="successMessage"
      @dismiss="successMessage = null"
    />

    <!-- Form -->
    <form v-if="!providerStore.isLoading" class="business-form" @submit.prevent="handleSubmit">
      <!-- Business Profile Section -->
      <Card class="form-section">
        <h2 class="section-title">Business Profile</h2>
        <div class="form-grid">
          <TextInput
            v-model="formData.businessName"
            label="Business Name"
            placeholder="e.g., Bella's Hair Salon"
            required
            :error="validationErrors.businessName"
          />

          <Select
            v-model="formData.type"
            label="Business Type"
            :options="businessTypeOptions"
            placeholder="Select business type"
            required
            :error="validationErrors.type"
          />

          <TextArea
            v-model="formData.description"
            label="Business Description"
            placeholder="Describe your business, services, and what makes you unique..."
            :rows="5"
            :maxlength="1000"
            show-count
            required
            :error="validationErrors.description"
            class="full-width"
          />

          <TextInput
            v-model="formData.websiteUrl"
            type="url"
            label="Website URL"
            placeholder="https://www.yourbusiness.com"
            :error="validationErrors.websiteUrl"
          />

          <TextInput
            v-model="formData.logoUrl"
            type="url"
            label="Logo URL"
            placeholder="https://example.com/logo.png"
            hint="URL to your business logo"
            :error="validationErrors.logoUrl"
          />

          <TextInput
            v-model="formData.coverImageUrl"
            type="url"
            label="Cover Image URL"
            placeholder="https://example.com/cover.jpg"
            hint="URL to your cover/banner image"
            :error="validationErrors.coverImageUrl"
          />
        </div>
      </Card>

      <!-- Contact Information Section -->
      <Card class="form-section">
        <h2 class="section-title">Contact Information</h2>
        <div class="form-grid">
          <TextInput
            v-model="formData.email"
            type="email"
            label="Business Email"
            placeholder="contact@yourbusiness.com"
            required
            :error="validationErrors.email"
          />

          <TextInput
            v-model="formData.primaryPhone"
            type="tel"
            label="Primary Phone"
            placeholder="+1 (555) 123-4567"
            required
            :error="validationErrors.primaryPhone"
          />

          <TextInput
            v-model="formData.secondaryPhone"
            type="tel"
            label="Secondary Phone"
            placeholder="+1 (555) 987-6543"
            hint="Optional alternative contact number"
            :error="validationErrors.secondaryPhone"
          />
        </div>
      </Card>

      <!-- Business Address Section -->
      <Card class="form-section">
        <h2 class="section-title">Business Address</h2>
        <div class="form-grid">
          <TextInput
            v-model="formData.addressLine1"
            label="Address Line 1"
            placeholder="123 Main Street"
            required
            :error="validationErrors.addressLine1"
            class="full-width"
          />

          <TextInput
            v-model="formData.addressLine2"
            label="Address Line 2"
            placeholder="Suite 100, Building A"
            hint="Optional - apartment, suite, unit, etc."
            :error="validationErrors.addressLine2"
            class="full-width"
          />

          <TextInput
            v-model="formData.city"
            label="City"
            placeholder="New York"
            required
            :error="validationErrors.city"
          />

          <TextInput
            v-model="formData.state"
            label="State / Province"
            placeholder="NY"
            required
            :error="validationErrors.state"
          />

          <TextInput
            v-model="formData.postalCode"
            label="Postal / ZIP Code"
            placeholder="10001"
            required
            :error="validationErrors.postalCode"
          />

          <TextInput
            v-model="formData.country"
            label="Country"
            placeholder="United States"
            required
            :error="validationErrors.country"
          />
        </div>
      </Card>

      <!-- Business Settings Section -->
      <Card class="form-section">
        <h2 class="section-title">Business Settings</h2>
        <div class="form-group">
          <div class="checkbox-group">
            <label class="checkbox-label">
              <input v-model="formData.allowOnlineBooking" type="checkbox" class="checkbox" />
              <span class="checkbox-text">
                <strong>Allow Online Booking</strong>
                <small>Customers can book appointments online through the platform</small>
              </span>
            </label>

            <label class="checkbox-label">
              <input v-model="formData.offersMobileServices" type="checkbox" class="checkbox" />
              <span class="checkbox-text">
                <strong>Offers Mobile Services</strong>
                <small>You provide services at customer locations</small>
              </span>
            </label>
          </div>

          <TextInput
            v-model="formData.tags"
            label="Tags / Keywords"
            placeholder="e.g., haircut, styling, coloring, spa"
            hint="Comma-separated keywords to help customers find your business"
            :error="validationErrors.tags"
            class="full-width-field"
          />
        </div>
      </Card>

      <!-- Social Media Section -->
      <Card class="form-section">
        <h2 class="section-title">Social Media Links</h2>
        <p class="section-description">
          Connect your social media profiles to help customers find and follow you
        </p>
        <div class="form-grid">
          <TextInput
            v-model="formData.socialMedia.facebook"
            type="url"
            label="Facebook"
            placeholder="https://facebook.com/yourbusiness"
            prefix-icon="📘"
            :error="validationErrors['socialMedia.facebook']"
          />

          <TextInput
            v-model="formData.socialMedia.instagram"
            type="url"
            label="Instagram"
            placeholder="https://instagram.com/yourbusiness"
            prefix-icon="📷"
            :error="validationErrors['socialMedia.instagram']"
          />

          <TextInput
            v-model="formData.socialMedia.twitter"
            type="url"
            label="Twitter / X"
            placeholder="https://twitter.com/yourbusiness"
            prefix-icon="🐦"
            :error="validationErrors['socialMedia.twitter']"
          />

          <TextInput
            v-model="formData.socialMedia.linkedin"
            type="url"
            label="LinkedIn"
            placeholder="https://linkedin.com/company/yourbusiness"
            prefix-icon="💼"
            :error="validationErrors['socialMedia.linkedin']"
          />
        </div>
      </Card>

      <!-- Form Actions -->
      <div class="form-actions">
        <Button type="button" variant="secondary" @click="goBack">Cancel</Button>
        <Button type="submit" variant="primary" :disabled="isSaving">
          {{ isSaving ? 'Saving...' : 'Save Business Information' }}
        </Button>
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import { ProviderType } from '@/modules/provider/types/provider.types'
import { Button, TextInput, TextArea, Select, Card, Alert, Spinner } from '@/shared/components'

const router = useRouter()
const providerStore = useProviderStore()

const isSaving = ref(false)
const errorMessage = ref<string | null>(null)
const successMessage = ref<string | null>(null)

const formData = reactive({
  businessName: '',
  type: '',
  description: '',
  websiteUrl: '',
  logoUrl: '',
  coverImageUrl: '',
  email: '',
  primaryPhone: '',
  secondaryPhone: '',
  addressLine1: '',
  addressLine2: '',
  city: '',
  state: '',
  postalCode: '',
  country: '',
  allowOnlineBooking: true,
  offersMobileServices: false,
  tags: '',
  socialMedia: {
    facebook: '',
    instagram: '',
    twitter: '',
    linkedin: '',
  },
})

const validationErrors = reactive<Record<string, string>>({})

const businessTypeOptions = [
  { value: ProviderType.Individual, label: 'Individual Professional' },
  { value: ProviderType.Salon, label: 'Salon' },
  { value: ProviderType.Clinic, label: 'Clinic' },
  { value: ProviderType.Spa, label: 'Spa' },
  { value: ProviderType.Studio, label: 'Studio' },
  { value: ProviderType.Professional, label: 'Professional Services' },
]

// Load existing provider data
onMounted(async () => {
  try {
    if (!providerStore.currentProvider) {
      await providerStore.loadCurrentProvider()
    }

    const provider = providerStore.currentProvider
    console.log(provider)
    if (!provider) {
      errorMessage.value = 'Provider profile not found. Please register as a provider first.'
      return
    }

    formData.businessName = provider.profile.businessName || ''
    formData.type = provider.type || ''
    formData.description = provider.profile.description || ''
    formData.websiteUrl = provider.profile.websiteUrl || ''
    formData.logoUrl = provider.profile.logoUrl || ''
    formData.coverImageUrl = provider.profile.coverImageUrl || ''
    formData.email = provider.contactInfo.email || ''
    formData.primaryPhone = provider.contactInfo.primaryPhone || ''
    formData.secondaryPhone = provider.contactInfo.secondaryPhone || ''
    formData.addressLine1 = provider.address.addressLine1 || ''
    formData.addressLine2 = provider.address.addressLine2 || ''
    formData.city = provider.address.city || ''
    formData.state = provider.address.state || ''
    formData.postalCode = provider.address.postalCode || ''
    formData.country = provider.address.country || ''
    formData.allowOnlineBooking = provider.allowOnlineBooking ?? true
    formData.offersMobileServices = provider.offersMobileServices ?? false
    formData.tags = provider.tags ? provider.tags.join(', ') : ''

    if (provider.profile.socialMediaLinks) {
      formData.socialMedia.facebook = provider.profile.socialMediaLinks.facebook || ''
      formData.socialMedia.instagram = provider.profile.socialMediaLinks.instagram || ''
      formData.socialMedia.twitter = provider.profile.socialMediaLinks.twitter || ''
      formData.socialMedia.linkedin = provider.profile.socialMediaLinks.linkedin || ''
    }
  } catch (error) {
    console.error('Error loading provider data:', error)
    errorMessage.value = 'Failed to load provider data. Please try again.'
  }
})

function validateForm(): boolean {
  // Clear previous errors
  Object.keys(validationErrors).forEach((key) => delete validationErrors[key])

  let isValid = true

  // Required fields
  if (!formData.businessName?.trim()) {
    validationErrors.businessName = 'Business name is required'
    isValid = false
  }

  if (!formData.type) {
    validationErrors.type = 'Business type is required'
    isValid = false
  }

  if (!formData.description?.trim()) {
    validationErrors.description = 'Business description is required'
    isValid = false
  } else if (formData.description.length < 50) {
    validationErrors.description = 'Description should be at least 50 characters'
    isValid = false
  }

  if (!formData.email?.trim()) {
    validationErrors.email = 'Email is required'
    isValid = false
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
    validationErrors.email = 'Invalid email format'
    isValid = false
  }

  if (!formData.primaryPhone?.trim()) {
    validationErrors.primaryPhone = 'Primary phone is required'
    isValid = false
  }

  if (!formData.addressLine1?.trim()) {
    validationErrors.addressLine1 = 'Address is required'
    isValid = false
  }

  if (!formData.city?.trim()) {
    validationErrors.city = 'City is required'
    isValid = false
  }

  if (!formData.state?.trim()) {
    validationErrors.state = 'State is required'
    isValid = false
  }

  if (!formData.postalCode?.trim()) {
    validationErrors.postalCode = 'Postal code is required'
    isValid = false
  }

  if (!formData.country?.trim()) {
    validationErrors.country = 'Country is required'
    isValid = false
  }

  // URL validations
  const urlPattern = /^https?:\/\/.+/
  if (formData.websiteUrl && !urlPattern.test(formData.websiteUrl)) {
    validationErrors.websiteUrl = 'Invalid URL format (must start with http:// or https://)'
    isValid = false
  }

  if (formData.logoUrl && !urlPattern.test(formData.logoUrl)) {
    validationErrors.logoUrl = 'Invalid URL format'
    isValid = false
  }

  if (formData.coverImageUrl && !urlPattern.test(formData.coverImageUrl)) {
    validationErrors.coverImageUrl = 'Invalid URL format'
    isValid = false
  }

  return isValid
}

async function handleSubmit() {
  errorMessage.value = null
  successMessage.value = null

  if (!validateForm()) {
    errorMessage.value = 'Please fix the validation errors before submitting'
    window.scrollTo({ top: 0, behavior: 'smooth' })
    return
  }

  isSaving.value = true

  try {
    const provider = providerStore.currentProvider
    console.log('Current provider before update:', provider)

    if (!provider) {
      throw new Error('Provider not found. Please try refreshing the page.')
    }

    // Prepare update data
    const updateData = {
      businessName: formData.businessName,
      description: formData.description,
      websiteUrl: formData.websiteUrl || undefined,
      logoUrl: formData.logoUrl || undefined,
      coverImageUrl: formData.coverImageUrl || undefined,
      email: formData.email,
      primaryPhone: formData.primaryPhone,
      secondaryPhone: formData.secondaryPhone || undefined,
      addressLine1: formData.addressLine1,
      addressLine2: formData.addressLine2 || undefined,
      city: formData.city,
      state: formData.state,
      postalCode: formData.postalCode,
      country: formData.country,
      allowOnlineBooking: formData.allowOnlineBooking,
      offersMobileServices: formData.offersMobileServices,
      tags: formData.tags
        ? formData.tags
            .split(',')
            .map((tag) => tag.trim())
            .filter(Boolean)
        : undefined,
    }

    console.log('Updating provider with data:', updateData)
    const updatedProvider = await providerStore.updateProvider(provider.id, updateData)

    // Check if update was successful
    if (!updatedProvider) {
      // Check if there's an error in the store
      if (providerStore.error) {
        throw new Error(providerStore.error)
      }
      throw new Error('Failed to update provider. Please try again.')
    }

    console.log('Provider updated successfully:', updatedProvider)
    successMessage.value = 'Business information saved successfully!'
    window.scrollTo({ top: 0, behavior: 'smooth' })

    // Redirect after a short delay
    setTimeout(() => {
      router.push({ name: 'ProviderRegistration' })
    }, 1500)
  } catch (error) {
    console.error('Error saving business information:', error)
    errorMessage.value =
      error instanceof Error ? error.message : 'Failed to save business information'
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    isSaving.value = false
  }
}

function goBack() {
  router.push({ name: 'ProviderRegistration' })
}
</script>

<style scoped>
.business-info-view {
  max-width: 1000px;
  margin: 0 auto;
  padding: 2rem;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 2rem;
  gap: 2rem;
}

.page-title {
  font-size: 2rem;
  font-weight: 700;
  margin: 0 0 0.5rem 0;
  color: #111827;
}

.page-subtitle {
  font-size: 1rem;
  color: #6b7280;
  margin: 0;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  gap: 1rem;
}

.business-form {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.form-section {
  padding: 2rem;
}

.section-title {
  font-size: 1.25rem;
  font-weight: 600;
  margin: 0 0 0.5rem 0;
  color: #111827;
}

.section-description {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0 0 1.5rem 0;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1.5rem;
}

.full-width {
  grid-column: 1 / -1;
}

.full-width-field {
  width: 100%;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.checkbox-group {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-bottom: 1rem;
}

.checkbox-label {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  cursor: pointer;
  padding: 1rem;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  transition: all 0.2s;
}

.checkbox-label:hover {
  border-color: #3b82f6;
  background: #eff6ff;
}

.checkbox {
  width: 1.25rem;
  height: 1.25rem;
  margin-top: 0.125rem;
  cursor: pointer;
  flex-shrink: 0;
}

.checkbox-text {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.checkbox-text strong {
  font-weight: 600;
  color: #111827;
}

.checkbox-text small {
  font-size: 0.875rem;
  color: #6b7280;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  padding-top: 1rem;
  border-top: 1px solid #e5e7eb;
}

@media (max-width: 768px) {
  .business-info-view {
    padding: 1rem;
  }

  .page-header {
    flex-direction: column;
  }

  .form-grid {
    grid-template-columns: 1fr;
  }

  .form-actions {
    flex-direction: column-reverse;
  }

  .form-actions button {
    width: 100%;
  }
}
</style>
