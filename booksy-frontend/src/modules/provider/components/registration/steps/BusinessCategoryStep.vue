<template>
  <StepContainer
    :title="$t('provider.registration.category.title')"
    :subtitle="$t('provider.registration.category.subtitle')"
  >
    <!-- Main Categories Grid -->
    <div class="categories-grid">
      <div
        v-for="category in mainCategories"
        :key="category.id"
        class="category-card"
        :class="{ selected: modelValue === category.id }"
        @click="selectCategory(category.id)"
      >
        <div class="category-icon">
          <img :src="category.icon" :alt="category.name" />
        </div>
        <h3 class="category-name">{{ $t(`provider.categories.${category.id}`) }}</h3>
      </div>
    </div>

    <!-- Other Categories -->
    <div class="other-categories">
      <h4 class="other-categories-title">{{ $t('provider.registration.category.other') }}</h4>
      <div class="other-categories-list">
        <button
          v-for="category in otherCategories"
          :key="category.id"
          type="button"
          class="other-category-item"
          :class="{ selected: modelValue === category.id }"
          @click="selectCategory(category.id)"
        >
          <span>{{ $t(`provider.categories.${category.id}`) }}</span>
          <svg
            class="chevron-icon"
            viewBox="0 0 20 20"
            fill="currentColor"
            xmlns="http://www.w3.org/2000/svg"
          >
            <path
              fill-rule="evenodd"
              d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z"
              clip-rule="evenodd"
            />
          </svg>
        </button>
      </div>
    </div>

    <!-- Navigation -->
    <NavigationButtons
      :show-back="false"
      :can-continue="!!modelValue"
      @next="handleNext"
    />
  </StepContainer>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import StepContainer from '../shared/StepContainer.vue'
import NavigationButtons from '../shared/NavigationButtons.vue'
import type { BusinessCategoryId } from '@/modules/provider/types/registration.types'

interface Props {
  modelValue?: BusinessCategoryId | null
}

interface Emits {
  (e: 'update:modelValue', value: BusinessCategoryId): void
  (e: 'next'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

// Categories with icons - using placeholder paths (will need actual assets)
const mainCategories = computed(() => [
  { id: 'nail_salon', name: 'Nail Salon', icon: '/images/categories/nail-salon.jpg' },
  { id: 'hair_salon', name: 'Hair Salon', icon: '/images/categories/hair-salon.jpg' },
  { id: 'brows_lashes', name: 'Brows & Lashes', icon: '/images/categories/brows-lashes.jpg' },
  { id: 'braids_locs', name: 'Braids & Locs', icon: '/images/categories/braids-locs.jpg' },
  { id: 'massage', name: 'Massage', icon: '/images/categories/massage.jpg' },
  { id: 'barbershop', name: 'Barbershop', icon: '/images/categories/barbershop.jpg' },
])

const otherCategories = computed(() => [
  { id: 'aesthetic_medicine', name: 'Aesthetic Medicine' },
  { id: 'dental_orthodontics', name: 'Dental & Orthodontics' },
  { id: 'hair_removal', name: 'Hair Removal' },
  { id: 'health_fitness', name: 'Health & Fitness' },
  { id: 'home_services', name: 'Home Services' },
])

const selectCategory = (categoryId: string) => {
  emit('update:modelValue', categoryId as BusinessCategoryId)
}

const handleNext = () => {
  if (props.modelValue) {
    emit('next')
  }
}
</script>

<style scoped>
/* Categories Grid */
.categories-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1.5rem;
  margin-bottom: 2.5rem;
}

.category-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  padding: 1.5rem;
  background: #ffffff;
  border: 2px solid #e5e7eb;
  border-radius: 0.75rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.category-card:hover {
  border-color: #10b981;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.15);
}

.category-card.selected {
  border-color: #10b981;
  background-color: #f0fdf4;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.1);
}

.category-icon {
  width: 5rem;
  height: 5rem;
  border-radius: 50%;
  overflow: hidden;
  background-color: #f3f4f6;
  display: flex;
  align-items: center;
  justify-content: center;
}

.category-icon img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.category-name {
  font-size: 0.875rem;
  font-weight: 600;
  color: #111827;
  text-align: center;
  line-height: 1.3;
}

/* Other Categories */
.other-categories {
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 1px solid #e5e7eb;
}

.other-categories-title {
  font-size: 0.875rem;
  font-weight: 600;
  color: #6b7280;
  margin-bottom: 1rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.other-categories-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.other-category-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 1.25rem;
  background: #ffffff;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.2s ease;
  font-size: 0.9375rem;
  font-weight: 500;
  color: #111827;
  text-align: left;
}

.other-category-item:hover {
  border-color: #10b981;
  background-color: #f9fafb;
}

.other-category-item.selected {
  border-color: #10b981;
  background-color: #f0fdf4;
  color: #10b981;
}

.chevron-icon {
  width: 1.25rem;
  height: 1.25rem;
  color: #9ca3af;
  transition: color 0.2s ease;
}

.other-category-item:hover .chevron-icon,
.other-category-item.selected .chevron-icon {
  color: #10b981;
}

/* Responsive */
@media (max-width: 768px) {
  .categories-grid {
    grid-template-columns: repeat(2, 1fr);
    gap: 1rem;
  }

  .category-card {
    padding: 1.25rem;
  }

  .category-icon {
    width: 4rem;
    height: 4rem;
  }

  .category-name {
    font-size: 0.8125rem;
  }
}

@media (max-width: 480px) {
  .categories-grid {
    grid-template-columns: repeat(2, 1fr);
    gap: 0.75rem;
  }

  .category-card {
    padding: 1rem;
  }

  .category-icon {
    width: 3.5rem;
    height: 3.5rem;
  }

  .other-category-item {
    padding: 0.875rem 1rem;
    font-size: 0.875rem;
  }
}
</style>
