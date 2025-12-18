<template>
  <div class="convert-to-organization-view">
    <div class="conversion-container">
      <div class="conversion-header">
        <h1 class="conversion-title">تبدیل به سازمان</h1>
        <p class="conversion-description">
          پروفایل فردی خود را به یک سازمان تجاری تبدیل کنید و امکان افزودن کارمند داشته باشید
        </p>
      </div>

      <!-- Step Indicator -->
      <div class="steps-indicator">
        <div
          v-for="(step, index) in steps"
          :key="index"
          class="step-item"
          :class="{ active: currentStep === index, completed: currentStep > index }"
        >
          <div class="step-number">
            <i v-if="currentStep > index" class="icon-check"></i>
            <span v-else>{{ index + 1 }}</span>
          </div>
          <span class="step-label">{{ step.label }}</span>
        </div>
      </div>

      <!-- Step 1: Impact Preview -->
      <div v-if="currentStep === 0" class="step-content">
        <h2 class="step-title">پیش‌نمایش تأثیرات تبدیل</h2>

        <div class="impact-section">
          <div class="impact-item positive">
            <i class="icon-check-circle"></i>
            <div>
              <h4>موارد حفظ شده:</h4>
              <ul>
                <li>تمام رزروهای فعلی و آینده</li>
                <li>نظرات و امتیازات</li>
                <li>خدمات تعریف شده</li>
                <li>برنامه زمانی و ساعات کاری</li>
                <li>تصاویر و اطلاعات تماس</li>
              </ul>
            </div>
          </div>

          <div class="impact-item info">
            <i class="icon-info-circle"></i>
            <div>
              <h4>تغییرات:</h4>
              <ul>
                <li>نوع پروفایل از "فردی" به "سازمان" تغییر می‌کند</li>
                <li>امکان افزودن کارمند فعال می‌شود</li>
                <li>نام تجاری جدید در نتایج جستجو نمایش داده می‌شود</li>
              </ul>
            </div>
          </div>

          <div class="impact-item warning">
            <i class="icon-alert-triangle"></i>
            <div>
              <h4>نکات مهم:</h4>
              <ul>
                <li>این عملیات غیرقابل برگشت است</li>
                <li>نام تجاری باید منحصر به فرد باشد</li>
                <li>پس از تبدیل، می‌توانید کارمند اضافه کنید</li>
              </ul>
            </div>
          </div>
        </div>

        <div class="step-actions">
          <AppButton variant="secondary" @click="$router.back()">
            انصراف
          </AppButton>
          <AppButton variant="primary" @click="nextStep">
            ادامه
          </AppButton>
        </div>
      </div>

      <!-- Step 2: Organization Details -->
      <div v-if="currentStep === 1" class="step-content">
        <h2 class="step-title">اطلاعات سازمان</h2>

        <form @submit.prevent="nextStep" class="organization-form">
          <div class="form-group">
            <label class="form-label">
              نام تجاری <span class="required">*</span>
            </label>
            <input
              v-model="formData.businessName"
              type="text"
              class="form-input"
              placeholder="مثال: آرایشگاه نیلوفر"
              required
            />
          </div>

          <div class="form-group">
            <label class="form-label">
              توضیحات <span class="required">*</span>
            </label>
            <textarea
              v-model="formData.description"
              class="form-textarea"
              rows="4"
              placeholder="درباره سازمان خود بنویسید..."
              required
            ></textarea>
          </div>

          <div class="form-group">
            <label class="form-label">
              نوع کسب‌وکار <span class="required">*</span>
            </label>
            <select v-model="formData.businessType" class="form-select" required>
              <option value="">انتخاب کنید</option>
              <option value="salon">آرایشگاه</option>
              <option value="clinic">کلینیک</option>
              <option value="spa">سالن زیبایی</option>
              <option value="other">سایر</option>
            </select>
          </div>

          <div class="form-group">
            <label class="form-label">آدرس لوگو (اختیاری)</label>
            <input
              v-model="formData.logoUrl"
              type="url"
              dir="ltr"
              class="form-input"
              placeholder="https://example.com/logo.png"
            />
          </div>

          <div class="step-actions">
            <AppButton type="button" variant="secondary" @click="previousStep">
              بازگشت
            </AppButton>
            <AppButton type="submit" variant="primary">
              ادامه
            </AppButton>
          </div>
        </form>
      </div>

      <!-- Step 3: Confirmation -->
      <div v-if="currentStep === 2" class="step-content">
        <h2 class="step-title">تأیید نهایی</h2>

        <div class="confirmation-preview">
          <h3>اطلاعات سازمان جدید شما:</h3>
          <div class="preview-item">
            <strong>نام تجاری:</strong>
            <span>{{ formData.businessName }}</span>
          </div>
          <div class="preview-item">
            <strong>توضیحات:</strong>
            <span>{{ formData.description }}</span>
          </div>
          <div class="preview-item">
            <strong>نوع کسب‌وکار:</strong>
            <span>{{ getBusinessTypeLabel(formData.businessType) }}</span>
          </div>
        </div>

        <div class="confirmation-checkbox">
          <input
            type="checkbox"
            id="confirmConversion"
            v-model="isConfirmed"
          />
          <label for="confirmConversion">
            تأیید می‌کنم که از تبدیل پروفایل خود به سازمان مطلع هستم و این تصمیم را پذیرفته‌ام.
          </label>
        </div>

        <!-- Error State with Retry Options -->
        <div v-if="conversionError" class="error-state">
          <div class="error-content">
            <i class="icon-alert-circle"></i>
            <h4>خطا در تبدیل</h4>
            <p>{{ conversionError }}</p>
          </div>
          <div class="error-actions">
            <AppButton variant="secondary" @click="goBackToEdit">
              ویرایش اطلاعات
            </AppButton>
            <AppButton variant="primary" @click="retryConversion" :loading="isSubmitting">
              تلاش مجدد
            </AppButton>
          </div>
        </div>

        <div v-else class="step-actions">
          <AppButton variant="secondary" @click="previousStep" :disabled="isSubmitting">
            بازگشت
          </AppButton>
          <AppButton
            variant="primary"
            @click="handleConvert"
            :disabled="!isConfirmed || isSubmitting"
            :loading="isSubmitting"
          >
            {{ isSubmitting ? 'در حال تبدیل...' : 'تبدیل به سازمان' }}
          </AppButton>
        </div>
      </div>

      <!-- Success State -->
      <div v-if="currentStep === 3" class="step-content success-state">
        <i class="icon-check-circle"></i>
        <h2>تبدیل با موفقیت انجام شد!</h2>
        <p>پروفایل شما به یک سازمان تبدیل شد. اکنون می‌توانید کارمند اضافه کنید.</p>
        <AppButton variant="primary" @click="$router.push('/provider/staff')">
          مدیریت کارمندان
        </AppButton>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useHierarchyStore } from '../../stores/hierarchy.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useToast } from '@/core/composables/useToast'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'

