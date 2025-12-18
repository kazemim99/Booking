<template>
  <div class="organization-preview-step">
    <div class="step-container">
      <div class="step-header">
        <h2 class="step-title">بررسی نهایی</h2>
        <p class="step-description">
          لطفاً اطلاعات وارد شده را بررسی کنید. در صورت نیاز می‌توانید به مراحل قبل بازگردید و تغییرات لازم را اعمال کنید.
        </p>
      </div>

      <div class="step-content">
        <!-- Business Information Summary -->
        <div class="preview-section">
          <div class="preview-section-header">
            <h3 class="preview-section-title">
              <i class="icon-building"></i>
              اطلاعات کسب‌و‌کار
            </h3>
            <AppButton variant="text" size="small" @click="$emit('edit', 1)">
              <i class="icon-edit"></i>
              ویرایش
            </AppButton>
          </div>
          <div class="preview-section-content">
            <!-- Logo Display -->
            <div v-if="logoUrl" class="logo-preview-container">
              <div class="logo-preview">
                <img :src="logoUrl" alt="لوگوی کسب‌وکار" />
              </div>
            </div>

            <div class="preview-item">
              <span class="preview-label">نام کسب‌و‌کار:</span>
              <span class="preview-value">{{ data.businessInfo.businessName }}</span>
            </div>
            <div class="preview-item">
              <span class="preview-label">نام مالک:</span>
              <span class="preview-value">
                {{ data.businessInfo.ownerFirstName }} {{ data.businessInfo.ownerLastName }}
              </span>
            </div>
            <div class="preview-item">
              <span class="preview-label">ایمیل:</span>
              <span class="preview-value">{{ data.businessInfo.email }}</span>
            </div>
            <div class="preview-item">
              <span class="preview-label">شماره تماس:</span>
              <span class="preview-value">{{ data.businessInfo.phone }}</span>
            </div>
            <div v-if="data.businessInfo.description" class="preview-item">
              <span class="preview-label">توضیحات:</span>
              <span class="preview-value">{{ data.businessInfo.description }}</span>
            </div>
          </div>
        </div>

        <!-- Location Summary -->
        <div class="preview-section">
          <div class="preview-section-header">
            <h3 class="preview-section-title">
              <i class="icon-map-pin"></i>
              موقعیت مکانی
            </h3>
            <AppButton variant="text" size="small" @click="$emit('edit', 3)">
              <i class="icon-edit"></i>
              ویرایش
            </AppButton>
          </div>
          <div class="preview-section-content">
            <div class="preview-item">
              <span class="preview-label">آدرس:</span>
              <span class="preview-value">{{ data.address.addressLine1 }}</span>
            </div>
            <div v-if="data.address.addressLine2" class="preview-item">
              <span class="preview-label">آدرس (خط دوم):</span>
              <span class="preview-value">{{ data.address.addressLine2 }}</span>
            </div>
            <div class="preview-item">
              <span class="preview-label">شهر:</span>
              <span class="preview-value">{{ data.address.city }}, {{ data.address.state }}</span>
            </div>
            <div v-if="data.address.postalCode" class="preview-item">
              <span class="preview-label">کد پستی:</span>
              <span class="preview-value">{{ data.address.postalCode }}</span>
            </div>
          </div>
        </div>

        <!-- Services Summary -->
        <div class="preview-section">
          <div class="preview-section-header">
            <h3 class="preview-section-title">
              <i class="icon-briefcase"></i>
              خدمات
            </h3>
            <AppButton variant="text" size="small" @click="$emit('edit', 4)">
              <i class="icon-edit"></i>
              ویرایش
            </AppButton>
          </div>
          <div class="preview-section-content">
            <div v-if="data.services.length > 0" class="preview-list">
              <div v-for="(service, index) in data.services" :key="index" class="preview-list-item-detailed">
                <div class="service-header">
                  <i class="icon-check-circle"></i>
                  <span class="service-name">{{ service.name || `خدمت ${index + 1}` }}</span>
                </div>
                <div class="service-details">
                  <span class="service-detail">
                    <i class="icon-clock-small"></i>
                    {{ formatDuration(service.durationHours, service.durationMinutes) }}
                  </span>
                  <span class="service-detail">
                    <i class="icon-currency"></i>
                    {{ formatPrice(service.price) }} تومان
                  </span>
                </div>
              </div>
            </div>
            <div v-else class="preview-empty">
              <i class="icon-alert-circle"></i>
              <span>هیچ خدمتی اضافه نشده است</span>
            </div>
          </div>
        </div>

        <!-- Working Hours Summary -->
        <div class="preview-section">
          <div class="preview-section-header">
            <h3 class="preview-section-title">
              <i class="icon-clock"></i>
              ساعات کاری
            </h3>
            <AppButton variant="text" size="small" @click="$emit('edit', 5)">
              <i class="icon-edit"></i>
              ویرایش
            </AppButton>
          </div>
          <div class="preview-section-content">
            <div v-if="data.businessHours.length > 0" class="preview-list">
              <div v-for="(hour, index) in data.businessHours.filter(h => h.isOpen)" :key="index" class="preview-list-item-detailed">
                <div class="hours-header">
                  <i class="icon-check-circle"></i>
                  <span class="day-name">{{ getDayName(hour.dayOfWeek) }}</span>
                </div>
                <div class="hours-details">
                  <span class="hours-time">
                    از {{ formatTime(hour.openTime) }} تا {{ formatTime(hour.closeTime) }}
                  </span>
                  <div v-if="hour.breaks && hour.breaks.length > 0" class="breaks-info">
                    <span class="breaks-label">استراحت:</span>
                    <span v-for="(breakItem, bIndex) in hour.breaks" :key="bIndex" class="break-time">
                      {{ formatTime(breakItem.start) }} - {{ formatTime(breakItem.end) }}
                      <span v-if="bIndex < hour.breaks.length - 1">،</span>
                    </span>
                  </div>
                </div>
              </div>
            </div>
            <div v-else class="preview-empty">
              <i class="icon-alert-circle"></i>
              <span>ساعات کاری تنظیم نشده است</span>
            </div>
          </div>
        </div>

        <!-- Gallery Summary -->
        <div class="preview-section">
          <div class="preview-section-header">
            <h3 class="preview-section-title">
              <i class="icon-image"></i>
              گالری تصاویر
            </h3>
            <AppButton variant="text" size="small" @click="$emit('edit', 6)">
              <i class="icon-edit"></i>
              ویرایش
            </AppButton>
          </div>
          <div class="preview-section-content">
            <div v-if="galleryImages.length > 0" class="gallery-preview">
              <div v-for="(image, index) in galleryImages" :key="index" class="gallery-thumbnail">
                <img :src="image.url" :alt="image.altText || 'تصویر گالری'" />
              </div>
            </div>
            <div v-else class="preview-empty">
              <i class="icon-alert-circle"></i>
              <span>هیچ تصویری اضافه نشده است</span>
            </div>
          </div>
        </div>

        <!-- Confirmation -->
        <div class="confirmation-section">
          <div class="checkbox-item" :class="{ selected: confirmed }">
            <input id="confirm" v-model="confirmed" type="checkbox" />
            <label for="confirm">
              <strong>تایید اطلاعات</strong>
              <span class="checkbox-description">
                تأیید می‌کنم که اطلاعات وارد شده صحیح است و می‌خواهم ثبت‌نام را تکمیل کنم.
              </span>
            </label>
          </div>
        </div>
      </div>

      <!-- Actions -->
      <div class="step-actions">
        <AppButton variant="secondary" size="large" @click="$emit('back')">
          <i class="icon-arrow-right"></i>
          بازگشت
        </AppButton>

        <AppButton variant="primary" size="large" :disabled="!confirmed" @click="handleSubmit">
          تکمیل ثبت‌نام
          <i class="icon-check"></i>
        </AppButton>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useProviderRegistration } from '@/modules/provider/composables/useProviderRegistration'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import { microservices } from '@/core/api/config/api-config'

