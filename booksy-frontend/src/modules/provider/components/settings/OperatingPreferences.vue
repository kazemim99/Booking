<template>
  <div class="operating-preferences">
    <div class="settings-section">
      <h2 class="section-title">Localization</h2>
      <p class="section-description">Configure timezone, language, and regional settings</p>

      <div class="form-grid">
        <div class="form-group">
          <label class="form-label">Timezone</label>
          <Select
            v-model="localization.timezone"
            :options="timezoneOptions"
            placeholder="Select timezone"
          />
        </div>

        <div class="form-group">
          <label class="form-label">Language</label>
          <Select
            v-model="localization.language"
            :options="languageOptions"
            placeholder="Select language"
          />
        </div>

        <div class="form-group">
          <label class="form-label">Currency</label>
          <Select
            v-model="localization.currency"
            :options="currencyOptions"
            placeholder="Select currency"
          />
        </div>

        <div class="form-group">
          <label class="form-label">Date Format</label>
          <Select
            v-model="localization.dateFormat"
            :options="dateFormatOptions"
            placeholder="Select date format"
          />
        </div>

        <div class="form-group">
          <label class="form-label">Time Format</label>
          <Select
            v-model="localization.timeFormat"
            :options="timeFormatOptions"
            placeholder="Select time format"
          />
        </div>

        <div class="form-group">
          <label class="form-label">Week Starts On</label>
          <Select
            v-model="localization.firstDayOfWeek"
            :options="weekStartOptions"
            placeholder="Select first day"
          />
        </div>
      </div>
    </div>

    <div class="settings-section">
      <h2 class="section-title">Default Service Settings</h2>
      <p class="section-description">Set default values for new services</p>

      <div class="form-grid">
        <div class="form-group">
          <label class="form-label">Default Duration</label>
          <div class="input-with-unit">
            <TextInput
              v-model.number="defaults.duration"
              type="number"
              min="15"
            />
            <span class="input-unit">minutes</span>
          </div>
        </div>

        <div class="form-group">
          <label class="form-label">Default Buffer Time</label>
          <div class="input-with-unit">
            <TextInput
              v-model.number="defaults.bufferTime"
              type="number"
              min="0"
            />
            <span class="input-unit">minutes</span>
          </div>
        </div>

        <div class="form-group">
          <label class="form-label">Default Deposit</label>
          <div class="input-with-unit">
            <TextInput
              v-model.number="defaults.depositPercentage"
              type="number"
              min="0"
              max="100"
            />
            <span class="input-unit">%</span>
          </div>
        </div>
      </div>

      <div class="form-group">
        <Checkbox
          v-model="defaults.allowOnlineBooking"
          label="Allow online booking by default"
        />
      </div>

      <div class="form-group">
        <Checkbox
          v-model="defaults.requiresDeposit"
          label="Require deposit by default"
        />
      </div>
    </div>

    <div class="settings-section">
      <h2 class="section-title">Profile Display Settings</h2>
      <p class="section-description">Control what information is visible on your public profile</p>

      <div class="form-group">
        <Checkbox
          v-model="display.businessHours"
          label="Display business hours on profile"
        />
      </div>

      <div class="form-group">
        <Checkbox
          v-model="display.pricing"
          label="Display pricing on profile"
        />
      </div>

      <div class="form-group">
        <Checkbox
          v-model="display.staff"
          label="Display staff members on profile"
        />
      </div>
    </div>

    <div class="settings-section">
      <h2 class="section-title">Customer Options</h2>
      <p class="section-description">Configure customer booking preferences</p>

      <div class="form-group">
        <Checkbox
          v-model="customer.allowNotes"
          label="Allow customers to add notes to bookings"
        />
      </div>

      <div class="form-group">
        <Checkbox
          v-model="customer.allowCancellation"
          label="Allow customers to cancel bookings"
        />
      </div>

      <div class="form-group">
        <Checkbox
          v-model="customer.allowRescheduling"
          label="Allow customers to reschedule bookings"
        />
      </div>
    </div>

    <div class="settings-actions">
      <Button variant="primary" @click="handleSave">
        Save Preferences
      </Button>
      <Button variant="secondary" @click="handleReset">
        Reset
      </Button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { Button, TextInput, Select, Checkbox } from '@/shared/components'

