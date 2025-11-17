# Customer Profile - Minimal Design Document

## Context

The Booksy platform has comprehensive provider-facing features but lacks basic customer account management. This design outlines a **minimal, lightweight** implementation that integrates customer profile features into the existing landing page using modals and sidebars—**not a separate dashboard**.

**Stakeholders:**
- Customers: Need basic account management and booking visibility
- Development Team: Must implement quickly with minimal complexity
- Support Team: Will see reduced support burden
- Product Team: Want MVP to validate customer needs

**Constraints:**
- Must follow existing DDD/CQRS patterns
- Must support Persian RTL UI consistently
- Must integrate with landing page (no separate dashboard)
- Must be simple and fast to implement (3 weeks target)
- Must have <500ms modal/sidebar load time

---

## Goals / Non-Goals

### Goals
1. **Basic Profile Management**: View/edit name, email (phone display-only)
2. **Booking Visibility**: See upcoming (5) and past (20) bookings
3. **Quick Rebooking**: Save favorite providers, rebook easily
4. **Review Management**: View and edit own reviews
5. **Notification Control**: SMS/email preferences
6. **Landing Page Integration**: No separate customer dashboard
7. **Fast Implementation**: 3 weeks, minimal complexity

### Non-Goals
1. ❌ **Avatar/Profile Images**: No image upload system
2. ❌ **Booking Export**: No PDF/Excel export
3. ❌ **Payment Methods**: No saved cards management
4. ❌ **Loyalty Points**: No points system
5. ❌ **Advanced Security**: No 2FA, no session management
6. ❌ **Privacy Controls**: No granular privacy settings
7. ❌ **Self-Service Deletion**: Manual process via support
8. ❌ **Customer Dashboard**: No dedicated pages/routes

---

## Decisions

### Decision 1: Landing Page Integration (Not Separate Dashboard)

**Options Considered:**

**A. Separate Customer Dashboard** (like provider dashboard)
- Pros: Feature parity with providers
- Cons: Confusing navigation, more development time, unnecessary complexity

**B. Landing Page Integration with Modals**
- Pros: Simple UX, familiar home base, faster development
- Cons: Limited screen space for complex features

**Decision: Option B - Landing Page Integration**

**Rationale:**
- Customers don't need full dashboard like providers
- Landing page is natural home for customers (browse services)
- Modals/sidebars provide quick access without navigation
- Less confusing than creating new "dashboard" concept
- Faster to build (3 weeks vs 6 weeks)
- Better mobile experience (bottom sheets)

**Implementation:**
```
HomeView.vue (existing landing page)
├── AppHeader.vue
│   └── UserMenuDropdown.vue (NEW)
│       ├── Profile info display
│       ├── Open Profile Edit → ProfileEditModal
│       ├── Open Bookings → BookingsSidebar
│       ├── Open Favorites → FavoritesModal
│       ├── Open Reviews → ReviewsModal
│       ├── Open Settings → SettingsModal
│       └── Logout button
│
├── HeroSection.vue (existing)
├── FeaturedProviders.vue (enhanced)
│   └── FavoriteButton.vue (NEW - heart icon)
└── ... other landing sections

Modal/Sidebar Components (NEW):
├── ProfileEditModal.vue
├── BookingsSidebar.vue
├── FavoritesModal.vue
├── ReviewsModal.vue
└── SettingsModal.vue
```

### Decision 2: No Profile Avatar/Image Upload

**Options Considered:**

**A. Avatar Upload with S3 Pre-Signed URLs**
- Pros: Personalized profile
- Cons: Complex implementation, moderation needed, security concerns

**B. Colored Circle with Initial Letter**
- Pros: Simple, no upload/storage/moderation, still visual identity
- Cons: Less personalization

**Decision: Option B - Initial Circle Only**

**Rationale:**
- Avatar upload adds 1+ week to implementation
- Requires S3 setup, image processing, moderation
- Customers don't need avatars for booking platform (unlike social networks)
- Colored circle with initial provides visual identity
- Can add avatar upload in future phase if requested

**Implementation:**
```vue
<div class="user-initial" :style="{ background: userColor }">
  {{ userInitial }}
</div>

<script>
const userInitial = computed(() => user.value.firstName?.charAt(0) || 'ک')
const userColor = computed(() => {
  const colors = ['#667eea', '#764ba2', '#f093fb', '#4facfe']
  const index = user.value.id.charCodeAt(0) % colors.length
  return colors[index]
})
</script>
```

### Decision 3: Minimal Backend - Extend UserManagement Context

**Options Considered:**

**A. New CustomerManagement Bounded Context**
- Pros: Clean separation
- Cons: Additional infrastructure, cross-context complexity

**B. Extend UserManagement Context**
- Pros: Simpler queries, shared user data, faster implementation
- Cons: Context grows larger

**Decision: Option B - Extend UserManagement**

**Rationale:**
- Customer profile is extension of user identity
- UserManagement already has auth and basic user data
- Avoids cross-context complexity for MVP
- Faster to implement
- Can extract to separate context later if needed