const router = useRouter()
const hierarchyStore = useHierarchyStore()
const authStore = useAuthStore()
const toast = useToast()

const steps = [
  { label: 'پیش‌نمایش' },
  { label: 'اطلاعات' },
  { label: 'تأیید' },
]

const currentStep = ref(0)
const isConfirmed = ref(false)
const isSubmitting = ref(false)
const conversionError = ref<string | null>(null)
const canRetry = ref(false)

const formData = reactive({
  businessName: '',
  description: '',
  businessType: '',
  logoUrl: '',
})

function nextStep() {
  if (currentStep.value < steps.length - 1) {
    currentStep.value++
  }
}

function previousStep() {
  if (currentStep.value > 0) {
    currentStep.value--
  }
}

function getBusinessTypeLabel(type: string): string {
  const labels: Record<string, string> = {
    salon: 'آرایشگاه',
    clinic: 'کلینیک',
    spa: 'سالن زیبایی',
    other: 'سایر',
  }
  return labels[type] || type
}

function resetError() {
  conversionError.value = null
  canRetry.value = false
}

function goBackToEdit() {
  resetError()
  currentStep.value = 1
}

async function handleConvert() {
  if (!isConfirmed.value) return

  isSubmitting.value = true
  resetError()

  try {
    const currentProviderId = authStore.currentUser?.providerId
    if (!currentProviderId) {
      throw new Error('شناسه ارائه‌دهنده یافت نشد')
    }

    await hierarchyStore.convertToOrganization(currentProviderId, {
      individualProviderId: currentProviderId,
      businessName: formData.businessName,
      description: formData.description,
      businessType: formData.businessType,
      logoUrl: formData.logoUrl || undefined,
    })

    toast.success('موفقیت', 'پروفایل شما با موفقیت به سازمان تبدیل شد')
    currentStep.value = 3
  } catch (error: any) {
    console.error('Error converting to organization:', error)

    // Parse error message for user-friendly display
    const errorMessage = error.response?.data?.message
      || error.message
      || 'خطا در تبدیل به سازمان'

    conversionError.value = errorMessage
    canRetry.value = true

    toast.error('خطا', errorMessage)
  } finally {
    isSubmitting.value = false
  }
}

async function retryConversion() {
  resetError()
  await handleConvert()
}
</script>

<style scoped lang="scss">
.convert-to-organization-view {
  min-height: 100vh;
  background: #f7fafc;
  padding: 2rem;
}

