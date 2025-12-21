# Utility Services Documentation

This document describes the centralized utility services available in the application. All utilities are located in `/src/core/utils/` and can be imported from the index file.

## Table of Contents

- [Price Utilities](#price-utilities)
- [String Utilities](#string-utilities)
- [Phone Utilities](#phone-utilities)
- [Date Utilities](#date-utilities)

---

## Price Utilities

**File:** `price.service.ts`

Utilities for formatting and parsing prices with thousand separators.

### Functions

#### `formatPriceDisplay(price: string | number): string`
Formats a price with comma separators for display.

```typescript
import { formatPriceDisplay } from '@/core/utils'

formatPriceDisplay('500000') // "500,000"
formatPriceDisplay(500000)   // "500,000"
```

#### `parsePriceInput(value: string): string`
Parses price input and returns clean numeric string, removing commas.

```typescript
import { parsePriceInput } from '@/core/utils'

parsePriceInput('500,000') // "500000"
parsePriceInput('invalid')  // ""
```

#### `priceToNumber(price: string | number): number`
Converts price string to number.

```typescript
import { priceToNumber } from '@/core/utils'

priceToNumber('500,000') // 500000
priceToNumber('invalid')  // 0
```

---

## String Utilities

**File:** `string.service.ts`

Utilities for string manipulation and formatting.

### Functions

#### `getNameInitials(fullName: string): string`
Gets initials from a full name.

```typescript
import { getNameInitials } from '@/core/utils'

getNameInitials('John Doe')     // "JD"
getNameInitials('Jane Smith')   // "JS"
getNameInitials('Invalid')      // "IN"
```

#### `capitalize(text: string): string`
Capitalizes first letter of a string.

```typescript
import { capitalize } from '@/core/utils'

capitalize('hello world') // "Hello world"
```

#### `titleCase(text: string): string`
Capitalizes each word in a string.

```typescript
import { titleCase } from '@/core/utils'

titleCase('hello world') // "Hello World"
```

#### `camelCaseToHuman(camelCase: string): string`
Converts camelCase to human-readable format.

```typescript
import { camelCaseToHuman } from '@/core/utils'

camelCaseToHuman('businessName') // "Business Name"
```

#### `snakeCaseToHuman(snakeCase: string): string`
Converts snake_case to human-readable format.

```typescript
import { snakeCaseToHuman } from '@/core/utils'

snakeCaseToHuman('business_name') // "Business Name"
```

#### `truncate(text: string, maxLength?: number, suffix?: string): string`
Truncates text with ellipsis.

```typescript
import { truncate } from '@/core/utils'

truncate('This is a long text', 10)    // "This is..."
truncate('This is a long text', 10, '→') // "This is a→"
```

#### `isEmpty(text: string): boolean`
Checks if string is empty or whitespace only.

```typescript
import { isEmpty } from '@/core/utils'

isEmpty('')        // true
isEmpty('   ')     // true
isEmpty('hello')   // false
```

#### `isNotEmpty(text: string): boolean`
Checks if string contains text.

```typescript
import { isNotEmpty } from '@/core/utils'

isNotEmpty('hello') // true
isNotEmpty('')      // false
```

#### `replaceAll(text: string, search: string, replacement: string): string`
Replaces all occurrences of a substring.

```typescript
import { replaceAll } from '@/core/utils'

replaceAll('hello hello', 'hello', 'hi') // "hi hi"
```

**And more utility functions available in `string.service.ts`**

---

## Phone Utilities

**File:** `phone.service.ts`

Utilities for phone number formatting and validation.

### Functions

#### `formatPhone(phoneNumber?: string): string`
Formats phone number to standard format.

```typescript
import { formatPhone } from '@/core/utils'

formatPhone('09121234567')    // "09121234567"
formatPhone('+989121234567')  // "09121234567"
```

#### `formatPhoneDisplay(phoneNumber?: string, format?: 'dashes' | 'spaces'): string`
Formats phone number with dashes or spaces for display.

```typescript
import { formatPhoneDisplay } from '@/core/utils'

formatPhoneDisplay('09121234567', 'spaces')  // "0912 123 4567"
formatPhoneDisplay('09121234567', 'dashes')  // "0912-123-4567"
```

#### `isValidIranianMobile(phoneNumber: string): boolean`
Validates Iranian mobile phone number.

```typescript
import { isValidIranianMobile } from '@/core/utils'

isValidIranianMobile('09121234567')   // true
isValidIranianMobile('02112345678')   // false (landline)
```

#### `isValidIranianLandline(phoneNumber: string): boolean`
Validates Iranian landline phone number.

```typescript
import { isValidIranianLandline } from '@/core/utils'

isValidIranianLandline('02112345678')  // true
isValidIranianLandline('09121234567')  // false (mobile)
```

#### `isValidPhoneNumber(phoneNumber: string): boolean`
Validates any phone number format (10-15 digits).

```typescript
import { isValidPhoneNumber } from '@/core/utils'

isValidPhoneNumber('09121234567')      // true
isValidPhoneNumber('02112345678')      // true
isValidPhoneNumber('+989121234567')    // true
```

#### `toInternationalFormat(phoneNumber?: string): string`
Converts phone number to international format with + prefix.

```typescript
import { toInternationalFormat } from '@/core/utils'

toInternationalFormat('09121234567') // "+989121234567"
```

#### `toLocalFormat(phoneNumber?: string): string`
Converts international phone number to local format.

```typescript
import { toLocalFormat } from '@/core/utils'

toLocalFormat('+989121234567')  // "09121234567"
toLocalFormat('00989121234567') // "09121234567"
```

#### `maskPhoneNumber(phoneNumber?: string, showChars?: number): string`
Masks phone number for security display.

```typescript
import { maskPhoneNumber } from '@/core/utils'

maskPhoneNumber('09121234567', 4) // "0912****4567"
```

#### `getIranianPhoneType(phoneNumber: string): 'mobile' | 'landline' | 'unknown'`
Gets phone number type (mobile vs landline).

```typescript
import { getIranianPhoneType } from '@/core/utils'

getIranianPhoneType('09121234567')  // "mobile"
getIranianPhoneType('02112345678')  // "landline"
```

---

## Date Utilities

**File:** `date.service.ts`

Utilities for date and time formatting.

### Functions

#### `formatDate(date: Date | string, locale?: string): string`
Formats date to display format.

```typescript
import { formatDate } from '@/core/utils'

formatDate('2024-01-15')         // "2024/01/15" (fa)
formatDate(new Date(), 'en')     // "January 15, 2024"
```

#### `formatDateTime(dateTime: Date | string, locale?: string): string`
Formats date and time together.

```typescript
import { formatDateTime } from '@/core/utils'

formatDateTime('2024-01-15T14:30:00') // "2024/01/15 14:30" (fa)
```

#### `formatTime(date: Date | string, locale?: string): string`
Formats time only.

```typescript
import { formatTime } from '@/core/utils'

formatTime('14:30')              // "14:30" (fa)
formatTime(new Date(), 'en')     // "2:30 PM"
```

#### `formatFullDate(date: Date | string): string`
Formats date with full details (weekday, month, time, etc).

```typescript
import { formatFullDate } from '@/core/utils'

formatFullDate('2024-01-15T14:30:00')
// "Monday, January 15, 2024 at 2:30 PM"
```

#### `formatRelativeTime(date: Date | string): string`
Formats date relative to now.

```typescript
import { formatRelativeTime } from '@/core/utils'

formatRelativeTime('2024-01-15T14:30:00') // "2 hours ago"
```

#### `formatDayName(date: Date, locale?: string): string`
Gets day name (e.g., "Monday" or "دوشنبه").

```typescript
import { formatDayName } from '@/core/utils'

formatDayName(new Date(), 'fa')  // "دوشنبه"
formatDayName(new Date(), 'en')  // "Monday"
```

#### `formatMonthName(monthIndex: number, locale?: string): string`
Gets month name (e.g., "January" or "فروردین").

```typescript
import { formatMonthName } from '@/core/utils'

formatMonthName(0, 'fa')  // "فروردین"
formatMonthName(0, 'en')  // "January"
```

#### `toISODate(date: Date | string): string`
Formats date as ISO string (YYYY-MM-DD).

```typescript
import { toISODate } from '@/core/utils'

toISODate('2024-01-15T14:30:00') // "2024-01-15"
```

#### `parseDate(dateStr: string): Date | null`
Parses date string to Date object.

```typescript
import { parseDate } from '@/core/utils'

parseDate('2024-01-15')         // Date object
parseDate('2024-01-15T14:30')   // Date object
```

#### `isToday(date: Date | string): boolean`
Checks if date is today.

```typescript
import { isToday } from '@/core/utils'

isToday(new Date())  // true
```

#### `isPast(date: Date | string): boolean`
Checks if date is in the past.

```typescript
import { isPast } from '@/core/utils'

isPast('2020-01-01') // true
```

#### `isFuture(date: Date | string): boolean`
Checks if date is in the future.

```typescript
import { isFuture } from '@/core/utils'

isFuture('2030-12-31') // true
```

---

## Usage Examples

### Single Import
```typescript
import { formatPhone, isValidIranianMobile } from '@/core/utils'

const phone = '09121234567'
if (isValidIranianMobile(phone)) {
  console.log(formatPhoneDisplay(phone)) // 0912 123 4567
}
```

### Multiple Imports
```typescript
import { formatPrice, formatDate, getNameInitials } from '@/core/utils'

const price = formatPrice('500000')           // "500,000"
const date = formatDate('2024-01-15')        // "2024/01/15"
const initials = getNameInitials('John Doe') // "JD"
```

### In Vue Components
```vue
<script setup>
import { formatPhone, formatDate } from '@/core/utils'

const phone = '09121234567'
const date = '2024-01-15'
</script>

<template>
  <div>
    <span>{{ formatPhone(phone) }}</span>
    <span>{{ formatDate(date) }}</span>
  </div>
</template>
```

---

## Migration Guide

If you're updating existing components to use centralized utilities:

### Before (Duplicated Code)
```typescript
function formatDate(dateString: string | Date): string {
  if (typeof dateString === 'string') {
    const date = new Date(dateString)
    return `${date.getFullYear()}/${date.getMonth() + 1}/${date.getDate()}`
  }
  return `${dateString.getFullYear()}/${dateString.getMonth() + 1}/${dateString.getDate()}`
}
```

### After (Using Utilities)
```typescript
import { formatDate } from '@/core/utils'

// Use it directly, no need to define locally
```

---

## Best Practices

1. **Use centralized utilities** - Always use utilities from `@/core/utils` instead of duplicating formatting logic
2. **Handle null/undefined** - Most utilities handle null/undefined gracefully, but always check their documentation
3. **Type safety** - All utilities are fully typed with TypeScript
4. **Locale awareness** - Date/time functions support both English ('en') and Persian ('fa') locales
5. **Consistent formatting** - Using centralized utilities ensures consistent formatting across the app

---

## Adding New Utilities

To add new utility functions:

1. Create a new file in `/src/core/utils/` (e.g., `feature.service.ts`)
2. Export functions from the file
3. Add export statement to `index.ts`
4. Document the functions here

Example:
```typescript
// src/core/utils/feature.service.ts
export function myUtilityFunction() {
  // implementation
}

// Update src/core/utils/index.ts
export * from './feature.service'
```
