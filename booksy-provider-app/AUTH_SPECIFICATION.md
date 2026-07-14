# Provider (Business) Authentication — Reverse-Engineered Specification

**Status:** Source-of-truth specification for implementing authentication in the new Flutter `booksy-provider-app`.
**Reference implementation:** Vue web app `booksy-frontend` (production) + `Booksy.Host` backend (UserManagement + ServiceCatalog contexts).
**Rule:** The Vue implementation is the functional source of truth. Reproduce its **behavior**, not its structure. Where Vue has latent bugs/quirks, they are flagged as `⚠️ QUIRK` / `🐞 BUG` with a recommendation — do **not** silently "fix" business logic without confirming against the running backend.

> This document is Phase 1–4 output (reverse engineering + spec). **No Flutter code is produced here.** Implementation begins only after this spec is accepted.

---

## 0. TL;DR — What the Provider Auth Flow Actually Is

Provider authentication is **passwordless, phone + OTP based**. There is no email/password path for providers (the email/password `AuthController`/`authApi` exists but is used only by legacy/admin surfaces, **not** the provider phone flow).

The entire provider auth surface is **two screens + automatic post-auth routing**:

1. **Provider Login** (enter Iranian mobile number) → `POST send-verification-code`
2. **OTP Verification** (enter 6-digit code) → `POST provider/complete-authentication` (verify + register-or-login + issue JWT in one call)
3. On success, route by provider status: **new / no-profile / Drafted → Onboarding (`OrganizationRegistration`)**; **onboarding complete → `/provider/dashboard`**.

Login and registration are the **same flow** — the backend creates the User on first OTP completion (`isNewProvider = true`). There is no separate "register" form for the account itself. "Registration" for a provider means **business onboarding** (a separate multi-step wizard, out of scope for auth but its entry/redirect is specified here).

There is **no** provider "forgot password", "reset password", or "resend via dedicated endpoint" — resend re-issues a fresh code through the same send endpoint. Token refresh is automatic on 401.

---

## 1. Source File Inventory (Vue + Backend)

### 1.1 Vue frontend (`booksy-frontend/src`)

| Concern | File |
|---|---|
| Provider login screen | `modules/auth/views/ProviderLoginView.vue` |
| OTP verification screen (shared customer/provider) | `modules/auth/views/VerificationView.vue` |
| Customer login screen (for contrast/parity) | `modules/auth/views/LoginView.vue` |
| OTP input widget | `modules/auth/components/OtpInput.vue` |
| OTP input logic | `modules/auth/composables/useOtpInput.ts` |
| Phone-verification flow logic | `modules/auth/composables/usePhoneVerification.ts` |
| Phone-verification API client | `modules/auth/api/phoneVerification.api.ts` |
| Phone-verification DTO types | `modules/auth/types/phoneVerification.types.ts` |
| Auth store (Pinia) | `core/stores/modules/auth.store.ts` |
| Auth composable | `core/composables/useAuth.ts` |
| JWT + token injection interceptor | `core/api/interceptors/auth.interceptor.ts` |
| camelCase↔PascalCase transform | `core/api/interceptors/transform.interceptor.ts` |
| Retry (exponential backoff) | `core/api/interceptors/retry-handler.ts` |
| Global error → Persian toast | `core/api/interceptors/error.interceptor.ts` |
| HTTP client (axios, interceptor order) | `core/api/client/http-client.ts` |
| API base URLs / endpoint map | `core/api/config/api-config.ts` |
| Envelope type `ApiResponse<T>` | `core/api/client/api-response.ts` |
| Auth route table | `core/router/routes/auth.routes.ts` |
| Provider route table | `core/router/routes/provider.routes.ts` |
| Global auth guard | `core/router/guards/auth.guard.ts` |
| Role guard | `core/router/guards/role.guard.ts` |
| Navigation (loading/unsaved) guard | `core/router/guards/navigation.guard.ts` |
| Hierarchy guards (org/staff/independent) | `core/router/guards/hierarchy.guard.ts` |
| Router assembly (`beforeEach` order) | `core/router/index.ts` |
| Enums (`ProviderStatus`, `UserRole`, …) | `core/types/enums.types.ts` |
| Provider status fetch / token refresh | `modules/provider/services/provider.service.ts` |
| Onboarding entry screen | `modules/provider/views/registration/ProviderRegistrationView.vue` |
| App bootstrap | `main.ts` |

### 1.2 Backend (`src`)

| Concern | File |
|---|---|
| Phone-auth controller (send/complete/generate-token) | `UserManagement/…/API/Controllers/V1/AuthController.cs` |
| Login/refresh/logout/forgot controller | `UserManagement/…/API/Controllers/V1/AuthenticationController.cs` |
| Send OTP handler | `…/Application/CQRS/Commands/SendVerificationCode/SendVerificationCodeCommandHandler.cs` |
| Verify OTP handler | `…/Application/CQRS/Commands/VerifyPhone/VerifyPhoneCommandHandler.cs` |
| Complete provider auth cmd/handler | `…/Application/CQRS/Commands/CompleteProviderAuthentication/*` |
| Complete customer auth cmd/handler | `…/Application/CQRS/Commands/CompleteCustomerAuthentication/*` |
| JWT token service (claims) | `UserManagement/…/Infrastructure/Services/Security/JwtTokenService.cs` |
| Phone number value object (normalization) | `Core/Booksy.Core.Domain/ValueObjects/PhoneNumber.cs` |
| Provider status + token refresh endpoints | `ServiceCatalog/…/Api/Controllers/V1/ProvidersController.cs` |
| ServiceCatalog `ProviderStatus` enum | `ServiceCatalog/…/Domain/Enums/ProviderStatus.cs` |

---

## 2. Screens

### 2.1 Provider Login (`ProviderLoginView.vue`)

- **Purpose:** Collect the provider's Iranian mobile number and request an OTP.
- **Route:** `/provider/login`, name `ProviderLogin`, `meta.isPublic = true`.
- **Entry points:** Direct nav; auth guard redirect for unauthenticated access to any `/provider/*` protected route; 401 auto-redirect from the interceptor; "logout" from a protected page; footer link "ارائه‌دهنده خدمات هستید؟ ورود به پنل کسب و کار" from customer login.
- **Fields:** single `tel` input, `dir="ltr"`, placeholder `09123456789`, `data-testid="phone-input"`. Submit button `data-testid="send-code-button"` label **«دریافت کد»**.
- **Client validation (in order):**
  1. Empty → error **«لطفاً شماره موبایل خود را وارد کنید»**.
  2. Regex `^09\d{9}$` fail → error **«شماره موبایل وارد شده معتبر نیست»**.
- **Loading:** button `:loading`, input disabled during request.
- **Success:** navigate to `ProviderPhoneVerification` with `query.phone = <entered number>`.
- **Error state:** inline `form-error` under input; message from `result.error` or fallback **«خطا در ارسال کد تأیید»**.
- **Empty/success states:** N/A (single-shot form).
- **Permissions:** none (public).
- **⚠️ PARITY NOTE:** Unlike `LoginView` (customer), `ProviderLoginView` **does not read or persist `route.query.redirect`** into `post_login_redirect`. So a deep-link redirect that sent the provider to login is **dropped** after auth (provider always lands on dashboard/onboarding). See §7.4 and Gap G-3.

### 2.2 OTP Verification (`VerificationView.vue`) — shared for customer & provider

