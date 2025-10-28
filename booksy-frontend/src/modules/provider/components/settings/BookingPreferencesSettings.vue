<template>
  <div class="booking-preferences-settings">
    <div class="settings-section">
      <h2 class="section-title">Booking Window</h2>
      <p class="section-description">Control how far in advance customers can book appointments</p>

      <div class="form-grid">
        <div class="form-group">
          <label class="form-label">Minimum advance booking</label>
          <div class="input-with-unit">
            <TextInput
              v-model.number="localSettings.bookingWindow.minAdvanceBookingHours"
              type="number"
              min="0"
              @input="markChanged"
            />
            <span class="input-unit">hours</span>
          </div>
          <p class="form-help">Customers must book at least this many hours in advance</p>
        </div>

        <div class="form-group">
          <label class="form-label">Maximum advance booking</label>
          <div class="input-with-unit">
            <TextInput
              v-model.number="localSettings.bookingWindow.maxAdvanceBookingDays"
              type="number"
              min="1"
              @input="markChanged"
            />
            <span class="input-unit">days</span>
          </div>
          <p class="form-help">Customers can book up to this many days in advance</p>
        </div>
      </div>
    </div>

    <div class="settings-section">
      <h2 class="section-title">Booking Approval</h2>
      <p class="section-description">Control whether bookings require manual approval</p>

      <div class="form-group">
        <Checkbox
          v-model="localSettings.approval.requiresApproval"
          label="Require approval for all bookings"
          @update:model-value="markChanged"
        />
        <p class="form-help">When enabled, you must manually approve each booking</p>
      </div>

      <div class="form-group">
        <Checkbox
          v-model="localSettings.approval.autoApproveForReturningCustomers"
          label="Auto-approve returning customers"
          :disabled="!localSettings.approval.requiresApproval"
          @update:model-value="markChanged"
        />
        <p class="form-help">Automatically approve bookings from customers who have completed previous appointments</p>
      </div>

      <div class="form-group" v-if="localSettings.approval.autoApproveForReturningCustomers">
        <label class="form-label">Auto-approve after</label>
        <div class="input-with-unit">
          <TextInput
            v-model.number="localSettings.approval.autoApproveThresholdBookings"
            type="number"
            min="1"
            @input="markChanged"
          />
          <span class="input-unit">completed bookings</span>
        </div>
      </div>
    </div>

    <div class="settings-section">
      <h2 class="section-title">Cancellation & Rescheduling</h2>
      <p class="section-description">Set policies for cancellations and reschedules</p>

      <div class="form-grid">
        <div class="form-group">
          <Checkbox
            v-model="localSettings.cancellationPolicy.allowCancellation"
            label="Allow customers to cancel bookings"
            @update:model-value="markChanged"
          />
        </div>

        <div class="form-group" v-if="localSettings.cancellationPolicy.allowCancellation">
          <label class="form-label">Cancellation window</label>
          <div class="input-with-unit">
            <TextInput
              v-model.number="localSettings.cancellationPolicy.cancellationWindowHours"
              type="number"
              min="0"
              @input="markChanged"
            />
            <span class="input-unit">hours before appointment</span>
          </div>
        </div>
      </div>

      <div class="form-grid">
        <div class="form-group">
          <Checkbox
            v-model="localSettings.cancellationPolicy.chargeNoShowFee"
            label="Charge no-show fee"
            @update:model-value="markChanged"
          />
        </div>

        <div class="form-group" v-if="localSettings.cancellationPolicy.chargeNoShowFee">
          <label class="form-label">No-show fee</label>
          <div class="input-with-unit">
            <TextInput
              v-model.number="localSettings.cancellationPolicy.noShowFeePercentage"
              type="number"
              min="0"
              max="100"
              @input="markChanged"
            />
            <span class="input-unit">% of service price</span>
          </div>
        </div>
      </div>

      <div class="form-grid">
        <div class="form-group">
          <Checkbox
            v-model="localSettings.cancellationPolicy.allowRescheduling"
            label="Allow customers to reschedule bookings"
            @update:model-value="markChanged"
          />
        </div>

        <div class="form-group" v-if="localSettings.cancellationPolicy.allowRescheduling">
          <label class="form-label">Reschedule window</label>
          <div class="input-with-unit">
            <TextInput
              v-model.number="localSettings.cancellationPolicy.rescheduleWindowHours"
              type="number"
              min="0"
              @input="markChanged"
            />
            <span class="input-unit">hours before appointment</span>
          </div>
        </div>
      </div>
    </div>

    <div class="settings-section">
      <h2 class="section-title">Deposit Settings</h2>
      <p class="section-description">Require deposits for bookings to reduce no-shows</p>

      <div class="form-group">
        <Checkbox
          v-model="localSettings.depositSettings.requiresDeposit"
          label="Require deposit for bookings"
          @update:model-value="markChanged"
        />
      </div>

      <div v-if="localSettings.depositSettings.requiresDeposit" class="deposit-settings">
        <div class="form-group">
          <label class="form-label">Deposit amount</label>
          <div class="input-with-unit">
            <TextInput
              v-model.number="localSettings.depositSettings.depositPercentage"
              type="number"
              min="0"
              max="100"
              @input="markChanged"
            />
            <span class="input-unit">% of service price</span>
          </div>
        </div>

        <div class="form-group">
          <Checkbox
            v-model="localSettings.depositSettings.refundableDeposit"
            label="Deposit is refundable"
            @update:model-value="markChanged"
          />
        </div>

        <div class="form-group" v-if="localSettings.depositSettings.refundableDeposit">
          <label class="form-label">Refund window</label>
          <div class="input-with-unit">
            <TextInput
              v-model.number="localSettings.depositSettings.refundWindowHours"
              type="number"
              min="0"
              @input="markChanged"
            />
            <span class="input-unit">hours before appointment</span>
          </div>
        </div>
      </div>
    </div>

    <div class="settings-section">
      <h2 class="section-title">Other Booking Options</h2>

      <div class="form-group">
        <Checkbox
          v-model="localSettings.allowOnlinePayment"
          label="Allow online payment"
          @update:model-value="markChanged"
        />
        <p class="form-help">Customers can pay online when booking</p>
      </div>

      <div class="form-group">
        <Checkbox
          v-model="localSettings.allowWalkins"
          label="Accept walk-in customers"
          @update:model-value="markChanged"
        />
        <p class="form-help">Allow customers to walk in without appointments</p>
      </div>

      <div class="form-group">
        <label class="form-label">Buffer between bookings</label>
        <div class="input-with-unit">
          <TextInput
            v-model.number="localSettings.bufferBetweenBookings"
            type="number"
            min="0"
            @input="markChanged"
          />
          <span class="input-unit">minutes</span>
        </div>
        <p class="form-help">Time gap between appointments for cleanup and preparation</p>
      </div>
    </div>

    <!-- Save Button -->
    <div class="settings-actions">
      <Button
        variant="primary"
        :disabled="!hasChanges || settingsStore.isSaving"
        @click="handleSave"
      >
        {{ settingsStore.isSaving ? 'Saving...' : 'Save Changes' }}
      </Button>
      <Button
        variant="secondary"
        :disabled="!hasChanges"
        @click="handleReset"
      >
        Reset
      </Button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useSettingsStore } from '../../stores/settings.store'
