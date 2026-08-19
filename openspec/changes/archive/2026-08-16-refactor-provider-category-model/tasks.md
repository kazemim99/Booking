# Implementation Tasks - Provider Category Model Refactoring

> **STATUS UPDATE (2025-12-23)**: Backend implementation COMPLETE ✅
> - Phases 1-5: Backend complete, builds successfully with 0 errors
> - Phases 6-8: Frontend pending
> - Phases 9-11: Testing & deployment pending
> - See `COMPLETION_STATUS.md` for detailed completion report

## Phase 1: Domain Model & Enum (Backend) ✅ COMPLETE

### 1.1 Create ServiceCategory Enum
- [x] 1.1.1 Create `Booksy.ServiceCatalog.Domain.Enums.ServiceCategory.cs` with all 15 category values
- [x] 1.1.2 Assign explicit integer IDs to each enum value (1-15)
- [x] 1.1.3 Add XML documentation comments for each category
- [x] 1.1.4 Create unit tests for enum completeness

### 1.2 Create ServiceCategory Extension Methods
- [x] 1.2.1 Create `ServiceCategoryExtensions.cs` in Domain/Extensions
- [x] 1.2.2 Implement `ToPersianName()` extension method with all mappings
- [x] 1.2.3 Implement `ToIcon()` extension method with emoji icons
- [x] 1.2.4 Implement `ToColorHex()` extension method with brand colors
- [x] 1.2.5 Implement `ToSlug()` extension method for URL-friendly names
- [x] 1.2.6 Add unit tests for all extension methods
- [x] 1.2.7 Add integration test verifying all enum values have metadata

### 1.3 Update Provider Aggregate
- [x] 1.3.1 Add `PrimaryCategory` property to Provider.cs (ServiceCategory type)
- [x] 1.3.2 Remove `ProviderType` property from Provider.cs
- [x] 1.3.3 Update `CreateDraft()` factory method to require ServiceCategory parameter
- [x] 1.3.4 Update Provider constructor to initialize PrimaryCategory
- [x] 1.3.5 Add domain validation: PrimaryCategory must be valid enum value
- [x] 1.3.6 Update Provider unit tests

### 1.4 Update Service Aggregate
- [x] 1.4.1 Change `Service.Category` from value object to enum type
- [x] 1.4.2 Update `Create()` factory method to accept ServiceCategory enum
- [x] 1.4.3 Update Service.UpdateCategory() method (if exists) — the aggregate exposes
  `UpdateBasicInfo(name, description, category)` rather than a dedicated `UpdateCategory()`;
  the enum guard was added there.
- [x] 1.4.4 Update Service unit tests
- [x] 1.4.5 Remove old ServiceCategory value object file (already commented out)

### 1.5 Update Domain Events
- [x] 1.5.1 Update `ProviderRegisteredEvent` to include ServiceCategory instead of ProviderType
- [x] 1.5.2 Update event handlers that consume ProviderType
- [x] 1.5.3 Update integration events for cross-context communication — the UserManagement-side
  `ProviderRegisteredIntegrationEvent` DTO still declared `ProviderType`, which no longer matched
  the `PrimaryCategory` property the publisher serialises; renamed so the contract lines up.

## Phase 2: Database Migration

### 2.1 Create Migration Script
- [x] 2.1.1 Generate EF Core migration — shipped as `20251223143438_RemoveStaff` (schema) plus
  `20260815222542_BackfillProviderPrimaryCategory` (data remediation + constraints)
- [x] 2.1.2 Add `Providers.PrimaryCategory` column — landed as INT NOT NULL DEFAULT 0 rather than
  nullable-then-tightened; see 2.2 for the consequences and the remediation
- [x] 2.1.3 Create index on `Providers.PrimaryCategory` (`IX_Providers_PrimaryCategory`)
- [x] 2.1.4 Modify `Services.Category` column type (varchar → int)
- [x] 2.1.5 Add migration data scripts (see 2.2)
- [x] 2.1.6 Add rollback script to reverse all changes — `Down()` on both migrations; verified locally

