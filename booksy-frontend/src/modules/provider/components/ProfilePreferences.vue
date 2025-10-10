<template>
  <div class="preferences-tab">
    <div class="tab-header">
      <div>
        <h2 class="tab-title">Preferences</h2>
        <p class="tab-description">Customize your experience and notification settings</p>
      </div>
    </div>

    <form @submit.prevent="handleSubmit" class="profile-form">
      <!-- Localization Section -->
      <div class="form-section">
        <h3 class="section-title">Localization</h3>

        <div class="form-grid">
          <!-- Language -->
          <div class="form-group">
            <label for="language" class="form-label">Language</label>
            <select id="language" v-model="form.language" class="form-select">
              <option value="en">English</option>
              <option value="es">Spanish</option>
              <option value="fr">French</option>
              <option value="de">German</option>
              <option value="ar">Arabic</option>
              <option value="zh">Chinese</option>
            </select>
          </div>

          <!-- Timezone -->
          <div class="form-group">
            <label for="timezone" class="form-label">Timezone</label>
            <select id="timezone" v-model="form.timezone" class="form-select">
              <optgroup label="North America">
                <option value="America/New_York">Eastern Time (ET)</option>
                <option value="America/Chicago">Central Time (CT)</option>
                <option value="America/Denver">Mountain Time (MT)</option>
                <option value="America/Los_Angeles">Pacific Time (PT)</option>
                <option value="America/Anchorage">Alaska Time (AKT)</option>
              </optgroup>
              <optgroup label="Europe">
                <option value="Europe/London">London (GMT)</option>
                <option value="Europe/Paris">Paris (CET)</option>
                <option value="Europe/Berlin">Berlin (CET)</option>
                <option value="Europe/Amsterdam">Amsterdam (CET)</option>
              </optgroup>
              <optgroup label="Asia">
                <option value="Asia/Dubai">Dubai (GST)</option>
                <option value="Asia/Tokyo">Tokyo (JST)</option>
                <option value="Asia/Shanghai">Shanghai (CST)</option>
              </optgroup>
            </select>
          </div>

          <!-- Currency -->
          <div class="form-group">
            <label for="currency" class="form-label">Currency</label>
            <select id="currency" v-model="form.currency" class="form-select">
              <option value="USD">USD - US Dollar ($)</option>
              <option value="EUR">EUR - Euro (€)</option>
              <option value="GBP">GBP - British Pound (£)</option>
              <option value="CAD">CAD - Canadian Dollar ($)</option>
              <option value="AUD">AUD - Australian Dollar ($)</option>
              <option value="JPY">JPY - Japanese Yen (¥)</option>
            </select>
          </div>

          <!-- Date Format -->
          <div class="form-group">
            <label for="dateFormat" class="form-label">Date Format</label>
            <select id="dateFormat" v-model="form.dateFormat" class="form-select">
              <option value="MM/DD/YYYY">MM/DD/YYYY (US)</option>
              <option value="DD/MM/YYYY">DD/MM/YYYY (International)</option>
              <option value="YYYY-MM-DD">YYYY-MM-DD (ISO)</option>
            </select>
            <span class="help-text"> Preview: {{ formatDatePreview }} </span>
          </div>

          <!-- Time Format -->
          <div class="form-group">
            <label for="timeFormat" class="form-label">Time Format</label>
            <select id="timeFormat" v-model="form.timeFormat" class="form-select">
              <option value="12h">12-hour (2:30 PM)</option>
              <option value="24h">24-hour (14:30)</option>
            </select>
          </div>

          <!-- Theme -->
          <div class="form-group">
            <label for="theme" class="form-label">Theme</label>
            <select id="theme" v-model="form.theme" class="form-select">
              <option value="light">Light</option>
              <option value="dark">Dark</option>
              <option value="auto">Auto (System)</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Notifications Section -->
      <div class="form-section">
        <h3 class="section-title">Notification Preferences</h3>

        <div class="notification-grid">
          <!-- Email Notifications -->
          <div class="notification-item">
            <div class="notification-info">
              <div class="notification-icon">📧</div>
              <div>
                <div class="notification-label">Email Notifications</div>
                <div class="notification-description">Receive notifications via email</div>
              </div>
            </div>
            <label class="toggle-switch">
              <input type="checkbox" v-model="form.notifications.email" />
              <span class="toggle-slider"></span>
            </label>
          </div>

          <!-- SMS Notifications -->
          <div class="notification-item">
            <div class="notification-info">
              <div class="notification-icon">💬</div>
              <div>
                <div class="notification-label">SMS Notifications</div>
                <div class="notification-description">Receive text message alerts</div>
              </div>
            </div>
            <label class="toggle-switch">
              <input type="checkbox" v-model="form.notifications.sms" />
              <span class="toggle-slider"></span>
            </label>
          </div>

          <!-- Push Notifications -->
          <div class="notification-item">
            <div class="notification-info">
              <div class="notification-icon">🔔</div>
              <div>
                <div class="notification-label">Push Notifications</div>
                <div class="notification-description">Browser push notifications</div>
              </div>
            </div>
            <label class="toggle-switch">
              <input type="checkbox" v-model="form.notifications.push" />
              <span class="toggle-slider"></span>
            </label>
          </div>

          <!-- Booking Reminders -->
          <div class="notification-item">
            <div class="notification-info">
              <div class="notification-icon">📅</div>
              <div>
                <div class="notification-label">Booking Reminders</div>
                <div class="notification-description">Get reminded about upcoming appointments</div>
              </div>
            </div>
            <label class="toggle-switch">
              <input type="checkbox" v-model="form.notifications.bookingReminders" />
              <span class="toggle-slider"></span>
            </label>
          </div>

          <!-- Promotions -->
          <div class="notification-item">
            <div class="notification-info">
              <div class="notification-icon">🎁</div>
              <div>
                <div class="notification-label">Promotions & Offers</div>
                <div class="notification-description">
                  Special deals from your favorite providers
                </div>
              </div>
            </div>
            <label class="toggle-switch">
              <input type="checkbox" v-model="form.notifications.promotions" />
              <span class="toggle-slider"></span>
            </label>
          </div>

          <!-- Marketing -->
          <div class="notification-item">
            <div class="notification-info">
              <div class="notification-icon">📢</div>
              <div>
                <div class="notification-label">Marketing Communications</div>
                <div class="notification-description">Product updates and news from Booksy</div>
              </div>
            </div>
            <label class="toggle-switch">
              <input type="checkbox" v-model="form.notifications.marketing" />
              <span class="toggle-slider"></span>
            </label>
          </div>

          <!-- Newsletter -->
          <div class="notification-item">
            <div class="notification-info">
              <div class="notification-icon">📰</div>
              <div>
                <div class="notification-label">Newsletter</div>
                <div class="notification-description">Monthly newsletter with tips and trends</div>
              </div>
            </div>
            <label class="toggle-switch">
              <input type="checkbox" v-model="form.notifications.newsletter" />
              <span class="toggle-slider"></span>
            </label>
          </div>
        </div>

        <!-- Quick Actions -->
        <div class="quick-actions">
          <button type="button" @click="enableAllNotifications" class="btn-text">Enable All</button>
          <button type="button" @click="disableAllNotifications" class="btn-text">
            Disable All
          </button>
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
        <button type="submit" class="btn btn-primary" :disabled="isSaving || !hasChanges">
          <span v-if="isSaving" class="btn-spinner"></span>
          {{ isSaving ? 'Saving...' : 'Save Changes' }}
        </button>
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
import { UpdatePreferencesRequest } from '@/modules/user-management/types/user-profile.types'
import { UserPreferences } from '@/modules/user-management/types/user.types'
import { ref, computed, watch, onMounted } from 'vue'

