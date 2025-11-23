<template>
  <div class="provider-type-selection">
    <div class="selection-header">
      <h1 class="selection-title">نوع کسب‌و‌کار خود را انتخاب کنید</h1>
      <p class="selection-description">
        لطفاً نوع ارائه‌دهنده خدمات خود را مشخص کنید. این انتخاب بر نحوه مدیریت پروفایل و رزروها تأثیر می‌گذارد.
      </p>
    </div>

    <div class="type-options">
      <!-- Organization Option -->
      <div
        class="type-card"
        :class="{ selected: selectedType === 'Organization' }"
        @click="selectType('Organization')"
      >
        <div class="type-card-header">
          <div class="type-icon">
            <i class="icon-building"></i>
          </div>
          <h3 class="type-title">سازمان / کسب‌و‌کار</h3>
        </div>

        <p class="type-description">
          برای کسب‌و‌کارهایی با موقعیت فیزیکی مانند آرایشگاه‌ها، کلینیک‌ها، و سالن‌های زیبایی
        </p>

        <div class="type-features">
          <h4 class="features-title">ویژگی‌ها:</h4>
          <ul class="features-list">
            <li class="feature-item">
              <i class="icon-check"></i>
              <span>امکان اضافه کردن کارکنان</span>
            </li>
            <li class="feature-item">
              <i class="icon-check"></i>
              <span>مدیریت چندین متخصص</span>
            </li>
            <li class="feature-item">
              <i class="icon-check"></i>
              <span>برند تجاری مستقل</span>
            </li>
            <li class="feature-item">
              <i class="icon-check"></i>
              <span>گزارش‌های عملکرد تیمی</span>
            </li>
            <li class="feature-item">
              <i class="icon-check"></i>
              <span>رشد و توسعه کسب‌و‌کار</span>
            </li>
          </ul>
        </div>

        <div v-if="selectedType === 'Organization'" class="type-badge">
          <i class="icon-check-circle"></i>
          <span>انتخاب شده</span>
        </div>
      </div>

      <!-- Individual Option -->
      <div
        class="type-card"
        :class="{ selected: selectedType === 'Individual' }"
        @click="selectType('Individual')"
      >
        <div class="type-card-header">
          <div class="type-icon">
            <i class="icon-user"></i>
          </div>
          <h3 class="type-title">فرد مستقل</h3>
        </div>

        <p class="type-description">
          برای متخصصان مستقل و فریلنسرها مانند آرایشگران سیار و مشاوران
        </p>

        <div class="type-features">
          <h4 class="features-title">ویژگی‌ها:</h4>
          <ul class="features-list">
            <li class="feature-item">
              <i class="icon-check"></i>
              <span>پروفایل شخصی</span>
            </li>
            <li class="feature-item">
              <i class="icon-check"></i>
              <span>مدیریت آسان</span>
            </li>
            <li class="feature-item">
              <i class="icon-check"></i>
              <span>انعطاف‌پذیری بالا</span>
            </li>
            <li class="feature-item">
              <i class="icon-check"></i>
              <span>خدمات سیار (اختیاری)</span>
            </li>
            <li class="feature-item">
              <i class="icon-check"></i>
              <span>امکان پیوستن به سازمان</span>
            </li>
          </ul>
        </div>

        <div v-if="selectedType === 'Individual'" class="type-badge">
          <i class="icon-check-circle"></i>
          <span>انتخاب شده</span>
        </div>
      </div>
    </div>

    <!-- Recommendation Box -->
    <div v-if="showRecommendation" class="recommendation-box">
      <div class="recommendation-header">
        <i class="icon-lightbulb"></i>
        <h4>توصیه ما</h4>
      </div>
      <p class="recommendation-text">{{ recommendationText }}</p>
    </div>

    <!-- Action Buttons -->
    <div class="selection-actions">
      <AppButton
        variant="secondary"
        size="large"
        @click="$emit('back')"
      >
        بازگشت
      </AppButton>

      <AppButton
        variant="primary"
        size="large"
        :disabled="!selectedType"
        @click="handleContinue"
      >
        ادامه
        <i class="icon-arrow-left"></i>
      </AppButton>
    </div>

    <!-- Help Section -->
    <div class="help-section">
      <p class="help-text">
        مطمئن نیستید کدام نوع مناسب شماست؟
        <a href="#" class="help-link" @click.prevent="showHelp = true">راهنما را ببینید</a>
      </p>
    </div>

    <!-- Help Modal -->
    <Teleport to="body">
      <div v-if="showHelp" class="modal-overlay" @click="showHelp = false">
        <div class="modal-content" @click.stop>
          <div class="modal-header">
            <h3 class="modal-title">راهنمای انتخاب نوع کسب‌و‌کار</h3>
            <button class="modal-close" @click="showHelp = false">
              <i class="icon-close"></i>
            </button>
          </div>

          <div class="modal-body">
            <div class="help-item">
              <h4>سازمان را انتخاب کنید اگر:</h4>
              <ul>
                <li>موقعیت فیزیکی (آرایشگاه، کلینیک) دارید</li>
                <li>قصد دارید کارکنان استخدام کنید</li>
                <li>می‌خواهید برند تجاری ایجاد کنید</li>
                <li>چندین متخصص در کسب‌و‌کار شما کار می‌کنند</li>
              </ul>
            </div>

            <div class="help-item">
              <h4>فرد مستقل را انتخاب کنید اگر:</h4>
              <ul>
                <li>به صورت فریلنسر کار می‌کنید</li>
                <li>خدمات سیار ارائه می‌دهید</li>
                <li>قصد دارید به عنوان کارمند به سازمانی بپیوندید</li>
                <li>فقط خودتان خدمات ارائه می‌دهید</li>
              </ul>
            </div>

            <div class="help-note">
              <i class="icon-info-circle"></i>
              <p>نگران نباشید! می‌توانید بعداً از فرد مستقل به سازمان تبدیل شوید.</p>
            </div>
          </div>

          <div class="modal-footer">
            <AppButton variant="primary" @click="showHelp = false">متوجه شدم</AppButton>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { ProviderHierarchyType } from '../../types/hierarchy.types'
