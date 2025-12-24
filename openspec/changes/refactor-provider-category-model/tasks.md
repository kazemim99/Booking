# Implementation Tasks - Provider Category Model Refactoring

> **STATUS UPDATE (2025-12-23)**: Backend implementation COMPLETE ✅
> - Phases 1-5: Backend complete, builds successfully with 0 errors
> - Phases 6-8: Frontend pending
> - Phases 9-11: Testing & deployment pending
> - See `COMPLETION_STATUS.md` for detailed completion report

## Phase 1: Domain Model & Enum (Backend) ✅ COMPLETE

### 1.1 Create ServiceCategory Enum
- [ ] 1.1.1 Create `Booksy.ServiceCatalog.Domain.Enums.ServiceCategory.cs` with all 15 category values
- [ ] 1.1.2 Assign explicit integer IDs to each enum value (1-15)
- [ ] 1.1.3 Add XML documentation comments for each category
- [ ] 1.1.4 Create unit tests for enum completeness

### 1.2 Create ServiceCategory Extension Methods
- [ ] 1.2.1 Create `ServiceCategoryExtensions.cs` in Domain/Extensions
- [ ] 1.2.2 Implement `ToPersianName()` extension method with all mappings
- [ ] 1.2.3 Implement `ToIcon()` extension method with emoji icons
- [ ] 1.2.4 Implement `ToColorHex()` extension method with brand colors
- [ ] 1.2.5 Implement `ToSlug()` extension method for URL-friendly names
- [ ] 1.2.6 Add unit tests for all extension methods
- [ ] 1.2.7 Add integration test verifying all enum values have metadata

### 1.3 Update Provider Aggregate
- [ ] 1.3.1 Add `PrimaryCategory` property to Provider.cs (ServiceCategory type)
- [ ] 1.3.2 Remove `ProviderType` property from Provider.cs
- [ ] 1.3.3 Update `CreateDraft()` factory method to require ServiceCategory parameter
- [ ] 1.3.4 Update Provider constructor to initialize PrimaryCategory
- [ ] 1.3.5 Add domain validation: PrimaryCategory must be valid enum value
- [ ] 1.3.6 Update Provider unit tests

### 1.4 Update Service Aggregate
- [ ] 1.4.1 Change `Service.Category` from value object to enum type
- [ ] 1.4.2 Update `Create()` factory method to accept ServiceCategory enum
- [ ] 1.4.3 Update Service.UpdateCategory() method (if exists)
- [ ] 1.4.4 Update Service unit tests
- [ ] 1.4.5 Remove old ServiceCategory value object file (already commented out)

### 1.5 Update Domain Events
- [ ] 1.5.1 Update `ProviderRegisteredEvent` to include ServiceCategory instead of ProviderType
- [ ] 1.5.2 Update event handlers that consume ProviderType
- [ ] 1.5.3 Update integration events for cross-context communication

## Phase 2: Database Migration

### 2.1 Create Migration Script
- [ ] 2.1.1 Generate EF Core migration: `Add-Migration RefactorProviderCategoryModel`
- [ ] 2.1.2 Add `providers.primary_category` column (INT, nullable initially)
- [ ] 2.1.3 Create index on `providers.primary_category`
- [ ] 2.1.4 Modify `services.category` column type (varchar → int)
- [ ] 2.1.5 Add migration data scripts (see 2.2)
- [ ] 2.1.6 Add rollback script to reverse all changes

### 2.2 Data Migration Logic
- [ ] 2.2.1 Write SQL to infer PrimaryCategory from existing services
- [ ] 2.2.2 Write SQL to fallback to ProviderType → ServiceCategory mapping
- [ ] 2.2.3 Write SQL to default remaining providers to HairSalon
- [ ] 2.2.4 Write SQL to convert Service.Category strings to enum integers
- [ ] 2.2.5 Verify no NULL values remain in primary_category
- [ ] 2.2.6 Make `providers.primary_category` non-nullable

### 2.3 Migration Testing
- [ ] 2.3.1 Create staging database clone with production data
- [ ] 2.3.2 Run migration on staging database
- [ ] 2.3.3 Generate category assignment report (provider count per category)
- [ ] 2.3.4 Manually review 50 random providers for correct categorization
- [ ] 2.3.5 Test rollback script on staging
- [ ] 2.3.6 Performance test: verify queries <50ms p95

### 2.4 Cleanup Old Schema
- [ ] 2.4.1 Drop `providers.type` column (after 30-day backup period)
- [ ] 2.4.2 Remove old indexes on `type` column
- [ ] 2.4.3 Update database documentation

