<template>
  <header class="simple-header">
    <div class="header-container">
      <!-- Back Button -->
      <button
        v-if="showBackButton"
        class="back-btn"
        @click="goBack"
        :title="backButtonTitle"
      >
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
        <span v-if="!isMobile">{{ backButtonText }}</span>
      </button>

      <!-- Logo -->
      <router-link to="/" class="logo">
        <img src="@/assets/logo.svg" alt="Booksy" />
        <span class="logo-text">Booksy</span>
      </router-link>

      <!-- Spacer -->
      <div class="header-spacer"></div>

      <!-- Header Actions -->
      <div class="header-actions">
        <!-- Optional: Breadcrumbs slot -->
        <slot name="breadcrumbs"></slot>

        <!-- User Menu -->
        <UserMenu />
      </div>
    </div>
  </header>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import UserMenu from './UserMenu.vue'

interface Props {
  showBackButton?: boolean
  backButtonText?: string
  backButtonTitle?: string
}

const props = withDefaults(defineProps<Props>(), {
  showBackButton: true,
  backButtonText: 'بازگشت',
  backButtonTitle: 'بازگشت به صفحه قبل',
})

const router = useRouter()

const isMobile = computed(() => window.innerWidth < 768)

const goBack = () => {
  if (window.history.length > 1) {
    router.go(-1)
  } else {
    router.push('/')
  }
}
</script>

<style scoped>
.simple-header {
  position: sticky;
  top: 0;
  z-index: 1000;
  background: white;
  border-bottom: 1px solid #e5e7eb;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.header-container {
  max-width: 1920px;
  margin: 0 auto;
  padding: 1rem 2rem;
  display: flex;
  align-items: center;
  gap: 1.5rem;
}

/* Back Button */
.back-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  background: transparent;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  color: #374151;
  font-size: 0.875rem;
  font-weight: 500;
  font-family: 'Vazir', 'IRANSans', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
}

.back-btn svg {
  width: 20px;
  height: 20px;
  flex-shrink: 0;
}

.back-btn:hover {
  background: #f9fafb;
  border-color: #8b5cf6;
  color: #8b5cf6;
}

.back-btn:active {
  transform: scale(0.98);
}

/* Logo */
.logo {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  text-decoration: none;
  transition: opacity 0.2s;
}

.logo:hover {
  opacity: 0.8;
}

.logo img {
  height: 36px;
  width: auto;
}

.logo-text {
  font-size: 1.5rem;
  font-weight: 700;
  background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  font-family: 'Vazir', 'IRANSans', sans-serif;
}

/* Spacer */
.header-spacer {
  flex: 1;
}

/* Header Actions */
.header-actions {
  display: flex;
  align-items: center;
  gap: 1rem;
}

/* Mobile Responsive */
@media (max-width: 768px) {
  .header-container {
    padding: 0.75rem 1rem;
    gap: 1rem;
  }

  .back-btn {
    padding: 0.5rem;
    border: none;
  }

  .back-btn span {
    display: none;
  }

  .logo img {
    height: 28px;
  }

  .logo-text {
    font-size: 1.25rem;
  }

  .header-actions {
    gap: 0.5rem;
  }
}

@media (max-width: 480px) {
  .logo-text {
    display: none;
  }
}

/* RTL Support */
[dir="rtl"] .back-btn svg {
  transform: scaleX(-1);
}
</style>
