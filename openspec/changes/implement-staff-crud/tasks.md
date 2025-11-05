# Implementation Tasks

## 1. Backend - Domain Layer

- [ ] 1.1 Update Staff entity with new properties
  - [ ] Add ProfilePhotoUrl property
  - [ ] Add Biography property (max 500 chars)
  - [ ] Add Notes property (max 1000 chars) - private
  - [ ] Add HireDate property (DateTime)
  - [ ] Update Role to support custom roles (string)
  - [ ] Add UpdateProfilePhoto method
  - [ ] Add UpdateBiography method
  - [ ] Add UpdateNotes method
  - [ ] Add UpdateHireDate method

- [ ] 1.2 Create or enhance StaffSchedule entity
  - [ ] Verify DayOfWeek, WorkingHours, BreakTimes properties exist
  - [ ] Add EffectiveFrom and EffectiveTo for schedule versioning
  - [ ] Add IsAvailable boolean flag

- [ ] 1.3 Create StaffService domain entity for service assignments
  - [ ] Create StaffService entity with StaffId, ServiceId, QualificationLevel
  - [ ] Add methods in Provider aggregate to manage staff-service assignments
  - [ ] Add domain events for service assignment changes

- [ ] 1.4 Add domain events
  - [ ] StaffProfileUpdatedEvent (for photo, bio, notes changes)
  - [ ] StaffReactivatedEvent
  - [ ] StaffScheduleUpdatedEvent
  - [ ] StaffServiceAssignedEvent / StaffServiceUnassignedEvent

## 2. Backend - Application Layer Commands

- [ ] 2.1 Create GetStaffByIdQuery
  - [ ] Query handler to fetch single staff with all details
  - [ ] Include schedule and service assignments
  - [ ] Map to StaffDetailsViewModel

- [ ] 2.2 Enhance AddStaffToProviderCommand
  - [ ] Add Email, Biography, Notes, HireDate, ProfilePhotoUrl parameters
  - [ ] Update command handler to set new properties
  - [ ] Validate biography and notes length
  - [ ] Validate hire date is not in future

- [ ] 2.3 Enhance UpdateProviderStaffCommand
  - [ ] Add Biography, Notes, HireDate, ProfilePhotoUrl parameters
  - [ ] Update command handler to update new properties
  - [ ] Implement partial update logic
  - [ ] Add validation for new fields

- [ ] 2.4 Enhance DeactivateProviderStaffCommand
  - [ ] Ensure TerminationReason is captured
  - [ ] Add TerminatedAt timestamp
  - [ ] Handle reassignment of future bookings
  - [ ] Emit deactivation event

- [ ] 2.5 Create ActivateProviderStaffCommand
  - [ ] Command to reactivate deactivated staff
  - [ ] Clear TerminatedAt and TerminationReason
  - [ ] Restore previous schedule and services
  - [ ] Emit reactivation event

- [ ] 2.6 Create UploadStaffPhotoCommand
  - [ ] Command handler to process photo upload
  - [ ] Use existing IImageStorageService
  - [ ] Generate thumbnail (100x100) and display (400x400)
  - [ ] Update Staff.ProfilePhotoUrl
  - [ ] Delete old photo if exists

- [ ] 2.7 Create AssignServicesToStaffCommand
  - [ ] Command to assign/unassign services
  - [ ] Accept list of service IDs
  - [ ] Create StaffService records
  - [ ] Validate services belong to same provider

- [ ] 2.8 Create UpdateStaffScheduleCommand
  - [ ] Command to set/update weekly schedule
  - [ ] Accept schedule data for each day
  - [ ] Validate working hours are within business hours
  - [ ] Handle break times

- [ ] 2.9 Create GetStaffPerformanceMetricsQuery (future enhancement)
  - [ ] Placeholder for booking count, revenue, ratings
  - [ ] Return empty/mock data for MVP

## 3. Backend - API Layer

- [ ] 3.1 Add new endpoints to ProvidersController
  - [ ] GET `/api/v1/providers/{providerId}/staff/{staffId}` - Get staff details
  - [ ] POST `/api/v1/providers/{providerId}/staff/{staffId}/photo` - Upload photo
  - [ ] POST `/api/v1/providers/{providerId}/staff/{staffId}/activate` - Reactivate
  - [ ] PUT `/api/v1/providers/{providerId}/staff/{staffId}/services` - Assign services
  - [ ] GET `/api/v1/providers/{providerId}/staff/{staffId}/schedule` - Get schedule
  - [ ] PUT `/api/v1/providers/{providerId}/staff/{staffId}/schedule` - Update schedule
  - [ ] GET `/api/v1/providers/{providerId}/staff/{staffId}/metrics` - Get metrics (future)

