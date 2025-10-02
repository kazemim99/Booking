<template>
  <div class="server-error">
    <div class="server-error-container">
      <div class="error-code">500</div>
      <h1 class="error-title">Server Error</h1>
      <p class="error-message">
        {{ message || "Something went wrong on our end. We're working to fix it." }}
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
            d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
          />
        </svg>
      </div>

      <div class="error-actions">
        <button @click="retry" class="btn btn-primary">Try Again</button>
        <button @click="goHome" class="btn btn-secondary">Go to Home</button>
      </div>

      <div class="error-details" v-if="showDetails && errorDetails">
        <button @click="toggleDetails" class="details-toggle">
          {{ detailsExpanded ? 'Hide Details' : 'Show Details' }}
        </button>
        <pre v-if="detailsExpanded" class="details-content">{{ errorDetails }}</pre>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

interface Props {
  message?: string
  errorDetails?: string
  showDetails?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  showDetails: false,
})
console.log(props)
const router = useRouter()
const detailsExpanded = ref(false)

function retry(): void {
  window.location.reload()
}

function goHome(): void {
  router.push('/')
}

function toggleDetails(): void {
  detailsExpanded.value = !detailsExpanded.value
}
</script>

<style scoped lang="scss">
.server-error {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #fa709a 0%, #fee140 100%);
  padding: 2rem;
}

.server-error-container {
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
  background: linear-gradient(135deg, #fa709a 0%, #fee140 100%);
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
  color: #fa709a;

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
  background: linear-gradient(135deg, #fa709a 0%, #fee140 100%);
  color: white;
}

.btn-secondary {
  background: #e5e7eb;
  color: #374151;

  &:hover {
    background: #d1d5db;
  }
}

.error-details {
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 1px solid #e5e7eb;
}

.details-toggle {
  background: none;
  border: none;
  color: #667eea;
  cursor: pointer;
  font-size: 0.875rem;
  font-weight: 600;
  text-decoration: underline;

  &:hover {
    color: #764ba2;
  }
}

.details-content {
  margin-top: 1rem;
  padding: 1rem;
  background: #f9fafb;
  border-radius: 8px;
  text-align: left;
  font-size: 0.75rem;
  overflow-x: auto;
  max-height: 200px;
  overflow-y: auto;
}

@media (max-width: 640px) {
  .server-error-container {
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
