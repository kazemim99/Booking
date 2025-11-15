# Create Booking Modal Component

## Overview

The `CreateBookingModal.vue` component provides a comprehensive form for creating new bookings. It includes customer search, service selection, Persian date/time picker, and form validation with a clear summary display.

## Location

`booksy-frontend/src/modules/provider/components/modals/CreateBookingModal.vue`

## Features

- **Customer Search**: Live filtering with dropdown selection
- **Service Selection**: Shows service details (name, duration, price)
- **Persian DateTime Picker**: Integrated `vue3-persian-datetime-picker`
- **Form Validation**: Real-time validation with disabled submit until complete
- **Booking Summary**: Shows duration and price before submission
- **Material Design**: Consistent styling with the rest of the application

## Props

| Prop | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `modelValue` | `boolean` | Yes | - | Controls modal open/close state (v-model) |
| `customers` | `Customer[]` | No | `[]` | Array of available customers for selection |
| `services` | `Service[]` | No | `[]` | Array of available services for selection |

### Customer Interface

```typescript
interface Customer {
  id: string
  name: string
  phone: string
}
```

### Service Interface

```typescript
interface Service {
  id: string
  name: string
  duration: number    // Duration in minutes
  price: number       // Price in Tomans
}
```

## Events

| Event | Payload | Description |
|-------|---------|-------------|
| `update:modelValue` | `boolean` | Emitted when modal should open/close (v-model) |
| `submit` | `BookingFormData` | Emitted when form is submitted with valid data |

### BookingFormData Interface

```typescript
interface BookingFormData {
  customerId: string
  serviceId: string
  dateTime: string     // ISO format from Persian datetime picker
  notes: string        // Optional notes
}
```

## Usage Example

```vue
<template>
  <CreateBookingModal
    v-model="showModal"
    :customers="customers"
    :services="services"
    @submit="handleNewBooking"
  />
</template>

<script setup lang="ts">
import { ref } from 'vue'
import CreateBookingModal from '@/modules/provider/components/modals/CreateBookingModal.vue'

const showModal = ref(false)

const customers = ref([
  {
    id: '1',
    name: 'علی احمدی',
    phone: '09123456789'
  }
])

const services = ref([
  {
    id: '1',
    name: 'کوتاهی مو',
    duration: 30,
    price: 50000
  }
])

const handleNewBooking = (formData) => {
  // Process the booking data
  console.log('New booking:', formData)

  // Create booking object
  const newBooking = {
    id: generateId(),
    customerId: formData.customerId,
    serviceId: formData.serviceId,
    dateTime: new Date(formData.dateTime),
    notes: formData.notes,
    status: 'pending'
  }

  // Add to bookings array and show confirmation
}
</script>
```

## Implementation Details

### Customer Search

The customer search provides live filtering with a dropdown:

```typescript
const filterCustomers = () => {
  const search = searchCustomer.value.toLowerCase().trim()
  if (!search) {
    // Show first 5 customers when no search
    filteredCustomers.value = props.customers.slice(0, 5)
  } else {
    // Filter by name or phone, limit to 5 results
    filteredCustomers.value = props.customers
      .filter(c =>
        c.name.toLowerCase().includes(search) ||
        c.phone.includes(search)
      )
      .slice(0, 5)
  }
}
```

### Form Validation

The form validates all required fields before allowing submission:

```typescript
const isFormValid = computed(() => {
  return formData.value.customerId &&
         formData.value.serviceId &&
         formData.value.dateTime
})
```

The submit button is disabled when form is invalid:

```vue
<button
  :disabled="!isFormValid"
  class="btn-primary"
>
  ثبت رزرو
</button>
```

### Persian DateTime Picker Configuration

```vue
<VuePersianDatetimePicker
  v-model="formData.dateTime"
  placeholder="تاریخ و زمان را انتخاب کنید"
  format="YYYY-MM-DD HH:mm"
  display-format="jYYYY/jMM/jDD - HH:mm"
  type="datetime"
  :min="new Date().toISOString()"
  auto-submit
  color="#1976d2"
  input-class="persian-datepicker-input"
/>
```

**Key Settings**:
- `format`: Internal format sent to v-model (Gregorian)
- `display-format`: What user sees (Jalali with Persian format)
- `type="datetime"`: Includes both date and time
- `min`: Prevents selecting past dates
- `auto-submit`: Closes picker after selection
- `color`: Material Design primary color

### Summary Display

Shows selected service details before submission:

```vue
<div v-if="selectedService" class="booking-summary">
  <h4>خلاصه رزرو</h4>
  <div class="summary-item">
    <span>مدت زمان:</span>
    <span>{{ convertToPersian(selectedService.duration) }} دقیقه</span>
  </div>
  <div class="summary-item">
    <span>هزینه:</span>
    <span>{{ convertToPersian(selectedService.price) }} تومان</span>
  </div>
</div>
```

## Form Fields

### 1. Customer Selection (Required)

- Search input with live filtering
- Dropdown shows up to 5 matching results
- Filters by name or phone number
- Shows selected customer in highlighted box

### 2. Service Selection (Required)

- Dropdown select showing all services
- Each option displays: Name - Duration - Price
- Persian numerals for duration and price

### 3. Date & Time (Required)

- Persian datetime picker
- Shows Jalali calendar
- Minimum date is today
- Format: `jYYYY/jMM/jDD - HH:mm`

### 4. Notes (Optional)

- Textarea for additional notes
- 3 rows default height
- Resizable vertically

