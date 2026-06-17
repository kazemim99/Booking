# Customer Mobile App Implementation - OpenSpec Proposal

## Status: 🚧 IN PROGRESS

This change proposes implementing a full-featured Flutter mobile application for Booksy customers (Persian/Iranian market).

## Quick Links

- **Proposal**: [proposal.md](./proposal.md) - Why, what, and impact
- **Design**: [design.md](./design.md) - Architecture and technical approach
- **Tasks**: [tasks.md](./tasks.md) - Implementation work items *(coming soon)*

## Specifications

### ✅ Completed Specs

1. **[customer-mobile-home](./specs/customer-mobile-home/spec.md)**
   - Home screen layout with bottom navigation
   - Popular categories display
   - Upcoming bookings widget
   - Promotions banner
   - Top providers recommendations
   - Performance, caching, and error handling
   - **15 Requirements | 30+ Scenarios**

2. **[customer-mobile-search](./specs/customer-mobile-search/spec.md)**
   - Real-time search with debouncing
   - Location-based filtering (GPS + manual)
   - Category and advanced filters
   - Provider result cards with favorites
   - Pagination and infinite scroll
   - Recent searches and sort options
   - **13 Requirements | 40+ Scenarios**

3. **[customer-mobile-booking](./specs/customer-mobile-booking/spec.md)**
   - Multi-step booking wizard (4 steps)
   - Service selection (single/multiple)
   - Staff selection (optional)
   - Date/time picker (Jalali calendar, Persian numbers)
   - Review and confirmation
   - Booking confirmation page
   - Wizard navigation and state persistence
   - **11 Requirements | 35+ Scenarios**

### 🏗️ Remaining Specs (To Be Created)

4. **customer-mobile-bookings-management**
   - View upcoming/past bookings
   - Booking detail page
   - Cancel and reschedule functionality
   - Calendar integration
   - Estimated effort: 2-3 hours

5. **customer-mobile-profile**
   - User profile editing
   - Favorites management
   - Review submission and management
   - Notification preferences
   - Settings and logout
   - Estimated effort: 2-3 hours

## Progress Summary

### What's Been Done ✅

- [x] **Proposal document** - Comprehensive "why" and "what changes"
- [x] **Design document** - Complete architecture guide (Clean Architecture + BLoC)
- [x] **3 core specs** - Home, Search, Booking (80+ scenarios total)
- [x] **Directory structure** - All spec folders created

### What's Next 📋

- [ ] **Complete remaining 2 specs** - Bookings management + Profile
- [ ] **Create tasks.md** - Break down into ordered work items
- [ ] **Validate proposal** - Run `openspec validate implement-customer-mobile-app --strict`
- [ ] **Address validation issues** - Fix any errors or warnings
- [ ] **Submit for review** - Ready for team approval

## Key Highlights

### Persian-First Design

All specs prioritize Persian language and cultural expectations:
- ✅ RTL layout throughout
- ✅ Jalali calendar (شمسی) for dates
- ✅ Persian numbers (۰۱۲۳۴۵۶۷۸۹)
- ✅ Persian currency format (۱۲۳٬۴۵۶ تومان)
- ✅ Vazir font (Persian typography)

### Professional & Gender-Neutral Design

