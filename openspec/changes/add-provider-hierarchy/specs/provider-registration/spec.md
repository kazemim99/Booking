# provider-registration Specification Delta

## ADDED Requirements

### Requirement: Provider Type Selection
The system SHALL present new registrants with a clear choice between provider types with smart recommendations.

#### Scenario: User reaches type selection step
- **WHEN** a user completes phone verification during registration
- **THEN** the system presents two primary options:
  1. "Register a Business" (Organization)
  2. "Work Independently" (Independent Individual)
- **AND** displays clear descriptions of each type
- **AND** shows benefits and use cases for each
- **AND** provides a comparison table

#### Scenario: System provides smart recommendations
- **WHEN** the user views the type selection page
- **THEN** the system asks qualifying questions:
  - "Do you have a physical business location?"
  - "Do you plan to hire staff in the future?"
  - "What best describes you?" (Business owner / Freelancer / Work at multiple locations)
- **AND** provides a recommendation based on answers
- **AND** explains the reasoning for the recommendation
- **AND** allows the user to choose differently if they prefer

#### Scenario: User chooses to join existing organization
- **WHEN** the user selects "Join an Existing Business"
- **THEN** the system redirects to organization search flow
- **AND** does not create a provider entity yet
- **AND** guides them through the join request process

### Requirement: Organization Registration Wizard
The system SHALL provide a comprehensive registration wizard for organizations with physical locations.

#### Scenario: Organization completes business information step
- **WHEN** an organization progresses through registration
- **THEN** Step 1 collects:
  - Business name (required)
  - Business category and subcategories (required)
  - Business description (required, min 50 characters)
  - Tax/Business license number (if required by region)
- **AND** validates business name uniqueness
- **AND** provides category suggestions based on keywords
- **AND** shows real-time character count for description

#### Scenario: Organization sets physical location
- **WHEN** an organization sets their location
- **THEN** Step 2 collects:
  - Full street address (required)
  - City, province, postal code (required)
  - Map coordinates via interactive map picker (required)
  - Service radius (if offering mobile services)
- **AND** validates address format
- **AND** geocodes address to coordinates automatically
- **AND** allows manual map pin adjustment
- **AND** shows preview of how location appears to customers

#### Scenario: Organization sets operating hours
- **WHEN** an organization configures operating hours
- **THEN** Step 3 provides:
  - Weekly schedule editor for each day
  - Support for multiple time periods per day
  - Break time configuration
  - Closed day marking
  - Copy-to-all-days functionality
- **AND** validates time ranges (open before close)
- **AND** prevents overlapping time periods
- **AND** shows preview of weekly schedule

#### Scenario: Organization uploads branding
- **WHEN** an organization uploads branding materials
- **THEN** Step 4 allows:
  - Business logo upload (required, max 5MB)
  - Cover/banner image upload (optional, max 10MB)
  - Gallery images upload (optional, up to 10 images)
  - Image cropping for proper aspect ratios
- **AND** provides real-time preview
- **AND** shows how branding appears on different devices
- **AND** validates file formats (PNG, JPG, WebP)

#### Scenario: Organization adds initial services (optional)
- **WHEN** an organization adds services during registration
- **THEN** Step 5 allows:
  - Adding services with name, description, duration, price
  - Categorizing services
  - Marking services as featured
  - Skipping this step to add services later
- **AND** validates service pricing and duration
- **AND** allows bulk import from templates
- **AND** suggests common services based on category

#### Scenario: Organization configures amenities
- **WHEN** an organization sets amenities and features
- **THEN** Step 6 collects:
  - Amenities (WiFi, parking, wheelchair access, etc.)
  - Payment methods accepted
  - Languages spoken
  - Special features (air conditioning, music, etc.)
- **AND** provides common amenity checklists
- **AND** allows custom amenity additions

#### Scenario: Organization submits for verification
- **WHEN** an organization completes all required steps
- **THEN** the system shows a summary preview
- **AND** requires terms and conditions acceptance
- **AND** submits for admin verification
- **AND** sets status to PendingVerification
- **AND** sends confirmation email/SMS
- **AND** estimates verification timeline (24-48 hours)

### Requirement: Independent Individual Registration Wizard
The system SHALL provide a streamlined registration wizard for independent professionals.

#### Scenario: Independent individual completes basic profile
- **WHEN** an independent individual registers
- **THEN** Step 1 collects:
  - Professional name (required, e.g., "Ali - Mobile Barber")
  - Category (required, single selection)
  - Bio/description (required, min 100 characters)
  - Specialties (optional, multi-select tags)
- **AND** validates name uniqueness
- **AND** provides example bios for inspiration
- **AND** shows character count for bio

#### Scenario: Independent individual sets service area
- **WHEN** an independent individual sets where they work
- **THEN** Step 2 provides options:
  - "I'm mobile - I go to customers"
  - "I have a fixed location"
  - "Both mobile and fixed location"
- **AND** for mobile: collects service area radius and center point
- **AND** for fixed: collects address and map coordinates
- **AND** shows coverage area on map
- **AND** allows adjusting service radius

#### Scenario: Independent individual sets working hours
- **WHEN** an independent individual configures schedule
- **THEN** Step 3 provides:
  - Flexible schedule editor (same as organization)
  - Option to mark as "flexible/by appointment"
  - Typical availability indicators
