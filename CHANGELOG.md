# Changelog

All notable changes to the Booksy project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
