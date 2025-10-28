<template>
  <div class="provider-settings-view">
    <!-- Header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">Settings</h1>
        <p class="page-subtitle">Configure your business settings and preferences</p>
      </div>
      <div class="header-actions">
        <span v-if="settingsStore.hasUnsavedChanges" class="unsaved-indicator">
          Unsaved changes
        </span>
        <Button
          v-if="settingsStore.hasUnsavedChanges"
          variant="secondary"
          size="small"
          @click="handleDiscardChanges"
        >
          Discard
        </Button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="settingsStore.isLoading" class="loading-state">
      <Spinner />
      <p>Loading settings...</p>
    </div>

    <!-- Error State -->
    <Alert
      v-if="settingsStore.error"
      type="error"
      :message="settingsStore.error"
      @dismiss="settingsStore.clearError()"
    />

    <!-- Success Message -->
    <Alert
      v-if="settingsStore.successMessage"
      type="success"
      :message="settingsStore.successMessage"
      @dismiss="settingsStore.clearSuccess()"
    />

    <!-- Settings Tabs -->
    <div v-if="!settingsStore.isLoading && settingsStore.hasSettings" class="settings-container">
      <Card class="settings-card">
        <!-- Tab Navigation -->
        <div class="tabs-nav">
          <button
            v-for="tab in settingsTabs"
            :key="tab.id"
            class="tab-button"
            :class="{ active: activeTab === tab.id }"
            @click="handleTabChange(tab.id)"
          >
            <span class="tab-icon">{{ tab.icon }}</span>
            <div class="tab-content">
              <span class="tab-label">{{ tab.label }}</span>
              <span class="tab-description">{{ tab.description }}</span>
            </div>
            <span v-if="tab.badge" class="tab-badge">{{ tab.badge }}</span>
          </button>
        </div>

        <!-- Tab Content -->
        <div class="tabs-content">
          <!-- Booking Preferences -->
          <div v-if="activeTab === 'booking-preferences'" class="tab-panel">
            <BookingPreferencesSettings />
          </div>

          <!-- Notifications -->
          <div v-if="activeTab === 'notifications'" class="tab-panel">
            <NotificationSettings />
          </div>

          <!-- Business Policies -->
          <div v-if="activeTab === 'business-policies'" class="tab-panel">
            <BusinessPoliciesSettings />
          </div>

          <!-- Operating Preferences -->
          <div v-if="activeTab === 'operating-preferences'" class="tab-panel">
            <OperatingPreferences />
          </div>

          <!-- Integrations -->
          <div v-if="activeTab === 'integrations'" class="tab-panel">
            <div class="placeholder-content">
              <h3>Integrations</h3>
              <p>Calendar sync, payment gateways, and social media connections</p>
              <p class="muted">Coming soon...</p>
            </div>
          </div>

          <!-- Account Security -->
          <div v-if="activeTab === 'account-security'" class="tab-panel">
            <div class="placeholder-content">
              <h3>Account Security</h3>
              <p>Password, two-factor authentication, and trusted devices</p>
              <p class="muted">Coming soon...</p>
            </div>
          </div>
        </div>
      </Card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useSettingsStore } from '../stores/settings.store'
import { useProviderStore } from '../stores/provider.store'
import { Button, Card, Alert, Spinner } from '@/shared/components'
import { SettingsSection, type SettingsTab } from '../types/settings.types'
import BookingPreferencesSettings from '../components/settings/BookingPreferencesSettings.vue'
import NotificationSettings from '../components/settings/NotificationSettings.vue'
import BusinessPoliciesSettings from '../components/settings/BusinessPoliciesSettings.vue'
import OperatingPreferences from '../components/settings/OperatingPreferences.vue'

const router = useRouter()
const settingsStore = useSettingsStore()
const providerStore = useProviderStore()

// Active tab state
const activeTab = ref<SettingsSection>(SettingsSection.BookingPreferences)

// Settings tabs configuration
const settingsTabs = computed<SettingsTab[]>(() => [
  {
    id: SettingsSection.BookingPreferences,
    label: 'Booking Preferences',
    icon: '📅',
    description: 'Booking rules, deposits, and cancellation policies',
  },
  {
    id: SettingsSection.Notifications,
    label: 'Notifications',
    icon: '🔔',
    description: 'Email, SMS, and push notification preferences',
  },
  {
    id: SettingsSection.BusinessPolicies,
    label: 'Business Policies',
    icon: '📜',
    description: 'Terms, privacy policy, and cancellation policy',
  },
  {
    id: SettingsSection.OperatingPreferences,
    label: 'Operating Preferences',
    icon: '⚙️',
    description: 'Timezone, language, currency, and defaults',
  },
  {
    id: SettingsSection.Integrations,
    label: 'Integrations',
    icon: '🔌',
    description: 'Calendar, payments, and social media',
    badge: settingsStore.hasActiveIntegrations ? '✓' : undefined,
  },
  {
    id: SettingsSection.AccountSecurity,
    label: 'Account & Security',
    icon: '🔒',
    description: 'Password, 2FA, and permissions',
    badge: settingsStore.twoFactorEnabled ? '✓' : undefined,
  },
])

