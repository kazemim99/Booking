# Booksy Documentation

Welcome to the Booksy booking platform documentation. This directory contains comprehensive guides, API documentation, and development notes.

## Quick Navigation

### Recent Features (November 2025)

#### Latest (Nov 20, 2025) - Role-Based Navigation 🎯
- **[Session Summary Nov 20](./SESSION_SUMMARY_2025_11_20.md)** - Complete UX redesign session
- **[Quick Reference](./QUICK_REFERENCE_ROLE_NAVIGATION.md)** - Role navigation quick start guide
- **[UX Design Document](./UX_ROLE_BASED_NAVIGATION.md)** - Comprehensive UX design and flow diagrams
- **[Implementation Guide](./UX_IMPLEMENTATION_SUMMARY.md)** - Integration and testing guide
- **[Double Auth Call Fix](./FIX_DOUBLE_AUTH_CALL.md)** - Authentication issue resolution

#### Previous Features
- **[Calendar View](./CALENDAR_VIEW.md)** - Persian/Jalali calendar component for booking visualization
- **[Create Booking Modal](./CREATE_BOOKING_MODAL.md)** - Comprehensive booking creation form
- **[Package Consolidation](./PACKAGE_CONSOLIDATION.md)** - Dependency cleanup and optimization
- **[Session Summary Nov 14](./SESSION_SUMMARY_2025_11_14.md)** - Development session overview

### Existing Documentation

- **[API Design Notes](./api-design-notes.md)** - API architecture and design decisions
- **[ZarinPal Sandbox Testing](./ZarinPal-Sandbox-Testing-Guide.md)** - Payment gateway testing guide

## Project Overview

Booksy is a comprehensive booking platform designed for service providers (salons, spas, etc.) to manage appointments, customers, and services. The platform supports Persian/Farsi language with full RTL layout and Jalali calendar integration.

## Tech Stack

### Frontend

- **Framework**: Vue 3 with Composition API
- **Language**: TypeScript
- **Styling**: SCSS with Material Design principles
- **State Management**: Pinia
- **Charts**: ECharts (migrated from Chart.js)
- **Date/Time**: jalaali-js, vue3-persian-datetime-picker
- **Persian Tools**: @persian-tools/persian-tools

### Backend

- **Framework**: Laravel
- **Database**: MySQL
- **Payment**: ZarinPal

## Project Structure

```
Booksy/
├── booksy-frontend/          # Vue 3 frontend application
│   ├── src/
│   │   ├── modules/
│   │   │   └── provider/     # Provider module
│   │   │       ├── components/
│   │   │       │   ├── calendar/      # Calendar components
│   │   │       │   ├── modals/        # Modal components
│   │   │       │   └── dashboard/     # Dashboard components
│   │   │       └── views/             # Page views
│   │   └── shared/           # Shared components and utilities
│   │       ├── components/   # Reusable components
│   │       └── utils/        # Utility functions
│   └── package.json
├── booksy-backend/           # Laravel backend
└── docs/                     # This documentation directory
```

## Key Features

### 1. Booking Management

- **Calendar View**: Visual calendar with Persian/Jalali support
- **List View**: Tabular booking list with filters
- **Create Booking**: Comprehensive form with customer search
- **Status Management**: Track booking lifecycle (pending → confirmed → completed)

### 2. Persian/Farsi Support

- **RTL Layout**: Full right-to-left interface
- **Jalali Calendar**: Accurate Persian calendar with proper month calculations
- **Persian Numerals**: All numbers displayed in Persian
- **B Nazanin Font**: Professional Persian typography

### 3. Material Design

