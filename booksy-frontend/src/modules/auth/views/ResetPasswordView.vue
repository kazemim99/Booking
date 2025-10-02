<template>
  <div class="auth-page">
    <div class="auth-container">
      <div class="auth-card">
        <ResetPasswordForm />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import ResetPasswordForm from '../components/ResetPasswordForm.vue'
</script>

<style scoped lang="scss">
.auth-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 2rem;
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