- [ ] 3.2 Create new Request/Response models
  - [ ] StaffDetailsResponse (with all fields)
  - [ ] UploadStaffPhotoRequest (multipart form)
  - [ ] AssignServicesRequest (list of service IDs)
  - [ ] UpdateScheduleRequest (schedule data)
  - [ ] ActivateStaffRequest (reactivation notes)
  - [ ] StaffPerformanceMetricsResponse (future)

- [ ] 3.3 Update existing Request models
  - [ ] AddStaffRequest - add Biography, Notes, HireDate, Email
  - [ ] UpdateStaffRequest - add Biography, Notes, HireDate, ProfilePhotoUrl

- [ ] 3.4 Update existing StaffSummaryResponse
  - [ ] Add profilePhotoUrl
  - [ ] Add role field
  - [ ] Add biography (truncated)
  - [ ] Add assignedServicesCount
  - [ ] Add tenure calculation

- [ ] 3.5 Add API documentation
  - [ ] Update XML doc comments for all new endpoints
  - [ ] Add example requests/responses
  - [ ] Document validation rules

## 4. Backend - Infrastructure Layer

- [ ] 4.1 Update StaffConfiguration (EF Core)
  - [ ] Add mapping for ProfilePhotoUrl
  - [ ] Add mapping for Biography (max length 500)
  - [ ] Add mapping for Notes (max length 1000)
  - [ ] Add mapping for HireDate

- [ ] 4.2 Create StaffServiceConfiguration (if new entity)
  - [ ] Configure StaffService entity
  - [ ] Set up foreign keys to Staff and Service
  - [ ] Configure QualificationLevel enum

- [ ] 4.3 Create database migration
  - [ ] Add new columns to Staff table
  - [ ] Create StaffService junction table (if needed)
  - [ ] Add indexes for performance

- [ ] 4.4 Update repositories if needed
  - [ ] Ensure queries include new properties
  - [ ] Add methods for service assignment queries

## 5. Frontend - Types and Models

- [ ] 5.1 Update staff.types.ts
  - [ ] Add profilePhotoUrl to Staff interface
  - [ ] Add biography to Staff interface
  - [ ] Add notes to Staff interface (private)
  - [ ] Add hiredAt date to Staff interface
  - [ ] Add terminatedAt, terminationReason to Staff interface
  - [ ] Create StaffDetailsView interface
  - [ ] Create ServiceAssignment interface
  - [ ] Create StaffSchedule interface (if not exists)

- [ ] 5.2 Create validation schemas
  - [ ] Create Zod or Yup schemas for form validation
  - [ ] Biography max 500 chars
  - [ ] Notes max 1000 chars
  - [ ] Email format validation
  - [ ] Phone format validation

## 6. Frontend - Services Layer

- [ ] 6.1 Update staff.service.ts
  - [ ] Add getStaffById(providerId, staffId) method
  - [ ] Add uploadStaffPhoto(providerId, staffId, file) method
  - [ ] Add activateStaff(providerId, staffId, notes) method
  - [ ] Add assignServices(providerId, staffId, serviceIds) method
  - [ ] Add getStaffSchedule(providerId, staffId) method
  - [ ] Add updateStaffSchedule(providerId, staffId, schedule) method
  - [ ] Update createStaff to include new fields
  - [ ] Update updateStaff to include new fields

## 7. Frontend - Store Layer

- [ ] 7.1 Update staff.store.ts
  - [ ] Add loadStaffById(providerId, staffId) action
  - [ ] Add uploadStaffPhoto(providerId, staffId, file) action
  - [ ] Add activateStaff(providerId, staffId, notes) action
  - [ ] Add assignServices(providerId, staffId, serviceIds) action
  - [ ] Add loadStaffSchedule(providerId, staffId) action
  - [ ] Add updateStaffSchedule(providerId, staffId, schedule) action
  - [ ] Update createStaff action to include new fields
  - [ ] Update updateStaff action to include new fields
  - [ ] Add role filter state
  - [ ] Add enhanced search filter state

## 8. Frontend - Shared Components

- [ ] 8.1 Create ImageCropper.vue component
  - [ ] Image upload with preview
  - [ ] Crop area with 1:1 aspect ratio constraint
  - [ ] Zoom and pan controls
  - [ ] Accept/Cancel buttons
  - [ ] Emit cropped image blob

- [ ] 8.2 Create RoleSelector.vue component
  - [ ] Dropdown with predefined roles
  - [ ] "Custom" option for custom role entry
  - [ ] Role badge preview with color
  - [ ] Emit selected role

