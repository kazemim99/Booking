<template>
  <div class="notification-settings">
    <div class="settings-section">
      <h2 class="section-title">Booking Notifications</h2>
      <p class="section-description">Configure when you receive notifications about bookings</p>

      <div class="notification-preferences">
        <div class="notification-item">
          <div class="notification-info">
            <h4>New Booking</h4>
            <p>Receive notifications when customers make new bookings</p>
          </div>
          <div class="notification-channels">
            <Checkbox v-model="notifications.newBooking.email" label="Email" />
            <Checkbox v-model="notifications.newBooking.sms" label="SMS" />
            <Checkbox v-model="notifications.newBooking.push" label="Push" />
          </div>
        </div>

        <div class="notification-item">
          <div class="notification-info">
            <h4>Booking Cancelled</h4>
            <p>Receive notifications when bookings are cancelled</p>
          </div>
          <div class="notification-channels">
            <Checkbox v-model="notifications.cancelled.email" label="Email" />
            <Checkbox v-model="notifications.cancelled.sms" label="SMS" />
            <Checkbox v-model="notifications.cancelled.push" label="Push" />
          </div>
        </div>

        <div class="notification-item">
          <div class="notification-info">
            <h4>Booking Rescheduled</h4>
            <p>Receive notifications when bookings are rescheduled</p>
          </div>
          <div class="notification-channels">
            <Checkbox v-model="notifications.rescheduled.email" label="Email" />
            <Checkbox v-model="notifications.rescheduled.sms" label="SMS" />
            <Checkbox v-model="notifications.rescheduled.push" label="Push" />
          </div>
        </div>
      </div>
    </div>

    <div class="settings-section">
      <h2 class="section-title">Reminder Notifications</h2>
      <p class="section-description">Send reminders to yourself and customers</p>

      <div class="form-group">
        <label class="form-label">Customer reminder</label>
        <div class="input-with-unit">
          <TextInput
            v-model.number="reminders.customerHours"
            type="number"
            min="0"
          />
          <span class="input-unit">hours before appointment</span>
        </div>
      </div>

      <div class="form-group">
        <label class="form-label">Provider reminder</label>
        <div class="input-with-unit">
          <TextInput
            v-model.number="reminders.providerMinutes"
            type="number"
            min="0"
          />
          <span class="input-unit">minutes before appointment</span>
        </div>
      </div>
    </div>

    <div class="settings-section">
      <h2 class="section-title">Quiet Hours</h2>
      <p class="section-description">Don't send notifications during specific hours</p>

      <div class="form-group">
        <Checkbox v-model="quietHours.enabled" label="Enable quiet hours" />
      </div>

      <div v-if="quietHours.enabled" class="form-grid">
        <div class="form-group">
          <label class="form-label">Start time</label>
          <TextInput v-model="quietHours.start" type="text" placeholder="HH:mm" />
        </div>

        <div class="form-group">
          <label class="form-label">End time</label>
          <TextInput v-model="quietHours.end" type="text" placeholder="HH:mm" />
        </div>
      </div>
    </div>

    <div class="settings-actions">
      <Button variant="primary" @click="handleSave">
        Save Changes
      </Button>
      <Button variant="secondary" @click="handleReset">
        Reset
      </Button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { Button, TextInput, Checkbox } from '@/shared/components'

const notifications = ref({
  newBooking: { email: true, sms: false, push: true },
  cancelled: { email: true, sms: true, push: false },
  rescheduled: { email: true, sms: false, push: false },
})

const reminders = ref({
  customerHours: 24,
  providerMinutes: 30,
})

const quietHours = ref({
  enabled: false,
  start: '22:00',
  end: '08:00',
})

function handleSave() {
  console.log('Saving notification settings...', { notifications: notifications.value, reminders: reminders.value, quietHours: quietHours.value })
}

function handleReset() {
  // Reset to defaults
  notifications.value = {
    newBooking: { email: true, sms: false, push: true },
    cancelled: { email: true, sms: true, push: false },
    rescheduled: { email: true, sms: false, push: false },
  }
}
</script>

<style scoped>
.notification-settings {
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

.notification-preferences {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.notification-item {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: 1rem;
  background: var(--color-background-subtle);
  border-radius: 8px;
  gap: 2rem;
}

.notification-info h4 {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-text-primary);
  margin: 0 0 0.25rem 0;
}

.notification-info p {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
  margin: 0;
}

.notification-channels {
  display: flex;
  gap: 1rem;
  flex-shrink: 0;
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
  .notification-item {
    flex-direction: column;
  }

  .settings-actions {
    flex-direction: column;
  }

  .settings-actions button {
    width: 100%;
  }
}
</style>
