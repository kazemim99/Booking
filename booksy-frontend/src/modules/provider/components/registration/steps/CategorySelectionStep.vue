<template>
  <div class="registration-step">

    <div class="step-card">
      <div class="step-header">
        <h2 class="step-title">دسته‌بندی کسب‌و‌کار</h2>
        <p class="step-description">نوع کسب‌و‌کار خود را انتخاب کنید</p>
      </div>

      <div class="categories-grid">
        <div
          v-for="category in categories"
          :key="category.id"
          class="category-card"
          :class="{ selected: selectedCategory === category.id }"
          @click="selectCategory(category.id)"
        >
          <div class="category-icon">{{ category.icon }}</div>
          <h3 class="category-name">{{ category.name }}</h3>
        </div>
      </div>

      <div class="step-actions">
        <AppButton
          type="button"
          variant="secondary"
          size="large"
          block
          @click="$emit('back')"
        >
          قبلی
        </AppButton>
        <AppButton
          type="button"
          variant="primary"
          size="large"
          block
          :disabled="!selectedCategory"
          @click="handleNext"
        >
          بعدی
        </AppButton>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import ProgressIndicator from '../shared/ProgressIndicator.vue'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'

interface Props {
  modelValue?: string | null
}

interface Emits {
  (e: 'update:modelValue', value: string): void
  (e: 'next'): void
  (e: 'back'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const selectedCategory = ref(props.modelValue || null)

const categories = [
  { id: 'hair_salon', name: 'آرایشگاه', icon: '💇' },
  { id: 'nail_salon', name: 'مانیکور و پدیکور', icon: '💅' },
  { id: 'beauty_spa', name: 'سالن زیبایی', icon: '✨' },
  { id: 'massage', name: 'ماساژ', icon: '💆' },
  { id: 'barber', name: 'آرایشگاه مردانه', icon: '✂️' },
  { id: 'gym', name: 'باشگاه ورزشی', icon: '🏋️' },
  { id: 'dental', name: 'دندانپزشکی', icon: '🦷' },
  { id: 'other', name: 'سایر', icon: '📋' },
]

const selectCategory = (categoryId: string) => {
  selectedCategory.value = categoryId
}

const handleNext = () => {
  if (selectedCategory.value) {
    emit('update:modelValue', selectedCategory.value)
    emit('next')
  }
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
  margin-bottom: 2rem;
}

.step-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin-bottom: 0.5rem;
}

.step-description {
  font-size: 0.875rem;
  color: #6b7280;
}

.categories-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  gap: 1rem;
  margin-bottom: 2rem;
}

.category-card {
  padding: 1.5rem 1rem;
  border: 2px solid #e5e7eb;
  border-radius: 0.75rem;
  cursor: pointer;
  transition: all 0.2s ease;
  text-align: center;
}

.category-card:hover {
  border-color: #8b5cf6;
  background: rgba(139, 92, 246, 0.05);
}

.category-card.selected {
  border-color: #8b5cf6;
  background: rgba(139, 92, 246, 0.1);
}

.category-icon {
  font-size: 2.5rem;
  margin-bottom: 0.5rem;
}

.category-name {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}

.step-actions {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  margin-top: 1.5rem;
  padding-top: 1.5rem;
  border-top: 1px solid #e5e7eb;
}

@media (max-width: 640px) {
  .categories-grid {
    grid-template-columns: repeat(auto-fill, minmax(100px, 1fr));
  }

  .step-actions {
    grid-template-columns: 1fr;
  }
}
</style>
