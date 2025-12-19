# Changelog

All notable changes to the Booksy project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added - 2025-11-17

#### Split Login Pages & Authentication Improvements
- **Added**: Separate login pages for customers and providers
  - Customer login at `/login` with customer-specific messaging
  - Provider login at `/provider/login` with business-focused messaging
  - "For Businesses" link in footer for provider portal discoverability
- **Improved**: User type handling with explicit route query params instead of sessionStorage
- **Removed**: 35+ lines of complex redirect-path detection logic
- **Files**:
  - `booksy-frontend/src/modules/auth/views/ProviderLoginView.vue` (new)
  - `booksy-frontend/src/modules/auth/views/LoginView.vue`
  - `booksy-frontend/src/modules/auth/views/VerificationView.vue`
  - `booksy-frontend/src/core/router/routes/auth.routes.ts`
  - `booksy-frontend/src/shared/components/layout/Footer/AppFooter.vue`

#### Multiple Service Selection in Booking Flow
- **Added**: Multi-select functionality for booking multiple services in one appointment
  - Toggle behavior for selecting/deselecting services
  - Real-time total price and duration calculation
  - Selected services summary with count and totals
  - Visual feedback with checkmarks and highlighting
- **Updated**: BookingWizard to handle service arrays instead of single service
- **Added**: `confirmationData` computed property to transform multi-service data
- **Files**:
  - `booksy-frontend/src/modules/booking/components/ServiceSelection.vue`
  - `booksy-frontend/src/modules/booking/components/BookingWizard.vue`

#### Persian Calendar Integration
- **Added**: Jalali (Persian/Solar Hijri) calendar support using `vue3-persian-datetime-picker`
  - Persian month names and weekday display
  - Gregorian to Jalali date conversion algorithm
  - Persian number formatting for dates
  - Enhanced calendar styling with larger size (min-height: 400px)
- **Improved**: Date display format showing "یکشنبه، ۲۵ دی ۱۴۰۲" in time slots section
- **Added**: Null check for date string to prevent runtime errors
- **Files**:
  - `booksy-frontend/src/modules/booking/components/SlotSelection.vue`

### Fixed - 2025-11-17

#### Booking Confirmation Page Display
- **Fixed**: Empty/white space issue on confirmation page after multi-service implementation
  - Root cause: Data structure mismatch between BookingWizard and BookingConfirmation
  - Solution: Added transformation layer to convert service array to expected format
  - Combines multiple service names with comma separator
  - Aggregates total price and duration
- **Files**:
  - `booksy-frontend/src/modules/booking/components/BookingWizard.vue`

#### Booking API Request Format
- **Fixed**: Frontend-backend interface mismatch in CreateBookingRequest
  - Updated frontend interface to match backend `CreateBookingRequest.cs`
  - Changed from `{endTime, notes, totalAmount}` to `{staffId, customerNotes}`
  - Backend now calculates endTime and price from service definition
- **Fixed**: Submit booking errors accessing non-existent properties
  - Fixed access to `serviceDuration`, `serviceId`, `servicePrice` that don't exist
  - Now correctly uses `services[0]` for first service data
  - Added TODO for backend multi-service support
- **Files**:
  - `booksy-frontend/src/modules/booking/api/booking.service.ts`
  - `booksy-frontend/src/modules/booking/components/BookingWizard.vue`

#### Timezone Handling in Booking Validation
- **Fixed**: "Cannot book appointments in the past" error for valid future bookings
  - Root cause: Frontend sends DateTime without timezone info (DateTimeKind.Unspecified)
  - Backend was comparing unspecified DateTime with DateTime.UtcNow causing ~7.8 hour offset
  - Calculation `(startTime - DateTime.UtcNow).TotalHours` returned negative values like -7.797
- **Solution**: Explicit timezone conversion in `ValidateBookingConstraintsAsync`
  - Converts DateTimeKind.Unspecified to UTC using `DateTime.SpecifyKind`
  - Converts DateTimeKind.Local to UTC using `ToUniversalTime`
  - Applied same logic in `GenerateTimeSlotsForStaffAsync` for slot filtering