.conversion-container {
  max-width: 800px;
  margin: 0 auto;
  background: white;
  border-radius: 16px;
  padding: 2rem;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.conversion-header {
  text-align: center;
  margin-bottom: 3rem;

  .conversion-title {
    font-size: 2rem;
    color: #1a202c;
    margin-bottom: 0.5rem;
  }

  .conversion-description {
    color: #718096;
    font-size: 1rem;
  }
}

.steps-indicator {
  display: flex;
  justify-content: space-between;
  margin-bottom: 3rem;

  .step-item {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    position: relative;

    &:not(:last-child)::after {
      content: '';
      position: absolute;
      top: 20px;
      right: 50%;
      width: 100%;
      height: 2px;
      background: #e2e8f0;
    }

    &.active .step-number,
    &.completed .step-number {
      background: #667eea;
      color: white;
    }

    &.completed::after {
      background: #667eea;
    }

    .step-number {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: #e2e8f0;
      color: #a0aec0;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 600;
      margin-bottom: 0.5rem;
      z-index: 1;
    }

    .step-label {
      font-size: 0.875rem;
      color: #718096;
    }
  }
}

.step-content {
  .step-title {
    font-size: 1.5rem;
    color: #1a202c;
    margin-bottom: 1.5rem;
  }
}

.impact-section {
  margin-bottom: 2rem;

  .impact-item {
    border-radius: 8px;
    padding: 1.5rem;
    margin-bottom: 1rem;
    display: flex;
    gap: 1rem;

    &.positive {
      background: #f0fff4;
      border: 1px solid #9ae6b4;

      i {
        color: #38a169;
      }
    }

    &.info {
      background: #ebf8ff;
      border: 1px solid #90cdf4;

      i {
        color: #3182ce;
      }
    }

    &.warning {
      background: #fffaf0;
      border: 1px solid #fbd38d;

      i {
        color: #dd6b20;
      }
    }

    i {
      font-size: 1.5rem;
      margin-top: 0.25rem;
    }

    h4 {
      font-size: 1rem;
      margin-bottom: 0.5rem;
      color: #1a202c;
    }

    ul {
      margin: 0;
      padding-right: 1.5rem;
      font-size: 0.95rem;
      line-height: 1.8;

      li {
        margin-bottom: 0.25rem;
      }
    }
  }
}

.organization-form {
  .form-group {
    margin-bottom: 1.5rem;

    .form-label {
      display: block;
      font-weight: 500;
      margin-bottom: 0.5rem;
      color: #2d3748;

      .required {
        color: #f56565;
      }
    }

    .form-input,
    .form-textarea,
    .form-select {
      width: 100%;
      padding: 0.75rem 1rem;
      border: 1px solid #cbd5e0;
      border-radius: 8px;
      font-size: 1rem;
      font-family: inherit;

      &:focus {
        outline: none;
        border-color: #667eea;
        box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
      }
    }

    .form-textarea {
      resize: vertical;
    }
  }
}

.confirmation-preview {
  background: #f7fafc;
  border-radius: 8px;
  padding: 1.5rem;
  margin-bottom: 1.5rem;

  h3 {
    font-size: 1.125rem;
    margin-bottom: 1rem;
    color: #1a202c;
  }

  .preview-item {
    padding: 0.75rem 0;
    border-bottom: 1px solid #e2e8f0;
    display: flex;
    justify-content: space-between;

    &:last-child {
      border-bottom: none;
    }

    strong {
      color: #4a5568;
    }

    span {
      color: #1a202c;
    }
  }
}

.confirmation-checkbox {
  background: #fffaf0;
  border: 1px solid #fbd38d;
  border-radius: 8px;
  padding: 1rem;
  margin-bottom: 2rem;
  display: flex;
  gap: 0.75rem;

  input[type='checkbox'] {
    margin-top: 0.25rem;
  }

  label {
    color: #742a2a;
    cursor: pointer;
  }
}

.success-state {
  text-align: center;
  padding: 3rem 1rem;

  i {
    font-size: 4rem;
    color: #48bb78;
    margin-bottom: 1rem;
  }

  h2 {
    font-size: 1.75rem;
    color: #1a202c;
    margin-bottom: 1rem;
  }

  p {
    color: #718096;
    margin-bottom: 2rem;
  }
}

.error-state {
  background: #fff5f5;
  border: 1px solid #feb2b2;
  border-radius: 12px;
  padding: 1.5rem;
  margin-top: 1.5rem;

  .error-content {
    display: flex;
    flex-direction: column;
    align-items: center;
    text-align: center;
    margin-bottom: 1.5rem;

    i {
      font-size: 3rem;
      color: #e53e3e;
      margin-bottom: 1rem;
    }

    h4 {
      font-size: 1.25rem;
      color: #c53030;
      margin-bottom: 0.5rem;
    }

    p {
      color: #742a2a;
      font-size: 0.95rem;
      line-height: 1.6;
    }
  }

  .error-actions {
    display: flex;
    gap: 1rem;
    justify-content: center;
  }
}

.step-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  margin-top: 2rem;
}

@media (max-width: 640px) {
  .conversion-container {
    padding: 1.5rem;
  }

  .steps-indicator {
    .step-label {
      display: none;
    }
  }

  .step-actions {
    flex-direction: column-reverse;

    button {
      width: 100%;
    }
  }
}
</style>
