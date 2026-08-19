# Add Provider Hierarchy

> ## ⚠ SUPERSEDED — archived 2026-08-19 without promoting its specs
>
> **Status: partially shipped, then superseded.** Sections 1–10 (backend) and 11–17 (Vue UI) were
> implemented in Nov–Dec 2025: migrations `AddProviderHierarchy` + `AddIndividualProviderIdToBookings`,
> `Provider.ParentProviderId`, the `ProviderInvitation` / `ProviderJoinRequest` aggregates, and the Vue
> hierarchy/invitation/join-request screens. Those remain in the codebase.
>
> **What is superseded is this change's core premise** — that a staff member is a second `Provider` linked
> by a scalar `ParentProviderId`. `refactor-identity-and-membership` retires exactly that:
> *"Supersedes the identity/parent mechanism of `add-provider-hierarchy`: its `ProviderInvitation` /
> `ProviderJoinRequest` aggregates and per-staff booking attribution are retained and rewired;
> `ParentProviderId`-as-membership is retired."* The model is replaced by
> Person → `OrganizationMembership` → `StaffProfile` (see `IDENTITY_AND_STAFF_ARCHITECTURE.md`), under which
> a person can belong to several salons, be owner *and* staff, and have a leave/rejoin lifecycle — none of
> which this design supports.
>
> **Archived with `--skip-specs`.** Its three delta specs carry `MODIFIED` **and `REMOVED`** requirements
> against `provider-management`, `provider-registration` and `staff-management`. Because the archiver
> replaces whole requirements with the delta text, a normal archive would have written the retired
> sub-provider staff model into the main specs and deleted existing requirements.
>
> **The 58 open tasks are deliberately not carried forward.** Tests in §11.9/12.9/15.7/13.6/14.6/16.6/17.7/20
> would pin the retired model; notification work in §12.7/13.3/14.5/17.4 now belongs to the archived
> `notification-delivery-reliability`; §18–23 (search/SEO, admin tooling, docs, a 10-step feature-flag
> rollout, post-launch monitoring) describe a project shape this repository does not work in. The one
> genuinely-needed remnant — migrating the Vue hierarchy UI off sub-providers onto memberships — is owned by
> `refactor-identity-and-membership` §8.7.
>
> The four "Decision Points Needed" in `README.md` were all settled long ago; read that file as a historical
> artefact, not an open question.

## Why

The current system treats all providers as single-entity businesses, which doesn't match real-world scenarios where:

1. **Salons/clinics have multiple service providers** - A salon owner may work solo initially but needs to hire barbers, stylists, or specialists as the business grows
2. **Each professional needs their own identity** - Barbers at a salon need individual profiles, schedules, services, and booking capabilities
3. **Business growth is hindered** - No clear path for solo businesses to add staff without complete restructuring
4. **Customer experience is unclear** - Customers can't select specific professionals when booking at multi-staff businesses

**Real-world example**: Elite Hair Salon (Organization) has Ali (owner/barber), Reza (beard specialist), and Sara (women's stylist). Each should have their own:
- Profile photo and bio
- Work schedule (within salon hours)
- Service offerings and pricing
- Direct booking capability
- Performance metrics

## What Changes

### Core Changes

1. **Introduce Provider Type Hierarchy**
   - Add `ProviderType` enum: `Organization` | `Individual`
   - Add `ParentProviderId` for hierarchical relationships
   - Add `IsIndependent` flag to distinguish solo vs organization-linked individuals

2. **Organization Provider (Salons/Clinics)**
   - Represents physical business location with brand identity
   - Can work solo initially (owner handles bookings)
   - Can add staff members who become Individual Providers
   - Controls overall business hours, location, amenities
   - Appears in search as primary entity

3. **Individual Provider (Professionals)**
   - Can be independent (solo freelancer) or linked to Organization
   - Has own profile, avatar, bio, services, schedule
   - Receives direct bookings from customers
   - Linked individuals work within parent organization's constraints

4. **Registration Flows**
   - **Independent Individual**: Solo professionals (mobile barbers, freelancers)
   - **Organization**: Business owners with physical location (may hire later)
   - **Invitation Flow**: Organizations invite individuals to join as staff
   - **Request-to-Join Flow**: Individuals can request to join existing organizations

5. **Booking Flow Changes**
   - Solo Organization: Direct booking with the business
   - Organization with Staff: Customer selects specific Individual Provider
   - Independent Individual: Direct booking as before

6. **Migration Support**
   - Allow Individual → Organization conversion when hiring staff
   - Preserve bookings, reviews, and services during conversion

### Breaking Changes

- **BREAKING**: `Provider` entity structure changes (adds `Type`, `ParentProviderId`, `IsIndependent`)
- **BREAKING**: Booking entity needs `IndividualProviderId` to track which staff member handles service
- **BREAKING**: Search/discovery logic must handle hierarchical display
- **BREAKING**: Provider registration flow splits into multiple paths

## Impact

### Affected Specs
- `provider-management` - Core provider model and hierarchy
- `staff-management` - Staff becomes Individual Providers
- `provider-registration` - New registration flows for each type
- `provider-settings` - New settings for organization staff management
- `service-management` - Services linked to individuals vs organizations
- `working-hours-management` - Individual schedules within org constraints

### Affected Code

**Backend**:
- `BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/Provider/` - Provider aggregate enhancement
- `BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/Registration/` - New registration commands
- `BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/` - Database migrations
- `BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/ProvidersController.cs` - New endpoints

**Frontend**:
- `booksy-frontend/src/modules/provider/components/registration/` - New registration flows
- `booksy-frontend/src/modules/provider/views/` - Organization staff management
- `booksy-frontend/src/modules/provider/services/` - API service updates
- `booksy-frontend/src/modules/provider/stores/` - State management for hierarchy
- `booksy-frontend/src/shared/components/` - Staff selection components

**Database**:
- New columns: `type`, `parent_provider_id`, `is_independent`
- New table: `provider_invitations` for invitation workflow
- New table: `provider_join_requests` for request-to-join workflow
- Migration for existing providers (default to Organization type)

### User Impact
- **Existing Providers**: Automatically become Organization type, no changes required
- **New Providers**: Choose between Organization or Independent Individual
- **Customers**: Better experience selecting specific professionals
- **Platform**: Supports wider range of business models

### Timeline Estimate
- **Phase 1 (Week 1-2)**: Domain model, database schema, migrations
- **Phase 2 (Week 2-3)**: Registration flows (Organization, Individual, Invitation)
- **Phase 3 (Week 3-4)**: Staff management UI, hierarchy display
- **Phase 4 (Week 4-5)**: Booking flow updates, testing
- **Phase 5 (Week 5-6)**: Conversion tool (Individual → Organization), polish
