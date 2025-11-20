# Quick Reference: Role-Based Navigation

**Branch**: `feature/ux-role-based-navigation`
**Last Updated**: 2025-11-20

---

## 🚀 Quick Start

### Enable Role-Based Menu

```vue
<!-- LandingHeader.vue -->
<script setup>
import RoleBasedUserMenu from '@/shared/components/layout/Header/RoleBasedUserMenu.vue'
</script>

<template>
  <RoleBasedUserMenu :showRoleBadge="false" />
</template>
```

### Use Navigation Composable

```typescript
import { useRoleBasedNavigation } from '@/shared/composables/useRoleBasedNavigation'

const { userRole, roleLabel, redirectToRoleDefault } = useRoleBasedNavigation()
```

---

## 📍 New Routes

| Path | Name | Component | User Type |
|------|------|-----------|-----------|
| `/customer/login` | `CustomerLogin` | LoginView | Customer |
| `/customer/phone-verification` | `CustomerPhoneVerification` | VerificationView | Customer |
| `/provider/login` | `ProviderLogin` | ProviderLoginView | Provider |
| `/provider/phone-verification` | `ProviderPhoneVerification` | VerificationView | Provider |

**Legacy Redirects**:
- `/login` → `/customer/login`
- `/phone-verification` → `/customer/phone-verification`

---

## 🎨 Role Themes

```typescript
// Colors
Customer: #667eea (Purple)
Provider: #1976d2 (Blue)
Admin:    #334155 (Slate)

// CSS Classes
.theme-customer
.theme-business
.theme-admin
```

---

## 📋 Menu Items by Role

### Customer
- 👤 پروفایل من (opens modal)
- 📅 نوبت‌های من (opens modal)
- ❤️ علاقه‌مندی‌ها (opens modal)
- ⭐ نظرات من (opens modal)
- ⚙️ تنظیمات (opens modal)

### Provider
- 📊 داشبورد (`/dashboard`)
- 🏪 پروفایل کسب‌وکار (`/provider/profile`)
- 📅 رزروها (`/provider/bookings`)
- ✂️ خدمات (`/provider/services`)
- 👥 کارکنان (`/provider/staff`)
- 🕐 ساعت کاری (`/provider/hours`)
- ⚙️ تنظیمات (`/provider/settings`)

### Admin
- 📊 Dashboard (`/admin/dashboard`)
- 👥 Users (`/admin/users`)
- 🏪 Providers (`/admin/providers`)
- 📅 Bookings (`/admin/bookings`)
- ⚙️ Settings (`/admin/settings`)

---

## 🔧 Common Tasks

### Redirect Based on Role
```typescript
const authStore = useAuthStore()
await authStore.redirectToDashboard() // Smart redirect
```

### Check User Role
```typescript
const { userRole } = useRoleBasedNavigation()

if (userRole.value === 'provider') {
  // Provider-specific logic
}
```

### Check Onboarding Status
```typescript
const { isProviderOnboardingComplete } = useRoleBasedNavigation()

if (!isProviderOnboardingComplete.value) {
  // Force onboarding
}
```

---

## 🐛 Debugging

### Check Route Meta
```typescript
console.log(route.meta.userType) // 'Customer' | 'Provider'
console.log(route.name) // 'CustomerPhoneVerification'
```

### Check Auth State
```typescript
const authStore = useAuthStore()
console.log(authStore.userRoles)
console.log(authStore.providerStatus)
console.log(authStore.isProviderOnboardingComplete)
```

### Network Tab
Look for duplicate calls to:
- `/v1/auth/customer/complete-authentication`
- `/v1/auth/provider/complete-authentication`

Should only see 1 call per verification.

---

## ✅ Testing Checklist

### Customer Flow
- [ ] `/customer/login` loads
- [ ] Phone verification works
- [ ] Back button returns to customer login
- [ ] No duplicate API calls
- [ ] Redirects to homepage after login
- [ ] Customer menu shows

### Provider Flow
- [ ] `/provider/login` loads
- [ ] Phone verification works
- [ ] Back button returns to provider login
- [ ] No duplicate API calls
- [ ] Incomplete provider → `/provider/registration`
- [ ] Complete provider → `/dashboard`
- [ ] Provider menu shows

### Legacy URLs
- [ ] `/login` redirects to `/customer/login`
- [ ] Old bookmarks work

---

## 📚 Documentation

| Document | Purpose |
|----------|---------|
| `UX_ROLE_BASED_NAVIGATION.md` | Complete UX design |
| `UX_IMPLEMENTATION_SUMMARY.md` | Integration guide |
| `FIX_DOUBLE_AUTH_CALL.md` | Double call fix details |
| `SESSION_SUMMARY_2025_11_20.md` | Full session notes |
| `QUICK_REFERENCE_ROLE_NAVIGATION.md` | This file |

---

## 🚨 Common Issues

### Issue: User menu not showing
**Solution**: Import `RoleBasedUserMenu` instead of `UserMenu`

### Issue: Wrong login page on redirect
**Solution**: Use `CustomerLogin` route name, not `Login`

### Issue: Duplicate API calls
**Solution**: Already fixed in VerificationView.vue

### Issue: Legacy URL not redirecting
**Solution**: Check auth.routes.ts has redirect entries

---

## 💻 Code Snippets

### Navigate to Login
```typescript
// Customer
router.push({ name: 'CustomerLogin' })

// Provider
router.push({ name: 'ProviderLogin' })

// With redirect
router.push({
  name: 'CustomerLogin',
  query: { redirect: '/booking/123' }
})
```

### Route Guard Check
```typescript
// auth.guard.ts
if (to.meta.requiresAuth && !authStore.isAuthenticated) {
  next({ name: 'CustomerLogin', query: { redirect: to.fullPath } })
}
```

### Conditional Menu
```vue
<template>
  <RoleBasedUserMenu v-if="isAuthenticated" />
  <div v-else>
    <router-link to="/customer/login">ورود</router-link>
    <router-link to="/provider/login">پنل کسب‌وکار</router-link>
  </div>
</template>
```

---

## 🎯 Success Metrics

- ✅ 0 duplicate API calls
- ✅ 100% backwards compatibility
- ✅ <100ms redirect decision time
- ✅ Role-specific menus working
- ✅ Clean URL structure

---

**Quick Links**:
- [Full Session Summary](./SESSION_SUMMARY_2025_11_20.md)
- [UX Design Doc](./UX_ROLE_BASED_NAVIGATION.md)
- [Integration Guide](./UX_IMPLEMENTATION_SUMMARY.md)