// Props
const props = defineProps<{
  preferences: UserPreferences
  isSaving: boolean
}>()

// Emits
const emit = defineEmits<{
  update: [data: UpdatePreferencesRequest]
}>()

// Form state
const form = ref({
  language: 'en',
  timezone: 'America/New_York',
  currency: 'USD',
  dateFormat: 'MM/DD/YYYY',
  timeFormat: '12h' as '12h' | '24h',
  theme: 'light' as 'light' | 'dark' | 'auto',
  notifications: {
    email: true,
    sms: true,
    push: true,
    marketing: false,
    bookingReminders: true,
    promotions: true,
    newsletter: false,
  },
})

const initialForm = ref<string>('')

// Computed
const formatDatePreview = computed(() => {
  const date = new Date()
  const day = String(date.getDate()).padStart(2, '0')
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const year = date.getFullYear()

  switch (form.value.dateFormat) {
    case 'MM/DD/YYYY':
      return `${month}/${day}/${year}`
    case 'DD/MM/YYYY':
      return `${day}/${month}/${year}`
    case 'YYYY-MM-DD':
      return `${year}-${month}-${day}`
    default:
      return `${month}/${day}/${year}`
  }
})

const hasChanges = computed(() => {
  return JSON.stringify(form.value) !== initialForm.value
})

