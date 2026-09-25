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
          :data-testid="`category-${category.slug}`"
          @click="selectCategory(category.id)"
        >
          <div class="category-icon">{{ category.icon }}</div>
          <h3 class="category-name">{{ category.persianName }}</h3>
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
import { ref, watch } from 'vue'

import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import { ProviderCategory } from '@/core/types/enums.types'
import { getCategoryMetadata, parseCategory } from '@/core/constants/provider-categories'

interface Props {
  /**
   * The category the wizard already holds. Accepts anything the backend or a saved draft may
   * carry — the canonical slug, the enum member name ("HairSalon", which is what the draft
   * endpoints return), or the numeric id — and normalises it so resuming a draft re-selects
   * the right card.
   */
  modelValue?: string | number | null
}

interface Emits {
  /** Emits the canonical category slug, e.g. "hair-salon". */
  (e: 'update:modelValue', value: string): void
  (e: 'next'): void
  (e: 'back'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

/**
 * Categories offered during registration.
 *
 * Deliberately a subset: the platform has 15 categories, but onboarding is currently open to
 * hair salons and barbershops only. Adding one here is all that is needed to open it up — the
 * label, icon and slug all come from the shared metadata table, so this list cannot drift from
 * the backend enum the way the old hardcoded array did.
 */
const ENABLED_CATEGORIES: ProviderCategory[] = [
  ProviderCategory.HairSalon,
  ProviderCategory.Barbershop,
  // ProviderCategory.BeautySalon,
  // ProviderCategory.NailSalon,
  // ProviderCategory.Spa,
  // ProviderCategory.Massage,
  // ProviderCategory.Gym,
  // ProviderCategory.Dental,
]

const categories = ENABLED_CATEGORIES.map(getCategoryMetadata)

const selectedCategory = ref<ProviderCategory | null>(null)

// The draft endpoints return the enum member name ("HairSalon"), not the slug the cards are
// keyed by, so the incoming value is normalised rather than compared verbatim — otherwise
// resuming a registration left every card unselected.
watch(
  () => props.modelValue,
  (newValue) => {
    const parsed = parseCategory(newValue)
    if (parsed !== null) {
      selectedCategory.value = parsed
    }
  },
  { immediate: true }
)

const selectCategory = (categoryId: ProviderCategory) => {
  selectedCategory.value = categoryId
}

const handleNext = () => {
  if (selectedCategory.value !== null) {
    emit('update:modelValue', getCategoryMetadata(selectedCategory.value).slug)
    emit('next')
  }
}
</script>

<style scoped>
.registration-step {
  min-height: 100vh;
  padding: 2rem 1rem;
  background: var(--color-gray-50);
  direction: rtl;
}

.step-card {
  max-width: 42rem;
  margin: 0 auto;
  background: white;
  border-radius: 1rem;
  box-shadow: var(--shadow-sm);
  padding: 2rem;
}

.step-header {
  margin-bottom: 2rem;
}

.step-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--color-gray-900);
  margin-bottom: 0.5rem;
}

.step-description {
  font-size: 0.875rem;
  color: var(--color-gray-600);
}

.categories-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  gap: 1rem;
  margin-bottom: 2rem;
}

.category-card {
  padding: 1.5rem 1rem;
  border: 2px solid var(--color-gray-300);
  border-radius: 0.75rem;
  cursor: pointer;
  transition: all 0.2s ease;
  text-align: center;
}

.category-card:hover {
  border-color: var(--color-primary-500);
  background: rgba(139, 92, 246, 0.05);
}

.category-card.selected {
  border-color: var(--color-primary-500);
  background: rgba(139, 92, 246, 0.1);
}

.category-icon {
  font-size: 2.5rem;
  margin-bottom: 0.5rem;
}

.category-name {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-gray-800);
}

.step-actions {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  margin-top: 1.5rem;
  padding-top: 1.5rem;
  border-top: 1px solid var(--color-gray-300);
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
