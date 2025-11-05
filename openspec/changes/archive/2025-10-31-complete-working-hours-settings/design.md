# Design Document: Complete Working Hours Settings

## Context

The current BusinessHoursView provides basic configuration of weekly operating hours. Providers need advanced scheduling capabilities to handle real-world scenarios like holidays, temporary closures, break times, and seasonal schedule changes. This enhancement maintains backward compatibility while adding comprehensive schedule management.

### Stakeholders
- **Providers**: Need flexible schedule management for their business reality
- **Customers**: Need accurate availability information to avoid booking conflicts
- **Development Team**: Responsible for implementation across frontend and backend

### Constraints
- Must maintain compatibility with existing BusinessHours value object
- Must not break existing bookings when schedule changes
- Calendar UI must work on mobile devices
- Holiday/exception logic must be performant for availability queries

## Goals / Non-Goals

### Goals
1. Enable providers to mark specific dates as closed (holidays, vacations)
2. Support temporary schedule overrides (exceptions) for date ranges
3. Allow configuration of break times within operating hours
4. Provide visual calendar view for better schedule understanding
5. Support recurring patterns for periodic closures
6. Notify customers when schedule changes affect their bookings
7. Make availability calculations accurate and performant

### Non-Goals
1. **NOT** implementing team member scheduling/shifts (separate concern in staff management)
2. **NOT** building advanced workforce scheduling/optimization
3. **NOT** creating customer-facing schedule negotiation
4. **NOT** implementing resource/room scheduling
5. **NOT** building payroll or time tracking features

## Decisions

### Decision 1: Three-Layer Schedule Model

**What**: Implement schedule as three layers: Base Hours → Breaks → Exceptions/Holidays (highest priority)

**Why**: Clear hierarchy makes availability calculation predictable and allows overrides without data conflicts.

**Model Structure**:
```
Daily Availability = Base Hours - Break Times + Exception Overrides - Holiday Closures
```

**Priority Order** (highest to lowest):
1. Holiday Closures (full day closed)
2. Exception Overrides (custom hours for specific dates)
3. Break Times (unavailable periods within base hours)
4. Base Hours (regular weekly schedule)

**Example**:
- Base: Monday 9 AM - 5 PM
- Break: 12 PM - 1 PM (lunch)
- Exception: Next Monday 10 AM - 3 PM (staff meeting)
- Holiday: Following Monday (Christmas) - CLOSED
- Result:
  - Regular Monday: 9-12, 1-5 (7 hours)
  - Next Monday: 10-12, 1-3 (4 hours)
  - Following Monday: CLOSED

**Alternatives Considered**:
- Flat list of all schedule entries: Complex to reason about, conflicts hard to detect
- Single overridable schedule: Loses base pattern, requires duplication

**Trade-offs**:
- Pro: Clear mental model, easy conflict resolution
- Pro: Efficient queries (check layers in order)
- Con: More complex data model than flat structure
- Mitigation: Encapsulate logic in domain, provide clear UI feedback

### Decision 2: Calendar View as Primary Interface

**What**: Default to calendar view with toggle to list view, enable drag-to-set hours on calendar

**Why**: Visual representation makes weekly patterns immediately apparent and is more intuitive for setting hours.

**Implementation**:
- Week calendar with 7-day grid showing hours (7 AM - 10 PM range)
- Month calendar for marking holidays/exceptions
- Color coding: green=open, gray=closed, yellow=exception, red=holiday
- Click date cell to edit, drag to set continuous hours
- List view still available for precise time entry

**Alternatives Considered**:
- Keep list as primary with calendar as secondary: Less discoverable, harder to see patterns
- Calendar only: Loses precision for exact minute entry
- Separate pages for calendar vs list: More navigation, fragmented UX

**Trade-offs**:
- Pro: Better visual understanding of schedule
- Pro: Faster for pattern-based configuration
- Con: More complex UI implementation
- Con: Requires careful mobile adaptation
- Mitigation: Progressive enhancement (mobile gets simplified calendar)

### Decision 3: Holiday/Exception as Separate Value Objects

**What**: Create new value objects for HolidaySchedule and ExceptionSchedule, reference them from Provider aggregate

**Why**: Maintains separation of concerns, allows independent validation, enables querying holidays separately

