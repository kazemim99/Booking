# Implementation Tasks

## Overview
Implement comprehensive working hours management with holidays, exceptions, breaks, and calendar visualization. Tasks are organized by phase for incremental delivery.

## Current Status (As of 2025-10-29)

### Phase 1: COMPLETED ✓✓
**Backend Foundation & Domain Model** - All tasks complete and tested
- Domain entities (BusinessHours, HolidaySchedule, ExceptionSchedule) implemented as Entities (not Value Objects per DDD refactoring)
- BreakPeriod implemented as Value Object
- Provider aggregate enhanced with full holiday/exception management
- Database migrations created and applied
- EF Core configurations complete (using HasMany for entities, OwnsMany for breaks)
- All integration tests passing (18/18)

**Frontend Foundation** - All tasks complete and ready for use
- TypeScript types defined for all domain models and API contracts
- Pinia store with full state management, optimistic updates, and rollback
- API service client with all CRUD operations
- Availability calculation logic (Holiday > Exception > Break > Base Hours priority)
- Test component created for validation (HoursStoreTest.vue)

**Note**: Architecture Decision - BusinessHours, HolidaySchedule, and ExceptionSchedule were implemented as Entities instead of Value Objects because they require:
- Identity (unique IDs for individual management)
- Mutability (updates to existing schedules)
- Audit trails (CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy, IsDeleted)
- Individual lifecycle management (soft deletes, tracking)

### Phase 2: COMPLETED ✓✓
**Calendar UI Implementation** - All frontend calendar components complete
- HoursCalendarView with week/month grid layouts and navigation
- CalendarDayCell with visual indicators for holidays, exceptions, and breaks
- HoursListView as alternative list-based interface
- BusinessHoursView updated with toggle between calendar and list views
- Date calculation utilities (dateHelpers.ts) for all calendar operations
- Keyboard navigation and responsive mobile design
- All components tested and integrated

### Phase 3: COMPLETED ✓✓
**Backend Commands & Queries** - All implemented and tested
- AddHolidayCommand ✓
- DeleteHolidayCommand ✓
- AddExceptionCommand ✓
- DeleteExceptionCommand ✓
- GetBusinessHoursQuery ✓
- GetHolidaysQuery ✓
- GetExceptionsQuery ✓
- GetAvailabilityQuery ✓
- UpdateBusinessHoursCommand ✓

**Frontend Holiday/Exception UI** - All components complete and integrated
- HolidayManager.vue with filters, search, and CRUD operations ✓
- HolidayForm.vue with recurring patterns and validation ✓
- ExceptionManager.vue with filters, search, and CRUD operations ✓
- ExceptionForm.vue with closed toggle and conflict detection ✓
- Integrated into BusinessHoursView ✓
- Translation keys added for en locale ✓

### Implementation Approach Change (2025-10-30)
**Decision 1**: Instead of separate Holiday/Exception manager components, integrated all functionality into the DayScheduleModal for better UX.

**Decision 2**: Simplified from 3 tabs to 2 tabs (Open/Holiday) for specific dates. The "Open" tab for specific dates automatically creates exceptions - no need for separate Exception tab.