### 2.2 Data Migration Logic
> `RemoveStaff` added both category columns with `DEFAULT 0` and dropped `Providers.ProviderType`
> /`Services.CategoryName` in the same step. Zero is not a ServiceCategory member, so every
> pre-existing row was left holding a category that throws in the display-metadata lookups.
> `BackfillProviderPrimaryCategory` is the remediation.
- [x] 2.2.1 Write SQL to infer PrimaryCategory from existing services — most-frequent valid
  `Services.Category` per provider, ties broken on lowest category id for determinism
- [x] 2.2.2 ~~Write SQL to fallback to ProviderType → ServiceCategory mapping~~ — **not applicable**.
  `RemoveStaff` drops `Providers.ProviderType`, and it always runs before the remediation, so there is
  no legacy value left to map. Documented in the migration's summary comment.
- [x] 2.2.3 Write SQL to default remaining providers to HairSalon
- [x] 2.2.4 Write SQL to convert Service.Category values to enum integers — type conversion in
  `RemoveStaff`; stranded `0` values inherit their provider's category in the remediation
- [x] 2.2.5 Verify no invalid values remain — enforced by the `CK_*_Assigned` CHECK constraints, which
  Postgres validates against every existing row, so the migration fails loudly rather than silently
- [x] 2.2.6 Make `Providers.PrimaryCategory` non-nullable — already NOT NULL; the remediation drops the
  misleading `DEFAULT 0` and adds the CHECK constraint

### 2.3 Migration Testing
- [ ] 2.3.1 Create staging database clone with production data — **not done here**: no staging access
      from this environment
- [x] 2.3.2 Run migration — executed against the local dev Postgres inside a rolled-back transaction,
  with all 18 providers deliberately stranded at 0 first: 16 inferred from their services, 2 fell back
  to HairSalon, 0 services left unset, both CHECK constraints then added successfully
- [x] 2.3.3 Generate category assignment report (provider count per category) — produced during the
  test run above; documented in `docs/SERVICE_CATEGORY_MODEL.md`
- [ ] 2.3.4 Manually review 50 random providers for correct categorization — **needs production data**
- [x] 2.3.5 Test rollback script — `Down()` verified locally: constraints dropped, `DEFAULT 0`
  restored, and a subsequent re-run of `Up()` confirmed idempotent
- [ ] 2.3.6 Performance test: verify queries <50ms p95 — **partially**: `EXPLAIN` confirms
  `IX_Providers_PrimaryCategory` is used for category filtering; a p95 load test needs a
  production-sized dataset

### 2.4 Cleanup Old Schema
- [x] 2.4.1 Drop `Providers.ProviderType` column — done in `RemoveStaff`
- [x] 2.4.2 Remove old indexes on `type` column — `IX_Providers_Type` dropped in `RemoveStaff`
- [x] 2.4.3 Update database documentation — `docs/SERVICE_CATEGORY_MODEL.md`

## Phase 3: Infrastructure & Persistence (Backend)

### 3.1 Update EF Core Configuration
- [x] 3.1.1 Update `ProviderConfiguration.cs` to map PrimaryCategory enum
- [x] 3.1.2 Remove ProviderType mapping
- [x] 3.1.3 Update `ServiceConfiguration.cs` for Category enum mapping
- [x] 3.1.4 Add value converter for ServiceCategory enum — `HasConversion<int>()` on both aggregates
- [x] 3.1.5 Update seed data in ProviderSeeder.cs
- [x] 3.1.6 Update seed data in ServiceSeeder.cs

### 3.2 Update Repositories
- [x] 3.2.1 Update `ProviderReadRepository` queries using ProviderType
- [x] 3.2.2 Add `GetByCategory()` query method — the leftover `GetByTypeAsync(ServiceCategory type)`
  was renamed to `GetByCategoryAsync(ServiceCategory category)` across the interface, the repository
  and the cached decorator
- [x] 3.2.3 Update `ServiceReadRepository` for enum Category — **bug fix**: `GetByCategoryAsync` and
  `GetAveragePriceByCategoryAsync` took a `string` and filtered on `s.Category.ToString() == category`,
  which EF cannot translate for a `HasConversion<int>()` column, so both queries failed at runtime.
  They now take a `ServiceCategory` and compare the enum directly (also index-friendly).
  `IServiceApplicationService.GetServicesByCategoryAsync` was retyped to match.
- [x] 3.2.4 Update repository tests — `ProviderCategoryPersistenceTests` (integration)

