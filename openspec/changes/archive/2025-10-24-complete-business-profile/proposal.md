# Complete Business Profile Management

## Why

Currently, the provider business profile management is incomplete. While basic business information and hours can be edited, critical features for providers to manage their business are missing:

1. **Service Management**: No UI exists for providers to add, edit, or manage their service catalog (backend exists but no frontend implementation)
2. **Staff Management**: No UI for managing team members, their roles, schedules, or availability (backend exists but placeholder views only)
3. **Settings Management**: Provider settings page is a placeholder with no functionality
4. **Media Gallery**: Missing ability to showcase business with photos/portfolio
5. **UX Consistency**: Existing business info and hours pages lack cohesive UX patterns and navigation

Providers cannot effectively operate their business on the platform without these essential management capabilities. This change completes the business profile by implementing comprehensive provider management tools with a polished, intuitive UX.

## What Changes

### New Capabilities
- **Service Catalog Management**: Full CRUD interface for services with pricing, duration, categories, availability settings, and staff assignment
- **Staff Management**: Complete interface for adding/editing staff members, setting schedules, managing availability, and role assignments
- **Provider Settings**: Comprehensive settings for booking preferences, notifications, business policies, and operational preferences
- **Media Gallery Management**: Upload and manage business photos, service images, and portfolio items
- **Business Profile Hub**: Centralized navigation with tabbed interface for all business management sections

### Enhanced Capabilities
- **Business Information**: Improve UX with better validation, real-time preview, image upload capabilities
- **Business Hours**: Enhance with visual calendar view, exception dates, holiday scheduling
- **Navigation**: Unified provider dashboard with clear navigation to all management sections

### UX Improvements
- Consistent card-based layouts across all management pages
- Real-time validation and inline feedback
- Drag-and-drop for ordering services and gallery items
- Bulk actions for efficient management
- Responsive design for mobile/tablet management
- Loading states, empty states, and error handling
- Action confirmations for destructive operations

## Impact

### Affected Specs
- **provider-management** (NEW): Complete business profile management capabilities
- **service-management** (NEW): Service catalog CRUD and configuration
- **staff-management** (NEW): Staff member and schedule management
- **provider-settings** (NEW): Business settings and preferences

### Affected Code

#### Frontend (booksy-frontend/src/modules/provider/)
- `views/ProviderServicesView.vue` - Replace placeholder with full service management
- `views/ProviderStaffView.vue` - Replace placeholder with full staff management
- `views/ProviderSettingsView.vue` - Replace placeholder with settings management
- `views/services/ServiceListView.vue` - Implement service list with CRUD
- `views/services/ServiceEditorView.vue` - Implement service create/edit form
- `views/staff/StaffListView.vue` - Implement staff list with CRUD
- `views/staff/StaffEditorView.vue` - Implement staff create/edit form
- `views/gallery/GalleryView.vue` - Implement media gallery management
- `views/profile/BusinessInfoView.vue` - Enhance UX and add image upload
- `views/profile/BusinessHoursView.vue` - Add calendar view and exceptions
- `components/profile/` - New shared components for profile management
- `stores/service.store.ts` - New Pinia store for service management
- `stores/staff.store.ts` - New Pinia store for staff management
- `stores/gallery.store.ts` - New Pinia store for media management
- `types/service.types.ts` - Complete service type definitions
- `types/staff.types.ts` - Complete staff type definitions

#### Backend (src/BoundedContexts/ServiceCatalog/)
- Existing commands/queries already support most operations (minimal backend changes needed)
- May need additional endpoints for:
  - Bulk service operations
  - Staff schedule management enhancements
  - Media upload/management API
  - Provider settings persistence

#### Shared Components (booksy-frontend/src/shared/components/)
- `ImageUpload.vue` - New component for image upload
- `ImageGallery.vue` - New component for gallery display
- `DragDropList.vue` - New component for reorderable lists
- `Calendar.vue` - Enhanced calendar component for schedules
- `TimeRangePicker.vue` - Reusable time range picker

### Dependencies
- Image upload requires file upload infrastructure (e.g., cloud storage integration)
- May require new backend endpoints for media management
- Frontend image cropping/preview library (e.g., vue-advanced-cropper or similar)

### Breaking Changes
None - this is additive functionality that completes existing features.

### Migration
No migration needed - existing provider data remains compatible.
