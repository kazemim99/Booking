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

        <!-- If user needs to register with OTP -->
        <div v-else class="quick-registration-section">
          <!-- Step 1: Show invitation preview -->
          <div v-if="registrationStep === 'preview'" class="invitation-preview">
            <p class="invitation-text">
              شما به عنوان کارمند به تیم <strong>{{ invitation?.organizationName }}</strong> دعوت شده‌اید
            </p>
            <AppButton
              variant="primary"
              size="large"
              @click="startQuickRegistration"
              full-width
            >
              پذیرش دعوت و ثبت‌نام
            </AppButton>
          </div>

          <!-- Step 2: Registration form -->
          <div v-else-if="registrationStep === 'form'" class="registration-form">
            <h3 class="form-title">تکمیل اطلاعات</h3>

            <div class="form-group">
              <label class="form-label">شماره موبایل</label>
              <input
                type="tel"
                :value="formatPhone(invitation?.inviteePhoneNumber)"
                disabled
                class="form-input form-input-disabled"
                dir="ltr"
              />
              <span class="form-hint">این شماره توسط سازمان برای شما ثبت شده است</span>
            </div>

            <div class="form-row">
              <div class="form-group">
                <label class="form-label">نام <span class="required">*</span></label>
                <input
                  v-model="registrationForm.firstName"
                  type="text"
                  class="form-input"
                  :class="{ 'form-input-error': registrationErrors.firstName }"
                  placeholder="نام خود را وارد کنید"
                  @blur="validateRegistrationField('firstName')"
                  required
                />
                <span v-if="registrationErrors.firstName" class="form-error">
                  {{ registrationErrors.firstName }}
                </span>
              </div>

              <div class="form-group">
                <label class="form-label">نام خانوادگی <span class="required">*</span></label>
                <input
                  v-model="registrationForm.lastName"
                  type="text"
                  class="form-input"
                  :class="{ 'form-input-error': registrationErrors.lastName }"
                  placeholder="نام خانوادگی خود را وارد کنید"
                  @blur="validateRegistrationField('lastName')"
                  required
                />
                <span v-if="registrationErrors.lastName" class="form-error">
                  {{ registrationErrors.lastName }}
                </span>
              </div>
            </div>

            <div class="form-group">
              <label class="form-label">ایمیل (اختیاری)</label>
              <input
                v-model="registrationForm.email"
                type="email"
                class="form-input"
                :class="{ 'form-input-error': registrationErrors.email }"
                placeholder="example@email.com"
                dir="ltr"
                @blur="validateRegistrationField('email')"
              />
              <span v-if="registrationErrors.email" class="form-error">
                {{ registrationErrors.email }}
              </span>
            </div>

            <div class="cloning-options">
              <h4 class="cloning-title">اطلاعات پروفایل</h4>
              <div class="checkbox-group">
                <label class="checkbox-label">
                  <input type="checkbox" v-model="registrationForm.cloneServices" disabled />
                  <span>کپی خدمات از سازمان</span>
                </label>
                <label class="checkbox-label">
                  <input type="checkbox" v-model="registrationForm.cloneWorkingHours" disabled />
                  <span>کپی ساعات کاری از سازمان</span>
                </label>
                <label class="checkbox-label">
                  <input type="checkbox" v-model="registrationForm.cloneGallery" disabled />
                  <span>کپی گالری تصاویر از سازمان</span>
                </label>
              </div>
              <p class="form-hint">
                اطلاعات از سازمان کپی می‌شود و شما بعداً می‌توانید آن‌ها را ویرایش کنید
              </p>
            </div>

            <AppButton
              variant="primary"
              size="large"
              @click="sendOTPForInvitation"
              :disabled="!isRegistrationFormValid || isSendingOTP"
              :loading="isSendingOTP"
              full-width
            >
              ارسال کد تأیید
            </AppButton>
          </div>

          <!-- Step 3: OTP verification -->
          <div v-else-if="registrationStep === 'otp'" class="otp-verification">
            <h3 class="otp-title">تأیید شماره موبایل</h3>
            <p class="otp-description">
              کد تأیید 6 رقمی به شماره
              <strong>{{ formatPhone(invitation?.inviteePhoneNumber) }}</strong>
              ارسال شد
            </p>

            <OTPInput
              v-model="otpCode"
              :length="6"
              :error="otpError"
              @complete="handleOTPComplete"
            />

            <div class="otp-actions">
              <button
                v-if="canResendOTP"
                @click="sendOTPForInvitation"
                class="resend-button"
                :disabled="isSendingOTP"
              >
                ارسال مجدد کد
              </button>
              <span v-else class="timer">
                ارسال مجدد در {{ otpTimer }} ثانیه
              </span>
            </div>

            <AppButton
              variant="primary"
              size="large"
              @click="verifyOTPAndAccept"
              :disabled="otpCode.length !== 6 || isVerifyingOTP"
              :loading="isVerifyingOTP"
              full-width
            >
              تأیید و پذیرش دعوت
            </AppButton>

            <button
              @click="registrationStep = 'form'"
              class="back-button"
            >
              بازگشت
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useHierarchyStore } from '../../stores/hierarchy.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useToast } from '@/core/composables/useToast'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import OTPInput from '@/shared/components/ui/OTPInput.vue'
import phoneVerificationApi from '@/modules/auth/api/phoneVerification.api'
import { hierarchyService } from '../../services/hierarchy.service'
import type { ProviderInvitation } from '../../types/hierarchy.types'
import { InvitationStatus } from '../../types/hierarchy.types'
import type { User } from '@/modules/user-management/types/user.types'
import { UserType, UserStatus } from '@/modules/user-management/types/user.types'

