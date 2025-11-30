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
          <label class="form-label required">توضیحات کسب‌و‌کار</label>
          <textarea
            v-model="localData.description"
            class="form-textarea"
            placeholder="توضیحات کوتاهی درباره کسب‌و‌کار خود بنویسید..."
            rows="4"
            @blur="validateDescription"
          ></textarea>
          <span v-if="errors.description" class="form-error">{{ errors.description }}</span>
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
              <div v-if="localData.logoPreview || localData.logoUrl" class="image-preview">
                <img :src="localData.logoPreview || localData.logoUrl" alt="Logo" />
                <div v-if="isUploadingLogo" class="upload-overlay">
                  <div class="upload-spinner"></div>
                  <span>در حال آپلود...</span>
                </div>
                <button
                  v-if="!isUploadingLogo"
                  type="button"
                  class="remove-image"
                  @click="removeLogo"
                >
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
        <AppButton variant="secondary" size="large" :disabled="isUploadingLogo" @click="$emit('back')">
          ← بازگشت
        </AppButton>

        <AppButton
          variant="primary"
          size="large"
          :disabled="!isValid || isUploadingLogo"
          @click="handleNext"
        >
          <span v-if="isUploadingLogo">در حال آپلود لوگو...</span>
          <span v-else>ادامه →</span>
        </AppButton>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import { providerRegistrationService } from '@/modules/provider/services/provider-registration.service'

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

interface LocalBusinessInfo extends BusinessInfo {
  logoFile?: File
  logoPreview?: string
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

const localData = ref<LocalBusinessInfo>({
  ...props.modelValue,
  logoFile: undefined,
  logoPreview: undefined
})
const logoInputRef = ref<HTMLInputElement>()
const isUploadingLogo = ref(false)

const errors = ref({
  businessName: '',
  ownerFirstName: '',
  ownerLastName: '',
  email: '',
  phone: '',
  description: '',
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
    localData.value.description.trim() !== '' &&
    !errors.value.businessName &&
    !errors.value.ownerFirstName &&
    !errors.value.ownerLastName &&
    !errors.value.email &&
    !errors.value.phone &&
    !errors.value.description
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

function validateDescription() {
  if (!localData.value.description.trim()) {
    errors.value.description = 'توضیحات کسب‌و‌کار الزامی است'
  } else if (localData.value.description.length > 500) {
    errors.value.description = 'توضیحات نباید بیشتر از 500 کاراکتر باشد'
  } else {
    errors.value.description = ''
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

  // Store the file object for later upload
  localData.value.logoFile = file

  // Show base64 preview immediately (no server upload yet)
  const reader = new FileReader()
  reader.onload = (e) => {
    localData.value.logoPreview = e.target?.result as string
  }
  reader.readAsDataURL(file)
}

function removeLogo() {
  localData.value.logoUrl = ''
  localData.value.logoPreview = undefined
  localData.value.logoFile = undefined
  if (logoInputRef.value) {
    logoInputRef.value.value = ''
  }
}

async function handleNext() {
  // Validate all fields
  validateBusinessName()
  validateOwnerFirstName()
  validateOwnerLastName()
  validateEmail()
  validatePhone()
  validateDescription()

  if (!isValid.value) {
    return
  }

  try {
    // Upload logo if a new file was selected
    if (localData.value.logoFile) {
      isUploadingLogo.value = true

      const imageUrl = await providerRegistrationService.uploadBusinessLogo(localData.value.logoFile)

      // Set the uploaded URL and clear temporary data
      localData.value.logoUrl = imageUrl
      localData.value.logoPreview = undefined
      localData.value.logoFile = undefined
    }

    // Emit the data without the temporary File object and preview
    const { logoFile: _logoFile, logoPreview: _logoPreview, ...businessData } = localData.value
    emit('update:modelValue', businessData)
    emit('next')
  } catch (error) {
    console.error('Failed to upload logo:', error)
    alert('خطا در آپلود لوگو. لطفا دوباره تلاش کنید')
  } finally {
    isUploadingLogo.value = false
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

watch(() => localData.value.description, () => {
  if (localData.value.description.trim()) {
    validateDescription()
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

// Override image upload size for logo (make it smaller)
.image-upload {
  max-width: 200px;
  margin: 0 auto;

  .image-preview,
  .image-placeholder {
    width: 200px;
    height: 200px;

    img {
      width: 100%;
      height: 100%;
      object-fit: contain;
      background-color: #f9fafb;
    }
  }

  .upload-overlay {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.7);
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 0.75rem;
    color: #fff;
    font-size: 0.875rem;
    border-radius: 8px;
  }

  .upload-spinner {
    width: 32px;
    height: 32px;
    border: 3px solid rgba(255, 255, 255, 0.3);
    border-top-color: #fff;
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
  }
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