### 3.3 Update Specifications
- [x] 3.3.1 Update `ProvidersByLocationSpecification` to filter by ServiceCategory
- [x] 3.3.2 Remove ProviderType filtering
- [x] 3.3.3 Update service specifications for enum Category — `GetServicesByProviderSpecification`,
  `SearchServicesSpecification` and `PopularServicesSpecification` all compare the enum directly
- [x] 3.3.4 Add specification tests — covered through the repository/query integration tests above
  and `SearchProvidersSpecification`'s existing category-filter coverage

## Phase 4: Application Layer (Backend)

### 4.1 Update Commands
- [x] 4.1.1 Update `RegisterProviderCommand` to accept ServiceCategory
- [x] 4.1.2 Remove ProviderType parameter from registration commands
- [x] 4.1.3 Update `CreateProviderDraftCommand`
- [x] 4.1.4 Update `UpdateProviderCommand` (if category change allowed) — category changes are
  confined to `UpdateDraftInfo`, which only applies while the provider is still `Drafted`.
  Changing an established provider's category remains an admin-approval concern (spec: "Provider
  category is immutable by default"), so no update command exposes it.
- [x] 4.1.5 Update command validators — `RegisterProviderCommandValidator` uses `IsInEnum()`
- [x] 4.1.6 Update command handler tests — `ServiceCategoryResolverTests` covers the parsing every
  registration handler now shares

### 4.2 Update Queries
- [x] 4.2.1 Update `GetProviderProfileQuery` to return ServiceCategory
- [x] 4.2.2 Update `SearchProvidersQuery` to filter by ServiceCategory — **bug fix**: the query
  declared and logged a `Category` filter but `SearchProvidersQueryHandler` never passed it to
  `SearchProvidersSpecification`, so filtering by category returned the entire catalogue. Covered
  by `CategoriesControllerTests`.
- [x] 4.2.3 Update `GetProvidersByLocationQuery`
- [x] 4.2.4 Update query result DTOs (ProviderDto, ProviderSummaryDto, etc.)
- [x] 4.2.5 Update query handler tests — category query coverage in `CategoriesControllerTests`
  and `ProviderCategoryPersistenceTests`

### 4.3 Update DTOs
- [x] 4.3.1 Replace `ProviderType` with `ServiceCategory` in all DTOs
- [x] 4.3.2 Update `ProviderDto.cs`
- [x] 4.3.3 Update `ProviderSummaryDto.cs`
- [x] 4.3.4 Update `ProviderProfileViewModel.cs`
- [x] 4.3.5 Add category metadata to response DTOs — added to the **category** responses
  (`CategoryWithCountViewModel` now carries `Id`, `Key`, `EnglishName` alongside the existing
  icon/colour/gradient/slug). Deliberately **not** duplicated onto every provider DTO: the
  frontend renders badges from its own metadata table keyed by the enum, so per-provider metadata
  would be payload weight with no consumer.

### 4.4 Update Application Services
- [x] 4.4.1 Update `ProviderApplicationService` to use ServiceCategory
- [x] 4.4.2 Update `ServiceApplicationService` for enum Category — `GetServicesByCategoryAsync`
  retyped from `string` to `ServiceCategory` (see 3.2.3)
- [x] 4.4.3 Update service interface contracts
- [x] 4.4.4 Update application service tests

## Phase 5: API Layer (Backend)

### 5.1 Update Controllers
- [x] 5.1.1 Update `ProvidersController` registration endpoints
- [x] 5.1.2 Update request models (remove ProviderType, add ServiceCategory)
- [x] 5.1.3 Update response models
- [x] 5.1.4 Update `ServicesController` for enum Category
- [x] 5.1.5 Update Swagger/OpenAPI annotations
- [x] 5.1.6 Add API documentation for ServiceCategory enum — `docs/SERVICE_CATEGORY_MODEL.md`,
  including the serialisation gotcha (enums go over the wire as **camelCase strings**, not ints)

### 5.2 Create Category Endpoints
- [x] 5.2.1 Create `GET /api/v1/categories` endpoint (list all categories) — now returns the whole
  taxonomy. It previously dropped empty categories, contradicting the spec's "categories with zero
  providers are shown but marked as Coming Soon"; they are now returned with `isComingSoon: true`.
  `GET /api/v1/categories/popular` keeps the filtered, count-ranked behaviour.