**Domain Model**:
```csharp
// Value Object for holidays
public sealed class HolidaySchedule : ValueObject
{
    public DateOnly Date { get; private init; }
    public string Reason { get; private init; }
    public bool IsRecurring { get; private init; }
    public RecurrencePattern? Pattern { get; private init; }

    public static HolidaySchedule Create(DateOnly date, string reason, bool isRecurring = false)
    public bool OccursOn(DateOnly date) // Handles recurring check
}

// Value Object for exception hours
public sealed class ExceptionSchedule : ValueObject
{
    public DateOnly Date { get; private init; }
    public TimeOnly? OpenTime { get; private init; }
    public TimeOnly? CloseTime { get; private init; }
    public string Reason { get; private init; }

    public static ExceptionSchedule Create(DateOnly date, TimeOnly? open, TimeOnly? close, string reason)
    public bool IsClosed => !OpenTime.HasValue
}

// Updated BusinessHours with breaks
public sealed class BusinessHours : ValueObject
{
    public DayOfWeek DayOfWeek { get; private init; }
    public TimeOnly? OpenTime { get; private init; }
    public TimeOnly? CloseTime { get; private init; }
    public List<BreakPeriod> Breaks { get; private init; } // NEW

    public static BusinessHours CreateWithBreaks(DayOfWeek day, TimeOnly open, TimeOnly close, List<BreakPeriod> breaks)
}

// Value Object for breaks
public sealed class BreakPeriod : ValueObject
{
    public TimeOnly StartTime { get; private init; }
    public TimeOnly EndTime { get; private init; }
    public string? Label { get; private init; } // e.g., "Lunch Break"

    public static BreakPeriod Create(TimeOnly start, TimeOnly end, string? label = null)
}
```

**Alternatives Considered**:
- Store as JSON in Provider: Loses type safety, validation harder
- Flatten into BusinessHours: Mixes concerns, makes queries complex
- Separate aggregate: Over-engineering for this bounded context

**Trade-offs**:
- Pro: Clean domain model, strong typing
- Pro: Each value object has single responsibility
- Con: More classes to maintain
- Mitigation: Comprehensive unit tests for each value object

### Decision 4: Optimistic Availability Calculation

**What**: Cache calculated availability windows, invalidate when schedule changes

**Why**: Checking holidays/exceptions on every availability query is expensive, especially for calendar views

**Caching Strategy**:
```
AvailabilityCache[providerId][date] = {
  isOpen: bool,
  hours: TimeRange[],
  reason: string? // if closed/modified
}

Cache Invalidation:
- When base hours change: clear all cache
- When holiday added/removed: clear affected dates
- When exception added/removed: clear affected dates
- TTL: 24 hours (safety net)
```

**Alternatives Considered**:
- Real-time calculation every time: Too slow for month calendar views
- Pre-calculate months ahead: Wastes resources, stale for exceptions
- No caching: Poor performance

**Trade-offs**:
- Pro: Fast availability queries
- Pro: Scales to many concurrent requests
- Con: Cache invalidation complexity
- Con: Memory overhead
- Mitigation: Clear invalidation rules, monitoring cache hit rate

### Decision 5: Customer Notification for Schedule Changes

**What**: When provider changes schedule affecting existing bookings, auto-generate notification to affected customers

**Why**: Customers need to know if their booked time is no longer available

**Notification Logic**:
```
IF schedule change affects date with existing bookings THEN
  FOR EACH affected booking
    IF booking is future AND not yet started THEN
      Generate notification to customer
      Mark booking as "schedule_changed"
      Offer reschedule/cancellation options
    END
  END
END
```

**Notification Content**:
- What changed (closed, different hours, break added)
- How it affects their booking
- Actions available (reschedule, cancel, keep if still valid)
- Deadline for action if needed

**Alternatives Considered**:
- Manual provider notification: Error-prone, burden on provider
- No notification: Poor customer experience, surprises customers
- Force cancel all: Too disruptive

**Trade-offs**:
- Pro: Proactive customer communication
- Pro: Maintains trust and satisfaction
- Con: Complexity in detecting conflicts
- Con: Notification volume could be high
- Mitigation: Batch notifications, smart conflict detection

### Decision 6: Mobile-First Calendar Implementation

**What**: Design calendar UI for mobile first, progressively enhance for desktop

**Why**: Many providers manage schedules on mobile devices

**Mobile UX**:
- Week view: Single day per screen, swipe to navigate
- Month view: Mini calendar for date selection
- Tap date to see/edit details in bottom sheet
- Simplified drag (long-press + drag)
- Large tap targets (min 44x44px)

