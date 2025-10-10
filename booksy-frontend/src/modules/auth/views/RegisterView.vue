<template>
  <div class="auth-page">
    <!-- Floating Language Switcher -->
    <div class="floating-language-switcher">
      <LanguageSwitcher />
    </div>

    <div class="auth-container">
      <div class="auth-card">
        <RegisterForm />
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
import RegisterForm from '../components/RegisterForm.vue'
import LanguageSwitcher from '@/shared/components/layout/Header/LanguageSwitcher.vue'
</script>

<style scoped lang="scss">
.auth-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #f5f3ff 0%, #ffffff 50%, #faf5ff 100%);
  padding: 2rem;
  position: relative;
  overflow: hidden;
}

.floating-language-switcher {
  position: absolute;
  top: 1.5rem;
  right: 1.5rem;
  z-index: 1000;

  :deep(.language-button) {
    background: rgba(139, 92, 246, 0.1);
    backdrop-filter: blur(10px);
    color: #8b5cf6;
    border: 1px solid rgba(139, 92, 246, 0.2);

    &:hover {
      background: rgba(139, 92, 246, 0.15);
      color: #7c3aed;
    }
  }

  :deep(.dropdown-arrow),
  :deep(.globe-icon) {
    color: #8b5cf6;
  }

  @media (max-width: 640px) {
    top: 1rem;
    right: 1rem;
  }
}

.auth-container {
  width: 100%;
  max-width: 600px;
  z-index: 10;
  position: relative;
  animation: fadeInUp 0.5s ease 0.3s both;
}

.auth-card {
  background: white;
  border-radius: 16px;
  padding: 3rem;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);

  @media (max-width: 640px) {
    padding: 2rem 1.5rem;
  }
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
  opacity: 0.1;
  animation: float 20s ease-in-out infinite;
}

.decoration-circle-1 {
  width: 20rem;
  height: 20rem;
  background: linear-gradient(135deg, #8b5cf6, #a78bfa);
  top: -5rem;
  right: -5rem;
  animation-delay: 0s;
}

.decoration-circle-2 {
  width: 15rem;
  height: 15rem;
  background: linear-gradient(135deg, #ec4899, #f472b6);
  bottom: -3rem;
  left: -3rem;
  animation-delay: -10s;
}

.decoration-circle-3 {
  width: 12rem;
  height: 12rem;
  background: linear-gradient(135deg, #10b981, #34d399);
  top: 50%;
  right: 10%;
  animation-delay: -5s;
}

/* Animations */
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

@media (max-width: 640px) {
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
