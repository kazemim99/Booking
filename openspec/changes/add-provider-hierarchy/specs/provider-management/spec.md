# provider-management Specification Delta

## ADDED Requirements

### Requirement: Provider Type Classification
The system SHALL support two distinct provider types: Organization and Individual, with clear registration and management paths for each.

#### Scenario: Organization provider characteristics
- **WHEN** a provider registers as an Organization
- **THEN** the system assigns type "Organization"
- **AND** allows the organization to invite staff members
- **AND** allows the organization to work solo initially without staff
- **AND** displays the organization as a searchable business entity
- **AND** enables direct bookings when no staff exists
- **AND** requires staff selection when staff members are added

#### Scenario: Independent individual provider characteristics
- **WHEN** a provider registers as an Independent Individual
- **THEN** the system assigns type "Individual" with isIndependent=true
- **AND** the provider appears in search as a standalone entity
- **AND** the provider cannot invite staff members
- **AND** the provider manages their own schedule, services, and pricing
- **AND** the provider receives direct bookings

#### Scenario: Organization-linked individual provider characteristics
- **WHEN** an individual joins an organization as staff
- **THEN** the system assigns type "Individual" with isIndependent=false
- **AND** links the individual to their parent organization
- **AND** the individual appears under the organization in search results
- **AND** the individual manages their own schedule within org constraints
- **AND** the individual manages their own services and pricing (if allowed)
- **AND** the individual receives direct bookings from customers

### Requirement: Hierarchical Provider Relationships
The system SHALL support parent-child relationships between Organization providers and Individual providers.

#### Scenario: Organization has multiple staff members
- **WHEN** an organization has added staff members
- **THEN** the system maintains a parent-child relationship via ParentProviderId
- **AND** displays the hierarchy in search results (organization → individuals)
- **AND** enforces data integrity constraints (prevent orphaned individuals)
- **AND** allows querying all staff members for an organization efficiently
- **AND** prevents circular relationships (individual cannot be parent of their organization)

#### Scenario: Individual leaves organization
- **WHEN** an organization removes a staff member OR staff member leaves
- **THEN** the system breaks the parent-child link
- **AND** the individual can either become independent or join another organization
- **AND** preserves the individual's historical bookings and reviews
- **AND** notifies both parties of the change
- **AND** updates search indexes to reflect new status

### Requirement: Organization Staff Invitation Workflow
Organizations SHALL be able to invite individuals to join as staff members via phone number.

#### Scenario: Organization sends staff invitation
- **WHEN** an organization owner invites a person by phone number
- **THEN** the system creates a pending invitation record
- **AND** sends an SMS notification with a unique invitation link
- **AND** sets an expiration date (7 days from invitation)
- **AND** allows the organization to track invitation status
- **AND** prevents duplicate invitations to the same phone number

#### Scenario: Individual accepts organization invitation
- **WHEN** an invited person clicks the invitation link
- **THEN** the system verifies the invitation is valid and not expired
- **AND** guides the user through phone verification
- **AND** links existing account OR creates new account
- **AND** prompts the individual to complete their staff profile
- **AND** notifies the organization when profile is submitted
- **AND** requires organization approval before profile goes live

#### Scenario: Organization approves staff profile
- **WHEN** an organization owner reviews a completed staff profile
- **THEN** the system allows approval or rejection with reason
- **AND** upon approval, activates the staff member's profile
- **AND** adds the individual to the organization's staff list
- **AND** makes the individual bookable by customers
- **AND** sends confirmation notification to the individual

#### Scenario: Invitation expires
- **WHEN** an invitation is not accepted within 7 days
- **THEN** the system marks the invitation as expired
- **AND** removes the invitation link's validity
- **AND** allows the organization to resend the invitation
- **AND** notifies the organization of the expiration

### Requirement: Join Request Workflow
Individuals SHALL be able to request to join existing organizations, pending organization approval.

#### Scenario: Individual searches for organizations to join
- **WHEN** an individual wants to join an organization
- **THEN** the system provides a search interface for organizations
- **AND** filters organizations by location, category, and type
- **AND** displays organization profiles with basic information
- **AND** shows if organization is accepting join requests

