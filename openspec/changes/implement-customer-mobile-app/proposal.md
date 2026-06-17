# Implement Customer Mobile App

## Why

The current Booksy platform lacks a dedicated mobile application for customers, limiting accessibility and user experience for the primary target audience—Persian (Iranian) customers who primarily access services via mobile devices.

**Current State:**
1. **No mobile customer interface** - Only web-based Vue.js frontend exists (primarily for admin/provider)
2. **Limited customer engagement** - No native mobile experience for service discovery and booking
3. **Incomplete customer journey** - Auth is implemented (Flutter app scaffold exists) but booking flow is missing
4. **Market expectations** - Competitors offer full-featured mobile apps with seamless booking

**Target Users:**
- Persian-speaking customers in Iran
- Mobile-first users (iOS & Android)
- Service seekers for beauty, wellness, and personal care
- Age range: 18-55, primarily urban areas

**Business Goals:**
- Provide fast, intuitive service discovery
- Enable frictionless booking experience
- Build customer trust through Persian-first UX
- Support cultural familiarity (Jalali calendar, RTL, Persian numbers)
- Increase booking conversion rates

## What Changes

### Core Changes

1. **Complete Customer Mobile App (Flutter)**
   - Expand existing auth-only scaffold to full-featured booking app
   - Implement search, booking, profile, and review flows
   - Follow established Clean Architecture + BLoC pattern
   - Support both iOS and Android platforms

2. **Customer Journey Implementation**
   - **Home/Landing**: Personalized dashboard with quick actions
   - **Search & Discovery**: Category-based and location-based provider search
   - **Provider Details**: Gallery, services, reviews, staff selection
   - **Booking Flow**: Multi-step wizard (service → staff → date/time → confirm)
   - **Booking Management**: View upcoming/past bookings, cancel, reschedule
   - **Profile**: Manage favorites, reviews, preferences, notifications

3. **Backend API Enhancements** (if needed)
   - Ensure all required endpoints exist and support mobile use cases
   - Optimize for mobile: pagination, response sizes, caching headers
   - Support Persian/Jalali date formats in responses

4. **Design System**
   - Persian-first UI (Vazir font, RTL layout, Jalali dates)
   - Professional, gender-neutral design (dark blue primary color)
   - Minimal, flat design with neutral tones (no decorative colors)
   - Custom design system optimized for clarity and trust
   - Responsive design (375x812 base, ScreenUtil scaling)

### Breaking Changes

**None** - This is a new greenfield mobile app that:
- Consumes existing backend APIs (no breaking API changes required)
- Operates independently from web frontend
- Augments platform with mobile capabilities

### Non-Breaking Enhancements

- **Backend**: May add mobile-optimized endpoints (e.g., `/api/v1/mobile/...`)
- **Backend**: May add push notification infrastructure
- **Backend**: May add app version checking endpoints

## Impact

### Affected Specs

**New Specs (to be created):**
- `customer-mobile-home` - Home screen, quick actions, recommendations
- `customer-mobile-search` - Provider search and filtering
- `customer-mobile-booking` - Multi-step booking flow
- `customer-mobile-bookings-management` - View/manage customer bookings
- `customer-mobile-profile` - User profile, favorites, reviews, settings

**Existing Specs (context/reference):**
- `authentication` - Already implemented in Flutter app (login, OTP, session)
- `customer-profile` - Web-based, informs mobile profile requirements
- `provider-management` - Defines provider entities consumed by mobile search
- `service-management` - Defines services displayed in mobile app

### Affected Code

**Flutter App** (`booksy-customer-app/`):
- **New Features:**
  - `lib/features/home/` - Home screen module (currently scaffolded)
  - `lib/features/search/` - Provider search module (currently scaffolded)
  - `lib/features/booking/` - Booking flow module (currently scaffolded)
  - `lib/features/profile/` - Customer profile module (currently scaffolded)

- **Existing (Complete):**
  - `lib/features/auth/` - Auth module ✅ (login, OTP, session management)
  - `lib/core/` - Core infrastructure ✅ (DI, API client, interceptors, storage)

- **Shared:**
  - `lib/config/routes/` - Navigation routes (GoRouter setup scaffolded)
  - `lib/config/theme/` - Theme configuration (scaffolded)
  - `assets/` - Images, icons, fonts (Vazir font installed)

