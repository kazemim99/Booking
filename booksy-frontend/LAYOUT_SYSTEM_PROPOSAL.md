# Layout System Architecture - UI/UX Best Practices

## Problem Statement

Currently, the app lacks a consistent master layout system. Pages have inconsistent headers/footers:
- ✅ Home page has custom LandingHeader
- ❌ Provider Search has no header/navigation
- ❌ Other pages lack consistent navigation
- ✅ Mobile bottom navigation exists globally

## Solution: Multi-Layout Architecture

Modern booking platforms (Airbnb, Booking.com, OpenTable) use **context-specific layouts**:

### **Layout Types Needed:**

1. **LandingLayout** - Marketing pages (home, about, contact)
2. **AppLayout** - Main authenticated experience (dashboard, bookings, favorites)
3. **FocusedLayout** - Task-focused flows (search, booking process)
4. **MinimalLayout** - Authentication flows (login, signup)

---

## 1. LandingLayout (Current Home Page)

**Use Cases**: Home page, About, Pricing, Contact

**Components**:
```
┌─────────────────────────────────────────┐
│  LandingHeader (Transparent/Sticky)     │
│  - Logo, Nav Links, Login/Signup        │
└─────────────────────────────────────────┘
│                                         │
│          Page Content                   │
│          (RouterView)                   │
│                                         │
┌─────────────────────────────────────────┐
│  LandingFooter                          │
│  - Links, Social, Newsletter            │
└─────────────────────────────────────────┘
```

**Features**:
- Transparent header on scroll top
- Solid header on scroll down
- Full footer with links
- No bottom navigation (desktop focus)

---

## 2. AppLayout (Main Application)

**Use Cases**: Dashboard, My Bookings, Favorites, Settings

**Components**:
```
┌─────────────────────────────────────────┐
│  AppHeader (Solid, Always Visible)      │
│  - Logo, Search Bar, User Menu          │
└─────────────────────────────────────────┘
│                                         │
│          Page Content                   │
│          (RouterView)                   │
│                                         │
┌─────────────────────────────────────────┐
│  BottomNavigation (Mobile Only)         │
│  - Home, Search, Bookings, Profile      │
└─────────────────────────────────────────┘
```

**Features**:
- Always visible header with navigation
- User menu (profile, settings, logout)
- Mobile: Bottom navigation
- Desktop: No footer (clean interface)

---

## 3. FocusedLayout (Task Flows)

**Use Cases**: Provider Search, Booking Flow, Payment

**Components**:
```
┌─────────────────────────────────────────┐
│  SimpleHeader (Minimal)                 │
│  - Back Button, Logo, User Menu         │
└─────────────────────────────────────────┘
│                                         │
│          Full-Screen Content            │
│          (No Distractions)              │
│                                         │
│                                         │
└─────────────────────────────────────────┘
```

**Features**:
- Minimal header (no navigation clutter)
- Back button for easy exit
- No footer (maximize content space)
- Mobile: No bottom nav (focus on task)
- Example: Provider Search is focused on filtering/results

---

## 4. MinimalLayout (Auth Flows)

**Use Cases**: Login, Signup, Password Reset, Verification

**Components**:
```
┌─────────────────────────────────────────┐
│  MinimalHeader (Logo Only)              │
└─────────────────────────────────────────┘
│                                         │
│      Centered Auth Form                 │
│      (RouterView)                       │
│                                         │
│                                         │
└─────────────────────────────────────────┘
```

**Features**:
- Logo only (no navigation)
- Centered content
- No footer
- No bottom navigation
- Clean, distraction-free

---

## Implementation Architecture

### **1. Create Layout Components**

```
src/shared/components/layout/
├── LandingLayout.vue      # Home, marketing pages
├── AppLayout.vue          # Main app (dashboard, bookings)
├── FocusedLayout.vue      # Provider search, booking flow
├── MinimalLayout.vue      # Auth pages
└── components/
    ├── Header/
    │   ├── LandingHeader.vue (exists)
    │   ├── AppHeader.vue (new)
    │   ├── SimpleHeader.vue (new)
    │   └── MinimalHeader.vue (new)
    └── Footer/
        ├── LandingFooter.vue (new)
        └── AppFooter.vue (optional)
```

### **2. Route Configuration**