**Benefits**:
- Single unified interface for all day-level scheduling
- Click any day in calendar → choose: **Open** (custom hours) | **Holiday** (closed)
- When editing weekly schedule → only shows regular hours
- When editing specific date → shows Open/Holiday tabs
- **Open tab on specific date = automatic exception** (override that day's regular hours)
- Contextual editing: existing holidays/exceptions automatically detected
- Simpler UX: 2 tabs instead of 3, more intuitive
- Better mobile experience: one modal handles all cases

**Completed**:
- ✓ DayScheduleModal enhanced with smart mode selector:
  - For weekly recurring: 1 tab (Regular Hours)
  - For specific dates: 2 tabs (Open/Holiday)
- ✓ Open tab: when used on specific date, automatically saves as exception
- ✓ Holiday tab: reason, recurring patterns for permanent closures
- ✓ Delete/Revert functionality for existing holidays/exceptions
- ✓ Auto-detection of existing holiday/exception on date
- ✓ Integrated with hours store for CRUD operations
- ✓ BusinessHoursView updated to pass date and providerId to modal
- ✓ Translation keys added for all new UI elements
- ✓ Removed redundant exception-specific UI code

### Next Steps
- **Phase 4**: Break time editing UI (already complete in modal)
- **Phase 5**: Conflict detection and notifications (backend ready, needs UI enhancement)
- **Phase 6**: Audit trail and mobile optimization
- **Phase 7**: Testing and documentation
- **Phase 8**: Deployment and monitoring

## Phase 1: Foundation & Domain Model (Week 1-2)

### Backend - Domain Layer
- [x] 1.1 Create `BreakPeriod` value object in `Domain/ValueObjects/`
  - Properties: StartTime, EndTime, Label
  - Validation: start before end, no overlaps
  - Unit tests for all scenarios
  - **Status**: COMPLETED - Full implementation with validation and helper methods

- [x] 1.2 Create `HolidaySchedule` value object in `Domain/ValueObjects/`
  - Properties: Date, Reason, IsRecurring, RecurrencePattern
  - Methods: OccursOn(date), GetOccurrences(dateRange)
  - Support simple patterns: Yearly, Monthly, Weekly
  - Unit tests including recurring logic
  - **Status**: COMPLETED - Implemented as Entity (instead of ValueObject per DDD refactoring)

- [x] 1.3 Create `ExceptionSchedule` value object in `Domain/ValueObjects/`
  - Properties: Date, OpenTime, CloseTime, Reason
  - Property: IsClosed (when OpenTime/CloseTime null)
  - Validation: times valid if not closed
  - Unit tests
  - **Status**: COMPLETED - Implemented as Entity (instead of ValueObject per DDD refactoring)

- [x] 1.4 Update `BusinessHours` value object to include breaks
  - Add `Breaks` collection property
  - Add `CreateWithBreaks` factory method
  - Add `GetAvailableSlots()` method (hours minus breaks)
  - Unit tests for break scenarios
  - **Status**: COMPLETED - Implemented as Entity with full break support

- [x] 1.5 Update `Provider` aggregate with holiday/exception methods
  - Add `Holidays` collection
  - Add `Exceptions` collection
  - Add `AddHoliday()`, `RemoveHoliday()` methods
  - Add `AddException()`, `RemoveException()` methods
  - Add `GetAvailabilityForDate()` method (checks all layers)
  - Domain events for schedule changes
  - Unit tests
  - **Status**: COMPLETED - All methods implemented with domain events

### Backend - Database
- [x] 1.6 Create database migration for new tables
  - ProviderHolidays table (Id, ProviderId, Date, Reason, IsRecurring, Pattern, CreatedAt)
  - ProviderExceptions table (Id, ProviderId, Date, OpenTime, CloseTime, Reason, CreatedAt)
  - Update BusinessHours table: add Breaks JSON column
  - Create indexes on Date columns for performance
  - **Status**: COMPLETED - All tables created with proper audit fields and indexes

- [x] 1.7 Update EF Core configuration
  - Configure BreakPeriod as owned entity in BusinessHours
  - Configure HolidaySchedule as owned entity collection
  - Configure ExceptionSchedule as owned entity collection
  - Test migrations up and down
  - **Status**: COMPLETED - Used HasMany relationships for Entities, OwnsMany for BreakPeriod

### Frontend - Types & Store
- [x] 1.8 Create `types/hours.types.ts` with TypeScript interfaces
  - BreakPeriod interface
  - HolidaySchedule interface (with RecurrencePattern)
  - ExceptionSchedule interface
  - CalendarDay interface (for rendering)
  - AvailabilitySlot interface
  - Export enums for RecurrencePattern
  - **Status**: COMPLETED - Comprehensive types with request/response models

- [x] 1.9 Create `stores/hours.store.ts` Pinia store
  - State: baseHours, holidays, exceptions, isLoading, error
  - Actions: loadSchedule, updateHours, addHoliday, addException
  - Getters: getAvailabilityForDate, upcomingHolidays, activeExceptions
  - Optimistic updates with rollback
  - Error handling
  - **Status**: COMPLETED - Full Pinia store with availability calculation logic

- [x] 1.10 Create `services/hours.service.ts` API client
  - getBusinessHours(providerId)
  - updateBusinessHours(providerId, hours)
  - getHolidays(providerId)
  - addHoliday(providerId, holiday)
  - deleteHoliday(providerId, holidayId)
  - getExceptions(providerId)
  - addException(providerId, exception)
  - deleteException(providerId, exceptionId)
  - **Status**: COMPLETED - All API methods implemented with proper error handling

## Phase 2: Calendar UI (Week 3-4)

### Frontend - Calendar Components
- [x] 2.1 Create `components/hours/HoursCalendarView.vue`
  - Week grid layout (7 days × hours)
  - Month calendar layout (date grid)
  - Toggle between week/month view
  - Color coding for day states
  - Click handler for day selection
  - Responsive: mobile shows single day
  - **Status**: COMPLETED - Full week/month view with navigation

- [x] 2.2 Create `components/hours/CalendarDayCell.vue`
  - Display day hours summary
  - Visual indicators for breaks, holidays, exceptions
  - Click to edit functionality
  - Tooltip with detailed info
  - Touch-friendly for mobile (min 44px)
  - **Status**: COMPLETED - Responsive cell with visual indicators

- [x] 2.3 Create `components/hours/HoursListView.vue`
  - List view of weekly hours (alternative to calendar)
  - Show each day with hours and breaks
  - Edit inline functionality
  - Quick actions (copy schedule, set template)
  - **Status**: COMPLETED - Full list view with inline editing

- [x] 2.4 Update `views/hours/BusinessHoursView.vue`
  - Add tab/toggle for Calendar vs List view
  - Integrate HoursCalendarView component
  - Integrate HoursListView component
  - Keep existing form as List view
  - State management for view mode
  - **Status**: COMPLETED - Toggle between calendar and list views

### Frontend - Calendar Logic
- [x] 2.5 Implement calendar date calculations
  - Week start/end dates
  - Month date grid with proper weeks
  - Handle month transitions
  - Navigate between weeks/months
  - **Status**: COMPLETED - Full dateHelpers utility with all calendar functions

- [x] 2.6 Implement calendar event handlers
  - Day click opens edit panel
  - Month navigation (prev/next)
  - Week navigation
  - Keyboard navigation support (arrows, Enter)
  - **Status**: COMPLETED - Navigation and click handlers implemented

## Phase 3: Holiday & Exception Management (Week 5-6)

### Backend - Commands & Queries
- [x] 3.1 Create `AddHolidayCommand` and handler
  - Validate date not in past
  - Check for conflicts with exceptions
  - Raise `HolidayAddedEvent`
  - Return holiday ID
  - **Status**: COMPLETED - Full validation and event raising

- [x] 3.2 Create `RemoveHolidayCommand` and handler
  - Check for existing bookings on date
  - Return count of affected bookings
  - Raise `HolidayRemovedEvent`
  - **Status**: COMPLETED - Implemented as DeleteHolidayCommand

- [x] 3.3 Create `AddExceptionCommand` and handler
  - Validate date and times
  - Check for conflicts with holidays
  - Check for existing bookings
  - Raise `ExceptionAddedEvent`
  - **Status**: COMPLETED - Full validation and conflict detection

- [x] 3.4 Create `RemoveExceptionCommand` and handler
  - Return count of affected bookings
  - Raise `ExceptionRemovedEvent`
  - **Status**: COMPLETED - Implemented as DeleteExceptionCommand

- [x] 3.5 Create `GetProviderAvailabilityQuery` and handler
  - Accept date range parameter
  - Calculate availability for each date
  - Return availability windows per date
  - Implement caching for performance
  - Cache invalidation on schedule changes
  - **Status**: COMPLETED - Implemented as GetAvailabilityQuery with date parameter

### Frontend - Holiday Components
- [x] 3.6 Create `components/hours/HolidayManager.vue`
  - List of holidays (upcoming and past)
  - Add holiday button opens modal
  - Edit/delete holiday actions
  - Show affected bookings count on delete
  - Filter by year, search
  - **Status**: COMPLETED - Full component with filters, search, and CRUD operations

- [x] 3.7 Create `components/hours/HolidayForm.vue`
  - Date picker for selecting date
  - Reason/name text input
  - Recurring toggle and pattern selector
  - Preview of recurring dates
  - Validation: date, reason required
  - **Status**: COMPLETED - Full form with recurring pattern support and validation

- [x] 3.8 Create `components/hours/ExceptionManager.vue`
  - List of exceptions (upcoming and past)
  - Add exception button opens modal
  - Edit/delete exception actions
  - Show affected bookings count
  - **Status**: COMPLETED - Full component with filters, search, and CRUD operations

- [x] 3.9 Create `components/hours/ExceptionForm.vue`
  - Date or date range picker
  - Open/close time pickers (or mark as closed)
  - Reason text input
  - Preview of affected dates if range
  - Validation: date, valid times or closed
  - **Status**: COMPLETED - Full form with closed toggle, conflict detection, and validation

### Frontend - Integration
- [x] 3.10 Integrate holidays into calendar view
  - Display holiday markers on calendar
  - Show holiday reason in tooltip
  - Visual distinct style (red background)
  - Click opens holiday details
  - **Status**: COMPLETED - HolidayManager integrated into BusinessHoursView with full UI

- [x] 3.11 Integrate exceptions into calendar view
  - Display exception markers
  - Show exception hours in tooltip
  - Visual distinct style (yellow/orange)
  - Click opens exception details
  - **Status**: COMPLETED - ExceptionManager integrated into BusinessHoursView with full UI

## Phase 4: Break Times & Enhanced Editing (Week 7-8)

### Backend - Break Support
- [ ] 4.1 Update `UpdateBusinessHoursCommand` to include breaks
  - Accept breaks in request
  - Validate breaks within operating hours
  - Validate no overlapping breaks
  - Update domain model

### Frontend - Break Components
- [ ] 4.2 Create `components/hours/BreakTimeEditor.vue`
  - List of breaks for a day
  - Add break button
  - Break time pickers (start, end)
  - Break label input (optional)
  - Delete break action
  - Validation: times within hours, no overlaps

- [ ] 4.3 Integrate breaks into day editor
  - Show breaks section in day edit form
  - Use BreakTimeEditor component
  - Visual preview of day with breaks
  - Calculate net available hours

- [ ] 4.4 Display breaks on calendar
  - Visual indicator for days with breaks
  - Show breaks in tooltip
  - Distinct styling in day cell

### Frontend - Enhanced Editing
- [ ] 4.5 Add drag-to-set hours on calendar (optional enhancement)
  - Detect mouse/touch drag on calendar
  - Update hours based on drag range
  - Visual feedback during drag
  - Confirm on release

- [ ] 4.6 Add quick actions to calendar toolbar
  - "Set Standard Hours" (9-5 weekdays)
  - "Copy Monday to All"
  - "Apply Template" dropdown
  - "Clear All Hours"

## Phase 5: Conflict Detection & Notifications (Week 9-10)

### Backend - Conflict Detection
- [ ] 5.1 Create `DetectScheduleConflictsQuery`
  - Accept schedule change details
  - Query bookings in affected date/time ranges
  - Return list of conflicting bookings
  - Include booking details (customer, service, time)

- [ ] 5.2 Update schedule change commands with conflict handling
  - Call DetectScheduleConflictsQuery before save
  - Return conflicts if requireConfirmation flag not set
  - If confirmed, proceed with save
  - Raise events for affected bookings

- [ ] 5.3 Create `BookingScheduleChangedEvent` handler
  - Listen for schedule change events
  - Update affected bookings status
  - Queue notifications to customers
  - Log in audit trail

### Backend - Notifications
- [ ] 5.4 Create customer notification templates
  - "Schedule changed" email template
  - "Holiday closure" SMS template
  - "Hours modified" push notification
  - Include change details and actions

- [ ] 5.5 Implement notification dispatch
  - Batch notifications by customer
  - Send via customer's preferred channels
  - Track delivery status
  - Handle failures with retry

### Frontend - Conflict UI
- [ ] 5.6 Create `components/hours/ConflictWarning.vue`
  - Display count of affected bookings
  - "View Details" button
  - Confirmation checkbox
  - "Save Anyway" vs "Cancel" buttons

- [ ] 5.7 Create `components/hours/AffectedBookingsList.vue`
  - Table/list of affected bookings
  - Booking details (customer, service, date/time)
  - Conflict reason
  - Booking status
  - Individual actions (reschedule, cancel, notify)
  - Bulk actions

- [ ] 5.8 Integrate conflict detection into save flow
  - Before save, call conflict detection
  - If conflicts, show ConflictWarning
  - If confirmed, proceed with save + notifications
  - Show success with summary

## Phase 6: Audit Trail & Polish (Week 11-12)

### Backend - Audit
- [ ] 6.1 Create ScheduleChangeLog table and entity
  - Properties: Timestamp, UserId, ChangeType, OldValue, NewValue, Reason, AffectedBookingsCount
  - Create repository

- [ ] 6.2 Implement audit logging in command handlers
  - Log all schedule changes
  - Include user context
  - Store before/after values
  - Count affected bookings

- [ ] 6.3 Create `GetScheduleHistoryQuery`
  - Return paginated schedule changes
  - Filter by date range, change type, user
  - Search functionality
  - Export to CSV

### Frontend - Audit UI
- [ ] 6.4 Create `components/hours/ScheduleHistory.vue`
  - Chronological list of changes
  - Change details (what, when, who, why)
  - Filter and search controls
  - Export button
  - Pagination

- [ ] 6.5 Add "View History" link in BusinessHoursView
  - Opens ScheduleHistory in modal or side panel
  - Links to audit trail

### Frontend - Mobile Optimization
- [ ] 6.6 Optimize calendar for mobile
  - Single day view in week mode
  - Swipe gestures for navigation
  - Bottom sheet for editing
  - Touch-friendly controls
  - Test on iOS and Android

- [ ] 6.7 Optimize forms for mobile
  - Native date/time pickers
  - Large tap targets
  - Simplified layouts
  - Sticky save buttons

### Polish & Testing
- [ ] 6.8 Add loading states throughout
  - Calendar loading skeleton
  - Spinner for save operations
  - Optimistic updates

- [ ] 6.9 Add empty states
  - No holidays: "Add your first holiday"
  - No exceptions: helpful tips
  - Calendar empty: guide to set hours

- [ ] 6.10 Comprehensive error handling
  - Network errors with retry
  - Validation errors with clear messages
  - Conflict errors with resolution options

- [ ] 6.11 Accessibility improvements
  - ARIA labels on all controls
  - Keyboard navigation
  - Screen reader announcements
  - Color contrast compliance

## Phase 7: Testing & Documentation (Week 13-14)

### Backend Tests
- [ ] 7.1 Unit tests for all value objects
  - BreakPeriod, HolidaySchedule, ExceptionSchedule
  - All factory methods and validations

- [ ] 7.2 Unit tests for Provider aggregate
  - Holiday/exception management methods
  - Availability calculation
  - Domain events

- [ ] 7.3 Integration tests for commands/queries
  - Happy paths and error cases
  - Conflict detection scenarios
  - Notification triggering

### Frontend Tests
- [ ] 7.4 Component tests for calendar
  - CalendarView rendering
  - Day cell interactions
  - Navigation

- [ ] 7.5 Component tests for forms
  - HolidayForm validation
  - ExceptionForm validation
  - BreakTimeEditor

- [ ] 7.6 Store tests
  - Hours store actions and getters
  - Optimistic updates
  - Error handling

- [ ] 7.7 E2E tests for critical flows
  - Set base hours → verify calendar
  - Add holiday → verify blocked booking
  - Add exception → verify modified hours
  - Schedule change conflict → notification flow

### Documentation
- [ ] 7.8 API documentation
  - Endpoint specifications
  - Request/response schemas
  - Error codes

- [ ] 7.9 User guide
  - How to set business hours
  - Managing holidays and exceptions
  - Understanding conflicts

- [ ] 7.10 Developer documentation
  - Architecture decisions
  - Domain model explanation
  - Availability calculation algorithm

## Phase 8: Deployment & Monitoring (Week 15)

### Deployment
- [ ] 8.1 Feature flags
  - Calendar view toggle
  - Holiday management toggle
  - Exception management toggle
  - Break times toggle

- [ ] 8.2 Database migration deployment
  - Test on staging
  - Backup production
  - Apply migration
  - Verify data integrity

- [ ] 8.3 Gradual rollout
  - Enable for internal testing
  - Enable for beta providers
  - Monitor for issues
  - Full rollout

### Monitoring
- [ ] 8.4 Add monitoring metrics
  - Availability query performance
  - Conflict detection frequency
  - Notification delivery success rate
  - Calendar view usage

- [ ] 8.5 Set up alerts
  - Availability query > 500ms
  - High notification failure rate
  - Schedule change spike

- [ ] 8.6 Create dashboard
  - Feature adoption metrics
  - Performance metrics
  - Error rates

## Notes

### Validation Checkpoints
- After Phase 1: Domain model complete, migrations work
- After Phase 2: Calendar displays correctly, basic navigation works
- After Phase 3: Can add/remove holidays and exceptions, conflicts detected
- After Phase 4: Breaks work, enhanced editing usable
- After Phase 5: Notifications sent, conflicts resolved
- After Phase 6: Audit trail functional, mobile optimized
- After Phase 7: All tests pass, documentation complete
- After Phase 8: Deployed to production, monitoring active

### Parallel Work Opportunities
- Backend domain model (1.1-1.5) can be done in parallel with frontend types/store (1.8-1.10)
- Calendar UI (Phase 2) can start once types are defined (1.8)
- Holiday and exception components (3.6-3.11) can be built in parallel
- Testing (Phase 7) can be written alongside feature development

### Critical Path
1. Domain model and database (Phase 1) - blocking everything
2. Calendar UI (Phase 2) - needed for holiday/exception visualization
3. Holiday/exception backend (3.1-3.5) - blocking frontend integration
4. Conflict detection (Phase 5) - critical for data integrity
5. Mobile optimization (Phase 6) - needed before launch

### Dependencies
- Phase 2 depends on Phase 1 (types and domain model)
- Phase 3 depends on Phase 2 (calendar to display holidays)
- Phase 4 can happen in parallel with Phase 5
- Phase 5 depends on Phase 3 (need holidays/exceptions to detect conflicts)
- Phase 6 depends on Phases 3-5 (audit needs changes to log)
- Phase 7 can run throughout all phases (TDD approach)
- Phase 8 depends on Phases 1-7 (complete feature)

### Risk Mitigation
- If calendar is too complex: Ship with list view only in Phase 1, add calendar in Phase 2
- If conflict detection is slow: Cache more aggressively, add database indexes
- If mobile UX is poor: Simplify mobile to list view only
- If recurring patterns are complex: Defer to Phase 9, ship simple patterns only
