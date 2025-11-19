# 📱 Phone Authentication API Migration Guide

## Overview

The phone authentication backend has been completely refactored and unified. **All frontend code MUST be updated** to use the new API endpoints.

---

## ⚠️ **BREAKING CHANGES**

### **OLD Endpoints (DEPRECATED - Will Not Work)**
```
❌ POST /api/v1/phone-verification/request
❌ POST /api/v1/phone-verification/verify
❌ POST /api/v1/phone-verification/register
```

### **NEW Endpoints (Use These)**
```
✅ POST /api/v1/auth/send-verification-code
✅ POST /api/v1/auth/customer/complete-authentication
✅ POST /api/v1/auth/provider/complete-authentication
```

---

## 🔄 **Migration Steps**

### **Step 1: Update Imports**

**Before:**
```typescript
import type {
  VerifyCodeRequest,
  VerifyCodeResponse,
  RegisterFromVerifiedPhoneRequest,
  RegisterFromVerifiedPhoneResponse,
} from '../types/phoneVerification.types'
```

**After:**
```typescript
import type {
  CompleteCustomerAuthenticationRequest,
  CompleteCustomerAuthenticationResponse,
  CompleteProviderAuthenticationRequest,
  CompleteProviderAuthenticationResponse,
} from '../types/phoneVerification.types'
```

---

### **Step 2: Update Customer Login/Registration Flow**

**Before (OLD - BROKEN):**
```typescript
// Step 1: Send OTP
await phoneVerificationApi.sendVerificationCode({
  phoneNumber: '09121234567',
  method: 'SMS',
  purpose: 'Registration',
})

// Step 2: Verify OTP
const verifyResponse = await phoneVerificationApi.verifyCode({
  verificationId: '...',
  code: '123456',
})

// Step 3: Register/Login
const authResponse = await phoneVerificationApi.registerFromVerifiedPhone({
  verificationId: '...',
  userType: 'Customer',
  firstName: 'علی',
  lastName: 'احمدی',
})
```

**After (NEW - WORKING):**
```typescript
// Step 1: Send OTP
const otpResponse = await phoneVerificationApi.sendVerificationCode({
  phoneNumber: '09121234567',
  countryCode: '+98', // Optional, defaults to +98
})

// Step 2: Complete Authentication (Verify + Login/Register in ONE step)
const authResponse = await phoneVerificationApi.completeCustomerAuthentication({
  phoneNumber: '09121234567',
  code: '123456',
  firstName: 'علی', // Optional
  lastName: 'احمدی', // Optional
  email: 'ali@example.com', // Optional
})

// authResponse contains:
// - isNewCustomer: boolean
// - userId: string
// - customerId: string
// - accessToken: string
// - refreshToken: string
// - message: string
```

---

### **Step 3: Update Provider Login/Registration Flow**

**Before (OLD - BROKEN):**
```typescript
const authResponse = await phoneVerificationApi.registerFromVerifiedPhone({
  verificationId: '...',
  userType: 'Provider',
  firstName: 'رضا',
  lastName: 'محمدی',
})
```

**After (NEW - WORKING):**
```typescript
const authResponse = await phoneVerificationApi.completeProviderAuthentication({
  phoneNumber: '09121234567',
  code: '123456',
  firstName: 'رضا', // Optional
  lastName: 'محمدی', // Optional
  email: 'reza@example.com', // Optional
})

// authResponse contains:
// - isNewProvider: boolean
// - userId: string
// - providerId: string | null
// - providerStatus: string | null
// - requiresOnboarding: boolean
// - accessToken: string
// - refreshToken: string
// - message: string

// Check if provider needs onboarding
if (authResponse.requiresOnboarding) {
  // Redirect to provider onboarding flow
  router.push('/provider/onboarding')
} else {
  // Redirect to provider dashboard
  router.push('/provider/dashboard')
}
```

---

## 🎯 **Key Differences**

### **1. Simpler Flow**

**OLD (3 steps):**
1. `sendVerificationCode()`
2. `verifyCode()`
3. `registerFromVerifiedPhone()`

**NEW (2 steps):**
1. `sendVerificationCode()`
2. `completeCustomerAuthentication()` OR `completeProviderAuthentication()`

### **2. No More UserType Parameter**

**OLD:** Needed to specify `userType: 'Customer' | 'Provider'`
**NEW:** The endpoint itself determines the user type

### **3. Automatic Customer Creation**

The new `completeCustomerAuthentication()` automatically:
- Creates User if doesn't exist
- Creates Customer aggregate if doesn't exist ✨ NEW
- Returns JWT tokens

### **4. Provider Onboarding Detection**

The new `completeProviderAuthentication()` includes:
```typescript
{
  requiresOnboarding: boolean // true if provider needs to complete setup
  providerId: string | null // null if no provider profile yet
  providerStatus: string | null // "Pending", "Active", etc.
}
```

---

## 📝 **Complete Example: Customer Login Component**