Each route specifies its layout via `meta`:

```typescript
// Home - Landing layout
{
  path: '/',
  component: () => import('@/views/HomeView.vue'),
  meta: { layout: 'landing' }
}

// Provider Search - Focused layout
{
  path: '/providers/search',
  component: () => import('@/modules/provider/views/ProviderSearchView.vue'),
  meta: { layout: 'focused' }
}

// Dashboard - App layout
{
  path: '/dashboard',
  component: () => import('@/views/DashboardView.vue'),
  meta: {
    layout: 'app',
    requiresAuth: true
  }
}

// Login - Minimal layout
{
  path: '/login',
  component: () => import('@/modules/auth/views/LoginView.vue'),
  meta: { layout: 'minimal' }
}
```

### **3. App.vue - Layout Renderer**

```vue
<template>
  <div id="app" :dir="direction">
    <AppToast />
    <CustomerModalsContainer />

    <!-- Dynamic Layout Rendering -->
    <component :is="currentLayout">
      <Suspense>
        <RouterView />
      </Suspense>
    </component>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import LandingLayout from '@/shared/components/layout/LandingLayout.vue'
import AppLayout from '@/shared/components/layout/AppLayout.vue'
import FocusedLayout from '@/shared/components/layout/FocusedLayout.vue'
import MinimalLayout from '@/shared/components/layout/MinimalLayout.vue'

const route = useRoute()

const layouts = {
  landing: LandingLayout,
  app: AppLayout,
  focused: FocusedLayout,
  minimal: MinimalLayout,
}

const currentLayout = computed(() => {
  const layoutName = route.meta.layout as keyof typeof layouts || 'app'
  return layouts[layoutName]
})
</script>
```

---

## Page-to-Layout Mapping

### **LandingLayout** (Marketing):
- `/` - Home
- `/about` - About Us
- `/how-it-works` - How It Works
- `/pricing` - Pricing
- `/contact` - Contact

### **AppLayout** (Authenticated):
- `/dashboard` - User Dashboard
- `/my-bookings` - My Bookings
- `/favorites` - Saved Providers
- `/settings` - Account Settings
- `/notifications` - Notifications

### **FocusedLayout** (Task Flows):
- `/providers/search` - Provider Search ⭐
- `/providers/:id` - Provider Profile
- `/book/:id` - Booking Flow
- `/payment/:id` - Payment Page

### **MinimalLayout** (Auth):
- `/login` - Login
- `/signup` - Signup
- `/forgot-password` - Password Reset
- `/verify` - Phone Verification

---

## Benefits of This Approach

### **1. User Experience**:
✅ **Consistent Navigation** - Users always know where they are
✅ **Context-Appropriate** - Layout matches user intent
✅ **Less Distraction** - Focused layouts for task completion
✅ **Professional Polish** - Consistent headers/footers

### **2. Development**:
✅ **DRY Principle** - Reusable layout components
✅ **Easy Maintenance** - Change header once, affects all pages
✅ **Clear Organization** - Layout logic separate from page logic
✅ **Flexible** - Easy to add new layouts or modify existing

### **3. Performance**:
✅ **Code Splitting** - Layouts loaded on demand
✅ **Lazy Loading** - Only load what's needed
✅ **Caching** - Layouts cached after first load

---

## Example: Provider Search Page

### **Current State** ❌:
```vue
<!-- ProviderSearchView.vue -->
<template>
  <div class="provider-search-view">
    <!-- No header, no navigation -->
    <!-- User is lost, no way to go home -->
    <ProviderFilters />
    <ProviderSearchResults />
  </div>
</template>
```

