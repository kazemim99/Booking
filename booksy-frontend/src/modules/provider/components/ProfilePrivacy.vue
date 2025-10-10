<template>
  <div class="privacy-tab">
    <div class="tab-header">
      <div>
        <h2 class="tab-title">Privacy Settings</h2>
        <p class="tab-description">Control who can see your information and how it's used</p>
      </div>
    </div>

    <form @submit.prevent="handleSubmit" class="profile-form">
      <!-- Profile Visibility Section -->
      <div class="form-section">
        <h3 class="section-title">Profile Visibility</h3>

        <div class="visibility-options">
          <label
            v-for="option in visibilityOptions"
            :key="option.value"
            class="visibility-option"
            :class="{ selected: form.profileVisibility === option.value }"
          >
            <input
              type="radio"
              name="profileVisibility"
              :value="option.value"
              v-model="form.profileVisibility"
              class="visibility-radio"
            />
            <div class="option-content">
              <div class="option-header">
                <span class="option-icon" v-html="option.icon"></span>
                <span class="option-label">{{ option.label }}</span>
              </div>
              <p class="option-description">{{ option.description }}</p>
            </div>
            <div class="option-check">
              <svg
                v-if="form.profileVisibility === option.value"
                width="20"
                height="20"
                viewBox="0 0 24 24"
                fill="currentColor"
              >
                <path d="M9 16.2L4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z" />
              </svg>
            </div>
          </label>
        </div>
      </div>

      <!-- Information Visibility Section -->
      <div class="form-section">
        <h3 class="section-title">Information Visibility</h3>
        <p class="section-description">Choose what information is visible on your public profile</p>

        <div class="toggle-list">
          <div class="toggle-item">
            <div class="toggle-info">
              <div class="toggle-icon">📧</div>
              <div>
                <span class="toggle-label">Show email address</span>
                <span class="toggle-description">Let others see your email address</span>
              </div>
            </div>
            <label class="toggle-switch">
              <input type="checkbox" v-model="form.showEmail" />
              <span class="toggle-slider"></span>
            </label>
          </div>

          <div class="toggle-item">
            <div class="toggle-info">
              <div class="toggle-icon">📞</div>
              <div>
                <span class="toggle-label">Show phone number</span>
                <span class="toggle-description">Let others see your phone number</span>
              </div>
            </div>
            <label class="toggle-switch">
              <input type="checkbox" v-model="form.showPhone" />
              <span class="toggle-slider"></span>
            </label>
          </div>

          <div class="toggle-item">
            <div class="toggle-info">
              <div class="toggle-icon">📍</div>
              <div>
                <span class="toggle-label">Show address</span>
                <span class="toggle-description">Display your full address on your profile</span>
              </div>
            </div>
            <label class="toggle-switch">
              <input type="checkbox" v-model="form.showAddress" />
              <span class="toggle-slider"></span>
            </label>
          </div>

          <div class="toggle-item">
            <div class="toggle-info">
              <div class="toggle-icon">🎂</div>
              <div>
                <span class="toggle-label">Show date of birth</span>
                <span class="toggle-description">Let others see your birthdate</span>
              </div>
            </div>
            <label class="toggle-switch">
              <input type="checkbox" v-model="form.showBirthdate" />
              <span class="toggle-slider"></span>
            </label>
          </div>
        </div>
      </div>

      <!-- Search Engine Indexing -->
      <div class="form-section">
        <h3 class="section-title">Search Engine Visibility</h3>

        <div class="toggle-list">
          <div class="toggle-item">
            <div class="toggle-info">
              <div class="toggle-icon">🔍</div>
              <div>
                <span class="toggle-label">Allow search engine indexing</span>
                <span class="toggle-description"
                  >Let search engines like Google find your public profile</span
                >
              </div>
            </div>
            <label class="toggle-switch">
              <input type="checkbox" v-model="form.allowSearchEngineIndexing" />
              <span class="toggle-slider"></span>
            </label>
          </div>
        </div>
      </div>

      <!-- Privacy Tips -->
      <div class="privacy-tips">
        <div class="tip-header">
          <svg
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
          >
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="16" x2="12" y2="12"></line>
            <line x1="12" y1="8" x2="12.01" y2="8"></line>
          </svg>
          <span>Privacy Tips</span>
        </div>
        <ul class="tip-list">
          <li>Setting your profile to "Private" means only you can see your full information</li>
          <li>
            Even with a public profile, sensitive information like payment details are never shown
          </li>
          <li>Providers you've booked with can always see your contact information</li>
          <li>You can change these settings at any time</li>
        </ul>
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
import { ref, computed, watch, onMounted } from 'vue'
import type {
  PrivacySettings,
  UpdatePrivacySettingsRequest,
} from '@/modules/user-management/types/user-profile.types'

