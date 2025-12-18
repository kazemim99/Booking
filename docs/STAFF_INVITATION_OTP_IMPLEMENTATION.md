# Staff Invitation OTP-Based Acceptance Implementation Guide

## Overview

This document provides a comprehensive guide for implementing the OTP-based staff invitation acceptance flow with automatic profile cloning from the parent organization.

**Last Updated:** 2025-11-27
**Status:** Ready for Implementation
**Estimated Effort:** 2-3 days

---

## Table of Contents

- [Requirements Summary](#requirements-summary)
- [Architecture Overview](#architecture-overview)
- [Frontend Implementation](#frontend-implementation)
- [Backend Implementation](#backend-implementation)
- [Database Changes](#database-changes)
- [API Endpoints](#api-endpoints)
- [Security Considerations](#security-considerations)
- [Testing Strategy](#testing-strategy)
- [Implementation Checklist](#implementation-checklist)
- [Edge Cases & Error Handling](#edge-cases--error-handling)

---

## Requirements Summary

### User Story

**As an** unregistered individual who received a staff invitation SMS
**I want to** accept the invitation and register using only an OTP code (no password)
**So that** I can quickly join the organization with a pre-configured profile

### Key Requirements

1. ✅ **No Password Required** - Users authenticate via OTP only
2. ✅ **Automatic Profile Cloning** - Clone services, hours, gallery, location from organization
3. ✅ **Required Fields in Invitation** - Organization must provide firstName + lastName when sending invitation
4. ✅ **Phone Verification** - Always verify phone number via OTP before account creation
5. ✅ **Individual Profile Image** - Staff uploads their own profile photo (not cloned)
6. ✅ **Gallery Cloning** - All organization gallery images are cloned to individual
7. ✅ **Post-Registration Customization** - Staff can edit all cloned data later

### What Gets Cloned vs What Doesn't

| Data Field | Cloned? | Editable Later? | Notes |
|------------|---------|-----------------|-------|
| **Services** | ✅ Yes | ✅ Yes | All organization services |
| **Service Prices** | ✅ Yes | ⚠️ Limited | Via multiplier system (future) |
| **Working Hours** | ✅ Yes | ✅ Yes | Can customize schedule |
| **Gallery Images** | ✅ Yes | ✅ Yes | Org portfolio + can add own |
| **Location** | ✅ Yes | ❌ No | Works at org location |
| **Category** | ✅ Yes | ❌ No | Must match org category |
| **Business Type** | ✅ Yes | ❌ No | Part of organization |
| **Profile Image** | ❌ No | ✅ Yes | Individual uploads own |
| **Bio/Description** | ❌ No | ✅ Yes | Individual writes own |
| **Avatar Photo** | ❌ No | ✅ Yes | Personal headshot |
| **First/Last Name** | ❌ No* | ✅ Yes | *Pre-filled from invitation |
| **Phone Number** | ❌ No* | ❌ No | *From invitation target |
| **Email** | ❌ No | ✅ Yes | Optional, personal |

---

## Architecture Overview

### Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│           Staff Invitation Acceptance Flow (OTP-Based)          │
└─────────────────────────────────────────────────────────────────┘

1. Organization sends invitation
   - Provides: phoneNumber, firstName, lastName, optional message
   ↓
2. Backend creates ProviderInvitation entity
   ↓
3. InvitationSentEvent raised
   ↓
4. SMS sent with invitation link
   ↓
5. Staff clicks link → AcceptInvitationView loads
   ↓
6. Check: Is user already registered?

   ┌─ YES (phone exists in system) ─┐
   │  - Show login option            │
   │  - Send OTP for login           │
   │  - Accept invitation            │
   │  - Redirect to dashboard        │
   └─────────────────────────────────┘

   ┌─ NO (new user) ────────────────┐
   │  Step 1: Show invitation preview│
   │  Step 2: Fill registration form │
   │    - firstName (pre-filled)     │
   │    - lastName (pre-filled)      │
   │    - email (optional)           │
   │    - Clone options (auto-check) │
   │  Step 3: Send OTP               │
   │  Step 4: Verify OTP             │
   │  Step 5: Create account +       │
   │    - Clone services             │
   │    - Clone working hours        │
   │    - Clone gallery images       │
   │    - Clone location             │
   │    - Accept invitation          │
   │  Step 6: Redirect to dashboard  │
   └─────────────────────────────────┘
```

### Component Architecture

```
Frontend:
├── AcceptInvitationView.vue (UPDATED)
│   ├── registrationStep: 'preview' | 'form' | 'otp'
│   ├── registrationForm: { firstName, lastName, email, clone options }
│   ├── OTPInput component
│   └── Methods: sendOTP(), verifyOTP(), acceptWithCloning()
│
├── InviteStaffModal.vue (UPDATED)
│   ├── firstName field (NEW - required)
│   ├── lastName field (NEW - required)
│   ├── email field (NEW - optional)
│   └── phoneNumber (existing)
│
└── OTPInput.vue (EXISTS - already implemented)

Backend:
├── Commands/
│   ├── SendInvitationCommand (UPDATED)
│   │   └── Add: FirstName, LastName, Email
│   │
│   └── AcceptInvitationWithCloningCommand (NEW)
│       ├── InvitationId
│       ├── PhoneNumber
│       ├── OTPCode
│       ├── FirstName, LastName, Email
│       └── Clone options
│
├── EventHandlers/
│   └── InvitationSentNotificationHandler (EXISTING)
│
├── Services/
│   ├── OTPService (NEW or use existing)
│   └── ProfileCloningService (NEW)
│
└── Domain/
    └── ProviderInvitation aggregate (UPDATED)
        └── Add: FirstName, LastName, Email fields
```

---

## Frontend Implementation

### Step 1: Update InviteStaffModal.vue

**File:** `booksy-frontend/src/modules/provider/components/staff/InviteStaffModal.vue`

**Changes Required:**

1. Add firstName and lastName fields (required)
2. Add email field (optional)
3. Split inviteeName into firstName/lastName
4. Update validation logic
5. Update request payload

**Code Changes:**

```vue
<!-- InviteStaffModal.vue -->
<template>
  <div class="modal-body">
    <form @submit.prevent="handleSubmit">
      <!-- Phone Number (EXISTING - keep as is) -->
      <div class="form-group">
        <label for="phoneNumber" class="form-label">
          شماره موبایل <span class="required">*</span>
        </label>
        <!-- ... existing phone input ... -->
      </div>

      <!-- First Name (NEW) -->
      <div class="form-group">
        <label for="firstName" class="form-label">
          نام <span class="required">*</span>
        </label>
        <input
          id="firstName"
          v-model="formData.firstName"
          type="text"
          class="form-input"
          :class="{ 'form-input-error': errors.firstName }"
          placeholder="مثال: رضا"
          @blur="validateField('firstName')"
          required
        />
        <span v-if="errors.firstName" class="form-error">
          {{ errors.firstName }}
        </span>
      </div>

      <!-- Last Name (NEW) -->
      <div class="form-group">
        <label for="lastName" class="form-label">
          نام خانوادگی <span class="required">*</span>
        </label>
        <input
          id="lastName"
          v-model="formData.lastName"
          type="text"
          class="form-input"
          :class="{ 'form-input-error': errors.lastName }"
          placeholder="مثال: احمدی"
          @blur="validateField('lastName')"
          required
        />
        <span v-if="errors.lastName" class="form-error">
          {{ errors.lastName }}
        </span>
      </div>

      <!-- Email (NEW - Optional) -->
      <div class="form-group">
        <label for="email" class="form-label">
          ایمیل (اختیاری)
        </label>
        <input
          id="email"
          v-model="formData.email"
          type="email"
          class="form-input"
          :class="{ 'form-input-error': errors.email }"
          placeholder="example@email.com"
          dir="ltr"
          @blur="validateField('email')"
        />
        <span v-if="errors.email" class="form-error">
          {{ errors.email }}
        </span>
        <span v-else class="form-hint">
          ایمیل برای ارتباط و اطلاع‌رسانی استفاده می‌شود
        </span>
      </div>

      <!-- Message (EXISTING - keep as is) -->
      <div class="form-group">
        <label for="message" class="form-label">پیام دعوت (اختیاری)</label>
        <!-- ... existing message textarea ... -->
      </div>

      <!-- Info Box (UPDATED) -->
      <div class="info-box">
        <i class="icon-info-circle"></i>
        <div class="info-content">
          <h4 class="info-title">نحوه عملکرد دعوت:</h4>
          <ul class="info-list">
            <li>یک پیامک حاوی لینک دعوت به شماره موبایل ارسال می‌شود</li>
            <li>کارمند با تأیید شماره موبایل (کد OTP) می‌تواند دعوت را بپذیرد</li>
            <li>پروفایل کارمند با کپی اطلاعات سازمان ساخته می‌شود</li>
            <li>کارمند می‌تواند بعداً پروفایل خود را ویرایش کند</li>
            <li>دعوت پس از 7 روز منقضی می‌شود</li>
          </ul>
        </div>
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
// ... existing imports ...

const formData = reactive({
  phoneNumber: '',
  firstName: '',      // NEW
  lastName: '',       // NEW
  email: '',          // NEW
  message: '',
})

const errors = reactive({
  phoneNumber: '',
  firstName: '',      // NEW
  lastName: '',       // NEW
  email: '',          // NEW
  message: '',
})

// Computed: Form validity
const isFormValid = computed(() => {
  return (
    formData.phoneNumber.length === 10 &&
    formData.firstName.trim() !== '' &&    // NEW
    formData.lastName.trim() !== '' &&     // NEW
    !errors.phoneNumber &&
    !errors.firstName &&
    !errors.lastName &&
    !errors.email &&
    !isSubmitting.value
  )
})

// Validation methods
function validateField(field: keyof typeof formData): void {
  errors[field] = ''

  if (field === 'phoneNumber') {
    // ... existing phone validation ...
  } else if (field === 'firstName') {
    if (!formData.firstName.trim()) {
      errors.firstName = 'نام الزامی است'
    } else if (formData.firstName.trim().length < 2) {
      errors.firstName = 'نام باید حداقل 2 کاراکتر باشد'
    }
  } else if (field === 'lastName') {
    if (!formData.lastName.trim()) {
      errors.lastName = 'نام خانوادگی الزامی است'
    } else if (formData.lastName.trim().length < 2) {
      errors.lastName = 'نام خانوادگی باید حداقل 2 کاراکتر باشد'
    }
  } else if (field === 'email') {
    if (formData.email && !isValidEmail(formData.email)) {
      errors.email = 'فرمت ایمیل نامعتبر است'
    }
  }
}

function isValidEmail(email: string): boolean {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email)
}

function validateForm(): boolean {
  validateField('phoneNumber')
  validateField('firstName')
  validateField('lastName')
  if (formData.email) {
    validateField('email')
  }

  return !errors.phoneNumber &&
         !errors.firstName &&
         !errors.lastName &&
         !errors.email
}

async function handleSubmit(): Promise<void> {
  if (!validateForm() || isSubmitting.value) return

  if (!props.organizationId || props.organizationId.trim() === '') {
    toast.error('خطا', 'شناسه سازمان نامعتبر است. لطفاً دوباره وارد شوید.')
    return
  }

  isSubmitting.value = true

  try {
    const request: SendInvitationRequest = {
      organizationId: props.organizationId,
      inviteePhoneNumber: `+98${formData.phoneNumber}`,
      inviteeFirstName: formData.firstName.trim(),    // NEW
      inviteeLastName: formData.lastName.trim(),      // NEW
      inviteeEmail: formData.email.trim() || undefined, // NEW
      message: formData.message || undefined,
    }

    await hierarchyStore.sendInvitation(props.organizationId, request)

    toast.success('موفقیت', 'دعوت با موفقیت ارسال شد')
    emit('invited')
    handleClose()
  } catch (error) {
    console.error('Error sending invitation:', error)
    toast.error('خطا', 'خطا در ارسال دعوت. لطفاً دوباره تلاش کنید.')
  } finally {
    isSubmitting.value = false
  }
}
</script>
```

---

### Step 2: Update AcceptInvitationView.vue

**File:** `booksy-frontend/src/modules/provider/views/invitation/AcceptInvitationView.vue`

**Note:** Frontend changes are already partially implemented (see system reminder). Need to add:

1. OTP sending logic
2. OTP verification logic
3. Integration with backend cloning API

**Code to Add:**

```typescript
<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useHierarchyStore } from '../../stores/hierarchy.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useToast } from '@/core/composables/useToast'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import OTPInput from '@/shared/components/ui/OTPInput.vue'
import type { ProviderInvitation } from '../../types/hierarchy.types'
import { InvitationStatus } from '../../types/hierarchy.types'

const route = useRoute()
const router = useRouter()
const hierarchyStore = useHierarchyStore()
const authStore = useAuthStore()
const toast = useToast()

// Existing state
const invitationId = ref<string>('')
const invitation = ref<ProviderInvitation | null>(null)
const isLoading = ref(true)
const error = ref<string | null>(null)

// New state for OTP registration
const registrationStep = ref<'preview' | 'form' | 'otp'>('preview')
const registrationForm = ref({
  firstName: '',
  lastName: '',
  email: '',
  cloneServices: true,
  cloneWorkingHours: true,
  cloneGallery: true,
})
const registrationErrors = ref({
  firstName: '',
  lastName: '',
  email: '',
})
const otpCode = ref('')
const otpError = ref('')
const otpTimer = ref(120) // 2 minutes
const canResendOTP = ref(false)
const isSendingOTP = ref(false)
const isVerifyingOTP = ref(false)
let otpTimerInterval: NodeJS.Timeout | null = null

// Computed
const isUserRegistered = computed(() => authStore.isAuthenticated)
const isExpired = computed(() => {
  if (!invitation.value) return false
  return invitation.value.status === InvitationStatus.Expired ||
    new Date(invitation.value.expiresAt) < new Date()
})
const isAccepted = computed(() => invitation.value?.status === InvitationStatus.Accepted)

const isRegistrationFormValid = computed(() => {
  return (
    registrationForm.value.firstName.trim() !== '' &&
    registrationForm.value.lastName.trim() !== '' &&
    (!registrationForm.value.email || isValidEmail(registrationForm.value.email))
  )
})

// Lifecycle
onMounted(async () => {
  await loadInvitation()
})

// Methods
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
    invitation.value = await hierarchyStore.getInvitation(orgId, id)

    // Pre-fill form with invitation data
    if (invitation.value.inviteeName) {
      const nameParts = invitation.value.inviteeName.split(' ')
      registrationForm.value.firstName = nameParts[0] || ''
      registrationForm.value.lastName = nameParts.slice(1).join(' ') || ''
    }

  } catch (err) {
    error.value = err instanceof Error ? err.message : 'خطا در بارگذاری دعوت'
    console.error('Error loading invitation:', err)
  } finally {
    isLoading.value = false
  }
}

function startQuickRegistration() {
  registrationStep.value = 'form'
}

function validateRegistrationField(field: 'firstName' | 'lastName' | 'email') {
  registrationErrors.value[field] = ''

  if (field === 'firstName') {
    if (!registrationForm.value.firstName.trim()) {
      registrationErrors.value.firstName = 'نام الزامی است'
    } else if (registrationForm.value.firstName.trim().length < 2) {
      registrationErrors.value.firstName = 'نام باید حداقل 2 کاراکتر باشد'
    }
  } else if (field === 'lastName') {
    if (!registrationForm.value.lastName.trim()) {
      registrationErrors.value.lastName = 'نام خانوادگی الزامی است'
    } else if (registrationForm.value.lastName.trim().length < 2) {
      registrationErrors.value.lastName = 'نام خانوادگی باید حداقل 2 کاراکتر باشد'
    }
  } else if (field === 'email') {
    if (registrationForm.value.email && !isValidEmail(registrationForm.value.email)) {
      registrationErrors.value.email = 'فرمت ایمیل نامعتبر است'
    }
  }
}

function isValidEmail(email: string): boolean {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email)
}

async function sendOTPForInvitation() {
  if (!invitation.value) return

  isSendingOTP.value = true
  otpError.value = ''

  try {
    // Call backend to send OTP
    await hierarchyStore.sendInvitationOTP({
      invitationId: invitationId.value,
      phoneNumber: invitation.value.inviteePhoneNumber,
    })

    registrationStep.value = 'otp'
    startOTPTimer()
    toast.success('کد تأیید به شماره موبایل شما ارسال شد')
  } catch (error) {
    console.error('Error sending OTP:', error)
    toast.error('خطا در ارسال کد تأیید. لطفاً دوباره تلاش کنید.')
  } finally {
    isSendingOTP.value = false
  }
}

function startOTPTimer() {
  otpTimer.value = 120 // 2 minutes
  canResendOTP.value = false

  if (otpTimerInterval) {
    clearInterval(otpTimerInterval)
  }

  otpTimerInterval = setInterval(() => {
    otpTimer.value--
    if (otpTimer.value <= 0) {
      canResendOTP.value = true
      if (otpTimerInterval) {
        clearInterval(otpTimerInterval)
      }
    }
  }, 1000)
}

function handleOTPComplete(code: string) {
  otpCode.value = code
  // Auto-verify when all digits entered
  // verifyOTPAndAccept()
}

async function verifyOTPAndAccept() {
  if (!invitation.value || otpCode.value.length !== 6) return

  isVerifyingOTP.value = true
  otpError.value = ''

  try {
    // Call backend to verify OTP and accept invitation with cloning
    const result = await hierarchyStore.acceptInvitationWithCloning({
      invitationId: invitationId.value,
      organizationId: invitation.value.organizationId,
      phoneNumber: invitation.value.inviteePhoneNumber,
      otpCode: otpCode.value,
      firstName: registrationForm.value.firstName.trim(),
      lastName: registrationForm.value.lastName.trim(),
      email: registrationForm.value.email.trim() || undefined,
      cloneServices: registrationForm.value.cloneServices,
      cloneWorkingHours: registrationForm.value.cloneWorkingHours,
      cloneGallery: registrationForm.value.cloneGallery,
    })

    // Show success message
    toast.success(
      `به تیم ${invitation.value.organizationName} خوش آمدید!`,
      `پروفایل شما با ${result.clonedServicesCount} خدمت و ${result.clonedWorkingHoursCount} ساعت کاری ساخته شد.`
    )

    // Redirect to dashboard
    setTimeout(() => {
      router.push('/provider/dashboard')
    }, 2000)

  } catch (error: any) {
    console.error('Error verifying OTP:', error)

    if (error.response?.status === 400) {
      otpError.value = 'کد تأیید نامعتبر است'
    } else {
      toast.error('خطا در تأیید کد. لطفاً دوباره تلاش کنید.')
    }
  } finally {
    isVerifyingOTP.value = false
  }
}

function formatPhone(phoneNumber?: string): string {
  if (!phoneNumber) return ''
  // +989123456789 -> 0912 XXX 4789
  const cleaned = phoneNumber.replace('+98', '0')
  if (cleaned.length === 11) {
    return `${cleaned.slice(0, 4)} XXX ${cleaned.slice(-4)}`
  }
  return cleaned
}

// Cleanup on unmount
onBeforeUnmount(() => {
  if (otpTimerInterval) {
    clearInterval(otpTimerInterval)
  }
})

// ... rest of existing methods (handleAccept, handleReject, etc.) ...
</script>
```

**Styling to Add:**

```scss
<style scoped lang="scss">
// ... existing styles ...

// New styles for registration flow
.quick-registration-section {
  border-top: 1px solid #e2e8f0;
  padding-top: 2rem;
}

.invitation-preview {
  text-align: center;

  .invitation-text {
    font-size: 1.1rem;
    color: #4a5568;
    margin-bottom: 2rem;
    line-height: 1.6;

    strong {
      color: #7c3aed;
      font-weight: 600;
    }
  }
}

.registration-form {
  .form-title {
    font-size: 1.25rem;
    font-weight: 600;
    color: #1a202c;
    margin-bottom: 1.5rem;
  }

  .form-row {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;

    @media (max-width: 640px) {
      grid-template-columns: 1fr;
    }
  }

  .form-group {
    margin-bottom: 1.5rem;
  }

  .form-label {
    display: block;
    font-size: 0.95rem;
    font-weight: 600;
    color: #374151;
    margin-bottom: 0.5rem;

    .required {
      color: #dc2626;
    }
  }

  .form-input {
    width: 100%;
    padding: 0.75rem 1rem;
    border: 1px solid #d1d5db;
    border-radius: 8px;
    font-size: 0.95rem;
    transition: all 0.2s;

    &:focus {
      outline: none;
      border-color: #7c3aed;
      box-shadow: 0 0 0 3px rgba(124, 58, 237, 0.1);
    }

    &-disabled {
      background-color: #f9fafb;
      color: #6b7280;
      cursor: not-allowed;
    }

    &-error {
      border-color: #dc2626;

      &:focus {
        box-shadow: 0 0 0 3px rgba(220, 38, 38, 0.1);
      }
    }
  }

  .form-hint {
    display: block;
    font-size: 0.875rem;
    color: #6b7280;
    margin-top: 0.25rem;
  }

  .form-error {
    display: block;
    font-size: 0.875rem;
    color: #dc2626;
    margin-top: 0.25rem;
  }

  .cloning-options {
    background: #f7fafc;
    border: 1px solid #e2e8f0;
    border-radius: 8px;
    padding: 1.25rem;
    margin-bottom: 1.5rem;

    .cloning-title {
      font-size: 1rem;
      font-weight: 600;
      color: #1a202c;
      margin-bottom: 0.75rem;
    }

    .checkbox-group {
      margin-bottom: 0.75rem;
    }

    .checkbox-label {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.5rem 0;
      cursor: pointer;

      input[type="checkbox"] {
        width: 18px;
        height: 18px;
        cursor: pointer;

        &:disabled {
          cursor: not-allowed;
          opacity: 0.6;
        }
      }

      span {
        font-size: 0.95rem;
        color: #374151;
      }
    }
  }
}

.otp-verification {
  text-align: center;

  .otp-title {
    font-size: 1.25rem;
    font-weight: 600;
    color: #1a202c;
    margin-bottom: 1rem;
  }

  .otp-description {
    font-size: 0.95rem;
    color: #6b7280;
    margin-bottom: 2rem;

    strong {
      color: #1a202c;
      font-weight: 600;
    }
  }

  .otp-actions {
    margin: 1.5rem 0;

    .resend-button {
      background: none;
      border: none;
      color: #7c3aed;
      font-size: 0.95rem;
      font-weight: 600;
      cursor: pointer;
      padding: 0.5rem 1rem;
      border-radius: 6px;
      transition: all 0.2s;

      &:hover:not(:disabled) {
        background: #f7fafc;
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .timer {
      font-size: 0.95rem;
      color: #6b7280;
    }
  }

  .back-button {
    margin-top: 1rem;
    background: none;
    border: none;
    color: #6b7280;
    font-size: 0.95rem;
    cursor: pointer;
    padding: 0.5rem 1rem;

    &:hover {
      color: #374151;
    }
  }
}
</style>
```

---

### Step 3: Update Hierarchy Store

**File:** `booksy-frontend/src/modules/provider/stores/hierarchy.store.ts`

**Add New Actions:**

```typescript
// Add to hierarchy.store.ts

interface HierarchyStoreState {
  // ... existing state ...
}

export const useHierarchyStore = defineStore('hierarchy', {
  state: (): HierarchyStoreState => ({
    // ... existing state ...
  }),

  actions: {
    // ... existing actions ...

    /**
     * Send OTP for invitation acceptance
     */
    async sendInvitationOTP(request: {
      invitationId: string
      phoneNumber: string
    }): Promise<void> {
      this.loading.invitations = true
      this.errors.invitations = undefined

      try {
        await hierarchyService.sendInvitationOTP(request)
      } catch (error: any) {
        console.error('Error sending invitation OTP:', error)
        this.errors.invitations = error.response?.data?.message || 'خطا در ارسال کد تأیید'
        throw error
      } finally {
        this.loading.invitations = false
      }
    },

    /**
     * Accept invitation with cloning (for new users)
     */
    async acceptInvitationWithCloning(request: {
      invitationId: string
      organizationId: string
      phoneNumber: string
      otpCode: string
      firstName: string
      lastName: string
      email?: string
      cloneServices: boolean
      cloneWorkingHours: boolean
      cloneGallery: boolean
    }): Promise<{
      individualProviderId: string
      organizationId: string
      organizationName: string
      servicesCloned: boolean
      workingHoursCloned: boolean
      galleryCloned: boolean
      clonedServicesCount: number
      clonedWorkingHoursCount: number
      clonedGalleryCount: number
    }> {
      this.loading.invitations = true
      this.errors.invitations = undefined

      try {
        const response = await hierarchyService.acceptInvitationWithCloning(
          request.organizationId,
          request.invitationId,
          request
        )

        // Update auth store with new user
        const authStore = useAuthStore()
        authStore.setProviderId(response.data.individualProviderId)

        return response.data
      } catch (error: any) {
        console.error('Error accepting invitation with cloning:', error)
        this.errors.invitations = error.response?.data?.message || 'خطا در پذیرش دعوت'
        throw error
      } finally {
        this.loading.invitations = false
      }
    },
  },
})
```

---

### Step 4: Update Hierarchy Service

**File:** `booksy-frontend/src/modules/provider/services/hierarchy.service.ts`

**Add New Methods:**

```typescript
// Add to hierarchy.service.ts

class HierarchyService {
  // ... existing methods ...

  /**
   * Send OTP for invitation acceptance
   */
  async sendInvitationOTP(request: {
    invitationId: string
    phoneNumber: string
  }): Promise<void> {
    const response = await api.post<void>(
      `/v1/providers/invitations/${request.invitationId}/send-otp`,
      {
        phoneNumber: request.phoneNumber,
      }
    )
    return response.data
  }

  /**
   * Accept invitation with profile cloning (new user registration)
   */
  async acceptInvitationWithCloning(
    organizationId: string,
    invitationId: string,
    request: {
      phoneNumber: string
      otpCode: string
      firstName: string
      lastName: string
      email?: string
      cloneServices: boolean
      cloneWorkingHours: boolean
      cloneGallery: boolean
    }
  ): Promise<HierarchyApiResponse<{
    individualProviderId: string
    organizationId: string
    organizationName: string
    servicesCloned: boolean
    workingHoursCloned: boolean
    galleryCloned: boolean
    clonedServicesCount: number
    clonedWorkingHoursCount: number
    clonedGalleryCount: number
  }>> {
    const response = await api.post(
      `/v1/providers/${organizationId}/hierarchy/invitations/${invitationId}/accept-with-cloning`,
      request
    )
    return response.data
  }
}

export const hierarchyService = new HierarchyService()
```

---

### Step 5: Update Types

**File:** `booksy-frontend/src/modules/provider/types/hierarchy.types.ts`

**Update Interfaces:**

```typescript
// Update SendInvitationRequest
export interface SendInvitationRequest {
  organizationId: string
  inviteePhoneNumber: string
  inviteeFirstName: string        // NEW - Required
  inviteeLastName: string         // NEW - Required
  inviteeEmail?: string           // NEW - Optional
  message?: string
  expiresInHours?: number
}

// Update ProviderInvitation
export interface ProviderInvitation {
  id: string
  organizationId: string
  organizationName: string
  organizationLogo?: string
  organizationType?: string
  inviteePhoneNumber: string
  inviteeFirstName?: string       // NEW
  inviteeLastName?: string        // NEW
  inviteeEmail?: string           // NEW
  inviteeName?: string            // DEPRECATED - use firstName + lastName
  message?: string
  status: InvitationStatus
  sentAt: Date
  expiresAt: Date
  respondedAt?: Date
  acceptedByProviderId?: string
  createdBy?: string
  createdByName?: string
}

// New: Accept invitation with cloning request
export interface AcceptInvitationWithCloningRequest {
  invitationId: string
  organizationId: string
  phoneNumber: string
  otpCode: string
  firstName: string
  lastName: string
  email?: string
  cloneServices: boolean
  cloneWorkingHours: boolean
  cloneGallery: boolean
}

// New: Accept invitation with cloning response
export interface AcceptInvitationWithCloningResponse {
  individualProviderId: string
  organizationId: string
  organizationName: string
  servicesCloned: boolean
  workingHoursCloned: boolean
  galleryCloned: boolean
  clonedServicesCount: number
  clonedWorkingHoursCount: number
  clonedGalleryCount: number
}
```

---

## Backend Implementation

### Step 1: Update Domain Model

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderInvitationAggregate/ProviderInvitation.cs`

**Add Properties:**

```csharp
public sealed class ProviderInvitation : AggregateRoot<Guid>
{
    // Existing properties
    public ProviderId OrganizationId { get; private set; }
    public PhoneNumber PhoneNumber { get; private set; }
    public string? InviteeName { get; private set; }  // DEPRECATED
    public string? Message { get; private set; }
    public InvitationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public ProviderId? AcceptedByProviderId { get; private set; }

    // NEW properties
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string? Email { get; private set; }

    // Private constructor for EF Core
    private ProviderInvitation() { }

    // Updated factory method
    public static ProviderInvitation Create(
        ProviderId organizationId,
        PhoneNumber phoneNumber,
        string firstName,
        string lastName,
        string? email = null,
        string? message = null,
        int expiryDays = 7)
    {
        Guard.Against.NullOrWhiteSpace(firstName, nameof(firstName));
        Guard.Against.NullOrWhiteSpace(lastName, nameof(lastName));

        var invitation = new ProviderInvitation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            PhoneNumber = phoneNumber,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Message = message,
            Status = InvitationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
        };

        invitation.RaiseDomainEvent(new InvitationSentEvent(
            invitation.Id,
            organizationId,
            phoneNumber.Value,
            firstName,
            lastName,
            DateTime.UtcNow));

        return invitation;
    }

    public void Accept(ProviderId individualProviderId)
    {
        if (Status != InvitationStatus.Pending)
        {
            throw new DomainValidationException("Only pending invitations can be accepted");
        }

        if (ExpiresAt < DateTime.UtcNow)
        {
            throw new DomainValidationException("Invitation has expired");
        }

        Status = InvitationStatus.Accepted;
        AcceptedByProviderId = individualProviderId;
        RespondedAt = DateTime.UtcNow;

        RaiseDomainEvent(new InvitationAcceptedEvent(
            Id,
            OrganizationId,
            individualProviderId,
            DateTime.UtcNow));
    }
}
```

---

### Step 2: Update SendInvitationCommand

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/ProviderHierarchy/SendInvitation/SendInvitationCommand.cs`

```csharp
public sealed record SendInvitationCommand(
    Guid OrganizationId,
    string PhoneNumber,
    string FirstName,            // NEW - Required
    string LastName,             // NEW - Required
    string? Email = null,        // NEW - Optional
    string? Message = null,
    Guid? IdempotencyKey = null) : ICommand<SendInvitationResult>;

public sealed record SendInvitationResult(
    Guid InvitationId,
    Guid OrganizationId,
    string OrganizationName,
    string? OrganizationLogo,
    string PhoneNumber,
    string FirstName,            // NEW
    string LastName,             // NEW
    string? Email,               // NEW
    string? Message,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string Status);
```

**Update Handler:**

```csharp
// SendInvitationCommandHandler.cs
public async Task<SendInvitationResult> Handle(
    SendInvitationCommand command,
    CancellationToken cancellationToken)
{
    // Validate organization
    var organization = await _providerRepository.GetByIdAsync(
        new ProviderId(command.OrganizationId),
        cancellationToken);

    if (organization == null)
    {
        throw new NotFoundException("Organization not found");
    }

    if (organization.HierarchyType != ProviderHierarchyType.Organization)
    {
        throw new DomainValidationException("Only organizations can send invitations");
    }

    // Check for existing pending invitation
    var existingInvitation = await _invitationRepository
        .GetPendingByPhoneNumberAsync(
            new ProviderId(command.OrganizationId),
            new PhoneNumber(command.PhoneNumber),
            cancellationToken);

    if (existingInvitation != null)
    {
        throw new DomainValidationException(
            "A pending invitation already exists for this phone number");
    }

    // Create invitation
    var invitation = ProviderInvitation.Create(
        new ProviderId(command.OrganizationId),
        new PhoneNumber(command.PhoneNumber),
        command.FirstName,
        command.LastName,
        command.Email,
        command.Message);

    await _invitationWriteRepository.SaveAsync(invitation, cancellationToken);
    await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

    return new SendInvitationResult(
        invitation.Id,
        organization.Id.Value,
        organization.BusinessName,
        organization.LogoUrl,
        invitation.PhoneNumber.Value,
        invitation.FirstName,
        invitation.LastName,
        invitation.Email,
        invitation.Message,
        invitation.CreatedAt,
        invitation.ExpiresAt,
        invitation.Status.ToString());
}
```

---

### Step 3: Create OTP Service

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Services/OTPService.cs`

```csharp
public interface IOTPService
{
    Task<string> GenerateOTPAsync(string phoneNumber, string purpose, CancellationToken cancellationToken);
    Task<bool> ValidateOTPAsync(string phoneNumber, string code, string purpose, CancellationToken cancellationToken);
    Task SendOTPAsync(string phoneNumber, string code, CancellationToken cancellationToken);
}

public class OTPService : IOTPService
{
    private readonly IDistributedCache _cache;
    private readonly ISmsNotificationService _smsService;
    private readonly ILogger<OTPService> _logger;

    public OTPService(
        IDistributedCache cache,
        ISmsNotificationService smsService,
        ILogger<OTPService> logger)
    {
        _cache = cache;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task<string> GenerateOTPAsync(
        string phoneNumber,
        string purpose,
        CancellationToken cancellationToken)
    {
        // Generate 6-digit code
        var code = new Random().Next(100000, 999999).ToString();

        // Store in cache with 2-minute expiration
        var cacheKey = $"otp:{phoneNumber}:{purpose}";
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        };

        await _cache.SetStringAsync(
            cacheKey,
            code,
            cacheOptions,
            cancellationToken);

        _logger.LogInformation(
            "Generated OTP for phone {PhoneNumber}, purpose {Purpose}",
            phoneNumber,
            purpose);

        return code;
    }

    public async Task<bool> ValidateOTPAsync(
        string phoneNumber,
        string code,
        string purpose,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"otp:{phoneNumber}:{purpose}";
        var storedCode = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (string.IsNullOrEmpty(storedCode))
        {
            _logger.LogWarning(
                "OTP validation failed: No code found for phone {PhoneNumber}",
                phoneNumber);
            return false;
        }

        if (storedCode != code)
        {
            _logger.LogWarning(
                "OTP validation failed: Invalid code for phone {PhoneNumber}",
                phoneNumber);
            return false;
        }

        // Remove OTP after successful validation
        await _cache.RemoveAsync(cacheKey, cancellationToken);

        _logger.LogInformation(
            "OTP validated successfully for phone {PhoneNumber}",
            phoneNumber);

        return true;
    }

    public async Task SendOTPAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken)
    {
        var message = $"کد تأیید شما: {code}\nاعتبار: 2 دقیقه";

        await _smsService.SendSmsAsync(
            phoneNumber,
            message,
            cancellationToken);

        _logger.LogInformation(
            "OTP sent to phone {PhoneNumber}",
            phoneNumber);
    }
}
```

---

### Step 4: Create Profile Cloning Service

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Services/ProfileCloningService.cs`

```csharp
public interface IProfileCloningService
{
    Task<ProfileCloningResult> CloneOrganizationProfileAsync(
        Guid organizationId,
        Guid individualProviderId,
        bool cloneServices,
        bool cloneWorkingHours,
        bool cloneGallery,
        CancellationToken cancellationToken);
}

public record ProfileCloningResult(
    int ServicesCloned,
    int WorkingHoursCloned,
    int GalleryImagesCloned);

public class ProfileCloningService : IProfileCloningService
{
    private readonly IProviderRepository _providerRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IBusinessHoursRepository _businessHoursRepository;
    private readonly IGalleryImageRepository _galleryRepository;
    private readonly ILogger<ProfileCloningService> _logger;

    public ProfileCloningService(
        IProviderRepository providerRepository,
        IServiceRepository serviceRepository,
        IBusinessHoursRepository businessHoursRepository,
        IGalleryImageRepository galleryRepository,
        ILogger<ProfileCloningService> logger)
    {
        _providerRepository = providerRepository;
        _serviceRepository = serviceRepository;
        _businessHoursRepository = businessHoursRepository;
        _galleryRepository = galleryRepository;
        _logger = logger;
    }

    public async Task<ProfileCloningResult> CloneOrganizationProfileAsync(
        Guid organizationId,
        Guid individualProviderId,
        bool cloneServices,
        bool cloneWorkingHours,
        bool cloneGallery,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting profile cloning from organization {OrgId} to individual {IndividualId}",
            organizationId,
            individualProviderId);

        var organization = await _providerRepository.GetByIdAsync(
            new ProviderId(organizationId),
            cancellationToken);

        if (organization == null)
        {
            throw new NotFoundException("Organization not found");
        }

        var individual = await _providerRepository.GetByIdAsync(
            new ProviderId(individualProviderId),
            cancellationToken);

        if (individual == null)
        {
            throw new NotFoundException("Individual provider not found");
        }

        int servicesCloned = 0;
        int hoursCloned = 0;
        int galleryCloned = 0;

        // Clone services
        if (cloneServices)
        {
            var orgServices = await _serviceRepository.GetByProviderIdAsync(
                new ProviderId(organizationId),
                cancellationToken);

            foreach (var service in orgServices)
            {
                var clonedService = service.CloneForProvider(new ProviderId(individualProviderId));
                await _serviceRepository.AddAsync(clonedService, cancellationToken);
                servicesCloned++;
            }

            _logger.LogInformation("Cloned {Count} services", servicesCloned);
        }

        // Clone working hours
        if (cloneWorkingHours)
        {
            var orgHours = await _businessHoursRepository.GetByProviderIdAsync(
                new ProviderId(organizationId),
                cancellationToken);

            foreach (var hours in orgHours)
            {
                var clonedHours = hours.CloneForProvider(new ProviderId(individualProviderId));
                await _businessHoursRepository.AddAsync(clonedHours, cancellationToken);
                hoursCloned++;
            }

            _logger.LogInformation("Cloned {Count} working hours", hoursCloned);
        }

        // Clone gallery images
        if (cloneGallery)
        {
            var orgGallery = await _galleryRepository.GetByProviderIdAsync(
                new ProviderId(organizationId),
                cancellationToken);

            foreach (var image in orgGallery)
            {
                var clonedImage = image.CloneForProvider(
                    new ProviderId(individualProviderId),
                    sourceProviderId: new ProviderId(organizationId));
                await _galleryRepository.AddAsync(clonedImage, cancellationToken);
                galleryCloned++;
            }

            _logger.LogInformation("Cloned {Count} gallery images", galleryCloned);
        }

        // Clone location
        individual.SetLocation(organization.Location);

        // Clone category
        individual.SetCategory(organization.Category);

        // Save individual with cloned data
        await _providerRepository.UpdateAsync(individual, cancellationToken);

        _logger.LogInformation(
            "Profile cloning completed: {Services} services, {Hours} hours, {Gallery} images",
            servicesCloned,
            hoursCloned,
            galleryCloned);

        return new ProfileCloningResult(
            servicesCloned,
            hoursCloned,
            galleryCloned);
    }
}
```

---

### Step 5: Create AcceptInvitationWithCloningCommand

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/ProviderHierarchy/AcceptInvitationWithCloning/AcceptInvitationWithCloningCommand.cs`

```csharp
public sealed record AcceptInvitationWithCloningCommand(
    Guid InvitationId,
    Guid OrganizationId,
    string PhoneNumber,
    string OTPCode,
    string FirstName,
    string LastName,
    string? Email,
    bool CloneServices,
    bool CloneWorkingHours,
    bool CloneGallery) : ICommand<AcceptInvitationWithCloningResult>;

public sealed record AcceptInvitationWithCloningResult(
    Guid IndividualProviderId,
    Guid OrganizationId,
    string OrganizationName,
    bool ServicesCloned,
    bool WorkingHoursCloned,
    bool GalleryCloned,
    int ClonedServicesCount,
    int ClonedWorkingHoursCount,
    int ClonedGalleryCount);
```

**Handler:**

```csharp
public class AcceptInvitationWithCloningCommandHandler
    : ICommandHandler<AcceptInvitationWithCloningCommand, AcceptInvitationWithCloningResult>
{
    private readonly IProviderInvitationRepository _invitationRepository;
    private readonly IProviderRepository _providerRepository;
    private readonly IUserService _userService;
    private readonly IOTPService _otpService;
    private readonly IProfileCloningService _cloningService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AcceptInvitationWithCloningCommandHandler> _logger;

    public AcceptInvitationWithCloningCommandHandler(
        IProviderInvitationRepository invitationRepository,
        IProviderRepository providerRepository,
        IUserService userService,
        IOTPService otpService,
        IProfileCloningService cloningService,
        IUnitOfWork unitOfWork,
        ILogger<AcceptInvitationWithCloningCommandHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _providerRepository = providerRepository;
        _userService = userService;
        _otpService = otpService;
        _cloningService = cloningService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AcceptInvitationWithCloningResult> Handle(
        AcceptInvitationWithCloningCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing invitation acceptance with cloning for invitation {InvitationId}",
            command.InvitationId);

        // 1. Validate OTP
        var otpValid = await _otpService.ValidateOTPAsync(
            command.PhoneNumber,
            command.OTPCode,
            "invitation_acceptance",
            cancellationToken);

        if (!otpValid)
        {
            throw new DomainValidationException("Invalid OTP code");
        }

        // 2. Load invitation
        var invitation = await _invitationRepository.GetByIdAsync(
            command.InvitationId,
            cancellationToken);

        if (invitation == null)
        {
            throw new NotFoundException("Invitation not found");
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new DomainValidationException("Invitation is not pending");
        }

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            throw new DomainValidationException("Invitation has expired");
        }

        // 3. Load organization
        var organization = await _providerRepository.GetByIdAsync(
            new ProviderId(command.OrganizationId),
            cancellationToken);

        if (organization == null)
        {
            throw new NotFoundException("Organization not found");
        }

        // 4. Create user account (no password - OTP only)
        var userId = await _userService.CreateUserWithPhoneAsync(
            command.PhoneNumber,
            command.Email,
            cancellationToken);

        _logger.LogInformation(
            "Created user account {UserId} for phone {Phone}",
            userId,
            command.PhoneNumber);

        // 5. Create individual provider
        var individual = Provider.CreateIndividual(
            firstName: command.FirstName,
            lastName: command.LastName,
            phoneNumber: new PhoneNumber(command.PhoneNumber),
            email: command.Email,
            ownerId: userId);

        await _providerRepository.AddAsync(individual, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created individual provider {ProviderId}",
            individual.Id);

        // 6. Clone profile data from organization
        var cloningResult = await _cloningService.CloneOrganizationProfileAsync(
            command.OrganizationId,
            individual.Id.Value,
            command.CloneServices,
            command.CloneWorkingHours,
            command.CloneGallery,
            cancellationToken);

        // 7. Accept invitation (create staff relationship)
        invitation.Accept(individual.Id);
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        // 8. Save all changes and publish events
        await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

        _logger.LogInformation(
            "Invitation {InvitationId} accepted successfully. Individual {IndividualId} joined organization {OrgId}",
            command.InvitationId,
            individual.Id,
            command.OrganizationId);

        return new AcceptInvitationWithCloningResult(
            IndividualProviderId: individual.Id.Value,
            OrganizationId: organization.Id.Value,
            OrganizationName: organization.BusinessName,
            ServicesCloned: command.CloneServices,
            WorkingHoursCloned: command.CloneWorkingHours,
            GalleryCloned: command.CloneGallery,
            ClonedServicesCount: cloningResult.ServicesCloned,
            ClonedWorkingHoursCount: cloningResult.WorkingHoursCloned,
            ClonedGalleryCount: cloningResult.GalleryImagesCloned);
    }
}
```

---

### Step 6: Create SendInvitationOTPCommand

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/ProviderHierarchy/SendInvitationOTP/SendInvitationOTPCommand.cs`

```csharp
public sealed record SendInvitationOTPCommand(
    Guid InvitationId,
    string PhoneNumber) : ICommand;

public class SendInvitationOTPCommandHandler : ICommandHandler<SendInvitationOTPCommand>
{
    private readonly IProviderInvitationRepository _invitationRepository;
    private readonly IOTPService _otpService;
    private readonly ILogger<SendInvitationOTPCommandHandler> _logger;

    public SendInvitationOTPCommandHandler(
        IProviderInvitationRepository invitationRepository,
        IOTPService otpService,
        ILogger<SendInvitationOTPCommandHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _otpService = otpService;
        _logger = logger;
    }

    public async Task Handle(
        SendInvitationOTPCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sending OTP for invitation {InvitationId}",
            command.InvitationId);

        // Validate invitation exists and is pending
        var invitation = await _invitationRepository.GetByIdAsync(
            command.InvitationId,
            cancellationToken);

        if (invitation == null)
        {
            throw new NotFoundException("Invitation not found");
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new DomainValidationException("Invitation is not pending");
        }

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            throw new DomainValidationException("Invitation has expired");
        }

        // Generate and send OTP
        var otpCode = await _otpService.GenerateOTPAsync(
            command.PhoneNumber,
            "invitation_acceptance",
            cancellationToken);

        await _otpService.SendOTPAsync(
            command.PhoneNumber,
            otpCode,
            cancellationToken);

        _logger.LogInformation(
            "OTP sent successfully for invitation {InvitationId}",
            command.InvitationId);
    }
}
```

---

### Step 7: Update API Controller

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/ProviderHierarchyController.cs`

**Add New Endpoints:**

```csharp
[ApiController]
[Route("api/v1/providers")]
public class ProviderHierarchyController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProviderHierarchyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ... existing endpoints ...

    /// <summary>
    /// Send OTP for invitation acceptance
    /// </summary>
    [HttpPost("invitations/{invitationId}/send-otp")]
    [AllowAnonymous]
    [RateLimit(MaxRequests = 3, WindowMinutes = 10)]
    public async Task<IActionResult> SendInvitationOTP(
        [FromRoute] Guid invitationId,
        [FromBody] SendInvitationOTPRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SendInvitationOTPCommand(
            invitationId,
            request.PhoneNumber);

        await _mediator.Send(command, cancellationToken);

        return Ok(new { message = "OTP sent successfully" });
    }

    /// <summary>
    /// Accept invitation with profile cloning (for new users)
    /// </summary>
    [HttpPost("{providerId}/hierarchy/invitations/{invitationId}/accept-with-cloning")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvitationWithCloning(
        [FromRoute] Guid providerId,
        [FromRoute] Guid invitationId,
        [FromBody] AcceptInvitationWithCloningRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AcceptInvitationWithCloningCommand(
            invitationId,
            providerId,
            request.PhoneNumber,
            request.OTPCode,
            request.FirstName,
            request.LastName,
            request.Email,
            request.CloneServices,
            request.CloneWorkingHours,
            request.CloneGallery);

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }
}

// Request DTOs
public record SendInvitationOTPRequest(string PhoneNumber);

public record AcceptInvitationWithCloningRequest(
    string PhoneNumber,
    string OTPCode,
    string FirstName,
    string LastName,
    string? Email,
    bool CloneServices,
    bool CloneWorkingHours,
    bool CloneGallery);
```

---

## Database Changes

### Migration: Add Name Fields to ProviderInvitation

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Migrations/AddNameFieldsToProviderInvitation.cs`

```csharp
public partial class AddNameFieldsToProviderInvitation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FirstName",
            table: "ProviderInvitations",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "LastName",
            table: "ProviderInvitations",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Email",
            table: "ProviderInvitations",
            type: "nvarchar(255)",
            maxLength: 255,
            nullable: true);

        // Migrate existing data: split InviteeName into FirstName and LastName
        migrationBuilder.Sql(@"
            UPDATE ProviderInvitations
            SET
                FirstName = CASE
                    WHEN CHARINDEX(' ', InviteeName) > 0
                    THEN LEFT(InviteeName, CHARINDEX(' ', InviteeName) - 1)
                    ELSE InviteeName
                END,
                LastName = CASE
                    WHEN CHARINDEX(' ', InviteeName) > 0
                    THEN SUBSTRING(InviteeName, CHARINDEX(' ', InviteeName) + 1, LEN(InviteeName))
                    ELSE ''
                END
            WHERE InviteeName IS NOT NULL AND InviteeName <> ''
        ");

        // InviteeName is now deprecated but kept for backward compatibility
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "FirstName", table: "ProviderInvitations");
        migrationBuilder.DropColumn(name: "LastName", table: "ProviderInvitations");
        migrationBuilder.DropColumn(name: "Email", table: "ProviderInvitations");
    }
}
```

---

### Add Gallery Image Source Tracking

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Entities/GalleryImage.cs`

```csharp
public class GalleryImage : Entity<Guid>
{
    public ProviderId ProviderId { get; private set; }
    public string ImageUrl { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public string? MediumUrl { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime UploadedAt { get; private set; }

    // NEW: Source tracking
    public GalleryImageSourceType SourceType { get; private set; }
    public ProviderId? SourceProviderId { get; private set; }
    public bool IsCloned { get; private set; }

    public GalleryImage CloneForProvider(
        ProviderId newProviderId,
        ProviderId? sourceProviderId = null)
    {
        return new GalleryImage
        {
            Id = Guid.NewGuid(),
            ProviderId = newProviderId,
            ImageUrl = this.ImageUrl,
            ThumbnailUrl = this.ThumbnailUrl,
            MediumUrl = this.MediumUrl,
            DisplayOrder = this.DisplayOrder,
            IsPrimary = false, // Don't clone primary flag
            UploadedAt = DateTime.UtcNow,
            SourceType = GalleryImageSourceType.Organization,
            SourceProviderId = sourceProviderId ?? this.ProviderId,
            IsCloned = true,
        };
    }
}

public enum GalleryImageSourceType
{
    Individual = 0,
    Organization = 1,
}
```

**Migration:**

```csharp
public partial class AddGalleryImageSourceTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "SourceType",
            table: "GalleryImages",
            type: "int",
            nullable: false,
            defaultValue: 0); // Individual

        migrationBuilder.AddColumn<Guid>(
            name: "SourceProviderId",
            table: "GalleryImages",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsCloned",
            table: "GalleryImages",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateIndex(
            name: "IX_GalleryImages_SourceProviderId",
            table: "GalleryImages",
            column: "SourceProviderId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_GalleryImages_SourceProviderId",
            table: "GalleryImages");

        migrationBuilder.DropColumn(name: "SourceType", table: "GalleryImages");
        migrationBuilder.DropColumn(name: "SourceProviderId", table: "GalleryImages");
        migrationBuilder.DropColumn(name: "IsCloned", table: "GalleryImages");
    }
}
```

---

## API Endpoints

### Summary

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/v1/providers/{orgId}/hierarchy/invitations` | ✅ Org Owner | Send invitation |
| GET | `/api/v1/providers/{orgId}/hierarchy/invitations/{id}` | ❌ Public | Get invitation details |
| POST | `/api/v1/providers/invitations/{id}/send-otp` | ❌ Public | Send OTP for acceptance |
| POST | `/api/v1/providers/{orgId}/hierarchy/invitations/{id}/accept-with-cloning` | ❌ Public | Accept with cloning |
| POST | `/api/v1/providers/{orgId}/hierarchy/invitations/{id}/accept` | ✅ Individual | Accept (existing user) |
| DELETE | `/api/v1/providers/{orgId}/hierarchy/invitations/{id}` | ✅ Org Owner | Cancel invitation |

---

## Security Considerations

### 1. Rate Limiting

```csharp
[RateLimit(MaxRequests = 3, WindowMinutes = 10)]
public async Task<IActionResult> SendInvitationOTP(...)
{
    // Limit: 3 OTP requests per 10 minutes per invitation
}
```

### 2. OTP Expiry

- OTP valid for 2 minutes only
- Maximum 3 verification attempts
- Auto-delete after successful verification

### 3. Invitation Security

- One-time acceptance (status check)
- 7-day expiration
- Phone verification required
- No password stored for OTP-only users

### 4. Profile Cloning Authorization

- Only clone from pending invitations
- Validate organization ownership
- Prevent duplicate acceptances
- Audit log all cloning operations

---

## Testing Strategy

### Unit Tests

```csharp
// AcceptInvitationWithCloningCommandHandlerTests.cs

[Fact]
public async Task Handle_ValidRequest_CreatesIndividualAndClonesProfile()
{
    // Arrange
    var command = new AcceptInvitationWithCloningCommand(...);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IndividualProviderId.Should().NotBeEmpty();
    result.ClonedServicesCount.Should().BeGreaterThan(0);
    result.ClonedWorkingHoursCount.Should().BeGreaterThan(0);
}

[Fact]
public async Task Handle_InvalidOTP_ThrowsValidationException()
{
    // Arrange
    _otpService.ValidateOTPAsync(...).Returns(false);
    var command = new AcceptInvitationWithCloningCommand(...);

    // Act & Assert
    await Assert.ThrowsAsync<DomainValidationException>(
        () => _handler.Handle(command, CancellationToken.None));
}

[Fact]
public async Task Handle_ExpiredInvitation_ThrowsValidationException()
{
    // Arrange
    var expiredInvitation = CreateExpiredInvitation();
    var command = new AcceptInvitationWithCloningCommand(...);

    // Act & Assert
    await Assert.ThrowsAsync<DomainValidationException>(
        () => _handler.Handle(command, CancellationToken.None));
}
```

### Integration Tests

```csharp
[Fact]
public async Task AcceptInvitationFlow_EndToEnd_CreatesStaffMember()
{
    // 1. Organization sends invitation
    var invitationId = await SendInvitation();

    // 2. Send OTP
    await SendOTP(invitationId);

    // 3. Get OTP from test SMS service
    var otpCode = await GetTestOTP();

    // 4. Accept invitation with cloning
    var result = await AcceptInvitationWithCloning(invitationId, otpCode);

    // 5. Verify individual created
    var individual = await GetProvider(result.IndividualProviderId);
    individual.Should().NotBeNull();

    // 6. Verify services cloned
    var services = await GetProviderServices(result.IndividualProviderId);
    services.Count.Should().Be(result.ClonedServicesCount);

    // 7. Verify staff relationship
    var staffMembers = await GetOrganizationStaff(organizationId);
    staffMembers.Should().Contain(s => s.IndividualId == result.IndividualProviderId);
}
```

### Manual Testing Checklist

- [ ] Organization can send invitation with firstName, lastName, email
- [ ] Invitation SMS contains correct link
- [ ] Unregistered user sees registration form with pre-filled names
- [ ] OTP is sent to correct phone number
- [ ] OTP expires after 2 minutes
- [ ] Invalid OTP shows error message
- [ ] Valid OTP creates account and accepts invitation
- [ ] Services are cloned correctly
- [ ] Working hours are cloned correctly
- [ ] Gallery images are cloned correctly
- [ ] Individual can login with OTP (no password needed)
- [ ] Individual can edit cloned profile data
- [ ] Individual dashboard shows welcome message
- [ ] Organization dashboard shows new staff member

---

## Implementation Checklist

### Phase 1: Backend Foundation (Day 1)

- [ ] Update `ProviderInvitation` domain model with firstName, lastName, email
- [ ] Create database migration for new fields
- [ ] Update `SendInvitationCommand` and handler
- [ ] Create `OTPService` implementation
- [ ] Add OTP storage (Redis/MemoryCache)
- [ ] Update SMS templates for OTP

### Phase 2: Profile Cloning (Day 1-2)

- [ ] Create `ProfileCloningService`
- [ ] Add `CloneForProvider()` methods to Service, BusinessHours, GalleryImage
- [ ] Add source tracking to GalleryImage (migration)
- [ ] Create `AcceptInvitationWithCloningCommand` and handler
- [ ] Create `SendInvitationOTPCommand` and handler
- [ ] Add API endpoints

### Phase 3: Frontend (Day 2)

- [ ] Update `InviteStaffModal` with firstName, lastName, email fields
- [ ] Update validation logic
- [ ] Update `AcceptInvitationView` with OTP flow
- [ ] Integrate `OTPInput` component
- [ ] Add registration form state management
- [ ] Update hierarchy store with new actions
- [ ] Update hierarchy service with new API calls
- [ ] Update types/interfaces
- [ ] Add styling for new UI

### Phase 4: Testing & Refinement (Day 3)

- [ ] Write unit tests for new commands
- [ ] Write integration tests for full flow
- [ ] Manual testing with real SMS
- [ ] Test OTP expiry and rate limiting
- [ ] Test profile cloning accuracy
- [ ] Test error scenarios
- [ ] Update documentation
- [ ] Code review

---

## Edge Cases & Error Handling

### 1. Invitation Already Accepted

```csharp
if (invitation.Status != InvitationStatus.Pending)
{
    throw new DomainValidationException(
        "این دعوت قبلاً پردازش شده است",
        "INVITATION_ALREADY_PROCESSED");
}
```

**Frontend:** Show message "این دعوت قبلاً پذیرفته شده است"

---

### 2. Phone Number Already Registered

```csharp
var existingUser = await _userService.GetByPhoneNumberAsync(command.PhoneNumber);
if (existingUser != null)
{
    throw new DomainValidationException(
        "این شماره قبلاً ثبت‌نام کرده است. لطفاً وارد شوید.",
        "PHONE_ALREADY_REGISTERED");
}
```

**Frontend:** Redirect to login page with message

---

### 3. OTP Expired

```csharp
if (DateTime.UtcNow > otpExpiryTime)
{
    throw new DomainValidationException(
        "کد تأیید منقضی شده است",
        "OTP_EXPIRED");
}
```

**Frontend:** Show "کد منقضی شده. درخواست کد جدید"

---

### 4. Too Many OTP Attempts

```csharp
if (attemptCount > 3)
{
    throw new DomainValidationException(
        "تعداد تلاش‌های شما تمام شده. لطفاً بعداً تلاش کنید.",
        "MAX_OTP_ATTEMPTS_EXCEEDED");
}
```

**Frontend:** Disable OTP input, show cooldown timer

---

### 5. Organization Has No Services to Clone

```csharp
if (cloneServices && orgServices.Count == 0)
{
    _logger.LogWarning("Organization {OrgId} has no services to clone", organizationId);
    // Continue without error - just log warning
}
```

**Frontend:** Show info message "سازمان خدماتی ندارد. بعداً می‌توانید اضافه کنید."

---

### 6. Network Failure During Cloning

```csharp
try
{
    await _cloningService.CloneOrganizationProfileAsync(...);
}
catch (Exception ex)
{
    // Rollback: Delete created individual and user
    await _providerRepository.DeleteAsync(individual.Id);
    await _userService.DeleteAsync(userId);

    _logger.LogError(ex, "Failed to clone profile, rolled back user creation");
    throw new ApplicationException("خطا در ساخت پروفایل. لطفاً دوباره تلاش کنید.");
}
```

**Frontend:** Show error with retry option

---

## Success Criteria

### Functional Requirements

- ✅ User can accept invitation using only OTP (no password)
- ✅ Organization provides firstName, lastName when sending invitation
- ✅ Profile is automatically cloned from organization
- ✅ Individual can edit all cloned data later
- ✅ Gallery images are cloned with source tracking
- ✅ Individual has separate profile image

### Non-Functional Requirements

- ✅ OTP sent within 5 seconds
- ✅ Profile cloning completes within 10 seconds
- ✅ Total acceptance flow < 30 seconds
- ✅ Rate limiting prevents SMS abuse
- ✅ All operations are idempotent
- ✅ Audit logs for all critical operations

### User Experience

- ✅ Clear progress indicators
- ✅ Helpful error messages in Persian
- ✅ Smooth transitions between steps
- ✅ Mobile-responsive design
- ✅ Accessible (keyboard navigation, screen readers)

---

## Rollback Plan

If critical issues are found in production:

1. **Disable new endpoints** via feature flag
2. **Fall back to manual registration** (temporarily)
3. **Fix issues** in development
4. **Deploy hotfix** with proper testing
5. **Re-enable** feature gradually

**Feature Flag:**

```csharp
if (!_featureManager.IsEnabledAsync("OTPInvitationAcceptance").Result)
{
    throw new FeatureDisabledException("This feature is temporarily disabled");
}
```

---

## Future Enhancements

1. **Selective Service Cloning** - Let organization choose which services to clone
2. **Staff Role Templates** - Pre-configured service sets (e.g., "Senior Stylist", "Junior")
3. **Price Multiplier System** - Staff can adjust prices by percentage
4. **Auto-Sync Option** - Optionally sync updates from organization
5. **Bulk Gallery Management** - Organize portfolio vs personal work
6. **QR Code Invitations** - In-person staff onboarding
7. **Email OTP Alternative** - For users without SMS access

---

## Support & Documentation

- **API Documentation:** Swagger UI at `/swagger`
- **Architecture Diagrams:** See `docs/architecture/`
- **Troubleshooting:** See [STAFF_INVITATION_FLOW.md](./STAFF_INVITATION_FLOW.md)
- **Support:** Create issue at GitHub repository

---

**Document Version:** 1.0
**Last Updated:** 2025-11-27
**Author:** Development Team
**Status:** Ready for Implementation
