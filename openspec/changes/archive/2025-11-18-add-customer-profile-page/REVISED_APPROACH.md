# Revised Customer Profile Approach - Landing Page Integration

## Key Decision: NO Separate Customer Dashboard

**User Feedback**: Creating a separate customer dashboard like the provider dashboard would be **confusing and unnecessary** for customers.

**Revised Approach**: Integrate customer profile features into the **existing landing page template** with lightweight modal/overlay components.

---

## New Simplified Architecture

### Landing Page Enhancements

**Current Landing Page** (`HomeView.vue`):
- HeroSection
- CategoryGrid
- FeaturedProviders
- HowItWorks
- Testimonials
- CTASection

**Add to Landing Page**:
- **User Menu Dropdown** (top-right when authenticated)
  - Profile Quick View (name, avatar, points)
  - My Bookings (link to modal)
  - My Favorites (link to modal)
  - Settings (link to modal)
  - Logout

**Customer Features as Modals/Overlays**:
1. **Profile Edit Modal** - Edit name, email, avatar, preferences
2. **My Bookings Sidebar** - Slide-in panel showing upcoming & history
3. **Favorites Modal** - Grid of favorite providers
4. **Settings Modal** - Notification preferences, privacy, security

---

## Benefits of This Approach

### ✅ Simplicity
- No new "customer dashboard" concept to learn
- Familiar landing page remains the home base
- Quick access via dropdown menu
- Less cognitive load

### ✅ Consistent with Customer Journey
- Customer lands on home page → sees services
- Can quickly check bookings without leaving home
- Book new service without navigating away
- Favorites integrated into provider search

### ✅ Less Development Overhead
- Reuse existing HomeView.vue layout
- Add modals/sidebars instead of full pages
- Fewer routes, simpler navigation
- Smaller bundle size

### ✅ Better Mobile Experience
- Bottom sheets for mobile (not full pages)
- Swipe gestures to dismiss
- No deep navigation stack
- Faster interactions

---

## Revised Component Structure

### Landing Page Integration

```
HomeView.vue (existing)
├── AppHeader.vue
│   └── UserMenuDropdown.vue (NEW)
│       ├── ProfileQuickView
│       ├── Link to Bookings
│       ├── Link to Favorites
│       └── Link to Settings
├── HeroSection.vue (existing)
├── CategoryGrid.vue (existing)
├── FeaturedProviders.vue (enhanced with favorites)
│   └── FavoriteButton.vue (NEW - heart icon)
└── ... other sections

Modal/Overlay Components (NEW)
├── ProfileEditModal.vue
├── BookingsSidebar.vue
├── FavoritesModal.vue
└── SettingsModal.vue
```

### User Menu Dropdown (Top-Right)

```vue
<template>
  <div class="user-menu" dir="rtl">
    <button @click="toggleMenu" class="avatar-button">
      <img :src="user.avatar" alt="avatar" />
      <span>{{ user.firstName }}</span>
      <ChevronDownIcon />
    </button>

    <div v-show="isOpen" class="dropdown">
      <!-- Profile Quick View -->
      <div class="profile-card">
        <img :src="user.avatar" />
        <div>
          <h4>{{ user.fullName }}</h4>
          <p>{{ user.email }}</p>
        </div>
        <button @click="openProfileEdit">ویرایش</button>
      </div>

      <!-- Quick Stats -->
      <div class="quick-stats">
        <div>
          <CalendarIcon />
          <span>{{ upcomingBookingsCount }} نوبت آینده</span>
        </div>
        <div>
          <StarIcon />
          <span>{{ loyaltyPoints }} امتیاز</span>
        </div>
      </div>

      <!-- Menu Items -->
      <button @click="openBookings" class="menu-item">
        <CalendarIcon />
        نوبت‌های من
      </button>

      <button @click="openFavorites" class="menu-item">
        <HeartIcon />
        علاقه‌مندی‌ها
      </button>

      <button @click="openSettings" class="menu-item">
        <SettingsIcon />
        تنظیمات
      </button>

      <hr />

      <button @click="logout" class="menu-item danger">
        <LogoutIcon />
        خروج
      </button>
    </div>
  </div>
</template>
```

---

## Revised Component Specs

### 1. ProfileEditModal.vue

**Trigger**: Click "ویرایش" (Edit) in user menu dropdown

**Modal Content**:
- Avatar upload (click to change, crop interface)
- Full name input
- Email input (optional)
- Birth date (Jalali picker, optional)
- Save/Cancel buttons

**Size**: Medium modal (600px width)

**Mobile**: Full-screen bottom sheet

---

### 2. BookingsSidebar.vue

**Trigger**: Click "نوبت‌های من" (My Bookings) in menu

**Sidebar Style**: Slide-in from left (800px width)

**Tabs**:
- **آینده** (Upcoming) - Top 10 upcoming bookings
- **تاریخچه** (History) - Paginated past bookings