- [x] 5.2.2 Return category metadata (ID, name, icon, color, slug) — `Id` and `Key` were missing
- [x] 5.2.3 Add `GET /api/v1/categories/{id}/providers` endpoint — accepts the numeric id or the
  slug, 404s on an unknown category rather than silently returning everything
- [x] 5.2.4 Add category count aggregation — `IProviderReadRepository.CountByCategoryAsync` does a
  `GROUP BY` in the database; the handler previously materialised every active provider to count
  them in memory
- [x] 5.2.5 Add integration tests for category endpoints — `CategoriesControllerTests` (15 tests)

### 5.3 Update Request/Response Models
- [x] 5.3.1 Create `ServiceCategoryResponse.cs` model
- [x] 5.3.2 Update `RegisterProviderRequest` with ServiceCategory
- [x] 5.3.3 Update `ProviderResponse` model
- [x] 5.3.4 Update `CreateServiceRequest` with ServiceCategory enum
- [x] 5.3.5 Add model validation attributes

## Phase 6: Frontend TypeScript Types

### 6.1 Define Category Types
- [x] 6.1.1 Category enum defined — landed as `ProviderCategory` in `src/core/types/enums.types.ts`
  (the repo has no `src/shared/types/`; `src/core/` is the equivalent)
- [x] 6.1.2 Match enum integer values exactly with backend — **and normalise on the way in**. The
  ids match, but the API does not send them: the host serialises enums with
  `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)`, so `primaryCategory` arrives as
  `"hairSalon"` while the frontend typed it `number`. Reading it directly yielded `undefined`
  metadata and a blank category badge. Added `parseCategory()`, which accepts the numeric id,
  either casing of the enum name, the slug, and the legacy wizard aliases.
- [x] 6.1.3 Create `CategoryMetadata` interface
- [x] 6.1.4 Create category metadata constant objects
- [x] 6.1.5 Export category helper functions

### 6.2 Update Provider Types
- [x] 6.2.1 Update `src/modules/provider/types/provider.types.ts`
- [x] 6.2.2 Remove `ProviderType` type/enum — deprecated rather than deleted; still referenced by
  `ProviderFilters.vue` and the mock/search paths (see 8.2)
- [x] 6.2.3 Add `primaryCategory: ProviderCategory` to Provider interface
- [x] 6.2.4 Update ProviderHierarchyType import/usage
- [x] 6.2.5 Update Service interface with ServiceCategory enum

### 6.3 Create Category Constants
- [x] 6.3.1 Created as `src/core/constants/provider-categories.ts`
- [x] 6.3.2 Define CATEGORY_METADATA constant with all category info
- [x] 6.3.3 Export helper functions: `getCategoryPersianName()`, `getCategoryIcon()`, etc.
- [x] 6.3.4 Add category color palette constants (`colorHex` + `gradient` per category)

## Phase 7: Frontend Registration Flow

### 7.1 Update CategorySelectionStep Component
- [x] 7.1.1 Update `CategorySelectionStep.vue` categories array — now derived from
  `CATEGORY_METADATA` via an explicit `ENABLED_CATEGORIES` list, so labels/icons/slugs cannot drift
  from the backend enum. The offered set stays HairSalon + Barbershop (a product decision, not a
  technical limit — the rest are one line away).
- [x] 7.1.2 Map category IDs to ServiceCategory enum values — **bug fix**: the step emitted
  `barber`, which no backend map listed, so every men's barbershop registered as `BeautySalon`.
  Now emits the canonical slug, and `ServiceCategoryResolver` still accepts `barber` for drafts
  already saved with it.
- [x] 7.1.3 Update category display with metadata from constants
- [x] 7.1.4 Update `selectCategory()` to carry the enum internally and emit the canonical slug
- [x] 7.1.5 Update validation logic
- [x] 7.1.6 Add unit tests for category selection — `CategorySelectionStep.spec.ts` (14 tests)

### 7.2 Update Registration Store
- [x] 7.2.1 Registration state lives in the flow views
  (`OrganizationRegistrationFlow.vue` / `IndividualRegistrationFlow.vue`), not a
  `registration.store.ts` — that file does not exist in this repo
- [x] 7.2.2 `categoryId` continues to hold a category **string**; the wire format stays a string on
  purpose, and every backend entry point resolves it through `ServiceCategoryResolver`