## Phase 3: Infrastructure & Persistence (Backend)

### 3.1 Update EF Core Configuration
- [ ] 3.1.1 Update `ProviderConfiguration.cs` to map PrimaryCategory enum
- [ ] 3.1.2 Remove ProviderType mapping
- [ ] 3.1.3 Update `ServiceConfiguration.cs` for Category enum mapping
- [ ] 3.1.4 Add value converter for ServiceCategory enum if needed
- [ ] 3.1.5 Update seed data in ProviderSeeder.cs
- [ ] 3.1.6 Update seed data in ServiceSeeder.cs

### 3.2 Update Repositories
- [ ] 3.2.1 Update `ProviderReadRepository` queries using ProviderType
- [ ] 3.2.2 Add `GetByCategory()` query method
- [ ] 3.2.3 Update `ServiceReadRepository` for enum Category
- [ ] 3.2.4 Update repository tests

### 3.3 Update Specifications
- [ ] 3.3.1 Update `ProvidersByLocationSpecification` to filter by ServiceCategory
- [ ] 3.3.2 Remove ProviderType filtering
- [ ] 3.3.3 Update `ServiceByProviderSpecification` for enum Category
- [ ] 3.3.4 Add specification tests

## Phase 4: Application Layer (Backend)

### 4.1 Update Commands
- [ ] 4.1.1 Update `RegisterProviderCommand` to accept ServiceCategory
- [ ] 4.1.2 Remove ProviderType parameter from registration commands
- [ ] 4.1.3 Update `CreateProviderDraftCommand`
- [ ] 4.1.4 Update `UpdateProviderCommand` (if category change allowed)
- [ ] 4.1.5 Update command validators
- [ ] 4.1.6 Update command handler tests

### 4.2 Update Queries
- [ ] 4.2.1 Update `GetProviderProfileQuery` to return ServiceCategory
- [ ] 4.2.2 Update `SearchProvidersQuery` to filter by ServiceCategory
- [ ] 4.2.3 Update `GetProvidersByLocationQuery`
- [ ] 4.2.4 Update query result DTOs (ProviderDto, ProviderSummaryDto, etc.)
- [ ] 4.2.5 Update query handler tests

### 4.3 Update DTOs
- [ ] 4.3.1 Replace `ProviderType` with `ServiceCategory` in all DTOs
- [ ] 4.3.2 Update `ProviderDto.cs`
- [ ] 4.3.3 Update `ProviderSummaryDto.cs`
- [ ] 4.3.4 Update `ProviderProfileViewModel.cs`
- [ ] 4.3.5 Add category metadata to response DTOs (name, icon, color)

### 4.4 Update Application Services
- [ ] 4.4.1 Update `ProviderApplicationService` to use ServiceCategory
- [ ] 4.4.2 Update `ServiceApplicationService` for enum Category
- [ ] 4.4.3 Update service interface contracts
- [ ] 4.4.4 Update application service tests

## Phase 5: API Layer (Backend)

### 5.1 Update Controllers
- [ ] 5.1.1 Update `ProvidersController` registration endpoints
- [ ] 5.1.2 Update request models (remove ProviderType, add ServiceCategory)
- [ ] 5.1.3 Update response models
- [ ] 5.1.4 Update `ServicesController` for enum Category
- [ ] 5.1.5 Update Swagger/OpenAPI annotations
- [ ] 5.1.6 Add API documentation for ServiceCategory enum

### 5.2 Create Category Endpoints
- [ ] 5.2.1 Create `GET /api/v1/categories` endpoint (list all categories)
- [ ] 5.2.2 Return category metadata (ID, name, icon, color, slug)
- [ ] 5.2.3 Add `GET /api/v1/categories/{id}/providers` endpoint
- [ ] 5.2.4 Add category count aggregation
- [ ] 5.2.5 Add integration tests for category endpoints

### 5.3 Update Request/Response Models
- [ ] 5.3.1 Create `ServiceCategoryResponse.cs` model
- [ ] 5.3.2 Update `RegisterProviderRequest` with ServiceCategory
- [ ] 5.3.3 Update `ProviderResponse` model
- [ ] 5.3.4 Update `CreateServiceRequest` with ServiceCategory enum
- [ ] 5.3.5 Add model validation attributes

## Phase 6: Frontend TypeScript Types

