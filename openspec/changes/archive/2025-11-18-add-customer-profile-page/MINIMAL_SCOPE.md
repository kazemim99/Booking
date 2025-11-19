# Minimal Customer Profile - Essential Features Only

## Simplified Scope

Based on user feedback, we're removing unnecessary complexity and focusing on **essential customer needs only**.

---

## ✅ KEEP - Essential Features

### 1. Basic Profile Info (Name, Phone, Email)
**Why**: Needed for bookings and contact
**Implementation**: Simple edit form in modal
**No Avatar/Image Upload** ❌

### 2. My Bookings (Upcoming & History)
**Why**: Core customer need - see appointments
**Implementation**: Sidebar with two tabs
- Upcoming bookings (next 5)
- Past bookings (paginated list)
**Actions**: Cancel, Reschedule, Rebook
**No Export to PDF/Excel** ❌

### 3. Favorite Providers
**Why**: Quick access to preferred providers
**Implementation**:
- Heart button on provider cards
- Favorites modal showing saved providers
**Actions**: Add, Remove, Quick Book

### 4. My Reviews
**Why**: See what I've reviewed, edit recent reviews
**Implementation**: Simple list of submitted reviews
**Actions**: View, Edit (within 7 days)

### 5. Basic Notification Preferences
**Why**: Control SMS/Email reminders
**Implementation**: Simple toggles in settings modal
- SMS notifications (on/off)
- Email notifications (on/off)
- Reminder timing (dropdown: 1h, 24h, 3 days)

---

## ❌ REMOVE - Unnecessary Complexity

### Profile Image/Avatar
- **Removed**: No image upload
- **Alternative**: Show first letter of name in colored circle
- **Reason**: Adds complexity, not essential for booking platform

### Booking History Export (PDF/Excel)
- **Removed**: No export functionality
- **Alternative**: Just view in browser
- **Reason**: Customers can screenshot if needed, export rarely used

### Payment Methods Management
- **Removed**: No saved payment cards UI
- **Alternative**: Enter payment each time during booking
- **Reason**: Security concern, payment gateway handles this

### Loyalty Points System
- **Removed**: No points tracking/display
- **Reason**: Adds complexity, not MVP feature

### Privacy Settings
- **Removed**: No privacy toggles
- **Alternative**: Simple privacy policy link
- **Reason**: Customers don't need granular privacy controls for v1

### Security Settings (2FA, Active Sessions)
- **Removed**: No 2FA setup, no session management
- **Alternative**: Basic phone-based auth is sufficient
- **Reason**: Overkill for customer accounts (unlike provider accounts)

### Account Data Export (GDPR)
- **Removed**: No data export button
- **Alternative**: Contact support for data requests
- **Reason**: Legal requirement but can be manual for v1

### Account Deletion
- **Removed**: No self-service deletion
- **Alternative**: Contact support to delete account
- **Reason**: Prevents accidental deletions, manual review is safer

---

## Revised Component Structure

### Landing Page Integration

```
HomeView.vue
├── AppHeader.vue
│   └── UserMenuDropdown.vue ← Simple menu
│       ├── Name + Phone display
│       ├── "نوبت‌های من" → Opens bookings
│       ├── "علاقه‌مندی‌ها" → Opens favorites
│       ├── "نظرات من" → Opens reviews
│       ├── "تنظیمات" → Opens settings
│       └── "خروج" → Logout
│
├── HeroSection.vue
├── FeaturedProviders.vue
│   └── FavoriteButton.vue ← Heart icon on each card
└── ...

Modals (4 only):
├── ProfileEditModal.vue ← Name, Phone, Email
├── BookingsSidebar.vue ← Upcoming + History
├── FavoritesModal.vue ← Saved providers
├── ReviewsModal.vue ← My reviews
└── SettingsModal.vue ← Notification preferences only
```

---

## Minimal User Menu Dropdown

```vue
<template>
  <div class="user-menu" dir="rtl">
    <button @click="toggle" class="user-button">
      <!-- No avatar, just colored circle with initial -->
      <div class="user-initial">{{ userInitial }}</div>
      <span>{{ user.firstName }}</span>
      <ChevronDownIcon />
    </button>

    <div v-if="isOpen" class="dropdown">
      <!-- Profile Info (not editable here) -->
      <div class="profile-info">
        <div class="user-initial large">{{ userInitial }}</div>
        <div>
          <h4>{{ user.fullName }}</h4>
          <p>{{ user.phoneNumber }}</p>
        </div>
        <button @click="editProfile">ویرایش</button>
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

      <button @click="openReviews" class="menu-item">
        <StarIcon />
        نظرات من
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

<script setup>
const userInitial = computed(() => user.value.firstName?.charAt(0) || 'ک')
</script>
```

---

## Minimal Components

### 1. ProfileEditModal.vue (Simplified)