```vue
<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { phoneVerificationApi } from '@/modules/auth/api/phoneVerification.api'
import { useAuthStore } from '@/modules/auth/stores/auth.store'

const router = useRouter()
const authStore = useAuthStore()

const phoneNumber = ref('')
const otpCode = ref('')
const step = ref<'phone' | 'otp'>('phone')
const loading = ref(false)
const error = ref('')

async function sendOtp() {
  try {
    loading.value = true
    error.value = ''

    await phoneVerificationApi.sendVerificationCode({
      phoneNumber: phoneNumber.value,
      countryCode: '+98',
    })

    step.value = 'otp'
  } catch (err: any) {
    error.value = err.message || 'Failed to send OTP'
  } finally {
    loading.value = false
  }
}

async function verifyAndLogin() {
  try {
    loading.value = true
    error.value = ''

    const response = await phoneVerificationApi.completeCustomerAuthentication({
      phoneNumber: phoneNumber.value,
      code: otpCode.value,
    })

    // Store tokens
    authStore.setTokens(response.accessToken, response.refreshToken)
    authStore.setUser({
      id: response.userId,
      phoneNumber: response.phoneNumber,
      fullName: response.fullName,
    })

    // Show welcome message
    if (response.isNewCustomer) {
      // First time registration
      alert(`خوش آمدید! حساب شما با موفقیت ایجاد شد.`)
    } else {
      // Returning customer
      alert(`خوش آمدید ${response.fullName}!`)
    }

    // Redirect to customer dashboard
    router.push('/customer/dashboard')

  } catch (err: any) {
    error.value = err.message || 'Verification failed'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="customer-login">
    <div v-if="step === 'phone'">
      <input v-model="phoneNumber" placeholder="09121234567" />
      <button @click="sendOtp" :disabled="loading">
        Send OTP
      </button>
    </div>

    <div v-else-if="step === 'otp'">
      <input v-model="otpCode" placeholder="123456" maxlength="6" />
      <button @click="verifyAndLogin" :disabled="loading">
        Verify & Login
      </button>
    </div>

    <div v-if="error" class="error">{{ error }}</div>
  </div>
</template>
```

---

## 🧪 **Testing**

### **Customer Flow Test:**
```bash
# 1. Send OTP
curl -X POST http://localhost:5020/api/v1/auth/send-verification-code \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber":"09121234567","countryCode":"+98"}'

# 2. Complete Customer Auth
curl -X POST http://localhost:5020/api/v1/auth/customer/complete-authentication \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber":"09121234567","code":"123456","firstName":"علی","lastName":"احمدی"}'
```

### **Provider Flow Test:**
```bash
# 1. Send OTP (same as customer)
curl -X POST http://localhost:5020/api/v1/auth/send-verification-code \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber":"09121234567","countryCode":"+98"}'

# 2. Complete Provider Auth
curl -X POST http://localhost:5020/api/v1/auth/provider/complete-authentication \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber":"09121234567","code":"123456","firstName":"رضا","lastName":"محمدی"}'
```

---

## 🚨 **Important Notes**

1. **Customer vs Provider:** Use the correct endpoint based on user type
2. **Optional Fields:** firstName, lastName, email are all optional
3. **Automatic Creation:** Customer aggregate is created automatically on first login
4. **Token Storage:** Store both accessToken and refreshToken
5. **Error Handling:** All endpoints return proper error messages in Persian

---

## ✅ **Migration Status**

### **Completed ✅**

- [x] **phoneVerification.api.ts** - Updated with new unified API methods
- [x] **phoneVerification.types.ts** - Added new request/response types
- [x] **usePhoneVerification.ts** - Added `completeCustomerAuthentication()` and `completeProviderAuthentication()`
- [x] **VerificationView.vue** - Updated to use new unified authentication flow
- [x] **LoginView.vue** - Already compatible (only sends OTP, passes userType to VerificationView)
- [x] **ProviderLoginView.vue** - Already compatible (only sends OTP, passes userType to VerificationView)

### **Key Changes Made:**

1. **usePhoneVerification.ts composable:**
   - Added `completeCustomerAuthentication()` method
   - Added `completeProviderAuthentication()` method
   - Marked old `verifyCode()` as deprecated with console warning
   - Both new methods handle: verify OTP + create User + create Customer/Provider + return tokens
   - Tokens and user info are automatically stored in authStore

2. **VerificationView.vue:**
   - Removed old 2-step flow (verifyCode → registerFromVerifiedPhone)
   - Now uses unified authentication based on userType query param
   - Customer flow: calls `completeCustomerAuthentication()`
   - Provider flow: calls `completeProviderAuthentication()`
   - Handles provider onboarding redirect if `requiresOnboarding: true`

### **Testing Checklist**

- [ ] Test customer login/registration flow end-to-end
- [ ] Test provider login/registration flow end-to-end
- [ ] Verify Customer aggregate is created automatically
- [ ] Verify tokens are stored correctly in authStore
- [ ] Test provider onboarding redirect
- [ ] Update any integration tests

---

## 📞 **Need Help?**

If you encounter any issues during migration, check:
1. Network tab for actual API responses
2. Console for deprecation warnings
3. This guide for correct endpoint usage

**The old endpoints will NOT work - you MUST migrate!** ⚠️