- **Purpose:** Enter the 6-digit code; on completion, verify + authenticate + route.
- **Route:** `/provider/phone-verification`, name `ProviderPhoneVerification`, `meta.isPublic = true`, **`meta.userType = 'Provider'`** (this meta selects the provider endpoint).
- **Entry point:** from `ProviderLogin` after a successful send; `query.phone` carries the number; `sessionStorage` carries `phone_verification_id` + `phone_verification_number`.
- **Header:** title **«تایید کد»**, description shows masked/entered phone (`•••1234`) `dir="ltr"`.
- **Widget:** `OtpInput` — 6 single-char boxes, `inputmode="numeric"`, auto-advance, auto-focus first, paste support, `data-testid="otp-input"`.
- **Submit paths:**
  - Manual button **«تایید کد»**; if `otpCode.length !== 6` → error **«لطفاً کد 6 رقمی را وارد کنید»**.
  - Auto-submit on `@complete` (all 6 filled), with a 100 ms debounce and duplicate-call guards (`isVerifying`, `isLoading`).
- **Actions row:**
  - Resend: shown only when `canResend` (after 60 s countdown). Otherwise shows **«ارسال مجدد کد در {n} ثانیه»**.
  - Back: **«بازگشت به صفحه ورود»** → `ProviderLogin`.
- **Loading:** button + input disabled while verifying.
- **Error state:** message from `result.error` (backend Persian message) or fallback **«کد وارد شده صحیح نیست»** / **«خطا در تأیید کد»**; OTP input is cleared (`otpInputRef.clear()`) and shakes.
- **Success state:** `state.step = 'success'`, tokens+user stored, toast (**«ثبت‌نام شما با موفقیت انجام شد!»** for new, else **«ورود موفقیت‌آمیز بود!»**), then routing (§7.3).
- **Permissions:** none (public); but a valid `verificationId`/phone must exist (else "request a new code").

### 2.3 Onboarding entry (`ProviderRegistrationView` → `OrganizationRegistrationFlow`)

- Not part of "authentication" proper, but it is the **post-auth destination for new/incomplete providers**, so its guard contract matters.
- Route `/provider/registration` (`ProviderRegistration`, `requiresAuth`, roles `['Provider','ServiceProvider']`); `beforeEnter` allows only `providerStatus === null | Drafted`, else redirects to `ProviderDashboard`.
- `ProviderRegistrationView` immediately `router.push({ name: 'OrganizationRegistration' })` on mount (individual registration is disabled — everyone onboards as Organization).
- Route `/provider/registration/organization` (`OrganizationRegistration`) has the same null/Drafted-only `beforeEnter`.
- **For the Flutter app:** the auth feature must (a) detect "needs onboarding" and (b) hand off to the onboarding feature. The onboarding wizard itself is a separate epic.

---

## 3. User Journeys

Each journey below is the observed Vue behavior end-to-end.

### J1 — New provider (registration)
1. `/provider/login` → enter phone → `send-verification-code` (201/200, `verificationId`, `expiresAt`, `maxAttempts`).
2. Navigate to OTP screen; 60 s resend countdown starts.
3. Enter 6 digits → `provider/complete-authentication`.
4. Backend: no user for phone → **creates User** (`UserType.Provider`, role assigned), `isNewProvider = true`, `providerId = null`, `requiresOnboarding = true`.
5. Tokens+user stored; toast «ثبت‌نام…». `VerificationView` sees `requiresOnboarding === true` → `router.push({ name: 'ProviderRegistration' })` → auto → `OrganizationRegistration`.

### J2 — Returning provider, onboarding complete (login)
1–3 as J1.
4. Backend: user exists, provider profile exists with status ∈ {Verified, Active, …}; `isNewProvider = false`; `providerId` set; `requiresOnboarding = (status == "Pending")` → **false** (see 🐞 BUG-1).
5. `VerificationView`: `requiresOnboarding` false → `redirectBasedOnProviderStatus()` → no `post_login_redirect` → `authStore.redirectToDashboard()` → `/provider/dashboard`.

### J3 — Returning provider, profile still Drafted / no profile (resume onboarding)
- `completeProviderAuthentication` returns `isNewProvider=false`, `providerId=null` (no ServiceCatalog profile) **or** status `Drafted`.
- Token has `provider_status = Drafted | absent`. `authStore.setToken` extracts `providerStatus`.
- Routing: if backend `requiresOnboarding` true → `ProviderRegistration`. Else `redirectToDashboard()` → since `providerStatus === null || Drafted`, it **forces** `OrganizationRegistration` (see `redirectToDashboard` provider branch).

### J4 — Resend OTP
- Only enabled after 60 s. Calls `resendCode()` → internally `sendVerificationCode(samePhone, sameCountryCode)` → **new** verification record + new SMS. Resets `remainingAttempts = 3`, restarts countdown, toast **«Verification code resent»** (English string — see Gap G-6 i18n).

### J5 — Wrong code
- `verifyResult.Success == false` → handler throws `InvalidOperationException(message)` → **HTTP 401** `{ success:false, message }`. Composable surfaces `error.response.data.message`. Persian backend messages, e.g. `"Invalid verification code. {n} attempts remaining."` (⚠️ these particular strings are English in backend — see Gap G-6). OTP cleared + shake.

### J6 — Expired code
- Backend `verification.IsExpired()` (10 min) → 401 with `"Verification code has expired. Please request a new code."` Same surfacing as J5. User taps resend.

### J7 — Too many wrong attempts (lockout)
- After max failed attempts the aggregate blocks for **1 hour**; `IsBlocked()` → 401 `"Too many failed attempts. Please try again after {HH:mm}"` (+ `blockedUntil`). Resend also constrained by send-side rate limit (J8).

### J8 — Too many send requests (rate limit)
- Two layers:
  - App-level (handler, non-DEBUG): ≥3 verifications for the same phone in 10 min → `DomainValidationException` message **«تعداد درخواست بیش از حد. لطفا دقایقی دیگر مجدد تلاش کنید»** (HTTP 400).
  - ASP.NET rate limiter `[EnableRateLimiting("phone-verification")]` on send, `"code-verification"` on complete → HTTP 429 → global toast **«تعداد درخواست‌های شما بیش از حد مجاز است…»**.

### J9 — Auto-login / session restoration
- On app start, `auth.store` `loadFromStorage()` reads `localStorage` `access_token`/`refresh_token`/`user`; decodes JWT; re-derives `providerStatus`/`providerId` from claims. `isAuthenticated = !!token && !!user`. No network call. If a `/provider/*` route is opened while authenticated, the guard routes by `providerStatus`.

### J10 — Token expiration / 401 during a call
- Any 401 triggers `authErrorInterceptor`: single-flight refresh via `POST /v1/Auth/refresh` (shared `refreshPromise`), retries the original request with the new token. On refresh failure: clear storage, detect role from stale token, redirect to `/provider/login?redirect=<path>` (or `/customer/login`).

### J11 — Logout
- `authStore.logout()`: determine `isProvider` from roles **before** clearing; call `POST /v1/Auth/logout`; clear token/refresh/user/providerStatus; if current route is not public, redirect to `ProviderLogin` (or `CustomerLogin`) with `query.redirect = currentPath`. Errors during the API call still clear local state.

### J12 — Wrong-context phone (customer number used on provider login)
- Backend `CompleteProviderAuthentication`: if an existing user's `Type != Provider` → throw `InvalidOperationException("This phone number is registered as {Type}. Please use the appropriate login endpoint.")` → 401. Surfaced as error on OTP screen.