- [x] 7.2.3 Update registration submission to send the canonical value
- [x] 7.2.4 Update store tests — covered at the component level (7.1.6)

### 7.3 Update Registration Service
- [x] 7.3.1 Registration requests go through `hierarchy.service.ts` /
  `provider-registration.service.ts`; no `registration.service.ts` exists
- [x] 7.3.2 Update API request payload with the canonical category value
- [x] 7.3.3 Handle category in response DTOs — `provider.service.ts` normalises via
  `parseCategory()`
- [x] 7.3.4 Update service tests

### 7.4 Update Registration Flow Validation
- [x] 7.4.1 Update step validation to require a valid category
- [x] 7.4.2 Add error messages in Persian — existing step validation message reused
- [x] 7.4.3 Update progress persistence — **bug fix**: resuming a draft returns the enum member
  name (`"HairSalon"`) while the cards were keyed by `hair_salon`, so no card was re-selected and
  the registrant had to pick again. The step now normalises the incoming value.
- [ ] 7.4.4 Add integration tests for full registration flow — **not done here**: the existing
  Reqnroll registration features cover the command path, but an end-to-end wizard test belongs in
  the Playwright suite and needs a running stack

## Phase 8: Frontend Provider Profile & Search

### 8.1 Update Provider Profile Display
- [x] 8.1.1 Update provider profile components to display category badge —
  `ProfileAbout.vue` / `ProfileHeader.vue`
- [x] 8.1.2 Add category icon + name display
- [x] 8.1.3 Update provider card components
- [x] 8.1.4 Add category color theming — `colorHex`/`gradient` exposed per category
- [x] 8.1.5 Update profile page tests — the label helpers are exercised through
  `provider-categories.spec.ts`; both components now resolve any inbound category shape via
  `parseCategory()` instead of only accepting a number

### 8.2 Update Search & Filter Components
- [x] 8.2.1 Category filter — served by `GET /api/v1/categories/{idOrSlug}/providers` plus the
  existing `category` filter on provider search
- [x] 8.2.2 Update search page with category filter — **two more dead filters found and fixed** in
  `ProviderFilters.vue`:
  (a) the "نوع کسب‌وکار" chips sent `type=Salon`, but `SearchProvidersRequest` has no `Type`
  parameter at all, so the filter did nothing — the section is removed;
  (b) the category dropdown offered ten ad-hoc ids (`haircut`, `coloring`, `facial`, `waxing`,
  `tattoo`, …), only two of which (`massage`, `spa`) are ServiceCategory members. The
  specification fails closed, so the other eight returned **zero** providers. The dropdown is now
  driven by `CATEGORY_METADATA` and sends canonical slugs, and
  `SearchProvidersSpecification` accepts slugs alongside enum member names.
- [x] 8.2.3 Update search service to filter by ServiceCategory — see the dead-filter fix in 4.2.2
- [x] 8.2.4 Add category-based provider listing pages — endpoint added (5.2.3); the route accepts
  the slug so category URLs stay readable
- [x] 8.2.5 Update search tests — `CategoriesControllerTests`

### 8.3 Update Provider Service
- [x] 8.3.1 Update `src/modules/provider/services/provider.service.ts`
- [x] 8.3.2 Handle ServiceCategory in API responses — normalised via `parseCategory()` (see 6.1.2)
- [x] 8.3.3 Update provider list/search methods
- [x] 8.3.4 Add category aggregation queries — `CountByCategoryAsync` aggregates with a `GROUP BY`
  in the database instead of materialising every active provider
- [x] 8.3.5 Update service tests

## Phase 9: Testing & Quality Assurance

### 9.1 Backend Tests
- [x] 9.1.1 Unit tests for ServiceCategory enum and extensions — `ServiceCategoryTests`
- [x] 9.1.2 Unit tests for updated Provider aggregate — `ProviderPrimaryCategoryTests`
- [x] 9.1.3 Unit tests for updated Service aggregate — `ServiceCategoryAssignmentTests`
- [x] 9.1.4 Integration tests for registration with category — `ServiceCategoryResolverTests`
  covers the parsing shared by every registration handler; `ProviderCategoryPersistenceTests`
  covers the round-trip through the database
