<template>
  <header class="landing-header" :class="{ scrolled: isScrolled }">
    <div class="header-container">
      <!-- Logo -->
      <router-link to="/" class="logo">
        <span class="logo-text">Booksy</span>
      </router-link>

      <!-- Right Side Actions -->
      <div class="header-actions">
        <!-- Language Switcher -->
        <LanguageSwitcher class="language-switcher" />

        <!-- User Menu or Login Buttons -->
        <template v-if="isAuthenticated">
          <UserMenu />
        </template>
        <template v-else>
          <!-- Provider Login Button (Business Portal) -->
          <router-link to="/provider/login" class="provider-button">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
            </svg>
            <span>پنل کسب‌وکار</span>
          </router-link>

          <!-- Customer Login Button -->
          <router-link to="/customer/login" class="login-button">
            <span>ورود / ثبت‌نام</span>
          </router-link>
        </template>
      </div>
    </div>
  </header>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import UserMenu from '@/shared/components/layout/Header/UserMenu.vue'
import LanguageSwitcher from '@/shared/components/layout/Header/LanguageSwitcher.vue'

const authStore = useAuthStore()
const isAuthenticated = computed(() => authStore.isAuthenticated)
const isScrolled = ref(false)

function handleScroll() {
  isScrolled.value = window.scrollY > 20
}

onMounted(() => {
  window.addEventListener('scroll', handleScroll)
})

onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll)
})
</script>

<style scoped lang="scss">
.landing-header {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 1000;
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border-bottom: 1px solid rgba(229, 231, 235, 0.2);

  // Default (not scrolled) - Larger/Thicker
  &:not(.scrolled) {
    .header-container {
      padding: 1.5rem 2rem;
    }

    .logo-text {
      font-size: 2rem;
    }

    .login-button {
      padding: 0.75rem 1.75rem;
      font-size: 0.9375rem;
    }
  }

  // Scrolled - Thinner/Smaller
  &.scrolled {
    background: rgba(255, 255, 255, 0.98);
    backdrop-filter: blur(24px);
    -webkit-backdrop-filter: blur(24px);
    border-bottom-color: rgba(229, 231, 235, 0.8);
    box-shadow: 0 2px 16px rgba(0, 0, 0, 0.04);

    .header-container {
      padding: 0.75rem 2rem;
    }

    .logo-text {
      font-size: 1.5rem;
    }

    .login-button {
      padding: 0.5rem 1.25rem;
      font-size: 0.875rem;
    }

    .language-switcher {
      transform: scale(0.9);
    }
  }
}

.header-container {
  max-width: 1280px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  justify-content: space-between;
  transition: padding 0.4s cubic-bezier(0.4, 0, 0.2, 1);

  @media (max-width: 768px) {
    padding: 1rem 1.5rem !important;
  }
}

.logo {
  display: flex;
  align-items: center;
  text-decoration: none;
  transition: all 0.3s ease;

  &:hover {
    transform: scale(1.05);
  }
}

.logo-text {
  font-weight: 800;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  letter-spacing: -0.5px;
  transition: font-size 0.4s cubic-bezier(0.4, 0, 0.2, 1);

  @media (max-width: 768px) {
    font-size: 1.5rem !important;
  }
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;

  @media (max-width: 768px) {
    gap: 0.5rem;
  }
}

.language-switcher {
  transition: transform 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}

.provider-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: white;
  color: #667eea;
  text-decoration: none;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  font-weight: 600;
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  white-space: nowrap;
  padding: 0.625rem 1.25rem;

  svg {
    width: 18px;
    height: 18px;
  }

  &:hover {
    border-color: #667eea;
    background: #f7f9fc;
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.15);
  }

  &:active {
    transform: translateY(0);
  }

  @media (max-width: 768px) {
    padding: 0.5rem 0.75rem !important;
    font-size: 0.8125rem !important;

    span {
      display: none; // Hide text on mobile, show only icon
    }

    svg {
      width: 20px;
      height: 20px;
    }
  }
}

.login-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  text-decoration: none;
  border-radius: 12px;
  font-weight: 600;
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.25);
  white-space: nowrap;

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 8px 20px rgba(102, 126, 234, 0.35);
  }

  &:active {
    transform: translateY(0);
  }

  @media (max-width: 768px) {
    padding: 0.5rem 1rem !important;
    font-size: 0.8125rem !important;
  }
}

// Responsive adjustments for scrolled state
.landing-header {
  &.scrolled {
    .provider-button {
      padding: 0.5rem 1rem;
      font-size: 0.875rem;

      svg {
        width: 16px;
        height: 16px;
      }
    }
  }
}

// RTL Support
[dir='rtl'] {
  .header-container {
    direction: rtl;
  }

  .logo-text {
    font-family: 'Vazirmatn', -apple-system, BlinkMacSystemFont, sans-serif;
  }
}
</style>