- **Impact**: All DateTime comparisons now use consistent UTC timezone
- **Files**:
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Services/AvailabilityService.cs`

### Documentation - 2025-11-17
- **Added**: `openspec/changes/split-login-pages/IMPLEMENTATION_SUMMARY.md` - Complete implementation summary
- **Added**: `docs/BOOKING_FLOW_IMPROVEMENTS.md` - Technical documentation for booking enhancements
- **Updated**: `AUTHENTICATION_FLOW_DOCUMENTATION.md` - Added section on separate login pages
- **Updated**: `CHANGELOG.md` - Documented all recent changes

### Added - 2025-11-16

#### Database Migrations & Schema Updates

##### ServiceCatalog Database Migrations
- **Added**: Complete EF Core migration system for ServiceCatalog bounded context
- **Migration**: `20251115202010_InitialCreate` - Created ProviderAvailability and Reviews tables
- **Tables Created**:
  - `ProviderAvailability` - Time slot management for provider availability calendar
    - Columns: AvailabilityId, ProviderId, StaffId, Date, StartTime, EndTime, Status, BookingId, BlockReason, HoldExpiresAt
    - Indexes: Optimized for date-based queries, booking lookups, and hold expiration cleanup
    - Supports: Available, Booked, Blocked, TentativeHold, Break statuses
  - `Reviews` - Customer review and rating system
    - Columns: ReviewId, ProviderId, CustomerId, BookingId, RatingValue (1-5), Comment, IsVerified, ProviderResponse, HelpfulCount, NotHelpfulCount
    - Indexes: Provider reviews, customer reviews, verified reviews, rating-based queries
    - Features: Provider responses, helpful voting, verified review tracking
- **Updated**: Payments table column types for PostgreSQL compatibility
- **Files**:
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Migrations/20251115202010_InitialCreate.cs`
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Migrations/ServiceCatalogDbContextModelSnapshot.cs`

##### Provider Availability System
- **Added**: Complete domain model for provider availability management
- **Aggregate**: `ProviderAvailability` with business logic for slot management
- **Features**:
  - Create available time slots with date, start/end times
  - Mark slots as booked with booking references
  - Block time slots with optional reasons
  - Tentative hold with expiration (for booking flow)
  - Release slots back to available status
  - Conflict detection between overlapping slots
- **Query Support**: Get availability by date range, status, and provider
- **Files**:
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderAvailabilityAggregate/ProviderAvailability.cs`
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Repositories/IProviderAvailabilityReadRepository.cs`
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/ProviderAvailabilityConfiguration.cs`

##### Review & Rating System
- **Added**: Complete domain model for customer reviews
- **Aggregate**: `Review` with business logic for rating management
- **Features**:
  - Create verified reviews tied to actual bookings
  - Rating system (1.0-5.0 with half-star increments)
  - Provider response capability
  - Helpful/Not Helpful voting
  - Review statistics and aggregation
- **Files**:
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/Review.cs`
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/ReviewConfiguration.cs`

#### API Enhancements

##### Provider Availability API
- **Added**: REST endpoint for provider availability calendar
- **Endpoint**: `GET /api/v1/providers/{providerId}/availability`
- **Query Parameters**:
  - `startDate` (optional, yyyy-MM-dd format) - Defaults to today
  - `days` (required, 7/14/30) - Number of days to fetch
- **Response**:
  - Time slots grouped by day with status (Available/Booked/Blocked)
  - Availability heatmap with statistics for UI visualization
  - Performance optimized with indexed queries
- **Features**:
  - Rate limiting to prevent abuse
  - Anonymous access for public availability viewing
  - Validation of date format and days parameter
- **Files**:
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/ProviderAvailabilityController.cs`
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Queries/Provider/GetProviderAvailabilityCalendar/`

##### API Error Response Standardization
- **Fixed**: ApiErrorResponse implementation in ServiceCatalog API
- **Added**: Unified error response format for consistent client-side error handling
- **Structure**:
  - Success flag
  - Error list with code, message, and optional field reference
  - Optional trace ID for debugging
