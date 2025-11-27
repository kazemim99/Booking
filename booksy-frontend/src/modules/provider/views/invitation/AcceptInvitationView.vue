<template>
  <div class="accept-invitation-view">
    <div class="accept-invitation-container">
      <!-- Loading State -->
      <div v-if="isLoading" class="loading-state">
        <div class="spinner"></div>
        <p>در حال بارگذاری دعوت...</p>
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="error-state">
        <i class="icon-alert-circle"></i>
        <h2>خطا در بارگذاری دعوت</h2>
        <p>{{ error }}</p>
        <AppButton variant="primary" @click="$router.push('/provider/register')">
          بازگشت به صفحه ثبت‌نام
        </AppButton>
      </div>

      <!-- Expired State -->
      <div v-else-if="isExpired" class="expired-state">
        <i class="icon-clock"></i>
        <h2>دعوت منقضی شده</h2>
        <p>این دعوت منقضی شده است. لطفاً از سازمان درخواست ارسال دعوت جدید کنید.</p>
        <div class="expired-info">
          <p><strong>سازمان:</strong> {{ invitation?.organizationName }}</p>
          <p><strong>تاریخ انقضا:</strong> {{ formatDate(invitation?.expiresAt) }}</p>
        </div>
        <AppButton variant="primary" @click="$router.push('/')">
          بازگشت به صفحه اصلی
        </AppButton>
      </div>

      <!-- Accepted State -->
      <div v-else-if="isAccepted" class="success-state">
        <i class="icon-check-circle"></i>
        <h2>دعوت پذیرفته شد!</h2>
        <p>شما با موفقیت به تیم {{ invitation?.organizationName }} اضافه شدید.</p>
        <AppButton variant="primary" @click="$router.push('/provider/dashboard')">
          رفتن به داشبورد
        </AppButton>
      </div>

      <!-- Invitation Details -->
      <div v-else class="invitation-details">
        <div class="invitation-header">
          <div class="organization-logo" v-if="invitation?.organizationLogo">
            <img
              :src="invitation.organizationLogo"
              :alt="invitation.organizationName"
              @error="handleImageError"
              @load="handleImageLoad"
            />
          </div>
          <div class="organization-logo organization-logo-fallback" v-else>
            <i class="icon-briefcase"></i>
          </div>
          <h1 class="invitation-title">دعوت برای پیوستن به تیم</h1>
          <h2 class="organization-name">{{ invitation?.organizationName }}</h2>
        </div>

        <div class="invitation-message" v-if="invitation?.message">
          <i class="icon-message"></i>
          <p>{{ invitation.message }}</p>
        </div>

        <div class="invitation-info">
          <div class="info-item">
            <i class="icon-calendar"></i>
            <span>تاریخ دعوت: {{ formatDate(invitation?.sentAt) }}</span>
          </div>
          <div class="info-item">
            <i class="icon-clock"></i>
            <span>اعتبار تا: {{ formatDate(invitation?.expiresAt) }}</span>
          </div>
        </div>

        <!-- Check if user is already registered -->
        <div v-if="isUserRegistered" class="action-section">
          <p class="action-description">
            برای پذیرش این دعوت، روی دکمه زیر کلیک کنید.
          </p>
          <div class="action-buttons">
            <AppButton variant="secondary" @click="handleReject" :disabled="isSubmitting">
              رد دعوت
            </AppButton>
            <AppButton
              variant="primary"
              @click="handleAccept"
              :disabled="isSubmitting"
              :loading="isSubmitting"
            >
              پذیرش دعوت
            </AppButton>
          </div>
        </div>

        <!-- If user needs to complete profile -->
        <div v-else class="profile-completion-section">
          <p class="section-description">
            برای پذیرش دعوت، لطفاً پروفایل خود را تکمیل کنید.
          </p>
          <CompleteStaffProfile
            :invitation-id="invitationId"
            :organization-id="invitation?.organizationId"
            :organization-name="invitation?.organizationName"
            @completed="handleProfileCompleted"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useHierarchyStore } from '../../stores/hierarchy.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import CompleteStaffProfile from '../../components/invitation/CompleteStaffProfile.vue'
