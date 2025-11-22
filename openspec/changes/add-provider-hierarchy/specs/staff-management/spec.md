# staff-management Specification Delta

## MODIFIED Requirements

### Requirement: Staff Member Management
The system SHALL allow organizations to manage staff members as Individual Provider entities with full provider capabilities.

**NOTE**: This requirement fundamentally changes from managing Staff as separate entities to managing them as Individual Providers linked to the organization.

#### Scenario: Staff members are individual providers
- **WHEN** an organization adds a staff member
- **THEN** the system creates or links an Individual Provider entity
- **AND** the individual provider has their own complete profile
- **AND** the individual provider manages their own schedule, services, and availability
- **AND** the individual provider receives direct bookings from customers
- **AND** the individual provider appears in search results under the organization

#### Scenario: Organization views staff list
- **WHEN** an organization owner views their staff list
- **THEN** the system displays all linked individual providers
- **AND** shows each staff member's status (Active, Pending, Inactive)
- **AND** shows each staff member's performance metrics (bookings, revenue, ratings)
- **AND** provides actions: View Profile, Edit Permissions, Remove
- **AND** allows sorting by name, join date, performance, or status

#### Scenario: Organization removes staff member
- **WHEN** an organization removes a staff member
- **THEN** the system breaks the parent-child provider link
- **AND** prompts for removal reason
- **AND** handles existing future bookings (reassign or cancel)
- **AND** preserves historical booking data
- **AND** notifies the staff member of removal
- **AND** offers the individual option to become independent or join another org

## ADDED Requirements

### Requirement: Staff Permission Levels
Organizations SHALL be able to configure permission levels for staff members regarding pricing, services, and schedule management.

#### Scenario: Organization sets staff pricing permissions
- **WHEN** an organization configures pricing permissions for staff
- **THEN** staff can either:
  - Use fixed organization pricing (no changes allowed)
  - Set pricing within organization-defined ranges
  - Set pricing freely
- **AND** the system enforces the chosen permission level
- **AND** displays permission level clearly to staff members

#### Scenario: Organization sets service catalog permissions
- **WHEN** an organization configures service catalog permissions
- **THEN** staff can either:
  - Only offer services from organization catalog
  - Offer org catalog + add custom services
  - Manage services completely independently
- **AND** the system enforces the chosen permission level
- **AND** custom services are tagged as staff-specific

### Requirement: Staff Performance Tracking
Organizations SHALL be able to view performance metrics for each staff member.

#### Scenario: Organization views staff performance dashboard
- **WHEN** an organization accesses the staff performance dashboard
- **THEN** the system displays metrics for each staff member:
  - Total bookings (current month, all-time)
  - Revenue generated
  - Average rating
  - Customer reviews count
  - Rebooking rate
  - Cancellation rate
- **AND** allows comparing staff members side-by-side
- **AND** allows filtering by date range
- **AND** provides exportable reports

#### Scenario: Individual staff member views own metrics
- **WHEN** a staff member accesses their personal dashboard
- **THEN** the system displays their individual performance metrics
- **AND** shows trends over time (weekly, monthly)
- **AND** compares to organization averages (if organization allows)
- **AND** provides insights and recommendations

### Requirement: Staff Availability Coordination
The system SHALL prevent scheduling conflicts between organization operating hours and staff member schedules.

#### Scenario: Staff schedule respects organization hours
- **WHEN** a staff member sets their working hours
- **THEN** the system validates hours fall within organization operating hours
- **AND** prevents staff from accepting bookings outside org hours
- **AND** displays organization hours as a reference
- **AND** allows staff to work fewer hours than organization (but not more)

#### Scenario: Organization closes temporarily
- **WHEN** an organization marks a day as closed (holiday/exception)
- **THEN** the system automatically blocks all staff member bookings for that day
- **AND** notifies all staff members of the closure
- **AND** handles existing bookings (notify customers, offer rescheduling)

## REMOVED Requirements

### Requirement: Staff CRUD Operations
**Reason**: Staff are no longer separate entities - they are Individual Providers with hierarchical relationships.

**Migration**: Existing Staff entities will be migrated to Individual Provider entities during the implementation. Organizations will transition to using the invitation/join request workflows instead of direct CRUD operations.

The new workflow is:
1. Organization invites individual (creates pending invitation)
2. Individual accepts and completes profile (creates Individual Provider)
3. Organization approves (links Individual to Organization)
4. Individual appears as staff member (but is actually a full Provider)