// ============================================
// Props & Emits
// ============================================

interface Props {
  data: any
}

const props = defineProps<Props>()

const emit = defineEmits<{
  (e: 'next'): void
  (e: 'back'): void
  (e: 'edit', step: number): void
}>()

// ============================================
// Composables
// ============================================

const registration = useProviderRegistration()

// ============================================
// State
// ============================================

const confirmed = ref(false)

// ============================================
// Computed
// ============================================

const galleryImages = computed(() => {
  return registration.registrationData.value.galleryImages || []
})

// Get full logo URL with API base URL
const logoUrl = computed(() => {
  // Try to get logoUrl from the data prop or registration store
  const relativeUrl = (props.data.businessInfo as any)?.logoUrl ||
                      registration.registrationData.value.businessInfo?.logoUrl
  if (!relativeUrl) return null

  // If it's already a full URL (starts with http:// or https://), return as is
  if (relativeUrl.startsWith('http://') || relativeUrl.startsWith('https://')) {
    return relativeUrl
  }

  // Otherwise, prepend the ServiceCatalog API base URL
  const baseUrl = microservices.serviceCategory.baseURL.replace('/api', '')
  return `${baseUrl}${relativeUrl}`
})

// ============================================
// Methods
// ============================================

// Format duration (hours and minutes)
function formatDuration(hours?: number, minutes?: number): string {
  const h = hours || 0
  const m = minutes || 0

  if (h > 0 && m > 0) {
    return `${h} ساعت و ${m} دقیقه`
  } else if (h > 0) {
    return `${h} ساعت`
  } else if (m > 0) {
    return `${m} دقیقه`
  }
  return '-'
}

