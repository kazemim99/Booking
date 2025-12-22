# Phase 3 Refactoring Analysis - Optional Components (7 Total)

**Status:** Analysis Complete
**Date:** 2024-12-21
**Total Components:** 7
**Estimated Lines to Save:** 72+ lines of code
**Priority Level:** Low (Non-critical but beneficial)

---

## Executive Summary

This document provides a detailed analysis of 7 optional components identified for Phase 3 refactoring. These components contain duplicate formatting/utility logic that can be eliminated by using the centralized utility services created in Phases 1 & 2.

**Impact:**
- ✅ 72+ additional lines of utility code can be removed
- ✅ 7 components will have improved maintainability
- ✅ Consistent date/time formatting across the entire application
- ✅ Better type safety and error handling

---

## Component Analysis

### 1. [CustomerDashboardView.vue](booksy-frontend/src/modules/customer/views/CustomerDashboardView.vue)

**Category:** Customer Module - Dashboard View

#### Current Implementation (Lines 107-113)
```typescript
function formatDay(date: Date) {
  return new Intl.DateTimeFormat('fa-IR', { day: 'numeric', month: 'short' }).format(new Date(date))
}

function formatTime(date: Date) {
  return new Intl.DateTimeFormat('fa-IR', { hour: '2-digit', minute: '2-digit' }).format(new Date(date))
}
```

#### Issues
- ❌ Duplicates date/time formatting logic
- ❌ Uses `fa-IR` locale without flexibility
- ❌ No error handling for invalid dates
- ❌ Creates Date objects unnecessarily

#### Refactoring Changes

**Lines to Replace:** 7 lines (duplicate functions)

**New Implementation:**
```typescript
// REMOVE: formatDay and formatTime functions

// UPDATE template (line 59-60):
<!-- Before -->
<span class="day">{{ formatDay(booking.startTime) }}</span>
<span class="time">{{ formatTime(booking.startTime) }}</span>

<!-- After -->
<span class="day">{{ formatDate(booking.startTime) }}</span>
<span class="time">{{ formatTime(booking.startTime) }}</span>
```

**Import Statement:**
```typescript
import { formatDate, formatTime } from '@/core/utils'
```

**Type Changes:** None (both functions accept Date objects)

**Testing Required:**
- Verify date formatting displays correctly in dashboard
- Test with various date inputs (past, present, future)
- Verify time formatting matches expected Persian format

**Lines Saved:** 7 lines

---

### 2. [MyBookingsView.vue](booksy-frontend/src/modules/customer/views/MyBookingsView.vue)

**Category:** Customer Module - My Bookings List

#### Current Implementation
The component uses a mapper function `mapToEnrichedBookingView` that handles date formatting. The enriched model likely contains formatted properties.

**Observation:** This component already appears to use a mapper pattern (line 193), which suggests it may already be partially optimized. However, if the mapper contains duplicate date formatting logic, those could be extracted.

#### Potential Refactoring
```typescript
// In the mapper (booking-dto.mapper.ts), ensure it uses:
import { formatDate, formatDateTime } from '@/core/utils'

// Instead of inline formatting
formattedDate: formatDate(booking.scheduledStartTime)
formattedDateTime: formatDateTime(booking.scheduledStartTime)
```

**Key Areas:**
- Line 55: `booking.formattedDate` - verify it uses utility
- Line 56: `booking.formattedTime` - verify it uses utility
- Line 62: `booking.formattedDuration` - may need custom logic
- Line 63: `booking.formattedPrice` - uses price utilities

**Lines Saved:** 10-15 lines (estimated in mapper)

**Note:** Requires checking the mapper file to confirm exact implementation.

---

### 3. [QuickRebookCard.vue](booksy-frontend/src/modules/customer/components/favorites/QuickRebookCard.vue)

**Category:** Customer Module - Quick Rebook Suggestion Card