**Backend** (minimal changes):
- May add: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/Mobile/` - Optional mobile-optimized endpoints
- May add: Push notification service integration
- Mostly: Consume existing APIs as-is

**Infrastructure:**
- May add: Firebase Cloud Messaging (FCM) for push notifications
- May add: App analytics (Firebase Analytics or similar)

### User Impact

- **Customers**: Gain native mobile app experience
  - Faster access to services (no browser needed)
  - Native features: push notifications, maps, calendar integration
  - Offline capability for viewing past bookings
  - Smoother UX with native animations

- **Providers**: Increased booking volume from mobile users
  - More customer engagement
  - Better reach to mobile-first demographic

- **Platform**: Market competitiveness
  - Matches competitor offerings
  - Opens mobile-specific monetization (app store presence)
  - Better user retention and engagement metrics

### Timeline Estimate

**Phase 1: Foundation** (2 weeks)
- Set up complete Flutter project structure
- Implement navigation and routing
- Design system components (buttons, cards, forms)
- API integration layer refinement

**Phase 2: Search & Discovery** (3 weeks)
- Home screen with categories and recommendations
- Provider search with filters
- Provider detail page with gallery and services
- Category browsing

**Phase 3: Booking Flow** (3 weeks)
- Multi-step booking wizard
- Date/time availability selection
- Service and staff selection
- Booking confirmation and payment preparation

**Phase 4: Booking Management** (2 weeks)
- View upcoming/past bookings
- Cancel and reschedule functionality
- Booking details screen
- Calendar integration

**Phase 5: Profile & Settings** (2 weeks)
- User profile editing
- Favorites management
- Review submission and management
- Notification preferences

**Phase 6: Polish & Testing** (2 weeks)
- End-to-end testing (Cypress equivalent for Flutter)
- Performance optimization
- Accessibility improvements
- Bug fixes and refinements

**Total: ~14 weeks** (3.5 months to MVP)

## Open Questions

1. **Push Notifications**:
   - Should we integrate FCM now or in later phase?
   - What notification events: booking confirmed, reminder 24h before, provider message?

2. **Offline Support**:
   - How much data should be cached locally?
   - Should users view cached bookings when offline?
   - Should booking creation queue when offline?

3. **Payment Integration**:
   - Which payment gateway for Iran market? (Zarinpal, Mellat, etc.)
   - When is payment collected: at booking, after service, deposit only?
   - Support for in-app payment vs pay-at-location?

4. **Location Services**:
   - Use Neshan Maps API (Iranian) or Google Maps?
   - Require GPS permission on first launch or on-demand?
   - Show providers on map view or list only?

5. **App Store Presence**:
   - iOS App Store availability (if targeting Iran, may be limited)?
   - Google Play Store + direct APK download?
   - App versioning strategy and update mechanisms?

6. **Analytics & Monitoring**:
   - Firebase Analytics, Sentry, or custom solution?
   - What metrics to track: bookings, searches, user retention?

7. **Backend API Gaps**:
   - Are all required endpoints implemented? (check against API constants)
   - Do we need mobile-specific endpoints or generic ones sufficient?
   - Rate limiting for mobile apps?

8. **Accessibility**:
   - Target WCAG compliance level (AA recommended)?
   - Screen reader support priority (Persian TalkBack support)?

## Success Criteria

1. **Functional Completeness**:
   - ✅ User can search providers by category/location
   - ✅ User can view provider details, gallery, reviews
   - ✅ User can book service with specific staff member and time
   - ✅ User can view upcoming and past bookings
   - ✅ User can cancel/reschedule bookings
   - ✅ User can manage favorites and submit reviews
   - ✅ User can edit profile and notification preferences

2. **Performance**:
   - 📱 App launches in <3 seconds (cold start)
   - 📱 Search results load in <2 seconds
   - 📱 Booking flow completes in <30 seconds
   - 📱 No janky scrolling (60 FPS maintained)

3. **UX Quality**:
   - 🎨 Full Persian RTL support with Jalali dates
   - 🎨 Professional, gender-neutral design (dark blue primary, neutral tones)
   - 🎨 Clean and minimal interface (no decorative elements)
   - 🎨 Intuitive navigation (bottom nav + modals)
   - 🎨 Clear error messages and loading states

4. **Reliability**:
   - 🔒 Auth tokens auto-refresh on 401
   - 🔒 Graceful degradation when offline
   - 🔒 No crashes in production (99.9% crash-free rate)

5. **Adoption**:
   - 📊 10,000 downloads in first 3 months
   - 📊 40% booking conversion rate (search → booking)
   - 📊 4.0+ app store rating
