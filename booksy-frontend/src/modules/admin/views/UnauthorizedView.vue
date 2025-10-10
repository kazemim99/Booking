<template>
  <div class="unauthorized-container">
    <div class="unauthorized-content">
      <div class="icon">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
          />
        </svg>
      </div>

      <h1 class="title">Access Denied</h1>
      <p class="message">You don't have permission to access this page.</p>

      <div class="actions">
        <button @click="goToDashboard" class="btn btn-primary">Go to Dashboard</button>
        <button @click="goBack" class="btn btn-secondary">Go Back</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/core/stores/modules/auth.store'

const router = useRouter()
const authStore = useAuthStore()

function goToDashboard() {
  authStore.redirectToDashboard()
}

function goBack() {
  router.back()
}
</script>

<style scoped>
.unauthorized-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--color-background);
  padding: 2rem;
}

.unauthorized-content {
  text-align: center;
  max-width: 500px;
}

.icon {
  width: 80px;
  height: 80px;
  margin: 0 auto 2rem;
  color: var(--color-warning);
}

.icon svg {
  width: 100%;
  height: 100%;
}

.title {
  font-size: 2rem;
  font-weight: 700;
  color: var(--color-text-primary);
  margin: 0 0 1rem;
}

.message {
  font-size: 1.125rem;
  color: var(--color-text-secondary);
  margin: 0 0 2rem;
}

.actions {
  display: flex;
  gap: 1rem;
  justify-content: center;
}

.btn {
  padding: 0.75rem 2rem;
  border-radius: 0.5rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  border: none;
}

.btn-primary {
  background: var(--color-primary);
  color: white;
}

.btn-primary:hover {
  background: var(--color-primary-dark);
}

.btn-secondary {
  background: white;
  color: var(--color-text-primary);
  border: 1px solid var(--color-border);
}

.btn-secondary:hover {
  background: var(--color-background);
}
</style>
