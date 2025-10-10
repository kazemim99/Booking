<template>
  <div class="registration-view" :dir="direction">
    <!-- Language Switcher -->
    <div class="language-switcher-container">
      <LanguageSwitcher />
    </div>

    <div class="registration-container">
      <!-- Branding -->
      <div class="branding">
        <div class="logo">
          <svg viewBox="0 0 40 40" fill="none" xmlns="http://www.w3.org/2000/svg">
            <rect width="40" height="40" rx="8" fill="#10b981" />
            <path
              d="M20 10C14.477 10 10 14.477 10 20C10 25.523 14.477 30 20 30C25.523 30 30 25.523 30 20C30 14.477 25.523 10 20 10ZM23 21H21V23C21 23.552 20.552 24 20 24C19.448 24 19 23.552 19 23V21H17C16.448 21 16 20.552 16 20C16 19.448 16.448 19 17 19H19V17C19 16.448 19.448 16 20 16C20.552 16 21 16.448 21 17V19H23C23.552 19 24 19.448 24 20C24 20.552 23.552 21 23 21Z"
              fill="white"
            />
          </svg>
        </div>
      </div>

      <!-- Registration Flow -->
      <div class="flow-container">
        <ProviderRegistrationFlow />
      </div>
    </div>

    <!-- Background Decoration -->
    <div class="background-decoration">
      <div class="decoration-circle decoration-circle-1"></div>
      <div class="decoration-circle decoration-circle-2"></div>
      <div class="decoration-circle decoration-circle-3"></div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import ProviderRegistrationFlow from './ProviderRegistrationFlow.vue'
import LanguageSwitcher from '@/shared/components/layout/Header/LanguageSwitcher.vue'
import { useRTL } from '@/core/composables/useRTL'

const { direction, initializeRTL } = useRTL()

onMounted(() => {
  initializeRTL()
})
</script>

<style scoped>
.registration-view {
  position: relative;
  min-height: 100vh;
  display: flex;
  align-items: flex-start;
  justify-content: center;
  background: linear-gradient(135deg, #f0fdf4 0%, #ffffff 50%, #f0fdf4 100%);
  overflow-y: auto;
  overflow-x: hidden;
  padding: 2rem 0;
}

/* Language Switcher */
.language-switcher-container {
  position: fixed;
  top: 1.5rem;
  right: 1.5rem;
  z-index: 100;
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(10px);
  border-radius: 0.75rem;
  padding: 0.5rem;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
}

[dir='rtl'] .language-switcher-container {
  right: auto;
  left: 1.5rem;
}

.registration-container {
  position: relative;
  z-index: 10;
  width: 100%;
  max-width: 64rem;
  padding: 0 1rem;
  box-sizing: border-box;
}

/* Branding */
.branding {
  text-align: center;
  margin-bottom: 2rem;
  margin-top: 1rem;
}

.logo {
  display: inline-block;
  margin-bottom: 1rem;
  animation: fadeInDown 0.5s ease;
}

.logo svg {
  width: 4rem;
  height: 4rem;
  filter: drop-shadow(0 4px 6px rgba(16, 185, 129, 0.2));
}

.brand-name {
  font-size: 2rem;
  font-weight: 800;
  color: #111827;
  margin-bottom: 0.5rem;
  animation: fadeInDown 0.5s ease 0.1s both;
}

.brand-tagline {
  font-size: 1rem;
  color: #6b7280;
  animation: fadeInDown 0.5s ease 0.2s both;
}

/* Flow Container */
.flow-container {
  background: #ffffff;
  border-radius: 1rem;
  padding: 2rem 1.5rem;
  box-shadow:
    0 20px 25px -5px rgba(0, 0, 0, 0.1),
    0 10px 10px -5px rgba(0, 0, 0, 0.04);
  animation: fadeInUp 0.5s ease 0.3s both;
  width: 100%;
  box-sizing: border-box;
  overflow-x: hidden;
}

/* Background Decoration */
.background-decoration {
  position: absolute;
  inset: 0;
  z-index: 0;
  overflow: hidden;
  pointer-events: none;
}

.decoration-circle {
  position: absolute;
  border-radius: 50%;
  opacity: 0.08;
  animation: float 20s ease-in-out infinite;
}

.decoration-circle-1 {
  width: 20rem;
  height: 20rem;
  background: linear-gradient(135deg, #10b981, #34d399);
  top: -5rem;
  right: -5rem;
  animation-delay: 0s;
}

.decoration-circle-2 {
  width: 15rem;
  height: 15rem;
  background: linear-gradient(135deg, #10b981, #6ee7b7);
  bottom: -3rem;
  left: -3rem;
  animation-delay: -10s;
}

.decoration-circle-3 {
  width: 12rem;
  height: 12rem;
  background: linear-gradient(135deg, #34d399, #a7f3d0);
  top: 50%;
  right: 10%;
  animation-delay: -5s;
}

/* Animations */
@keyframes fadeInDown {
  from {
    opacity: 0;
    transform: translateY(-1rem);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(1rem);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes float {
  0%,
  100% {
    transform: translateY(0) rotate(0deg);
  }
  50% {
    transform: translateY(-2rem) rotate(180deg);
  }
}

/* Responsive */
@media (max-width: 768px) {
  .registration-view {
    padding: 1rem 0;
  }

  .language-switcher-container {
    top: 1rem;
    right: 1rem;
    padding: 0.375rem;
  }

  [dir='rtl'] .language-switcher-container {
    right: auto;
    left: 1rem;
  }

  .registration-container {
    padding: 0 0.5rem;
  }

  .branding {
    margin-bottom: 1.5rem;
    margin-top: 0.5rem;
  }

  .flow-container {
    padding: 1.5rem 1rem;
  }

  .brand-name {
    font-size: 1.5rem;
  }

  .brand-tagline {
    font-size: 0.875rem;
  }

  .decoration-circle-1 {
    width: 15rem;
    height: 15rem;
  }

  .decoration-circle-2 {
    width: 12rem;
    height: 12rem;
  }

  .decoration-circle-3 {
    display: none;
  }
}
</style>
