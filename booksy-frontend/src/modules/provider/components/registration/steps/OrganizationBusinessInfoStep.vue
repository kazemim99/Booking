<template>
  <div class="organization-business-info-step">
    <div class="step-container">
      <div class="step-header">
        <h2 class="step-title">اطلاعات کسب‌و‌کار</h2>
        <p class="step-description">
          لطفاً اطلاعات کسب‌و‌کار خود را وارد کنید. این اطلاعات برای مشتریان شما نمایش داده می‌شود.
        </p>
      </div>

      <div class="step-content">
        <!-- Business Name -->
        <div class="form-group">
          <label class="form-label required">نام کسب‌و‌کار</label>
          <input
            v-model="localData.businessName"
            type="text"
            class="form-input"
            placeholder="مثال: سالن زیبایی الیت"
            @blur="validateBusinessName"
          />
          <span v-if="errors.businessName" class="form-error">{{ errors.businessName }}</span>
          <span class="form-hint">نام کسب‌و‌کار شما که برای مشتریان نمایش داده می‌شود</span>
        </div>

        <!-- Owner Information -->
        <div class="form-section">
          <h3 class="section-title">اطلاعات مالک</h3>
          <div class="form-row">
            <div class="form-group">
              <label class="form-label required">نام مالک</label>
              <input
                v-model="localData.ownerFirstName"
                type="text"
                class="form-input"
                placeholder="نام"
                @blur="validateOwnerFirstName"
              />
              <span v-if="errors.ownerFirstName" class="form-error">{{ errors.ownerFirstName }}</span>
            </div>

            <div class="form-group">
              <label class="form-label required">نام خانوادگی مالک</label>
              <input
                v-model="localData.ownerLastName"
                type="text"
                class="form-input"
                placeholder="نام خانوادگی"
                @blur="validateOwnerLastName"
              />
              <span v-if="errors.ownerLastName" class="form-error">{{ errors.ownerLastName }}</span>
            </div>
          </div>
        </div>

        <!-- Contact Information -->
        <div class="form-section">
          <h3 class="section-title">اطلاعات تماس</h3>
          <div class="form-row">
            <div class="form-group">
              <label class="form-label required">ایمیل</label>
              <input
                v-model="localData.email"
                type="email"
                class="form-input"
                placeholder="example@domain.com"
                dir="ltr"
                @blur="validateEmail"
              />
              <span v-if="errors.email" class="form-error">{{ errors.email }}</span>
            </div>

            <div class="form-group">
              <label class="form-label required">شماره تماس</label>
              <input
                v-model="localData.phone"
                type="tel"
                class="form-input"
                placeholder="09123456789"
                dir="ltr"
                @blur="validatePhone"
              />
              <span v-if="errors.phone" class="form-error">{{ errors.phone }}</span>
            </div>
          </div>
        </div>

        <!-- Description -->
        <div class="form-group">
          <label class="form-label">توضیحات کسب‌و‌کار</label>
          <textarea
            v-model="localData.description"
            class="form-textarea"
            placeholder="توضیحات کوتاهی درباره کسب‌و‌کار خود بنویسید..."
            rows="4"
          ></textarea>
          <span class="form-hint">{{ localData.description.length }} / 500 کاراکتر</span>
        </div>

        <!-- Logo & Cover Image (Optional) -->
        <div class="form-section">
          <h3 class="section-title">تصاویر (اختیاری)</h3>
          <div class="form-row">
            <div class="form-group">
              <label class="form-label">لوگو کسب‌و‌کار</label>
              <div class="image-upload">
                <div v-if="localData.logoUrl" class="image-preview">
                  <img :src="localData.logoUrl" alt="Logo" />
                  <button class="remove-image" @click="localData.logoUrl = ''">
                    <i class="icon-close"></i>
                  </button>
                </div>
                <div v-else class="image-placeholder">
                  <i class="icon-image"></i>
                  <span>آپلود لوگو</span>
                </div>
              </div>
            </div>

            <div class="form-group">
              <label class="form-label">تصویر کاور</label>
              <div class="image-upload">
                <div v-if="localData.coverImageUrl" class="image-preview">
                  <img :src="localData.coverImageUrl" alt="Cover" />
                  <button class="remove-image" @click="localData.coverImageUrl = ''">
                    <i class="icon-close"></i>
                  </button>
                </div>
                <div v-else class="image-placeholder">
                  <i class="icon-image"></i>
                  <span>آپلود تصویر کاور</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Actions -->
      <div class="step-actions">
        <AppButton variant="secondary" size="large" @click="$emit('back')">
          <i class="icon-arrow-right"></i>
          بازگشت
        </AppButton>

        <AppButton variant="primary" size="large" :disabled="!isValid" @click="handleNext">
          ادامه
          <i class="icon-arrow-left"></i>
        </AppButton>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import AppButton from '@/shared/components/AppButton.vue'

