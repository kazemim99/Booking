<template>
  <div class="phone-verification-view">
    <div class="verification-container">
      <!-- Logo/Branding -->
      <div class="branding">
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
      <div class="flow-container">
        <PhoneVerificationFlow @success="handleSuccess" @error="handleError" />
      </div>

      <!-- Footer Links -->
      <div class="footer-links">
        <router-link to="/login" class="footer-link">
          {{ $t('auth.phoneVerification.haveAccount') }}
        </router-link>
        <span class="separator">•</span>
        <router-link to="/help" class="footer-link">
          {{ $t('auth.phoneVerification.needHelp') }}
        </router-link>
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
import { useI18n } from 'vue-i18n'
import { useToast } from '@/shared/composables/useToast'
import PhoneVerificationFlow from '../components/PhoneVerificationFlow.vue'

const { t } = useI18n()
const toast = useToast()

const handleSuccess = async () => {
  toast.success(t('auth.phoneVerification.successToast'))

  // The redirect logic is now handled by PhoneVerificationFlow component
  // which calls redirectAfterVerification() from usePhoneVerification
  // This will check provider existence and redirect appropriately
}

const handleError = (error: string) => {
  toast.error(error)
}
</script>

<style scoped>
.phone-verification-view {
  position: relative;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #f5f3ff 0%, #ffffff 50%, #faf5ff 100%);
  overflow: hidden;
}

.verification-container {
  position: relative;
  z-index: 10;
  width: 100%;
  max-width: 32rem;
  padding: 2rem;
}

/* Branding */
.branding {
  text-align: center;
  margin-bottom: 3rem;
}

.logo {
  display: inline-block;
  margin-bottom: 1rem;
  animation: fadeInDown 0.5s ease;
}

.logo svg {
  width: 4rem;
  height: 4rem;
  filter: drop-shadow(0 4px 6px rgba(139, 92, 246, 0.2));
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
  padding: 2.5rem;
  box-shadow:
    0 20px 25px -5px rgba(0, 0, 0, 0.1),
    0 10px 10px -5px rgba(0, 0, 0, 0.04);
  animation: fadeInUp 0.5s ease 0.3s both;
}

/* Footer Links */
.footer-links {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  margin-top: 2rem;
  animation: fadeInUp 0.5s ease 0.4s both;
}

.footer-link {
  font-size: 0.875rem;
  color: #6b7280;
  text-decoration: none;
  transition: color 0.2s ease;
}

.footer-link:hover {
  color: #8b5cf6;
}

.separator {
  color: #d1d5db;
  font-size: 0.875rem;
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

/* Responsive */
@media (max-width: 640px) {
  .verification-container {
    padding: 1rem;
  }

  .flow-container {
    padding: 1.5rem;
  }

  .brand-name {
    font-size: 1.5rem;
  }

  .brand-tagline {
    font-size: 0.875rem;
  }

  .footer-links {
    flex-direction: column;
    gap: 0.5rem;
  }

  .separator {
    display: none;
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
