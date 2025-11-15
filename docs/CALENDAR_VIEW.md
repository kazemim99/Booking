# Calendar View Component

## Overview

The `BookingCalendar.vue` component provides a full-featured Persian/Jalali calendar view for visualizing and managing bookings. It displays bookings with visual indicators and allows users to interact with specific dates to view detailed booking information.

## Location

`booksy-frontend/src/modules/provider/components/calendar/BookingCalendar.vue`

## Features

- **Full Jalali Calendar**: Accurate Persian calendar with proper month lengths and date conversions
- **Persian Localization**: Persian month names, weekday names, and Persian numerals throughout
- **Booking Visualization**: Shows booking counts for each day with color-coded badges
- **Interactive Day Selection**: Click any day to view all bookings for that date
- **Month Navigation**: Navigate between months with previous/next buttons
- **Today Highlighting**: Current day is visually highlighted
- **Responsive Design**: Material Design principles with smooth transitions

## Props

| Prop | Type | Required | Description |
|------|------|----------|-------------|
| `bookings` | `Booking[]` | Yes | Array of booking objects to display on the calendar |

### Booking Interface

```typescript
interface Booking {
  id: string
  customerName: string
  service: string
  date: string        // Format: YYYY-MM-DD (Gregorian)
  time: string        // Format: HH:mm (Persian numerals)
  status: string      // 'pending', 'confirmed', 'completed', 'cancelled'
}
```

## Events

| Event | Payload | Description |
|-------|---------|-------------|
| `booking-click` | `Booking` | Emitted when a booking item is clicked in the selected day view |

## Usage Example

```vue
<template>
  <BookingCalendar
    :bookings="bookings"
    @booking-click="handleBookingClick"
  />
</template>

<script setup lang="ts">
import { ref } from 'vue'
import BookingCalendar from '@/modules/provider/components/calendar/BookingCalendar.vue'

const bookings = ref([
  {
    id: '1',
    customerName: 'علی احمدی',
    service: 'کوتاهی مو',
    date: '2025-11-15',
    time: '۱۰:۳۰',
    status: 'confirmed'
  }
])

const handleBookingClick = (booking) => {
  console.log('Booking clicked:', booking)
}
</script>
```

## Implementation Details

### Jalali Calendar Generation

The component uses the `jalaali-js` library for accurate Persian/Gregorian date conversion:

```typescript
// Convert current Jalali month to Gregorian for day generation
const firstDayGregorian = jalaali.toGregorian(
  currentJalaliYear.value,
  currentJalaliMonth.value,
  1
)

// Get accurate month length for Jalali calendar
const daysInMonth = jalaali.jalaaliMonthLength(
  currentJalaliYear.value,
  currentJalaliMonth.value
)
```

### Timezone-Safe Date Formatting

**Critical**: Always use local date formatting to avoid timezone issues:

```typescript
// ✅ CORRECT - Timezone safe
const formatDateToString = (date: Date): string => {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

// ❌ WRONG - Can cause date shifts
const dateStr = date.toISOString().split('T')[0]
```

### CalendarDay Structure

Each calendar cell contains both Gregorian and Jalali date information:

```typescript
interface CalendarDay {
  gregorianDate: Date      // For matching with booking data
  jalaliDay: number        // Jalali day number (1-31)
  jalaliMonth: number      // Jalali month (1-12)
  jalaliYear: number       // Jalali year (e.g., 1404)
  dayNumber: number        // Display number
  isOtherMonth: boolean    // Is from prev/next month
  isToday: boolean         // Is current day
  bookingsCount: number    // Number of bookings on this day
}
```

## Styling

The component uses Material Design colors:

- **Primary**: `#1976d2` (blue)
- **Success**: `#4caf50` (green) - for days with bookings
- **Today**: `#e3f2fd` (light blue background)
- **Selected**: `#1976d2` (blue background with white text)

### Status Color Coding

```scss
.status-pending {
  background: #fff3cd;
  color: #856404;
}

.status-confirmed {
  background: #d1ecf1;
  color: #0c5460;
}

.status-completed {
  background: #d4edda;
  color: #155724;
}

.status-cancelled {
  background: #f8d7da;
  color: #721c24;
}
```

## Key Features

### 1. Auto-Select Today

The component automatically selects today's date when mounted:

```typescript
watch(() => props.bookings, () => {
  if (!selectedDay.value) {
    const todayDay = calendarDays.value.find(day => day.isToday && !day.isOtherMonth)
    if (todayDay) {
      selectedDay.value = todayDay
    }
  }
}, { immediate: true })
```

### 2. Persian Number Conversion

All numbers are displayed in Persian numerals:

```typescript
const convertToPersian = (num: number | string) => {
  return convertEnglishToPersianNumbers(num.toString())
}
```

### 3. Month Names

```typescript
const jalaliMonthNames = [
  'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
  'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
]
```

### 4. Week Days (Saturday first)

```typescript
const weekDays = ['شنبه', 'یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنج‌شنبه', 'جمعه']
```

## Debug Mode

The component includes a temporary debug banner that shows:
- Total bookings count
- Current month bookings count
- Selected day bookings count

This can be removed by deleting the `debug-info` div in the template (lines 4-8).

## Common Issues

### Issue 1: Empty Calendar

**Symptom**: Calendar shows no bookings even though data is present

**Cause**: Date format mismatch between booking data and calendar

**Solution**: Ensure booking dates are in `YYYY-MM-DD` format (Gregorian)

### Issue 2: Bookings Not Rendering

**Symptom**: Console shows bookings found but UI doesn't update

**Cause**: Type mismatch in helper functions

**Solution**: Ensure all helper functions accept both `number` and `string` types

### Issue 3: Date Timezone Shifts

**Symptom**: Bookings appear on wrong days

**Cause**: Using `toISOString()` for date formatting

**Solution**: Use the `formatDateToString()` helper function

## Future Enhancements

Potential improvements for the component:

1. **Week View**: Add ability to switch to week view
2. **Multiple Bookings Preview**: Show 3 mini booking previews in each day cell (Google Calendar style)
3. **Drag and Drop**: Allow drag-and-drop rescheduling
4. **Color Themes**: Allow customization of status colors
5. **Keyboard Navigation**: Arrow keys to navigate dates
6. **Accessibility**: Add ARIA labels and keyboard support

## Related Components

- `CreateBookingModal.vue` - For creating new bookings
- `Toast.vue` - For notification feedback
- `Modal.vue` - For detailed booking views