- [x] 9.1.5 Integration tests for category-based search — `CategoriesControllerTests`
- [x] 9.1.6 Coverage for new/changed code — every new/changed backend path has direct tests
  (enum + metadata, both aggregates' guards, the resolver, both repositories' category lookups,
  all three category endpoints, and the CHECK constraints). No coverage tool was run for a
  percentage figure.

### 9.2 Frontend Tests
- [x] 9.2.1 Unit tests for category constants and helpers — `provider-categories.spec.ts` (16)
- [x] 9.2.2 Component tests for CategorySelectionStep — `CategorySelectionStep.spec.ts` (14)
- [x] 9.2.3 Integration tests for registration flow — covered at component level; see 7.4.4
- [ ] 9.2.4 Write E2E tests for category selection and profile display — **not done here**: needs a
  running stack (Playwright suite)
- [x] 9.2.5 Test category filter functionality — backend side covered; the Vue filter component
  is still on the deprecated `ProviderType` (see 8.2.2)
- [x] 9.2.6 Coverage for new/changed code — all new frontend logic has direct tests

### 9.3 Manual QA Testing
> **Not performed** — all seven require a human driving a running stack. Automated equivalents
> exist for 9.3.2 (draft-resume re-selection) and 9.3.3/9.3.4 (category search and display).
- [ ] 9.3.1 Test provider registration with each category
- [ ] 9.3.2 Test category persistence across registration steps
- [ ] 9.3.3 Test provider search by category
- [ ] 9.3.4 Test category display on provider profiles
- [ ] 9.3.5 Test backward navigation and category change
- [ ] 9.3.6 Test mobile responsive design for category selection
- [ ] 9.3.7 Test RTL layout for Persian category names

### 9.4 Performance Testing
- [ ] 9.4.1 Benchmark provider search with category filter (<50ms p95) — **needs a
  production-sized dataset**; index usage confirmed (9.4.4)
- [x] 9.4.2 Benchmark category aggregation queries — addressed structurally rather than measured:
  the aggregation moved from "load every active provider and group in memory" to a database
  `GROUP BY` served by `IX_Providers_PrimaryCategory`
- [ ] 9.4.3 Load test registration endpoint with category — **needs a load-testing environment**
- [x] 9.4.4 Verify database index usage with EXPLAIN plans — `EXPLAIN` confirms
  `Index Scan using "IX_Providers_PrimaryCategory"` for category filtering

## Phase 10: Deployment & Monitoring

> **Phase 10 is an operations phase and was not executed here** — it needs deploy credentials,
> staging/production access and a scheduled window. The items below stay unchecked so nobody reads
> this change as deployed. `docs/SERVICE_CATEGORY_MODEL.md` carries what the deployer needs.

### 10.1 Pre-Deployment
- [ ] 10.1.1 Review all code changes in PR
- [x] 10.1.2 Run full test suite (backend + frontend) — see the Testing Summary at the foot of this
  file for what passed and the pre-existing failures that did not
- [ ] 10.1.3 Generate deployment checklist
- [x] 10.1.4 Prepare rollback plan — both migrations have a working `Down()`;
  `BackfillProviderPrimaryCategory` was verified reversible and re-runnable against a real
  Postgres. Note its `Down()` intentionally does not un-backfill the categories.
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
- [x] 11.1.1 Delete ProviderType enum file
- [x] 11.1.2 Delete ServiceCategory value object file (confirmed deleted)
- [x] 11.1.3 Remove dead code referencing ProviderType — the only remaining backend mentions are
  historical EF migration snapshots (immutable by definition) and one comment in
  `ProviderHierarchyType.cs`. Also removed three duplicated category-parsing `switch` blocks in
  favour of `ServiceCategoryResolver`, and an unused `IServiceReadRepository` injection in
  `GetCategoriesWithCountsQueryHandler`.
- [x] 11.1.4 Update XML documentation comments
- [x] 11.1.5 Run code formatter and linter — backend builds with 0 errors;
  `vue-tsc --noEmit` reports 0 errors

### 11.2 Documentation
- [x] 11.2.1 Update API documentation with ServiceCategory enum
- [x] 11.2.2 Update developer README with category model explanation
- [x] 11.2.3 Document category selection mapping (frontend ↔ backend)
- [x] 11.2.4 Update database schema documentation
- [x] 11.2.5 Create migration guide for developers
> All five land in **[docs/SERVICE_CATEGORY_MODEL.md](../../../docs/SERVICE_CATEGORY_MODEL.md)**:
> the enum table with its persisted ids, the metadata contract, the database columns and CHECK
> constraints, the three validation layers, the resolver's accepted input shapes, the endpoints,
> the serialisation gotcha, the migration history and why the backfill was needed, and how to add
> a category.

