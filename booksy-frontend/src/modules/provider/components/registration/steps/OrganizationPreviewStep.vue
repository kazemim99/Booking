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
              <div v-for="(service, index) in data.services" :key="index" class="preview-list-item">
                <i class="icon-check-circle"></i>
                <span>{{ service.name || `خدمت ${index + 1}` }}</span>
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
              <div v-for="(hour, index) in data.businessHours" :key="index" class="preview-list-item">
                <i class="icon-check-circle"></i>
                <span>{{ hour.day || `روز ${index + 1}` }}</span>
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
            <div v-if="data.gallery && data.gallery.length > 0" class="gallery-preview">
              <div v-for="(image, index) in data.gallery" :key="index" class="gallery-thumbnail">
                <img :src="image" alt="Gallery image" />
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
import { ref } from 'vue'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'

// ============================================
// Props & Emits
// ============================================

interface Props {
  data: any
}

defineProps<Props>()

const emit = defineEmits<{
  (e: 'next'): void
  (e: 'back'): void
  (e: 'edit', step: number): void
}>()

// ============================================
// State
// ============================================

const confirmed = ref(false)

// ============================================
// Methods
// ============================================

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

.confirmation-section {
  margin-top: 2rem;
  padding: 1.5rem;
  background: linear-gradient(135deg, #f8f5ff 0%, #fff 100%);
  border: 2px solid #7c3aed;
  border-radius: 12px;
}
</style>