**Fields**:
- ✅ Full Name (text input, required)
- ✅ Phone Number (display only, not editable)
- ✅ Email (text input, optional)

**Buttons**:
- Save
- Cancel

**No**: Avatar upload, birth date, gender, etc.

```vue
<template>
  <modal title="ویرایش اطلاعات" @close="emit('close')">
    <form @submit.prevent="save">
      <div class="form-group">
        <label>نام و نام خانوادگی</label>
        <input v-model="form.fullName" required />
      </div>

      <div class="form-group">
        <label>شماره موبایل</label>
        <input :value="user.phoneNumber" disabled />
        <small>برای تغییر شماره با پشتیبانی تماس بگیرید</small>
      </div>

      <div class="form-group">
        <label>ایمیل (اختیاری)</label>
        <input v-model="form.email" type="email" />
      </div>

      <div class="actions">
        <button type="submit">ذخیره</button>
        <button type="button" @click="emit('close')">انصراف</button>
      </div>
    </form>
  </modal>
</template>
```

---

### 2. BookingsSidebar.vue (Simplified)

**Tabs**:
- آینده (Upcoming) - Shows next 5 bookings
- گذشته (Past) - Shows last 20 bookings

**Booking Card**:
- Provider name
- Service name
- Date/Time (Persian)
- Status badge
- Actions: Cancel (upcoming), Rebook (past)

**No**:
- Countdown timers
- Staff member display
- Export buttons
- Advanced filters (just simple list)

```vue
<template>
  <sidebar title="نوبت‌های من" @close="emit('close')">
    <tabs>
      <tab name="آینده">
        <booking-card
          v-for="booking in upcomingBookings"
          :key="booking.id"
          :booking="booking"
        >
          <button @click="cancelBooking(booking.id)">لغو</button>
          <button @click="rescheduleBooking(booking.id)">تغییر زمان</button>
        </booking-card>

        <empty-state v-if="!upcomingBookings.length">
          شما نوبت آینده‌ای ندارید
        </empty-state>
      </tab>

      <tab name="گذشته">
        <booking-card
          v-for="booking in pastBookings"
          :key="booking.id"
          :booking="booking"
        >
          <button @click="rebookBooking(booking)">رزرو مجدد</button>
        </booking-card>
      </tab>
    </tabs>
  </sidebar>
</template>
```

---

### 3. FavoritesModal.vue (Unchanged)

**Content**: Grid of favorite providers
**Actions**: Remove from favorites, Quick book

---

### 4. ReviewsModal.vue (New, Simple)

**Content**: List of customer's reviews

**Review Card**:
- Provider name + logo
- Service name
- Star rating
- Review text
- Date
- Edit button (if < 7 days old)

```vue
<template>
  <modal title="نظرات من" @close="emit('close')">
    <div v-for="review in reviews" :key="review.id" class="review-card">
      <div class="review-header">
        <img :src="review.providerLogo" />
        <div>
          <h4>{{ review.providerName }}</h4>
          <p>{{ review.serviceName }}</p>
        </div>
      </div>

      <div class="review-rating">
        <star-rating :value="review.rating" readonly />
        <span>{{ formatDate(review.createdAt) }}</span>
      </div>

      <p class="review-text">{{ review.text }}</p>

      <button
        v-if="canEdit(review)"
        @click="editReview(review.id)"
        class="edit-btn"
      >
        ویرایش
      </button>
    </div>

    <empty-state v-if="!reviews.length">
      شما هنوز نظری ثبت نکرده‌اید
    </empty-state>
  </modal>
</template>
```

---

### 5. SettingsModal.vue (Minimal)

**Only Notification Preferences**:

```vue
<template>
  <modal title="تنظیمات" @close="emit('close')">
    <section>
      <h3>اعلان‌ها</h3>

      <div class="setting-row">
        <label>اعلان پیامکی</label>
        <toggle v-model="preferences.smsEnabled" />
      </div>

      <div class="setting-row">
        <label>اعلان ایمیل</label>
        <toggle v-model="preferences.emailEnabled" />
      </div>

      <div class="setting-row">
        <label>زمان یادآوری</label>
        <select v-model="preferences.reminderTiming">
          <option value="1h">۱ ساعت قبل</option>
          <option value="24h">۱ روز قبل</option>
          <option value="3d">۳ روز قبل</option>
        </select>
      </div>
    </section>

    <hr />

    <section>
      <h3>حساب کاربری</h3>
      <p>برای تغییر شماره موبایل یا حذف حساب، با پشتیبانی تماس بگیرید.</p>
      <a href="tel:02177777777">۰۲۱-۷۷۷۷۷۷۷۷</a>
    </section>

    <button @click="savePreferences" class="save-btn">ذخیره تنظیمات</button>
  </modal>
</template>
```

---