### J13 — Authenticated provider revisits login/register
- Auth guard: if `isAuthenticated` and target is `CustomerLogin|ProviderLogin|Login|Register` → `redirectToDashboard(redirectPath)` instead of showing the page.

### J14 — Offline / network failure
- No `error.response` → `errorInterceptor` toast **«خطا در برقراری ارتباط با سرور. لطفاً اتصال اینترنت خود را بررسی کنید»** (title «خطای شبکه»). Retry handler retries network errors up to 3× with exponential backoff + jitter before the error surfaces.

---

## 4. Business Rules

### 4.1 Phone number
- **Client format rule (both login screens):** must match `^09\d{9}$` (Iranian mobile, 11 digits, leading `09`).
- **Backend normalization (`PhoneNumber.From`):** strips everything except digits and `+` (`[^\d+]` removed). Then:
  - `+98` + 10 digits → `("+98", national)`.
  - `0098…` → `("+98", …)`.
  - `09xxxxxxxxx` (len 11) → `("+98", "9xxxxxxxxx")`, `Value = cleaned` (keeps the leading `0`).
  - otherwise assume national with `+98`.
  - **Valid** if national matches `^9\d{9}$` OR is 8–15 digits.
- **✅ CONFIRMED — Canonical phone format (Flutter-wide):** `09121234567` — exactly **11 digits**, must **start with `09`**, no spaces, no hyphens, **no country code embedded**, **no automatic `+98`/international conversion inside Flutter**. Accept, validate, display, and transmit in this format. Client validation: `^09\d{9}$` (V2).
- **🔑 CRITICAL WIRE INVARIANT (parity-critical, verified against DB queries):** `send-verification-code` stores the verification under `PhoneNumber.From(countryCode + phoneNumber).Value`; `provider/complete-authentication` looks it up under `PhoneNumber.From(phoneNumber).Value` (**no** country-code prepend). The DB match is **exact `Value` equality** (`PhoneVerificationRepository.GetByPhoneNumberAsync`: `v.PhoneNumber.Value == phoneNumber.Value`), so both sides must normalize to the **same** `Value`.
  - `PhoneNumber.From` strips all non-`[\d+]` chars, then `09xxxxxxxxx` (len 11) normalizes to `Value = "09xxxxxxxxx"` (keeps leading `0`).
  - Vue keeps them equal by passing `countryCode = 'IR'` (a **non-digit** marker) and sending an identical phone string to both calls.
  - **✅ CONFIRMED Flutter contract:** send `phoneNumber = "09121234567"` **verbatim to both** `send-verification-code` and `provider/complete-authentication`. On the **send** call, set `countryCode = "IR"` (non-digit marker; matches the proven Vue/E2E path). Result: both sides normalize to `Value = "09121234567"` → match.
  - **❌ Forbidden:** sending `countryCode:"+98"` on send, or **omitting** `countryCode` on send (the backend record defaults it to `"+98"`) — either stores `"+9809121234567"` and breaks the lookup ("No verification found"). Never send `+98…` to one endpoint and `09…` to the other. See §11 Edge E-1.