// ============================================
// Lifecycle
// ============================================

onMounted(async () => {
  // Load current provider first
  await providerStore.loadCurrentProvider()

  if (providerStore.currentProvider?.id) {
    // Load settings for the current provider
    await settingsStore.loadSettings(providerStore.currentProvider.id)
  } else {
    console.warn('[ProviderSettingsView] No current provider found')
  }
})

// ============================================
// Event Handlers
// ============================================

function handleTabChange(tabId: SettingsSection) {
  // Warn about unsaved changes
  if (settingsStore.hasUnsavedChanges) {
    const confirmed = confirm('You have unsaved changes. Are you sure you want to switch tabs?')
    if (!confirmed) return
  }

  activeTab.value = tabId
  settingsStore.setCurrentSection(tabId)
}

function handleDiscardChanges() {
  const confirmed = confirm('Are you sure you want to discard all unsaved changes?')
  if (!confirmed) return

  // Reload settings to reset changes
  if (providerStore.currentProvider?.id) {
    settingsStore.loadSettings(providerStore.currentProvider.id)
  }
}
</script>

<style scoped>
.provider-settings-view {
  padding: 2rem;
  max-width: 1400px;
  margin: 0 auto;
}

/* Header */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 2rem;
}

.page-title {
  font-size: 2rem;
  font-weight: 700;
  color: var(--color-text-primary);
  margin: 0 0 0.5rem 0;
}

.page-subtitle {
  font-size: 1rem;
  color: var(--color-text-secondary);
  margin: 0;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.unsaved-indicator {
  font-size: 0.875rem;
  color: var(--color-warning);
  font-weight: 500;
}

/* Loading State */
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 0;
  gap: 1rem;
}

.loading-state p {
  color: var(--color-text-secondary);
  margin: 0;
}

/* Settings Container */
.settings-container {
  margin-top: 1.5rem;
}

.settings-card {
  overflow: hidden;
}

/* Tabs Navigation */
.tabs-nav {
  display: flex;
  flex-direction: column;
  border-right: 1px solid var(--color-border);
  background: var(--color-background-subtle);
  min-width: 280px;
}

.tab-button {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem 1.5rem;
  border: none;
  background: transparent;
  cursor: pointer;
  transition: all 0.2s;
  text-align: left;
  border-left: 3px solid transparent;
}

.tab-button:hover {
  background: var(--color-background-hover);
}

.tab-button.active {
  background: var(--color-background);
  border-left-color: var(--color-primary);
}

.tab-icon {
  font-size: 1.5rem;
  flex-shrink: 0;
}

.tab-content {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  flex: 1;
}

.tab-label {
  font-size: 0.9375rem;
  font-weight: 600;
  color: var(--color-text-primary);
}

.tab-description {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
  line-height: 1.4;
}

.tab-badge {
  font-size: 0.75rem;
  padding: 0.25rem 0.5rem;
  background: var(--color-success);
  color: white;
  border-radius: 12px;
  font-weight: 600;
}

/* Tab Content */
.tabs-content {
  flex: 1;
  padding: 2rem;
  min-height: 500px;
}

.tab-panel {
  animation: fadeIn 0.2s ease-in;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(8px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Placeholder Content */
.placeholder-content {
  text-align: center;
  padding: 3rem 0;
}

.placeholder-content h3 {
  font-size: 1.5rem;
  font-weight: 600;
  color: var(--color-text-primary);
  margin: 0 0 0.5rem 0;
}

.placeholder-content p {
  font-size: 1rem;
  color: var(--color-text-secondary);
  margin: 0.5rem 0;
}

.placeholder-content .muted {
  color: var(--color-text-tertiary);
  font-style: italic;
}

/* Responsive */
@media (max-width: 768px) {
  .provider-settings-view {
    padding: 1rem;
  }

  .page-header {
    flex-direction: column;
    gap: 1rem;
  }

  .header-actions {
    width: 100%;
    justify-content: flex-end;
  }

  .settings-card {
    display: flex;
    flex-direction: column;
  }

  .tabs-nav {
    flex-direction: row;
    overflow-x: auto;
    border-right: none;
    border-bottom: 1px solid var(--color-border);
    min-width: unset;
  }

  .tab-button {
    flex-direction: column;
    text-align: center;
    padding: 1rem;
    min-width: 120px;
    border-left: none;
    border-bottom: 3px solid transparent;
  }

  .tab-button.active {
    border-left-color: transparent;
    border-bottom-color: var(--color-primary);
  }

  .tab-description {
    display: none;
  }

  .tabs-content {
    padding: 1.5rem 1rem;
  }
}
</style>