## Revised Backend API (Minimal)

### ✅ Keep These Endpoints:

```
GET    /api/v1/customers/profile
PATCH  /api/v1/customers/profile
  Body: { fullName, email }

GET    /api/v1/customers/bookings/upcoming?limit=5
GET    /api/v1/customers/bookings/history?page=1&size=20

GET    /api/v1/customers/favorites
POST   /api/v1/customers/favorites/{providerId}
DELETE /api/v1/customers/favorites/{providerId}

GET    /api/v1/customers/reviews
PATCH  /api/v1/customers/reviews/{id}
  Body: { rating, text }

GET    /api/v1/customers/preferences
PATCH  /api/v1/customers/preferences
  Body: { smsEnabled, emailEnabled, reminderTiming }
```

### ❌ Remove These Endpoints:

```
POST   /api/v1/customers/avatar/upload-url  ← No avatar
GET    /api/v1/customers/statistics  ← No stats display
GET    /api/v1/customers/loyalty/transactions  ← No loyalty
POST   /api/v1/customers/data-export  ← Manual process
DELETE /api/v1/customers/account  ← Manual process
GET    /api/v1/customers/sessions  ← No session mgmt
POST   /api/v1/customers/2fa/enable  ← No 2FA
```

---

## Revised Database Schema (Minimal)

### ✅ Keep These Tables:

```sql
-- Extend users table
ALTER TABLE users ADD COLUMN full_name VARCHAR(100);
ALTER TABLE users ADD COLUMN email VARCHAR(255);

-- Favorites
CREATE TABLE favorite_providers (
    id UUID PRIMARY KEY,
    customer_id UUID REFERENCES users(id),
    provider_id UUID REFERENCES providers(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(customer_id, provider_id)
);
CREATE INDEX idx_favorites_customer ON favorite_providers(customer_id);

-- Booking history read model
CREATE TABLE customer_booking_history (
    booking_id UUID PRIMARY KEY,
    customer_id UUID REFERENCES users(id),
    provider_id UUID,
    provider_name VARCHAR(255),
    service_name VARCHAR(255),
    start_time TIMESTAMPTZ,
    status VARCHAR(50),
    total_price DECIMAL(10,2),
    created_at TIMESTAMPTZ
);
CREATE INDEX idx_booking_history ON customer_booking_history(customer_id, start_time DESC);

-- Preferences
CREATE TABLE customer_preferences (
    customer_id UUID PRIMARY KEY REFERENCES users(id),
    sms_enabled BOOLEAN DEFAULT true,
    email_enabled BOOLEAN DEFAULT true,
    reminder_timing VARCHAR(10) DEFAULT '24h',
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

### ❌ Remove These Tables:

```sql
customer_profiles  ← Not needed (use users table)
payment_methods  ← Not implementing
loyalty_transactions  ← Not implementing
customer_statistics  ← Not implementing
active_sessions  ← Not implementing
```

---

## Implementation Effort (Revised)

### Original Proposal: 6 weeks, 200+ tasks
### Minimal Proposal: **3 weeks, ~50 tasks**

**Week 1**: Backend
- [ ] Database schema (3 tables)
- [ ] CustomerProfile commands/queries
- [ ] FavoriteProvider commands/queries
- [ ] API endpoints (10 endpoints)
- [ ] Integration tests

**Week 2**: Frontend Core
- [ ] UserMenuDropdown component
- [ ] ProfileEditModal component
- [ ] BookingsSidebar component
- [ ] FavoritesModal component
- [ ] ReviewsModal component
- [ ] SettingsModal component
- [ ] customer.store.ts (Pinia)
- [ ] customer.service.ts (API)

**Week 3**: Integration & Polish
- [ ] Add favorite buttons to provider cards
- [ ] Connect bookings to rebooking flow
- [ ] Mobile bottom navigation
- [ ] Bottom sheets for mobile
- [ ] Persian translations
- [ ] Testing & bug fixes
- [ ] Deployment

---

## What This Simplification Achieves

### ✅ Faster Development
- 3 weeks instead of 6 weeks
- 50 tasks instead of 200+ tasks
- Fewer components to maintain

### ✅ Simpler UX
- No overwhelming feature set
- Focused on core needs
- Less confusion for customers

### ✅ Easier Maintenance
- Less code to maintain
- Fewer edge cases
- Simpler state management

### ✅ Better Performance
- Smaller bundle size
- Fewer API calls
- Less data to cache

### ✅ MVP-Focused
- Ship essential features first
- Iterate based on user feedback
- Add complexity only if needed

---

## Future Enhancements (Post-MVP)

If customers request these features later:
- Avatar/profile image upload
- Loyalty points system
- Advanced booking filters
- Export to PDF/Excel
- Self-service account deletion
- 2FA and session management

**But for now, keep it simple!** 🎯