A minimal design system for all users:
- ✅ Single primary color: Dark Blue (#1A365D) - professional, trustworthy
- ✅ Neutral gray palette - no decorative colors
- ✅ Flat design - no shadows, gradients, or decorations
- ✅ Gender-neutral throughout (suitable for all service types)
- ✅ Clarity and simplicity over visual flair

### Mobile-Optimized UX

Every scenario considers mobile constraints:
- ✅ Touch targets ≥48dp
- ✅ Bottom navigation (thumb-friendly)
- ✅ Infinite scroll (no pagination buttons)
- ✅ Skeleton loaders (perceived performance)
- ✅ Offline support and caching

### Production-Ready

Specs include real-world concerns:
- ✅ Error handling (network failures, API errors, validation)
- ✅ Performance SLAs (2s page load, 300ms transitions)
- ✅ Accessibility (screen readers, keyboard navigation)
- ✅ Analytics tracking (user behavior insights)
- ✅ Security (token refresh, encrypted storage)

## Implementation Estimate

Based on spec scope:

| Phase | Features | Duration | Effort |
|-------|----------|----------|--------|
| Phase 1 | Foundation, design system | 2 weeks | ~80 hours |
| Phase 2 | Search & home screen | 3 weeks | ~120 hours |
| Phase 3 | Booking wizard | 3 weeks | ~120 hours |
| Phase 4 | Bookings management | 2 weeks | ~80 hours |
| Phase 5 | Profile & settings | 2 weeks | ~80 hours |
| Phase 6 | Polish, testing, QA | 2 weeks | ~80 hours |
| **Total** | **Full customer app** | **14 weeks** | **~560 hours** |

*(Assumes 1 full-time Flutter developer, 3.5 months to production)*

## Technical Approach

- **Framework**: Flutter 3.0+ (Dart 3.0+)
- **Architecture**: Clean Architecture (3 layers: Presentation, Domain, Data)
- **State Management**: BLoC pattern (flutter_bloc 8.1.3)
- **Networking**: Dio 5.4.0 + Retrofit 4.0.3
- **DI**: GetIt 7.6.7 + Injectable 2.3.2
- **Navigation**: GoRouter (declarative routing)
- **Storage**: Flutter Secure Storage (encrypted)
- **Error Handling**: Either/Result pattern (dartz 0.10.1)

## Backend API Dependencies

Existing APIs required (all expected to exist):

```
Authentication:
- POST /api/v1/Auth/send-verification-code
- POST /api/v1/Auth/customer/complete-authentication
- POST /api/v1/Auth/refresh
- POST /api/v1/Auth/logout

Search & Discovery:
- GET /api/v1/Categories
- GET /api/v1/Categories/popular
- POST /api/v1/Providers/search
- GET /api/v1/Providers/{id}
- GET /api/v1/Providers/{id}/services
- GET /api/v1/Providers/{id}/staff

Booking:
- GET /api/v1/Availability/slots
- POST /api/v1/Bookings
- GET /api/v1/Bookings/my-bookings
- GET /api/v1/Bookings/{id}
- POST /api/v1/Bookings/{id}/cancel
- POST /api/v1/Bookings/{id}/reschedule

Customer:
- GET /api/v1/Customers/profile
- PUT /api/v1/Customers/profile
- POST /api/v1/Customers/favorites/{providerId}
- DELETE /api/v1/Customers/favorites/{providerId}
- POST /api/v1/Reviews
- GET /api/v1/Reviews/my-reviews
```

## Questions for Stakeholders

Before finalizing, we need decisions on:

1. **Push Notifications**: FCM now or later phase?
2. **Payment Gateway**: Which provider for Iran? (Zarinpal, Mellat, etc.)
3. **Maps Provider**: Neshan Maps (Iranian) or Google Maps?
4. **Analytics**: Firebase Analytics, Sentry, or custom?
5. **App Store Strategy**: iOS App Store + Google Play, or APK only?
6. **Offline Booking**: Should booking creation queue when offline?

## Validation Checklist

Before marking as ready:

- [ ] All 5 specs created with scenarios
- [ ] Each requirement has ≥1 scenario
- [ ] Scenarios follow GIVEN-WHEN-THEN format
- [ ] Cross-references between specs added
- [ ] Tasks.md created with dependencies
- [ ] `openspec validate` passes with no errors
- [ ] Design decisions documented
- [ ] Open questions identified
- [ ] Success criteria defined

## How to Continue

To complete this proposal:

```bash
# 1. Create remaining specs
cd openspec/changes/implement-customer-mobile-app/specs
# - customer-mobile-bookings-management/spec.md
# - customer-mobile-profile/spec.md

# 2. Create implementation tasks
# Edit: tasks.md

# 3. Validate proposal
cd /c/Repos/Booking
openspec validate implement-customer-mobile-app --strict

# 4. Fix any validation errors
# Review output and address issues

# 5. Mark as ready for review
```

## Contributors

- **Proposal Author**: Claude Code (AI Assistant)
- **Design Patterns**: Based on existing `booksy-customer-app` Flutter scaffold
- **Spec Format**: Following existing OpenSpec conventions from `provider-registration`, `customer-profile`

---

**Last Updated**: January 2, 2026
**Status**: 60% complete (3 of 5 specs done)
