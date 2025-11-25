<template>
  <div class="individual-personal-info-step">
    <div class="step-container">
      <div class="step-header">
        <h2 class="step-title">اطلاعات شخصی</h2>
        <p class="step-description">
          لطفاً اطلاعات شخصی خود را وارد کنید. این اطلاعات در پروفایل عمومی شما نمایش داده می‌شود.
        </p>
      </div>

      <div class="step-content">
        <!-- Profile Photo -->
        <div class="form-group">
          <label class="form-label">عکس پروفایل (اختیاری)</label>
          <div class="avatar-upload">
            <div v-if="localData.avatarUrl" class="avatar-preview">
              <img :src="localData.avatarUrl" alt="Avatar" />
              <button class="remove-avatar" @click="localData.avatarUrl = ''">
                <i class="icon-close"></i>
              </button>
            </div>
            <div v-else class="avatar-placeholder">
              <i class="icon-user"></i>
              <span>آپلود عکس</span>
            </div>
          </div>
        </div>

        <!-- Name -->
        <div class="form-row">
          <div class="form-group">
            <label class="form-label required">نام</label>
            <input
              v-model="localData.firstName"
              type="text"
              class="form-input"
              placeholder="نام"
              @blur="validateFirstName"
            />
            <span v-if="errors.firstName" class="form-error">{{ errors.firstName }}</span>
          </div>

          <div class="form-group">
            <label class="form-label required">نام خانوادگی</label>
            <input
              v-model="localData.lastName"
              type="text"
              class="form-input"
              placeholder="نام خانوادگی"
              @blur="validateLastName"
            />
            <span v-if="errors.lastName" class="form-error">{{ errors.lastName }}</span>
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

        <!-- Bio -->
        <div class="form-group">
          <label class="form-label">بیوگرافی</label>
          <textarea
            v-model="localData.bio"
            class="form-textarea"
            placeholder="توضیحات کوتاهی درباره خود و تجربیاتتان بنویسید..."
            rows="4"
          ></textarea>
          <span class="form-hint">{{ localData.bio.length }} / 500 کاراکتر</span>
        </div>

        <!-- Specializations -->
        <div class="form-group">
          <label class="form-label">تخصص‌ها (اختیاری)</label>
          <div class="specializations-input">
            <input
              v-model="specializationInput"
              type="text"
              class="form-input"
              placeholder="مثال: کوتاهی مو، رنگ مو، میکاپ..."
              @keypress.enter.prevent="addSpecialization"
            />
            <AppButton variant="secondary" @click="addSpecialization">افزودن</AppButton>
          </div>
          <div v-if="localData.specializations.length > 0" class="specializations-list">
            <div
              v-for="(spec, index) in localData.specializations"
              :key="index"
              class="specialization-tag"
            >
              <span>{{ spec }}</span>
              <button @click="removeSpecialization(index)">
                <i class="icon-close"></i>
              </button>
            </div>
          </div>
          <span class="form-hint">تخصص‌های خود را اضافه کنید تا مشتریان بهتر شما را بشناسند</span>
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

interface PersonalInfo {
  firstName: string
  lastName: string
  email: string
  phone: string
  bio: string
  avatarUrl: string
  specializations: string[]
}

interface Props {
  modelValue: PersonalInfo
}

const props = defineProps<Props>()
const emit = defineEmits<{
  (e: 'update:modelValue', value: PersonalInfo): void
  (e: 'next'): void
  (e: 'back'): void
}>()

// ============================================
// State
// ============================================

const localData = ref<PersonalInfo>({ ...props.modelValue })
const specializationInput = ref('')

const errors = ref({
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
})

// ============================================
// Computed
// ============================================

const isValid = computed(() => {
  return (
    localData.value.firstName.trim() !== '' &&
    localData.value.lastName.trim() !== '' &&
    localData.value.email.trim() !== '' &&
    localData.value.phone.trim() !== '' &&
    !errors.value.firstName &&
    !errors.value.lastName &&
    !errors.value.email &&
    !errors.value.phone
  )
})

// ============================================
// Methods
// ============================================

function validateFirstName() {
  if (!localData.value.firstName.trim()) {
    errors.value.firstName = 'نام الزامی است'
  } else {
    errors.value.firstName = ''
  }
}

function validateLastName() {
  if (!localData.value.lastName.trim()) {
    errors.value.lastName = 'نام خانوادگی الزامی است'
  } else {
    errors.value.lastName = ''
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

function addSpecialization() {
  const value = specializationInput.value.trim()
  if (value && !localData.value.specializations.includes(value)) {
    localData.value.specializations.push(value)
    specializationInput.value = ''
  }
}

function removeSpecialization(index: number) {
  localData.value.specializations.splice(index, 1)
}

function handleNext() {
  // Validate all fields
  validateFirstName()
  validateLastName()
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

.avatar-upload {
  display: flex;
  justify-content: center;
  margin-bottom: 2rem;
}

.avatar-preview {
  position: relative;
  width: 150px;
  height: 150px;
  border-radius: 50%;
  overflow: hidden;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  .remove-avatar {
    position: absolute;
    top: 0.5rem;
    right: 0.5rem;
    width: 32px;
    height: 32px;
    background: rgba(239, 68, 68, 0.9);
    border: none;
    border-radius: 50%;
    color: #fff;
    font-size: 1rem;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    transition: all 0.2s ease;

    &:hover {
      background: #dc2626;
      transform: scale(1.1);
    }
  }
}

.avatar-placeholder {
  width: 150px;
  height: 150px;
  background: #f3f4f6;
  border: 2px dashed #d1d5db;
  border-radius: 50%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  color: #6b7280;
  cursor: pointer;
  transition: all 0.2s ease;

  i {
    font-size: 2.5rem;
  }

  &:hover {
    border-color: #7c3aed;
    color: #7c3aed;
    background: #f8f5ff;
  }
}

.specializations-input {
  display: flex;
  gap: 0.75rem;
}

.specializations-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-top: 1rem;
}

.specialization-tag {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  background: #ede9fe;
  color: #6b21a8;
  border-radius: 20px;
  font-size: 0.875rem;
  font-weight: 500;

  button {
    background: none;
    border: none;
    color: #6b21a8;
    cursor: pointer;
    padding: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.2s ease;

    &:hover {
      color: #581c87;
      transform: scale(1.2);
    }
  }
}
</style>