- **Files**:
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Models/Responses/ApiErrorResponse.cs`

### Fixed - 2025-11-16

#### Backend Compilation Fixes

##### Specification Pattern Implementation
- **Fixed**: Missing Specification base class in Core.Domain
- **Added**: `Specification<T>` abstract base class implementing `ISpecification<T>`
- **Features**: Expression-based query composition, And/Or/Not combinators, Include support
- **Impact**: Enables clean query abstraction for complex filtering scenarios
- **Files**: `src/Core/Booksy.Core.Domain/Abstractions/Entities/Specifications/Specification.cs`

##### Namespace Conflict Resolution
- **Fixed**: Booking class namespace conflicts using type aliases
- **Pattern**: `using BookingAggregate = Booksy.Booking.Domain.Aggregates.Booking;`
- **Impact**: Resolves ambiguity between Booking namespace and Booking class
- **Files**: Multiple files across Booking.Domain, Booking.Application, Booking.Infrastructure

##### Repository Pattern Refinement
- **Fixed**: Separated read and write repository concerns
- **Pattern**:
  - Read repositories: `IBookingReadRepository` with query methods (GetByIdAsync, ListAsync, etc.)
  - Write repositories: `IBookingWriteRepository` with persistence methods (SaveAsync, UpdateAsync, DeleteAsync)
- **Base Classes**:
  - `EfReadRepositoryBase<TEntity, TId, TContext>` for query operations
  - `EfWriteRepositoryBase<TEntity, TId, TContext>` for persistence operations
- **Impact**: Cleaner separation of concerns, better scalability for CQRS
- **Files**: All repository implementations in Booking and ServiceCatalog bounded contexts

##### Command/Query Fixes
- **Fixed**: Missing IdempotencyKey property on commands
- **Pattern**: Added `public Guid? IdempotencyKey { get; init; }` to all command classes
- **Impact**: Enables proper idempotent command handling
- **Files**: All command classes in Booking.Application and ServiceCatalog.Application

##### DayOfWeek Enum Ambiguity
- **Fixed**: Conflicts between System.DayOfWeek and Domain.DayOfWeek
- **Pattern**: Type alias `using SystemDayOfWeek = System.DayOfWeek;` + mapper methods
- **Impact**: Resolves compilation errors in seeders and query handlers
- **Files**:
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Queries/Provider/GetProviderAvailabilityCalendar/GetProviderAvailabilityCalendarQueryHandler.cs`
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Seeders/AvailabilitySeeder.cs`

##### Database Context Improvements
- **Fixed**: Removed invalid audit field setter logic from DbContext.SaveChangesAsync
- **Issue**: IAuditableEntity setters are inaccessible (private/protected)
- **Impact**: DbContext no longer attempts to set audit fields directly
- **Files**: `src/BoundedContexts/Booking/Booksy.Booking.Infrastructure/Persistence/Context/BookingDbContext.cs`

##### Pagination Request Fixes
- **Fixed**: Wrong pagination class usage in API controllers
- **Pattern**: Use `PaginationRequest.Create(pageNumber, pageSize)` from Core.Application.DTOs
- **Impact**: Consistent pagination across all API endpoints
- **Files**: Booking.Api and ServiceCatalog.Api controllers

##### Invalid Migration Cleanup
- **Removed**: Broken migration `20251109070253_AddBookingSystem2`
- **Issue**: Migration tried to add column to non-existent table
- **Impact**: Database migration process now works cleanly
- **Files**: Removed from `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Migrations/`

#### Build Status
- **Result**: ✅ Entire solution builds successfully with 0 compilation errors
- **Verification**: All bounded contexts (Booking, ServiceCatalog, UserManagement) compile cleanly
- **Database**: All migrations applied successfully to ServiceCatalog database

### Fixed - 2025-11-11

#### Registration Flow Fixes

##### Gallery Image Submission During Registration
- **Fixed**: Gallery images now properly submit to backend during Step 7 of registration flow
- **Issue**: `saveGallery()` function was a no-op that didn't call the registration API endpoint
- **Impact**: Images uploaded during registration were not being saved
- **Files**: `booksy-frontend/src/modules/provider/composables/useProviderRegistration.ts`
- **Commit**: `e6273aa`

##### CompletionStep UI Distortion
- **Fixed**: Final completion screen (Step 9) UI rendering correctly with proper CSS
- **Issue**: Broken Tailwind CSS escape sequences (`from-primary\/5`, `to-accent\/20`) caused distorted UI
- **Impact**: Unprofessional appearance on the success screen after completing registration
- **Solution**: Rewrote component with semantic scoped CSS, proper gradient backgrounds, and RTL support
- **Files**: `booksy-frontend/src/modules/provider/components/registration/steps/CompletionStep.vue`
- **Commit**: `2cead84`

##### Registration Progress Query After Completion
- **Fixed**: `GetRegistrationProgress` query now works correctly after registration completion
- **Issue**: Query returned "not found" after provider status changed from `Drafted` to `PendingVerification`
- **Impact**: Page refresh after completing registration would fail to load provider data
- **Solution**:
  - Backend: Added fallback to check for completed providers, not just drafted ones
  - Frontend: Added handler for completed registration state
- **Files**:
  - `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Queries/Provider/GetRegistrationProgress/GetRegistrationProgressQueryHandler.cs`
  - `booksy-frontend/src/modules/provider/composables/useProviderRegistration.ts`
- **Commit**: `f4be06d`

##### OptionalFeedbackStep UI Distortion
- **Fixed**: Optional feedback screen (Step 8) UI rendering correctly
- **Issue**: Similar to CompletionStep - broken Tailwind CSS escape sequences
- **Impact**: Distorted UI on the feedback collection step
- **Solution**: Rewrote with proper scoped CSS and RTL support
- **Files**: `booksy-frontend/src/modules/provider/components/registration/steps/OptionalFeedbackStep.vue`
- **Commit**: `d7b8a79`

## Previous Changes

### Added - Gallery Management Feature

#### Backend Implementation
- Created `GalleryImage` entity with proper domain modeling
- Implemented `IFileStorageService` with local file storage
- Implemented `IImageOptimizationService` using ImageSharp (WebP conversion, multiple sizes)
- Added CQRS commands: `UploadGalleryImages`, `DeleteGalleryImage`, `ReorderGalleryImages`
- Added query: `GetGalleryImages`
- Created RESTful API endpoints for gallery management
- Added integration tests for all gallery operations
- Database migrations for `provider_gallery_images` table

#### Frontend Implementation
- Created TypeScript types for gallery management
- Implemented gallery service with progress tracking
- Created Pinia store for state management
- Built reusable components:
  - `GalleryUpload.vue` - Drag-and-drop upload with validation
  - `GalleryImageCard.vue` - Image display with edit/delete actions
  - `GalleryGrid.vue` - Responsive grid with drag-to-reorder
- Created `GalleryViewNew.vue` - Complete gallery management interface
- Integrated gallery upload into registration flow (Step 7)

### Added - Provider Registration Flow (9 Steps)

#### Registration Steps
1. Business Info - Basic business and owner information
2. Category Selection - Business type selection
3. Location - Address and map integration (Neshan Maps)
4. Services - Service catalog configuration
5. Staff - Team member management
6. Working Hours - Business hours and breaks
7. Gallery - Portfolio image uploads
8. Optional Feedback - User experience survey
9. Completion - Success confirmation screen

#### Features
- Progressive registration with draft saving
- Step-by-step validation
- Visual progress indicator
- RTL (Right-to-Left) support for Persian language
- Mobile-responsive design
- Automatic draft recovery on page refresh
- Provider ID persistence across page reloads

### Technical Improvements

#### Architecture
- Domain-Driven Design (DDD) with aggregate roots and entities
- CQRS pattern for commands and queries
- Clean Architecture with proper layer separation
- Repository pattern with Unit of Work
- Dependency Injection for all services

#### Security
- Authorization checks on all mutating endpoints
- File type validation (whitelist: JPG, PNG, WebP)
- File size limits (10MB per file)
- Max image count per provider (50 images)
- Soft delete for data preservation

#### Performance
- Image optimization (WebP conversion, 3 sizes: thumbnail, medium, original)
- Lazy loading for images
- Database indexes on provider_id and display_order
- Optimistic UI updates
- Skeleton loading states

---

## Contributing

When updating this changelog:
1. Add new entries under `[Unreleased]` section
2. Use categories: `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`
3. Include commit hashes for traceability
4. Reference issue/ticket numbers when applicable
5. Keep entries clear and user-focused

## References

- [Provider Registration Spec](/openspec/specs/provider-registration/spec.md)
- [Gallery Management Implementation Summary](/openspec/changes/add-provider-image-gallery/IMPLEMENTATION_SUMMARY.md)
- [Technical Documentation](/TECHNICAL_DOCUMENTATION.md)
