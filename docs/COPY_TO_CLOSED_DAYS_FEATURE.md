# Copy to Closed Days Feature

## Overview
Enhanced the business hours copy functionality to allow copying schedules to closed days, automatically opening them with the copied schedule.

## Problem
Previously, when a provider wanted to copy business hours from one day to others, closed days were disabled in the copy modal. This meant providers had to:
1. Manually enable each closed day first
2. Then copy the schedule
3. This was a two-step process that was inconvenient

## Solution
Modified the `DayScheduleEditor` component to allow copying to closed days directly. When a user checks a closed day in the copy modal, it automatically becomes open with the copied schedule.

## Changes Made

### File: `booksy-frontend/src/shared/components/schedule/DayScheduleEditor.vue`

**1. Removed Disabled State for Closed Days**

**Before (line 97-103):**
```vue
<label
  v-for="(day, index) in weekDays"
  :key="index"
  :class="['day-checkbox', { disabled: index === copySourceIndex || !localSchedule[index].isOpen }]"
>
  <input
    type="checkbox"
    v-model="selectedDaysToCopy"
    :value="index"
    :disabled="index === copySourceIndex || !localSchedule[index].isOpen"
  />
```

**After:**
```vue
<label
  v-for="(day, index) in weekDays"
  :key="index"
  :class="['day-checkbox', { disabled: index === copySourceIndex }]"
>
  <input
    type="checkbox"
    v-model="selectedDaysToCopy"
    :value="index"
    :disabled="index === copySourceIndex"
  />
```

**2. Updated Pre-selection Logic**

**Before (line 231-238):**
```typescript
const copyToAll = (sourceIndex: number) => {
  copySourceIndex.value = sourceIndex
  copySourceDayName.value = props.weekDays[sourceIndex]
  // Pre-select all open days except the source day
  selectedDaysToCopy.value = props.weekDays
    .map((_, index) => index)
    .filter(index => index !== sourceIndex && localSchedule.value[index].isOpen)
  copyModalOpen.value = true
}
```

**After:**
```typescript
const copyToAll = (sourceIndex: number) => {
  copySourceIndex.value = sourceIndex
  copySourceDayName.value = props.weekDays[sourceIndex]
  // Pre-select all days except the source day (including closed days)
  selectedDaysToCopy.value = props.weekDays
    .map((_, index) => index)
    .filter(index => index !== sourceIndex)
  copyModalOpen.value = true
}
```

## Behavior

### Before
- Closed days were **disabled** in the copy modal
- Only open days could be selected
- Closed days showed as grayed out
- User had to manually open days first, then copy

### After
- **All days are selectable** (except the source day)
- Closed days show a **(تعطیل)** tag for clarity
- All days are **pre-selected by default**
- Checking a closed day **opens it** with the copied schedule
- Unchecking any day leaves it unchanged

## User Flow Example

1. User has Saturday with hours: 10:00 - 22:00 with 1 break
2. Friday is currently closed
3. User clicks "Copy" button on Saturday
4. Modal opens showing all days
5. Friday is pre-selected and shows "(تعطیل)" tag
6. User clicks "Apply Changes"
7. **Result:** Friday is now open with the same schedule as Saturday (10:00 - 22:00 with the same break)

## Use Cases

### Use Case 1: Setting Up Initial Schedule
Provider wants to set all weekdays to the same schedule:
1. Set Monday's hours and breaks
2. Click copy on Monday
3. Keep all weekdays checked (pre-selected)
4. Click apply
5. All weekdays now have the same schedule

### Use Case 2: Selective Copying
Provider wants to copy only to specific days:
1. Set Saturday's hours
2. Click copy on Saturday
3. Uncheck days that should remain different (e.g., keep Friday closed)
4. Click apply
5. Only selected days are updated

### Use Case 3: Converting Closed Day to Open
Provider decided to open on a previously closed day:
1. Click copy on any open day
2. Closed day is pre-selected
3. Click apply
4. Closed day becomes open with copied schedule

## Technical Notes

- The existing `handleCopyConfirm` function (line 247-261) already sets `isOpen: true` when copying, so no changes were needed there
- The copy operation creates deep copies of schedule data including breaks
- Visual indicator "(تعطیل)" remains visible for closed days to help users identify them
- Only the source day itself remains disabled (can't copy to itself)

## Benefits

1. **Faster Setup**: One-click operation to open and set hours for closed days
2. **Better UX**: More intuitive workflow
3. **Fewer Steps**: Reduced from 2-step to 1-step process
4. **Flexible**: Users can still choose which days to copy to
5. **Clear Feedback**: Visual tags show which days are currently closed

## Testing

To test this feature:

1. Navigate to Provider Dashboard → Hours Tab
2. Set hours for one day (e.g., Saturday)
3. Ensure at least one other day is closed
4. Click the "Copy" button on the day with hours
5. Verify:
   - Closed days are **not grayed out**
   - Closed days show **(تعطیل)** tag
   - All days (except source) are pre-checked
   - You can check/uncheck closed days
6. Click "Apply Changes"
7. Verify:
   - Checked closed days are now **open** with copied schedule
   - Unchecked days remain unchanged

## Related Files

- [DayScheduleEditor.vue](../booksy-frontend/src/shared/components/schedule/DayScheduleEditor.vue) - Main component with copy modal
- [ProfileManager.vue](../booksy-frontend/src/modules/provider/components/dashboard/ProfileManager.vue) - Uses DayScheduleEditor
- [WorkingHoursStepNew.vue](../booksy-frontend/src/modules/provider/components/registration/steps/WorkingHoursStepNew.vue) - Uses DayScheduleEditor in registration

## Previous Related Changes

- [BUSINESS_HOURS_FIX_SUMMARY.md](./BUSINESS_HOURS_FIX_SUMMARY.md) - Fixed business hours API type mismatch