- **Consistent Colors**: Primary (#1976d2), Success (#4caf50), Warning (#ff9800), Error (#ef4444)
- **Elevation**: Proper shadow system
- **Transitions**: Smooth animations
- **Responsive**: Mobile-first design

## Development Guidelines

### Date Handling

**CRITICAL**: Always use timezone-safe date formatting

❌ **Wrong**:
```typescript
const dateStr = date.toISOString().split('T')[0]
```

✅ **Correct**:
```typescript
const formatDateToString = (date: Date): string => {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}
```

### Persian Numbers

Always convert numbers for display:

```typescript
import { convertEnglishToPersianNumbers } from '@/shared/utils/date/jalali.utils'

const persianNumber = convertEnglishToPersianNumbers('123')
// Result: '۱۲۳'
```

### Color Usage

Use Material Design color variables:

```scss
.primary { color: #1976d2; }
.success { color: #4caf50; }
.warning { color: #ff9800; }
.error { color: #ef4444; }
```

### Component Structure

Follow Vue 3 Composition API patterns:

```vue
<template>
  <!-- Template -->
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'

// Interfaces
interface Props {
  // ...
}

// Props and Emits
const props = defineProps<Props>()
const emit = defineEmits<{
  'event-name': [payload: Type]
}>()

// State
const state = ref(initialValue)

// Computed
const computed = computed(() => {
  // ...
})

// Methods
const method = () => {
  // ...
}
</script>

<style scoped lang="scss">
// Styles
</style>
```

## Getting Started

### Prerequisites

- Node.js 18+
- npm or yarn
- PHP 8.1+
- Composer
- MySQL 8.0+

### Frontend Setup

```bash
cd booksy-frontend
npm install
npm run dev
```

### Backend Setup

```bash
cd booksy-backend
composer install
php artisan migrate
php artisan serve
```

## Component Documentation

### Calendar Components

- **[BookingCalendar](./CALENDAR_VIEW.md)** - Main calendar view with Jalali support

### Modal Components

- **[CreateBookingModal](./CREATE_BOOKING_MODAL.md)** - Booking creation form

### Chart Components

- **LineChart** - Line chart using ECharts
- **PieChart** - Pie chart using ECharts

### Shared Components

- **Modal** - Base modal component
- **Toast** - Notification system
- **VuePersianDatetimePicker** - Persian date/time picker

## Common Patterns

### Form Validation

```typescript
const isFormValid = computed(() => {
  return formData.value.field1 &&
         formData.value.field2 &&
         formData.value.field3
})
```

```vue
<button :disabled="!isFormValid">Submit</button>
```

### Toast Notifications

```typescript
import { useToast } from '@/composables/useToast'

const toast = useToast()

toast.success('عملیات با موفقیت انجام شد')
toast.error('خطا در انجام عملیات')
```

### Modal Management

```vue
<template>
  <Modal v-model="isOpen" title="عنوان">
    <!-- Content -->
  </Modal>
</template>

<script setup>
const isOpen = ref(false)

const openModal = () => {
  isOpen.value = true
}
</script>
```

## API Integration

### Base URL

```typescript
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL
```

### Example API Call

```typescript
import axios from 'axios'

const fetchBookings = async () => {
  try {
    const response = await axios.get('/api/bookings')
    return response.data
  } catch (error) {
    console.error('Error fetching bookings:', error)
    throw error
  }
}
```

## Testing

### Unit Tests

```bash
npm run test:unit
```

### E2E Tests

```bash
npm run test:e2e
```

### Component Testing

```bash
npm run test:component
```

## Build and Deployment

### Development Build

```bash
npm run build:dev
```

### Production Build

```bash
npm run build
```

### Preview Production

```bash
npm run preview
```

## Performance Optimization

### Bundle Size

Current bundle sizes after optimization:
- **Total**: ~4.5MB
- **Vendor**: ~3.2MB
- **App**: ~1.3MB

### Lazy Loading

```typescript
// Lazy load routes
const routes = [
  {
    path: '/calendar',
    component: () => import('@/modules/provider/views/CalendarView.vue')
  }
]
```

### Code Splitting

```typescript
// Dynamic imports
const component = await import('@/components/HeavyComponent.vue')
```

## Troubleshooting

### Common Issues

#### Issue: Calendar shows wrong dates

**Solution**: Check timezone handling - use local date methods, not `toISOString()`

#### Issue: Persian numbers not showing

**Solution**: Ensure `convertEnglishToPersianNumbers()` is used on all display numbers

#### Issue: Charts not rendering

**Solution**: Check ECharts registration and data format conversion

#### Issue: Modal not opening

**Solution**: Verify v-model binding and initial state

## Contributing

### Code Style

- Use TypeScript for type safety
- Follow Vue 3 Composition API patterns
- Use SCSS for styling
- Follow Material Design principles
- Add JSDoc comments for complex functions

### Commit Messages

```
feat: Add new feature
fix: Fix bug
refactor: Refactor code
docs: Update documentation
style: Format code
test: Add tests
chore: Update dependencies
```

### Pull Request Process

1. Create feature branch from `master`
2. Implement feature with tests
3. Update documentation
4. Submit PR with clear description
5. Address review comments
6. Merge after approval

## Resources

### External Documentation

- [Vue 3 Documentation](https://vuejs.org/)
- [TypeScript Documentation](https://www.typescriptlang.org/)
- [ECharts Documentation](https://echarts.apache.org/)
- [jalaali-js GitHub](https://github.com/jalaali/jalaali-js)
- [Material Design Guidelines](https://material.io/design)

### Useful Links

- [Persian Tools](https://persian-tools.github.io/)
- [Vue Persian Datetime Picker](https://github.com/talkhabi/vue-persian-datetime-picker)

## Changelog

### November 2025

- ✅ Package consolidation (~1.7MB reduction)
- ✅ Calendar view with Jalali support
- ✅ Create booking modal
- ✅ Chart migration to ECharts
- ✅ Comprehensive documentation

### October 2025

- Material Design UI refresh
- B Nazanin font integration
- Toast notification system
- Modal component library

## License

[Your License Here]

## Support

For questions or issues:
1. Check this documentation
2. Review git commit history
3. Check component documentation
4. Review session summaries

---

**Last Updated**: November 14, 2025
**Version**: 1.0.0
**Maintained By**: Development Team