const route = useRoute()
const router = useRouter()
const hierarchyStore = useHierarchyStore()
const authStore = useAuthStore()
const toast = useToast()

const invitationId = ref<string>('')
const invitation = ref<ProviderInvitation | null>(null)
const isLoading = ref(true)
const isSubmitting = ref(false)
const error = ref<string | null>(null)

// Registration flow state
type RegistrationStep = 'preview' | 'form' | 'otp'
const registrationStep = ref<RegistrationStep>('preview')

const registrationForm = reactive({
  firstName: '',
  lastName: '',
  email: '',
  cloneServices: true,
  cloneWorkingHours: true,
  cloneGallery: true,
})

const registrationErrors = reactive({
  firstName: '',
  lastName: '',
  email: '',
})

// OTP state
const otpCode = ref('')
const otpError = ref('')
const isSendingOTP = ref(false)
const isVerifyingOTP = ref(false)
const canResendOTP = ref(false)
const otpTimer = ref(120)
let otpTimerInterval: number | null = null

const isUserRegistered = computed(() => authStore.isAuthenticated)
const isExpired = computed(() => {
  if (!invitation.value) return false
  return invitation.value.status === InvitationStatus.Expired ||
    new Date(invitation.value.expiresAt) < new Date()
})
const isAccepted = computed(() => invitation.value?.status === InvitationStatus.Accepted)

const isRegistrationFormValid = computed(() => {
  return (
    registrationForm.firstName.trim().length >= 2 &&
    registrationForm.lastName.trim().length >= 2 &&
    (registrationForm.email === '' || isValidEmail(registrationForm.email)) &&
    !registrationErrors.firstName &&
    !registrationErrors.lastName &&
    !registrationErrors.email
  )
})

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

// Registration flow functions
function startQuickRegistration() {
  registrationStep.value = 'form'

  // Pre-fill name from invitation if available
  if (invitation.value?.inviteeName) {
    const nameParts = invitation.value.inviteeName.split(' ')
    registrationForm.firstName = nameParts[0] || ''
    registrationForm.lastName = nameParts.slice(1).join(' ') || ''
  }
}

function isValidEmail(email: string): boolean {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email)
}

function validateRegistrationField(field: keyof typeof registrationErrors) {
  switch (field) {
    case 'firstName':
      if (!registrationForm.firstName.trim()) {
        registrationErrors.firstName = 'نام الزامی است'
      } else if (registrationForm.firstName.length < 2) {
        registrationErrors.firstName = 'نام باید حداقل 2 کاراکتر باشد'
      } else {
        registrationErrors.firstName = ''
      }
      break

    case 'lastName':
      if (!registrationForm.lastName.trim()) {
        registrationErrors.lastName = 'نام خانوادگی الزامی است'
      } else if (registrationForm.lastName.length < 2) {
        registrationErrors.lastName = 'نام خانوادگی باید حداقل 2 کاراکتر باشد'
      } else {
        registrationErrors.lastName = ''
      }
      break

    case 'email':
      if (registrationForm.email && !isValidEmail(registrationForm.email)) {
        registrationErrors.email = 'فرمت ایمیل نامعتبر است'
      } else {
        registrationErrors.email = ''
      }
      break
  }
}

async function sendOTPForInvitation() {
  if (!invitation.value?.inviteePhoneNumber) return

  isSendingOTP.value = true
  otpError.value = ''

  try {
    const response = await phoneVerificationApi.sendVerificationCode({
      phoneNumber: invitation.value.inviteePhoneNumber,
      countryCode: '+98',
    })

    if (response.success) {
      registrationStep.value = 'otp'
      startOTPTimer()
    } else {
      const errorMessage = typeof response.error === 'string' ? response.error : response.error?.message
      otpError.value = errorMessage || 'خطا در ارسال کد. لطفاً دوباره تلاش کنید.'
    }
  } catch (err) {
    console.error('Error sending OTP:', err)
    otpError.value = 'خطا در ارسال کد. لطفاً دوباره تلاش کنید.'
  } finally {
    isSendingOTP.value = false
  }
}