### 11.3 Admin Tools (Optional)
> **Not built** — marked Optional in the plan, and each is a feature in its own right rather than
> part of the category model. Recorded as a known gap in `docs/SERVICE_CATEGORY_MODEL.md`; matters
> most for correcting providers the backfill defaulted to HairSalon.
- [ ] 11.3.1 Create admin UI to view category distribution
- [ ] 11.3.2 Create admin UI to change provider category (with approval)
- [ ] 11.3.3 Create category analytics dashboard
- [ ] 11.3.4 Add alerts for category imbalance

### 11.4 Future Enhancements Documentation
- [x] 11.4.1 Document Phase 2: Service category alignment validation — recorded as a known gap
  (design.md Decision 7 defers it; `ServiceCategoryAssignmentTests` pins the current permissive
  behaviour and names itself as the test to rewrite if a compatibility matrix lands)
- [ ] 11.4.2 Document Phase 3: Category-specific service templates — not specified anywhere yet
- [ ] 11.4.3 Document Phase 4: Subcategory/tag system — not specified anywhere yet
- [ ] 11.4.4 Document Phase 5: Category-based analytics — not specified anywhere yet

## Estimated Timeline

- **Phase 1-2**: Week 1 (Domain model + Database)
- **Phase 3-5**: Week 1-2 (Backend implementation)
- **Phase 6-8**: Week 2-3 (Frontend implementation)
- **Phase 9**: Week 3 (Testing)
- **Phase 10**: Week 3-4 (Deployment)
- **Phase 11**: Week 4 (Cleanup & Documentation)

**Total**: 3-4 weeks for full implementation

---

## Testing Summary

### Defects found and fixed while completing this change

The backend was reported complete in `COMPLETION_STATUS.md`, and it did compile — but "builds with
0 errors" had been standing in for "works". Six real defects were sitting behind that:

1. **Every pre-existing provider and service held category `0`.** `RemoveStaff` added both columns
   as `NOT NULL DEFAULT 0` and dropped the legacy source columns in the same migration. Zero is not
   a `ServiceCategory`, so the metadata lookups throw on those rows.
   → `20260815222542_BackfillProviderPrimaryCategory` + CHECK constraints.
2. **`ServiceReadRepository` category queries could not run.** `GetByCategoryAsync` and
   `GetAveragePriceByCategoryAsync` filtered on `s.Category.ToString() == category`, which EF cannot
   translate for a `HasConversion<int>()` column.
   → Retyped to take the enum and compare it directly.
3. **`SearchProvidersQuery.Category` was a dead filter.** Declared, logged, and never passed to the
   specification — filtering by category returned the entire catalogue.
   → Passed through; covered by a test that fails without it.
4. **Every men's barbershop registered as `BeautySalon`.** The wizard emits `barber`; none of the
   three duplicated category maps listed it, so it hit the `_ => BeautySalon` default.
   → All parsing unified in `ServiceCategoryResolver`.
5. **The category badge was blank.** The API serialises enums as camelCase strings (`"hairSalon"`),
   the frontend typed `primaryCategory` as a number, so metadata lookups returned `undefined`.
   → `parseCategory()` normalises every shape the category legitimately arrives in.
6. **Resuming a draft lost the category selection.** The draft endpoints return `"HairSalon"` while
   the cards were keyed by `hair_salon`, so nothing was re-selected.
   → The step normalises the incoming value.

7. **The search page's category filter returned nothing for 8 of its 10 options.** The dropdown
   offered ad-hoc ids (`haircut`, `coloring`, `facial`, `waxing`, `tattoo`, …) that are not
   ServiceCategory members; the specification fails closed, so those searches came back empty.
   → Dropdown driven by the shared metadata table; the specification now accepts slugs too.
8. **A "business type" filter that was wired to nothing.** `ProviderFilters.vue` sent `type=Salon`,
   but `SearchProvidersRequest` has no `Type` parameter.
   → Section removed.

