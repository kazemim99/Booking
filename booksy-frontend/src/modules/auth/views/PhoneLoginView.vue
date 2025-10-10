<template>
  <div class="auth-page">
    <!-- Floating Language Switcher -->
    <div class="floating-language-switcher">
      <LanguageSwitcher />
    </div>

    <div class="auth-container">
      <div class="auth-card">
        <!-- Logo -->
        <div class="logo-container">
          <div class="logo">
            <svg viewBox="0 0 40 40" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect width="40" height="40" rx="8" fill="#8B5CF6" />
              <path
                d="M20 10C14.477 10 10 14.477 10 20C10 25.523 14.477 30 20 30C25.523 30 30 25.523 30 20C30 14.477 25.523 10 20 10ZM23 21H21V23C21 23.552 20.552 24 20 24C19.448 24 19 23.552 19 23V21H17C16.448 21 16 20.552 16 20C16 19.448 16.448 19 17 19H19V17C19 16.448 19.448 16 20 16C20.552 16 21 16.448 21 17V19H23C23.552 19 24 19.448 24 20C24 20.552 23.552 21 23 21Z"
                fill="white"
              />
            </svg>
          </div>
        </div>

        <!-- Phone Verification Flow -->
        <PhoneVerificationFlow @success="handleVerificationSuccess" @error="handleError" />

        <!-- Traditional Login Link -->
        <div class="form-footer">
          <p>
            {{ $t('auth.preferEmail') }}
            <router-link to="/login/email" class="login-link">
              {{ $t('auth.emailLogin') }}
            </router-link>
          </p>
        </div>
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
import { useRouter } from 'vue-router'
import { useToast } from '@/shared/composables/useToast'
import LanguageSwitcher from '@/shared/components/layout/Header/LanguageSwitcher.vue'
import PhoneVerificationFlow from '../components/PhoneVerificationFlow.vue'
import type { UserInfo } from '../types/phoneVerification.types'

// Initialize router, store, and toast
const router = useRouter()
const toast = useToast()

// Methods
const handleVerificationSuccess = async (data: {
  user: UserInfo
  token: string
  isNewUser?: boolean
}) => {
  try {
    // Navigate based on user status
    const redirectPath = router.currentRoute.value.query.redirect as string
    const userStatus = data.user.status

    // Check if user needs to complete registration (Draft status)
    if (userStatus === 'Draft') {
      // User needs to complete registration
      toast.success('Account created successfully! Please complete your profile.')

      if (data.user.roles.includes('Provider')) {
        router.push({ name: 'ProviderRegistration' })
      } else if (data.user.roles.includes('Admin')) {
        router.push({ name: 'AdminDashboard' })
      } else {
        // For customers, redirect to home or profile setup
        router.push({ path: '/' })
      }
    } else {
      // Active users - login successful
      toast.success('Login successful!')

      if (redirectPath) {
        router.push(redirectPath)
      } else if (data.user.roles.includes('Provider')) {
        // Check if provider needs onboarding
        router.push({ name: 'ProviderDashboard' })
      } else if (data.user.roles.includes('Admin')) {
        router.push({ name: 'AdminDashboard' })
      } else {
        router.push({ name: 'CustomerDashboard' })
      }
    }
  } catch (error) {
    console.error('Error handling verification success:', error)
    toast.error('An error occurred during login')
  }
}

const handleError = (error: string) => {
  toast.error(error)
}
</script>

<style scoped>
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
}

.auth-container {
  width: 100%;
  max-width: 450px;
  z-index: 10;
  position: relative;
  animation: fadeInUp 0.5s ease 0.3s both;
}

.auth-card {
  background: white;
  border-radius: 16px;
  padding: 3rem;
  box-shadow:
    0 20px 25px -5px rgba(0, 0, 0, 0.1),
    0 10px 10px -5px rgba(0, 0, 0, 0.04);
}

.logo-container {
  text-align: center;
  margin-bottom: 2rem;
}

.logo {
  display: inline-block;
  animation: fadeInDown 0.5s ease;
}

.logo svg {
  width: 4rem;
  height: 4rem;
  filter: drop-shadow(0 4px 6px rgba(139, 92, 246, 0.2));
}

/* Form Footer */
.form-footer {
  margin-top: 2rem;
  text-align: center;
  font-size: 0.875rem;
  color: #6b7280;
}

.login-link {
  color: #8b5cf6;
  font-weight: 500;
  text-decoration: none;
  transition: color 0.2s ease;
}

.login-link:hover {
  color: #7c3aed;
  text-decoration: underline;
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

/* Fade Transition */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

@media (max-width: 640px) {
  .auth-card {
    padding: 2rem 1.5rem;
  }

  .form-title {
    font-size: 1.5rem;
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
