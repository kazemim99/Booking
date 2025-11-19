<template>
  <ResponsiveModal :is-open="isOpen" @close="handleClose" title="تنظیمات" size="md" mobile-height="auto">
    <div class="settings-content">
      <!-- Loading State -->
      <div v-if="loading" class="loading-state">
        <div class="spinner"></div>
        <p>در حال بارگذاری...</p>
      </div>

      <template v-else>
        <!-- Notifications Section -->
        <div class="settings-section">
          <h3 class="section-title">اعلان‌ها</h3>
          <p class="section-description">تنظیمات اعلان‌های خود را مدیریت کنید</p>

          <!-- SMS Notifications Toggle -->
          <div class="setting-item">
            <div class="setting-info">
              <label for="smsEnabled" class="setting-label">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="icon">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 18h.01M8 21h8a2 2 0 002-2V5a2 2 0 00-2-2H8a2 2 0 00-2 2v14a2 2 0 002 2z" />
                </svg>
                <span>اعلان‌های پیامکی (SMS)</span>
              </label>
              <p class="setting-description">دریافت یادآوری و اطلاعیه‌ها از طریق پیامک</p>
            </div>
            <label class="toggle">
              <input
                id="smsEnabled"
                v-model="form.smsEnabled"
                type="checkbox"
                class="toggle-input"
                @change="handleAutoSave"
              />
              <span class="toggle-slider"></span>
            </label>
          </div>

          <!-- Email Notifications Toggle -->
          <div class="setting-item">
            <div class="setting-info">
              <label for="emailEnabled" class="setting-label">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="icon">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
                <span>اعلان‌های ایمیل</span>
              </label>
              <p class="setting-description">دریافت یادآوری و اطلاعیه‌ها از طریق ایمیل</p>
            </div>
            <label class="toggle">
              <input
                id="emailEnabled"
                v-model="form.emailEnabled"
                type="checkbox"
                class="toggle-input"
                @change="handleAutoSave"
              />
              <span class="toggle-slider"></span>
            </label>
          </div>

          <!-- Reminder Timing Dropdown -->
          <div class="setting-item">
            <div class="setting-info">
              <label for="reminderTiming" class="setting-label">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="icon">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span>زمان ارسال یادآوری</span>
              </label>
              <p class="setting-description">زمان ارسال یادآوری قبل از نوبت</p>
            </div>
            <select
              id="reminderTiming"
              v-model="form.reminderTiming"
              class="select-input"
              @change="handleAutoSave"
            >
              <option value="1h">۱ ساعت قبل</option>
              <option value="24h">۱ روز قبل</option>
              <option value="3d">۳ روز قبل</option>
            </select>
          </div>

          <!-- Warning if all notifications disabled -->
          <div v-if="!form.smsEnabled && !form.emailEnabled" class="warning-message">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="warning-icon">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            <p>توجه: با غیرفعال کردن همه اعلان‌ها، یادآوری‌های نوبت دریافت نخواهید کرد.</p>
          </div>
        </div>

        <div class="divider"></div>

        <!-- Account Section -->
        <div class="settings-section">
          <h3 class="section-title">حساب کاربری</h3>

          <div class="info-box">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" class="info-icon">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <div>
              <p class="info-title">برای تغییرات حساب کاربری با پشتیبانی تماس بگیرید</p>
              <p class="info-description">
                برای تغییر شماره تلفن، حذف حساب کاربری، یا سایر درخواست‌ها، لطفاً با تیم پشتیبانی ما تماس بگیرید.
              </p>
              <a href="tel:+982188888888" class="contact-link">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
                </svg>
                <span>۰۲۱-۸۸۸۸۸۸۸۸</span>
              </a>
            </div>
          </div>
        </div>

        <!-- Success Message -->
        <transition name="fade">
          <div v-if="showSaveMessage" class="success-message">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path fill-rule="evenodd" d="M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12zm13.36-1.814a.75.75 0 10-1.22-.872l-3.236 4.53L9.53 12.22a.75.75 0 00-1.06 1.06l2.25 2.25a.75.75 0 001.14-.094l3.75-5.25z" clip-rule="evenodd" />
            </svg>
            <span>تنظیمات ذخیره شد</span>
          </div>
        </transition>
      </template>
    </div>
  </ResponsiveModal>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useCustomerStore } from '../../stores/customer.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useToast } from '@/core/composables/useToast'
import ResponsiveModal from '@/shared/components/ui/ResponsiveModal.vue'
import type { ReminderTiming } from '../../types/customer.types'

interface Props {
  isOpen: boolean
}

const props = defineProps<Props>()
const emit = defineEmits<{
  close: []
}>()

const customerStore = useCustomerStore()
const authStore = useAuthStore()
const { showError } = useToast()

const loading = computed(() => customerStore.loading.preferences)
const preferences = computed(() => customerStore.preferences)

const form = ref({
  smsEnabled: true,
  emailEnabled: true,
  reminderTiming: '24h' as ReminderTiming
})