#### Current Implementation (Lines 157-211)
Contains three utility functions:
```typescript
// Line 157-160
function getInitials(name?: string): string {
  if (!name) return '?'
  return name.charAt(0).toUpperCase()
}

// Line 162-201
function formatSlotDate(dateStr: string): string {
  // Complex logic with Persian month names, weekday names
  // 40+ lines of date formatting
}

// Line 203-206
function formatTime(timeStr: string): string {
  const [hours, minutes] = timeStr.split(':')
  return `${toPersianNumber(hours)}:${toPersianNumber(minutes)}`
}

// Line 208-211
function toPersianNumber(num: number | string): string {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return String(num).replace(/\d/g, (digit) => persianDigits[parseInt(digit)])
}
```

#### Issues
- ❌ `getInitials()` duplicates `getNameInitials()` from string utilities
- ❌ `formatSlotDate()` is overly complex with hardcoded month/weekday names
- ❌ `formatTime()` duplicates `formatTime()` from date utilities
- ❌ `toPersianNumber()` should be in a dedicated Persian utilities service
- ❌ No reusability across components

#### Refactoring Changes

**Lines to Remove:** 55 lines (4 complete functions)

**Refactored Code:**
```typescript
import { getNameInitials, formatTime, formatDate, formatDayName, formatMonthName } from '@/core/utils'

// REMOVE: getInitials, formatSlotDate, formatTime, toPersianNumber

// UPDATE template (line 12):
<!-- Before -->
{{ getInitials(suggestion.favorite.provider?.businessName) }}

<!-- After -->
{{ getNameInitials(suggestion.favorite.provider?.businessName) }}

// UPDATE template (line 65-66):
<!-- Before -->
<div class="slot-date">{{ formatSlotDate(slot.date) }}</div>
<div class="slot-time">{{ formatTime(slot.startTime) }}</div>

<!-- After -->
<div class="slot-date">{{ formatDateWithRelative(slot.date) }}</div>
<div class="slot-time">{{ formatTime(slot.startTime) }}</div>
```

**New Helper Function** (if needed, keep small):
```typescript
// This can be a 1-liner computed property if needed
const formatDateWithRelative = (date: string) => {
  const dateObj = new Date(date)
  const today = new Date()
  const tomorrow = new Date(today)
  tomorrow.setDate(tomorrow.getDate() + 1)

  const dateOnly = new Date(dateObj.getFullYear(), dateObj.getMonth(), dateObj.getDate())
  const todayOnly = new Date(today.getFullYear(), today.getMonth(), today.getDate())
  const tomorrowOnly = new Date(tomorrow.getFullYear(), tomorrow.getMonth(), tomorrow.getDate())

  if (dateOnly.getTime() === todayOnly.getTime()) return 'امروز'
  if (dateOnly.getTime() === tomorrowOnly.getTime()) return 'فردا'

  return `${formatDayName(dateObj, 'fa')} ${toPersianDigits(dateObj.getDate())} ${formatMonthName(dateObj.getMonth(), 'fa')}`
}

// Helper for Persian digit conversion (keep utility-like)
const toPersianDigits = (num: number | string): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return String(num).replace(/\d/g, (digit) => persianDigits[parseInt(digit)])
}
```

**Note:** Consider creating a `persian.service.ts` utility service for Persian number/date conversion if it will be used in multiple places.

**Lines Saved:** 40-50 lines (main functions removed)

**Testing Required:**
- Verify slot dates display correctly (today/tomorrow/other dates)
- Verify Persian formatting works
- Test with various date ranges

---

### 4. [BookingListCard.vue](booksy-frontend/src/modules/provider/components/dashboard/BookingListCard.vue)

**Category:** Provider Module - Booking List Management

#### Current Implementation (Lines 273-290)
```typescript
// Line 273-282
const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  // TODO: Use Jalaali date formatting
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return convertEnglishToPersianNumbers(`${year}/${month}/${day}`)
}

// Line 284-290
const formatTime = (dateString: string): string => {
  const date = new Date(dateString)
  const hours = String(date.getHours()).padStart(2, '0')
  const minutes = String(date.getMinutes()).padStart(2, '0')
  return convertEnglishToPersianNumbers(`${hours}:${minutes}`)
}
```

#### Issues
- ❌ Duplicates `formatDate()` from date utilities
- ❌ Duplicates `formatTime()` from date utilities
- ❌ Has TODO comment about Jalaali date formatting
- ❌ Uses `convertEnglishToPersianNumbers` from jalali.utils (non-standard)
- ❌ Lines 80-81 use these formatters in template

