# Implementation Tasks

## 1. Frontend - Create Provider Login Page
- [ ] 1.1 Create `ProviderLoginView.vue` based on current LoginView
- [ ] 1.2 Update messaging for providers: "ورود به پنل ارائه‌دهندگان"
- [ ] 1.3 Set `userType = 'Provider'` explicitly (no detection logic)
- [ ] 1.4 Pass userType to VerificationView via route state/query

## 2. Frontend - Update Customer Login Page
- [ ] 2.1 Update `LoginView.vue` messaging for customers: "رزرو نوبت"
- [ ] 2.2 Remove redirect path detection logic (lines 98-129)
- [ ] 2.3 Set `userType = 'Customer'` explicitly
- [ ] 2.4 Remove `registration_user_type` sessionStorage dependency
- [ ] 2.5 Pass userType to VerificationView via route state/query

## 3. Frontend - Update Verification Flow
- [ ] 3.1 Update `VerificationView.vue` to receive userType from route state/query
- [ ] 3.2 Remove sessionStorage.getItem('registration_user_type') fallback
- [ ] 3.3 Add validation: redirect to login if userType is missing

## 4. Frontend - Update Routes
- [ ] 4.1 Add new route: `{ path: '/provider/login', name: 'ProviderLogin', component: ProviderLoginView }`
- [ ] 4.2 Add meta tags for SEO differentiation
- [ ] 4.3 Ensure both routes have `requiresAuth: false`

## 5. Frontend - Update Navigation Components
- [ ] 5.1 Update `HeroSection.vue`: "رزرو کنید" → `router.push('/login')`
- [ ] 5.2 Update `Footer.vue`: Add "برای کسب‌وکارها" → `/provider/login`
- [ ] 5.3 Update `Header.vue`: Provider nav link → `/provider/login`
- [ ] 5.4 Update any other components with login links

## 6. Testing
- [ ] 6.1 Test customer booking flow: Home → Search → Provider Detail → "رزرو" → `/login` → Customer registration
- [ ] 6.2 Test provider flow: Footer "برای کسب‌وکارها" → `/provider/login` → Provider registration
- [ ] 6.3 Test direct navigation to both `/login` and `/provider/login`
- [ ] 6.4 Verify userType is correctly passed to registration API
- [ ] 6.5 Test backward compatibility: existing redirect flows still work
- [ ] 6.6 Verify no sessionStorage dependencies remain

## 7. Documentation
- [ ] 7.1 Update `AUTHENTICATION_FLOW_DOCUMENTATION.md` with new login flow
- [ ] 7.2 Add comments in code explaining userType flow
- [ ] 7.3 Update any README files mentioning login URLs