**Upcoming Booking Card**:
- Provider name + avatar
- Service name
- Date/time (Jalali, Persian numbers)
- Countdown timer
- Quick actions: Cancel, Reschedule, View Details

**History Entry**:
- Provider name + service
- Date, status badge
- Actions: Rebook, View Receipt

**Footer**: "رزرو نوبت جدید" (Book New) button

**Mobile**: Full-screen overlay, swipe right to close

---

### 3. FavoritesModal.vue

**Trigger**: Click "علاقه‌مندی‌ها" (Favorites) in menu

**Modal Content**:
- Grid of favorite provider cards (2 columns)
- Each card:
  - Provider logo/avatar
  - Business name
  - Category
  - Rating
  - "رزرو" (Book) button
  - Heart icon (filled, click to remove)
- Empty state: "شما هنوز ارائه‌دهنده‌ای را به علاقه‌مندی‌ها اضافه نکرده‌اید"

**Size**: Large modal (900px width)

**Mobile**: Full-screen with scroll

---

### 4. SettingsModal.vue

**Trigger**: Click "تنظیمات" (Settings) in menu

**Modal Content** (Accordion sections):

**اعلان‌ها** (Notifications):
- Toggle: SMS notifications
- Toggle: Email notifications
- Dropdown: Reminder timing (1h, 24h, 3 days before)

**حریم خصوصی** (Privacy):
- Toggle: Allow personalized recommendations
- Toggle: Share analytics data

**امنیت** (Security):
- Button: Enable 2FA
- Button: View active sessions
- Button: Change phone number

**حساب کاربری** (Account):
- Button: Download my data
- Button: Delete account (warning)

**Size**: Medium modal (700px width)

**Mobile**: Full-screen with tabs

---

## Enhanced Landing Page Features

### Favorite Button on Provider Cards

**Location**: `FeaturedProviders.vue` component

**Enhancement**: Add heart icon to each provider card

```vue
<div class="provider-card">
  <img :src="provider.logo" />
  <h3>{{ provider.name }}</h3>
  <p>{{ provider.category }}</p>

  <!-- NEW: Favorite Button -->
  <button
    @click.stop="toggleFavorite(provider.id)"
    class="favorite-btn"
    :class="{ active: isFavorite(provider.id) }"
  >
    <HeartIcon :filled="isFavorite(provider.id)" />
  </button>
</div>
```

**Behavior**:
- Click heart → Add to favorites (API call)
- Filled heart → Already favorited, click to remove
- Toast notification: "به علاقه‌مندی‌ها اضافه شد" / "از علاقه‌مندی‌ها حذف شد"

---

### Upcoming Booking Banner (Optional)

**Location**: Above `HeroSection` (conditionally rendered if user has upcoming booking)

```vue
<div v-if="nextBooking" class="upcoming-booking-banner">
  <div class="banner-content">
    <CalendarIcon />
    <div>
      <strong>نوبت بعدی شما</strong>
      <p>{{ nextBooking.serviceName }} در {{ nextBooking.providerName }}</p>
      <p>{{ formatDate(nextBooking.startTime) }} • {{ countdown }}</p>
    </div>
  </div>
  <button @click="openBookingDetails">جزئیات</button>
</div>
```

**Styling**: Subtle gradient banner, dismissible

---

## Revised Routing

### No New Routes!

**Existing Routes Remain**:
- `/` - Landing page (HomeView.vue)
- `/providers` - Provider search
- `/providers/:id` - Provider details
- `/booking/:providerId` - Booking wizard

**No Customer Dashboard Routes** ❌
- ~~`/customer/profile`~~
- ~~`/customer/bookings`~~
- ~~`/customer/favorites`~~
- ~~`/customer/settings`~~

**Query Parameters for Modals** (optional, for deep linking):
- `/?modal=profile` - Opens profile edit modal
- `/?modal=bookings` - Opens bookings sidebar
- `/?modal=favorites` - Opens favorites modal
- `/?modal=settings` - Opens settings modal

---

## Backend API (Unchanged)

Backend API endpoints **remain the same** as original proposal:
- `GET /api/v1/customers/profile`
- `PATCH /api/v1/customers/profile`
- `GET /api/v1/customers/bookings/upcoming`
- `GET /api/v1/customers/bookings/history`
- `GET /api/v1/customers/favorites`
- `POST /api/v1/customers/favorites/{providerId}`
- `DELETE /api/v1/customers/favorites/{providerId}`
- `PATCH /api/v1/customers/preferences`

---

## State Management (Pinia)

### Simplified Stores

**customer.store.ts** (single store, not multiple):

