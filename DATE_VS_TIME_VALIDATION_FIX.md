# Date vs Time Validation Separation Fix

**Date**: November 17, 2025
**Issue**: Time-based validation being applied when selecting a date
**Status**: ✅ FIXED

---

## Problem

When users select a **DATE** from the calendar to check availability, the system was incorrectly applying **TIME-based validations** that should only be checked when creating an actual booking with a specific time slot.

### User's Question
> "when we select a data from calender for get booking avalabiltiy why we should check time in ValidateBookingConstraintsAsync in GetAvailableSlotsQueryHandler???"

This was a valid logical concern - when just selecting a date to see what time slots are available, we shouldn't validate:
- ❌ "Booking must be made at least X **hours** in advance"
- ❌ "Booking time must be between business hours" (specific time check)
- ❌ Past **time** checks (only past date matters)

We should only validate:
- ✅ Provider is active
- ✅ Service is active
- ✅ Provider allows online booking
- ✅ Date is within max advance booking **days**
- ✅ Provider is open on that day of week
- ✅ Not a holiday
- ✅ Not an exception/closed day

---

## Root Cause

The `ValidateBookingConstraintsAsync` method mixed date-level and time-level validations:

```csharp
public async Task<AvailabilityValidationResult> ValidateBookingConstraintsAsync(
    Provider provider,
    Service service,
    DateTime startTime,  // ← Called with just a DATE
    CancellationToken cancellationToken = default)
{
    // ✅ Date-level checks
    if (provider.Status != ProviderStatus.Active) { }
    if (service.Status != ServiceStatus.Active) { }

    // ❌ TIME-level checks (shouldn't run for date selection)
    var hoursUntilBooking = (startTime - DateTime.UtcNow).TotalHours;
    if (service.MinAdvanceBookingHours.HasValue &&
        hoursUntilBooking < service.MinAdvanceBookingHours.Value) { }

    // ❌ TIME within business hours check
    if (bookingTime < openTime || endTime > closeTime) { }
}
```

This method was being called in:
1. `GetAvailableSlotsQueryHandler` (line 64) - when selecting a **DATE**
2. `GetAvailableTimeSlotsAsync` (line 59) - when getting slots for a **DATE**
3. `IsTimeSlotAvailableAsync` (line 145) - when checking a specific **TIME** ✅ Correct!

---

## Solution

### 1. Created New `ValidateDateConstraintsAsync` Method

