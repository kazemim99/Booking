# Design: Replace Dashboard with Figma RTL Design

## Overview
This change replaces the existing provider dashboard with a modern RTL Persian design exported from Figma. The new dashboard introduces tabbed navigation, enhanced statistics, Jalali calendar integration, and advanced gallery management while preserving all existing backend data flows.

## Architecture Decisions

### 1. Dashboard Layout Structure

**Decision**: Use a sidebar layout with tabbed content area instead of the current card-based dashboard

**Rationale**:
- **Better organization**: Tabbed interface groups related functionality (Profile → Personal/Business/Location/Staff/Hours/Gallery)
- **Scalability**: Easier to add new features without cluttering the main dashboard
- **Consistency**: Matches modern web app patterns and the Figma design system
- **Mobile-friendly**: Sidebar collapses to hamburger menu on mobile devices

**Implementation**:
```
DashboardLayout (sidebar + header)
├── Header (logo, user avatar, mobile menu toggle)
├── Sidebar (navigation menu - desktop)
├── Mobile Menu (slide-out navigation - mobile)
└── Content Area
    ├── Bookings Tab (BookingStats + BookingList)
    └── Profile Tab
        ├── Personal Info
        ├── Business Info
        ├── Location
        ├── Staff Management
        ├── Business Hours
        └── Gallery
```

### 2. Jalali Calendar Integration

**Decision**: Use `@persian-tools/persian-tools` for Jalali date conversion with a custom Vue wrapper

**Alternatives Considered**:
1. `vue-persian-datetime-picker` - Full component, but less flexible
2. `moment-jalaali` - Deprecated, maintenance concerns
3. Custom implementation - Too much effort

**Chosen Approach**: `@persian-tools/persian-tools`
- Actively maintained
- Lightweight (only conversion utilities needed)
- Flexible (can build custom UI components)
- Good TypeScript support

**Trade-offs**:
- ✅ Full control over UI/UX
- ✅ Smaller bundle size
- ❌ Need to build calendar UI ourselves (mitigated by Figma design providing UI)

### 3. Chart Library Selection

**Decision**: Use `vue-chartjs` (Vue 3 wrapper for Chart.js)

**Alternatives Considered**:
1. `echarts-for-vue` - More features, larger bundle
2. `apexcharts-vue` - Good, but less community support
3. Native Recharts port - Recharts is React-specific

**Chosen Approach**: `vue-chartjs` with `chart.js`
- Excellent Vue 3 support
- Widely used, well-documented
- Lightweight for the features needed (pie + line charts)
- Good RTL support with proper configuration

**Charts Needed**:
- **Pie Chart**: Booking completion ratio (completed, cancelled, pending)
- **Line Chart**: Revenue trend over time (daily/weekly/monthly)

### 4. Multiple Breaks Per Day

**Decision**: Create separate `WorkingHourBreaks` table with one-to-many relationship to `WorkingHours`

**Rationale**:
- **Flexibility**: Providers can have multiple breaks (lunch, prayer time, etc.)
- **Data integrity**: Validates breaks are within open/close hours
- **Scalability**: Easier to extend with break types in future

**Schema**:
```sql
WorkingHours
- Id (PK)
- ProviderId
- DayOfWeek
- IsOpen
- OpenTime
- CloseTime

WorkingHourBreaks
- Id (PK)
- WorkingHoursId (FK)
- StartTime
- EndTime
```

**Alternative Rejected**: JSON column for breaks
- ❌ Harder to query
- ❌ No database-level validation
- ❌ Poor type safety

### 5. Custom Day Scheduling

**Decision**: Create `CustomDaySchedules` table for special days (holidays, events)

**Rationale**:
- **Separation of concerns**: Weekly schedule vs. special days
- **Date-specific**: Each custom day has a specific date (Jalali or Gregorian)
- **Override behavior**: Custom days take precedence over weekly schedule

**Schema**:
```sql
CustomDaySchedules
- Id (PK)
- ProviderId (FK)
- Date (DateTime)
- IsOpen (bool)
- OpenTime (nullable)
- CloseTime (nullable)
- Breaks (JSON or separate table)
```

### 6. Gallery Primary Image

**Decision**: Add `IsPrimary` boolean flag to `GalleryImages` with unique constraint per provider

**Rationale**:
- **Simple**: Single boolean field
- **Enforceable**: Database constraint ensures only one primary image
- **Queryable**: Easy to filter/sort by primary image

**Business Logic**:
- When setting an image as primary, unset any existing primary image for the same provider
- Use database transaction to ensure atomicity

### 7. State Management Approach

**Decision**: Use Vue 3 Composition API with dedicated composables for each feature area

**Composables**:
- `useBusinessHours()` - Weekly hours + custom days state
- `useGallery()` - Gallery images + CRUD operations
- `useBookingStats()` - Statistics + chart data
- `useProfile()` - Profile tabs state

**Rationale**:
- **Reusability**: Composables can be used in multiple components
- **Testability**: Easier to unit test logic separately from UI
- **Maintainability**: Clear separation of concerns

### 8. Backend API Structure

