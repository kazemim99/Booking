<template>
  <section class="how-it-works-section" dir="rtl">
    <div class="container">
      <div class="section-header">
        <h2 class="section-title">چگونه کار می‌کند</h2>
        <p class="section-subtitle">
          تجربه عالی زیبایی و سلامتی خود را در ۳ مرحله ساده رزرو کنید
        </p>
      </div>

      <div class="steps-container">
        <div
          v-for="(step, index) in steps"
          :key="step.id"
          class="step-card"
          :style="{ animationDelay: `${index * 0.2}s` }"
        >
          <div class="step-number-wrapper">
            <div class="step-number" :style="{ background: step.color }">
              {{ step.id }}
            </div>
            <div v-if="index < steps.length - 1" class="step-connector"></div>
          </div>

          <div class="step-icon" :style="{ background: step.gradient }">
            <span>{{ step.icon }}</span>
          </div>

          <h3 class="step-title">{{ step.title }}</h3>
          <p class="step-description">{{ step.description }}</p>

          <div class="step-features">
            <div
              v-for="feature in step.features"
              :key="feature"
              class="feature-item"
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M19.916 4.626a.75.75 0 01.208 1.04l-9 13.5a.75.75 0 01-1.154.114l-6-6a.75.75 0 011.06-1.06l5.353 5.353 8.493-12.739a.75.75 0 011.04-.208z" clip-rule="evenodd" />
              </svg>
              {{ feature }}
            </div>
          </div>
        </div>
      </div>

      <div class="cta-section">
        <h3 class="cta-title">آماده شروع هستید؟</h3>
        <button class="cta-btn" @click="getStarted">
          ارائه‌دهنده خود را پیدا کنید
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 17l-5-5m0 0l5-5m-5 5h12" />
          </svg>
        </button>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'

const router = useRouter()

const steps = [
  {
    id: '۱',
    title: 'جستجو و کشف',
    description: 'هزاران متخصص تاییدشده زیبایی و سلامتی را در منطقه خود مرور کنید',
    icon: '🔍',
    color: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
    gradient: 'linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%)',
    features: [
      'فیلتر بر اساس نوع خدمات',
      'بررسی امتیازات و نظرات',
      'مشاهده دسترسی آنلاین',
    ],
  },
  {
    id: '۲',
    title: 'رزرو فوری',
    description: 'زمان دلخواه خود را انتخاب کنید و نوبت خود را در چند ثانیه رزرو کنید',
    icon: '📅',
    color: 'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
    gradient: 'linear-gradient(135deg, rgba(240, 147, 251, 0.1) 0%, rgba(245, 87, 108, 0.1) 100%)',
    features: [
      'تایید فوری',
      'پرداخت امن آنلاین',
      'همگام‌سازی با تقویم',
    ],
  },
  {
    id: '۳',
    title: 'لذت ببرید',
    description: 'در قرار ملاقات حاضر شوید و از خدمات لذت ببرید. نظر خود را برای کمک به دیگران بنویسید',
    icon: '✨',
    color: 'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
    gradient: 'linear-gradient(135deg, rgba(79, 172, 254, 0.1) 0%, rgba(0, 242, 254, 0.1) 100%)',
    features: [
      'تغییر زمان آسان',
      'رسید دیجیتال',
      'جوایز وفاداری',
    ],
  },
]

const getStarted = () => {
  router.push('/providers/search')
}
</script>

<style scoped>
.how-it-works-section {
  padding: 5rem 0;
  background: linear-gradient(180deg, #f8fafc 0%, #ffffff 100%);
  position: relative;
  overflow: hidden;
}

.how-it-works-section::before {
  content: '';
  position: absolute;
  top: 0;
  left: 50%;
  transform: translateX(-50%);
  width: 200%;
  height: 100%;
  background: radial-gradient(circle at 50% 0%, rgba(102, 126, 234, 0.05) 0%, transparent 50%);
  pointer-events: none;
}

.container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 2rem;
  position: relative;
  z-index: 1;
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
}

.steps-container {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 3rem;
  margin-bottom: 4rem;
}

.step-card {
  position: relative;
  background: white;
  border-radius: 24px;
  padding: 2.5rem 2rem;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.06);
  animation: fadeInUp 0.8s ease-out;
  animation-fill-mode: both;
  transition: all 0.4s;
}

.step-card:hover {
  transform: translateY(-8px);
  box-shadow: 0 12px 40px rgba(0, 0, 0, 0.12);
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(40px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.step-number-wrapper {
  position: absolute;
  top: -20px;
  left: 50%;
  transform: translateX(-50%);
  display: flex;
  align-items: center;
  gap: 1rem;
}

.step-number {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-weight: 800;
  font-size: 1.125rem;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.step-connector {
  width: 60px;
  height: 2px;
  background: linear-gradient(90deg, #667eea 0%, transparent 100%);
  position: relative;
  top: 0;
}

.step-icon {
  width: 80px;
  height: 80px;
  border-radius: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 2rem auto 1.5rem;
  font-size: 2.5rem;
  transition: transform 0.4s;
}

.step-card:hover .step-icon {
  transform: scale(1.1) rotate(5deg);
}

.step-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 0.75rem 0;
  text-align: center;
}

.step-description {
  font-size: 1rem;
  color: #64748b;
  line-height: 1.6;
  margin: 0 0 1.5rem 0;
  text-align: center;
}

.step-features {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.feature-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #475569;
}

.feature-item svg {
  width: 16px;
  height: 16px;
  color: #10b981;
  flex-shrink: 0;
}

.cta-section {
  text-align: center;
  padding: 3rem 2rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 24px;
  position: relative;
  overflow: hidden;
}

.cta-section::before {
  content: '';
  position: absolute;
  inset: 0;
  background: url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='0.05'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E");
}

.cta-title {
  font-size: 2rem;
  font-weight: 800;
  color: white;
  margin: 0 0 1.5rem 0;
  position: relative;
  z-index: 1;
}

.cta-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem 2.5rem;
  background: white;
  color: #667eea;
  border: none;
  border-radius: 12px;
  font-size: 1.125rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.3s;
  position: relative;
  z-index: 1;
}

.cta-btn svg {
  width: 20px;
  height: 20px;
  transition: transform 0.3s;
}

.cta-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 12px 30px rgba(0, 0, 0, 0.2);
}

.cta-btn:hover svg {
  transform: translateX(-4px);
}

/* Responsive */
@media (max-width: 1024px) {
  .steps-container {
    grid-template-columns: 1fr;
    gap: 4rem;
  }

  .step-connector {
    display: none;
  }
}

@media (max-width: 768px) {
  .how-it-works-section {
    padding: 3rem 0;
  }

  .section-title {
    font-size: 2rem;
  }

  .step-card {
    padding: 2rem 1.5rem;
  }

  .cta-title {
    font-size: 1.5rem;
  }
}
</style>