### 6.1 Define Category Types
- [ ] 6.1.1 Create `src/shared/types/service-category.ts` with enum definition
- [ ] 6.1.2 Match enum integer values exactly with backend
- [ ] 6.1.3 Create `ServiceCategoryMetadata` interface
- [ ] 6.1.4 Create category metadata constant objects
- [ ] 6.1.5 Export category helper functions

### 6.2 Update Provider Types
- [ ] 6.2.1 Update `src/shared/types/provider.types.ts`
- [ ] 6.2.2 Remove `ProviderType` type/enum
- [ ] 6.2.3 Add `primaryCategory: ServiceCategory` to Provider interface
- [ ] 6.2.4 Update ProviderHierarchyType import/usage
- [ ] 6.2.5 Update Service interface with ServiceCategory enum

### 6.3 Create Category Constants
- [ ] 6.3.1 Create `src/shared/constants/categories.ts`
- [ ] 6.3.2 Define CATEGORY_METADATA constant with all category info
- [ ] 6.3.3 Export helper functions: `getCategoryName()`, `getCategoryIcon()`, etc.
- [ ] 6.3.4 Add category color palette constants

## Phase 7: Frontend Registration Flow

### 7.1 Update CategorySelectionStep Component
- [ ] 7.1.1 Update `CategorySelectionStep.vue` categories array
- [ ] 7.1.2 Map category IDs to ServiceCategory enum values
- [ ] 7.1.3 Update category display with metadata from constants
- [ ] 7.1.4 Update `selectCategory()` to emit ServiceCategory enum value
- [ ] 7.1.5 Update validation logic
- [ ] 7.1.6 Add unit tests for category selection

### 7.2 Update Registration Store
- [ ] 7.2.1 Update `src/modules/provider/stores/registration.store.ts`
- [ ] 7.2.2 Change `category` state property type to ServiceCategory
- [ ] 7.2.3 Update registration submission to send enum value
- [ ] 7.2.4 Update store tests

### 7.3 Update Registration Service
- [ ] 7.3.1 Update `src/modules/provider/services/registration.service.ts`
- [ ] 7.3.2 Update API request payload with ServiceCategory enum
- [ ] 7.3.3 Handle category in response DTOs
- [ ] 7.3.4 Update service tests

### 7.4 Update Registration Flow Validation
- [ ] 7.4.1 Update step validation to require valid ServiceCategory
- [ ] 7.4.2 Add error messages in Persian
- [ ] 7.4.3 Update progress persistence
- [ ] 7.4.4 Add integration tests for full registration flow

## Phase 8: Frontend Provider Profile & Search

### 8.1 Update Provider Profile Display
- [ ] 8.1.1 Update provider profile components to display category badge
- [ ] 8.1.2 Add category icon + name display
- [ ] 8.1.3 Update provider card components
- [ ] 8.1.4 Add category color theming
- [ ] 8.1.5 Update profile page tests

### 8.2 Update Search & Filter Components
- [ ] 8.2.1 Create category filter component
- [ ] 8.2.2 Update search page with category filter chips
- [ ] 8.2.3 Update search service to filter by ServiceCategory
- [ ] 8.2.4 Add category-based provider listing pages
- [ ] 8.2.5 Update search tests

### 8.3 Update Provider Service
- [ ] 8.3.1 Update `src/modules/provider/services/provider.service.ts`
- [ ] 8.3.2 Handle ServiceCategory in API responses
- [ ] 8.3.3 Update provider list/search methods
- [ ] 8.3.4 Add category aggregation queries
- [ ] 8.3.5 Update service tests

## Phase 9: Testing & Quality Assurance

### 9.1 Backend Tests
- [ ] 9.1.1 Write unit tests for ServiceCategory enum and extensions
- [ ] 9.1.2 Write unit tests for updated Provider aggregate
- [ ] 9.1.3 Write unit tests for updated Service aggregate
- [ ] 9.1.4 Write integration tests for registration with category
- [ ] 9.1.5 Write integration tests for category-based search
- [ ] 9.1.6 Achieve >80% code coverage for new/changed code

### 9.2 Frontend Tests
- [ ] 9.2.1 Write unit tests for category constants and helpers
- [ ] 9.2.2 Write component tests for CategorySelectionStep
- [ ] 9.2.3 Write integration tests for registration flow
- [ ] 9.2.4 Write E2E tests for category selection and profile display
- [ ] 9.2.5 Test category filter functionality
- [ ] 9.2.6 Achieve >70% code coverage for new/changed code