**New Aggregates in UserManagement:**
```
UserManagement/
├── Domain/
│   ├── Aggregates/
│   │   ├── User/ (existing)
│   │   └── FavoriteProvider/ (NEW - simple join aggregate)
│   └── ReadModels/
│       └── CustomerBookingHistory/ (NEW - event-sourced)
```

### Decision 4: Event-Driven Booking History (Not Direct Queries)

**Options Considered:**

**A. Direct Cross-Context Queries**
- Pros: Real-time data
- Cons: Violates boundaries, tight coupling

**B. Integration Events with Read Model**
- Pros: Maintains boundaries, optimized for reads
- Cons: Eventual consistency (1-2 second delay)

**Decision: Option B - Event-Driven Read Model**

**Rationale:**
- Booking history is immutable (historical data)
- 1-2 second delay acceptable for history view
- Optimized for read-heavy workload
- Enables simple filtering/sorting without cross-context joins
- Maintains architectural boundaries

**Event Flow:**
```
ServiceCatalog                    UserManagement
   ↓                                   ↓
BookingCreated      →    Store in customer_booking_history
BookingCompleted    →    Update status in read model
BookingCancelled    →    Update status in read model
```

### Decision 5: Single Pinia Store (Not Multiple)

**Options Considered:**

**A. Multiple Stores** (profile, bookings, favorites, reviews, preferences)
- Pros: Modular, clear boundaries
- Cons: More boilerplate, more files

**B. Single Customer Store**
- Pros: Simpler, less boilerplate, easier to manage
- Cons: Larger single store

**Decision: Option B - Single customer.store.ts**

**Rationale:**
- Customer features are simple and related
- Single store is easier to debug
- Less overhead for small feature set
- Can split later if store grows too large

**Implementation:**
```typescript
export const useCustomerStore = defineStore('customer', {
  state: () => ({
    profile: null,
    upcomingBookings: [],
    bookingHistory: [],
    favorites: [],
    reviews: [],
    preferences: null,
    activeModal: null
  }),
  actions: {
    async fetchProfile() { /* ... */ },
    async updateProfile() { /* ... */ },
    async fetchUpcomingBookings() { /* ... */ },
    async fetchBookingHistory() { /* ... */ },
    async addFavorite() { /* ... */ },
    async removeFavorite() { /* ... */ },
    async fetchReviews() { /* ... */ },
    async updateReview() { /* ... */ },
    async updatePreferences() { /* ... */ },
    openModal(modal) { this.activeModal = modal },
    closeModal() { this.activeModal = null }
  }
})
```

---

## Architecture

### Minimal Database Schema

**Extend users table:**
```sql
ALTER TABLE users
ADD COLUMN full_name VARCHAR(100),
ADD COLUMN email VARCHAR(255);
```

**New tables (3 only):**
```sql
-- Favorites (simple junction table)
CREATE TABLE favorite_providers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id UUID NOT NULL REFERENCES users(id),
    provider_id UUID NOT NULL REFERENCES providers(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(customer_id, provider_id)
);
CREATE INDEX idx_favorites_customer ON favorite_providers(customer_id);

-- Booking history read model (event-sourced)
CREATE TABLE customer_booking_history (
    booking_id UUID PRIMARY KEY,
    customer_id UUID NOT NULL REFERENCES users(id),
    provider_id UUID NOT NULL,
    provider_name VARCHAR(255) NOT NULL,
    service_name VARCHAR(255) NOT NULL,
    start_time TIMESTAMPTZ NOT NULL,
    status VARCHAR(50) NOT NULL,
    total_price DECIMAL(10,2),
    created_at TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_booking_history_customer_time
ON customer_booking_history(customer_id, start_time DESC);
CREATE INDEX idx_booking_history_status
ON customer_booking_history(status);

-- Preferences (simple key-value)
CREATE TABLE customer_preferences (
    customer_id UUID PRIMARY KEY REFERENCES users(id),
    sms_enabled BOOLEAN DEFAULT true,
    email_enabled BOOLEAN DEFAULT true,
    reminder_timing VARCHAR(10) DEFAULT '24h',
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

### Backend API Endpoints (10 total)

```
# Profile
GET    /api/v1/customers/profile
PATCH  /api/v1/customers/profile
  Body: { fullName: string, email?: string }

# Bookings
GET    /api/v1/customers/bookings/upcoming?limit=5
GET    /api/v1/customers/bookings/history?page=1&size=20

# Favorites
GET    /api/v1/customers/favorites
POST   /api/v1/customers/favorites/{providerId}
DELETE /api/v1/customers/favorites/{providerId}

# Reviews
GET    /api/v1/customers/reviews
PATCH  /api/v1/customers/reviews/{id}
  Body: { rating: number, text: string }

# Preferences
GET    /api/v1/customers/preferences
PATCH  /api/v1/customers/preferences
  Body: { smsEnabled: boolean, emailEnabled: boolean, reminderTiming: string }