**Desktop UX**:
- Full week grid visible
- Hover tooltips for details
- Click-and-drag to set hours
- Keyboard navigation support
- Side panels for details

**Alternatives Considered**:
- Desktop-first: Breaks on mobile, requires complete redesign
- Separate mobile/desktop apps: Duplication, maintenance burden
- No calendar on mobile: Forces providers to desktop

**Trade-offs**:
- Pro: Works everywhere
- Pro: Consistent feature parity
- Con: More complex responsive design
- Mitigation: Component library with built-in responsive patterns

## Risks / Trade-offs

### Risk 1: Calendar UI Complexity

**Risk**: Calendar component is complex to build and maintain, especially with drag-and-drop

**Impact**: High - Core feature UX depends on calendar quality

**Mitigation**:
1. Evaluate existing calendar libraries (FullCalendar, DayPilot, custom Vue calendar)
2. Build MVP with basic click-to-edit, add drag-and-drop in phase 2
3. Comprehensive browser/device testing
4. Fallback to list view if calendar fails to load
5. Feature flag for gradual rollout

**Library Evaluation**:
- **FullCalendar**: Mature, feature-rich, but heavy (~200KB), licensing cost
- **DayPilot**: Business-focused, good touch support, paid license
- **Custom Vue Component**: Full control, lighter weight, more development time
- **Recommendation**: Start with custom component using native HTML5 date inputs, evaluate paid libraries if custom proves insufficient

### Risk 2: Performance with Large Date Ranges

**Risk**: Querying availability for full month/year with many holidays/exceptions could be slow

**Impact**: Medium - Month calendar view could lag

**Mitigation**:
1. Paginate availability queries (load only visible month)
2. Cache calculated availability (see Decision 4)
3. Index holiday/exception dates in database
4. Lazy-load calendar cells on scroll
5. Show loading skeleton while calculating

**Performance Targets**:
- Single day availability: <50ms
- Week availability: <200ms
- Month availability: <500ms

### Risk 3: Booking Conflicts on Schedule Changes

**Risk**: Provider changes schedule, creating conflicts with existing bookings

**Impact**: High - Could lead to no-shows, customer frustration

**Mitigation**:
1. Preview affected bookings before saving schedule change
2. Require confirmation if conflicts detected
3. Auto-notification to affected customers (see Decision 5)
4. Provide bulk reschedule tool for provider
5. Log all schedule changes for audit trail

**Conflict Handling Flow**:
```
1. Provider modifies schedule
2. System checks for existing bookings in affected times
3. If conflicts found:
   - Show warning with list of affected bookings
   - Show "View Affected Bookings" button
   - Require confirmation checkbox
   - Suggest alternative actions (reschedule all, notify)
4. If confirmed, apply changes + trigger notifications
```

### Risk 4: Recurring Pattern Complexity

**Risk**: Recurring holidays/exceptions (every 2nd Monday, last Friday of month) are complex to implement and understand

**Impact**: Medium - Nice-to-have feature, not critical for MVP

**Mitigation**:
1. Phase 1: Simple recurring (weekly, monthly, yearly)
2. Phase 2: Complex patterns (nth weekday of month)
3. Clear UI preview of which dates affected by pattern
4. Limit complexity (no custom cron expressions)
5. Provide pattern templates (common holidays, bi-weekly)

**Phased Approach**:
- **MVP**: Single date holidays/exceptions only
- **Phase 1**: Simple recurrence (every Monday, every Jan 1)
- **Phase 2**: Complex recurrence (2nd Tuesday, last Friday)

### Risk 5: Timezone Handling

**Risk**: Time conversions between provider timezone and customer timezone could cause confusion

**Impact**: Medium - Especially for providers with remote customers

**Mitigation**:
1. Always store times in provider's configured timezone
2. Display times in provider timezone in management UI
3. Customer-facing displays convert to customer timezone
4. Clear "All times in [Timezone]" labels
5. Handle daylight saving time transitions

**Example**:
- Provider (NYC, EST): Sets hours 9 AM - 5 PM
- Storage: 09:00 - 17:00 America/New_York
- Customer (London): Sees 2 PM - 10 PM GMT
- Display: "9 AM - 5 PM EST" in provider UI, "2 PM - 10 PM GMT" in customer UI

## Migration Plan