// Props
const props = defineProps<{
  privacySettings: PrivacySettings
  isSaving: boolean
}>()

// Emits
const emit = defineEmits<{
  update: [data: UpdatePrivacySettingsRequest]
}>()

// Form state
const form = ref<PrivacySettings>({
  profileVisibility: 'public',
  showEmail: false,
  showPhone: false,
  showAddress: false,
  showBirthdate: false,
  allowSearchEngineIndexing: true,
})

const initialForm = ref<string>('')

// Visibility options
const visibilityOptions = [
  {
    value: 'public',
    label: 'Public',
    icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><line x1="2" y1="12" x2="22" y2="12"></line><path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"></path></svg>',
    description: 'Anyone can view your profile and public information',
  },
  {
    value: 'contacts',
    label: 'Contacts Only',
    icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle><path d="M23 21v-2a4 4 0 0 0-3-3.87"></path><path d="M16 3.13a4 4 0 0 1 0 7.75"></path></svg>',
    description: "Only providers you've booked with can see your profile",
  },
  {
    value: 'private',
    label: 'Private',
    icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect><path d="M7 11V7a5 5 0 0 1 10 0v4"></path></svg>',
    description: 'Only you can see your profile information',
  },
]

// Computed
const hasChanges = computed(() => {
  return JSON.stringify(form.value) !== initialForm.value
})

// Methods
function initializeForm() {
  form.value = {
    profileVisibility: props.privacySettings.profileVisibility || 'public',
    showEmail: props.privacySettings.showEmail ?? false,
    showPhone: props.privacySettings.showPhone ?? false,
    showAddress: props.privacySettings.showAddress ?? false,
    showBirthdate: props.privacySettings.showBirthdate ?? false,
    allowSearchEngineIndexing: props.privacySettings.allowSearchEngineIndexing ?? true,
  }

  initialForm.value = JSON.stringify(form.value)
}

function handleSubmit() {
  const updateData: UpdatePrivacySettingsRequest = {
    profileVisibility: form.value.profileVisibility,
    showEmail: form.value.showEmail,
    showPhone: form.value.showPhone,
    showAddress: form.value.showAddress,
    showBirthdate: form.value.showBirthdate,
    allowSearchEngineIndexing: form.value.allowSearchEngineIndexing,
  }

  emit('update', updateData)
}

function handleReset() {
  initializeForm()
}

// Watchers
watch(
  () => props.privacySettings,
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
.privacy-tab {
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

.section-description {
  color: var(--color-text-secondary);
  margin: 0 0 1.5rem;
  font-size: 0.9375rem;
}

// Visibility Options
.visibility-options {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.visibility-option {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1.5rem;
  border: 2px solid var(--color-border);
  border-radius: 0.75rem;
  cursor: pointer;
  transition: all 0.2s;
  background: white;

  &:hover {
    border-color: var(--color-primary-light);
    box-shadow: 0 2px 8px rgba(99, 102, 241, 0.1);
  }

  &.selected {
    border-color: var(--color-primary);
    background: rgba(99, 102, 241, 0.02);
    box-shadow: 0 2px 12px rgba(99, 102, 241, 0.15);
  }
}

.visibility-radio {
  appearance: none;
  width: 0;
  height: 0;
  position: absolute;
}

.option-content {
  flex: 1;
}

.option-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.5rem;
}

.option-icon {
  display: flex;
  align-items: center;
  color: var(--color-primary);
}

.option-label {
  font-weight: 600;
  color: var(--color-text-primary);
  font-size: 1.0625rem;
}

.option-description {
  color: var(--color-text-secondary);
  margin: 0;
  font-size: 0.875rem;
  padding-left: 2rem;
}

.option-check {
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--color-primary);
}

// Toggle List
.toggle-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.toggle-item {
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

.toggle-info {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex: 1;
}

.toggle-icon {
  font-size: 1.5rem;
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: white;
  border-radius: 0.5rem;
  border: 1px solid var(--color-border);
}

.toggle-label {
  display: block;
  font-weight: 500;
  color: var(--color-text-primary);
  margin-bottom: 0.25rem;
}

.toggle-description {
  display: block;
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

// Privacy Tips
.privacy-tips {
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.05) 0%, rgba(139, 92, 246, 0.05) 100%);
  border: 1px solid rgba(99, 102, 241, 0.2);
  border-radius: 0.75rem;
  padding: 1.5rem;
  margin-bottom: 2rem;
}

.tip-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 600;
  color: var(--color-primary);
  margin-bottom: 1rem;

  svg {
    color: var(--color-primary);
  }
}

.tip-list {
  margin: 0;
  padding-left: 1.5rem;
  color: var(--color-text-secondary);

  li {
    margin-bottom: 0.5rem;
    line-height: 1.6;

    &:last-child {
      margin-bottom: 0;
    }
  }
}

// Form Actions
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