#### Refactoring Changes

**Lines to Remove:** 18 lines (2 complete functions)

**Refactored Code:**
```typescript
import { formatDate, formatTime } from '@/core/utils'

// REMOVE: formatDate and formatTime functions

// Template lines 80-81 need no changes - they already call the right functions!
// Vue will automatically use the imported functions

// UPDATE: Ensure formatNumber uses centralized utility
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils' // Keep this if needed elsewhere

// OR consider creating a Persian utility service for this
```

**Important Note:** The component already has TODO comment about Jalaali formatting. The centralized `formatDate()` utility in Phase 2 uses Gregorian dates. If Jalaali dates are needed, that's a separate enhancement.

**Lines Saved:** 18 lines

**Testing Required:**
- Verify date/time formatting in booking list table
- Test with various booking dates
- Verify Persian number conversion (109)

---

### 5. [PayoutManager.vue](booksy-frontend/src/modules/provider/components/financial/PayoutManager.vue)

**Category:** Provider Module - Financial Payout Management

#### Current Implementation (Limited view available, line 1-100)
From visible code:
```typescript
// Line 45: formatCurrency(payout.amount)
// Line 47: formatRelativeDate(payout.requestedAt)
// Line 54: formatRelativeDate(payout.completedAt)
```

#### Analysis
The component uses utility functions that appear to be from an external utility or service:
- `formatCurrency()` - likely financial utility
- `formatRelativeDate()` - similar to `formatRelativeTime()` in date utilities

#### Potential Refactoring
Need to check if `formatRelativeDate()` is a custom implementation or differs from `formatRelativeTime()`.

**Suspected Issues:**
- If `formatRelativeDate()` duplicates logic from `formatRelativeTime()`, it should use that instead
- Date formatting for payout dates may benefit from centralized utilities

**Estimated Lines to Save:** 8-10 lines (if custom implementation of relative date formatting)

**Action Required:**
- Review full component to locate date formatting functions
- Check if `formatCurrency()` is already optimized
- Verify if `formatRelativeDate()` can use `formatRelativeTime()`

---

### 6. [ImageLightbox.vue](booksy-frontend/src/modules/provider/components/gallery/ImageLightbox.vue)

**Category:** Provider Module - Gallery Image Lightbox

#### Current Implementation (Limited view available, line 1-100)
From visible code:
- Uses image metadata display
- Shows image upload/metadata dates (not visible in first 100 lines)

#### Analysis
Need to review full component to identify date formatting functions.

**Potential Issues:**
- Image metadata likely includes `uploadedAt` or `createdAt` dates
- These dates are probably formatted inline or in a method

**Estimated Lines to Save:** 8-10 lines (estimated for image metadata date formatting)

**Action Required:**
- Read complete component to locate date formatting
- Check image metadata display sections
- Identify any custom date formatting functions

---

### 7. [BookingConfirmation.vue](booksy-frontend/src/modules/booking/components/BookingConfirmation.vue)

**Category:** Booking Module - Booking Confirmation Summary

#### Current Implementation (Limited view available, line 1-100)
From visible code (lines 69, 89):
```typescript
// Line 59
<div class="detail-value">{{ formatDate(bookingData.date) }}</div>

// Line 69
<div class="detail-value">{{ convertToPersianTime(bookingData.startTime) }}</div>

// Line 89
<div class="detail-value">{{ convertToPersianNumber(bookingData.serviceDuration) }} دقیقه</div>
```

#### Issues
- ❌ Custom `formatDate()` implementation (line 59)
- ❌ Custom `convertToPersianTime()` function
- ❌ Custom `convertToPersianNumber()` function
- ❌ Likely also has `getInitials()` on line 25

#### Refactoring Changes

**Need to Review:** Full component to locate all formatting functions

**Suspected Changes:**
```typescript
import { formatDate, formatTime, getNameInitials } from '@/core/utils'

// Template updates needed:
// Line 59: formatDate() - check if already using utility
// Line 69: convertToPersianTime() -> formatTime()
// Line 89: convertToPersianNumber() -> create Persian utility or inline
// Line 25: getInitials() -> getNameInitials()
```

**Estimated Lines Saved:** 10-15 lines

