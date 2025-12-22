<template>
  <div class="completion-container">
    <div class="completion-card">
      <div class="card-content">
        <!-- Success Icon -->
        <div class="icon-container">
          <div class="icon-wrapper">
            <svg xmlns="http://www.w3.org/2000/svg" class="check-icon" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <svg xmlns="http://www.w3.org/2000/svg" class="sparkle-icon sparkle-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z" />
          </svg>
          <svg xmlns="http://www.w3.org/2000/svg" class="sparkle-icon sparkle-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z" />
          </svg>
        </div>

        <!-- Title & Message -->
        <h1 class="completion-title">تبریک می‌گوییم!</h1>
        <p class="completion-message">
          ثبت‌نام شما با موفقیت انجام شد. اکنون می‌توانید از تمامی امکانات پنل استفاده کنید
        </p>

        <!-- Stats Cards -->
        <div class="stats-grid">
          <div class="stat-card">
            <div class="stat-icon">✓</div>
            <p class="stat-label">پروفایل کامل</p>
          </div>
          <div class="stat-card">
            <div class="stat-icon">🎯</div>
            <p class="stat-label">آماده فعالیت</p>
          </div>
          <div class="stat-card">
            <div class="stat-icon">🚀</div>
            <p class="stat-label">شروع موفق</p>
          </div>
        </div>

        <!-- CTA Button -->
        <AppButton
          type="button"
          variant="primary"
          size="large"
          block
          @click="goToDashboard"
          :loading="isLoading"
          :disabled="isLoading"
          class="dashboard-button"
        >
          {{ isLoading ? 'در حال بارگذاری...' : 'ورود به داشبورد' }}
        </AppButton>

        <!-- Next Steps -->
        <div class="next-steps">
          <p class="next-steps-title">گام‌های بعدی:</p>
          <ul class="next-steps-list">
            <li>• بررسی و تکمیل اطلاعات پروفایل</li>
            <li>• افزودن نوبت‌های جدید</li>
            <li>• دعوت از مشتریان</li>
          </ul>
        </div>
      </div>

      <!-- Additional Help -->
      <div class="help-section">
        <p class="help-text">
          نیاز به راهنمایی دارید؟{" "}
          <a href="#" class="help-link">
            مشاهده راهنمای کامل
          </a>
        </p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useProviderStore } from '@/modules/provider/stores/provider.store'
import { useHierarchyStore } from '@/modules/provider/stores/hierarchy.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { toastService } from '@/core/services/toast.service'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'

const router = useRouter()
const providerStore = useProviderStore()
const hierarchyStore = useHierarchyStore()
const authStore = useAuthStore()

const isLoading = ref(false)

const goToDashboard = async () => {
  try {
    isLoading.value = true

    // Step 0: Check current auth state
    console.log('[CompletionStep] Step 0: Current auth state:', {
      providerId: authStore.providerId,
      providerStatus: authStore.providerStatus,
      isAuthenticated: authStore.isAuthenticated
    })

    // Step 1: Refresh JWT token with updated provider status
    // This gets a new token from the backend with fresh provider info
    console.log('[CompletionStep] Step 1: Refreshing provider token...')
    const tokenRefreshed = await authStore.refreshProviderToken()

    if (tokenRefreshed) {
      console.log('[CompletionStep] ✅ Token refreshed successfully')
      console.log('[CompletionStep] Updated auth state:', {
        providerId: authStore.providerId,
        providerStatus: authStore.providerStatus
      })
    } else {
      console.warn('[CompletionStep] ⚠️ Token refresh failed, continuing with old token')
    }

    // Step 2: Reload current provider data to get fresh provider info
    console.log('[CompletionStep] Step 2: Loading current provider...')
    console.log('[CompletionStep] Provider ID from auth store:', authStore.providerId)

    await providerStore.loadCurrentProvider(true) // Force refresh

    if (providerStore.currentProvider) {
      console.log('[CompletionStep] ✅ Provider loaded:', {
        id: providerStore.currentProvider.id,
        businessName: providerStore.currentProvider.profile?.businessName
      })
    } else {
      console.error('[CompletionStep] ❌ Provider not loaded - currentProvider is null')
    }

    // Step 3: Load hierarchy using the fresh provider ID
    if (providerStore.currentProvider?.id) {
      console.log('[CompletionStep] Step 3: Loading hierarchy for provider:', providerStore.currentProvider.id)
      await hierarchyStore.loadProviderHierarchy(providerStore.currentProvider.id)
      console.log('[CompletionStep] ✅ Hierarchy loaded:', hierarchyStore.currentHierarchy)
    } else {
      console.warn('[CompletionStep] ⚠️ Skipping hierarchy load - no provider ID available')
    }

    // Step 4: Fetch latest provider status from API (updates auth store)
    // This is a fallback in case token refresh didn't work
    console.log('[CompletionStep] Step 4: Fetching provider status...')
    await authStore.fetchProviderStatus()
    console.log('[CompletionStep] ✅ Provider status fetched:', authStore.providerStatus)

    // Step 5: Now redirect to dashboard with fresh data loaded
    console.log('[CompletionStep] Step 5: Redirecting to dashboard...')
    console.log('[CompletionStep] Final state before redirect:', {
      providerId: authStore.providerId,
      providerStatus: authStore.providerStatus,
      currentProvider: providerStore.currentProvider?.id,
      hierarchyLoaded: !!hierarchyStore.currentHierarchy
    })
    router.push({ name: 'ProviderDashboard' })
  } catch (error) {
    console.error('[CompletionStep] Failed to load provider data:', error)
    // Show warning but still redirect
    toastService.warning('در حال بارگذاری اطلاعات...')
    router.push({ name: 'ProviderDashboard' })
  } finally {
    isLoading.value = false
  }
}
</script>