import AppButton from '@/shared/components/AppButton.vue'

// ============================================
// Props & Emits
// ============================================

interface Props {
  initialSelection?: ProviderHierarchyType
}

const props = withDefaults(defineProps<Props>(), {
  initialSelection: undefined,
})

const emit = defineEmits<{
  (e: 'select', type: ProviderHierarchyType): void
  (e: 'back'): void
}>()

// ============================================
// State
// ============================================

const selectedType = ref<ProviderHierarchyType | null>(props.initialSelection || null)
const showHelp = ref(false)

// ============================================
// Computed
// ============================================

const showRecommendation = computed(() => !!selectedType.value)

const recommendationText = computed(() => {
  if (selectedType.value === 'Organization') {
    return 'انتخاب عالی! به عنوان سازمان می‌توانید کارکنان اضافه کنید و کسب‌و‌کار خود را گسترش دهید. مشتریان می‌توانند متخصص مورد نظر خود را انتخاب کنند.'
  } else if (selectedType.value === 'Individual') {
    return 'انتخاب خوبی! به عنوان فرد مستقل می‌توانید به راحتی خدمات خود را مدیریت کنید و در آینده به سازمانی بپیوندید یا به سازمان تبدیل شوید.'
  }
  return ''
})

// ============================================
// Methods
// ============================================

function selectType(type: ProviderHierarchyType): void {
  selectedType.value = type
}

function handleContinue(): void {
  if (selectedType.value) {
    emit('select', selectedType.value)
  }
}
</script>

<style scoped lang="scss">
.provider-type-selection {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}

.selection-header {
  text-align: center;
  margin-bottom: 3rem;
}

.selection-title {
  font-size: 2rem;
  font-weight: 700;
  color: #1a1a1a;
  margin-bottom: 0.5rem;
}

.selection-description {
  font-size: 1rem;
  color: #666;
  max-width: 600px;
  margin: 0 auto;
  line-height: 1.6;
}

.type-options {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
  gap: 2rem;
  margin-bottom: 2rem;
}

