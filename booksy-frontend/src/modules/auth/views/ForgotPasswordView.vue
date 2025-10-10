<template>
  <div class="auth-page">
    <!-- Floating Language Switcher -->
    <div class="floating-language-switcher">
      <LanguageSwitcher />
    </div>

    <div class="auth-container">
      <div class="auth-card">
        <ForgotPasswordForm />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import ForgotPasswordForm from '../components/ForgotPasswordForm.vue'
import LanguageSwitcher from '@/shared/components/layout/Header/LanguageSwitcher.vue'
</script>

<style scoped lang="scss">
.auth-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 2rem;
  position: relative;
}

.floating-language-switcher {
  position: absolute;
  top: 1.5rem;
  right: 1.5rem;
  z-index: 1000;

  :deep(.language-button) {
    background: rgba(255, 255, 255, 0.2);
    backdrop-filter: blur(10px);
    color: white;
    border: 1px solid rgba(255, 255, 255, 0.3);

    &:hover {
      background: rgba(255, 255, 255, 0.3);
      color: white;
    }
  }

  :deep(.dropdown-arrow),
  :deep(.globe-icon) {
    color: white;
  }

  @media (max-width: 640px) {
    top: 1rem;
    right: 1rem;
  }
}

.auth-container {
  width: 100%;
  max-width: 500px;
}

.auth-card {
  background: white;
  border-radius: 16px;
  padding: 3rem;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);

  @media (max-width: 640px) {
    padding: 2rem 1.5rem;
  }
}
</style>