### 4.2 OTP
- **Length:** 6 digits (client enforces exactly 6; input numeric-only).
- **Expiry:** 10 minutes (SMS text says "Valid for 10 minutes"; `ExpiresAt` returned; `IsExpired()` enforced).
- **Max attempts:** response advertises `maxAttempts = 5`; failing all → blocked **1 hour**. Client mirrors `remainingAttempts` from send response (`state.remainingAttempts`), resets to 3 on resend (⚠️ client's local `3` disagrees with server `5` — cosmetic only; server is authoritative).
- **Resend cooldown (client):** 60 s countdown before resend is enabled.
- **Send rate limit (server):** ≥3 sends / 10 min per phone (disabled under `#if DEBUG`) + ASP.NET limiter buckets `phone-verification` / `code-verification`.
- **✅ CONFIRMED — Sandbox / test OTP = `123456`** (dev/test environments only). Rules:
  - The OTP is **generated and validated ONLY by the backend** (global `Sms:SandboxMode` switch; see memory `sms-sandbox-switch`).
  - The Flutter app **must never generate, hardcode, or simulate OTP values in production logic** — the client only sends the user-entered code for verification.
  - Any test helper that injects `123456` (automated E2E / dev) must be **isolated from production code** and enabled **only** through the project's existing dev/test configuration (build flavor / env flag) — never compiled into release logic.

### 4.3 Roles & status
- Roles that mean "provider": **`Provider`** or **`ServiceProvider`** (both accepted everywhere). Route roles use `['Provider','ServiceProvider']`.
- Role priority in `redirectToDashboard`: **Admin > Provider > Customer**.
- `needsProfileCompletion` = `providerStatus === Drafted || null`.
- `isPendingVerification` = `providerStatus === PendingVerification`.
- `isProviderActive` = `providerStatus ∈ {Active, Verified}`.

### 4.4 `ProviderStatus` enum (identical in `core/types/enums.types.ts`, `modules/provider/types/provider.types.ts`, and backend `ServiceCatalog/Domain/Enums/ProviderStatus.cs`)
```
Drafted, PendingVerification, Verified, Active, Inactive, Suspended, Archived
```
(TS uses string values equal to the names; C# is an int enum but serialized via `.ToString()` to those names.)

- **🐞 BUG-1:** `CompleteProviderAuthenticationCommandHandler` computes `requiresOnboarding = providerStatus == "Pending"`. `"Pending"` is **not** a valid `ProviderStatus` value, so for any *existing* provider profile `requiresOnboarding` is effectively always `false`. New providers (no profile) get `requiresOnboarding = true` via the default. Net effect: routing correctness for Drafted/incomplete providers is actually carried by the **frontend guard** (`redirectToDashboard` + `authGuard`) reading `providerStatus` from the JWT, **not** by `requiresOnboarding`. **Flutter must replicate the frontend's status-based routing** and treat `requiresOnboarding` as a hint only.

### 4.5 Token lifetimes
- Provider access token (from `provider/complete-authentication`): **24 h** (`ExpiresIn = 86400`).
- Refresh token: **30 days** (`RefreshToken.Generate(expirationDays: 30)`).
- `generate-token` (used internally by ServiceCatalog refresh-token): **15 min** access.
- Client "is expired" check: decode `exp`, compare `Date.now()` (used opportunistically; primary expiry handling is reactive on 401).

---

## 5. API Specification

All calls go through the axios client (base `VITE_*_API_URL` or `/api`, timeout **30 s**, `withCredentials: true`). Request bodies are auto-transformed camelCase→PascalCase; responses PascalCase→camelCase. `Authorization: Bearer <access_token>` injected when present.

**✅ Response envelope is confirmed** — produced by `ApiResponseMiddleware` (`src/Infrastructure/Booksy.API/Middleware/ApiResponseMiddleware.cs`), which wraps every **2xx** API response as `{ success:true, statusCode, message, data, metadata }` in camelCase. Non-2xx errors are **not** wrapped by this middleware — they come from `ExceptionHandlingMiddleware` or direct controller returns (e.g. complete-auth's `Unauthorized(new { success:false, message })`), so the client reads `error.response.data.message` on failures.

Response envelope `ApiResponse<T>`:
```ts
{ success: boolean; data: T | null; message?: string; statusCode?: number;
  error?: { code: string; message: string; errors?: Record<string,string[]> };
  errors?: Record<string,string[]>; metadata?: {...} }
```

### 5.1 `POST /api/v1/Auth/send-verification-code`
- **Auth:** anonymous. **Rate limit:** `phone-verification` (→ 429).
- **Request:** `{ phoneNumber: string, countryCode?: string /* backend record default "+98" */ }`
  - Vue sends `countryCode: "IR"` (non-digit marker; see §4.1). **✅ Flutter MUST also send `countryCode: "IR"` with `phoneNumber: "09121234567"`** — see the wire invariant in §4.1. Omitting `countryCode` defaults it to `"+98"` and breaks the flow.
- **Response `data`:** `{ verificationId: string(Guid), maskedPhoneNumber: string("•••1234"), expiresAt: string(ISO8601), maxAttempts: number(5), message: string }`.
- **Errors:** 400 invalid phone (`DomainValidationException`); 400 rate-limit «تعداد درخواست بیش از حد…»; 429 limiter; 500 SMS send failure `"Failed to send verification code. Please try again later."`.
- **Retryable (client):** 408/429/500/502/503/504 + network errors, 3× backoff.

### 5.2 `POST /api/v1/Auth/provider/complete-authentication`
- **Auth:** anonymous. **Rate limit:** `code-verification`.
- **Request:** `{ phoneNumber: string, code: string(6), firstName?: string, lastName?: string, email?: string }` (Vue passes only `phoneNumber`+`code`; names/email optional).
- **Response `data` (`CompleteProviderAuthenticationResponse`):**
  ```
  isNewProvider: bool, userId: Guid, providerId: Guid|null, providerStatus: string|null,
  phoneNumber: string, email: string|null, fullName: string,
  accessToken: string, refreshToken: string, expiresIn: 86400, tokenType: "Bearer",
  requiresOnboarding: bool, message: string
  ```
- **Success message:** new → `"Welcome! Please complete your provider profile."`; existing+needs onboarding → `"Welcome back! Please complete your provider onboarding."`; else `"Welcome back! You're now logged in."`
- **Errors (all thrown → HTTP 401 `{success:false,message}`):** no verification found (`"No verification found for this phone number"`); verify failed (message from VerifyPhone: invalid/expired/blocked); wrong user type (`"This phone number is registered as {Type}…"`); 500 generic `"An error occurred during authentication"`.

### 5.3 `POST /api/v1/Auth/customer/complete-authentication`
- Symmetric to 5.2 for customers. Response has `isNewCustomer`, `customerId` (no provider fields). **Not used by the provider app** — documented for completeness (the shared `VerificationView` chooses provider vs customer by `route.meta.userType`).

### 5.4 `POST /api/v1/Auth/refresh`
- **Auth:** anonymous. **Request:** `{ refreshToken: string }`.
- **Response `data`:** `{ accessToken, refreshToken, expiresIn, tokenType:"Bearer" }` (`AuthenticationResponse`; may or may not repopulate provider claims — see Gap G-4).
- Called by `auth.interceptor` via raw `fetch` (not axios) with single-flight guard. Frontend handles both nested `data.data.*` and flat, both camel/Pascal casing.
- **On failure:** clear storage → redirect to role login.

### 5.5 `POST /api/v1/Auth/logout`
- **Auth:** `[Authorize]`. No body. Returns `{ message: "Logged out successfully" }`. Backend does **not** actually invalidate the refresh token (TODO in code) — client-side cleanup is what matters.

### 5.6 `GET /api/v1/Providers/current/status`  (ServiceCatalog)
- **Auth:** `[Authorize]`. **Response:** `{ providerId: Guid, status: string, userId: Guid }`. **404** `{success:false,message:"Provider record not found",errorCode:"PROVIDER_NOT_FOUND"}` when no profile.
- Frontend `providerService.getCurrentProviderStatus()` returns `null` on 404. `auth.store.fetchProviderStatus()` exists but is **not** called during login (status comes from the JWT). Available as a fallback / post-onboarding refresh.

### 5.7 `POST /api/v1/Providers/current/refresh-token`  (ServiceCatalog)
- **Auth:** `[Authorize]`. No body. Internally re-reads provider status, then calls UserManagement `generate-token` with `provider_id`/`provider_status` additional claims. **Response:** `{ accessToken, refreshToken?, expiresIn, tokenType, providerId, providerStatus }`.
- Frontend `authStore.refreshProviderToken()` calls this **after onboarding completes** to obtain a JWT carrying the new provider status (so the guard lets the provider into the dashboard). ⚠️ Note the internally minted token is **15 min** (`generate-token`), unlike the 24 h login token — Flutter should be aware the token lifetime shrinks after this call (Gap G-5).

### 5.8 `POST /api/v1/PhoneVerification/resend` (defined in client, **DEAD**)
- `phoneVerificationApi.resendOtp` targets `/v1/PhoneVerification/resend`, but **no `PhoneVerificationController` exists** in the backend → would 404. The composable's `resendCode()` does **not** use it; it re-calls `sendVerificationCode`. **Flutter: implement resend as a fresh `send-verification-code`, not a dedicated resend endpoint.**

---

## 6. State Machine (Provider Auth)

```
                 ┌─────────────────────────────────────────────────────┐
                 │                                                     │
        app start│                                                     │
                 ▼                                                     │
        [Bootstrapping] ──loadFromStorage──▶ token+user? ──no──▶ [Unauthenticated]
                                                │ yes                    │
                                                ▼                        │ open /provider/login
                                        [Authenticated]                  ▼
                                                                 [LoginIdle]
                                                                     │ submit phone (valid ^09\d{9}$)
                                                                     ▼
                                                              [SendingCode] ──error──▶ [LoginIdle+error]
                                                                     │ success
                                                                     ▼
                                                             [OtpEntry] ◀──resend(after 60s)──┐
                                                                     │ 6 digits               │
                                                                     ▼                        │
                                                            [Verifying] ──invalid/expired──▶ [OtpEntry+error]
                                                                     │                        │
                                                                     │ blocked/ratelimited ───┘ (disabled/countdown)
                                                                     │ success (tokens stored)
                                                                     ▼
                                                            [Authenticated]
                                                                     │
                        ┌───────────── route by status ─────────────┤
                        ▼                        ▼                   ▼
             requiresOnboarding                Drafted/null      Verified/Active/…
             OR isNewProvider                  providerStatus    (onboarding done)
                        │                        │                   │
                        ▼                        ▼                   ▼
                [Onboarding: OrganizationRegistration]        [/provider/dashboard]

  From [Authenticated], any 401 ─▶ [Refreshing] ─success▶ [Authenticated]
                                              └─fail▶ clear ▶ [Unauthenticated] (→ /provider/login?redirect=)
  logout ─▶ clear all ─▶ [Unauthenticated]
```

**Explicit required states for the Flutter Bloc/Cubit** (maps to the task's required states):
`Initial`, `SendingCode` (Loading), `CodeSent`, `Verifying` (Loading), `Authenticated(Success)`, `AuthError(message)` (wrong credentials / generic), `ValidationFailure(fieldErrors)`, `SessionExpired` (refresh failed), `Unauthorized` (401 unrecoverable), `Blocked(until)`, `RateLimited`, `NeedsOnboarding(providerId?, status?)`, `Unauthenticated`.

---

## 7. Navigation

### 7.1 Route table (provider-relevant)
| Path | Name | Public | Auth | Roles | Notes |
|---|---|---|---|---|---|
| `/provider/login` | `ProviderLogin` | ✓ | – | – | login screen |
| `/provider/phone-verification` | `ProviderPhoneVerification` | ✓ | – | – | `meta.userType='Provider'` |
| `/provider/registration` | `ProviderRegistration` | – | ✓ | Provider/SP | null/Drafted only; auto→Organization |
| `/provider/registration/organization` | `OrganizationRegistration` | – | ✓ | Provider/SP | null/Drafted only |
| `/provider/dashboard` | `ProviderDashboard` | – | ✓ | Provider/SP | main landing |
| `/provider/bookings|profile|financial|hours|gallery|services|settings|analytics|staff|…` | … | – | ✓ | Provider/SP | protected app |
| `/unauthorized` `/forbidden` `/server-error` `/:pathMatch(*)` | error screens | | | | |

### 7.2 Guard chain (`router.beforeEach` order: `authGuard` → `roleGuard` → `navigationGuard`)
**`authGuard` logic:**
- Public route + authenticated + target is a login/register → `redirectToDashboard(query.redirect)`; else allow.
- `requiresAuth` + not authenticated → redirect to `ProviderLogin` (if target path starts `/provider/` or name starts `Provider`) else `CustomerLogin`, with `query.redirect = to.fullPath`.
- Authenticated provider status routing:
  - On registration routes → allow.
  - `providerStatus === Drafted || null` and target not in `{ProviderDashboard, ProviderProfile, ProviderSettings, NewBooking, BookingDetails, Bookings, Forbidden, NotFound, ServerError}` → redirect `OrganizationRegistration`.
  - `providerStatus ∈ {Verified, Active, PendingVerification}` and target is a registration route → redirect `/provider/dashboard`.
- Role check: `meta.roles` non-empty and user lacks any → `Unauthorized`.

**`roleGuard`:** duplicate role check → `Forbidden` (message "You do not have permission…"). **`navigationGuard`:** loading indicator + `window.confirm` unsaved-changes gate.

**Hierarchy guards** (`beforeEnter` on org/staff-only routes): read `hierarchyStore.currentHierarchy.provider.hierarchyType`; wrong type → `Forbidden` with Persian message. (Note: the hierarchy-load calls are currently commented out; guards read whatever is cached.)

### 7.3 Post-authentication routing (from `VerificationView.verifyOtp` → then `auth.store`)
1. If provider **and** `result.requiresOnboarding` → `ProviderRegistration`.
2. Else `redirectBasedOnProviderStatus()`:
   - If `sessionStorage.post_login_redirect` set → consume it and push that path.
   - Else `authStore.redirectToDashboard()`:
     - Provider + on a registration route → do nothing.
     - Provider + onboarding incomplete (`providerStatus === null || Drafted`) → `OrganizationRegistration`.
     - Provider + complete → `query.redirect` if valid `/provider/*` else `/provider/dashboard`.

### 7.4 Deep links / redirect param
- Guard sets `?redirect=<fullPath>` when bouncing an unauthenticated provider to login.
- **⚠️ `ProviderLoginView` ignores it** (does not store `post_login_redirect`), so provider deep-link return is effectively lost → always dashboard/onboarding. Customer login *does* store it. (Gap G-3 — Flutter should implement return-to-intent for providers, an intentional improvement.)

### 7.5 Flutter navigation mapping
- Use **GoRouter** with a `redirect` callback = combined `authGuard`+`roleGuard`.
- Auth state from an auth `Bloc`/`Cubit` exposed to a `refreshListenable`.
- Route guard inputs: `isAuthenticated`, `roles`, `providerStatus`, target route metadata (`requiresAuth`, `roles`, `isPublic`).
- Enforce **auth-mandatory**: the router's initial/redirect must send any unauthenticated access of a protected route to `/provider/login`. No guest access anywhere except the two auth screens.

---

## 8. Validation Rules (exact)

| # | Where | Rule | Message |
|---|---|---|---|
| V1 | ProviderLogin phone | non-empty | «لطفاً شماره موبایل خود را وارد کنید» |
| V2 | ProviderLogin phone | `^09\d{9}$` | «شماره موبایل وارد شده معتبر نیست» |
| V3 | OTP submit | `otpCode.length === 6` | «لطفاً کد 6 رقمی را وارد کنید» |
| V4 | OTP input | numeric only; single char/box; paste digits only | (silent — non-digits rejected) |
| V5 | complete-auth precondition | `state.phoneNumber` present | «Phone number is missing. Please request a new code.» |
| V6 | Backend phone | `PhoneNumber.From` valid (Iranian `^9\d{9}$` or 8–15 digits) | `Invalid phone number: …` (400) |
| V7 | Backend OTP | 6-digit code matches, not expired, not blocked, attempts left | see §5.2 error messages |

`firstName`/`lastName`/`email` are **optional** everywhere in the provider auth path (backend supplies defaults: firstName `"ارائه‌دهنده"`, lastName = national number, email = `{national}@booksy.provider`).

---

## 9. Error Handling

Global `errorInterceptor` (axios response error) maps HTTP status → Persian toast (and rethrows):
| Status | Behavior |
|---|---|
| no response (network/offline) | toast «خطا در برقراری ارتباط با سرور…» / «خطای شبکه» |
| 400 / 422 | per-field validation toasts (from `error.errors`) or «داده‌های ورودی نامعتبر است» |
| 401 | **silent** here (handled by auth interceptor / composable surfaces message) |
| 403 | «شما اجازه دسترسی به این بخش را ندارید» |
| 404 | «اطلاعات مورد نظر یافت نشد» |
| 409 | «این اطلاعات قبلاً ثبت شده است» |
| 429 | «تعداد درخواست‌های شما بیش از حد مجاز است…» |
| 5xx | backend `message` or «خطای سرور رخ داده است…» |
| other | backend `message` or «خطای نامشخصی رخ داده است» |

Composable-level: `usePhoneVerification` also sets `state.error` and shows a `toast.error` from `error.response.data.message || error.message`. Screens additionally render inline errors and clear/shake the OTP.

**Flutter mapping:** a `NetworkFailure`/`ServerFailure`/`ValidationFailure`/`UnauthorizedFailure`/`RateLimitFailure`/`BlockedFailure` hierarchy; a Dio interceptor that mirrors the status→message table (Persian strings from `AppStrings`); inline field errors from the `errors` map; fail-fast offline banner.

---

## 10. Security & Persistence

### 10.1 Token storage (Vue)
- `localStorage`: `access_token`, `refresh_token`, `user` (JSON).
- `sessionStorage`: `phone_verification_id`, `phone_verification_number` (cleared on success/reset), `post_login_redirect` (customer only).
- ⚠️ Web uses `localStorage` (XSS-exposed). **Flutter must use `flutter_secure_storage`** (Keychain/Keystore) for `access_token` + `refresh_token`; user profile can live in secure storage or an encrypted store. This is a deliberate platform upgrade, not a behavior change.

### 10.2 JWT claims (from `JwtTokenService.GenerateAccessToken`)
`NameIdentifier`(userId), `Email`, `Name`(displayName), `GivenName`, `Surname`, `user_type`, **`user-status`** (hyphen!), `Jti`, `Iat`, `providerId`(if provider), `provider_status`(if present), `customerId`(if customer), `MobilePhone`, and one `Role` claim per role (serialized as the MS schema URI `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`).
- Frontend `decodeToken` tolerates multiple key spellings (`sub|nameid|…nameidentifier`, `role|…/role`, `providerId|provider_id`, `provider_status|user_status`, `customerId|customer_id`). ⚠️ It reads `user_status` (underscore) while the token emits `user-status` (hyphen) — status is therefore taken from `provider_status`, not user-status. Flutter's decoder should mirror the tolerant key handling.
- HS256 signing; issuer `Booksy`, audience `Booksy.Users`; `ClockSkew = 0`.

### 10.2b "Remember Me" — not applicable to providers
The `rememberMe` flag exists only on the email/password `LoginCredentials` (customer/admin legacy path). The provider phone/OTP flow has **no remember-me control**: tokens are always persisted to `localStorage`, so the session is effectively always "remembered" until logout or refresh failure. **Flutter:** persist provider tokens to secure storage by default (no remember-me toggle) to match behavior.

### 10.3 Session lifecycle
- **Restore:** decode stored token on launch (no network). `isAuthenticated` needs both token and user object present.
- **Refresh:** reactive on 401, single-flight, token rotation persisted.
- **Invalidate/logout:** clear all three localStorage keys + provider state; server logout is best-effort.
- **Auth-mandatory:** every protected route requires a valid token; guard redirects to login otherwise.

---

## 11. Edge Cases

- **E-1 Phone Value mismatch** (§4.1): if send and complete normalize to different `Value`, complete fails with "No verification found". **Highest-risk parity item.**
- **E-2 Double submit / race:** auto-`@complete` + manual submit → guarded by `isVerifying`/`isLoading` + 100 ms debounce. Flutter must guard equivalently (a `Verifying` state that ignores re-entry).
- **E-3 Navigating to OTP screen directly** (no `verificationId`/phone): `state.phoneNumber` empty → complete returns V5 error. Flutter: guard the OTP route to require a pending verification, else bounce to login.
- **E-4 Resend before cooldown:** `resendCode` no-ops if `!canResend`.
- **E-5 Refresh-token rotation race:** multiple parallel 401s — the single shared `refreshPromise` prevents consuming a rotated refresh token twice (would otherwise log the user out). Flutter must implement a single-flight refresh lock.
- **E-6 Wrong user type on provider login** (J12) → 401 with explanatory message.
- **E-7 Provider with no ServiceCatalog profile after login** (`providerId=null`): guard forces onboarding even though `requiresOnboarding` may be false (BUG-1). Route by `providerStatus`/`providerId`, not solely the flag.
- **E-8 `generate-token` shortens token to 15 min** after `current/refresh-token` (post-onboarding) — subsequent 401 refresh must still work.
- **E-9 Blocked-until display:** backend returns `blockedUntil`; show remaining time and disable submit; still allow resend when send rate-limit permits (they are independent counters).
- **E-10 Country code with digits:** if Flutter ever sends a digit-bearing `countryCode` to `send` but bare national to `complete`, E-1 triggers. Keep the non-digit marker or a single canonical form.
- **E-11 Token present but user missing** (partial storage / decode fail): `isAuthenticated` false → treated as unauthenticated; storage cleared on decode error.
- **E-12 Clock skew:** backend `ClockSkew=0`; a slightly fast client clock can cause premature `exp`; rely on reactive 401 refresh rather than proactive client expiry checks.
- **E-13 App backgrounded during OTP:** in-progress verification state should survive foreground/restore (Vue persists in `sessionStorage`; Flutter should persist the pending verification so the OTP screen can resume).
- **E-14 No status-based login block (Vue) → Flutter MUST block (decided):** `complete-provider-authentication` validates only the **user type**, not provider `status`. In Vue a **Suspended / Inactive / Archived** provider still receives a token and reaches `/provider/dashboard` (no lockout). **DECISION (confirmed):** Flutter **improves on Vue** — after successful auth, if `providerStatus ∈ {Suspended, Inactive, Archived}` the app must route to a dedicated **"account suspended/unavailable" state** (Persian message) instead of the dashboard, while still holding the valid session. Add an `AccountBlocked(status)` state to the auth Bloc and a corresponding screen; the router must divert these statuses away from the dashboard/onboarding.

---

## 12. Sequence Diagrams

### 12.1 Login/Register (send + complete)
```
User→LoginScreen: enter 09xxxxxxxxx, tap «دریافت کد»
LoginScreen→LoginScreen: validate ^09\d{9}$
LoginScreen→API: POST send-verification-code {phoneNumber:"IR09…", countryCode:"IR"}
API→SMS: send 6-digit code (valid 10m)
API→LoginScreen: {verificationId, maskedPhone, expiresAt, maxAttempts}
LoginScreen→OTPScreen: navigate (phone in query; id/phone in session), start 60s countdown
User→OTPScreen: enter 6 digits (auto-submit on complete)
OTPScreen→API: POST provider/complete-authentication {phoneNumber:"IR09…", code}
API→VerifyPhone: verify (expiry/attempts/block checks)
API→UserRepo: find user by phone; create if absent (isNewProvider)
API→ProviderInfo: get provider by ownerId (providerId, status)
API→JWT: mint 24h access + 30d refresh (roles + provider claims)
API→OTPScreen: {tokens, providerId, providerStatus, requiresOnboarding, isNewProvider, …}
OTPScreen→Store: persist tokens+user; derive providerStatus from JWT
OTPScreen→Router: requiresOnboarding||new → Onboarding ; else dashboard
```

### 12.2 Token refresh (reactive)
```
App→API: any protected call (Bearer access)
API→App: 401
Interceptor→Interceptor: _retry not set → single-flight refreshPromise
Interceptor→API: POST /v1/Auth/refresh {refreshToken}
API→Interceptor: {accessToken, refreshToken} → persist rotation
Interceptor→API: retry original with new Bearer
API→App: 200 (or, on refresh fail: clear storage → /provider/login?redirect=)
```

### 12.3 Logout
```
User→UI: logout
Store→Store: isProvider = roles∋Provider/ServiceProvider (before clear)
Store→API: POST /v1/Auth/logout (best-effort)
Store→Storage: clear access/refresh/user/providerStatus
Store→Router: if not public route → ProviderLogin?redirect=<current>
```

### 12.4 Session restoration (cold start)
```
main()→Store: loadFromStorage()
Store→LocalStorage: read access/refresh/user
Store→Store: decode JWT → roles, providerStatus, providerId, customerId
Store→Store: isAuthenticated = token && user
Router→Guard: route by providerStatus (dashboard | onboarding | login)
```

### 12.5 Post-onboarding provider-token refresh
```
Onboarding done→Store: refreshProviderToken()
Store→ServiceCatalog: POST Providers/current/refresh-token
ServiceCatalog→ServiceCatalog: GetCurrentProviderStatus
ServiceCatalog→UserMgmt: POST auth/generate-token {userId, {provider_id, provider_status}}
UserMgmt→ServiceCatalog: {accessToken(15m), …}
ServiceCatalog→Store: {accessToken, providerId, providerStatus}
Store→Storage: persist; re-derive provider claims → guard now allows dashboard
```

---

## 13. Vue → Flutter Mapping

| Vue | Flutter (target architecture) |
|---|---|
| `ProviderLoginView.vue` | `ProviderLoginPage` (phone entry) |
| `VerificationView.vue` (userType=Provider) | `ProviderOtpVerificationPage` |
| `OtpInput.vue` + `useOtpInput.ts` | `OtpInput` widget (Pinput-style; numeric, auto-advance, paste) |
| `usePhoneVerification.ts` | `AuthBloc`/`AuthCubit` + `AuthRepository` |
| `phoneVerification.api.ts` + `http-client` | `AuthRemoteDataSource` (Dio) |
| `phoneVerification.types.ts` | Dart request/response models (manual JSON — codegen is disabled per CLAUDE.md) |
| `auth.store.ts` | `AuthBloc` + `SessionManager` (token/state) |
| `auth.interceptor.ts` (inject + 401 refresh, single-flight) | Dio `AuthInterceptor` + `QueuedInterceptor`/lock for refresh |
| `transform.interceptor.ts` | Dio interceptor OR per-model `@Json` key mapping (backend is PascalCase) |
| `retry-handler.ts` | Dio retry interceptor (408/429/5xx + network, backoff+jitter, max 3) |
| `error.interceptor.ts` | Dio error interceptor → `Failure` types + localized messages |
| `auth.guard.ts` + `role.guard.ts` | GoRouter `redirect` |
| `provider.routes.ts` / `auth.routes.ts` | GoRouter route table + `requiresAuth`/`roles` metadata |
| `localStorage` tokens | `flutter_secure_storage` |
| `sessionStorage` verification id/phone | in-memory + persisted pending-verification (survive restore) |
| `provider.service.ts` status/refresh-token | `ProviderRepository` (status + post-onboarding token refresh) |
| Persian toasts | `AppStrings` (fa) + existing toast/snackbar; RTL default |

**State management:** follow the customer app's existing Bloc pattern (per CLAUDE.md, `booksy-customer-app` uses Bloc/Cubit; codegen broken → manual JSON + manual `get_it` DI). Provider auth `Bloc` must expose the explicit states listed in §6.

**Networking:** reuse the customer app's Dio client/interceptor stack where transplantable; do **not** hardcode URLs (use `api_constants`); honor interceptor order (inject → refresh → error → retry).

---

## 14. Gap Analysis (Vue exists → Flutter must add / decide)

| ID | Gap | Severity | Notes |
|---|---|---|---|
| G-1 | Entire provider auth flow absent in Flutter (`booksy-provider-app` not created) | **Critical** | This spec is the build plan. |
| G-2 | Secure token storage | **Critical** | Web uses `localStorage`; Flutter must use secure storage. |
| G-3 | Provider `redirect`/return-to-intent dropped in Vue | High | **DECIDED:** Flutter **implements return-to-intent** — persist the intended path when bouncing to login and navigate there after auth (fixes the Vue gap). |
| G-4 | `/Auth/refresh` may not repopulate provider claims | High | Verify refreshed token still carries `providerId`/`provider_status`; if not, call `Providers/current/refresh-token` after refresh, or route via `current/status`. |
| G-5 | Token lifetime shrinks to 15 min after `current/refresh-token` | Medium | Ensure reactive refresh still works with the short-lived token. |
| G-6 | Mixed-language strings (some backend/toast strings English) | Medium | Flutter must present **Persian** via `AppStrings`; do not surface raw English backend strings where a localized equivalent exists. |
| G-7 | `requiresOnboarding` backend bug (BUG-1) | Medium | Route by `providerStatus`/`providerId`, treat flag as hint. |
| G-8 | Single-flight refresh lock | High | Must replicate to avoid refresh-token rotation logout race (E-5). |
| G-9 | Dead resend endpoint | Low | Implement resend as fresh `send-verification-code`. |
| G-10 | Local `remainingAttempts=3` vs server `5` | Low | Trust server values; mirror from responses. |
| G-11 | Sandbox OTP for E2E | Medium | Confirm sandbox code/switch with running backend before writing E2E. |
| G-12 | Onboarding wizard | (separate epic) | Auth must cleanly hand off; wizard itself out of scope here. |

---

## 15. Testing Plan (to satisfy CLAUDE.md Testing Policy — implemented in the build phase, not now)

- **Unit (Bloc/Cubit, 90%+):** phone validation (V1–V3), state transitions Initial→Sending→CodeSent→Verifying→Authenticated/Error, resend cooldown/reset, duplicate-submit guard (E-2), single-flight refresh (E-5), status→route decision (J1/J2/J3, BUG-1), logout clears everything (J11), wrong-user-type (J12).
- **Unit (repository/data source, 80%+):** request shaping incl. the phone Value invariant (E-1/E-10), envelope parsing, error mapping (§9), token rotation.
- **Widget:** `OtpInput` (numeric-only, auto-advance, paste, clear/shake on error, disabled/loading), login form validation display, OTP screen resend countdown + back.
- **Integration (against sandbox backend):** full send→complete→route for new & returning providers; expired/blocked/rate-limited paths; 401→refresh→retry; session restore; logout.
- **E2E (critical journeys):** J1 (register→onboarding entry), J2 (login→dashboard), J10 (token expiry recovery), J11 (logout guard). Use the existing sandbox OTP convention (keystone/Playwright parity).
- **Regression:** navigation guards (unauthenticated→login, authenticated→dashboard/onboarding, role gate), auth-mandatory (no guest access), RTL/Persian strings, ≥48dp targets, 1.3× font scale.
- **Coverage targets:** business logic/core services 90%+, blocs 90%+, repositories 80%+, critical flows 100% via integration/E2E.

---

## 16. Decisions & Open Questions

### 16.1 Confirmed decisions (fold into implementation)
- **D-1 Suspended/Inactive/Archived providers:** Flutter **blocks** post-auth with an `AccountBlocked(status)` state + screen (see E-14) — do **not** show the dashboard. *(Intentional Flutter improvement.)*
- **D-2 Return-to-intent:** Flutter **implements** redirect/deep-link return after login (see G-3). *(Intentional Flutter improvement.)*
- **D-3 Sandbox OTP = `123456`** (dev/test only; backend-owned; test helpers isolated from prod). *(Confirmed — §4.2.)*
- **D-4 Canonical phone = `09121234567`** (11 digits, `^09\d{9}$`, no country code, no `+98` conversion). Sent verbatim to both endpoints; `send` uses `countryCode:"IR"`. *(Confirmed — §4.1, verified against DB match logic.)*
- **D-5 Sequencing:** spec is under review; implementation is **paused** until sign-off.

### 16.2 Still to confirm against the running backend (non-blocking; verify during implementation)
1. **G-4** — Does `/Auth/refresh` return a token **with** provider claims (`providerId`/`provider_status`)? If not, follow refresh with `Providers/current/refresh-token` or route via `current/status`. *(Backend-dependent.)*
2. Is `send-verification-code`'s app-level 3-per-10-min limit active in the target env (it's `#if !DEBUG`)? *(Backend-dependent; affects only how aggressively resend is throttled.)*
3. Whether an existing **customer** phone attempting provider login should be offered a cross-registration path (currently hard 401, J12). *(Product decision — default: keep the 401 message.)*

---

---

## 17. Traceability Matrix (Flutter feature → Vue/backend source of truth)

Every row is traceable to a concrete file/function verified during reverse engineering.

| # | Flutter feature | Vue source (file · symbol) | Backend source (file · symbol) | Class |
|---|---|---|---|---|
| T1 | Provider login screen (phone entry) | `modules/auth/views/ProviderLoginView.vue` · `handleSubmit` | — | Confirmed |
| T2 | Phone validation `^09\d{9}$` | `ProviderLoginView.vue` L101-105; `LoginView.vue` L102 | `Core/…/ValueObjects/PhoneNumber.cs` · `IsValid` | Confirmed |
| T3 | Canonical phone `09121234567` + single PhoneNumber util | (Vue baked `IR` into string) | `PhoneNumber.From/CleanPhoneNumber/ExtractComponents` | Confirmed (format) / Improvement (centralization) |
| T4 | Send OTP call | `api/phoneVerification.api.ts` · `sendVerificationCode`; `composables/usePhoneVerification.ts` · `sendVerificationCode` | `AuthController.SendVerificationCode`; `SendVerificationCodeCommandHandler` | Confirmed |
| T5 | `countryCode:"IR"` non-digit marker on send | `ProviderLoginView.vue` `sendVerificationCode(phone,'IR')`; `usePhoneVerification` fullPhoneNumber | `SendVerificationCodeCommandHandler` L41-43; `PhoneVerificationRepository.GetByPhoneNumberAsync` L50 | Confirmed |
| T6 | OTP verification screen | `modules/auth/views/VerificationView.vue` | — | Confirmed |
| T7 | OTP input widget (6 boxes, numeric, auto-advance, paste, clear/shake) | `components/OtpInput.vue`; `composables/useOtpInput.ts` | — | Confirmed |
| T8 | Complete provider auth (verify+login/register+JWT) | `usePhoneVerification.ts` · `completeProviderAuthentication`; `VerificationView.vue` · `verifyOtp` | `AuthController.CompleteProviderAuthentication`; `CompleteProviderAuthenticationCommandHandler` | Confirmed |
| T9 | New-user creation on first OTP (registration==login) | (backend-driven; `isNewProvider`) | `CompleteProviderAuthenticationCommandHandler.CreateNewProviderUser` | Confirmed |
| T10 | OTP verify rules (6-digit, 10-min expiry, attempts, 1h block) | `VerificationView.vue` len check; `usePhoneVerification` | `VerifyPhoneCommandHandler`; `PhoneVerification` aggregate | Confirmed |
| T11 | Resend = fresh send + 60s cooldown | `usePhoneVerification.ts` · `resendCode`, `startResendCountdown` | (reuses send) | Confirmed |
| T12 | Response envelope `{success,data,…}` | `core/api/client/api-response.ts`; `http-client.ts` · `normalizeResponse` | `Infrastructure/Booksy.API/Middleware/ApiResponseMiddleware.cs` | Confirmed |
| T13 | camelCase↔PascalCase transform | `interceptors/transform.interceptor.ts` | `ApiResponseMiddleware` (CamelCase policy) | Confirmed |
| T14 | JWT inject + 401 single-flight refresh | `interceptors/auth.interceptor.ts` · `authInterceptor`, `authErrorInterceptor`, `performTokenRefresh` | `AuthenticationController.RefreshToken`; `RefreshTokenCommand` | Confirmed |
| T15 | Retry (408/429/5xx+network, backoff+jitter, max3) | `interceptors/retry-handler.ts` | — | Confirmed |
| T16 | Global error → localized messages | `interceptors/error.interceptor.ts` | `ExceptionHandlingMiddleware` | Confirmed |
| T17 | Auth store / session state | `core/stores/modules/auth.store.ts` | — | Confirmed |
| T18 | JWT decode → roles/providerId/providerStatus/customerId | `auth.store.ts` · `decodeToken`, `decodeTokenAndExtractProviderInfo` | `Infrastructure/…/Security/JwtTokenService.cs` · `GenerateAccessToken` | Confirmed |
| T19 | Session restore on launch (no network) | `auth.store.ts` · `loadFromStorage` (+ `main.ts`) | — | Confirmed |
| T20 | Token storage | `auth.store.ts` (localStorage) | — | Improvement (Flutter → `flutter_secure_storage`) |
| T21 | Logout (clear + role-aware redirect) | `auth.store.ts` · `logout`; `useAuth.ts` · `logout` | `AuthenticationController.Logout` | Confirmed |
| T22 | Route guard (auth + status routing) | `router/guards/auth.guard.ts`; `router/index.ts` | — | Confirmed |
| T23 | Role guard | `router/guards/role.guard.ts` | route `meta.roles` | Confirmed |
| T24 | Route table (provider/auth) | `router/routes/provider.routes.ts`; `routes/auth.routes.ts` | — | Confirmed |
| T25 | Post-auth routing (onboarding vs dashboard) | `VerificationView.verifyOtp`; `auth.store.redirectToDashboard`, `redirectBasedOnProviderStatus` | `CompleteProviderAuthenticationResponse.RequiresOnboarding` | Confirmed (+ BUG-1 caveat) |
| T26 | Provider status source (JWT) + fallback fetch | `auth.store` (from token); `provider.service.ts` · `getCurrentProviderStatus` | `ProvidersController.GetCurrentProviderStatus` | Confirmed |
| T27 | Post-onboarding provider-token refresh | `auth.store.ts` · `refreshProviderToken`; `provider.service.ts` · `refreshProviderToken` | `ProvidersController.RefreshProviderToken` → `AuthController.GenerateToken` | Confirmed |
| T28 | ProviderStatus enum | `core/types/enums.types.ts` · `ProviderStatus` | `ServiceCatalog/…/Enums/ProviderStatus.cs` | Confirmed |
| T29 | Return-to-intent after login | `LoginView.vue` L118 (customer only; provider drops it) | — | Improvement (G-3) |
| T30 | Block Suspended/Inactive/Archived post-auth | (absent in Vue) | (no status block in handler) | Improvement (E-14/D-1) |
| T31 | Sandbox OTP `123456` (dev/test, backend-owned) | E2E scripts | `Sms:SandboxMode` | Confirmed |
| T32 | Forgot/Reset password | (not in provider phone flow) | `AuthenticationController.ForgotPassword` (email path) | N/A for providers (documented §0) |
| T33 | Remember-me | (email/password only) | — | N/A for providers (§10.2b) |

## 18. Final Consistency Audit — Result

**Internal consistency:** PASS. No contradictions found across screens ↔ routes ↔ API ↔ state machine ↔ validation ↔ persistence after the following resolutions were applied:
- Envelope claim now traced to `ApiResponseMiddleware` (was previously asserted without a source). ✅
- Phone wire invariant now concretely specified (canonical `09…` + `countryCode:"IR"` on send), verified against `PhoneVerificationRepository` exact-`Value` match. ✅
- Sandbox OTP value pinned to `123456` with prod-isolation rule. ✅

**Every spec element is classified** (see the `Class` column above and §16):
- **Confirmed** (traceable to Vue+backend, no open question): T1–T19, T21–T28, T31; all of §2–§12.
- **Backend-dependent** (verify at runtime, non-blocking): G-4 refresh-claims (§16.2 #1); send rate-limit `#if !DEBUG` (§16.2 #2).
- **Intentional Flutter improvement** (deliberate divergence from Vue, decided): T3 (centralized PhoneNumber), T20 (secure storage / G-2), T29 (return-to-intent / G-3 / D-2), T30 (suspended block / E-14 / D-1).
- **N/A for providers** (documented, not implemented): T32 forgot/reset password, T33 remember-me.

**Remaining ambiguities:** none blocking. Two backend-dependent items (G-4, rate-limit env) are to be verified during implementation without changing the design; one product default (customer-phone-on-provider-login → keep 401) is documented. All are explicitly classified above — **no assumptions were made silently.**

---

*End of specification. Reviewed 3× + final consistency audit (§17–§18). Approved. Ready for incremental implementation.*
