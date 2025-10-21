<template>
  <div class="welcome-card">
    <div class="welcome-card-background">
      <!-- Decorative gradient background -->
      <div class="gradient-orb orb-1"></div>
      <div class="gradient-orb orb-2"></div>
    </div>

    <div class="welcome-card-content">
      <!-- Provider Avatar and Info -->
      <div class="provider-info">
        <div class="provider-avatar">
          <img
            v-if="provider.profile.logoUrl"
            :src="provider.profile.logoUrl"
            :alt="provider.profile.businessName"
            class="avatar-image"
          />
          <div v-else class="avatar-placeholder">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
              />
            </svg>
          </div>
        </div>

        <div class="provider-details">
          <h1 class="welcome-title">
            {{ welcomeMessage }}
            <span class="business-name">{{ provider.profile.businessName }}</span>
          </h1>
          <p class="welcome-subtitle">{{ getCurrentTimeGreeting() }}</p>
        </div>
      </div>

      <!-- Onboarding Progress (if incomplete) -->
      <div v-if="showOnboarding" class="onboarding-alert">
        <div class="alert-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
        </div>
        <div class="alert-content">
          <h3 class="alert-title">{{ $t('dashboard.welcome.completeProfile') }}</h3>
          <p class="alert-message">
            {{ $t('dashboard.welcome.completeProfileMessage') }}
          </p>
          <router-link to="/provider/profile" class="alert-action">
            {{ $t('dashboard.welcome.completeNow') }}
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M9 5l7 7-7 7"
              />
            </svg>
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { Provider } from '../../types/provider.types'

interface Props {
  provider: Provider
  showOnboarding?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  showOnboarding: false,
})

const welcomeMessage = computed(() => {
  // Can be extended with i18n support
  return 'Welcome back,'
})

const getCurrentTimeGreeting = (): string => {
  const hour = new Date().getHours()

  if (hour < 12) {
    return 'Good morning! Ready to make today great?'
  } else if (hour < 18) {
    return 'Good afternoon! Keep up the great work!'
  } else {
    return 'Good evening! Time to review your day.'
  }
}
</script>

<style scoped>
.welcome-card {
  position: relative;
  overflow: hidden;
  background: linear-gradient(135deg, var(--color-primary-500) 0%, var(--color-primary-600) 100%);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-lg);
  color: white;
  min-height: 180px;
}

.welcome-card-background {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
}

.gradient-orb {
  position: absolute;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(255, 255, 255, 0.2) 0%, rgba(255, 255, 255, 0) 70%);
  filter: blur(40px);
  animation: float 6s ease-in-out infinite;
}

.orb-1 {
  width: 300px;
  height: 300px;
  top: -150px;
  inset-inline-end: -100px;
}

.orb-2 {
  width: 200px;
  height: 200px;
  bottom: -50px;
  inset-inline-start: -50px;
  animation-delay: -3s;
}

@keyframes float {
  0%,
  100% {
    transform: translateY(0) scale(1);
  }
  50% {
    transform: translateY(-20px) scale(1.05);
  }
}

.welcome-card-content {
  position: relative;
  padding: var(--spacing-xl);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
}

.provider-info {
  display: flex;
  align-items: center;
  gap: var(--spacing-lg);
}

.provider-avatar {
  flex-shrink: 0;
  width: 80px;
  height: 80px;
  border-radius: var(--radius-lg);
  background: rgba(255, 255, 255, 0.15);
  backdrop-filter: blur(10px);
  border: 3px solid rgba(255, 255, 255, 0.3);
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: var(--shadow-md);
}

.avatar-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.avatar-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
}

.avatar-placeholder svg {
  width: 40px;
  height: 40px;
  stroke-width: 2;
}

.provider-details {
  flex: 1;
  min-width: 0;
}

.welcome-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  margin: 0 0 var(--spacing-xs) 0;
  line-height: 1.3;
  display: flex;
  flex-wrap: wrap;
  gap: var(--spacing-xs);
  align-items: baseline;
}

.business-name {
  color: rgba(255, 255, 255, 0.95);
  font-weight: var(--font-weight-extrabold);
  text-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.welcome-subtitle {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-medium);
  margin: 0;
  color: rgba(255, 255, 255, 0.9);
  opacity: 0.95;
}

.onboarding-alert {
  display: flex;
  gap: var(--spacing-md);
  padding: var(--spacing-md) var(--spacing-lg);
  background: rgba(255, 255, 255, 0.15);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: var(--radius-lg);
  transition: all var(--transition-base);
}

.onboarding-alert:hover {
  background: rgba(255, 255, 255, 0.2);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.alert-icon {
  flex-shrink: 0;
  width: 24px;
  height: 24px;
  color: rgba(255, 255, 255, 0.95);
}

.alert-icon svg {
  width: 100%;
  height: 100%;
  stroke-width: 2.5;
}

.alert-content {
  flex: 1;
  min-width: 0;
}

.alert-title {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  margin: 0 0 var(--spacing-xs) 0;
  color: white;
}

.alert-message {
  font-size: var(--font-size-sm);
  margin: 0 0 var(--spacing-sm) 0;
  color: rgba(255, 255, 255, 0.9);
  line-height: 1.5;
}

.alert-action {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-xs);
  padding: var(--spacing-xs) var(--spacing-md);
  background: white;
  color: var(--color-primary-600);
  text-decoration: none;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  border-radius: var(--radius-md);
  transition: all var(--transition-fast);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.alert-action:hover {
  background: rgba(255, 255, 255, 0.95);
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
}

.alert-action svg {
  width: 16px;
  height: 16px;
  stroke-width: 2.5;
  transition: transform var(--transition-fast);
}

.alert-action:hover svg {
  transform: translateX(2px);
}

/* Mobile responsive */
@media (max-width: 767px) {
  .welcome-card {
    min-height: auto;
  }

  .welcome-card-content {
    padding: var(--spacing-lg);
  }

  .provider-info {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-md);
  }

  .provider-avatar {
    width: 64px;
    height: 64px;
  }

  .avatar-placeholder svg {
    width: 32px;
    height: 32px;
  }

  .welcome-title {
    font-size: var(--font-size-xl);
  }

  .welcome-subtitle {
    font-size: var(--font-size-sm);
  }

  .onboarding-alert {
    flex-direction: column;
    padding: var(--spacing-md);
  }

  .alert-action {
    align-self: flex-start;
  }
}

/* Tablet */
@media (min-width: 768px) and (max-width: 1023px) {
  .welcome-card-content {
    padding: var(--spacing-lg) var(--spacing-xl);
  }

  .provider-avatar {
    width: 72px;
    height: 72px;
  }

  .welcome-title {
    font-size: calc(var(--font-size-xl) + 0.25rem);
  }
}

/* RTL Support - Already handled by logical properties */

/* Reduced motion support */
@media (prefers-reduced-motion: reduce) {
  .gradient-orb {
    animation: none;
  }

  .alert-action:hover {
    transform: none;
  }

  .alert-action:hover svg {
    transform: none;
  }
}

/* Print styles */
@media print {
  .welcome-card {
    background: white;
    color: black;
    border: 1px solid #000;
  }

  .gradient-orb,
  .onboarding-alert {
    display: none;
  }
}
</style>
