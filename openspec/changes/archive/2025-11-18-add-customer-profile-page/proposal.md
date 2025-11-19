# Customer Profile - Minimal Essential Features Proposal

## Why

The Booksy platform currently lacks basic customer account management features. While the provider experience is fully developed with dashboards and business management tools, customers have no way to:
- View their upcoming and past bookings
- Save favorite providers for quick access
- Manage basic profile information (name, email)
- Control notification preferences
- View and edit their reviews

This creates an asymmetric user experience and increases support burden as customers cannot self-serve basic account tasks. A lightweight customer profile system integrated into the landing page is essential for:

1. **Basic Account Management**: Customers need to view/edit name, email, and phone number
2. **Booking Visibility**: Customers need to see upcoming appointments and booking history
3. **Quick Rebooking**: Customers need to save favorite providers and rebook easily
4. **Review Management**: Customers need to view and edit their submitted reviews
5. **Notification Control**: Customers need to manage SMS/email preferences

**Important**: This is a **minimal MVP** focused on essential features only. No complex dashboard, no avatar uploads, no loyalty points, no advanced security features.

## What Changes

### Landing Page Integration (No Separate Dashboard)

**User Menu Dropdown** (top-right corner when authenticated):
- Shows user's first name + initial circle (no avatar images)
- Dropdown menu with links to:
  - Edit Profile (opens modal)
  - My Bookings (opens sidebar)
  - My Favorites (opens modal)
  - My Reviews (opens modal)
  - Settings (opens modal)
  - Logout

**Favorite Buttons** on provider cards:
- Heart icon on each provider card in FeaturedProviders
- Click to add/remove from favorites
- Toast notification on action

### Minimal Components (Modals/Sidebars Only)

**1. ProfileEditModal.vue**
- Full name (editable)
- Phone number (display only, not editable)
- Email (editable, optional)
- Save/Cancel buttons
- **No avatar upload, no birth date, no gender**

**2. BookingsSidebar.vue**
- Slide-in sidebar from left
- Two tabs: Upcoming (5 bookings) and Past (20 bookings)
- Each booking shows: provider, service, date/time, status
- Actions: Cancel (upcoming), Rebook (past)
- **No export, no advanced filters, no countdown timers**

**3. FavoritesModal.vue**
- Grid of favorite provider cards (2 columns)
- Each card: logo, name, category, rating, Quick Book button
- Remove from favorites button
- Empty state with illustration

**4. ReviewsModal.vue**
- List of customer's submitted reviews
- Each review: provider, service, rating, text, date
- Edit button (if within 7 days)
- Empty state

**5. SettingsModal.vue**
- **Notifications section only**:
  - SMS notifications toggle
  - Email notifications toggle
  - Reminder timing dropdown (1h, 24h, 3 days)
- Contact support section for account changes
- **No 2FA, no session management, no privacy controls**

### Backend Changes (Minimal)

**Extend UserManagement Bounded Context**:
- Add `full_name` and `email` to `users` table
- New aggregate: `FavoriteProvider` (customer-provider relationship)
- New read model: `CustomerBookingHistory` (synced via events)
- New table: `customer_preferences` (notification settings)

**API Endpoints** (10 total):
```
GET    /api/v1/customers/profile
PATCH  /api/v1/customers/profile { fullName, email }

GET    /api/v1/customers/bookings/upcoming?limit=5
GET    /api/v1/customers/bookings/history?page=1&size=20

GET    /api/v1/customers/favorites
POST   /api/v1/customers/favorites/{providerId}
DELETE /api/v1/customers/favorites/{providerId}

GET    /api/v1/customers/reviews
PATCH  /api/v1/customers/reviews/{id} { rating, text }

GET    /api/v1/customers/preferences
PATCH  /api/v1/customers/preferences { smsEnabled, emailEnabled, reminderTiming }
```

**Command Handlers**:
- UpdateCustomerProfile
- AddFavoriteProvider
- RemoveFavoriteProvider
- UpdateNotificationPreferences

**Query Handlers**:
- GetCustomerProfile
- GetUpcomingBookings
- GetBookingHistory
- GetFavoriteProviders
- GetCustomerReviews

**Event Subscribers**:
- BookingCreatedEvent → Update customer_booking_history
- BookingCompletedEvent → Update customer_booking_history
- BookingCancelledEvent → Update customer_booking_history
- ProviderDeletedEvent → Soft-delete favorites

### Database Schema (Minimal)

**Extend users table**:
```sql
ALTER TABLE users ADD COLUMN full_name VARCHAR(100);
ALTER TABLE users ADD COLUMN email VARCHAR(255);
```