### Phase 1: Foundation & Basic Calendar (Week 1-2)
1. Create new value objects (HolidaySchedule, ExceptionSchedule, BreakPeriod)
2. Update Provider aggregate with holiday/exception collections
3. Create database migrations
4. Implement basic calendar component (week view, no drag-and-drop)
5. **Validation**: Can display current hours in calendar view

### Phase 2: Holiday & Exception Management (Week 3-4)
1. Build HolidayManager UI component
2. Implement date picker for marking holidays
3. Create ExceptionSchedule UI for temporary hour overrides
4. Implement backend commands for holidays/exceptions
5. Add availability query that checks holidays/exceptions
6. **Validation**: Can mark holidays, set exceptions, see accurate availability

### Phase 3: Break Times & Enhanced Calendar (Week 5-6)
1. Add break period support to BusinessHours
2. Create BreakTimeEditor UI component
3. Enhance calendar with drag-to-set hours
4. Add visual indicators for breaks on calendar
5. Implement calendar interaction improvements
6. **Validation**: Can set breaks, drag to modify hours, visual feedback works

### Phase 4: Conflict Detection & Notifications (Week 7-8)
1. Implement booking conflict detection on schedule changes
2. Build preview of affected bookings UI
3. Create customer notification templates
4. Implement notification sending on schedule changes
5. Add audit log for schedule changes
6. **Validation**: Detects conflicts, notifies customers, logs changes

### Phase 5: Mobile & Recurring Patterns (Week 9-10)
1. Optimize calendar for mobile devices
2. Implement touch-friendly interactions
3. Add simple recurring holiday patterns (weekly, monthly, yearly)
4. Test across devices and browsers
5. Performance optimization and caching
6. **Validation**: Works on mobile, recurring patterns function correctly

### Rollback Plan
Each phase is independently valuable and can be shipped:
- Phase 1: Basic calendar view improves UX over list
- Phase 2: Holiday/exception management solves immediate need
- Phase 3: Breaks and enhanced calendar add polish
- Phases 4-5: Advanced features, not critical for core functionality

Feature flags control rollout of each phase independently.

### Database Migration
Add new tables:
- `ProviderHolidays` (Id, ProviderId, Date, Reason, IsRecurring, RecurrencePattern)
- `ProviderExceptions` (Id, ProviderId, Date, OpenTime, CloseTime, Reason)

Update table:
- `BusinessHours` add `Breaks` JSON column (or separate BreakPeriods table)

Migration is additive, no data transformation needed.

## Open Questions

### Q1: Calendar Library vs Custom Implementation
**Question**: Should we use a paid calendar library (FullCalendar/DayPilot) or build custom?

**Recommendation**: Start custom for MVP using native HTML5 inputs. Evaluate paid libraries in Phase 3 if custom proves insufficient.

**Decision Needed By**: Before Phase 1 (Week 1)

### Q2: Recurring Pattern Complexity
**Question**: How complex should recurring patterns be? Support cron-like expressions or simple patterns?

**Recommendation**: Simple patterns only (weekly, monthly, yearly). Complex patterns (nth weekday) in Phase 5 if needed.

**Decision Needed By**: Before Phase 5 (Week 9)

### Q3: Booking Auto-Rescheduling
**Question**: Should system automatically reschedule bookings when schedule changes, or just notify customers?

**Recommendation**: Notification only. Auto-rescheduling is complex and could upset customers who prefer their original time.

**Decision Needed By**: Before Phase 4 (Week 7)

### Q4: Break Time Granularity
**Question**: Allow multiple breaks per day or single break only?

**Recommendation**: Multiple breaks (lunch, afternoon tea, etc.). Use list/array of BreakPeriod.

**Decision Needed By**: Before Phase 3 (Week 5)

### Q5: Historical Schedule Data
**Question**: Should we keep history of schedule changes for analytics/auditing?

**Recommendation**: Yes, log all changes in audit table (timestamp, user, old values, new values). Useful for compliance and debugging.

**Decision Needed By**: Before Phase 4 (Week 7)

## Success Criteria

1. **Functional**: Providers can set holidays, exceptions, and breaks through intuitive calendar UI
2. **Accurate**: Availability calculations reflect all schedule layers correctly
3. **Performant**: Month calendar loads within 500ms
4. **Mobile-Friendly**: Full functionality on mobile devices with touch-optimized UX
5. **Reliable**: Booking conflicts detected and customers notified proactively
6. **Tested**: >80% test coverage, E2E tests for critical schedule scenarios passing
7. **Adopted**: >60% of providers use holiday/exception features within first month