const localization = ref({
  timezone: 'UTC',
  language: 'en',
  currency: 'USD',
  dateFormat: 'MM/DD/YYYY',
  timeFormat: '12h',
  firstDayOfWeek: '0',
})

const defaults = ref({
  duration: 60,
  bufferTime: 15,
  depositPercentage: 20,
  allowOnlineBooking: true,
  requiresDeposit: false,
})

const display = ref({
  businessHours: true,
  pricing: true,
  staff: true,
})

const customer = ref({
  allowNotes: true,
  allowCancellation: true,
  allowRescheduling: true,
})

// Options
const timezoneOptions = [
  { value: 'UTC', label: 'UTC' },
  { value: 'America/New_York', label: 'Eastern Time (US)' },
  { value: 'America/Chicago', label: 'Central Time (US)' },
  { value: 'America/Denver', label: 'Mountain Time (US)' },
  { value: 'America/Los_Angeles', label: 'Pacific Time (US)' },
  { value: 'Europe/London', label: 'London' },
  { value: 'Europe/Paris', label: 'Paris' },
  { value: 'Asia/Dubai', label: 'Dubai' },
  { value: 'Asia/Tehran', label: 'Tehran' },
]

const languageOptions = [
  { value: 'en', label: 'English' },
  { value: 'fa', label: 'Persian (فارسی)' },
  { value: 'es', label: 'Spanish' },
  { value: 'fr', label: 'French' },
  { value: 'ar', label: 'Arabic' },
]

const currencyOptions = [
  { value: 'USD', label: 'USD ($)' },
  { value: 'EUR', label: 'EUR (€)' },
  { value: 'GBP', label: 'GBP (£)' },
  { value: 'IRR', label: 'IRR (﷼)' },
  { value: 'AED', label: 'AED (د.إ)' },
]

const dateFormatOptions = [
  { value: 'MM/DD/YYYY', label: 'MM/DD/YYYY (12/31/2024)' },
  { value: 'DD/MM/YYYY', label: 'DD/MM/YYYY (31/12/2024)' },
  { value: 'YYYY-MM-DD', label: 'YYYY-MM-DD (2024-12-31)' },
]

const timeFormatOptions = [
  { value: '12h', label: '12-hour (3:00 PM)' },
  { value: '24h', label: '24-hour (15:00)' },
]

const weekStartOptions = [
  { value: '0', label: 'Sunday' },
  { value: '1', label: 'Monday' },
  { value: '6', label: 'Saturday' },
]

function handleSave() {
  console.log('Saving operating preferences...', {
    localization: localization.value,
    defaults: defaults.value,
    display: display.value,
    customer: customer.value,
  })
}

function handleReset() {
  // Reset to defaults
  localization.value = {
    timezone: 'UTC',
    language: 'en',
    currency: 'USD',
    dateFormat: 'MM/DD/YYYY',
    timeFormat: '12h',
    firstDayOfWeek: '0',
  }
}
</script>

<style scoped>
.operating-preferences {
  max-width: 800px;
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

.form-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
  margin-bottom: 1rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.form-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-text-primary);
}

.input-with-unit {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.input-unit {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
  white-space: nowrap;
}

.settings-actions {
  display: flex;
  gap: 1rem;
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 1px solid var(--color-border);
}

@media (max-width: 768px) {
  .form-grid {
    grid-template-columns: 1fr;
  }

  .settings-actions {
    flex-direction: column;
  }

  .settings-actions button {
    width: 100%;
  }
}
</style>