- [ ] 8.3 Create CharacterCounter.vue component
  - [ ] Display current/max character count
  - [ ] Change color when approaching limit (yellow at 90%, red at 100%)
  - [ ] Works with textarea v-model

## 9. Frontend - Staff Components

- [ ] 9.1 Create StaffForm.vue component
  - [ ] Comprehensive form with all staff fields
  - [ ] First name and last name inputs (required)
  - [ ] Email input (optional) with validation
  - [ ] Phone number input (optional) with validation
  - [ ] Role selector with predefined + custom options
  - [ ] Hire date picker (defaults to today)
  - [ ] Biography textarea with 500 char limit and counter
  - [ ] Notes textarea with 1000 char limit and counter
  - [ ] Profile photo upload with cropping
  - [ ] Photo preview with remove option
  - [ ] Real-time validation with error messages
  - [ ] Submit and cancel buttons
  - [ ] Loading states during submission
  - [ ] Success/error notifications

- [ ] 9.2 Create StaffDetailView.vue
  - [ ] Full-page staff detail view
  - [ ] Header with photo, name, role badge, status badge
  - [ ] Contact information section
  - [ ] Biography section (if present)
  - [ ] Internal notes section (if present)
  - [ ] Assigned services list with add/remove
  - [ ] Weekly schedule summary with edit button
  - [ ] Performance metrics section (placeholder for MVP)
  - [ ] Action buttons: Edit, Deactivate/Activate, Delete
  - [ ] Tabs or sections for organization

- [ ] 9.3 Update StaffCard.vue component
  - [ ] Display profile photo (or default avatar)
  - [ ] Show role badge with color
  - [ ] Display truncated biography
  - [ ] Show hire date and tenure
  - [ ] Show assigned services count
  - [ ] Add status indicator (active/inactive)
  - [ ] Click to open detail view

- [ ] 9.4 Create ServiceAssignmentModal.vue
  - [ ] Modal for assigning services to staff
  - [ ] Searchable list of available services
  - [ ] Multi-select checkboxes
  - [ ] Show currently assigned services
  - [ ] Filter by service category
  - [ ] Save and cancel buttons
  - [ ] Optimistic UI updates

- [ ] 9.5 Create StaffScheduleEditor.vue
  - [ ] Weekly schedule grid (7 days)
  - [ ] For each day: IsWorking toggle, StartTime, EndTime pickers
  - [ ] Break times management (add/remove breaks)
  - [ ] "Copy to all days" helper
  - [ ] Validation: working hours within business hours
  - [ ] Visual conflicts indicator
  - [ ] Save and cancel buttons

- [ ] 9.6 Create DeactivateStaffModal.vue
  - [ ] Modal for staff deactivation workflow
  - [ ] Reason dropdown (Resignation, Terminated, etc.)
  - [ ] Termination date picker (defaults to today)
  - [ ] Optional notes textarea
  - [ ] Warning about future bookings
  - [ ] Reassign bookings option (future enhancement)
  - [ ] Confirm and cancel buttons

- [ ] 9.7 Create ActivateStaffModal.vue
  - [ ] Modal for staff reactivation
  - [ ] Display previous termination info
  - [ ] Optional reactivation notes
  - [ ] Confirm restoration of schedule and services
  - [ ] Confirm and cancel buttons

## 10. Frontend - Views

- [ ] 10.1 Update StaffListView.vue
  - [ ] Add role filter dropdown
  - [ ] Add status filter (All, Active, Inactive)
  - [ ] Enhance search to include email, phone, role
  - [ ] Show staff count by filter
  - [ ] "Show inactive" toggle
  - [ ] Improved empty states
  - [ ] Use enhanced StaffCard component
  - [ ] Click card to open detail view

- [ ] 10.2 Create staff detail route
  - [ ] Add route `/provider/staff/:staffId`
  - [ ] Route to StaffDetailView.vue
  - [ ] Auth guard for provider access

- [ ] 10.3 Update StaffForm modal usage
  - [ ] Replace simple form with comprehensive StaffForm.vue
  - [ ] Handle all new fields
  - [ ] Photo upload during create/edit

## 11. Frontend - Routing

- [ ] 11.1 Add staff detail route
  - [ ] Path: `/provider/staff/:id`
  - [ ] Component: StaffDetailView
  - [ ] Auth guard: provider only
  - [ ] Breadcrumb navigation

## 12. Testing - Backend

- [ ] 12.1 Unit tests for Staff entity
  - [ ] Test UpdateProfilePhoto method
  - [ ] Test UpdateBiography with length validation
  - [ ] Test UpdateNotes with length validation
  - [ ] Test Reactivate method

