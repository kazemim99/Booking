<template>
  <div class="forbidden">
    <div class="forbidden-container">
      <div class="error-code">403</div>
      <h1 class="error-title">Access Forbidden</h1>
      <p class="error-message">
        {{ message || "You don't have permission to access this page." }}
      </p>

      <div class="error-icon">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
          />
        </svg>
      </div>

      <div class="error-actions">
        <button @click="goHome" class="btn btn-primary">Go to Home</button>
        <button @click="goBack" class="btn btn-secondary">Go Back</button>
      </div>

      <div class="info-box">
        <p>
          <strong>Need access?</strong><br />
          Contact your administrator if you believe this is a mistake.
        </p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router'
import { computed } from 'vue'

defineOptions({
  name: 'ForbiddenPage'
})

const router = useRouter()
const route = useRoute()

const message = computed(() => {
  return (route.params.message as string) || undefined
})

function goHome(): void {
  router.push('/')
}

function goBack(): void {
  router.back()
}
</script>

<style scoped lang="scss">
.forbidden {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
  padding: 2rem;
}

.forbidden-container {
  max-width: 600px;
  text-align: center;
  background: white;
  padding: 3rem;
  border-radius: 16px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}

.error-code {
  font-size: 8rem;
  font-weight: 900;
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  line-height: 1;
  margin-bottom: 1rem;
}

.error-title {
  font-size: 2rem;
  font-weight: 700;
  color: #1f2937;
  margin-bottom: 1rem;
}

.error-message {
  font-size: 1.125rem;
  color: #6b7280;
  margin-bottom: 2rem;
  line-height: 1.6;
}

.error-icon {
  width: 80px;
  height: 80px;
  margin: 0 auto 2rem;
  color: #f5576c;

  svg {
    width: 100%;
    height: 100%;
  }
}

.error-actions {
  display: flex;
  gap: 1rem;
  justify-content: center;
  margin-bottom: 2rem;
}

.btn {
  padding: 0.75rem 1.5rem;
  font-size: 1rem;
  font-weight: 600;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.3s ease;

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  }

  &:active {
    transform: translateY(0);
  }
}

.btn-primary {
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
  color: white;
}

.btn-secondary {
  background: #e5e7eb;
  color: #374151;

  &:hover {
    background: #d1d5db;
  }
}

.info-box {
  margin-top: 2rem;
  padding: 1.5rem;
  background: #fef2f2;
  border-radius: 8px;
  border-left: 4px solid #f5576c;

  p {
    margin: 0;
    color: #991b1b;
    font-size: 0.875rem;
    line-height: 1.5;

    strong {
      display: block;
      margin-bottom: 0.5rem;
      font-size: 1rem;
    }
  }
}

@media (max-width: 640px) {
  .forbidden-container {
    padding: 2rem 1.5rem;
  }

  .error-code {
    font-size: 5rem;
  }

  .error-title {
    font-size: 1.5rem;
  }

  .error-actions {
    flex-direction: column;
  }
}
</style>