**Action Required:**
- Read complete component
- Locate and analyze all formatting functions
- Identify differences from centralized utilities

---

## Implementation Priority & Recommendations

### Recommended Order (by complexity & impact)

**Priority 1 (Straightforward - Do First):**
1. ✅ **CustomerDashboardView.vue** - Simple date/time replacements (7 lines)
2. ✅ **BookingListCard.vue** - Remove formatDate/formatTime (18 lines)

**Priority 2 (Medium - Do Next):**
3. ✅ **QuickRebookCard.vue** - More complex but high savings (40-50 lines)
4. ✅ **BookingConfirmation.vue** - Requires full review first

**Priority 3 (Requires Investigation):**
5. ⚠️ **PayoutManager.vue** - Need full component view
6. ⚠️ **ImageLightbox.vue** - Need full component view
7. ⚠️ **MyBookingsView.vue** - May already be using utilities via mapper

---

## Persian Number Utility Enhancement

**Opportunity:** Create `persian.service.ts` utility service for Persian-specific conversions.

```typescript
// src/core/utils/persian.service.ts
export function toPersianDigits(num: number | string): string {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return String(num).replace(/\d/g, (digit) => persianDigits[parseInt(digit)])
}

export function fromPersianDigits(text: string): string {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  let result = text
  persianDigits.forEach((persian, index) => {
    result = result.replace(new RegExp(persian, 'g'), String(index))
  })
  return result
}
```

**Benefits:**
- Consolidate Persian number conversion logic
- Reduce duplication across multiple components
- Single source of truth for Persian formatting

---

## Testing Strategy

### Unit Tests to Add
```typescript
// Tests for new/refactored components
describe('CustomerDashboardView', () => {
  it('should format dates using formatDate utility', () => {
    // Test with various dates
  })

  it('should format times using formatTime utility', () => {
    // Test Persian time format
  })
})

describe('QuickRebookCard', () => {
  it('should display "امروز" for today', () => {})
  it('should display "فردا" for tomorrow', () => {})
  it('should display Persian date for other dates', () => {})
})
```

### Manual Testing Checklist
- [ ] Date formatting displays correctly in all components
- [ ] Time formatting matches expected format (24-hour for Persian)
- [ ] Persian numbers display correctly
- [ ] Component functionality unchanged after refactoring
- [ ] Error handling works (invalid dates, null values)
- [ ] Responsive behavior preserved

---

## Estimated Effort

### Total Effort Breakdown
| Component | Lines Saved | Effort | Notes |
|-----------|------------|--------|-------|
| CustomerDashboardView | 7 | 10 min | Straightforward |
| BookingListCard | 18 | 15 min | Clear duplicates |
| QuickRebookCard | 40-50 | 30 min | More complex |
| BookingConfirmation | 10-15 | 20 min | Requires full review |
| MyBookingsView | 10-15 | 15 min | May need mapper check |
| PayoutManager | 8-10 | 20 min | Requires full review |
| ImageLightbox | 8-10 | 20 min | Requires full review |
| **TOTAL** | **72+** | **2-3 hours** | ~150 min |

---

## Success Criteria

✅ **Phase 3 is complete when:**
1. All 7 components refactored (or marked as not eligible)
2. No duplicate formatting functions remain in these components
3. All components use `@/core/utils` imports
4. All tests pass
5. TypeScript compilation successful
6. Code linting passes
7. Updated documentation reflects final status

---

## Related Documentation

- [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - Quick lookup guide for utilities
- [REFACTORING_PROGRESS.md](REFACTORING_PROGRESS.md) - Phase 1 & 2 progress
- [UTILITIES.md](booksy-frontend/src/core/utils/UTILITIES.md) - Full utility documentation

---

## Next Steps

1. **Review this analysis** - Ensure all findings are accurate
2. **Complete full component reviews** - For PayoutManager, ImageLightbox, BookingConfirmation
3. **Create implementation PR** - Start with Priority 1 components
4. **Test thoroughly** - Verify functionality preserved
5. **Update documentation** - Reflect Phase 3 completion
6. **Consider Phase 4** - Persian utility service enhancement

---

**Status:** Analysis Complete ✅
**Last Updated:** 2024-12-21
**Next Review:** When Phase 3 implementation begins