- [ ] 12.2 Unit tests for Commands/Queries
  - [ ] Test AddStaffToProviderCommand with new fields
  - [ ] Test UpdateProviderStaffCommand with new fields
  - [ ] Test ActivateProviderStaffCommand
  - [ ] Test UploadStaffPhotoCommand
  - [ ] Test AssignServicesToStaffCommand
  - [ ] Test UpdateStaffScheduleCommand
  - [ ] Test GetStaffByIdQuery

- [ ] 12.3 Integration tests for API
  - [ ] Test GET /staff/{id} endpoint
  - [ ] Test POST /staff with new fields
  - [ ] Test PUT /staff/{id} with new fields
  - [ ] Test POST /staff/{id}/photo upload
  - [ ] Test POST /staff/{id}/activate
  - [ ] Test PUT /staff/{id}/services
  - [ ] Test PUT /staff/{id}/schedule
  - [ ] Test validation errors
  - [ ] Test authorization (CanManageProvider)

## 13. Testing - Frontend

- [ ] 13.1 Unit tests for components
  - [ ] Test StaffForm validation logic
  - [ ] Test ImageCropper functionality
  - [ ] Test RoleSelector component
  - [ ] Test ServiceAssignmentModal
  - [ ] Test StaffScheduleEditor
  - [ ] Test DeactivateStaffModal
  - [ ] Test ActivateStaffModal

- [ ] 13.2 Unit tests for store
  - [ ] Test loadStaffById action
  - [ ] Test uploadStaffPhoto action
  - [ ] Test activateStaff action
  - [ ] Test enhanced filtering logic
  - [ ] Test search functionality

- [ ] 13.3 E2E tests
  - [ ] Test complete staff creation flow with all fields
  - [ ] Test staff editing with photo upload
  - [ ] Test staff deactivation workflow
  - [ ] Test staff reactivation workflow
  - [ ] Test service assignment flow
  - [ ] Test schedule management flow
  - [ ] Test filtering and search

## 14. Documentation

- [ ] 14.1 Update API documentation
  - [ ] Document all new endpoints
  - [ ] Add request/response examples
  - [ ] Document validation rules

- [ ] 14.2 Update frontend documentation
  - [ ] Document new components
  - [ ] Document store actions
  - [ ] Add usage examples

- [ ] 14.3 Create user guide
  - [ ] How to add staff with full profile
  - [ ] How to assign services
  - [ ] How to manage schedules
  - [ ] How to deactivate/reactivate staff

## 15. Validation and Cleanup

- [ ] 15.1 Code review
  - [ ] Review all new backend code
  - [ ] Review all new frontend code
  - [ ] Check for code duplication
  - [ ] Verify error handling

- [ ] 15.2 Manual testing
  - [ ] Test complete CRUD flows
  - [ ] Test photo upload with various file types
  - [ ] Test form validation edge cases
  - [ ] Test filtering and search combinations
  - [ ] Test deactivate/reactivate workflows
  - [ ] Test service assignment
  - [ ] Test schedule management

- [ ] 15.3 Performance testing
  - [ ] Test with large number of staff (100+)
  - [ ] Test photo upload with large files
  - [ ] Test list rendering performance
  - [ ] Optimize queries if needed

- [ ] 15.4 Accessibility testing
  - [ ] Test keyboard navigation
  - [ ] Test screen reader compatibility
  - [ ] Verify ARIA labels
  - [ ] Check color contrast

- [ ] 15.5 Cross-browser testing
  - [ ] Test in Chrome, Firefox, Safari, Edge
  - [ ] Test responsive layouts on mobile
  - [ ] Verify photo upload on all browsers

## 16. Deployment

- [ ] 16.1 Database migration
  - [ ] Run migrations in development
  - [ ] Run migrations in staging
  - [ ] Run migrations in production
  - [ ] Verify data integrity

- [ ] 16.2 Deploy backend
  - [ ] Deploy to staging environment
  - [ ] Run smoke tests
  - [ ] Deploy to production
  - [ ] Monitor for errors

- [ ] 16.3 Deploy frontend
  - [ ] Build production bundle
  - [ ] Deploy to staging
  - [ ] Run E2E tests
  - [ ] Deploy to production
  - [ ] Monitor for errors

- [ ] 16.4 Post-deployment verification
  - [ ] Verify all staff CRUD operations work
  - [ ] Verify photo uploads work
  - [ ] Verify filtering and search work
  - [ ] Check logs for errors
  - [ ] Monitor performance metrics