**New tables** (3 only):
```sql
CREATE TABLE favorite_providers (
    id UUID PRIMARY KEY,
    customer_id UUID REFERENCES users(id),
    provider_id UUID REFERENCES providers(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(customer_id, provider_id)
);

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

CREATE TABLE customer_preferences (
    customer_id UUID PRIMARY KEY REFERENCES users(id),
    sms_enabled BOOLEAN DEFAULT true,
    email_enabled BOOLEAN DEFAULT true,
    reminder_timing VARCHAR(10) DEFAULT '24h',
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

### Frontend State Management

**Single Pinia Store** (`customer.store.ts`):
```typescript
export const useCustomerStore = defineStore('customer', {
  state: () => ({
    profile: null as CustomerProfile | null,
    upcomingBookings: [] as Booking[],
    bookingHistory: [] as Booking[],
    favorites: [] as FavoriteProvider[],
    reviews: [] as Review[],
    preferences: null as CustomerPreferences | null,
    activeModal: null as 'profile' | 'bookings' | 'favorites' | 'reviews' | 'settings' | null,
  }),
  actions: {
    // Profile
    async fetchProfile() { /* ... */ },
    async updateProfile(data) { /* ... */ },

    // Bookings
    async fetchUpcomingBookings() { /* ... */ },
    async fetchBookingHistory(page) { /* ... */ },

    // Favorites
    async fetchFavorites() { /* ... */ },
    async addFavorite(providerId) { /* ... */ },
    async removeFavorite(providerId) { /* ... */ },

    // Reviews
    async fetchReviews() { /* ... */ },
    async updateReview(id, data) { /* ... */ },

    // Preferences
    async fetchPreferences() { /* ... */ },
    async updatePreferences(data) { /* ... */ },

    // UI
    openModal(modal) { this.activeModal = modal },
    closeModal() { this.activeModal = null }
  }
})
```

### Mobile Responsive Design

**Desktop**: User menu dropdown in header

**Mobile** (<768px):
- Bottom navigation bar with 5 tabs:
  - Home (landing page)
  - Search (provider search)
  - Bookings (opens bottom sheet)
  - Favorites (opens bottom sheet)
  - Profile (opens user menu as bottom sheet)
- Bottom sheets instead of modals (swipe to dismiss)

### Persian RTL Support

- All components designed RTL-first
- Jalali calendar for date displays
- Persian number formatting (۰۱۲۳...)
- Persian translations (~50 keys, not 100+)
- User initial in Persian letters

## Impact

### Affected Specs
- **NEW: customer-profile** - Core profile management (minimal scope)
- **MODIFIED: authentication** - Initialize customer preferences after registration

### Affected Code

**Frontend** (`booksy-frontend/src/`):
- `components/common/UserMenuDropdown.vue` (NEW)
- `modules/customer/views/` (NOT NEEDED - using modals)
- `modules/customer/components/` (5 modal components)
  - `ProfileEditModal.vue`
  - `BookingsSidebar.vue`
  - `FavoritesModal.vue`
  - `ReviewsModal.vue`
  - `SettingsModal.vue`
- `modules/customer/stores/customer.store.ts` (single store)
- `modules/customer/api/customer.service.ts`
- `modules/customer/types/customer.types.ts`
- `components/landing/FeaturedProviders.vue` (add favorite buttons)
- `core/i18n/locales/fa.json` (add ~50 keys)

**Backend** (`src/BoundedContexts/UserManagement/`):
- `Domain/Aggregates/FavoriteProvider/`
- `Application/Commands/UpdateCustomerProfile/`
- `Application/Commands/AddFavoriteProvider/`
- `Application/Commands/RemoveFavoriteProvider/`
- `Application/Commands/UpdateNotificationPreferences/`
- `Application/Queries/GetCustomerProfile/`
- `Application/Queries/GetUpcomingBookings/`
- `Application/Queries/GetBookingHistory/`
- `Application/Queries/GetFavoriteProviders/`
- `Application/Queries/GetCustomerReviews/`
- `Infrastructure/EventHandlers/BookingEventHandlers.cs`
- `Api/Controllers/V1/CustomersController.cs`

**Database**:
- Migration to extend users table
- Migration for 3 new tables
- Indexes for query optimization

### User-Facing Changes
- **Customers** see user menu dropdown in top-right (when authenticated)
- **Customers** can view/edit basic profile info via modal
- **Customers** can see upcoming and past bookings via sidebar
- **Customers** can save favorite providers via heart buttons
- **Customers** can view and edit their reviews
- **Customers** can control notification preferences
- **Support Team** handles account deletion and phone number changes manually

### Performance Considerations
- Profile data cached in Redis (5 min TTL)
- Booking history paginated (20 per page)
- Favorites limited to 100 per customer
- Lazy loading for modals (load content on open)
- Small bundle size increase (<30KB gzipped)

### Security Considerations
- Phone number not editable (requires verification flow if needed)
- Email validation before save
- Rate limiting on API endpoints (10 req/min per customer)
- CSRF protection for mutations
- Input sanitization for name and email

### What We're NOT Doing (Out of Scope)
- ❌ Profile avatar/image upload
- ❌ Booking history export (PDF/Excel)
- ❌ Payment methods management
- ❌ Loyalty points system
- ❌ Advanced privacy settings
- ❌ Two-factor authentication (2FA)
- ❌ Active session management
- ❌ Self-service account deletion
- ❌ Self-service data export
- ❌ Customer statistics dashboard
- ❌ Advanced booking filters

These can be added in future phases if customers request them.

### Breaking Changes
- **None** - This is purely additive functionality

## Timeline

**Estimated Duration**: 3 weeks (vs 6 weeks in original proposal)

**Phase 1 (Week 1)**: Backend
- Database schema
- Aggregates and repositories
- Command/query handlers
- API endpoints
- Integration tests

**Phase 2 (Week 2)**: Frontend Core
- User menu dropdown
- 5 modal/sidebar components
- Pinia store
- API service
- Persian translations

**Phase 3 (Week 3)**: Integration & Polish
- Favorite buttons on provider cards
- Mobile bottom navigation
- Bottom sheets for mobile
- Connect to booking flow
- Testing and bug fixes
- Deployment

## Success Metrics

**Technical**:
- Modal load time: <500ms
- Booking history query: <300ms
- Zero N+1 query problems

**User Experience**:
- >50% of customers save at least 1 favorite
- >60% of customers view bookings within first week
- >40% of customers update profile info

**Business**:
- 15% fewer support tickets for booking questions
- +10% rebooking rate via favorites
- +5% customer retention
