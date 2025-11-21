# Business Hours Update API Fix Summary

## Issue
When updating business hours from the provider dashboard, the values were being sent as empty to the backend, causing the business hours to not save correctly.

## Root Cause
The frontend was sending business hours data in a **flat structure** (with separate `openTimeHours`, `openTimeMinutes` properties), but the backend `UpdateBusinessHoursCommand` expects a **nested structure** matching the `DayHoursDto` from the registration flow.

### Incorrect Format (Before Fix)
```typescript
{
  dayOfWeek: 6,
  isOpen: true,
  openTimeHours: 10,        // ❌ Flat structure
  openTimeMinutes: 0,
  closeTimeHours: 22,
  closeTimeMinutes: 0,
  breaks: [...]
}
```

### Correct Format (After Fix)
```typescript
{
  dayOfWeek: 6,
  isOpen: true,
  openTime: {               // ✅ Nested structure
    hours: 10,
    minutes: 0
  },
  closeTime: {
    hours: 22,
    minutes: 0
  },
  breaks: [
    {
      start: { hours: 12, minutes: 0 },
      end: { hours: 13, minutes: 0 }
    }
  ]
}
```

## Backend Structure

The backend uses the same `DayHoursDto` for both registration and updates:

**From: `SaveStep6WorkingHoursCommand.cs`**
```csharp
public sealed record DayHoursDto(
    int DayOfWeek,
    bool IsOpen,
    TimeSlotDto? OpenTime,    // Nested object
    TimeSlotDto? CloseTime,   // Nested object
    List<BreakTimeDto> Breaks
);

public sealed record TimeSlotDto(
    int Hours,
    int Minutes
);

public sealed record BreakTimeDto(
    TimeSlotDto Start,
    TimeSlotDto End
);
```

**Used by: `UpdateBusinessHoursCommand.cs`**
```csharp
public sealed record UpdateBusinessHoursCommand(
    Guid ProviderId,
    List<DayHoursDto> BusinessHours,  // Reuses DayHoursDto
    Guid? IdempotencyKey = null) : ICommand<UpdateBusinessHoursResult>;
```

## Changes Made

### 1. Fixed Frontend Type Definition
**File:** `booksy-frontend/src/modules/provider/services/provider-profile.service.ts`

Changed the `BusinessHoursRequest` interface to match the backend structure:

```typescript
export interface BusinessHoursRequest {
  dayOfWeek: number
  isOpen: boolean
  openTime?: {
    hours: number
    minutes: number
  } | null
  closeTime?: {
    hours: number
    minutes: number
  } | null
  breaks: {
    start: {
      hours: number
      minutes: number
    }
    end: {
      hours: number
      minutes: number
    }
  }[]
}
```

### 2. Fixed Frontend Data Transformation
**File:** `booksy-frontend/src/modules/provider/components/dashboard/ProfileManager.vue`

Updated the `saveHours()` function to send nested objects:

**Before:**
```typescript
return {
  dayOfWeek: day.backendDayOfWeek,
  isOpen: true,
  openTimeHours: openTime.hours,      // ❌ Flat
  openTimeMinutes: openTime.minutes,
  closeTimeHours: closeTime.hours,
  closeTimeMinutes: closeTime.minutes,
  breaks: [...]
}
```

**After:**
```typescript
return {
  dayOfWeek: day.backendDayOfWeek,
  isOpen: true,
  openTime: {                          // ✅ Nested
    hours: openTime.hours,
    minutes: openTime.minutes,
  },
  closeTime: {
    hours: closeTime.hours,
    minutes: closeTime.minutes,
  },
  breaks: hours.breaks ? hours.breaks.map((b: any) => ({
    start: {
      hours: startTime.hours,
      minutes: startTime.minutes,
    },
    end: {
      hours: endTime.hours,
      minutes: endTime.minutes,
    },
  })) : [],
}
```

For closed days:
```typescript
return {
  dayOfWeek: day.backendDayOfWeek,
  isOpen: false,
  openTime: null,    // ✅ Explicitly null
  closeTime: null,
  breaks: [],
}
```

## Backend Handler Verification

The backend handler in `UpdateBusinessHoursCommandHandler.cs` correctly expects and uses the nested structure:

```csharp
var openTime = new TimeOnly(dayDto.OpenTime.Hours, dayDto.OpenTime.Minutes);
var closeTime = new TimeOnly(dayDto.CloseTime.Hours, dayDto.CloseTime.Minutes);

var breaks = dayDto.Breaks?.Select(b =>
{
    var start = new TimeOnly(b.Start.Hours, b.Start.Minutes);
    var end = new TimeOnly(b.End.Hours, b.End.Minutes);
    return BreakPeriod.Create(start, end);
}).ToList() ?? new List<BreakPeriod>();
```

## API Endpoint

**Endpoint:** `PUT /api/v1/providers/{id}/business-hours`

**Request Body:**
```json
{
  "businessHours": [
    {
      "dayOfWeek": 6,
      "isOpen": true,
      "openTime": {
        "hours": 10,
        "minutes": 0
      },
      "closeTime": {
        "hours": 22,
        "minutes": 0
      },
      "breaks": [
        {
          "start": { "hours": 12, "minutes": 0 },
          "end": { "hours": 13, "minutes": 0 }
        }
      ]
    },
    {
      "dayOfWeek": 0,
      "isOpen": false,
      "openTime": null,
      "closeTime": null,
      "breaks": []
    }
  ]
}
```

## Testing

To test the fix:

1. Navigate to Provider Dashboard → Hours Tab
2. Set working hours for multiple days with breaks
3. Save the changes
4. Verify:
   - Success message appears
   - Hours are persisted (refresh and check)
   - Backend logs show correct data structure
   - Database reflects the correct hours

## References

- Backend Command: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/UpdateBusinessHours/`
- Registration Command: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/Registration/SaveStep6WorkingHoursCommand.cs`
- Frontend Service: `booksy-frontend/src/modules/provider/services/provider-profile.service.ts`
- Frontend Component: `booksy-frontend/src/modules/provider/components/dashboard/ProfileManager.vue`
- API Controller: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/ProviderSettingsController.cs`

## Notes

- The `UpdateBusinessHoursCommand` correctly reuses `DayHoursDto` from the registration namespace to maintain consistency
- Both registration flow (Step 6) and profile update now use the exact same data structure
- Closed days must still be included in the request with `isOpen: false` and `openTime/closeTime` set to `null`
- Breaks are optional but must be an empty array if no breaks are defined
