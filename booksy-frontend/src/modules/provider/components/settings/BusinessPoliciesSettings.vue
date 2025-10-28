<template>
  <div class="business-policies-settings">
    <div class="settings-section">
      <h2 class="section-title">Cancellation Policy</h2>
      <p class="section-description">Define your cancellation policy for customers</p>

      <div class="form-group">
        <label class="form-label">Policy Content</label>
        <textarea
          v-model="policies.cancellation"
          class="policy-textarea"
          rows="6"
          placeholder="Enter your cancellation policy..."
        ></textarea>
        <p class="form-help">This policy will be displayed to customers when they book services</p>
      </div>

      <div class="form-group">
        <Checkbox
          v-model="policySettings.cancellation.displayOnBooking"
          label="Display on booking page"
        />
      </div>

      <div class="form-group">
        <Checkbox
          v-model="policySettings.cancellation.requireAcceptance"
          label="Require customer acceptance"
        />
      </div>
    </div>

    <div class="settings-section">
      <h2 class="section-title">Privacy Policy</h2>
      <p class="section-description">Define how you handle customer data and privacy</p>

      <div class="form-group">
        <label class="form-label">Policy Content</label>
        <textarea
          v-model="policies.privacy"
          class="policy-textarea"
          rows="8"
          placeholder="Enter your privacy policy..."
        ></textarea>
        <p class="form-help">Explain how you collect, use, and protect customer information</p>
      </div>

      <div class="form-group">
        <Checkbox
          v-model="policySettings.privacy.displayOnBooking"
          label="Display on booking page"
        />
      </div>

      <div class="form-group">
        <Checkbox
          v-model="policySettings.privacy.requireAcceptance"
          label="Require customer acceptance"
        />
      </div>
    </div>

    <div class="settings-section">
      <h2 class="section-title">Terms and Conditions</h2>
      <p class="section-description">Set the terms and conditions for using your services</p>

      <div class="form-group">
        <label class="form-label">Terms Content</label>
        <textarea
          v-model="policies.terms"
          class="policy-textarea"
          rows="8"
          placeholder="Enter your terms and conditions..."
        ></textarea>
        <p class="form-help">Define the rules and requirements for service bookings</p>
      </div>

      <div class="form-group">
        <Checkbox
          v-model="policySettings.terms.displayOnBooking"
          label="Display on booking page"
        />
      </div>

      <div class="form-group">
        <Checkbox
          v-model="policySettings.terms.requireAcceptance"
          label="Require customer acceptance"
        />
      </div>
    </div>

    <div class="policy-preview">
      <h3>📄 Policy Preview</h3>
      <p>This is how your policies will appear to customers</p>
      <Button variant="secondary" size="small" @click="showPreview = !showPreview">
        {{ showPreview ? 'Hide Preview' : 'Show Preview' }}
      </Button>

      <div v-if="showPreview" class="preview-content">
        <div class="preview-section" v-if="policySettings.cancellation.displayOnBooking">
          <h4>Cancellation Policy</h4>
          <p>{{ policies.cancellation || 'No cancellation policy set' }}</p>
        </div>
        <div class="preview-section" v-if="policySettings.privacy.displayOnBooking">
          <h4>Privacy Policy</h4>
          <p>{{ policies.privacy || 'No privacy policy set' }}</p>
        </div>
        <div class="preview-section" v-if="policySettings.terms.displayOnBooking">
          <h4>Terms and Conditions</h4>
          <p>{{ policies.terms || 'No terms set' }}</p>
        </div>
      </div>
    </div>

    <div class="settings-actions">
      <Button variant="primary" @click="handleSave">
        Save Policies
      </Button>
      <Button variant="secondary" @click="handleReset">
        Reset
      </Button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { Button, Checkbox } from '@/shared/components'

const showPreview = ref(false)

const policies = ref({
  cancellation: 'Customers may cancel or reschedule appointments up to 24 hours in advance without penalty. Cancellations made less than 24 hours before the appointment may be subject to a cancellation fee.',
  privacy: 'We respect your privacy and protect your personal information. Your data is used solely for appointment management and will not be shared with third parties without your consent.',
  terms: 'By booking a service, you agree to arrive on time for your appointment. Late arrivals may result in shortened service time or rescheduling. Payment is due at the time of service unless otherwise arranged.',
})

const policySettings = ref({
  cancellation: {
    displayOnBooking: true,
    requireAcceptance: false,
  },
  privacy: {
    displayOnBooking: true,
    requireAcceptance: true,
  },
  terms: {
    displayOnBooking: true,
    requireAcceptance: true,
  },
})

function handleSave() {
  console.log('Saving business policies...', { policies: policies.value, settings: policySettings.value })
}

function handleReset() {
  // Reset to defaults
  policies.value = {
    cancellation: '',
    privacy: '',
    terms: '',
  }
}
</script>

<style scoped>
.business-policies-settings {
  max-width: 900px;
}

.settings-section {
  margin-bottom: 2.5rem;
  padding-bottom: 2rem;
  border-bottom: 1px solid var(--color-border);
}

.section-title {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--color-text-primary);
  margin: 0 0 0.5rem 0;
}

.section-description {
  font-size: 0.9375rem;
  color: var(--color-text-secondary);
  margin: 0 0 1.5rem 0;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-text-primary);
  margin-bottom: 0.5rem;
}

.form-help {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
  margin: 0.5rem 0 0 0;
  line-height: 1.4;
}

.policy-textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  font-family: inherit;
  font-size: 0.9375rem;
  line-height: 1.6;
  resize: vertical;
  transition: border-color 0.2s;
}

.policy-textarea:focus {
  outline: none;
  border-color: var(--color-primary);
}

.policy-preview {
  margin: 2rem 0;
  padding: 1.5rem;
  background: var(--color-background-subtle);
  border-radius: 8px;
}

.policy-preview h3 {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--color-text-primary);
  margin: 0 0 0.5rem 0;
}

.policy-preview > p {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
  margin: 0 0 1rem 0;
}

.preview-content {
  margin-top: 1.5rem;
  padding: 1.5rem;
  background: white;
  border: 1px solid var(--color-border);
  border-radius: 8px;
}

.preview-section {
  margin-bottom: 1.5rem;
}

.preview-section:last-child {
  margin-bottom: 0;
}

.preview-section h4 {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-text-primary);
  margin: 0 0 0.5rem 0;
}

.preview-section p {
  font-size: 0.9375rem;
  color: var(--color-text-secondary);
  margin: 0;
  line-height: 1.6;
  white-space: pre-wrap;
}

.settings-actions {
  display: flex;
  gap: 1rem;
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 1px solid var(--color-border);
}

@media (max-width: 768px) {
  .settings-actions {
    flex-direction: column;
  }

  .settings-actions button {
    width: 100%;
  }
}
</style>
