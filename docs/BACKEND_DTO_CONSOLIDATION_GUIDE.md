# Backend DTO Consolidation Guide

## Problem Identified

Multiple classes with the same or similar names across different layers:

### Current Duplication

| **Name** | **Locations** | **Purpose** |
|----------|---------------|-------------|
| `BusinessHours` | Domain/Entities, Domain/ValueObjects (commented), Application/DTOs, Application/ViewModels | Multiple representations of business hours |
| `TimeSlot` | Domain/ValueObjects, API/Requests, API/Responses | Different time slot concepts |
| `BusinessHoursDto` | Application/DTOs | Data transfer object |
| `BusinessHoursViewModel` | Application/Queries | View model for queries |
| `BusinessHoursRequest` | API/Models/Requests | API request model |
| `BusinessHoursResponse` | API/Models/Responses | API response model |
| `DayHoursRequest` | API/Models/Requests | Registration flow |
| `TimeSlotRequest` | API/Models/Requests | Registration flow |
| `TimeSlotResponse` | API/Models/Responses | Availability calendar |
| `BreakTimeRequest` | API/Models/Requests | Registration flow |

## Recommended Solution

### Strategy: Use Clear Naming Convention with Context

Instead of consolidating (which may break existing code), use **explicit contextual naming**:

```
[Layer][Context][Purpose][Type]
```

### Proposed Naming Convention

#### 1. **Domain Layer** (Entities & Value Objects)
Keep current names - these are domain concepts:
- ✅ `BusinessHours` (Entity) - Provider's operating hours entity
- ✅ `TimeSlot` (Value Object) - Booking time slot value object
- ✅ `BreakPeriod` (Value Object) - Break period within business hours

#### 2. **Application Layer** (DTOs & View Models)
Use explicit naming to show purpose:

**Current:**
- ❌ `BusinessHoursDto` - Generic, unclear purpose
- ❌ `BusinessHoursViewModel` - Which view?

**Recommended:**
- ✅ `ProviderBusinessHoursDto` - Provider business hours data transfer
- ✅ `BusinessHoursListViewModel` - For listing/calendar views
- ✅ `BusinessHoursDetailViewModel` - For detailed views with breaks

#### 3. **API Layer** (Requests & Responses)
Use action-based naming:

**Current:**
- ❌ `BusinessHoursRequest` - Which operation?
- ❌ `TimeSlotRequest` - Ambiguous

**Recommended:**
- ✅ `UpdateBusinessHoursRequest` - Update operation
- ✅ `CreateBusinessHoursRequest` - Create operation
- ✅ `BusinessHoursResponse` - Keep simple for GET responses
- ✅ `ProviderRegistrationDayHoursRequest` - Registration context
- ✅ `AvailabilityTimeSlotResponse` - Availability context

### Implementation Plan

## Phase 1: API Layer Refactoring

### Step 1.1: Rename Request Models

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Models/Requests/BusinessHoursRequest.cs`

```csharp
// BEFORE
public class BusinessHoursRequest
{
    public DayOfWeek DayOfWeek { get; set; }
    // ...
}

// AFTER
public class UpdateBusinessHoursRequest
{
    public DayOfWeek DayOfWeek { get; set; }
    // ...
}
```

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Models/Requests/UpdateWorkingHoursRequest.cs`

```csharp
// BEFORE
public sealed class DayHoursRequest { }
public sealed class TimeSlotRequest { }
public sealed class BreakTimeRequest { }

// AFTER
public sealed class ProviderRegistrationDayHoursRequest { }
public sealed class ProviderRegistrationTimeSlotRequest { }
public sealed class ProviderRegistrationBreakRequest { }
```

### Step 1.2: Rename Response Models

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Models/Responses/ProviderAvailabilityCalendarResponse.cs`

```csharp
// BEFORE
public class TimeSlotResponse { }

// AFTER
public class AvailabilityTimeSlotResponse { }
```

## Phase 2: Application Layer Refactoring

### Step 2.1: Rename DTOs

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/DTOs/Provider/BusinessHoursDto.cs`

```csharp
// BEFORE
public sealed class BusinessHoursDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsOpen { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
}

// AFTER
public sealed class ProviderBusinessHoursDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsOpen { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public List<BreakPeriodDto> Breaks { get; set; } = new();
}

public sealed class BreakPeriodDto
{
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
```

