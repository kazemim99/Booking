# Customer Profile Page Proposal

## Why

The Booksy platform currently lacks a comprehensive customer profile management interface. While the provider experience is fully developed with dashboards, business management, and detailed settings, customers have minimal ability to manage their account, view booking history, track favorites, or configure preferences. This creates an asymmetric user experience where service providers have sophisticated tools but customers are left with basic functionality.

This gap reduces customer engagement, limits personalization opportunities, and increases support burden as users cannot self-serve common account management tasks. A dedicated customer profile page is essential for:

1. **User Retention**: Customers need a sense of ownership and investment in the platform through personalized dashboards and profile management
2. **Self-Service**: Reducing support tickets by enabling customers to manage accounts, bookings, and preferences independently
3. **Personalization**: Storing favorites, preferences, and history to deliver tailored recommendations
4. **Trust Building**: Displaying verification status, review history, and account statistics to build confidence
5. **Feature Parity**: Matching the sophistication of the provider experience with equally polished customer tools

## What Changes

### New Customer Profile Page Features
- **Profile Header**: Avatar upload, personal information display, verification badges, account age
- **Tabbed Navigation**: Organized sections for Bookings, Favorites, History, Payments, Reviews, Settings
- **Upcoming Appointments**: Dashboard widget showing next 3 bookings with countdown timers and quick actions
- **Booking History**: Filterable, sortable table of all past bookings with export capability
- **Favorite Providers**: Grid view of saved providers with quick rebooking and sorting options
- **Payment Methods**: Secure management of saved payment cards and wallets
- **Loyalty & Rewards**: Points tracking, tier progress, rewards catalog integration
- **Review Management**: View, edit, and track all submitted reviews
- **Settings Panel**: Notification preferences, privacy controls, security options, language/region
- **Quick Actions Sidebar**: Fast access to new booking, messages, loyalty points

### Frontend Components (Vue 3)
- New `/customer/profile` route with nested child routes
- `ProfilePage.vue` - Main container with tab navigation
- `ProfileHeader.vue` - Avatar, name, verification badges
- `UpcomingBookingsWidget.vue` - Dashboard widget with countdown
- `BookingsTab.vue` - Full booking history with filters
- `FavoritesTab.vue` - Favorite providers grid/list
- `PaymentMethodsCard.vue` - Payment management
- `LoyaltyCard.vue` - Points and rewards tracking
- `ReviewsTab.vue` - Review history and management
- `SettingsPanel.vue` - Preferences and account settings

### Backend Additions (C# / .NET)
- **New Bounded Context**: `CustomerManagement` (or extend UserManagement)
- **Customer Profile Aggregate**: Personal info, preferences, settings
- **Favorite Providers Aggregate**: Saved providers with metadata
- **Customer Statistics Service**: Booking counts, spending analytics, loyalty points
- **Query Handlers**: GetCustomerProfile, GetUpcomingBookings, GetBookingHistory, GetFavoriteProviders
- **Command Handlers**: UpdateCustomerProfile, AddFavoriteProvider, RemoveFavoriteProvider, UpdatePreferences
- **API Controllers**: CustomersController (v1) with profile, favorites, stats endpoints

### Database Schema
- `customer_profiles` table (extends users)
- `favorite_providers` junction table
- `customer_preferences` table (notifications, privacy, settings)
- `customer_statistics` table (cached aggregates)
- `payment_methods` table (tokenized payment data)
- `loyalty_points` table (point transactions)

### Integration Points
- **Authentication**: Extend existing phone-based auth to include customer profile initialization
- **Booking System**: Link bookings to customer profile for history display
- **Provider System**: Enable favoriting providers, track last visit dates
- **Payment System**: Integrate saved payment methods with booking checkout
- **Notification System**: Respect customer notification preferences
- **Review System**: Display customer's submitted reviews in profile

### Persian RTL Support
- All new components designed RTL-first
- Jalali calendar for date displays
- Persian number formatting (۰۱۲۳...)
- Persian translations for all UI text
- Culturally appropriate date/time/currency formatting

### Mobile Responsiveness
- Mobile-first design with responsive breakpoints
- Bottom navigation for mobile tabs
- Swipe gestures for booking actions
- Touch-optimized interactive elements (44px minimum)
- Progressive Web App features for offline access

## Impact

### Affected Specs
- **NEW: customer-profile** - Core profile management specification
- **MODIFIED: authentication** - Add profile initialization after first login
- **MODIFIED: booking-management** (to be created) - Add customer booking history queries
- **NEW: customer-favorites** - Favorite providers management
- **NEW: customer-loyalty** - Points and rewards tracking

### Affected Code

**Frontend** (`booksy-frontend/src/`):
- `modules/customer/` - New module for customer-specific features
  - `views/ProfilePage.vue`
  - `views/BookingsView.vue`
  - `views/FavoritesView.vue`
  - `components/` (10+ new components)
  - `stores/customer.store.ts`
  - `api/customer.service.ts`
  - `types/customer.types.ts`
- `core/router/routes/customer.routes.ts` - Add profile routes
- `modules/booking/` - Extend with history queries
- `core/i18n/locales/fa.json` - Add 100+ Persian translations

**Backend** (`src/BoundedContexts/`):
- Option 1: New `CustomerManagement/` bounded context
- Option 2: Extend `UserManagement/` with customer features
- `ServiceCatalog/` - Add favorite providers support
  - `Aggregates/CustomerProfileAggregate/`
  - `Commands/CustomerProfile/UpdateCustomerProfile/`
  - `Queries/CustomerProfile/GetCustomerProfile/`
  - `Queries/Booking/GetCustomerBookingHistory/`
  - `Api/Controllers/V1/CustomersController.cs`
- `Infrastructure/Persistence/` - New configurations and repositories

**Database** (`Migrations/`):
- New migration for customer profile tables
- Indexes for booking history queries
- Foreign keys for favorite providers

### User-Facing Changes
- **Customers** see new "پروفایل من" (My Profile) navigation item
- **Customers** can now manage personal info, view bookings, save favorites
- **Customers** receive personalized recommendations based on preferences
- **Support Team** sees reduced ticket volume for account management questions

### Performance Considerations
- Profile data cached in Redis (5 min TTL)
- Booking history paginated (20 per page)
- Lazy loading for tab content
- Image optimization for avatars (WebP, CDN)
- Database indexes on frequently queried fields

### Security Considerations
- Payment data tokenized (PCI DSS compliance)
- Personal data encrypted at rest
- GDPR-compliant data export/deletion
- Rate limiting on profile updates
- CSRF protection for all mutations

### Breaking Changes
- **None** - This is purely additive functionality
