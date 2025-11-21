# Backend DTO Consolidation - Implementation Plan

## Executive Summary

**Problem**: Multiple DTOs with same/similar names causing confusion:
- `BusinessHoursDto`, `BusinessHoursRequest`, `BusinessHoursResponse`, `BusinessHoursViewModel`
- `TimeSlotRequest` vs `TimeSlotResponse` (different purposes)
- `ServiceDto`, `ServiceRequest`, `ServiceResponse` (different contexts)

**Strategy**:
1. **Remove** true duplicates (identical purpose/structure)
2. **Rename** context-specific DTOs with clear naming
3. **Consolidate** to single shared DTO where appropriate

---

## Phase 1: BusinessHours Consolidation

### Current State Analysis

| Class | Location | Fields | Usage |
|-------|----------|--------|-------|
| `BusinessHoursDto` | Application/DTOs/Provider | `DayOfWeek`, `IsOpen`, `TimeOnly? OpenTime/CloseTime` | Internal data transfer |
| `BusinessHoursViewModel` | Application/Queries/GetProviderById | Same as DTO | Query response |
| `BusinessHoursRequest` | API/Models/Requests | Same + `BreakStart/BreakEnd` | Simple update |
| `BusinessHoursResponse` | API/Models/Responses | Same + `BreakStart/BreakEnd` | Simple response |

### Decision: **REMOVE** Duplicates

**Keep**: `BusinessHoursDto` (Application layer) - Add breaks support
**Remove**: `BusinessHoursViewModel` (use `BusinessHoursDto` instead)
**Keep but Rename**:
- `BusinessHoursRequest` → **`UpdateProviderBusinessHoursRequest`** (more specific)
- `BusinessHoursResponse` → Use `BusinessHoursDto` directly

### Updated BusinessHoursDto

```csharp
// src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/DTOs/Provider/BusinessHoursDto.cs
namespace Booksy.ServiceCatalog.Application.DTOs.Provider
{
    /// <summary>
    /// Business hours for a single day of the week
    /// </summary>
    public sealed class BusinessHoursDto
    {
        public DayOfWeek DayOfWeek { get; set; }
        public bool IsOpen { get; set; }
        public TimeOnly? OpenTime { get; set; }
        public TimeOnly? CloseTime { get; set; }

        /// <summary>
        /// Break periods within the business hours
        /// </summary>
        public List<BreakPeriodDto> Breaks { get; set; } = new();
    }

    /// <summary>
    /// Break period within business hours
    /// </summary>
    public sealed class BreakPeriodDto
    {
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
}
```

### Migration Steps

1. **Add breaks to `BusinessHoursDto`**
2. **Remove `BusinessHoursViewModel.cs`** file
3. **Update `GetProviderByIdQueryHandler`** to use `BusinessHoursDto`
4. **Update `ProviderDetailsViewModel`** to use `BusinessHoursDto`
5. **Rename `BusinessHoursRequest`** → `UpdateProviderBusinessHoursRequest`
6. **Remove `BusinessHoursResponse.cs`** - use `BusinessHoursDto`
7. **Update `ProviderSettingsController`** to use new names
8. **Update mapping profiles**

---

## Phase 2: Registration Flow Types (Keep with Better Names)

### Current State

**In `UpdateWorkingHoursRequest.cs`**:
- `DayHoursRequest` - Day schedule with time slots
- `TimeSlotRequest` - `{Hours, Minutes}` format
- `BreakTimeRequest` - Uses `TimeSlotRequest`

### Decision: **RENAME** for Clarity

These are specific to the registration/update flow and have a different structure than other types.

**Rename**:
```csharp
// OLD → NEW
DayHoursRequest      → RegistrationDayScheduleRequest
TimeSlotRequest      → TimeComponentsRequest
BreakTimeRequest     → RegistrationBreakPeriodRequest
```

### Updated Code

```csharp
// src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Models/Requests/UpdateWorkingHoursRequest.cs

namespace Booksy.ServiceCatalog.Api.Models.Requests;

/// <summary>
/// Request to update provider working hours during registration
/// </summary>
public sealed class UpdateWorkingHoursRequest
{
    [Required]
    public Dictionary<string, RegistrationDayScheduleRequest?> BusinessHours { get; set; } = new();
}

/// <summary>
/// Day schedule in registration flow (uses hours/minutes components)
/// </summary>
public sealed class RegistrationDayScheduleRequest
{
    [Range(0, 6)]
    public int DayOfWeek { get; set; }

    public bool IsOpen { get; set; }

    public TimeComponentsRequest? OpenTime { get; set; }

    public TimeComponentsRequest? CloseTime { get; set; }

    public List<RegistrationBreakPeriodRequest> Breaks { get; set; } = new();
}

/// <summary>
/// Time represented as hours and minutes (for registration forms)
/// </summary>
public sealed class TimeComponentsRequest
{
    [Range(0, 23)]
    public int Hours { get; set; }

    [Range(0, 59)]
    public int Minutes { get; set; }
}

/// <summary>
/// Break period in registration flow
/// </summary>
public sealed class RegistrationBreakPeriodRequest
{
    [Required]
    public TimeComponentsRequest Start { get; set; } = new();

    [Required]
    public TimeComponentsRequest End { get; set; } = new();
}
```