function startOTPTimer() {
  canResendOTP.value = false
  otpTimer.value = 120

  if (otpTimerInterval) {
    clearInterval(otpTimerInterval)
  }

  otpTimerInterval = window.setInterval(() => {
    otpTimer.value--
    if (otpTimer.value <= 0) {
      canResendOTP.value = true
      if (otpTimerInterval) {
        clearInterval(otpTimerInterval)
        otpTimerInterval = null
      }
    }
  }, 1000)
}

function handleOTPComplete(code: string) {
  otpCode.value = code
  otpError.value = ''
}

async function verifyOTPAndAccept() {
  if (otpCode.value.length !== 6 || !invitation.value) return

  isVerifyingOTP.value = true
  otpError.value = ''

  try {
    // Call the hierarchy service directly since store method doesn't exist yet
    const response = await hierarchyService.acceptInvitationWithRegistration({
      invitationId: invitationId.value,
      organizationId: invitation.value.organizationId,
      phoneNumber: invitation.value.inviteePhoneNumber,
      firstName: registrationForm.firstName,
      lastName: registrationForm.lastName,
      email: registrationForm.email || undefined,
      otpCode: otpCode.value,
      cloneServices: registrationForm.cloneServices,
      cloneWorkingHours: registrationForm.cloneWorkingHours,
      cloneGallery: registrationForm.cloneGallery,
    })

    console.log('acceptInvitationWithRegistration response:', response)

    // The service already unwraps the response, so response is directly the data object
    if (response && response.userId && response.accessToken) {
      // Store authentication tokens
      authStore.setToken(response.accessToken)
      authStore.setRefreshToken(response.refreshToken)

      // Create and set user object from registration data
      const userData: User = {
        id: response.userId,
        email: registrationForm.email || `${invitation.value.inviteePhoneNumber.replace('+', '')}@booksy.temp`,
        firstName: registrationForm.firstName,
        lastName: registrationForm.lastName,
        fullName: `${registrationForm.firstName} ${registrationForm.lastName}`,
        phoneNumber: invitation.value.inviteePhoneNumber,
        roles: ['Provider', 'Staff'],
        userType: UserType.Provider,
        status: UserStatus.Active,
        emailVerified: !!registrationForm.email,
        phoneVerified: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        lastModifiedAt: new Date().toISOString(),
        profile: {
          firstName: registrationForm.firstName,
          lastName: registrationForm.lastName,
          phoneNumber: invitation.value.inviteePhoneNumber,
          avatarUrl: undefined,
        },
        preferences: {
          theme: 'light',
          notifications: {
            email: true,
            sms: true,
            push: true,
            marketing: false,
            bookingReminders: true,
            promotions: false,
            newsletter: false,
          },
          language: 'fa',
          timezone: 'Asia/Tehran',
          currency: 'IRR',
          dateFormat: 'YYYY-MM-DD',
          timeFormat: '24h',
          notificationSettings: {
            emailNotifications: true,
            smsNotifications: true,
            pushNotifications: true,
            appointmentReminders: true,
            promotionalEmails: false,
          },
          privacySettings: undefined,
        },
        metadata: {
          totalBookings: 0,
          completedBookings: 0,
          cancelledBookings: 0,
          noShows: 0,
          favoriteProviders: [],
          lastActivityAt: new Date().toISOString(),
        },
      }
      authStore.setUser(userData)

      // Update invitation status
      invitation.value.status = InvitationStatus.Accepted

      // Build success message with cloning details
      const orgName = invitation.value.organizationName
      const clonedItems: string[] = []

      if (response.clonedServicesCount > 0) {
        clonedItems.push(`${response.clonedServicesCount} خدمت`)
      }
      if (response.clonedWorkingHoursCount > 0) {
        clonedItems.push(`${response.clonedWorkingHoursCount} ساعت کاری`)
      }
      if (response.clonedGalleryCount > 0) {
        clonedItems.push(`${response.clonedGalleryCount} تصویر`)
      }

      const successMessage = clonedItems.length > 0
        ? `به تیم ${orgName} خوش آمدید! ${clonedItems.join(' و ')} کپی شد.`
        : `به تیم ${orgName} خوش آمدید!`

      // Show success message using toast (imported from useToast composable)
      toast.success('موفقیت', successMessage)

      // Redirect to dashboard
      setTimeout(() => {
        router.push('/provider/dashboard')
      }, 2000)
    } else {
      otpError.value = 'خطا در تأیید کد و پذیرش دعوت'
    }
  } catch (err) {
    console.error('Error verifying OTP and accepting invitation:', err)
    const error = err as { response?: { data?: { message?: string } }; message?: string }
    otpError.value = error.response?.data?.message || error.message || 'خطا در تأیید کد'
  } finally {
    isVerifyingOTP.value = false
  }
}

