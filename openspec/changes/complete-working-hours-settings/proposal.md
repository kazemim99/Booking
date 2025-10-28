# Complete Working Hours Settings for Providers

## Why

Providers currently have only basic business hours configuration - simple open/close times for each day of the week. This is insufficient for real-world business operations which require:

1. **Holiday Management**: No way to mark specific dates as closed (holidays, special events)
2. **Exception Dates**: Cannot handle temporary schedule changes (vacation, maintenance)
3. **Break Times**: No support for lunch breaks or split shifts within a day
4. **Seasonal Hours**: Cannot configure different hours for different seasons/periods
5. **Visual Calendar**: Current list view makes it hard to see weekly patterns at a glance
6. **Recurring Patterns**: No way to set recurring closures (every other week, monthly)

Without these capabilities, providers must manually reject bookings or communicate schedule changes outside the system, creating poor customer experience and administrative burden.

## What Changes

### New Capabilities
- **Holiday Calendar**: Mark specific dates as closed with reasons (e.g., "Christmas Day", "Annual Maintenance")
- **Exception Dates**: Set temporary schedule overrides for specific dates or date ranges
- **Break Times**: Configure break periods within operating hours (lunch breaks, staff breaks)
- **Recurring Exceptions**: Set recurring closures (bi-weekly, monthly, custom patterns)
- **Visual Calendar View**: Week/month calendar view showing hours at a glance
- **Seasonal Hours**: Configure different hours for different time periods (summer/winter hours)

### Enhanced Capabilities
- **Business Hours View**: Add calendar visualization alongside list view
- **Quick Actions**: Copy schedules, apply templates, batch operations
- **Conflict Detection**: Warn when exceptions conflict with existing bookings
- **Customer Communication**: Auto-notify customers affected by schedule changes

### UX Improvements
- Toggle between list view and calendar view
- Drag-and-drop on calendar to set hours
- Visual indicators for holidays, exceptions, and breaks
- Bulk select dates for applying same schedule
- Preview customer-facing availability before saving

## Impact

### Affected Specs
- **provider-management** (MODIFIED): Enhance "Advanced Business Hours Management" requirement
- **working-hours-management** (NEW): Comprehensive working hours and exceptions specification

### Affected Code

#### Frontend (booksy-frontend/src/modules/provider/)
- `views/hours/BusinessHoursView.vue` - Add calendar view, holidays, exceptions
- `components/hours/HoursCalendarView.vue` - NEW: Calendar visualization
- `components/hours/HolidayManager.vue` - NEW: Holiday/exception management
- `components/hours/BreakTimeEditor.vue` - NEW: Break time configuration
- `types/hours.types.ts` - NEW: Holiday, exception, break types
- `stores/hours.store.ts` - NEW: Hours management state
- `services/hours.service.ts` - NEW: Hours API client

#### Backend (src/BoundedContexts/ServiceCatalog/)
- `Domain/ValueObjects/BusinessHours.cs` - Add break times support
- `Domain/ValueObjects/HolidaySchedule.cs` - NEW: Holiday/exception value object
- `Domain/ValueObjects/BreakPeriod.cs` - NEW: Break period value object
- `Domain/Aggregates/ProviderAggregate/Provider.cs` - Add holiday management methods
- `Application/Commands/Provider/SetHolidays/` - NEW: Holiday management command
- `Application/Commands/Provider/SetExceptions/` - NEW: Exception management command
- `Application/Queries/Provider/GetAvailability/` - MODIFIED: Include holidays/exceptions
- `Api/Controllers/V1/ProviderScheduleController.cs` - NEW: Schedule endpoints

### Dependencies
- Calendar UI component library or custom implementation
- Date range picker component
- Backend support for complex schedule queries

### Breaking Changes
None - additive changes that enhance existing functionality.

### Migration
No data migration needed. Existing BusinessHours remain compatible. New features are opt-in.