import type { ProviderInvitation } from '../../types/hierarchy.types'
import { InvitationStatus } from '../../types/hierarchy.types'

const route = useRoute()
const router = useRouter()
const hierarchyStore = useHierarchyStore()
const authStore = useAuthStore()

const invitationId = ref<string>('')
const invitation = ref<ProviderInvitation | null>(null)
const isLoading = ref(true)
const isSubmitting = ref(false)
const error = ref<string | null>(null)

const isUserRegistered = computed(() => authStore.isAuthenticated)
const isExpired = computed(() => {
  if (!invitation.value) return false
  return invitation.value.status === InvitationStatus.Expired ||
    new Date(invitation.value.expiresAt) < new Date()
})
const isAccepted = computed(() => invitation.value?.status === InvitationStatus.Accepted)

onMounted(async () => {
  await loadInvitation()
})

async function loadInvitation() {
  isLoading.value = true
  error.value = null

  try {
    const id = route.params.id as string
    const orgId = route.query.org as string

    if (!id || !orgId) {
      throw new Error('شناسه دعوت یا سازمان نامعتبر است')
    }

    invitationId.value = id

    // Load invitation details from API
    invitation.value = await hierarchyStore.getInvitation(orgId, id)

  } catch (err) {
    error.value = err instanceof Error ? err.message : 'خطا در بارگذاری دعوت'
    console.error('Error loading invitation:', err)
  } finally {
    isLoading.value = false
  }
}

async function handleAccept() {
  if (!invitation.value || !isUserRegistered.value) return

  // Get current user's provider ID
  const currentProviderId = authStore.providerId
  console.log('=== ACCEPT INVITATION DEBUG ===')
  console.log('Current Provider ID from authStore:', currentProviderId)
  console.log('Organization ID from invitation:', invitation.value.organizationId)

  if (!currentProviderId) {
    error.value = 'لطفاً ابتدا ثبت‌نام کنید'
    return
  }

  // Check if current provider has hierarchy info loaded
  if (!hierarchyStore.currentHierarchy) {
    // Try to load hierarchy to check if user is Individual type
    try {
      console.log('Loading hierarchy for provider:', currentProviderId)
      await hierarchyStore.loadProviderHierarchy(currentProviderId)
      console.log('Hierarchy loaded:', hierarchyStore.currentHierarchy)
    } catch (err) {
      console.error('Could not load provider hierarchy:', err)
      // If we can't load hierarchy, let the backend validate
      // The backend will return a proper error if the user is not an Individual
    }
  }

  // Validate that current user is an Individual provider (if hierarchy is loaded)
  const hierarchyType = hierarchyStore.currentHierarchy?.provider?.hierarchyType
  console.log('Current provider hierarchy type:', hierarchyType)

  if (hierarchyType === 'Organization') {
    error.value = 'فقط ارائه‌دهندگان فردی می‌توانند دعوت را بپذیرند. شما با حساب سازمانی وارد شده‌اید.'
    return
  }

  isSubmitting.value = true
  error.value = null

  try {
    console.log('Calling acceptInvitation with provider ID:', currentProviderId)
    console.log('API URL will be: /api/v1/providers/' + currentProviderId + '/hierarchy/invitations/' + invitationId.value + '/accept')

    // Use current user's provider ID (not organization ID) in the API call
    await hierarchyStore.acceptInvitation(
      currentProviderId,
      invitationId.value,
      {
        invitationId: invitationId.value,
      }
    )

    invitation.value.status = InvitationStatus.Accepted

    // Redirect to dashboard after short delay
    setTimeout(() => {
      router.push('/provider/dashboard')
    }, 2000)
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'خطا در پذیرش دعوت'
    console.error('Error accepting invitation:', err)
  } finally {
    isSubmitting.value = false
  }
}

async function handleReject() {
  if (!invitation.value) return

  const confirmed = confirm('آیا مطمئن هستید که می‌خواهید این دعوت را رد کنید؟')
  if (!confirmed) return

  invitation.value.status = InvitationStatus.Rejected
  router.push('/')
}

function handleProfileCompleted() {
  invitation.value!.status = InvitationStatus.Accepted
}

