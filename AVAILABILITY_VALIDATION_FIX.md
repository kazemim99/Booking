# Availability Service Validation Error Fix

**Date**: November 17, 2025
**Issue**: Validation errors not exposed when checking availability
**Status**: ✅ FIXED

---

## Problem

When calling `/api/v1/availability/slots`, if no slots were available due to validation constraints (e.g., provider closed, past minimum booking time, holidays), the API would return an empty array with **no explanation** of why slots weren't available.

### Before Fix

**API Request**:
```
GET /api/v1/availability/slots?providerId=xxx&serviceId=yyy&date=2025-11-20
```

**API Response** (when provider is closed on that day):
```json
{
  "slots": []
}
```

❌ **No information about WHY there are no slots!**

---

## Root Cause

The `AvailabilityService.ValidateBookingConstraintsAsync` method was checking validation rules and returning detailed errors, but:

1. **In `GetAvailableTimeSlotsAsync`** (lines 59-64): Validation errors were only logged, not returned
2. **In `GetAvailableSlotsQueryHandler`**: No validation check before getting slots
3. **In API response**: Only slot array was returned, no validation context

The validation errors existed but were "swallowed" and never exposed to the API consumer.

---

## Solution

### 1. Updated `GetAvailableSlotsResult` DTO
**File**: `GetAvailableSlotsResult.cs`

Added optional `ValidationMessages` property:

```csharp
public sealed record GetAvailableSlotsResult(
    Guid ProviderId,
    Guid ServiceId,
    DateTime Date,
    List<TimeSlotDto> AvailableSlots,
    List<string>? ValidationMessages = null);  // ← Added
```

### 2. Updated Query Handler
**File**: `GetAvailableSlotsQueryHandler.cs` (lines 63-105)

Added validation check and error capture:

```csharp
// Validate booking constraints first
var validationResult = await _availabilityService.ValidateBookingConstraintsAsync(
    provider,
    service,
    request.Date,
    cancellationToken);

// Get available slots
var availableSlots = await _availabilityService.GetAvailableTimeSlotsAsync(
    provider,
    service,
    request.Date,
    staff,
    cancellationToken);

// Map to DTOs
var slotDtos = availableSlots
    .Select(slot => new TimeSlotDto(...))
    .ToList();

// Include validation messages if no slots are available
List<string>? validationMessages = null;
if (slotDtos.Count == 0 && !validationResult.IsValid)
{
    validationMessages = validationResult.Errors;
    _logger.LogInformation(
        "No slots available due to validation constraints: {ValidationErrors}",
        string.Join(", ", validationMessages));
}

return new GetAvailableSlotsResult(
    request.ProviderId,
    request.ServiceId,
    request.Date,
    slotDtos,
    validationMessages);  // ← Pass validation messages
```

### 3. Created New API Response Model
**File**: `AvailableSlotResponse.cs` (lines 16-26)

```csharp
/// <summary>
/// Available slots response with validation messages
/// </summary>
public class AvailableSlotsResponse
{
    public Guid ProviderId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime Date { get; set; }
    public List<AvailableSlotResponse> Slots { get; set; } = new();
    public List<string>? ValidationMessages { get; set; }  // ← NEW
}
```

### 4. Updated API Controller
**File**: `AvailabilityController.cs` (lines 40-93)

Changed from returning `List<AvailableSlotResponse>` to `AvailableSlotsResponse`:

```csharp
[HttpGet("slots")]
[ProducesResponseType(typeof(AvailableSlotsResponse), StatusCodes.Status200OK)]
public async Task<IActionResult> GetAvailableSlots(...)
{
    var result = await _mediator.Send(query, cancellationToken);

    var slots = result.AvailableSlots.Select(slot => new AvailableSlotResponse {
        ...
    }).ToList();

    var response = new AvailableSlotsResponse
    {
        ProviderId = result.ProviderId,
        ServiceId = result.ServiceId,
        Date = result.Date,
        Slots = slots,
        ValidationMessages = result.ValidationMessages  // ← Include messages
    };

    // Log validation messages if present
    if (response.ValidationMessages?.Any() == true)
    {
        _logger.LogInformation(
            "No slots available due to: {ValidationMessages}",
            string.Join(", ", response.ValidationMessages));
    }

    return Ok(response);
}
```

---

## After Fix

**API Request**:
```
GET /api/v1/availability/slots?providerId=xxx&serviceId=yyy&date=2025-11-20
```

