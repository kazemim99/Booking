# Replace Dashboard with Figma RTL Design

## Why
The current provider dashboard uses a basic UI design that doesn't match the modern RTL Persian user experience established in the new onboarding flow. A comprehensive Figma-generated dashboard design has been created that provides:
- Modern RTL Persian interface with consistent Booksy-style design system
- Tabbed navigation for better organization (Bookings, Profile with sub-tabs for Personal/Business/Location/Staff/Hours/Gallery)
- Enhanced booking statistics with charts (pie charts for completion ratios, line charts for revenue trends)
- Jalali (Persian) calendar integration for business hours management
- Advanced gallery management with CRUD operations and primary image selection
- Support for multiple daily breaks in business hours
- Improved mobile responsiveness and accessibility

The current dashboard has limited functionality and uses an outdated design pattern that creates visual inconsistency with the newly redesigned onboarding flow.

## What Changes
- **Remove legacy dashboard**: Delete existing dashboard components ([WelcomeCard.vue](booksy-frontend/src/modules/provider/components/dashboard/WelcomeCard.vue), [QuickStatsCard.vue](booksy-frontend/src/modules/provider/components/dashboard/QuickStatsCard.vue), [RecentBookingsCard.vue](booksy-frontend/src/modules/provider/components/dashboard/RecentBookingsCard.vue), [QuickActionsCard.vue](booksy-frontend/src/modules/provider/components/dashboard/QuickActionsCard.vue))
- **Convert Figma React components to Vue 3**: Translate all dashboard components from React to Vue 3 Composition API:
  - DashboardLayout.tsx → DashboardLayout.vue (sidebar navigation, header, mobile menu)
  - BookingStats.tsx → BookingStatsCard.vue (statistics with charts)
  - BookingList.tsx → BookingListCard.vue (recent bookings table)
  - Profile.tsx → ProfileManager.vue (tabbed profile management)
  - BusinessHours.tsx → BusinessHoursManager.vue (weekly schedule editor)
  - BusinessHoursEditor.tsx → BusinessHoursEditor.vue (time picker component)
  - CustomDayModal.tsx → CustomDayModal.vue (special day configuration)
  - GalleryManager.tsx → GalleryManager.vue (image upload/management)
- **Add Jalali calendar support**: Integrate Persian calendar library for date selection in business hours
- **Extend backend APIs**: Add support for new dashboard features:
  - Multiple breaks per day in business hours
  - Custom day scheduling with Jalali dates
  - Gallery image primary flag
  - Enhanced booking statistics queries
- **Update routing**: Modify dashboard routes to support tabbed navigation structure
- **Preserve existing data flows**: Maintain all existing backend integrations for bookings, profile, staff, hours, and gallery

## Impact
- **Affected specs**:
  - `provider-dashboard` (ADDED - new spec for dashboard UI and features)
  - `working-hours-management` (MODIFIED - add Jalali calendar, multiple breaks support)
  - Note: `gallery-management` and `booking-statistics` specs are deferred to future phases

- **Affected code**:
  - **Frontend**:
    - **Dashboard Module** (`booksy-frontend/src/modules/provider/`):
      - Replace `views/dashboard/ProviderDashboardView.vue` with new dashboard layout
      - Delete `components/dashboard/WelcomeCard.vue`
      - Delete `components/dashboard/QuickStatsCard.vue`
      - Delete `components/dashboard/RecentBookingsCard.vue`
      - Delete `components/dashboard/QuickActionsCard.vue`
      - Add `components/dashboard/DashboardLayout.vue`
      - Add `components/dashboard/BookingStatsCard.vue`
      - Add `components/dashboard/BookingListCard.vue`
      - Add `components/dashboard/ProfileManager.vue`
      - Add `components/dashboard/BusinessHoursManager.vue`
      - Add `components/dashboard/BusinessHoursEditor.vue`
      - Add `components/dashboard/CustomDayModal.vue`
      - Add `components/dashboard/GalleryManager.vue`
    - **Charts** (if not exists):
      - Add chart library integration (Chart.js or Recharts equivalent for Vue)
      - Add `components/charts/PieChart.vue`
      - Add `components/charts/LineChart.vue`
    - **Calendar**:
      - Add Jalali calendar library (@persian-tools/persian-tools or vue-persian-datetime-picker)
      - Add calendar utilities for date conversion
    - **Router** (`booksy-frontend/src/core/router/`):
      - Update dashboard route structure for tab-based navigation

  - **Backend**:
    - **Working Hours** (`Booksy.ServiceCatalog.Application/`):
      - Extend `WorkingHours` entity to support multiple breaks
      - Add `CustomDaySchedule` entity for special days
      - Update `SaveWorkingHoursCommand` to handle multiple breaks
      - Add `SaveCustomDayCommand` for special day scheduling
    - **Gallery** (`Booksy.ServiceCatalog.Application/`):
      - Add `SetPrimaryGalleryImageCommand`
      - Extend `GalleryImage` entity with `IsPrimary` flag
    - **Booking Statistics** (`Booksy.Booking.Application/` or ServiceCatalog):
      - Add `GetBookingStatisticsQuery` (completion ratio, revenue trend)
      - Add `GetRevenueChartDataQuery` (time-series data)

- **Database**: Schema changes required:
  - `WorkingHours` table: Add support for multiple breaks (new `WorkingHourBreaks` table)
  - `CustomDaySchedule` table: Store special day configurations
  - `GalleryImages` table: Add `IsPrimary` boolean column

- **Breaking changes**: None for existing features; new features are additive

## Dependencies
- Jalali calendar library (e.g., `@persian-tools/persian-tools` or `vue-persian-datetime-picker`)
- Chart library for Vue 3 (e.g., `vue-chartjs` with `chart.js`, or Vue wrapper for Recharts)
- Ensure backend API supports enhanced data structures

## Migration Path
1. Install required dependencies (Jalali calendar, charts)
2. Convert React components to Vue 3 while preserving functionality
3. Update backend entities and commands for new features
4. Run database migrations for schema changes
5. Update dashboard routing and navigation
6. Test all existing dashboard features still work
7. Test new features (Jalali calendar, multiple breaks, gallery CRUD)
8. Remove old dashboard components after verification