// Format price with thousand separators
function formatPrice(price?: number): string {
  if (!price) return '0'
  return price.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',')
}

// Format time from object {hours, minutes}
function formatTime(time?: { hours: number; minutes: number }): string {
  if (!time) return '00:00'
  const h = String(time.hours).padStart(2, '0')
  const m = String(time.minutes).padStart(2, '0')
  return `${h}:${m}`
}

// Get Persian day name
function getDayName(dayOfWeek: number): string {
  const days = ['شنبه', 'یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنج‌شنبه', 'جمعه']
  return days[dayOfWeek] || `روز ${dayOfWeek + 1}`
}

function handleSubmit() {
  if (confirmed.value) {
    emit('next')
  }
}
</script>

<style scoped lang="scss">
@import './steps-common.scss';

.preview-section {
  background: #fff;
  border: 2px solid #e5e7eb;
  border-radius: 12px;
  padding: 1.5rem;
  margin-bottom: 1.5rem;
  transition: all 0.2s ease;

  &:hover {
    border-color: #d1d5db;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  }
}

.preview-section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid #e5e7eb;
}

.preview-section-title {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-size: 1.125rem;
  font-weight: 600;
  color: #111827;
  margin: 0;

  i {
    color: #7c3aed;
    font-size: 1.25rem;
  }
}

.preview-section-content {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.preview-item {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
  padding: 0.75rem;
  background: #f9fafb;
  border-radius: 8px;

  @media (max-width: 640px) {
    flex-direction: column;
  }
}

.preview-label {
  font-weight: 600;
  color: #6b7280;
  font-size: 0.875rem;
  min-width: 150px;
}

.preview-value {
  color: #111827;
  font-size: 0.9375rem;
  text-align: right;
  word-break: break-word;
}

.preview-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.preview-list-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem;
  background: #f9fafb;
  border-radius: 8px;
  font-size: 0.9375rem;
  color: #111827;

  i {
    color: #10b981;
    font-size: 1.125rem;
    flex-shrink: 0;
  }
}

.preview-empty {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem;
  background: #fef3c7;
  border-radius: 8px;
  color: #92400e;
  font-size: 0.9375rem;

  i {
    color: #f59e0b;
    font-size: 1.25rem;
    flex-shrink: 0;
  }
}

.gallery-preview {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
  gap: 1rem;
}

.gallery-thumbnail {
  aspect-ratio: 1;
  border-radius: 8px;
  overflow: hidden;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }
}

.logo-preview-container {
  display: flex;
  justify-content: center;
  margin-bottom: 1.5rem;
  padding: 1rem;
  background: #f9fafb;
  border-radius: 8px;
}

.logo-preview {
  width: 120px;
  height: 120px;
  border-radius: 8px;
  overflow: hidden;
  border: 2px solid #e5e7eb;
  background: #fff;
  display: flex;
  align-items: center;
  justify-content: center;

  img {
    width: 100%;
    height: 100%;
    object-fit: contain;
  }
}

.confirmation-section {
  margin-top: 2rem;
  padding: 1.5rem;
  background: linear-gradient(135deg, #f8f5ff 0%, #fff 100%);
  border: 2px solid #7c3aed;
  border-radius: 12px;
}

// Detailed list items for services and working hours
.preview-list-item-detailed {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding: 1rem;
  background: #f9fafb;
  border-radius: 8px;
  border: 1px solid #e5e7eb;
  transition: all 0.2s ease;

  &:hover {
    border-color: #d1d5db;
    box-shadow: 0 1px 4px rgba(0, 0, 0, 0.05);
  }
}

// Service section styling
.service-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-weight: 600;
  color: #111827;
  font-size: 1rem;

  i {
    color: #10b981;
    font-size: 1.125rem;
    flex-shrink: 0;
  }
}

.service-name {
  flex: 1;
}

.service-details {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  padding-right: 2rem;
  flex-wrap: wrap;
}

.service-detail {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #6b7280;

  i {
    color: #7c3aed;
    font-size: 0.875rem;
    flex-shrink: 0;
  }
}

// Working hours section styling
.hours-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-weight: 600;
  color: #111827;
  font-size: 1rem;

  i {
    color: #10b981;
    font-size: 1.125rem;
    flex-shrink: 0;
  }
}

.day-name {
  flex: 1;
}

.hours-details {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding-right: 2rem;
}

.hours-time {
  font-size: 0.875rem;
  color: #6b7280;
}

.breaks-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #6b7280;
  flex-wrap: wrap;
}

.breaks-label {
  font-weight: 600;
  color: #7c3aed;
}

.break-time {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
}

// Responsive adjustments
@media (max-width: 640px) {
  .service-details,
  .hours-details {
    padding-right: 1.5rem;
  }

  .service-details {
    gap: 1rem;
  }
}
</style>