// Methods
function initializeForm() {
  form.value = {
    language: props.preferences.language || 'en',
    timezone: props.preferences.timezone || 'America/New_York',
    currency: props.preferences.currency || 'USD',
    dateFormat: props.preferences.dateFormat || 'MM/DD/YYYY',
    timeFormat: (props.preferences.timeFormat as '12h' | '24h') || '12h',
    theme: (props.preferences.theme as 'light' | 'dark' | 'auto') || 'light',
    notifications: {
      email: props.preferences.notifications?.email ?? true,
      sms: props.preferences.notifications?.sms ?? true,
      push: props.preferences.notifications?.push ?? true,
      marketing: props.preferences.notifications?.marketing ?? false,
      bookingReminders: props.preferences.notifications?.bookingReminders ?? true,
      promotions: props.preferences.notifications?.promotions ?? true,
      newsletter: props.preferences.notifications?.newsletter ?? false,
    },
  }

  initialForm.value = JSON.stringify(form.value)
}

function handleSubmit() {
  const updateData: UpdatePreferencesRequest = {
    language: form.value.language,
    timezone: form.value.timezone,
    currency: form.value.currency,
    dateFormat: form.value.dateFormat,
    timeFormat: form.value.timeFormat,
  }

  emit('update', updateData)
}

function handleReset() {
  initializeForm()
}

function enableAllNotifications() {
  Object.keys(form.value.notifications).forEach((key) => {
    form.value.notifications[key as keyof typeof form.value.notifications] = true
  })
}

function disableAllNotifications() {
  Object.keys(form.value.notifications).forEach((key) => {
    form.value.notifications[key as keyof typeof form.value.notifications] = false
  })
}

// Watchers
watch(
  () => props.preferences,
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
.preferences-tab {
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
}

.form-label {
  font-weight: 500;
  color: var(--color-text-primary);
  font-size: 0.875rem;
}

.form-select {
  padding: 0.75rem 1rem;
  border: 1px solid var(--color-border);
  border-radius: 0.375rem;
  font-size: 0.9375rem;
  transition: all 0.2s;
  font-family: inherit;
  background: white;

  &:focus {
    outline: none;
    border-color: var(--color-primary);
    box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
  }
}

.help-text {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.notification-grid {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.notification-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.25rem;
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  transition: all 0.2s;

  &:hover {
    border-color: var(--color-primary-light);
    box-shadow: 0 2px 8px rgba(99, 102, 241, 0.1);
  }
}

.notification-info {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex: 1;
}

.notification-icon {
  font-size: 1.75rem;
  width: 48px;
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: white;
  border-radius: 0.5rem;
  border: 1px solid var(--color-border);
}

.notification-label {
  font-weight: 500;
  color: var(--color-text-primary);
  margin-bottom: 0.25rem;
}

.notification-description {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
}

// Toggle Switch
.toggle-switch {
  position: relative;
  display: inline-block;
  width: 52px;
  height: 28px;
  flex-shrink: 0;

  input {
    opacity: 0;
    width: 0;
    height: 0;

    &:checked + .toggle-slider {
      background-color: var(--color-primary);

      &::before {
        transform: translateX(24px);
      }
    }

    &:focus + .toggle-slider {
      box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.2);
    }
  }
}

.toggle-slider {
  position: absolute;
  cursor: pointer;
  inset: 0;
  background-color: #cbd5e1;
  transition: 0.3s;
  border-radius: 28px;

  &::before {
    position: absolute;
    content: '';
    height: 20px;
    width: 20px;
    left: 4px;
    bottom: 4px;
    background-color: white;
    transition: 0.3s;
    border-radius: 50%;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
  }
}

.quick-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid var(--color-border);
}

.btn-text {
  background: none;
  border: none;
  color: var(--color-primary);
  font-weight: 500;
  cursor: pointer;
  padding: 0.5rem 1rem;
  border-radius: 0.375rem;
  transition: all 0.2s;

  &:hover {
    background: rgba(99, 102, 241, 0.1);
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