### 9.3 Manual QA Testing
- [ ] 9.3.1 Test provider registration with each category
- [ ] 9.3.2 Test category persistence across registration steps
- [ ] 9.3.3 Test provider search by category
- [ ] 9.3.4 Test category display on provider profiles
- [ ] 9.3.5 Test backward navigation and category change
- [ ] 9.3.6 Test mobile responsive design for category selection
- [ ] 9.3.7 Test RTL layout for Persian category names

### 9.4 Performance Testing
- [ ] 9.4.1 Benchmark provider search with category filter (<50ms p95)
- [ ] 9.4.2 Benchmark category aggregation queries (<100ms p95)
- [ ] 9.4.3 Load test registration endpoint with category
- [ ] 9.4.4 Verify database index usage with EXPLAIN plans

## Phase 10: Deployment & Monitoring

### 10.1 Pre-Deployment
- [ ] 10.1.1 Review all code changes in PR
- [ ] 10.1.2 Run full test suite (backend + frontend)
- [ ] 10.1.3 Generate deployment checklist
- [ ] 10.1.4 Prepare rollback plan
- [ ] 10.1.5 Schedule deployment window (low traffic)

### 10.2 Database Migration Deployment
- [ ] 10.2.1 Backup production database
- [ ] 10.2.2 Run migration script (zero-downtime)
- [ ] 10.2.3 Verify migration success (all providers have category)
- [ ] 10.2.4 Generate category distribution report
- [ ] 10.2.5 Monitor database performance after migration

### 10.3 Backend Deployment
- [ ] 10.3.1 Deploy backend services to staging
- [ ] 10.3.2 Smoke test staging environment
- [ ] 10.3.3 Deploy to production (rolling deployment)
- [ ] 10.3.4 Monitor error rates and response times
- [ ] 10.3.5 Verify registration API works with new category

### 10.4 Frontend Deployment
- [ ] 10.4.1 Deploy frontend to staging
- [ ] 10.4.2 Test registration flow on staging
- [ ] 10.4.3 Deploy to production with feature flag
- [ ] 10.4.4 Gradual rollout: 10% → 50% → 100%
- [ ] 10.4.5 Monitor frontend errors and user behavior

### 10.5 Post-Deployment Monitoring
- [ ] 10.5.1 Monitor registration completion rate (target: >70%)
- [ ] 10.5.2 Monitor category distribution (expect: balanced)
- [ ] 10.5.3 Monitor search performance (target: <50ms p95)
- [ ] 10.5.4 Monitor error rates (target: <1% increase)
- [ ] 10.5.5 Review support tickets for category-related issues
- [ ] 10.5.6 Collect user feedback on category selection UX

## Phase 11: Cleanup & Documentation

### 11.1 Code Cleanup
- [ ] 11.1.1 Delete ProviderType enum file
- [ ] 11.1.2 Delete ServiceCategory value object file (confirmed deleted)
- [ ] 11.1.3 Remove dead code referencing ProviderType
- [ ] 11.1.4 Update XML documentation comments
- [ ] 11.1.5 Run code formatter and linter

### 11.2 Documentation
- [ ] 11.2.1 Update API documentation with ServiceCategory enum
- [ ] 11.2.2 Update developer README with category model explanation
- [ ] 11.2.3 Document category selection mapping (frontend ↔ backend)
- [ ] 11.2.4 Update database schema documentation
- [ ] 11.2.5 Create migration guide for developers

### 11.3 Admin Tools (Optional)
- [ ] 11.3.1 Create admin UI to view category distribution
- [ ] 11.3.2 Create admin UI to change provider category (with approval)
- [ ] 11.3.3 Create category analytics dashboard
- [ ] 11.3.4 Add alerts for category imbalance

### 11.4 Future Enhancements Documentation
- [ ] 11.4.1 Document Phase 2: Service category alignment validation
- [ ] 11.4.2 Document Phase 3: Category-specific service templates
- [ ] 11.4.3 Document Phase 4: Subcategory/tag system
- [ ] 11.4.4 Document Phase 5: Category-based analytics

## Estimated Timeline

- **Phase 1-2**: Week 1 (Domain model + Database)
- **Phase 3-5**: Week 1-2 (Backend implementation)
- **Phase 6-8**: Week 2-3 (Frontend implementation)
- **Phase 9**: Week 3 (Testing)
- **Phase 10**: Week 3-4 (Deployment)
- **Phase 11**: Week 4 (Cleanup & Documentation)

**Total**: 3-4 weeks for full implementation
