# Utility Services - Quick Reference Guide

**For developers working with formatting utilities**

---

## 🚀 Quick Start

### Import All Common Utilities
```typescript
import {
  // Price
  formatPriceDisplay, parsePriceInput, priceToNumber,
  // String
  getNameInitials, capitalize, titleCase,
  // Phone
  formatPhone, isValidIranianMobile, isValidPhoneNumber,
  // Date
  formatDate, formatDateTime, formatTime
} from '@/core/utils'
```

### Or Import by Service
```typescript
// Single service import
import { formatDate, formatDateTime } from '@/core/utils'
```

---

## 💰 Price Utilities

**File:** `src/core/utils/price.service.ts`

| Function | Input | Output | Example |
|----------|-------|--------|---------|
| `formatPriceDisplay` | `'500000'` | `'500,000'` | Display with commas |
| `parsePriceInput` | `'500,000'` | `'500000'` | Parse user input |
| `priceToNumber` | `'500,000'` | `500000` | Convert to number |

---

## 📝 String Utilities

**File:** `src/core/utils/string.service.ts`

| Function | Input | Output | Use Case |
|----------|-------|--------|----------|
| `getNameInitials` | `'John Doe'` | `'JD'` | Avatar initials |
| `capitalize` | `'hello'` | `'Hello'` | First letter uppercase |
| `titleCase` | `'hello world'` | `'Hello World'` | Title casing |
| `truncate` | `'long text...'` | `'long te...'` | Truncate with ellipsis |
| `isEmpty` | `'  '` | `true` | Check if empty |
| `replaceAll` | Search & replace | Replaced string | Replace all occurrences |

---

## ☎️ Phone Utilities

**File:** `src/core/utils/phone.service.ts`

### Formatting
```typescript
formatPhone('09121234567')           // "09121234567"
formatPhoneDisplay('09121234567')    // "0912 123 4567"
toInternationalFormat('09121234567') // "+989121234567"
toLocalFormat('+989121234567')       // "09121234567"
```

### Validation
```typescript
isValidIranianMobile('09121234567')   // true
isValidIranianLandline('02112345678') // true
isValidPhoneNumber('09121234567')     // true
```

### Other
```typescript
maskPhoneNumber('09121234567', 4)     // "0912****4567"
getIranianPhoneType('09121234567')    // "mobile" | "landline" | "unknown"
```

---

## 📅 Date Utilities

**File:** `src/core/utils/date.service.ts`

### Basic Formatting
```typescript
formatDate('2024-01-15')                    // "2024/01/15"
formatDateTime('2024-01-15T14:30:00')      // "2024/01/15 14:30"
formatTime('14:30')                         // "14:30" (fa) or "2:30 PM" (en)
formatFullDate('2024-01-15T14:30:00')      // "Monday, January 15, 2024 at 2:30 PM"
formatRelativeTime('2024-01-15')            // "5 days ago"
```

### Locale Support
```typescript
// Persian formatting (default)
formatDate(date, 'fa')  // "۱۴۰۳/۱۰/۲۵"

// English formatting
formatDate(date, 'en')  // "1403/10/25" or formatted English date
```

### Parsing & Conversion
```typescript
parseDate('2024-01-15')        // Date object
toISODate('2024-01-15')        // "2024-01-15"
daysBetween(date1, date2)      // Number of days
```

### Date Comparisons
```typescript
isToday(date)                  // true/false
isPast('2020-01-01')          // true
isFuture('2030-12-31')        // true
```

---

## 📋 Common Patterns

### Pattern 1: Format and Display Date
```vue
<template>
  <span>{{ formatDate(booking.date) }}</span>
</template>

<script setup>
import { formatDate } from '@/core/utils'
</script>
```

### Pattern 2: Validate Phone Input
```vue
<template>
  <input
    v-model="phone"
    @blur="validatePhone"
  />
  <span v-if="phoneError">{{ phoneError }}</span>
</template>

<script setup>
import { isValidPhoneNumber } from '@/core/utils'

const phoneError = ref('')

function validatePhone() {
  phoneError.value = isValidPhoneNumber(phone.value)
    ? ''
    : 'Invalid phone number'
}
</script>
```