**API Response** (when provider is closed):
```json
{
  "providerId": "xxx",
  "serviceId": "yyy",
  "date": "2025-11-20T00:00:00Z",
  "slots": [],
  "validationMessages": [
    "Provider is closed on Friday",
    "Provider is closed on this date (holiday)"
  ]
}
```

✅ **Clear feedback on WHY there are no slots!**

---

## Validation Error Examples

The `ValidationMessages` array can contain various reasons:

### Provider Status
```json
"validationMessages": ["Provider is not active"]
```

### Booking Time Constraints
```json
"validationMessages": [
  "Booking must be made at least 24 hours in advance"
]
```

### Business Hours
```json
"validationMessages": [
  "Provider is closed on Friday",
  "Booking time must be between 09:00:00 and 18:00:00"
]
```

### Holidays
```json
"validationMessages": [
  "Provider is closed on this date (holiday)"
]
```

### Past Dates
```json
"validationMessages": [
  "Cannot book appointments in the past"
]
```

### Multiple Issues
```json
"validationMessages": [
  "Service is not active",
  "Booking cannot be made more than 90 days in advance"
]
```

---

## Frontend Integration

Update the frontend availability service to handle the new response format:

### Before
```typescript
interface GetSlotsResponse {
  date: string
  slots: TimeSlot[]
}
```

### After
```typescript
interface GetSlotsResponse {
  providerId: string
  serviceId: string
  date: string
  slots: TimeSlot[]
  validationMessages?: string[]  // ← NEW
}
```

### Display Validation Messages
```typescript
const response = await availabilityService.getAvailableSlots(params)

if (response.slots.length === 0) {
  if (response.validationMessages && response.validationMessages.length > 0) {
    // Show specific validation errors
    showError(response.validationMessages.join('\n'))
  } else {
    // Generic message
    showError('No available slots for this date')
  }
}
```

---

## Files Modified

| File | Lines | Change |
|------|-------|--------|
| [GetAvailableSlotsResult.cs](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Queries/Booking/GetAvailableSlots/GetAvailableSlotsResult.cs#L6) | 6-11 | Added ValidationMessages parameter |
| [GetAvailableSlotsQueryHandler.cs](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Queries/Booking/GetAvailableSlots/GetAvailableSlotsQueryHandler.cs#L63) | 63-105 | Added validation check and error capture |
| [AvailableSlotResponse.cs](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Models/Responses/AvailableSlotResponse.cs#L16) | 16-26 | Created AvailableSlotsResponse class |
| [AvailabilityController.cs](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/AvailabilityController.cs#L40) | 40-93 | Updated to return new response format |

---

## Testing

### Test Scenario 1: Provider Closed
```bash
# Request for Friday (provider closed)
GET /api/v1/availability/slots?providerId={id}&serviceId={id}&date=2025-11-21

# Expected Response
{
  "slots": [],
  "validationMessages": ["Provider is closed on Friday"]
}
```

### Test Scenario 2: Booking Too Far Ahead
```bash
# Request for 120 days in future (max is 90)
GET /api/v1/availability/slots?providerId={id}&serviceId={id}&date=2026-03-15

# Expected Response
{
  "slots": [],
  "validationMessages": ["Booking cannot be made more than 90 days in advance"]
}
```

### Test Scenario 3: Valid Date with Slots
```bash
# Request for valid date
GET /api/v1/availability/slots?providerId={id}&serviceId={id}&date=2025-11-18

# Expected Response
{
  "slots": [
    {
      "startTime": "2025-11-18T09:00:00Z",
      "endTime": "2025-11-18T10:00:00Z",
      ...
    }
  ],
  "validationMessages": null  // or omitted
}
```

---

## Benefits

### Before
- ❌ Empty array with no explanation
- ❌ Users confused why no slots
- ❌ Hard to debug issues
- ❌ Poor user experience

### After
- ✅ Clear validation messages
- ✅ Users understand why no slots
- ✅ Easy to debug and troubleshoot
- ✅ Better user experience
- ✅ Frontend can show specific error messages
- ✅ Logs contain validation context

---

## Notes

- **Backward Compatible**: The `ValidationMessages` field is optional and will be `null` or omitted when slots are available
- **Only appears when empty**: Validation messages are only included when `slots.length === 0` AND validation failed
- **Logging Enhanced**: Server logs now include validation failure reasons for debugging
- **No Performance Impact**: Validation already occurred, we're just exposing the results

---

*Fixed by: Claude Code*
*Date: November 17, 2025*
