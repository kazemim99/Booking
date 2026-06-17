<template>
  <div class="not-found">
    <div class="not-found-container">
      <div class="error-code">404</div>
      <h1 class="error-title">Page Not Found</h1>
      <p class="error-message">
        {{ message || "The page you're looking for doesn't exist or has been moved." }}
      </p>

      <div class="error-actions">
        <button @click="goHome" class="btn btn-primary">Go to Home</button>
        <button @click="goBack" class="btn btn-secondary">Go Back</button>
      </div>

      <div class="helpful-links">
        <h3>Maybe you'd like to visit:</h3>
        <ul>
          <li><router-link to="/">Home</router-link></li>
          <li><router-link to="/bookings">My Bookings</router-link></li>
          <li><router-link to="/profile">My Profile</router-link></li>
        </ul>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'

interface Props {
  message?: string
}

defineProps<Props>()

const router = useRouter()

function goHome(): void {
  router.push('/')
}

function goBack(): void {
  router.back()
}
</script>

<style scoped lang="scss">
.not-found {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: linear-gradient(135deg, var(--color-primary-500) 0%, var(--color-primary-700) 100%);
  padding: 2rem;
}

.not-found-container {
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
  background: linear-gradient(135deg, var(--color-primary-500) 0%, var(--color-primary-700) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  line-height: 1;
  margin-bottom: 1rem;
}

.error-title {
  font-size: 2rem;
  font-weight: 700;
  color: var(--color-gray-900);
  margin-bottom: 1rem;
}

.error-message {
  font-size: 1.125rem;
  color: var(--color-gray-600);
  margin-bottom: 2rem;
  line-height: 1.6;
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
  background: linear-gradient(135deg, var(--color-primary-500) 0%, var(--color-primary-700) 100%);
  color: white;
}

.btn-secondary {
  background: var(--color-gray-300);
  color: var(--color-gray-800);

  &:hover {
    background: var(--color-gray-400);
  }
}

.helpful-links {
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 1px solid var(--color-gray-300);

  h3 {
    font-size: 1rem;
    color: var(--color-gray-600);
    margin-bottom: 1rem;
    font-weight: 600;
  }

  ul {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    gap: 1.5rem;
    justify-content: center;
    flex-wrap: wrap;
  }

  li {
    margin: 0;
  }

  a {
    color: var(--color-primary-500);
    text-decoration: none;
    font-weight: 500;
    transition: color 0.2s;

    &:hover {
      color: var(--color-primary-700);
      text-decoration: underline;
    }
  }
}

@media (max-width: 640px) {
  .not-found-container {
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

  .helpful-links ul {
    flex-direction: column;
    gap: 0.75rem;
  }
}
</style>