// ============================================
// Props & Emits
// ============================================

interface BusinessInfo {
  businessName: string
  ownerFirstName: string
  ownerLastName: string
  email: string
  phone: string
  description: string
  logoUrl: string
  coverImageUrl: string
}

interface Props {
  modelValue: BusinessInfo
}

const props = defineProps<Props>()
const emit = defineEmits<{
  (e: 'update:modelValue', value: BusinessInfo): void
  (e: 'next'): void
  (e: 'back'): void
}>()

// ============================================
// State
// ============================================

const localData = ref<BusinessInfo>({ ...props.modelValue })

const errors = ref({
  businessName: '',
  ownerFirstName: '',
  ownerLastName: '',
  email: '',
  phone: '',
})

// ============================================
// Computed
// ============================================

const isValid = computed(() => {
  return (
    localData.value.businessName.trim() !== '' &&
    localData.value.ownerFirstName.trim() !== '' &&
    localData.value.ownerLastName.trim() !== '' &&
    localData.value.email.trim() !== '' &&
    localData.value.phone.trim() !== '' &&
    !errors.value.businessName &&
    !errors.value.ownerFirstName &&
    !errors.value.ownerLastName &&
    !errors.value.email &&
    !errors.value.phone
  )
})

// ============================================
// Methods
// ============================================

function validateBusinessName() {
  if (!localData.value.businessName.trim()) {
    errors.value.businessName = 'نام کسب‌و‌کار الزامی است'
  } else if (localData.value.businessName.length < 3) {
    errors.value.businessName = 'نام کسب‌و‌کار باید حداقل ۳ کاراکتر باشد'
  } else {
    errors.value.businessName = ''
  }
}

function validateOwnerFirstName() {
  if (!localData.value.ownerFirstName.trim()) {
    errors.value.ownerFirstName = 'نام مالک الزامی است'
  } else {
    errors.value.ownerFirstName = ''
  }
}

function validateOwnerLastName() {
  if (!localData.value.ownerLastName.trim()) {
    errors.value.ownerLastName = 'نام خانوادگی مالک الزامی است'
  } else {
    errors.value.ownerLastName = ''
  }
}

function validateEmail() {
  const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  if (!localData.value.email.trim()) {
    errors.value.email = 'ایمیل الزامی است'
  } else if (!emailPattern.test(localData.value.email)) {
    errors.value.email = 'ایمیل معتبر نیست'
  } else {
    errors.value.email = ''
  }
}

function validatePhone() {
  const phonePattern = /^09\d{9}$/
  if (!localData.value.phone.trim()) {
    errors.value.phone = 'شماره تماس الزامی است'
  } else if (!phonePattern.test(localData.value.phone)) {
    errors.value.phone = 'شماره تماس معتبر نیست (مثال: 09123456789)'
  } else {
    errors.value.phone = ''
  }
}

function handleNext() {
  // Validate all fields
  validateBusinessName()
  validateOwnerFirstName()
  validateOwnerLastName()
  validateEmail()
  validatePhone()

  if (isValid.value) {
    emit('update:modelValue', localData.value)
    emit('next')
  }
}

// ============================================
// Watchers
// ============================================

watch(
  localData,
  (newValue) => {
    emit('update:modelValue', newValue)
  },
  { deep: true }
)
</script>

<style scoped lang="scss">
@import './steps-common.scss';
</style>