import { useProviderStore } from '../../stores/provider.store'
import { Button, TextInput, Checkbox } from '@/shared/components'
import type { BookingPreferences } from '../../types/settings.types'

const settingsStore = useSettingsStore()
const providerStore = useProviderStore()

// Local state for form
const hasChanges = ref(false)
const localSettings = ref<BookingPreferences>(getDefaultSettings())

// Watch for settings changes from store
watch(
  () => settingsStore.bookingPreferences,
  (newSettings) => {
    if (newSettings && !hasChanges.value) {
      localSettings.value = JSON.parse(JSON.stringify(newSettings))
    }
  },
  { immediate: true, deep: true }
)

function getDefaultSettings(): BookingPreferences {
  return settingsStore.bookingPreferences || {
    bookingWindow: {
      minAdvanceBookingHours: 2,
      maxAdvanceBookingDays: 90,
    },
    approval: {
      requiresApproval: false,
      autoApproveForReturningCustomers: true,
      autoApproveThresholdBookings: 3,
    },
    cancellationPolicy: {
      allowCancellation: true,
      cancellationWindowHours: 24,
      chargeNoShowFee: false,
      allowRescheduling: true,
      rescheduleWindowHours: 12,
    },
    depositSettings: {
      requiresDeposit: false,
      depositType: 'Percentage',
      depositPercentage: 20,
      refundableDeposit: true,
      refundWindowHours: 48,
    },
    allowOnlinePayment: true,
    allowWalkins: true,
    bufferBetweenBookings: 15,
  }
}

function markChanged() {
  hasChanges.value = true
  settingsStore.markAsChanged()
}

async function handleSave() {
  if (!providerStore.currentProvider?.id) return

  try {
    await settingsStore.updateBookingPreferences({
      providerId: providerStore.currentProvider.id,
      bookingPreferences: localSettings.value,
    })
    hasChanges.value = false
  } catch (error) {
    console.error('Failed to save booking preferences:', error)
  }
}

function handleReset() {
  if (settingsStore.bookingPreferences) {
    localSettings.value = JSON.parse(JSON.stringify(settingsStore.bookingPreferences))
    hasChanges.value = false
  }
}
</script>

<style scoped>
.booking-preferences-settings {
  max-width: 800px;
}

.settings-section {
  margin-bottom: 2.5rem;
  padding-bottom: 2rem;
  border-bottom: 1px solid var(--color-border);
}

.settings-section:last-of-type {
  border-bottom: none;
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
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 1.5rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-text-primary);
}

.form-help {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
  margin: 0;
  line-height: 1.4;
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

.deposit-settings {
  margin-top: 1rem;
  padding-left: 1.5rem;
  border-left: 3px solid var(--color-primary);
  display: flex;
  flex-direction: column;
  gap: 1rem;
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