## Styling

### Material Design Components

All form elements follow Material Design principles:

```scss
.form-input,
.form-select,
.form-textarea {
  padding: 12px 16px;
  border: 1px solid #d1d5db;
  border-radius: 8px;

  &:focus {
    border-color: #1976d2;
    box-shadow: 0 0 0 3px rgba(25, 118, 210, 0.1);
  }
}
```

### Required Field Indicator

Required fields show a red asterisk:

```scss
.form-label.required::after {
  content: ' *';
  color: #ef4444;
}
```

### Customer Dropdown

Positioned absolutely below search input:

```scss
.customers-dropdown {
  position: absolute;
  top: 100%;
  max-height: 200px;
  overflow-y: auto;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  z-index: 10;
}
```

### Button States

Primary button with disabled state:

```scss
.btn-primary {
  background: #1976d2;
  color: white;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}
```

## Form Lifecycle

### Opening Modal

1. Modal opens with empty form
2. Customer dropdown shows first 5 customers
3. All fields are empty
4. Submit button is disabled

### Filling Form

1. User types in customer search → dropdown filters
2. User selects customer → search field fills, dropdown closes
3. User selects service → summary appears
4. User selects date/time → validation checks
5. Submit button enables when all required fields filled

### Submitting Form

1. User clicks "ثبت رزرو" (Submit Booking)
2. Form validates all required fields
3. Emits `submit` event with form data
4. Form resets to initial state
5. Modal closes

### Cancelling

1. User clicks "انصراف" (Cancel)
2. Form resets immediately
3. Modal closes
4. No event emitted

## Integration Example

Complete integration with bookings view:

```vue
<template>
  <div class="bookings-view">
    <!-- Trigger button -->
    <button @click="showCreateModal = true">
      رزرو جدید
    </button>

    <!-- Modal -->
    <CreateBookingModal
      v-model="showCreateModal"
      :customers="customers"
      :services="services"
      @submit="handleNewBooking"
    />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useToast } from '@/composables/useToast'
import CreateBookingModal from '@/modules/provider/components/modals/CreateBookingModal.vue'
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'

const toast = useToast()
const showCreateModal = ref(false)
const bookings = ref([])

const customers = ref([
  { id: '1', name: 'علی احمدی', phone: '09123456789' },
  { id: '2', name: 'سارا محمدی', phone: '09876543210' }
])

const services = ref([
  { id: '1', name: 'کوتاهی مو', duration: 30, price: 50000 },
  { id: '2', name: 'رنگ مو', duration: 120, price: 200000 }
])

const handleNewBooking = (formData) => {
  const selectedService = services.value.find(s => s.id === formData.serviceId)
  const selectedCustomer = customers.value.find(c => c.id === formData.customerId)

  if (!selectedService || !selectedCustomer) return

  // Convert datetime to display format
  const dateTime = new Date(formData.dateTime)
  const date = dateTime.toISOString().split('T')[0]
  const hours = dateTime.getHours().toString().padStart(2, '0')
  const minutes = dateTime.getMinutes().toString().padStart(2, '0')
  const time = convertEnglishToPersianNumbers(`${hours}:${minutes}`)

  // Create booking object
  const newBooking = {
    id: (bookings.value.length + 1).toString(),
    customerName: selectedCustomer.name,
    customerPhone: selectedCustomer.phone,
    date,
    time,
    service: selectedService.name,
    price: selectedService.price,
    status: 'pending',
  }

  // Add to bookings array
  bookings.value.unshift(newBooking)

  // Show success message
  toast.success('رزرو جدید با موفقیت ثبت شد')
}
</script>
```

## Common Issues

### Issue 1: Dropdown Not Closing

**Symptom**: Customer dropdown stays open after selection

**Cause**: `showCustomerDropdown` not being set to false

**Solution**: Ensure `selectCustomer()` sets `showCustomerDropdown.value = false`

### Issue 2: Form Not Validating

**Symptom**: Submit button stays disabled even with all fields filled

**Cause**: Date picker not updating `formData.dateTime`

**Solution**: Ensure datetime picker uses `v-model="formData.dateTime"`

### Issue 3: Persian Numbers Not Showing

**Symptom**: Numbers appear in English

**Cause**: Not using `convertToPersian()` helper

**Solution**: Wrap all numbers with `convertToPersian()`

### Issue 4: Form Not Resetting

**Symptom**: Previous form data appears when reopening modal

**Cause**: Form not being reset on modal close

**Solution**: Watch `isOpen` and call `resetForm()` when false

## Accessibility

Improvements for accessibility:

1. Add ARIA labels to form fields
2. Add keyboard shortcuts (Escape to close)
3. Focus management (auto-focus first field on open)
4. Screen reader announcements for validation errors

## Future Enhancements

1. **Add Customer**: Button to create new customer inline
2. **Multiple Services**: Allow selecting multiple services
3. **Recurring Bookings**: Option to create recurring appointments
4. **Availability Check**: Show provider availability
5. **Service Duration Visualization**: Calendar-based time slot selection
6. **Price Calculator**: Show total with discounts/packages
7. **Customer History**: Show customer's previous bookings
8. **Validation Messages**: Show specific error messages for each field

## Related Components

- `BookingCalendar.vue` - For viewing bookings in calendar format
- `Modal.vue` - Base modal component used by this component
- `Toast.vue` - For showing success/error notifications
- `VuePersianDatetimePicker` - Third-party Persian datetime picker
