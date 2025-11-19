<template>
  <section class="category-section" dir="rtl">
    <div class="container">
      <div class="section-header">
        <h2 class="section-title">دسته‌بندی‌های محبوب</h2>
        <p class="section-subtitle">
          بهترین خدمات از سالون‌های زیبایی، اسپاها، مراکز سلامتی و بیشتر را کشف کنید
        </p>
      </div>

      <div class="category-grid">
        <div
          v-for="category in categories"
          :key="category.slug"
          class="category-card"
          @click="navigateToCategory(category.slug)"
        >
          <div class="category-icon" :style="{ background: category.gradient }">
            <span class="icon">{{ category.icon }}</span>
          </div>
          <h3 class="category-name">{{ category.name }}</h3>
          <p class="category-count">{{ formatProviderCount(category.providerCount) }}+ ارائه‌دهنده</p>
          <div class="category-arrow">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { categoryService } from '@/core/api/services/category.service'
import type { PopularCategory } from '@/core/api/services/category.service'

const router = useRouter()

const categories = ref<PopularCategory[]>([])

// Load popular categories on mount
onMounted(async () => {
  try {
    const data = await categoryService.getPopularCategories(8)
    categories.value = data
  } catch (error) {
    console.error('Error loading categories:', error)
  }
})

// Helper function to convert Western numbers to Persian
const toPersianNumber = (num: number): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return num.toString().replace(/\d/g, (digit) => persianDigits[parseInt(digit)])
}

// Format provider count
const formatProviderCount = (count: number): string => {
  if (count >= 1000) {
    const thousands = Math.floor(count / 1000)
    const remainder = count % 1000
    if (remainder === 0) {
      return `${toPersianNumber(thousands)},۰۰۰`
    }
    return `${toPersianNumber(thousands)},${toPersianNumber(remainder)}`
  }
  return toPersianNumber(count)
}

const navigateToCategory = (slug: string) => {
  router.push({
    path: '/providers/search',
    query: {
      serviceCategory: slug,
      pageNumber: 1,
      pageSize: 12,
    }
  })
}
</script>

<style scoped>
.category-section {
  padding: 5rem 0;
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
}

.container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 2rem;
}

.section-header {
  text-align: center;
  margin-bottom: 4rem;
}

.section-title {
  font-size: 2.5rem;
  font-weight: 800;
  color: #1e293b;
  margin: 0 0 1rem 0;
}

.section-subtitle {
  font-size: 1.125rem;
  color: #64748b;
  max-width: 600px;
  margin: 0 auto;
  line-height: 1.6;
}

.category-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
  gap: 2rem;
}

.category-card {
  position: relative;
  background: white;
  border-radius: 20px;
  padding: 2rem;
  cursor: pointer;
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  border: 2px solid transparent;
  overflow: hidden;
}

.category-card::before {
  content: '';
  position: absolute;
  inset: 0;
  border-radius: 20px;
  padding: 2px;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.2), rgba(118, 75, 162, 0.2));
  -webkit-mask: linear-gradient(#fff 0 0) content-box, linear-gradient(#fff 0 0);
  -webkit-mask-composite: xor;
  mask-composite: exclude;
  opacity: 0;
  transition: opacity 0.4s;
}

.category-card:hover {
  transform: translateY(-8px);
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
}

.category-card:hover::before {
  opacity: 1;
}

.category-icon {
  width: 80px;
  height: 80px;
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 1.5rem;
  transition: transform 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}

.category-card:hover .category-icon {
  transform: scale(1.1) rotate(5deg);
}

.icon {
  font-size: 2.5rem;
  filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.1));
}

.category-name {
  font-size: 1.25rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.5rem 0;
}

.category-count {
  font-size: 0.875rem;
  color: #64748b;
  margin: 0;
}

.category-arrow {
  position: absolute;
  top: 2rem;
  left: 2rem;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f1f5f9;
  border-radius: 50%;
  opacity: 0;
  transform: translateX(10px);
  transition: all 0.3s;
}

.category-card:hover .category-arrow {
  opacity: 1;
  transform: translateX(0);
}

.category-arrow svg {
  width: 16px;
  height: 16px;
  color: #667eea;
}

/* Animation on scroll */
.category-card {
  animation: fadeInUp 0.6s ease-out;
  animation-fill-mode: both;
}

.category-card:nth-child(1) { animation-delay: 0.1s; }
.category-card:nth-child(2) { animation-delay: 0.2s; }
.category-card:nth-child(3) { animation-delay: 0.3s; }
.category-card:nth-child(4) { animation-delay: 0.4s; }
.category-card:nth-child(5) { animation-delay: 0.5s; }
.category-card:nth-child(6) { animation-delay: 0.6s; }
.category-card:nth-child(7) { animation-delay: 0.7s; }
.category-card:nth-child(8) { animation-delay: 0.8s; }

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(30px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Responsive */
@media (max-width: 768px) {
  .category-section {
    padding: 3rem 0;
  }

  .section-title {
    font-size: 2rem;
  }

  .category-grid {
    grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
    gap: 1rem;
  }

  .category-card {
    padding: 1.5rem 1rem;
  }

  .category-icon {
    width: 60px;
    height: 60px;
    margin-bottom: 1rem;
  }

  .icon {
    font-size: 2rem;
  }

  .category-name {
    font-size: 1rem;
  }

  .category-arrow {
    display: none;
  }
}
</style>