**File**: [AvailabilityService.cs:326-400](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Services/AvailabilityService.cs#L326-L400)

```csharp
public Task<AvailabilityValidationResult> ValidateDateConstraintsAsync(
    Provider provider,
    Service service,
    DateTime date,  // ← Just a date, not a specific time
    CancellationToken cancellationToken = default)
{
    var errors = new List<string>();

    // Check if provider is active
    if (provider.Status != ProviderStatus.Active)
    {
        errors.Add("Provider is not active");
    }

    // Check if provider allows online booking
    if (!provider.AllowOnlineBooking)
    {
        errors.Add("Provider does not allow online booking");
    }

    // Check if service is active
    if (service.Status != ServiceStatus.Active)
    {
        errors.Add("Service is not active");
    }

    // Check maximum advance booking (DATE-LEVEL only, not hours)
    var daysUntilBooking = (date.Date - DateTime.UtcNow.Date).TotalDays;
    if (service.MaxAdvanceBookingDays.HasValue &&
        daysUntilBooking > service.MaxAdvanceBookingDays.Value)
    {
        errors.Add($"Booking cannot be made more than {service.MaxAdvanceBookingDays.Value} days in advance");
    }

    // Check if date is in the past
    if (date.Date < DateTime.UtcNow.Date)
    {
        errors.Add("Cannot book appointments in the past");
    }

    // Check if provider is open on this day of week
    var dayOfWeek = (DayOfWeek)(int)date.DayOfWeek;
    var businessHours = provider.BusinessHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);

    if (businessHours == null || !businessHours.IsOpen)
    {
        errors.Add($"Provider is closed on {dayOfWeek}");
    }

    // Check for holidays
    if (IsHoliday(provider, date.Date))
    {
        errors.Add("Provider is closed on this date (holiday)");
    }

    // Check exceptions
    var exception = GetExceptionSchedule(provider, date.Date);
    if (exception != null && exception.IsClosed)
    {
        errors.Add("Provider is closed on this date (exception)");
    }

    return Task.FromResult(errors.Any()
        ? AvailabilityValidationResult.Failure(errors.ToArray())
        : AvailabilityValidationResult.Success());
}
```

**Key Differences from `ValidateBookingConstraintsAsync`**:
- ❌ **Removed**: `MinAdvanceBookingHours` check (hours-based)
- ❌ **Removed**: Time within business hours check
- ✅ **Kept**: All date-level validations
- ✅ **Changed**: Max advance uses `.TotalDays` not `.TotalHours`
- ✅ **Changed**: Past check uses `date.Date` not `startTime`

### 2. Added Interface Method

**File**: [IAvailabilityService.cs:79-92](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/DomainServices/IAvailabilityService.cs#L79-L92)

```csharp
/// <summary>
/// Validate date-level constraints (holidays, day of week, max advance days) without time checks
/// Used when checking if a date has any availability before generating time slots
/// </summary>
Task<AvailabilityValidationResult> ValidateDateConstraintsAsync(
    Provider provider,
    Service service,
    DateTime date,
    CancellationToken cancellationToken = default);
```

### 3. Updated Query Handler

**File**: [GetAvailableSlotsQueryHandler.cs:63-68](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Queries/Booking/GetAvailableSlots/GetAvailableSlotsQueryHandler.cs#L63-L68)

**Before**:
```csharp
// Validate booking constraints first
var validationResult = await _availabilityService.ValidateBookingConstraintsAsync(
    provider,
    service,
    request.Date,  // ← Passing date to time-based validation
    cancellationToken);
```

**After**:
```csharp
// Validate date-level constraints (not time-level since we're just selecting a date)
var validationResult = await _availabilityService.ValidateDateConstraintsAsync(
    provider,
    service,
    request.Date,  // ← Passing date to date-based validation
    cancellationToken);
```

### 4. Updated GetAvailableTimeSlotsAsync

**File**: [AvailabilityService.cs:58-65](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Services/AvailabilityService.cs#L58-L65)

**Before**:
```csharp
// Check if date is within booking window
var validationResult = await ValidateBookingConstraintsAsync(provider, service, date, cancellationToken);
if (!validationResult.IsValid)
{
    _logger.LogWarning("Booking constraints validation failed: {Errors}",
        string.Join(", ", validationResult.Errors));
    return Array.Empty<AvailableTimeSlot>();
}
```

**After**:
```csharp
// Check if date is within booking window (date-level validation only)
var validationResult = await ValidateDateConstraintsAsync(provider, service, date, cancellationToken);
if (!validationResult.IsValid)
{
    _logger.LogWarning("Date constraints validation failed: {Errors}",
        string.Join(", ", validationResult.Errors));
    return Array.Empty<AvailableTimeSlot>();
}
```

---

## When to Use Each Validation Method

### Use `ValidateDateConstraintsAsync` when:
- ✅ User selects a **date** from calendar
- ✅ Getting available time slots for a date
- ✅ Checking if a date has ANY availability
- ✅ Validating date range requests

**Example Scenarios**:
- User clicks on November 20th in calendar → Use `ValidateDateConstraintsAsync`
- API call: `GET /api/v1/availability/slots?date=2025-11-20` → Use `ValidateDateConstraintsAsync`
- API call: `GET /api/v1/availability/dates?fromDate=...&toDate=...` → Use `ValidateDateConstraintsAsync`

### Use `ValidateBookingConstraintsAsync` when:
- ✅ User selects a **specific time slot** to book
- ✅ Creating an actual booking
- ✅ Checking if a specific time is available
- ✅ Need to validate hours-in-advance requirements

**Example Scenarios**:
- User clicks "Book" for 2:30 PM slot → Use `ValidateBookingConstraintsAsync`
- API call: `POST /api/v1/bookings` with specific startTime → Use `ValidateBookingConstraintsAsync`
- API call: `GET /api/v1/availability/check?startTime=2025-11-20T14:30:00Z` → Use `ValidateBookingConstraintsAsync`

---

## Validation Comparison

| Validation Check | `ValidateDateConstraintsAsync` | `ValidateBookingConstraintsAsync` |
|-----------------|-------------------------------|----------------------------------|
| Provider is active | ✅ | ✅ |
| Service is active | ✅ | ✅ |
| Online booking allowed | ✅ | ✅ |
| **Min hours in advance** | ❌ | ✅ |
| **Max days in advance** | ✅ (date-level) | ✅ (time-level) |
| Date in past | ✅ | ✅ |
| Day of week open | ✅ | ✅ |
| Not a holiday | ✅ | ✅ |
| Not an exception | ✅ | ✅ |
| **Time within business hours** | ❌ | ✅ |

---

## Benefits

### Before Fix
```
User selects: "November 20, 2025"
System checks: "Is it at least 24 HOURS in advance?" ❌ Wrong!
Result: May incorrectly reject the date even though there could be valid slots
```

### After Fix
```
User selects: "November 20, 2025"
System checks: "Is November 20th open? Not a holiday? Within max advance DAYS?" ✅ Correct!
Result: Shows all available time slots for that day
```

### Specific Improvements

**Scenario 1: Next-Day Booking with 24-Hour Minimum**
- **Before**: User selects tomorrow (Nov 18) at 5 PM on Nov 17 → Gets "must book 24 hours in advance" error even though there are 9 AM slots available tomorrow
- **After**: User selects tomorrow → Sees all available slots, only invalid if trying to book a slot less than 24 hours away

**Scenario 2: Checking Availability for Future Date**
- **Before**: User selects Dec 1st on Nov 1st → System checks if current time + duration fits in business hours (doesn't make sense)
- **After**: User selects Dec 1st → System checks if Dec 1st is open, not a holiday, etc. (makes sense)

**Scenario 3: API Response**
- **Before**: `GET /api/v1/availability/slots?date=2025-11-20` returns empty with validation message "Booking must be made at least 24 hours in advance" even at 11 PM the day before
- **After**: Returns actual time slots that ARE valid (like 2 PM the next day which is 15 hours away)

---

## Files Modified

| File | Lines | Change |
|------|-------|--------|
| [IAvailabilityService.cs](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/DomainServices/IAvailabilityService.cs#L79) | 79-92 | Added ValidateDateConstraintsAsync method signature |
| [AvailabilityService.cs](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Services/AvailabilityService.cs#L326) | 326-400 | Implemented ValidateDateConstraintsAsync method |
| [AvailabilityService.cs](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Services/AvailabilityService.cs#L58) | 58-65 | Updated GetAvailableTimeSlotsAsync to use date validation |
| [GetAvailableSlotsQueryHandler.cs](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Queries/Booking/GetAvailableSlots/GetAvailableSlotsQueryHandler.cs#L63) | 63-68 | Updated to use ValidateDateConstraintsAsync |

---

## Testing

### Build Status
✅ Domain project builds successfully
✅ Application project builds successfully
✅ No compilation errors
✅ All interfaces implemented correctly

### Test Scenarios

1. **Select date from calendar**
   - Should NOT check hours-in-advance
   - Should only validate date-level constraints

2. **Create booking with specific time**
   - Should check hours-in-advance
   - Should validate time within business hours

3. **Get available dates endpoint**
   - Should use date-level validation
   - Should show dates that have ANY valid time slots

---

## Related Documentation

- [AVAILABILITY_VALIDATION_FIX.md](AVAILABILITY_VALIDATION_FIX.md) - Previous fix for exposing validation messages
- [BOOKING_DATE_FIX_SUMMARY.md](BOOKING_DATE_FIX_SUMMARY.md) - Fix for date selection payload issue

---

*Fixed by: Claude Code*
*Date: November 17, 2025*