```typescript
export const useCustomerStore = defineStore('customer', {
  state: () => ({
    profile: null as CustomerProfile | null,
    upcomingBookings: [] as Booking[],
    bookingHistory: [] as Booking[],
    favorites: [] as FavoriteProvider[],
    preferences: null as CustomerPreferences | null,

    // UI state
    activeModal: null as 'profile' | 'bookings' | 'favorites' | 'settings' | null,
  }),

  actions: {
    async fetchProfile() { /* ... */ },
    async updateProfile(data) { /* ... */ },
    async fetchUpcomingBookings() { /* ... */ },
    async fetchBookingHistory(page) { /* ... */ },
    async fetchFavorites() { /* ... */ },
    async addFavorite(providerId) { /* ... */ },
    async removeFavorite(providerId) { /* ... */ },
    async updatePreferences(prefs) { /* ... */ },

    // UI actions
    openModal(modal: 'profile' | 'bookings' | 'favorites' | 'settings') {
      this.activeModal = modal
    },
    closeModal() {
      this.activeModal = null
    }
  }
})
```

---

## Mobile Experience

### Bottom Navigation (Mobile Only)

**Replace desktop header menu with bottom nav on mobile**:

```
┌─────────────────────────┐
│   Landing Page Content  │
│                         │
│   (scroll infinitely)   │
│                         │
└─────────────────────────┘
┌─────────────────────────┐
│ [🏠] [🔍] [📅] [❤️] [👤]│  ← Fixed bottom bar
└─────────────────────────┘
   Home Search Books Favs  Me
```

**Bottom Nav Buttons**:
1. **Home** - Scrolls to top of landing page
2. **Search** - Opens provider search
3. **Bookings** - Opens bookings bottom sheet
4. **Favorites** - Opens favorites bottom sheet
5. **Profile** - Opens user menu (bottom sheet)

**Bottom Sheets** (not modals):
- Swipe down to dismiss
- Snap to half/full height
- Native feel on mobile

---

## Implementation Priority (Revised)

### Phase 1: Core Profile (Week 1-2)
- [ ] Backend: CustomerProfile aggregate, API endpoints
- [ ] Frontend: UserMenuDropdown component
- [ ] Frontend: ProfileEditModal component
- [ ] Store: customer.store.ts
- [ ] API service: customer.service.ts

### Phase 2: Bookings Integration (Week 2-3)
- [ ] Backend: Booking history read model, queries
- [ ] Frontend: BookingsSidebar component
- [ ] Upcoming booking banner (conditional)
- [ ] Countdown timers

### Phase 3: Favorites System (Week 3-4)
- [ ] Backend: FavoriteProvider aggregate, commands/queries
- [ ] Frontend: Favorite button on provider cards
- [ ] Frontend: FavoritesModal component
- [ ] Toast notifications for add/remove

### Phase 4: Settings & Preferences (Week 4-5)
- [ ] Backend: Preferences update commands
- [ ] Frontend: SettingsModal component
- [ ] 2FA setup flow (if required)
- [ ] Data export/delete account flows

### Phase 5: Polish & Mobile (Week 5-6)
- [ ] Mobile bottom navigation
- [ ] Bottom sheets for mobile
- [ ] Swipe gestures
- [ ] Performance optimization
- [ ] Accessibility audit

---

## Benefits Summary

### For Customers:
- ✅ Simple, familiar landing page
- ✅ Quick access to bookings without navigation
- ✅ Inline favorites on provider search
- ✅ Less confusion (no "dashboard" concept)

### For Development:
- ✅ Faster implementation (modals vs. full pages)
- ✅ Smaller codebase (fewer components)
- ✅ Easier maintenance (less routing complexity)
- ✅ Better code reuse

### For UX:
- ✅ No unnecessary navigation
- ✅ Contextual actions (favorites on provider cards)
- ✅ Faster task completion
- ✅ Better mobile experience (bottom sheets)

---

## What Changes from Original Proposal

### ❌ Removed:
- Separate customer dashboard pages
- Full-page profile views
- Dedicated bookings/favorites/settings pages
- Complex multi-tab navigation
- Sidebar layouts

### ✅ Replaced With:
- User menu dropdown (top-right)
- Modal/sidebar overlays
- Inline favorite buttons
- Bottom navigation (mobile)
- Landing page remains home base

### ✅ Kept (Backend):
- All backend APIs unchanged
- Same aggregates and commands
- Same database schema
- Same event-driven architecture

---

## Next Steps

1. **Approve Revised Approach**: Confirm landing page integration is preferred
2. **Update Original Proposal**: Revise [proposal.md](proposal.md) to reflect modal-based approach
3. **Update Design Doc**: Revise [design.md](design.md) frontend decisions
4. **Update Spec**: Revise [specs/customer-profile/spec.md](specs/customer-profile/spec.md) scenarios for modals
5. **Update Tasks**: Revise [tasks.md](tasks.md) to reflect simpler frontend implementation

---

**This approach is simpler, faster to build, and better UX for customers!** 🎉
