# Business Hours Type Consolidation Guide

## Problem
Multiple files defined similar types with the same names (`TimeSlot`, `BreakTime`, `BusinessHours`), causing confusion and maintenance issues.

## Solution
Created centralized type definitions in `@/shared/types/business-hours.types.ts` with clear naming conventions.

## Type Naming Convention

### Time Representations
- **`TimeComponents`** - `{ hours: number, minutes: number }` - Used in registration flow
- **`TimeString`** - `"HH:mm"` format - Used for display and some APIs

### Break Periods
- **`BreakPeriodComponents`** - Uses `TimeComponents` (registration)
- **`BreakPeriodString`** - Uses `TimeString` (display/UI)
- **`BreakPeriodBackend`** - Separate hours/minutes fields (backend API)

### Business Hours
- **`DayHoursComponents`** - Uses `TimeComponents` (registration flow)
- **`DayHoursString`** - Uses `TimeString` (UI components)
- **`BusinessHoursBackend`** - Backend API format
- **`BusinessHoursEntity`** - Provider entity format

## Migration Guide

### Before (Multiple Definitions)
```typescript
// In registration.types.ts
export interface TimeSlot {
  hours: number
  minutes: number
}

// In provider.types.ts
export interface BusinessHours {
  openTime: string
  closeTime: string
}

// In hours.types.ts
export interface BreakPeriod {
  startTime: string
  endTime: string
}
```

### After (Centralized)
```typescript
import {
  TimeComponents,
  TimeString,
  BreakPeriodString,
  DayHoursString,
  BusinessHoursBackend
} from '@/shared/types/business-hours.types'

// Use the appropriate type for your context
const displayTime: TimeString = "10:30"
const componentTime: TimeComponents = { hours: 10, minutes: 30 }
```

## Usage Examples

### In Registration Components
```typescript
import { DayHoursComponents, TimeComponents } from '@/shared/types/business-hours.types'

const hours: DayHoursComponents = {
  dayOfWeek: 0,
  isOpen: true,
  openTime: { hours: 10, minutes: 0 },
  closeTime: { hours: 22, minutes: 0 },
  breaks: []
}
```

### In Display Components
```typescript
import { DayHoursString } from '@/shared/types/business-hours.types'

const hours: DayHoursString = {
  isOpen: true,
  startTime: "10:00",
  endTime: "22:00",
  breaks: []
}
```

### In API Services
```typescript
import { BusinessHoursBackend } from '@/shared/types/business-hours.types'

const apiData: BusinessHoursBackend = {
  dayOfWeek: 0,
  isOpen: true,
  openTimeHours: 10,
  openTimeMinutes: 0,
  closeTimeHours: 22,
  closeTimeMinutes: 0,
  breaks: []
}
```

## Utility Functions

The centralized file includes helper functions:

```typescript
import {
  timeComponentsToString,
  timeStringToComponents,
  backendTimeToString
} from '@/shared/types/business-hours.types'

// Convert between formats
const str = timeComponentsToString({ hours: 10, minutes: 30 }) // "10:30"
const comp = timeStringToComponents("10:30") // { hours: 10, minutes: 30 }
const backendStr = backendTimeToString(10, 30) // "10:30"
```

## Constants

Day mappings and names are also centralized:

```typescript
import {
  DayOfWeek,
  PERSIAN_WEEKDAYS,
  ENGLISH_WEEKDAYS,
  PERSIAN_TO_BACKEND_DAY_MAP
} from '@/shared/types/business-hours.types'

// Use enums instead of magic numbers
const monday = DayOfWeek.Monday // 0

// Get Persian day name
const persianDay = PERSIAN_WEEKDAYS[0] // "شنبه"

// Map Persian week index to backend enum
const backendDay = PERSIAN_TO_BACKEND_DAY_MAP[0] // DayOfWeek.Saturday (5)
```

## Migration Checklist

- [ ] Update `provider.types.ts` to re-export from shared types
- [ ] Update `registration.types.ts` to re-export from shared types
- [ ] Update `hours.types.ts` to re-export from shared types
- [ ] Update all components importing these types
- [ ] Update ProfileManager to use new types
- [ ] Update BusinessHoursStep to use new types
- [ ] Remove duplicate type definitions
- [ ] Run TypeScript compiler to check for errors

## Files to Update

1. `src/modules/provider/types/provider.types.ts`
2. `src/modules/provider/types/registration.types.ts`
3. `src/modules/provider/types/hours.types.ts`
4. `src/modules/provider/components/dashboard/ProfileManager.vue`
5. `src/modules/provider/components/registration/steps/BusinessHoursStep.vue`
6. `src/modules/provider/services/provider-registration.service.ts`
7. `src/modules/provider/services/provider-profile.service.ts`

## Benefits

✅ Single source of truth for business hours types
✅ Clear naming convention avoids confusion
✅ Utility functions prevent code duplication
✅ Constants centralized for easy updates
✅ Better TypeScript intellisense and error checking
✅ Easier to maintain and refactor
