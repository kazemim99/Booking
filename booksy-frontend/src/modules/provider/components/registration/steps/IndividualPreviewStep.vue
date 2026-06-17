<template>
  <div class="individual-preview-step">
    <div class="step-container">
      <div class="step-header">
        <h2 class="step-title">بررسی نهایی</h2>
        <p class="step-description">
          لطفاً اطلاعات وارد شده را بررسی کنید. در صورت نیاز می‌توانید به مراحل قبل بازگردید و تغییرات لازم را اعمال کنید.
        </p>
      </div>

      <div class="step-content">
        <!-- Personal Information Summary -->
        <div class="preview-section">
          <div class="preview-section-header">
            <h3 class="preview-section-title">
              <i class="icon-user"></i>
              اطلاعات شخصی
            </h3>
            <AppButton variant="link" size="small" @click="$emit('edit', 1)">
              <i class="icon-edit"></i>
              ویرایش
            </AppButton>
          </div>
          <div class="preview-section-content">
            <div class="preview-item">
              <span class="preview-label">نام و نام خانوادگی:</span>
              <span class="preview-value">
                {{ data.personalInfo.firstName }} {{ data.personalInfo.lastName }}
              </span>
            </div>
            <div class="preview-item">
              <span class="preview-label">ایمیل:</span>
              <span class="preview-value">{{ data.personalInfo.email }}</span>
            </div>
            <div class="preview-item">
              <span class="preview-label">شماره تماس:</span>
              <span class="preview-value">{{ data.personalInfo.phone }}</span>
            </div>
            <div v-if="data.personalInfo.bio" class="preview-item">
              <span class="preview-label">بیوگرافی:</span>
              <span class="preview-value">{{ data.personalInfo.bio }}</span>
            </div>
            <div v-if="data.personalInfo.specializations.length > 0" class="preview-item">
              <span class="preview-label">تخصص‌ها:</span>
              <div class="preview-tags">
                <span
                  v-for="(spec, index) in data.personalInfo.specializations"
                  :key="index"
                  class="preview-tag"
                >
                  {{ spec }}
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Service Area Summary -->
        <div class="preview-section">
          <div class="preview-section-header">
            <h3 class="preview-section-title">
              <i class="icon-map-pin"></i>
              منطقه خدمات
            </h3>
            <AppButton variant="link" size="small" @click="$emit('edit', 3)">
              <i class="icon-edit"></i>
              ویرایش
            </AppButton>
          </div>
          <div class="preview-section-content">
            <div class="preview-item">
              <span class="preview-label">محل فعالیت:</span>
              <span class="preview-value">{{ data.serviceArea.city }}, {{ data.serviceArea.state }}</span>
            </div>
            <div class="preview-item">
              <span class="preview-label">نوع خدمات:</span>
              <span class="preview-value">
                {{ data.offersMobileServices ? 'خدمات سیار' : 'محل ثابت' }}
              </span>
            </div>
            <div v-if="data.offersMobileServices" class="preview-item">
              <span class="preview-label">شعاع خدمات:</span>
              <span class="preview-value">{{ data.serviceArea.serviceRadius }} کیلومتر</span>
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
            <AppButton variant="link" size="small" @click="$emit('edit', 4)">
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
            <AppButton variant="link" size="small" @click="$emit('edit', 5)">
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

        <!-- Portfolio Summary -->
        <div class="preview-section">
          <div class="preview-section-header">
            <h3 class="preview-section-title">
              <i class="icon-image"></i>
              نمونه کارها
            </h3>
            <AppButton variant="link" size="small" @click="$emit('edit', 6)">
              <i class="icon-edit"></i>
              ویرایش
            </AppButton>
          </div>
          <div class="preview-section-content">
            <div v-if="data.gallery && data.gallery.length > 0" class="gallery-preview">
              <div v-for="(image, index) in data.gallery" :key="index" class="gallery-thumbnail">
                <img :src="image" alt="Portfolio image" />
              </div>
            </div>
            <div v-else class="preview-empty">
              <i class="icon-alert-circle"></i>
              <span>هیچ نمونه کاری اضافه نشده است</span>
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
  border: 2px solid var(--color-gray-300);
  border-radius: 12px;
  padding: 1.5rem;
  margin-bottom: 1.5rem;
  transition: all 0.2s ease;

  &:hover {
    border-color: var(--color-gray-400);
    box-shadow: var(--shadow-sm);
  }
}

.preview-section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--color-gray-300);
}

.preview-section-title {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--color-gray-900);
  margin: 0;

  i {
    color: var(--color-primary-700);
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
  background: var(--color-gray-50);
  border-radius: 8px;

  @media (max-width: 640px) {
    flex-direction: column;
  }
}

.preview-label {
  font-weight: 600;
  color: var(--color-gray-600);
  font-size: 0.875rem;
  min-width: 150px;
}

.preview-value {
  color: var(--color-gray-900);
  font-size: 0.9375rem;
  text-align: right;
  word-break: break-word;
}

.preview-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.preview-tag {
  display: inline-block;
  padding: 0.375rem 0.75rem;
  background: var(--color-primary-50);
  color: #6b21a8;
  border-radius: 12px;
  font-size: 0.875rem;
  font-weight: 500;
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
  background: var(--color-gray-50);
  border-radius: 8px;
  font-size: 0.9375rem;
  color: var(--color-gray-900);

  i {
    color: var(--color-success-500);
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
    color: var(--color-warning-500);
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
  border: 2px solid var(--color-primary-700);
  border-radius: 12px;
}
</style>
