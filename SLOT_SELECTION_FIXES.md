# Slot Selection UI Fixes

**Date**: November 17, 2025
**Issue**: Slots not displaying + Date extraction error + Wrong locale
**Status**: ✅ FIXED

---

## Problems Fixed

### 1. **Date Extraction Error**
**Error**: `TypeError: dateValue._i.split is not a function`

**Root Cause**: The Persian datetime picker's `_i` property can be either a string OR an object with `{year, month, day}` properties, but the code assumed it was always a string.

**Fix**: Added type checking before attempting to split:
```typescript
if (dateValue._i) {
  // Check if _i is a string before splitting
  if (typeof dateValue._i === 'string') {
    dateString = dateValue._i.split('T')[0]
  } else if (typeof dateValue._i === 'object' && dateValue._i.year) {
    // _i might be an object with year, month, day properties
    const year = dateValue._i.year
    const month = String(dateValue._i.month + 1).padStart(2, '0')
    const day = String(dateValue._i.date || dateValue._i.day).padStart(2, '0')
    dateString = `${year}-${month}-${day}`
  }
}
```

### 2. **Wrong Locale for Time Display**
**Problem**: Times were displaying in English format using `'en-US'` locale instead of Persian/Farsi.

**Fix**: Changed locale from `'en-US'` to `'fa-IR'`:
```typescript
// Before
startTime: new Date(slot.startTime).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false })

// After
startTime: new Date(slot.startTime).toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit', hour12: false })
```

### 3. **API Response Property Mismatch**
**Problem**: Frontend was looking for old property names:
- ❌ `slot.available` (old)
- ❌ `slot.staffMemberId` (old)
- ❌ `slot.staffName` (old)

But API now returns:
- ✅ `slot.isAvailable` (new)
- ✅ `slot.availableStaffId` (new)
- ✅ `slot.availableStaffName` (new)

**Fix**: Updated property names in both TypeScript interfaces and component code.

---

## Files Changed

### 1. Frontend - SlotSelection.vue
**File**: [SlotSelection.vue:163-176](booksy-frontend/src/modules/booking/components/SlotSelection.vue#L163-L176)

**Date Extraction Fix**:
```typescript
// Try moment object format (_i property)
if (dateValue._i) {
  // Check if _i is a string before splitting
  if (typeof dateValue._i === 'string') {
    dateString = dateValue._i.split('T')[0]
    console.log('[SlotSelection] Extracted from moment._i (string):', dateString)
  } else if (typeof dateValue._i === 'object' && dateValue._i.year) {
    // _i might be an object with year, month, day properties
    const year = dateValue._i.year
    const month = String(dateValue._i.month + 1).padStart(2, '0') // month is 0-indexed
    const day = String(dateValue._i.date || dateValue._i.day).padStart(2, '0')
    dateString = `${year}-${month}-${day}`
    console.log('[SlotSelection] Extracted from moment._i (object):', dateString)
  }
}
```

**Locale & Property Fix** (lines 227-235):
```typescript
// Filter for available slots and map to our interface
availableSlots.value = response.slots
  .filter(slot => slot.isAvailable)  // ✅ Changed from .available
  .map(slot => ({
    startTime: new Date(slot.startTime).toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit', hour12: false }),  // ✅ Changed to 'fa-IR'
    endTime: new Date(slot.endTime).toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit', hour12: false }),  // ✅ Changed to 'fa-IR'
    staffId: slot.availableStaffId || '',  // ✅ Changed from staffMemberId
    staffName: slot.availableStaffName || 'کارشناس',  // ✅ Changed from staffName
    available: true,
  }))
```

### 2. Frontend - availability.service.ts
**File**: [availability.service.ts:17-29](booksy-frontend/src/modules/booking/api/availability.service.ts#L17-L29)

**Updated TimeSlot Interface**:
```typescript
export interface TimeSlot {
  startTime: string // ISO 8601 format
  endTime: string
  durationMinutes: number  // ✅ Added
  isAvailable: boolean  // ✅ Added (new primary field)
  availableStaffId?: string  // ✅ Added
  availableStaffName?: string  // ✅ Added
  // Legacy fields for backward compatibility
  available?: boolean
  staffMemberId?: string
  staffName?: string
  reason?: string // Why unavailable
}
```

**Updated GetSlotsResponse** (lines 85-92):
```typescript
export interface GetSlotsResponse {
  date: string
  providerId: string
  serviceId: string
  slots: TimeSlot[]
  validationMessages?: string[]  // ✅ Added
  timezone?: string  // ✅ Made optional
}
```

---

## Related Backend Changes

These frontend fixes work together with the backend changes made earlier:

1. **Date vs Time Validation** - `ValidateDateConstraintsAsync` for date-only checks
2. **Staff Qualification** - Services now have qualified staff assigned
3. **DTO Property Mapping** - Backend sets `IsAvailable`, `AvailableStaffId`, `AvailableStaffName`

---

## Testing

### Test Scenario 1: Date Selection
1. Open booking wizard
2. Select service
3. Click on a date in Persian calendar
4. **Expected**: ✅ Slots load without console errors
5. **Expected**: ✅ Times display in Persian numerals (۰۰:۱۰ format)

### Test Scenario 2: Available Slots Display
1. After selecting a date with available slots
2. **Expected**: ✅ Slots show with Persian time format
3. **Expected**: ✅ Staff names appear (Persian names like "نگار رحمانی")
4. **Expected**: ✅ "انتخاب زمان" button is enabled

### Test Scenario 3: No Available Slots
1. Select a date when provider is closed (like Friday)
2. **Expected**: ✅ "زمانی موجود نیست" message shows
3. **Expected**: ✅ Validation messages explain why (if any)

---

## Before vs After

### Before
```json
{
  "slots": [
    {
      "startTime": "2025-11-17T10:00:00",
      "endTime": "2025-11-17T10:45:00",
      "durationMinutes": 45,
      "isAvailable": false  // ❌ Always false
      // ❌ Missing: availableStaffId, availableStaffName
    }
  ]
}
```
**Result**: "زمانی موجود نیست" even though slots exist

### After
```json
{
  "slots": [
    {
      "startTime": "2025-11-17T10:00:00",
      "endTime": "2025-11-17T10:45:00",
      "durationMinutes": 45,
      "isAvailable": true,  // ✅ Correct value
      "availableStaffId": "0d479ad1-795e-46ba-9e5f-cd7a979dba5a",  // ✅ Present
      "availableStaffName": "نگار رحمانی"  // ✅ Present
    }
  ]
}
```
**Result**: ✅ Slots display correctly with Persian times and staff names

---

## Complete Fix Chain

1. ✅ **Database** - Assigned all staff to services with SQL script
2. ✅ **Backend Domain** - Separated date vs time validation
3. ✅ **Backend Application** - Set DTO properties correctly
4. ✅ **Backend API** - Return new response format
5. ✅ **Frontend Service** - Update TypeScript interfaces
6. ✅ **Frontend Component** - Fix date extraction + locale + property names

---

*Fixed by: Claude Code*
*Date: November 17, 2025*