### Pattern 3: Format Price with Separators
```vue
<template>
  <input
    :value="formatPriceDisplay(price)"
    @input="e => price = parsePriceInput(e.target.value)"
  />
</template>

<script setup>
import { formatPriceDisplay, parsePriceInput } from '@/core/utils'

const price = ref('')
</script>
```

### Pattern 4: Display User Initials in Avatar
```vue
<template>
  <div class="avatar">
    <span>{{ getNameInitials(user.fullName) }}</span>
  </div>
</template>

<script setup>
import { getNameInitials } from '@/core/utils'
</script>
```

---

## ✨ Best Practices

### ✅ DO
```typescript
// ✅ Import from central location
import { formatDate } from '@/core/utils'

// ✅ Use in templates
<span>{{ formatDate(date) }}</span>

// ✅ Use in computed/methods
const formattedDate = computed(() => formatDate(booking.date))

// ✅ Chain utilities
const formatted = formatDateTime(parseDate(dateString))
```

### ❌ DON'T
```typescript
// ❌ Don't duplicate functions
function formatDate(date) { /* ... */ }

// ❌ Don't create local helpers for common operations
const myFormatDate = (d) => new Intl.DateTimeFormat('fa-IR').format(d)

// ❌ Don't forget to import
<span>{{ formatDate(date) }}</span> <!-- This will be undefined -->
```

---

## 🔧 Troubleshooting

### Issue: TypeScript errors on import
**Solution:** Check import path is `@/core/utils` not `@/core/utils/date.service`
```typescript
// ✅ Correct
import { formatDate } from '@/core/utils'

// ❌ Wrong
import { formatDate } from '@/core/utils/date.service'
```

### Issue: Function not found
**Solution:** Ensure function is exported from index.ts
```typescript
// Check src/core/utils/index.ts has:
export * from './date.service'
```

### Issue: Locale not working
**Solution:** Most date functions accept locale parameter
```typescript
// Persian (default)
formatDate(date)       // or formatDate(date, 'fa')

// English
formatDate(date, 'en')
```

---

## 📞 Function Index

### By Use Case

**User Names & Initials:**
- `getNameInitials()` - Extract initials

**Phone Numbers:**
- `formatPhone()` - Basic formatting
- `formatPhoneDisplay()` - Pretty formatting
- `isValidIranianMobile()` - Validate mobile
- `isValidPhoneNumber()` - Generic validation
- `maskPhoneNumber()` - Hide middle digits
- `toInternationalFormat()` - Convert to +98
- `toLocalFormat()` - Convert to 09...

**Dates & Times:**
- `formatDate()` - Date only
- `formatDateTime()` - Date + time
- `formatTime()` - Time only
- `formatFullDate()` - Full details
- `formatRelativeTime()` - Relative (ago/in)
- `isToday()`, `isPast()`, `isFuture()` - Comparisons
- `daysBetween()` - Duration

**Prices:**
- `formatPriceDisplay()` - Add separators
- `parsePriceInput()` - Remove separators
- `priceToNumber()` - Convert to number

**Text Manipulation:**
- `capitalize()` - First letter upper
- `titleCase()` - Each word upper
- `truncate()` - Shorten with ellipsis
- `isEmpty()`, `isNotEmpty()` - Check emptiness
- `replaceAll()` - Replace all occurrences

---

## 🎯 Decision Tree

**Need to format something?**

```
Is it a...
├─ Phone number? → isValid...(), formatPhone(), toLocalFormat()
├─ Date/Time? → formatDate(), formatDateTime(), formatTime()
├─ Price? → formatPriceDisplay(), parsePriceInput()
├─ Person's name? → getNameInitials()
├─ Text? → capitalize(), truncate(), isEmpty()
└─ Something else? → Check UTILITIES.md
```

---

## 📚 More Information

- **Full Documentation:** [UTILITIES.md](booksy-frontend/src/core/utils/UTILITIES.md)
- **Progress Tracking:** [REFACTORING_PROGRESS.md](REFACTORING_PROGRESS.md)
- **Source Files:** `src/core/utils/*.service.ts`

---

**Last Updated:** 2024-12-21
**Version:** 1.0
**Status:** ✅ Production Ready

