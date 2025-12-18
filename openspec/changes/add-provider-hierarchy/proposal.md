# Add Provider Hierarchy

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