const showSaveMessage = ref(false)

// Load preferences when modal opens
watch(() => props.isOpen, async (isOpen) => {
  if (isOpen && authStore.user?.id) {
    try {
      await customerStore.fetchPreferences(authStore.user.id)

      if (preferences.value) {
        form.value.smsEnabled = preferences.value.smsEnabled
        form.value.emailEnabled = preferences.value.emailEnabled
        form.value.reminderTiming = preferences.value.reminderTiming
      }
    } catch (error) {
      console.error('[SettingsModal] Error fetching preferences:', error)
      showError('خطا در بارگذاری تنظیمات')
    }
  }
}, { immediate: true })

let saveTimeout: ReturnType<typeof setTimeout> | null = null

async function handleAutoSave(): Promise<void> {
  if (!authStore.user?.id) return

  // Clear previous timeout
  if (saveTimeout) {
    clearTimeout(saveTimeout)
  }

  // Debounce save (wait 500ms after last change)
  saveTimeout = setTimeout(async () => {
    try {
      await customerStore.updatePreferences(authStore.user!.id, {
        smsEnabled: form.value.smsEnabled,
        emailEnabled: form.value.emailEnabled,
        reminderTiming: form.value.reminderTiming
      })

      // Show success message
      showSaveMessage.value = true
      setTimeout(() => {
        showSaveMessage.value = false
      }, 2000)
    } catch (error) {
      console.error('[SettingsModal] Error saving preferences:', error)
      showError('خطا در ذخیره تنظیمات')
    }
  }, 500)
}

function handleClose(): void {
  emit('close')
}
</script>

<style scoped lang="scss">
.settings-content {
  min-height: 300px;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem 1rem;
  color: #6b7280;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 3px solid #e5e7eb;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-bottom: 1rem;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.settings-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.section-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: #111827;
  margin: 0;
}

.section-description {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

.setting-item {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1rem;
  background: #f9fafb;
  border-radius: 8px;
}

.setting-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.setting-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 500;
  color: #374151;
  font-size: 0.875rem;
  cursor: pointer;
}

.icon {
  width: 18px;
  height: 18px;
  color: #9ca3af;
  flex-shrink: 0;
}

.setting-description {
  font-size: 0.75rem;
  color: #9ca3af;
  margin: 0;
}

.toggle {
  position: relative;
  display: inline-block;
  width: 48px;
  height: 24px;
  flex-shrink: 0;
}

.toggle-input {
  opacity: 0;
  width: 0;
  height: 0;

  &:checked + .toggle-slider {
    background-color: #667eea;

    &::before {
      transform: translateX(24px);
    }
  }

  &:focus + .toggle-slider {
    box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
  }
}

.toggle-slider {
  position: absolute;
  cursor: pointer;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: #d1d5db;
  transition: 0.2s;
  border-radius: 24px;

  &::before {
    position: absolute;
    content: '';
    height: 18px;
    width: 18px;
    left: 3px;
    bottom: 3px;
    background-color: white;
    transition: 0.2s;
    border-radius: 50%;
  }
}

.select-input {
  padding: 0.5rem 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  font-size: 0.875rem;
  background: white;
  cursor: pointer;
  transition: border-color 0.2s;

  &:focus {
    outline: none;
    border-color: #667eea;
    box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
  }
}

.warning-message {
  display: flex;
  gap: 0.75rem;
  padding: 0.75rem;
  background: #fffbeb;
  border: 1px solid #fef3c7;
  border-radius: 8px;
  font-size: 0.875rem;
  color: #92400e;
}

.warning-icon {
  width: 20px;
  height: 20px;
  flex-shrink: 0;
  color: #f59e0b;
}

.divider {
  height: 1px;
  background: #e5e7eb;
  margin: 0.5rem 0;
}

.info-box {
  display: flex;
  gap: 0.75rem;
  padding: 1rem;
  background: #f0f9ff;
  border: 1px solid #bfdbfe;
  border-radius: 8px;
}

.info-icon {
  width: 20px;
  height: 20px;
  flex-shrink: 0;
  color: #3b82f6;
}

.info-title {
  font-size: 0.875rem;
  font-weight: 600;
  color: #1e40af;
  margin: 0 0 0.25rem 0;
}

.info-description {
  font-size: 0.75rem;
  color: #1e40af;
  margin: 0 0 0.75rem 0;
  line-height: 1.5;
}

.contact-link {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  color: #3b82f6;
  text-decoration: none;
  font-weight: 500;
  font-size: 0.875rem;
  transition: color 0.2s;

  svg {
    width: 16px;
    height: 16px;
  }

  &:hover {
    color: #2563eb;
  }
}

.success-message {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem;
  background: #d1fae5;
  border: 1px solid #a7f3d0;
  border-radius: 8px;
  color: #065f46;
  font-size: 0.875rem;
  font-weight: 500;

  svg {
    width: 20px;
    height: 20px;
  }
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