function formatPhone(phoneNumber?: string): string {
  if (!phoneNumber) return ''

  // Remove +98 prefix if present
  const cleaned = phoneNumber.replace(/^\+98/, '')

  // Format as: 0912 XXX XXXX
  if (cleaned.length === 10 && cleaned.startsWith('9')) {
    return `0${cleaned.substring(0, 3)} ${cleaned.substring(3, 6)} ${cleaned.substring(6)}`
  }

  return phoneNumber
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
  .profile-completion-section,
  .quick-registration-section {
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

  // Quick Registration Section
  .quick-registration-section {
    .invitation-preview {
      text-align: center;

      .invitation-text {
        color: #4a5568;
        font-size: 1rem;
        line-height: 1.6;
        margin-bottom: 2rem;

        strong {
          color: #1a202c;
          font-weight: 600;
        }
      }
    }

    .registration-form,
    .otp-verification {
      max-width: 500px;
      margin: 0 auto;

      .form-title,
      .otp-title {
        font-size: 1.25rem;
        color: #1a202c;
        margin-bottom: 0.5rem;
        font-weight: 600;
        text-align: center;
      }

      .otp-description {
        text-align: center;
        color: #718096;
        margin-bottom: 2rem;
        line-height: 1.6;

        strong {
          color: #1a202c;
        }
      }

      .form-group {
        margin-bottom: 1.5rem;

        .form-label {
          display: block;
          font-weight: 500;
          margin-bottom: 0.5rem;
          color: #2d3748;
          font-size: 0.95rem;

          .required {
            color: #f56565;
            margin-right: 0.25rem;
          }
        }

        .form-input {
          width: 100%;
          padding: 0.75rem 1rem;
          border: 1px solid #cbd5e0;
          border-radius: 8px;
          font-size: 1rem;
          transition: all 0.2s;

          &:focus {
            outline: none;
            border-color: #667eea;
            box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
          }

          &.form-input-error {
            border-color: #f56565;
          }

          &.form-input-disabled {
            background-color: #f7fafc;
            cursor: not-allowed;
            color: #718096;
          }
        }

        .form-error {
          display: block;
          color: #f56565;
          font-size: 0.875rem;
          margin-top: 0.25rem;
        }

        .form-hint {
          display: block;
          color: #a0aec0;
          font-size: 0.875rem;
          margin-top: 0.25rem;
        }
      }

      .form-row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1rem;

        @media (max-width: 640px) {
          grid-template-columns: 1fr;
        }
      }

      .cloning-options {
        margin-bottom: 1.5rem;
        padding: 1rem;
        background: #f7fafc;
        border-radius: 8px;

        .cloning-title {
          font-size: 1rem;
          font-weight: 600;
          color: #2d3748;
          margin-bottom: 0.75rem;
        }

        .checkbox-group {
          display: flex;
          flex-direction: column;
          gap: 0.75rem;
          margin-bottom: 0.75rem;

          .checkbox-label {
            display: flex;
            align-items: center;
            gap: 0.5rem;
            color: #4a5568;
            cursor: pointer;

            input[type='checkbox'] {
              width: 1.125rem;
              height: 1.125rem;
              border-radius: 4px;
              border: 1px solid #cbd5e0;
              cursor: pointer;

              &:checked {
                background-color: #667eea;
                border-color: #667eea;
              }

              &:disabled {
                cursor: not-allowed;
                opacity: 0.6;
              }
            }
          }
        }
      }

      .otp-actions {
        text-align: center;
        margin: 1.5rem 0;

        .resend-button {
          background: none;
          border: none;
          color: #667eea;
          font-weight: 500;
          cursor: pointer;
          padding: 0.5rem 1rem;
          border-radius: 4px;
          transition: all 0.2s;

          &:hover:not(:disabled) {
            background-color: #f7fafc;
          }

          &:disabled {
            opacity: 0.5;
            cursor: not-allowed;
          }
        }

        .timer {
          color: #718096;
          font-size: 0.95rem;
        }
      }

      .back-button {
        display: block;
        width: 100%;
        margin-top: 1rem;
        padding: 0.75rem;
        background: none;
        border: 1px solid #e2e8f0;
        border-radius: 8px;
        color: #4a5568;
        font-weight: 500;
        cursor: pointer;
        transition: all 0.2s;

        &:hover {
          background-color: #f7fafc;
          border-color: #cbd5e0;
        }
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
