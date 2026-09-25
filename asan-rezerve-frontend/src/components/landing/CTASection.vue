<template>
  <section class="cta-section" dir="rtl">
    <div class="container">
      <!-- Customer CTA -->
      <div class="cta-content">
        <h2 class="cta-title">آماده تغییر ظاهر خود هستید؟</h2>
        <p class="cta-subtitle">
          به هزاران مشتری راضی بپیوندید که تجربه‌های عالی زیبایی و سلامتی خود را با ما رزرو می‌کنند
        </p>
        <div class="cta-buttons">
          <button class="cta-btn primary" @click="getStarted">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
            ارائه‌دهندگان نزدیک خود را پیدا کنید
          </button>
          <button class="cta-btn secondary" @click="learnMore">
            بیشتر بدانید
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
          </button>
        </div>
      </div>

      <!-- Provider CTA Card -->
      <div class="provider-cta-card">
        <div class="provider-cta-icon">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
          </svg>
        </div>
        <h3 class="provider-cta-card-title">صاحب کسب‌وکار هستید؟</h3>
        <p class="provider-cta-card-text">
          کسب‌وکار خود را با Booksy رشد دهید. به هزاران ارائه‌دهنده موفق بپیوندید
        </p>
        <button class="cta-btn provider" @click="goToProviderLogin">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
          </svg>
          شروع کنید - رایگان
        </button>
        <ul class="provider-benefits">
          <li>
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
            </svg>
            مدیریت آسان نوبت‌ها
          </li>
          <li>
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
            </svg>
            افزایش مشتریان جدید
          </li>
          <li>
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
            </svg>
            گزارش‌های تحلیلی دقیق
          </li>
        </ul>
      </div>
    </div>

    <div class="cta-pattern"></div>
  </section>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'

const router = useRouter()

const getStarted = () => {
  router.push('/providers/search')
}

const learnMore = () => {
  // Scroll to how it works section
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

const goToProviderLogin = () => {
  router.push('/provider/login')
}
</script>

<style scoped>
.cta-section {
  position: relative;
  padding: 6rem 0;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  overflow: hidden;

  @media (max-width: 768px) {
    padding: 4rem 0;
  }
}

.cta-pattern {
  position: absolute;
  inset: 0;
  background-image: url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='0.05'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E");
  opacity: 0.4;
}

.container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 2rem;
  position: relative;
  z-index: 1;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 3rem;
  align-items: center;

  @media (max-width: 968px) {
    grid-template-columns: 1fr;
    gap: 3rem;
  }

  @media (max-width: 768px) {
    padding: 0 1.5rem;
  }
}

.cta-content {
  text-align: center;
  max-width: 600px;

  @media (max-width: 968px) {
    max-width: 100%;
  }
}

.cta-title {
  font-size: clamp(2rem, 5vw, 3rem);
  font-weight: 800;
  color: white;
  margin: 0 0 1rem 0;
  line-height: 1.2;
}

.cta-subtitle {
  font-size: 1.25rem;
  color: rgba(255, 255, 255, 0.95);
  margin: 0 0 2.5rem 0;
  line-height: 1.6;
}

.cta-buttons {
  display: flex;
  gap: 1rem;
  justify-content: center;
  flex-wrap: wrap;
}

.cta-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem 2rem;
  border: none;
  border-radius: 12px;
  font-size: 1.125rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.3s;
  white-space: nowrap;
}

.cta-btn svg {
  width: 20px;
  height: 20px;
}

.cta-btn.primary {
  background: white;
  color: #667eea;
}

.cta-btn.primary:hover {
  transform: translateY(-3px);
  box-shadow: 0 12px 30px rgba(0, 0, 0, 0.3);
}

.cta-btn.secondary {
  background: rgba(255, 255, 255, 0.1);
  color: white;
  border: 2px solid rgba(255, 255, 255, 0.3);
  backdrop-filter: blur(10px);
}

.cta-btn.secondary:hover {
  background: rgba(255, 255, 255, 0.2);
  border-color: rgba(255, 255, 255, 0.5);
  transform: translateY(-3px);
}

/* Responsive */
@media (max-width: 768px) {
  .cta-section {
    padding: 4rem 0;
  }

  .cta-buttons {
    flex-direction: column;
    align-items: stretch;
  }

  .cta-btn {
    justify-content: center;
  }
}

/* Provider CTA Card */
.provider-cta-card {
  background: white;
  border-radius: 20px;
  padding: 2.5rem;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  text-align: center;
  max-width: 500px;
  animation: slideUp 0.6s ease-out 0.2s both;

  @keyframes slideUp {
    from {
      opacity: 0;
      transform: translateY(30px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }

  @media (max-width: 968px) {
    max-width: 100%;
  }

  @media (max-width: 768px) {
    padding: 2rem 1.5rem;
  }
}

.provider-cta-icon {
  width: 64px;
  height: 64px;
  margin: 0 auto 1.5rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 8px 24px rgba(102, 126, 234, 0.3);

  svg {
    width: 32px;
    height: 32px;
    color: white;
  }
}

.provider-cta-card-title {
  font-size: 1.75rem;
  font-weight: 800;
  color: #1f2937;
  margin: 0 0 1rem 0;
  line-height: 1.3;

  @media (max-width: 768px) {
    font-size: 1.5rem;
  }
}

.provider-cta-card-text {
  font-size: 1rem;
  color: #6b7280;
  line-height: 1.6;
  margin: 0 0 2rem 0;
}

.cta-btn.provider {
  width: 100%;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  margin-bottom: 1.5rem;

  &:hover {
    transform: translateY(-3px);
    box-shadow: 0 16px 40px rgba(102, 126, 234, 0.4);
  }
}

.provider-benefits {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;

  li {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    font-size: 0.9375rem;
    color: #4b5563;
    text-align: right;

    svg {
      width: 20px;
      height: 20px;
      color: #10b981;
      flex-shrink: 0;
    }
  }
}
</style>