- **AND** explains that schedule can be updated anytime
- **AND** allows skipping if truly flexible

#### Scenario: Independent individual uploads profile photo
- **WHEN** an independent individual uploads their photo
- **THEN** Step 4 allows:
  - Profile photo/avatar upload (required, max 2MB)
  - Gallery images (optional, up to 5 images)
  - Portfolio/before-after images
  - Image cropping for proper aspect ratio (1:1)
- **AND** provides guidelines for professional photos
- **AND** shows preview of how photo appears

#### Scenario: Independent individual adds services
- **WHEN** an independent individual defines their services
- **THEN** Step 5 allows:
  - Adding services with details (name, description, duration, price)
  - At least one service required
  - Service categorization
  - Featured service marking
- **AND** validates pricing and duration
- **AND** provides service templates by category
- **AND** allows quick service creation

#### Scenario: Independent individual submits for verification
- **WHEN** an independent individual completes registration
- **THEN** the system shows profile preview
- **AND** requires terms acceptance
- **AND** submits for admin verification
- **AND** sets status to PendingVerification
- **AND** sends confirmation notification
- **AND** allows profile editing while pending

### Requirement: Invitation-Based Registration Flow
Individuals invited by organizations SHALL complete a simplified registration flow.

#### Scenario: Invited individual accesses invitation link
- **WHEN** an invited person clicks the invitation link from SMS
- **THEN** the system validates the invitation token
- **AND** checks invitation hasn't expired (7 days)
- **AND** displays organization name and logo
- **AND** shows invitation message: "Elite Hair Salon invited you to join their team"
- **AND** requires phone verification

#### Scenario: Invited individual completes phone verification
- **WHEN** the invitee completes phone verification
- **THEN** the system checks if phone matches invitation
- **AND** checks if user has existing account
- **AND** for existing users: links account to invitation
- **AND** for new users: creates account
- **AND** proceeds to profile completion

#### Scenario: Invited individual completes staff profile
- **WHEN** the invitee completes their staff profile
- **THEN** the system pre-fills organization affiliation
- **AND** Step 1 collects: Name, role/title, bio, specialties
- **AND** Step 2: Upload avatar/profile photo
- **AND** Step 3: Add services (from org catalog or custom)
- **AND** Step 4: Set working hours (within org hours)
- **AND** Step 5: Set pricing (within org policies)
- **AND** shows organization context throughout
- **AND** requires fewer fields than independent registration

#### Scenario: Invited individual submits profile for approval
- **WHEN** the invitee submits their completed profile
- **THEN** the system sets status to PendingOrganizationApproval
- **AND** notifies the organization owner
- **AND** allows the organization to review before making public
- **AND** sends confirmation to invitee
- **AND** allows invitee to edit while pending

### Requirement: Join Request Registration Flow
Individuals requesting to join organizations SHALL complete a full profile before submitting request.

#### Scenario: Individual searches for organizations
- **WHEN** a user chooses to join an existing business
- **THEN** the system provides organization search with filters:
  - Location (city, neighborhood)
  - Category (salons, clinics, spas)
  - Business name keyword search
- **AND** displays organization cards with basic info
- **AND** shows "Open to join requests" badge if applicable
- **AND** allows viewing full organization profile

#### Scenario: Individual initiates join request
- **WHEN** a user selects an organization to join
- **THEN** the system explains the join request process
- **AND** requires completing individual profile first
- **AND** allows attaching a message/cover letter
- **AND** shows organization's requirements (if any)

#### Scenario: Individual completes profile for join request
- **WHEN** the requester completes their profile
- **THEN** the system collects:
  - Full professional profile (name, bio, specialties)
  - Profile photo and portfolio images
  - Services offered and pricing
  - Preferred working hours
  - Experience and qualifications
  - Message to organization
- **AND** validates all fields
- **AND** shows profile preview as organization will see it
- **AND** allows saving as draft

#### Scenario: Individual submits join request
- **WHEN** the requester submits their join request
- **THEN** the system creates a join request record
- **AND** notifies the organization immediately
- **AND** sets individual status to PendingOrganizationApproval
- **AND** allows tracking request status
- **AND** prevents duplicate requests to same organization
- **AND** allows withdrawing request before review

## MODIFIED Requirements

### Requirement: Multi-Step Registration Process
The system SHALL guide providers through a step-by-step registration process, **adapted to their provider type**.

**Changes**: Registration is now split into three distinct flows:
1. Organization Registration (6-7 steps)
2. Independent Individual Registration (5-6 steps)
3. Invited Individual Registration (4-5 steps, simplified)

Each flow is optimized for its use case with appropriate field requirements and guidance.

#### Scenario: Registration progress tracking (all types)
- **WHEN** a user is partway through registration
- **THEN** the system displays a progress indicator showing:
  - Current step number and name
  - Total steps remaining
  - Percentage complete
  - Ability to navigate to previous steps
- **AND** saves draft progress automatically
- **AND** allows returning later to complete
- **AND** expires incomplete registrations after 30 days

#### Scenario: Registration validation (all types)
- **WHEN** a user attempts to proceed to next step
- **THEN** the system validates all required fields in current step
- **AND** displays inline error messages for invalid fields
- **AND** prevents progression until all errors are resolved
- **AND** highlights first error field
- **AND** provides helpful error messages in Persian

## REMOVED Requirements

None - existing registration requirements are extended, not removed.