function handleImageError(event: Event) {
  console.error('Failed to load organization logo:', invitation.value?.organizationLogo)
  const img = event.target as HTMLImageElement
  // Hide the image on error
  if (img.parentElement) {
    img.parentElement.style.display = 'none'
  }
}

function handleImageLoad() {
  console.log('Organization logo loaded successfully:', invitation.value?.organizationLogo)
}

function formatDate(dateString?: string | Date): string {
  if (!dateString) return 'نامشخص'

  const date = new Date(dateString)

  // Check if date is valid
  if (isNaN(date.getTime())) {
    console.warn('Invalid date value:', dateString)
    return 'نامشخص'
  }

  return new Intl.DateTimeFormat('fa-IR', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date)
}
</script>

<style scoped lang="scss">
.accept-invitation-view {
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
}

.accept-invitation-container {
  background: white;
  border-radius: 16px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  max-width: 600px;
  width: 100%;
  padding: 3rem;
}

.loading-state,
.error-state,
.expired-state,
.success-state {
  text-align: center;
  padding: 2rem 0;

  i {
    font-size: 4rem;
    margin-bottom: 1rem;
  }

  h2 {
    font-size: 1.5rem;
    margin-bottom: 1rem;
    color: #1a202c;
  }

  p {
    color: #718096;
    margin-bottom: 1.5rem;
    line-height: 1.6;
  }
}

.loading-state {
  .spinner {
    width: 50px;
    height: 50px;
    border: 4px solid #f3f4f6;
    border-top-color: #667eea;
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin: 0 auto 1rem;
  }
}

.error-state i {
  color: #f56565;
}

.expired-state i {
  color: #ed8936;
}

.success-state i {
  color: #48bb78;
}

.expired-info {
  background: #fef5e7;
  border: 1px solid #f39c12;
  border-radius: 8px;
  padding: 1rem;
  margin: 1.5rem 0;
  text-align: right;

  p {
    margin: 0.5rem 0;
    color: #1a202c;
  }
}

.invitation-details {
  .invitation-header {
    text-align: center;
    margin-bottom: 2rem;

    .organization-logo {
      width: 100px;
      height: 100px;
      margin: 0 auto 1rem;
      border-radius: 50%;
      overflow: hidden;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);

      img {
        width: 100%;
        height: 100%;
        object-fit: cover;
      }

      &.organization-logo-fallback {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        display: flex;
        align-items: center;
        justify-content: center;

        i {
          font-size: 3rem;
          color: #fff;
        }
      }
    }

    .invitation-title {
      font-size: 1.25rem;
      color: #718096;
      margin-bottom: 0.5rem;
    }

    .organization-name {
      font-size: 2rem;
      color: #1a202c;
      font-weight: bold;
    }
  }

  .invitation-message {
    background: #f7fafc;
    border-right: 4px solid #667eea;
    border-radius: 8px;
    padding: 1.5rem;
    margin-bottom: 2rem;
    display: flex;
    align-items: flex-start;
    gap: 1rem;

    i {
      color: #667eea;
      font-size: 1.5rem;
      margin-top: 0.25rem;
    }

    p {
      flex: 1;
      color: #2d3748;
      line-height: 1.6;
      margin: 0;
    }
  }

  .invitation-info {
    margin-bottom: 2rem;

    .info-item {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.75rem 0;
      color: #4a5568;

      i {
        color: #667eea;
        font-size: 1.25rem;
      }

      span {
        font-size: 0.95rem;
      }
    }
  }

  .action-section,
  .profile-completion-section {
    border-top: 1px solid #e2e8f0;
    padding-top: 2rem;

    .action-description,
    .section-description {
      text-align: center;
      color: #718096;
      margin-bottom: 1.5rem;
    }

    .action-buttons {
      display: flex;
      gap: 1rem;
      justify-content: center;

      button {
        min-width: 140px;
      }
    }
  }
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 640px) {
  .accept-invitation-container {
    padding: 2rem 1.5rem;
  }

  .invitation-header {
    .organization-name {
      font-size: 1.5rem !important;
    }
  }

  .action-buttons {
    flex-direction: column;

    button {
      width: 100%;
    }
  }
}
</style>
