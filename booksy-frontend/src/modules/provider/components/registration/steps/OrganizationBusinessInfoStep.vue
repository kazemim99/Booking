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
              <label class="form-label">ایمیل (اختیاری)</label>
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
                disabled
                @blur="validatePhone"
              />
              <span v-if="errors.phone" class="form-error">{{ errors.phone }}</span>
              <span class="form-hint">شماره تماس تأیید شده شما</span>
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

        <!-- Logo (Optional) -->
        <div class="form-section">
          <h3 class="section-title">لوگو (اختیاری)</h3>
          <div class="form-group">
            <label class="form-label">لوگو کسب‌و‌کار</label>
            <div class="image-upload">
              <input
                ref="logoInputRef"
                type="file"
                accept="image/*"
                style="display: none"
                @change="handleLogoUpload"
              />
              <div v-if="localData.logoUrl" class="image-preview">
                <img :src="localData.logoUrl" alt="Logo" />
                <button type="button" class="remove-image" @click="removeLogo">
                  ×
                </button>
              </div>
              <div v-else class="image-placeholder" @click="triggerLogoUpload">
                <i class="icon-image"></i>
                <span>آپلود لوگو</span>
                <span class="upload-hint">PNG, JPG تا 5MB</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Actions -->
      <div class="step-actions">
        <AppButton variant="secondary" size="large" @click="$emit('back')">
          ← بازگشت
        </AppButton>

        <AppButton variant="primary" size="large" :disabled="!isValid" @click="handleNext">
          ادامه →
        </AppButton>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'

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
const logoInputRef = ref<HTMLInputElement>()

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
  // Email is optional, only validate if provided
  if (localData.value.email.trim() && !emailPattern.test(localData.value.email)) {
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

function triggerLogoUpload() {
  logoInputRef.value?.click()
}

function handleLogoUpload(event: Event) {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]

  if (!file) return

  // Validate file size (5MB max)
  if (file.size > 5 * 1024 * 1024) {
    alert('حجم فایل باید کمتر از 5 مگابایت باشد')
    return
  }

  // Validate file type
  if (!file.type.startsWith('image/')) {
    alert('فقط فایل‌های تصویری مجاز هستند')
    return
  }

  // Read file and convert to base64/data URL
  const reader = new FileReader()
  reader.onload = (e) => {
    localData.value.logoUrl = e.target?.result as string
  }
  reader.readAsDataURL(file)
}

function removeLogo() {
  localData.value.logoUrl = ''
  if (logoInputRef.value) {
    logoInputRef.value.value = ''
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

// Watch individual fields to auto-clear errors when user types
watch(() => localData.value.businessName, () => {
  if (localData.value.businessName.trim()) {
    validateBusinessName()
  }
})

watch(() => localData.value.ownerFirstName, () => {
  if (localData.value.ownerFirstName.trim()) {
    validateOwnerFirstName()
  }
})

watch(() => localData.value.ownerLastName, () => {
  if (localData.value.ownerLastName.trim()) {
    validateOwnerLastName()
  }
})

watch(() => localData.value.email, () => {
  validateEmail()
})

watch(() => localData.value.phone, () => {
  if (localData.value.phone.trim()) {
    validatePhone()
  }
})

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

.upload-hint {
  font-size: 0.75rem;
  color: #9ca3af;
  margin-top: 0.25rem;
}
</style>