#### Scenario: Individual submits join request
- **WHEN** an individual selects an organization and submits a join request
- **THEN** the system creates a pending join request record
- **AND** allows the individual to include a message/introduction
- **AND** attaches the individual's profile information to the request
- **AND** notifies the organization owner immediately
- **AND** prevents duplicate requests to the same organization

#### Scenario: Organization reviews join request
- **WHEN** an organization owner views a join request
- **THEN** the system displays the individual's profile, bio, and services
- **AND** shows the individual's message/introduction
- **AND** provides options to approve or reject
- **AND** allows the organization to send a message with the decision

#### Scenario: Organization approves join request
- **WHEN** an organization approves a join request
- **THEN** the system links the individual to the organization
- **AND** sets the individual's parentProviderId to the organization
- **AND** activates the individual's profile under the organization
- **AND** sends approval notification to the individual
- **AND** adds the individual to searchable staff list

#### Scenario: Organization rejects join request
- **WHEN** an organization rejects a join request
- **THEN** the system marks the request as rejected
- **AND** sends rejection notification to the individual
- **AND** optionally includes organization's message/reason
- **AND** suggests the individual register as independent instead

### Requirement: Solo Organization Booking Flow
Solo organizations (with no staff) SHALL accept direct bookings without staff selection.

#### Scenario: Customer books with solo organization
- **WHEN** a customer selects a solo organization for booking
- **THEN** the system skips the staff selection step
- **AND** creates the booking directly with the organization
- **AND** assigns the booking to the organization owner implicitly
- **AND** displays clear confirmation showing the organization name
- **AND** sends notifications to the organization owner

#### Scenario: Organization adds first staff member
- **WHEN** a solo organization adds their first staff member
- **THEN** the system transitions the booking flow to require staff selection
- **AND** notifies existing customers of the change (if they have future bookings)
- **AND** allows the organization to assign existing bookings to specific staff

### Requirement: Multi-Staff Organization Booking Flow
Organizations with staff SHALL require customers to select a specific staff member when booking.

#### Scenario: Customer views organization with staff
- **WHEN** a customer selects an organization that has staff members
- **THEN** the system displays all active staff members
- **AND** shows each staff member's avatar, name, and specialties
- **AND** displays each staff member's available time slots
- **AND** allows filtering by service, date, or staff member
- **AND** shows staff member's individual ratings and reviews

#### Scenario: Customer selects specific staff member
- **WHEN** a customer selects a staff member for booking
- **THEN** the system displays only that staff member's services and availability
- **AND** creates the booking linked to the specific individual provider
- **AND** sends notifications to the selected staff member
- **AND** displays the staff member's name prominently in confirmation

### Requirement: Provider Conversion (Individual to Organization)
Independent individuals SHALL be able to convert their provider account to an Organization when they want to hire staff.

#### Scenario: Individual initiates conversion to organization
- **WHEN** an independent individual requests conversion to organization
- **THEN** the system displays a preview of the conversion impact
- **AND** shows what will change (profile type, branding, capabilities)
- **AND** preserves all existing data (bookings, reviews, services)
- **AND** requires a new business name for the organization
- **AND** optionally requests new business logo and branding

#### Scenario: System performs conversion
- **WHEN** an individual confirms the conversion
- **THEN** the system changes the provider type from Individual to Organization
- **AND** preserves the same provider ID (URLs remain valid)
- **AND** migrates all bookings to reference the converted provider
- **AND** retains all reviews and ratings
- **AND** creates the individual as the first staff member (optional)
- **AND** enables staff invitation capabilities
- **AND** sends confirmation notification

#### Scenario: Converted organization manages first hire
- **WHEN** a newly converted organization invites their first staff member
- **THEN** the system follows the standard invitation workflow
- **AND** transitions booking flow to require staff selection
- **AND** allows the owner to appear as a staff member if desired
- **AND** maintains all historical bookings under the organization

### Requirement: Staff Schedule Constraints
Individual providers linked to an organization SHALL set schedules that respect the organization's operating hours.

