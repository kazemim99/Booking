# Implement Full Staff CRUD Functionality

## Why

The Staff tab currently has basic list and modal functionality but lacks complete CRUD operations that align with the comprehensive staff-management specification. The backend API endpoints exist in ProvidersController (GetStaff, AddStaff, UpdateStaff, RemoveStaff) but need to be reviewed and potentially enhanced to fully support all staff management requirements including role management, notes, profile photos, service assignments, schedule management, and performance tracking.

The current implementation only supports basic creation and update of staff with firstName, lastName, and phoneNumber. Many critical features from the staff-management spec are not yet implemented, including:
- Staff profile photos and rich profiles
- Role assignment with predefined and custom roles
- Service assignments and qualification levels
- Schedule management and availability tracking
- Performance metrics and booking history
- Bulk operations for multiple staff
- Staff communication and notifications
- Activation/deactivation workflows

## What Changes

This proposal implements complete CRUD functionality for staff management across both backend and frontend:

### Backend Enhancements
- **Extend Staff API endpoints** to support:
  - Profile photo upload and management
  - Extended staff profile fields (biography, notes, hire date)
  - Service assignment operations
  - Schedule management operations
  - Staff activation/reactivation operations
  - Performance metrics and statistics endpoints
  - Bulk operation endpoints
- **Add missing Commands/Queries** for new operations
- **Enhance Staff domain entity** with new methods for profile management
- **Add StaffService** for schedule and availability calculations

### Frontend Enhancements
- **Enhanced Staff Form** with:
  - Profile photo upload with cropping (1:1 aspect ratio)
  - Role selection (predefined + custom)
  - Biography and notes fields
  - Hire date picker
  - Email field (optional invitation)
- **Staff Detail View** showing:
  - Complete profile information
  - Assigned services list
  - Weekly schedule view
  - Performance statistics
  - Booking history
- **Service Assignment Interface** for assigning/unassigning services
- **Schedule Management Interface** for setting working hours
- **Staff Status Management** for activation/deactivation with reasons
- **Enhanced Filtering** by role, status, and assigned services

### API Changes
- `GET /api/v1/providers/{providerId}/staff/{staffId}` - Get staff details (**NEW**)
- `POST /api/v1/providers/{providerId}/staff/{staffId}/photo` - Upload profile photo (**NEW**)
- `PUT /api/v1/providers/{providerId}/staff/{staffId}/services` - Assign services (**NEW**)
- `GET /api/v1/providers/{providerId}/staff/{staffId}/schedule` - Get schedule (**NEW**)
- `PUT /api/v1/providers/{providerId}/staff/{staffId}/schedule` - Update schedule (**NEW**)
- `POST /api/v1/providers/{providerId}/staff/{staffId}/activate` - Reactivate staff (**NEW**)
- `GET /api/v1/providers/{providerId}/staff/{staffId}/metrics` - Get performance metrics (**NEW**)
- Enhance existing `POST /api/v1/providers/{providerId}/staff` to include new fields
- Enhance existing `PUT /api/v1/providers/{providerId}/staff/{staffId}` to include new fields
- Enhance existing `DELETE /api/v1/providers/{providerId}/staff/{staffId}` to require deactivation reason

## Impact

### Affected Specs
- **staff-management**: Complete implementation of all 10 requirements (MODIFIED - extensive additions)

### Affected Code

#### Backend
- `Booksy.ServiceCatalog.Api/Controllers/V1/ProvidersController.cs` - Add new staff endpoints
- `Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Entities/Staff.cs` - Add new properties and methods
- `Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Entities/StaffSchedule.cs` - May need enhancements
- `Booksy.ServiceCatalog.Application/Commands/Provider/` - Add new command handlers
- `Booksy.ServiceCatalog.Application/Queries/Provider/` - Add new query handlers
- `Booksy.ServiceCatalog.Application/Services/` - Add StaffService for business logic
- `Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/StaffConfiguration.cs` - Update entity configuration
- `Booksy.ServiceCatalog.Api/Models/Requests/` - Add new request models
- `Booksy.ServiceCatalog.Api/Models/Responses/` - Add new response models

#### Frontend
- `booksy-frontend/src/modules/provider/views/staff/StaffListView.vue` - Enhanced list with filters
- `booksy-frontend/src/modules/provider/views/staff/StaffDetailView.vue` - **NEW** detailed view
- `booksy-frontend/src/modules/provider/components/staff/StaffCard.vue` - Enhanced card display
- `booksy-frontend/src/modules/provider/components/staff/StaffForm.vue` - **NEW** comprehensive form
- `booksy-frontend/src/modules/provider/components/staff/StaffScheduleEditor.vue` - **NEW** schedule editor
- `booksy-frontend/src/modules/provider/components/staff/ServiceAssignment.vue` - **NEW** service picker
- `booksy-frontend/src/modules/provider/stores/staff.store.ts` - Add new actions and state
- `booksy-frontend/src/modules/provider/services/staff.service.ts` - Add new API methods
- `booksy-frontend/src/modules/provider/types/staff.types.ts` - Extend types for new features

### Breaking Changes
None - this is additive functionality. Existing basic CRUD operations remain compatible.

### Migration Notes
- Existing staff records will not have new optional fields (biography, notes, profile photo) - these default to null/empty
- No database migration required for MVP if new fields are optional
- Future enhancement: Add migrations for new fields if they become required

### Dependencies
- Image storage service (already exists for provider profile/gallery images)
- Schedule calculation logic (new service layer component)
- Service entity access for assignments (read-only initially)

### Testing Requirements
- Unit tests for new command/query handlers
- Integration tests for new API endpoints
- Frontend unit tests for new components
- E2E tests for complete staff management workflow
- Test staff deactivation/reactivation scenarios
- Test service assignment workflows
- Test schedule management and conflict detection
