<template>
  <div class="hours-store-test">
    <h2>Hours Store Integration Test</h2>

    <!-- Initialization Error -->
    <div v-if="initError" class="init-error">
      <strong>⚠️ Initialization Error:</strong>
      <p>{{ initError }}</p>
      <p>Make sure you are logged in as a provider and have completed your profile.</p>
    </div>

    <!-- Test Status -->
    <div class="test-status">
      <p><strong>Debug Info:</strong></p>
      <ul>
        <li>Auth Token: {{ localStorage.getItem('auth_token') ? 'Present ✓' : 'Missing ✗' }}</li>
        <li>Provider ID (localStorage): {{ localStorage.getItem('provider_id') || 'Not set' }}</li>
        <li>User ID: {{ authStore.user?.id || 'Not loaded' }}</li>
        <li>User Role: {{ authStore.user?.role || 'Not loaded' }}</li>
      </ul>

      <p><strong>Provider Info:</strong></p>
      <ul>
        <li>Provider ID: {{ providerStore.currentProvider?.id || 'Not loaded' }}</li>
        <li>Business Name: {{ providerStore.currentProvider?.profile?.businessName || 'N/A' }}</li>
        <li>Provider Store Loading: {{ providerStore.isLoading }}</li>
        <li>Provider Store Error: {{ providerStore.error || 'None' }}</li>
      </ul>

      <p><strong>Hours Store State:</strong></p>
      <ul>
        <li>Loading: {{ hoursStore.state.isLoading }}</li>
        <li>Error: {{ hoursStore.state.error || 'None' }}</li>
        <li>Base Hours: {{ hoursStore.state.baseHours.length }} days configured</li>
        <li>Holidays: {{ hoursStore.state.holidays.length }} holidays</li>
        <li>Exceptions: {{ hoursStore.state.exceptions.length }} exceptions</li>
      </ul>
    </div>

    <!-- Test Actions -->
    <div class="test-actions">
      <button @click="testLoadSchedule" :disabled="loading">
        Test Load Schedule
      </button>
      <button @click="testAddHoliday" :disabled="loading">
        Test Add Holiday
      </button>
      <button @click="testGetAvailability" :disabled="loading">
        Test Get Availability
      </button>
    </div>

    <!-- Test Results -->
    <div v-if="testResult" class="test-result">
      <h3>Test Result:</h3>
      <pre>{{ testResult }}</pre>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useHoursStore } from '../../stores/hours.store'
import { useProviderStore } from '../../stores/provider.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'

const hoursStore = useHoursStore()
const providerStore = useProviderStore()
const authStore = useAuthStore()
const loading = ref(false)
const testResult = ref<string | null>(null)
const initError = ref<string | null>(null)

// Make localStorage available in template
const localStorage = window.localStorage

// Load provider on mount
onMounted(async () => {
  try {
    if (!providerStore.currentProvider) {
      loading.value = true
      await providerStore.loadCurrentProvider()
      loading.value = false
    }
  } catch (error) {
    initError.value = `Failed to load provider: ${error instanceof Error ? error.message : 'Unknown error'}`
    loading.value = false
  }
})

async function testLoadSchedule() {
  loading.value = true
  testResult.value = null

  try {
    // Ensure provider is loaded
    if (!providerStore.currentProvider) {
      await providerStore.loadCurrentProvider()
    }

    const providerId = providerStore.currentProvider?.id
    if (!providerId) {
      throw new Error('No provider ID available. Please ensure you are logged in as a provider.')
    }

    await hoursStore.loadSchedule(providerId)
    testResult.value = JSON.stringify(
      {
        success: true,
        providerId,
        baseHours: hoursStore.state.baseHours,
        holidays: hoursStore.state.holidays,
        exceptions: hoursStore.state.exceptions,
      },
      null,
      2
    )
  } catch (error) {
    testResult.value = `Error: ${error instanceof Error ? error.message : 'Unknown error'}`
  } finally {
    loading.value = false
  }
}

async function testAddHoliday() {
  loading.value = true
  testResult.value = null

  try {
    // Ensure provider is loaded
    if (!providerStore.currentProvider) {
      await providerStore.loadCurrentProvider()
    }

    const providerId = providerStore.currentProvider?.id
    if (!providerId) {
      throw new Error('No provider ID available. Please ensure you are logged in as a provider.')
    }

    const tomorrow = new Date()
    tomorrow.setDate(tomorrow.getDate() + 1)
    const dateStr = tomorrow.toISOString().split('T')[0]

    const holiday = await hoursStore.addHoliday({
      providerId,
      holiday: {
        date: dateStr,
        reason: 'Test Holiday',
        isRecurring: false,
      },
    })

    testResult.value = JSON.stringify(
      { success: true, holiday },
      null,
      2
    )
  } catch (error) {
    testResult.value = `Error: ${error instanceof Error ? error.message : 'Unknown error'}`
  } finally {
    loading.value = false
  }
}

async function testGetAvailability() {
  loading.value = true
  testResult.value = null

  try {
    // Ensure schedule is loaded first
    if (!providerStore.currentProvider) {
      await providerStore.loadCurrentProvider()
    }

    const providerId = providerStore.currentProvider?.id
    if (!providerId) {
      throw new Error('No provider ID available. Please ensure you are logged in as a provider.')
    }

    // Load schedule if not already loaded
    if (hoursStore.state.baseHours.length === 0) {
      await hoursStore.loadSchedule(providerId)
    }

    const today = new Date().toISOString().split('T')[0]
    const availability = hoursStore.getAvailabilityForDate(today)

    testResult.value = JSON.stringify(
      { success: true, date: today, availability },
      null,
      2
    )
  } catch (error) {
    testResult.value = `Error: ${error instanceof Error ? error.message : 'Unknown error'}`
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.hours-store-test {
  padding: 2rem;
  max-width: 800px;
  margin: 0 auto;
}

.init-error {
  background: #fee;
  border: 2px solid #f44;
  padding: 1rem;
  border-radius: 4px;
  margin-bottom: 1rem;
  color: #c00;
}

.init-error p {
  margin: 0.5rem 0;
}

.test-status {
  background: #f5f5f5;
  padding: 1rem;
  border-radius: 4px;
  margin-bottom: 1rem;
}

.test-status ul {
  list-style: none;
  padding: 0;
  margin: 0.5rem 0 0 0;
}

.test-status li {
  padding: 0.25rem 0;
}

.test-actions {
  display: flex;
  gap: 1rem;
  margin-bottom: 1rem;
}

.test-actions button {
  padding: 0.5rem 1rem;
  background: #4CAF50;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.test-actions button:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.test-actions button:hover:not(:disabled) {
  background: #45a049;
}

.test-result {
  background: #f5f5f5;
  padding: 1rem;
  border-radius: 4px;
  border-left: 4px solid #4CAF50;
}

.test-result pre {
  margin: 0;
  white-space: pre-wrap;
  word-wrap: break-word;
}
</style>