#### Scenario: Staff member sets working hours
- **WHEN** a staff member configures their working hours
- **THEN** the system validates hours are within organization's operating hours
- **AND** displays clear validation errors if outside allowed range
- **AND** shows the organization's hours as reference
- **AND** allows flexibility within the constraints
- **AND** permits shorter hours than organization hours
- **AND** prevents booking during organization closed hours

#### Scenario: Organization changes operating hours
- **WHEN** an organization updates their operating hours
- **THEN** the system validates against all staff members' schedules
- **AND** warns if change would invalidate existing staff schedules
- **AND** suggests conflicting staff members to review
- **AND** optionally auto-adjusts staff schedules to fit new hours
- **AND** notifies affected staff members

### Requirement: Staff Service and Pricing Management
Individual providers SHALL manage their own services and pricing, with optional organization-level constraints.

#### Scenario: Organization has master service catalog
- **WHEN** an organization defines a master service catalog
- **THEN** staff members can select services from the catalog
- **AND** staff members can add custom services (if allowed by organization)
- **AND** staff members can set their own pricing (if allowed by organization)
- **AND** the system displays both org-level and staff-level services

#### Scenario: Staff member adds custom service
- **WHEN** a staff member adds a service not in org catalog
- **THEN** the system allows the custom service (if org policy permits)
- **AND** tags the service as "staff-specific"
- **AND** only that staff member can provide the service
- **AND** the service appears in search when filtering by that staff member

### Requirement: Provider Hierarchy Display in Search
The system SHALL display provider hierarchy clearly in search results and profile pages.

#### Scenario: Search results show organizations with staff
- **WHEN** a customer searches for services
- **THEN** search results include both organizations and independent individuals
- **AND** organization results show a "staff badge" or indicator
- **AND** organization results preview top staff members (e.g., first 3)
- **AND** clicking an organization expands to show all staff
- **AND** each staff member is clickable for their individual profile

#### Scenario: Organization profile page displays hierarchy
- **WHEN** a customer views an organization's profile
- **THEN** the system displays organization information (logo, address, hours)
- **AND** displays a "Our Team" section with all staff members
- **AND** shows each staff member's avatar, name, role, and specialties
- **AND** allows filtering/sorting staff by service, availability, or rating
- **AND** enables direct booking with a specific staff member

#### Scenario: Individual profile shows parent organization
- **WHEN** a customer views an individual (non-independent) profile
- **THEN** the system displays the parent organization name and logo
- **AND** shows a link to the organization's full profile
- **AND** displays "Works at [Organization Name]"
- **AND** shows the individual's specific schedule and services
- **AND** enables direct booking with that individual

## MODIFIED Requirements

### Requirement: Enhanced Business Information Management
The system SHALL allow providers to update their business profile with rich media and comprehensive information, adapted for provider type.

#### Scenario: Organization logo upload
- **WHEN** an organization uploads a business logo
- **THEN** the system provides image cropping tools to ensure proper aspect ratio (1:1 square)
- **AND** generates optimized versions for different display contexts (thumbnail, preview, full)
- **AND** validates file size (max 5MB) and format (PNG, JPG, WebP)
- **AND** displays a real-time preview of the uploaded logo
- **AND** the logo represents the organization brand

#### Scenario: Individual avatar upload
- **WHEN** an individual provider uploads an avatar
- **THEN** the system provides image cropping tools for profile photo (1:1 square)
- **AND** generates optimized versions for different display contexts
- **AND** validates file size (max 2MB) and format (PNG, JPG, WebP)
- **AND** displays a real-time preview
- **AND** the avatar represents the individual person

#### Scenario: Business information validation for organizations
- **WHEN** an organization submits updated business information
- **THEN** the system validates required fields: business name, category, location, hours
- **AND** displays inline error messages for validation failures
- **AND** prevents submission until all validation rules are satisfied
- **AND** validates business license (if required by region)

#### Scenario: Profile information validation for individuals
- **WHEN** an individual submits updated profile information
- **THEN** the system validates required fields: name, bio, services, schedule
- **AND** validates parent organization constraints (if not independent)
- **AND** displays inline error messages for validation failures
- **AND** prevents submission until all validation rules are satisfied

## REMOVED Requirements

None - all existing requirements remain valid and are extended by the hierarchical model.