---

## Phase 3: Availability Calendar Types (Keep - Different Purpose)

### Current State

**In `ProviderAvailabilityCalendarResponse.cs`**:
- `TimeSlotResponse` - Availability slot with booking info
- `DayAvailabilityResponse` - Day with available/booked slots

### Decision: **KEEP** (Different Purpose)

These are NOT duplicates - they represent available booking slots, not business hours configuration.

**Optional Rename** for extra clarity:
```csharp
TimeSlotResponse → AvailabilitySlotResponse
```

But current name is acceptable since it's in `ProviderAvailabilityCalendarResponse` context.

---

## Phase 4: Service Types Consolidation

### Current State

| Class | Location | Purpose |
|-------|----------|---------|
| `ServiceDto` | Application/DTOs/Service | Full service data |
| `ServiceRequest` | API/Requests/RegisterProviderFullRequest | Registration form |
| `ServiceResponse` | API/Responses | Command result (status/message) |

### Decision: **Different Purposes - Rename for Clarity**

**Keep**:
- `ServiceDto` ✅ (Application layer)
- `ServiceResponse` ✅ (It's a command response, not service data)

**Rename**:
```csharp
ServiceRequest → RegistrationServiceRequest
```

### Updated Code

```csharp
// src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Models/Requests/RegisterProviderFullRequest.cs

/// <summary>
/// Service offering in registration flow
/// </summary>
public sealed class RegistrationServiceRequest
{
    [Required(ErrorMessage = "Service name is required")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int DurationHours { get; set; }

    [Range(0, 59)]
    public int DurationMinutes { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [RegularExpression("^(fixed|variable)$")]
    public string PriceType { get; set; } = "fixed";
}
```

Update `RegisterProviderFullRequest`:
```csharp
public List<RegistrationServiceRequest> Services { get; set; } = new();
```

---

## Implementation Checklist

### ✅ Phase 1: BusinessHours
- [ ] Add `BreakPeriodDto` to `BusinessHoursDto.cs`
- [ ] Add `Breaks` property to `BusinessHoursDto`
- [ ] Delete `BusinessHoursViewModel.cs`
- [ ] Update `GetProviderByIdQueryHandler.cs` - use `BusinessHoursDto`
- [ ] Update `ProviderDetailsViewModel.cs` - use `BusinessHoursDto`
- [ ] Rename `BusinessHoursRequest.cs` → file content to `UpdateProviderBusinessHoursRequest`
- [ ] Delete `BusinessHoursResponse.cs` - use `BusinessHoursDto`
- [ ] Update `ProviderSettingsController.cs` - use new names
- [ ] Update `ProviderMappingProfile.cs`

### ✅ Phase 2: Registration Flow
- [ ] Rename `DayHoursRequest` → `RegistrationDayScheduleRequest` in `UpdateWorkingHoursRequest.cs`
- [ ] Rename `TimeSlotRequest` → `TimeComponentsRequest`
- [ ] Rename `BreakTimeRequest` → `RegistrationBreakPeriodRequest`
- [ ] Update `UpdateWorkingHoursRequest` to use new names
- [ ] Update `RegisterProviderFullRequest` to use new names
- [ ] Update all controllers using these types

### ✅ Phase 3: Service Types
- [ ] Rename `ServiceRequest` → `RegistrationServiceRequest` in `RegisterProviderFullRequest.cs`
- [ ] Update `RegisterProviderFullRequest.Services` property type
- [ ] Update registration command handlers

### ✅ Phase 4: Testing
- [ ] Compile solution
- [ ] Run unit tests
- [ ] Test registration flow
- [ ] Test business hours update
- [ ] Test provider queries
- [ ] Update API documentation

---

## Summary of Changes

### Files to DELETE:
1. `BusinessHoursViewModel.cs`
2. `BusinessHoursResponse.cs`

### Files to RENAME (content):
1. `BusinessHoursRequest` → `UpdateProviderBusinessHoursRequest`
2. `DayHoursRequest` → `RegistrationDayScheduleRequest`
3. `TimeSlotRequest` → `TimeComponentsRequest`
4. `BreakTimeRequest` → `RegistrationBreakPeriodRequest`
5. `ServiceRequest` → `RegistrationServiceRequest`

### Files to UPDATE (add breaks):
1. `BusinessHoursDto.cs` - add `BreakPeriodDto` and `Breaks` property

### Files to REFACTOR (use new types):
1. All query handlers
2. All command handlers
3. All controllers
4. All mapping profiles

---

## Benefits

✅ **Reduced Confusion** - Clear, context-specific names
✅ **Single Source of Truth** - `BusinessHoursDto` used across layers
✅ **Better Maintainability** - Less duplication
✅ **Self-Documenting** - Names indicate purpose
✅ **Easier Onboarding** - New developers understand the structure

---

## Risk Mitigation

⚠️ **Breaking Changes**: API contracts will change
**Mitigation**: Version the API endpoints or provide backward compatibility

⚠️ **Frontend Impact**: TypeScript types may need updates
**Mitigation**: Update frontend types in parallel

⚠️ **Testing Coverage**: Ensure all affected tests are updated
**Mitigation**: Run full test suite after each phase