.type-card {
  position: relative;
  background: #fff;
  border: 2px solid #e0e0e0;
  border-radius: 12px;
  padding: 2rem;
  cursor: pointer;
  transition: all 0.3s ease;

  &:hover {
    border-color: #7c3aed;
    box-shadow: 0 4px 12px rgba(124, 58, 237, 0.1);
    transform: translateY(-4px);
  }

  &.selected {
    border-color: #7c3aed;
    background: linear-gradient(135deg, #f8f5ff 0%, #fff 100%);
    box-shadow: 0 8px 24px rgba(124, 58, 237, 0.15);
  }
}

.type-card-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
}

.type-icon {
  width: 60px;
  height: 60px;
  background: linear-gradient(135deg, #7c3aed 0%, #9333ea 100%);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.75rem;
  color: #fff;
}

.type-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1a1a1a;
}

.type-description {
  font-size: 0.95rem;
  color: #666;
  line-height: 1.6;
  margin-bottom: 1.5rem;
}

.type-features {
  margin-top: 1.5rem;
}

.features-title {
  font-size: 0.95rem;
  font-weight: 600;
  color: #1a1a1a;
  margin-bottom: 0.75rem;
}

.features-list {
  list-style: none;
  padding: 0;
  margin: 0;
}

.feature-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0;
  font-size: 0.9rem;
  color: #555;

  i {
    color: #10b981;
    font-size: 1rem;
  }
}

.type-badge {
  position: absolute;
  top: 1rem;
  left: 1rem;
  background: #10b981;
  color: #fff;
  padding: 0.5rem 1rem;
  border-radius: 20px;
  font-size: 0.875rem;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.recommendation-box {
  background: linear-gradient(135deg, #fef3c7 0%, #fef9e7 100%);
  border: 2px solid #f59e0b;
  border-radius: 12px;
  padding: 1.5rem;
  margin-bottom: 2rem;
}

.recommendation-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.75rem;

  i {
    color: #f59e0b;
    font-size: 1.5rem;
  }

  h4 {
    font-size: 1.1rem;
    font-weight: 700;
    color: #92400e;
  }
}

.recommendation-text {
  font-size: 0.95rem;
  color: #92400e;
  line-height: 1.6;
  margin: 0;
}

.selection-actions {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.help-section {
  text-align: center;
  padding: 1rem;
}

.help-text {
  font-size: 0.95rem;
  color: #666;
}

.help-link {
  color: #7c3aed;
  text-decoration: none;
  font-weight: 600;

  &:hover {
    text-decoration: underline;
  }
}

// Modal Styles
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 1rem;
}

.modal-content {
  background: #fff;
  border-radius: 16px;
  max-width: 600px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid #e0e0e0;
}

.modal-title {
  font-size: 1.25rem;
  font-weight: 700;
  color: #1a1a1a;
}

.modal-close {
  background: none;
  border: none;
  font-size: 1.5rem;
  color: #666;
  cursor: pointer;
  padding: 0.5rem;
  display: flex;
  align-items: center;
  justify-content: center;

  &:hover {
    color: #1a1a1a;
  }
}

.modal-body {
  padding: 1.5rem;
}

.help-item {
  margin-bottom: 1.5rem;

  h4 {
    font-size: 1.1rem;
    font-weight: 600;
    color: #1a1a1a;
    margin-bottom: 0.75rem;
  }

  ul {
    list-style: none;
    padding: 0;

    li {
      padding: 0.5rem 0;
      padding-right: 1.5rem;
      position: relative;
      font-size: 0.95rem;
      color: #555;

      &::before {
        content: '•';
        position: absolute;
        right: 0;
        color: #7c3aed;
        font-weight: bold;
      }
    }
  }
}

.help-note {
  background: #f3f4f6;
  border-radius: 8px;
  padding: 1rem;
  display: flex;
  gap: 0.75rem;
  align-items: flex-start;

  i {
    color: #3b82f6;
    font-size: 1.25rem;
    flex-shrink: 0;
  }

  p {
    font-size: 0.9rem;
    color: #1f2937;
    margin: 0;
    line-height: 1.5;
  }
}

.modal-footer {
  padding: 1.5rem;
  border-top: 1px solid #e0e0e0;
  display: flex;
  justify-content: flex-end;
}

// Responsive
@media (max-width: 768px) {
  .type-options {
    grid-template-columns: 1fr;
  }

  .selection-actions {
    flex-direction: column;
  }
}
</style>
