<template>
  <ResponsiveModal :is-open="isOpen" @close="handleClose" title="تنظیمات" size="md" mobile-height="auto">
    <div class="settings-content">
      <template v-if="isOpen">
        <!-- Notifications Section -->
        <div class="settings-section">
          <h3 class="section-title">اعلان‌ها</h3>
          <!-- Only what this product can honour. The SMS, email and reminder-timing controls that used to be
               here saved to fields nothing reads — a customer could switch SMS off, be told it was saved, and
               keep receiving SMS. -->
          <NotificationPreferencesPanel audience="customer" />
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

      </template>
    </div>
  </ResponsiveModal>
</template>

<script setup lang="ts">
import ResponsiveModal from '@/shared/components/ui/ResponsiveModal.vue'
import NotificationPreferencesPanel from '@/modules/notifications/components/NotificationPreferencesPanel.vue'

interface Props {
  isOpen: boolean
}

defineProps<Props>()
const emit = defineEmits<{
  close: []
}>()

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
  color: var(--color-gray-600);
}

.spinner {
  width: 40px;
  height: 40px;
  border: 3px solid var(--color-gray-300);
  border-top-color: var(--color-primary-500);
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
  color: var(--color-gray-900);
  margin: 0;
}

.section-description {
  font-size: 0.875rem;
  color: var(--color-gray-600);
  margin: 0;
}

.setting-item {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1rem;
  background: var(--color-gray-50);
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
  color: var(--color-gray-800);
  font-size: 0.875rem;
  cursor: pointer;
}

.icon {
  width: 18px;
  height: 18px;
  color: var(--color-gray-500);
  flex-shrink: 0;
}

.setting-description {
  font-size: 0.75rem;
  color: var(--color-gray-500);
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
    background-color: var(--color-primary-500);

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
  background-color: var(--color-gray-400);
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
  border: 1px solid var(--color-gray-400);
  border-radius: 6px;
  font-size: 0.875rem;
  background: white;
  cursor: pointer;
  transition: border-color 0.2s;

  &:focus {
    outline: none;
    border-color: var(--color-primary-500);
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
  color: var(--color-warning-500);
}

.divider {
  height: 1px;
  background: var(--color-gray-300);
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
  color: var(--color-primary-500);
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
  color: var(--color-primary-500);
  text-decoration: none;
  font-weight: 500;
  font-size: 0.875rem;
  transition: color 0.2s;

  svg {
    width: 16px;
    height: 16px;
  }

  &:hover {
    color: var(--color-primary-600);
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