```

### Frontend Component Tree

```
AppHeader
└── UserMenuDropdown (NEW)
    ├── User Info Display
    │   ├── Colored Circle with Initial
    │   ├── Full Name
    │   └── Phone Number
    ├── Menu Items
    │   ├── Edit Profile → ProfileEditModal
    │   ├── My Bookings → BookingsSidebar
    │   ├── My Favorites → FavoritesModal
    │   ├── My Reviews → ReviewsModal
    │   ├── Settings → SettingsModal
    │   └── Logout

FeaturedProviders (enhanced)
└── Provider Card
    └── FavoriteButton (NEW)
        └── Heart Icon (filled/outlined)

Modals/Sidebars:
├── ProfileEditModal
│   ├── Full Name Input
│   ├── Phone Display (not editable)
│   ├── Email Input (optional)
│   └── Save/Cancel Buttons
│
├── BookingsSidebar
│   ├── Upcoming Tab
│   │   └── Booking Card (×5)
│   │       ├── Provider Info
│   │       ├── Service, Date/Time
│   │       └── Cancel/Reschedule
│   └── Past Tab
│       └── Booking Card (×20, paginated)
│           ├── Provider Info
│           ├── Service, Date/Time, Status
│           └── Rebook Button
│
├── FavoritesModal
│   └── Provider Grid (2 cols)
│       └── Favorite Provider Card
│           ├── Logo, Name, Category
│           ├── Rating
│           ├── Quick Book Button
│           └── Remove Heart Icon
│
├── ReviewsModal
│   └── Review List
│       └── Review Card
│           ├── Provider, Service
│           ├── Star Rating
│           ├── Review Text
│           ├── Date
│           └── Edit Button (if <7 days)
│
└── SettingsModal
    ├── Notifications Section
    │   ├── SMS Toggle
    │   ├── Email Toggle
    │   └── Reminder Timing Dropdown
    └── Account Section
        └── Contact Support Message
```

---

## Mobile Experience

### Desktop (>768px):
- User menu dropdown in top-right corner of header
- Modals centered on screen (with overlay)
- Sidebar slides in from left

### Mobile (<768px):
- **Bottom Navigation Bar** replaces dropdown:
  ```
  [🏠 Home] [🔍 Search] [📅 Bookings] [❤️ Favorites] [👤 Profile]
  ```
- **Bottom Sheets** instead of modals:
  - Swipe down to dismiss
  - Snap to half/full height
  - Native feel
- **Bookings Sidebar** becomes full-screen overlay
- **Favorite Button** larger touch target (44px minimum)

---

## Risks / Trade-offs

### Risk 1: No Avatar Upload Limits Personalization
**Mitigation**: Use colored circle with initial, add avatar in Phase 2 if customers request

### Risk 2: Eventual Consistency for Booking History
**Mitigation**: 1-2 second delay acceptable for historical data, not real-time critical

### Risk 3: Manual Account Deletion Process
**Mitigation**: Prevents accidental deletions, legal review for GDPR, acceptable for MVP

### Risk 4: Limited Booking History (20 per page)
**Mitigation**: Most customers rarely view beyond recent bookings, pagination sufficient

---

## Migration Plan (3 Weeks)

### Week 1: Backend
- Day 1-2: Database migrations (3 tables)
- Day 3-4: Command handlers (4 handlers)
- Day 4-5: Query handlers (5 handlers)
- Day 5: CustomersController (10 endpoints)
- Weekend: Integration tests

### Week 2: Frontend
- Day 1: UserMenuDropdown component
- Day 2: ProfileEditModal + customer.store.ts
- Day 3: BookingsSidebar
- Day 4: FavoritesModal + favorite buttons
- Day 5: ReviewsModal + SettingsModal
- Weekend: Persian translations, mobile styling

### Week 3: Integration & Polish
- Day 1-2: Connect to booking flow, test rebooking
- Day 3: Mobile bottom navigation, bottom sheets
- Day 4: Performance optimization, caching
- Day 5: Bug fixes, deployment prep
- Weekend: Staging deployment, UAT

---

## Open Questions

1. **Review Edit Window**: 7 days to edit reviews? Or shorter?
   - **Proposal**: 7 days (matches industry standard)

2. **Favorites Limit**: Cap at 100 favorites per customer?
   - **Proposal**: Yes, 100 limit with "Your favorites list is full" message

3. **Booking History Retention**: Keep all bookings forever?
   - **Proposal**: Yes, indefinite retention (storage is cheap)

4. **Phone Number Change**: Allow self-service or support-only?
   - **Proposal**: Support-only (requires OTP verification, security concern)

5. **Account Deletion**: Manual process or self-service?
   - **Proposal**: Manual via support (prevents accidental deletion, allows legal review)

---

## Success Metrics

### Technical
- Modal load time: <500ms
- Booking history query: <300ms (p95)
- Favorite add/remove: <200ms
- Zero N+1 query problems
- <30KB bundle size increase

### User Experience
- >50% save at least 1 favorite provider
- >60% view bookings within first week
- >40% update profile info
- >30% use rebooking feature

### Business
- 15% fewer "where are my bookings?" support tickets
- +10% rebooking rate
- +5% customer retention (90-day)
