<template>
  <div class="registration-step">

    <div class="step-card">
      <div class="step-header">
        <div class="icon-wrapper">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            class="icon"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M7 8h10M7 12h4m1 8l-4-4H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-3l-4 4z"
            />
          </svg>
        </div>
        <h2 class="step-title">نظر شما برای ما مهم است</h2>
        <p class="step-description">چطور می‌توانیم بیشتر به شما کمک کنیم؟</p>
      </div>

      <form @submit.prevent="handleSubmit" class="form-content">
        <!-- Pre-defined Answers -->
        <div class="form-section">
          <label class="section-label">گزینه‌های پیشنهادی را انتخاب کنید (اختیاری)</label>
          <div class="options-grid">
            <button
              v-for="answer in preAnswers"
              :key="answer"
              type="button"
              @click="togglePreAnswer(answer)"
              :class="['option-button', { selected: selectedPreAnswers.includes(answer) }]"
            >
              <div class="option-content">
                <div :class="['checkbox', { checked: selectedPreAnswers.includes(answer) }]">
                  <svg
                    v-if="selectedPreAnswers.includes(answer)"
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    class="check-icon"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="3"
                      d="M5 13l4 4L19 7"
                    />
                  </svg>
                </div>
                <span class="option-text">{{ answer }}</span>
              </div>
            </button>
          </div>
        </div>

        <!-- Custom Feedback -->
        <div class="form-section">
          <label for="feedback" class="section-label">
            یا نظرات دیگری دارید؟ (اختیاری)
          </label>
          <textarea
            id="feedback"
            v-model="feedback"
            placeholder="لطفاً نظرات، پیشنهادات یا موارد خاصی که نیاز دارید را بنویسید..."
            class="feedback-textarea"
          />
          <p class="helper-text">
            می‌توانید هم از گزینه‌های بالا و هم از این بخش استفاده کنید
          </p>
        </div>

        <!-- Info Box -->
        <div class="info-box">
          <p class="info-text">
            💡 <span class="font-medium">نکته:</span> بازخورد شما به ما کمک می‌کند تا خدمات
            بهتری ارائه دهیم
          </p>
        </div>

        <!-- Actions -->
        <div class="step-actions">
          <AppButton type="button" variant="secondary" size="large" block @click="$emit('back')">
            قبلی
          </AppButton>
          <AppButton type="submit" variant="primary" size="large" block> پایان </AppButton>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

import AppButton from '@/shared/components/ui/Button/AppButton.vue'

interface Emits {
  (e: 'next', feedback?: string): void
  (e: 'back'): void
}

const emit = defineEmits<Emits>()

const preAnswers = [
  'رابط کاربری ساده و کاربردی است',
  'نیاز به قابلیت‌های بیشتر دارم',
  'فرآیند ثبت‌نام سریع و آسان بود',
  'دوست دارم آموزش بیشتری ببینم',
  'همه چیز عالی است',
]

const feedback = ref('')
const selectedPreAnswers = ref<string[]>([])

const togglePreAnswer = (answer: string) => {
  if (selectedPreAnswers.value.includes(answer)) {
    selectedPreAnswers.value = selectedPreAnswers.value.filter((a) => a !== answer)
  } else {
    selectedPreAnswers.value = [...selectedPreAnswers.value, answer]
  }
}

const handleSubmit = () => {
  const combinedFeedback = [...selectedPreAnswers.value, feedback.value.trim() && `دیگر: ${feedback.value.trim()}`]
    .filter(Boolean)
    .join(' | ')

  emit('next', combinedFeedback)
}
</script>

<style scoped>
.registration-step {
  min-height: 100vh;
  padding: 2rem 1rem;
  background: #f9fafb;
  direction: rtl;
}

.step-card {
  max-width: 42rem;
  margin: 0 auto;
  background: white;
  border-radius: 1rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  padding: 2rem;
}

.step-header {
  text-align: center;
  margin-bottom: 2rem;
}

.icon-wrapper {
  width: 4rem;
  height: 4rem;
  background: rgba(139, 92, 246, 0.1);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 1rem;
}

.icon {
  width: 2rem;
  height: 2rem;
  color: #8b5cf6;
}

.step-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin: 0 0 0.5rem 0;
}

.step-description {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

.form-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-section {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.section-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #111827;
}

.options-grid {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.option-button {
  padding: 0.75rem;
  border: 2px solid #d1d5db;
  border-radius: 0.5rem;
  background: white;
  cursor: pointer;
  transition: all 0.2s;
  text-align: right;
}

.option-button:hover {
  border-color: rgba(139, 92, 246, 0.5);
}

.option-button.selected {
  border-color: #8b5cf6;
  background: rgba(139, 92, 246, 0.05);
}

.option-content {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.checkbox {
  width: 1.25rem;
  height: 1.25rem;
  border: 2px solid rgba(107, 114, 128, 0.3);
  border-radius: 0.25rem;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: all 0.2s;
}

.checkbox.checked {
  border-color: #8b5cf6;
  background: #8b5cf6;
}

.check-icon {
  width: 0.75rem;
  height: 0.75rem;
  color: white;
  stroke-width: 3;
}

.option-text {
  flex: 1;
  font-size: 0.875rem;
  color: #111827;
}

.feedback-textarea {
  width: 100%;
  min-height: 120px;
  padding: 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  line-height: 1.5;
  resize: vertical;
  font-family: inherit;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.feedback-textarea:focus {
  outline: none;
  border-color: #8b5cf6;
  box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.1);
}

.feedback-textarea::placeholder {
  color: #9ca3af;
}

.helper-text {
  font-size: 0.75rem;
  color: #6b7280;
  margin: 0;
}

.info-box {
  padding: 1rem;
  background: rgba(236, 72, 153, 0.1);
  border-radius: 0.5rem;
}

.info-text {
  font-size: 0.875rem;
  color: #111827;
  margin: 0;
}

.font-medium {
  font-weight: 500;
}

.step-actions {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  padding-top: 1.5rem;
  border-top: 1px solid #e5e7eb;
}

@media (max-width: 640px) {
  .registration-step {
    padding: 1rem 0.5rem;
  }

  .step-card {
    padding: 1.5rem;
  }

  .step-actions {
    grid-template-columns: 1fr;
  }

  .step-actions button:first-child {
    order: 2;
  }

  .step-actions button:last-child {
    order: 1;
  }
}
</style>