<style scoped>
.completion-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  background: linear-gradient(to bottom right, rgba(139, 92, 246, 0.05), rgba(236, 72, 153, 0.2));
  direction: rtl;
}

.completion-card {
  width: 100%;
  max-width: 28rem;
}

.card-content {
  background: white;
  border-radius: 1rem;
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
  padding: 2rem;
  text-align: center;
}

/* Success Icon */
.icon-container {
  position: relative;
  margin-bottom: 1.5rem;
}

.icon-wrapper {
  width: 6rem;
  height: 6rem;
  background: rgba(139, 92, 246, 0.1);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto;
}

.check-icon {
  width: 4rem;
  height: 4rem;
  color: #8b5cf6;
}

.sparkle-icon {
  position: absolute;
  color: #8b5cf6;
  animation: sparkle-pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}

.sparkle-1 {
  width: 1.5rem;
  height: 1.5rem;
  top: 0;
  right: 33%;
}

.sparkle-2 {
  width: 1.25rem;
  height: 1.25rem;
  bottom: 0.5rem;
  left: 25%;
  animation-delay: 75ms;
}

@keyframes sparkle-pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}

/* Title & Message */
.completion-title {
  margin-bottom: 0.75rem;
  font-size: 1.875rem;
  font-weight: 700;
  color: #111827;
}

.completion-message {
  color: #6b7280;
  margin-bottom: 2rem;
  line-height: 1.5;
}

/* Stats Cards */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0.75rem;
  margin-bottom: 2rem;
}

.stat-card {
  padding: 0.75rem;
  background: rgba(156, 163, 175, 0.1);
  border-radius: 0.5rem;
}

.stat-icon {
  font-size: 1.5rem;
  margin-bottom: 0.25rem;
}

.stat-label {
  font-size: 0.75rem;
  color: #6b7280;
  margin: 0;
}

/* Dashboard Button */
.dashboard-button {
  margin-bottom: 1rem;
}

/* Next Steps */
.next-steps {
  margin-top: 1.5rem;
  padding: 1rem;
  background: rgba(236, 72, 153, 0.1);
  border-radius: 0.5rem;
  text-align: right;
}

.next-steps-title {
  font-size: 0.875rem;
  font-weight: 500;
  margin-bottom: 0.5rem;
  color: #111827;
}

.next-steps-list {
  list-style: none;
  padding: 0;
  margin: 0;
}

.next-steps-list li {
  font-size: 0.875rem;
  color: #6b7280;
  margin-bottom: 0.25rem;
}

.next-steps-list li:last-child {
  margin-bottom: 0;
}

/* Help Section */
.help-section {
  margin-top: 1.5rem;
  text-align: center;
}

.help-text {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

.help-link {
  color: #8b5cf6;
  text-decoration: none;
  transition: text-decoration 0.2s;
}

.help-link:hover {
  text-decoration: underline;
}

/* Responsive */
@media (max-width: 640px) {
  .completion-container {
    padding: 0.5rem;
  }

  .card-content {
    padding: 1.5rem;
  }

  .completion-title {
    font-size: 1.5rem;
  }

  .stats-grid {
    gap: 0.5rem;
  }

  .stat-card {
    padding: 0.5rem;
  }

  .stat-icon {
    font-size: 1.25rem;
  }

  .stat-label {
    font-size: 0.625rem;
  }
}
</style>