**Problems**:
- No header (user can't navigate away)
- No breadcrumbs (user doesn't know where they are)
- No user menu (can't access account)
- Inconsistent with rest of app

### **With FocusedLayout** ✅:
```vue
<!-- In routes -->
{
  path: '/providers/search',
  component: ProviderSearchView,
  meta: { layout: 'focused' }
}

<!-- FocusedLayout.vue renders -->
<template>
  <div class="focused-layout">
    <SimpleHeader>
      <BackButton />
      <Logo />
      <SearchBar />
      <UserMenu />
    </SimpleHeader>

    <main>
      <!-- ProviderSearchView content here -->
      <slot />
    </main>

    <!-- No footer - maximize content -->
  </div>
</template>
```

**Benefits**:
- ✅ User can navigate back
- ✅ Logo links to home
- ✅ Search bar always accessible
- ✅ User menu for account actions
- ✅ Consistent with booking flow
- ✅ Maximizes search/results space

---

## Responsive Behavior

### **Desktop** (> 768px):
```
LandingLayout: Full header + footer
AppLayout: Full header + sidebar (optional)
FocusedLayout: Simple header only
MinimalLayout: Logo only
```

### **Mobile** (< 768px):
```
LandingLayout: Collapsed header + footer
AppLayout: Minimal header + bottom nav
FocusedLayout: Minimal header + no bottom nav
MinimalLayout: Logo only
```

### **BottomNavigation Visibility**:
- ✅ AppLayout (mobile): Show bottom nav
- ❌ FocusedLayout: Hide (task focus)
- ❌ LandingLayout: Hide (desktop-first)
- ❌ MinimalLayout: Hide (distraction-free)

---

## Industry Examples

### **Airbnb**:
- Landing: Full header + footer
- Search: Focused header (search bar, filters)
- Booking: Minimal header (back, logo, help)
- Profile: App header (full navigation)

### **Booking.com**:
- Home: Marketing header + footer
- Search: Focused (search + filters prominent)
- Hotel Page: Simple header (back, save, share)
- Payment: Minimal (secure checkout)

### **OpenTable**:
- Home: Landing header + footer
- Restaurant Search: Focused layout
- Reservation: Minimal (checkout flow)
- My Reservations: App layout

---

## Migration Plan

### **Phase 1: Create Layout Components** (1-2 hours)
1. Create FocusedLayout.vue (for Provider Search)
2. Create SimpleHeader.vue
3. Update App.vue to use dynamic layout rendering

### **Phase 2: Update Routes** (30 mins)
1. Add `meta: { layout: 'focused' }` to provider routes
2. Add `meta: { layout: 'landing' }` to home route

### **Phase 3: Create Remaining Layouts** (2-3 hours)
1. Create AppLayout.vue + AppHeader.vue
2. Create MinimalLayout.vue + MinimalHeader.vue
3. Create LandingFooter.vue

### **Phase 4: Apply to All Routes** (1 hour)
1. Update all route definitions with layout meta
2. Test navigation between layouts
3. Verify mobile responsiveness

---

## Recommendation for Provider Search

**Use FocusedLayout**:

```vue
<!-- FocusedLayout.vue -->
<template>
  <div class="focused-layout">
    <!-- Simple Header -->
    <header class="simple-header">
      <button @click="$router.go(-1)" class="back-btn">
        <ArrowLeftIcon />
      </button>

      <router-link to="/" class="logo">
        <img src="@/assets/logo.svg" alt="Booksy" />
      </router-link>

      <div class="header-actions">
        <!-- Optional: Quick search -->
        <SearchBar v-if="!isMobile" compact />

        <!-- User menu -->
        <UserMenu />
      </div>
    </header>

    <!-- Full-screen content -->
    <main class="focused-main">
      <slot />
    </main>

    <!-- No footer - maximize space for filters/results/map -->
  </div>
</template>

<style scoped>
.simple-header {
  position: sticky;
  top: 0;
  z-index: 1000;
  background: white;
  border-bottom: 1px solid #e5e7eb;
  padding: 1rem 2rem;
  display: flex;
  align-items: center;
  gap: 1.5rem;
}

.focused-main {
  min-height: calc(100vh - 72px); /* Full viewport minus header */
}
</style>
```

---

## Next Steps

**Should I implement this layout system for you?**

**Quick Win** (30 mins):
1. Create FocusedLayout for Provider Search
2. Add SimpleHeader with back button, logo, user menu
3. Update route to use `meta: { layout: 'focused' }`

**Full System** (4-6 hours):
1. Create all 4 layout components
2. Create all header variants
3. Create footers
4. Update all routes
5. Test responsive behavior

**What would you like me to do?**
- [ ] Quick win: Just add FocusedLayout for Provider Search
- [ ] Full system: Implement complete layout architecture
- [ ] Custom: Specific layouts you want to prioritize

Let me know and I'll get started! 🚀