Two smaller ones: `TryParseSlug("0")` returned `true` with an invalid category, and the categories
endpoint hid every empty category, contradicting the spec's "Coming Soon" requirement.

### Tests added

| Suite | File | Tests |
|---|---|---|
| Domain unit | `ServiceCategoryTests` | enum ids, metadata completeness, slug round-trip, undefined-value rejection |
| Domain unit | `ProviderPrimaryCategoryTests` | category capture, validation, draft-only mutation, staff inheritance |
| Domain unit | `ServiceCategoryAssignmentTests` | category capture, validation, recategorisation |
| Application unit | `ServiceCategoryResolverTests` | all accepted input shapes, the `barber` regression, rejection of out-of-range numbers |
| Integration | `ProviderCategoryPersistenceTests` | int round-trip, both repositories' category lookups translate, CHECK constraints reject `0` |
| Integration | `CategoriesControllerTests` | full taxonomy, coming-soon flag, popular ranking, category pages by id and slug, 404 on unknown, search filter across slug/name/alias |
| Frontend unit | `provider-categories.spec.ts` | id contract, metadata, `parseCategory` across every shape |
| Frontend component | `CategorySelectionStep.spec.ts` | rendering, selection, canonical slug emission, draft-resume re-selection |

### Tests modified

None. No existing test was changed, weakened, or removed.

### Results

| Suite | Result |
|---|---|
| `Booksy.ServiceCatalog.Domain.UnitTests` | **549 passed**, 0 failed |
| `Booksy.ServiceCatalog.Application.UnitTests` | **133 passed**, 0 failed |
| Integration — `Categories` + `Persistence` | **30 passed**, 0 failed |
| Integration — `Categories` + `Persistence` + `Authorization` + `Unit` filters | **120 passed, 14 failed** — all 14 are pre-existing Reqnroll scenarios (see below); every test added by this change passed |
| Frontend vitest (category files) | **30 passed**, 0 failed |
| Backend build | 0 errors |
| `vue-tsc --noEmit` | 0 errors |
| Migration `Up`/`Down` | verified against a real Postgres, including a run with all 18 providers stranded at `0` |

### Pre-existing failures (not caused by this change, not fixed here)

Each was confirmed pre-existing before being dismissed — none is an unexamined red test.

- **Reqnroll BDD suite: 1083 of 1207 scenarios fail** with *"No matching step definition found"*.
  The `.feature` files reference steps that were never bound — the features use
  `the provider has a service "X"` while `TestDataSteps.cs` only defines `... "X" with:` and
  `... "X" priced at N USD`. `git status` shows no step-definition or feature file touched by this
  change, and a binding-resolution error cannot be produced by a domain/repository edit. This suite
  is not a deploy gate (the gates are the unit tests and the keystone script), but it warrants its
  own change.
- **Most `API.*` integration tests fail on `localhost:5021` connection refused.** That is the
  external identity/token service (`"BaseUrl": "https://localhost:5021/api"`), which is not running
  in this environment. Confirmed environmental, not category-related: in the same failing run the
  provider `INSERT` completes with `"PrimaryCategory" = 2`, and the repo's own committed logs record
  the identical error on 2025-12-31.
- **Frontend: 6 spec files fail** — the three `tests/integration/*.spec.ts` make real HTTP calls to
  a backend that is not running, plus `auth.api`, `LoginForm` and `hierarchy.store`. Verified by
  running the suite on a clean tree with this change stashed: the same 6 files fail identically.

### Regression risks addressed

- **Existing category strings keep working.** `ServiceCategoryResolver` accepts every id the wizard
  has ever emitted, so in-flight drafts and saved registrations still resolve.
- **Legacy rows still load.** Validation lives in the factories and mutators, not the EF
  constructor, so materialising a pre-backfill row does not throw.
- **The now-live search filter changes nothing for existing callers.** No existing caller populated
  `SearchProvidersQuery.Category`; the API's `ToQuery()` passes the separate `ServiceCategory`
  string. Only the new category endpoint sets it.
- **Category ids are pinned on both sides**, so a renumbering that would silently re-label every
  provider row fails a test instead.

### Known gaps

Recorded in `docs/SERVICE_CATEGORY_MODEL.md`: no service/provider category alignment validation
(deferred by design), and no admin UI for reviewing or correcting the categories the backfill
assigned by default.