### Step 2.2: Rename View Models

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Queries/Provider/GetProviderById/BusinessHoursViewModel.cs`

```csharp
// BEFORE
public sealed class BusinessHoursViewModel { }

// AFTER
public sealed class ProviderBusinessHoursViewModel
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsOpen { get; set; }
    public int? OpenTimeHours { get; set; }
    public int? OpenTimeMinutes { get; set; }
    public int? CloseTimeHours { get; set; }
    public int? CloseTimeMinutes { get; set; }
    public List<BreakPeriodViewModel> Breaks { get; set; } = new();
}

public sealed class BreakPeriodViewModel
{
    public int StartTimeHours { get; set; }
    public int StartTimeMinutes { get; set; }
    public int EndTimeHours { get; set; }
    public int EndTimeMinutes { get; set; }
}
```

## Phase 3: Create Shared DTOs (Optional)

Create a shared DTOs project/folder for common models:

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Contracts/DTOs/TimeSlotDto.cs`

```csharp
namespace Booksy.ServiceCatalog.Contracts.DTOs;

/// <summary>
/// Represents a time slot (used across multiple contexts)
/// </summary>
public record TimeSlotDto(
    DateTime StartTime,
    DateTime EndTime,
    int DurationMinutes
);

/// <summary>
/// Represents business hours for a single day
/// </summary>
public record DayBusinessHoursDto(
    DayOfWeek DayOfWeek,
    bool IsOpen,
    TimeComponents? OpenTime,
    TimeComponents? CloseTime,
    List<BreakPeriodDto> Breaks
);

/// <summary>
/// Time represented as hours and minutes
/// </summary>
public record TimeComponents(
    int Hours,
    int Minutes
);

/// <summary>
/// Represents a break period within business hours
/// </summary>
public record BreakPeriodDto(
    TimeComponents StartTime,
    TimeComponents EndTime
);
```

## Migration Checklist

### API Layer
- [ ] Rename `BusinessHoursRequest` → `UpdateBusinessHoursRequest`
- [ ] Rename `DayHoursRequest` → `ProviderRegistrationDayHoursRequest`
- [ ] Rename `TimeSlotRequest` → `ProviderRegistrationTimeSlotRequest`
- [ ] Rename `BreakTimeRequest` → `ProviderRegistrationBreakRequest`
- [ ] Rename `TimeSlotResponse` → `AvailabilityTimeSlotResponse`
- [ ] Update controller actions to use new names
- [ ] Update Swagger documentation

### Application Layer
- [ ] Rename `BusinessHoursDto` → `ProviderBusinessHoursDto`
- [ ] Rename `BusinessHoursViewModel` → `ProviderBusinessHoursViewModel`
- [ ] Add `BreakPeriodDto` and `BreakPeriodViewModel`
- [ ] Update mapping profiles (AutoMapper/Mapster)
- [ ] Update query handlers
- [ ] Update command handlers

### Frontend Alignment
- [ ] Update TypeScript interfaces to match new backend names
- [ ] Update API service methods
- [ ] Update component imports

### Testing
- [ ] Update unit tests with new class names
- [ ] Update integration tests
- [ ] Verify API contracts haven't broken
- [ ] Test registration flow
- [ ] Test business hours CRUD operations

## Alternative: Use Namespaces

If renaming is too disruptive, use namespaces to differentiate:

```csharp
// Registration context
namespace Booksy.ServiceCatalog.Api.Models.Requests.Registration
{
    public class TimeSlotRequest { }
    public class BreakTimeRequest { }
}

// Availability context
namespace Booksy.ServiceCatalog.Api.Models.Responses.Availability
{
    public class TimeSlotResponse { }
}

// Provider context
namespace Booksy.ServiceCatalog.Application.DTOs.Provider
{
    public class BusinessHoursDto { }
}
```

## Benefits

✅ **Clear Intent** - Name shows purpose and context
✅ **Prevents Confusion** - No ambiguity about which class to use
✅ **Better IntelliSense** - IDE suggestions are more helpful
✅ **Easier Onboarding** - New developers understand the codebase faster
✅ **Maintainable** - Changes are isolated to specific contexts

## Recommendation

**Start with Phase 1** (API Layer) as it directly affects the frontend integration. This will:
1. Fix the immediate confusion with request/response models
2. Align backend names with frontend types
3. Make API contracts self-documenting

**Then Phase 2** (Application Layer) to improve internal architecture.

**Phase 3** is optional and only needed if you have significant code duplication across bounded contexts.