**Decision**: Follow existing CQRS pattern with Commands and Queries

**New Commands**:
- `SaveWorkingHoursCommand` (extended with breaks)
- `SaveCustomDayScheduleCommand`
- `SetPrimaryGalleryImageCommand`
- `UpdateGalleryImageCommand`
- `DeleteGalleryImageCommand`

**New Queries**:
- `GetWorkingHoursQuery` (includes breaks)
- `GetCustomDaySchedulesQuery`
- `GetBookingStatisticsQuery`
- `GetRevenueChartDataQuery`

**Rationale**:
- Consistency with existing codebase
- Clear separation of read/write operations
- Supports caching for queries

### 9. Data Migration Strategy

**Decision**: Additive migrations only, no destructive changes

**Approach**:
1. Add new tables (`WorkingHourBreaks`, `CustomDaySchedules`)
2. Add new column (`GalleryImages.IsPrimary`)
3. Seed default values (existing hours have empty breaks array)
4. Update API to support both old and new formats (backward compatible)

**Rollback Plan**:
- If issues occur, can revert to old dashboard UI
- Backend changes are additive, won't break existing functionality
- Database migrations can be rolled back if needed

### 10. Performance Considerations

**Decision**: Implement caching for statistics queries

**Rationale**:
- Statistics don't change frequently (daily aggregation is sufficient)
- Chart data can be expensive to compute (multiple date ranges)
- Improves dashboard load time

**Caching Strategy**:
- Use in-memory cache (IMemoryCache) for short-lived data (5-10 minutes)
- Use Redis cache for longer-lived data (1 hour+) if available
- Cache key includes provider ID and date range

### 11. Mobile Responsiveness

**Decision**: Mobile-first approach with progressive enhancement

**Breakpoints**:
- **Mobile**: < 768px (single column, hamburger menu)
- **Tablet**: 768px - 1024px (2 columns, sidebar visible)
- **Desktop**: > 1024px (full layout with sidebar)

**Mobile Optimizations**:
- Sidebar becomes slide-out drawer
- Tabs become vertical accordion on very small screens
- Charts use responsive containers
- Touch-friendly controls (larger buttons, spacing)

### 12. RTL Support

**Decision**: Use CSS logical properties throughout

**Implementation**:
- `margin-inline-start` instead of `margin-left`
- `padding-inline-end` instead of `padding-right`
- `flex-direction: row-reverse` for RTL layouts
- Persian number formatting for all numeric displays

**Chart RTL**:
- Configure Chart.js with RTL option
- Reverse axis labels and tooltips
- Use Persian number formatting in tooltips

## Component Hierarchy

```
ProviderDashboardView.vue
└── DashboardLayout.vue
    ├── DashboardHeader.vue
    ├── DashboardSidebar.vue
    ├── MobileMenu.vue
    └── (Tab Content)
        ├── BookingsTab
        │   ├── BookingStatsCard.vue
        │   │   ├── PieChart.vue
        │   │   └── LineChart.vue
        │   └── BookingListCard.vue
        └── ProfileTab
            └── ProfileManager.vue
                ├── PersonalInfoTab.vue
                ├── BusinessInfoTab.vue
                ├── LocationTab.vue
                ├── StaffManagementTab.vue
                ├── BusinessHoursTab.vue
                │   ├── BusinessHoursManager.vue
                │   ├── BusinessHoursEditor.vue
                │   ├── JalaliDatePicker.vue
                │   └── CustomDayModal.vue
                └── GalleryTab.vue
                    └── GalleryManager.vue
```

## Testing Strategy

### Unit Tests
- Composables logic (state management, API calls)
- Date conversion utilities (Jalali ↔ Gregorian)
- Chart data transformation
- Form validation logic

### Integration Tests
- Dashboard tab navigation
- Business hours save/load with breaks
- Custom day scheduling
- Gallery CRUD operations
- Statistics queries with real data

### E2E Tests
- Complete user flow: Login → Dashboard → Edit Hours → Save
- Mobile navigation flow
- Chart interaction (hover, date range selection)
- Gallery upload and primary image selection

## Security Considerations

1. **Authorization**: Ensure all API endpoints check provider ownership
2. **Input Validation**: Validate all date inputs (prevent invalid Jalali dates)
3. **File Upload**: Validate gallery image types and sizes (prevent malicious uploads)
4. **SQL Injection**: Use parameterized queries for all database operations
5. **XSS Prevention**: Sanitize all user inputs displayed in dashboard

## Accessibility

1. **Keyboard Navigation**: All tabs and controls accessible via keyboard
2. **Screen Reader Support**: Proper ARIA labels for charts and interactive elements
3. **Color Contrast**: Ensure WCAG AA compliance for all text
4. **Focus Indicators**: Clear visual focus states
5. **RTL Screen Readers**: Test with Persian screen readers

## Monitoring & Observability

1. **Dashboard Load Time**: Track time to render full dashboard
2. **Chart Rendering Performance**: Monitor chart.js rendering times
3. **API Response Times**: Track statistics query performance
4. **Error Tracking**: Log and alert on dashboard errors
5. **User Engagement**: Track tab usage and feature adoption
