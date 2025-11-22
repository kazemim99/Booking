# Implementation Tasks

## 1. Database Schema & Migrations
- [ ] 1.1 Create migration: Add `type`, `parent_provider_id`, `is_independent` columns to providers table
- [ ] 1.2 Create migration: Add indexes on `parent_provider_id` and `type`
- [ ] 1.3 Create migration: Add foreign key constraint for parent-child relationship
- [ ] 1.4 Create migration: Create `provider_invitations` table
- [ ] 1.5 Create migration: Create `provider_join_requests` table
- [ ] 1.6 Create migration: Add `individual_provider_id` to bookings table
- [ ] 1.7 Data migration: Set existing providers to type=Organization, is_independent=false
- [ ] 1.8 Test migrations on staging database clone

## 2. Domain Model Enhancements
- [ ] 2.1 Add `ProviderType` enum (Organization, Individual) to domain
- [ ] 2.2 Update Provider aggregate: Add Type, ParentProviderId, IsIndependent properties
- [ ] 2.3 Add domain validation: Prevent circular parent-child relationships
- [ ] 2.4 Add domain validation: Individuals can't have staff members
- [ ] 2.5 Add domain method: `ConvertToOrganization()`
- [ ] 2.6 Add domain method: `AddStaffMember(individualProviderId)`
- [ ] 2.7 Add domain method: `CanAcceptDirectBookings()`
- [ ] 2.8 Create `ProviderInvitation` aggregate
- [ ] 2.9 Create `ProviderJoinRequest` aggregate
- [ ] 2.10 Add domain events: `ProviderConvertedToOrganization`, `StaffMemberAdded`, `StaffMemberRemoved`
- [ ] 2.11 Add domain events: `InvitationSent`, `InvitationAccepted`, `JoinRequestApproved`
- [ ] 2.12 Write unit tests for all domain logic

## 3. Application Layer - Commands
- [ ] 3.1 Create `RegisterOrganizationProviderCommand` and handler
- [ ] 3.2 Create `RegisterIndependentIndividualCommand` and handler
- [ ] 3.3 Create `InviteStaffMemberCommand` and handler
- [ ] 3.4 Create `AcceptInvitationCommand` and handler
- [ ] 3.5 Create `RequestToJoinOrganizationCommand` and handler
- [ ] 3.6 Create `ApproveJoinRequestCommand` and handler
- [ ] 3.7 Create `RejectJoinRequestCommand` and handler
- [ ] 3.8 Create `ConvertIndividualToOrganizationCommand` and handler
- [ ] 3.9 Create `RemoveStaffMemberCommand` and handler
- [ ] 3.10 Update `UpdateProviderCommand` to handle hierarchy constraints
- [ ] 3.11 Add FluentValidation for all commands
- [ ] 3.12 Write integration tests for all command handlers

## 4. Application Layer - Queries
- [ ] 4.1 Create `GetProviderWithStaffQuery` and handler
- [ ] 4.2 Create `GetOrganizationStaffListQuery` and handler
- [ ] 4.3 Create `GetPendingInvitationsQuery` and handler
- [ ] 4.4 Create `GetPendingJoinRequestsQuery` and handler
- [ ] 4.5 Update `SearchProvidersQuery` to include hierarchy information
- [ ] 4.6 Update `GetProviderDetailsQuery` to include parent/staff info
- [ ] 4.7 Write integration tests for all query handlers

## 5. Infrastructure Layer
- [ ] 5.1 Update `ProviderRepository`: Add methods for hierarchical queries
- [ ] 5.2 Create `ProviderInvitationRepository`
- [ ] 5.3 Create `ProviderJoinRequestRepository`
- [ ] 5.4 Update EF Core configurations for new entities and relationships
- [ ] 5.5 Add database indexes for performance optimization
- [ ] 5.6 Update caching strategy for provider hierarchy
- [ ] 5.7 Write repository integration tests

## 6. API Layer - Controllers
- [ ] 6.1 Create `POST /api/v1/providers/organizations` endpoint
- [ ] 6.2 Create `POST /api/v1/providers/individuals` endpoint
- [ ] 6.3 Create `POST /api/v1/providers/{id}/staff/invite` endpoint
- [ ] 6.4 Create `POST /api/v1/providers/{id}/staff/{staffId}` - Accept invitation
- [ ] 6.5 Create `GET /api/v1/providers/{id}/staff` - List staff members
- [ ] 6.6 Create `DELETE /api/v1/providers/{id}/staff/{staffId}` - Remove staff
- [ ] 6.7 Create `POST /api/v1/providers/{id}/join-requests` endpoint
- [ ] 6.8 Create `GET /api/v1/providers/{id}/join-requests` endpoint
- [ ] 6.9 Create `PUT /api/v1/providers/{id}/join-requests/{requestId}/approve` endpoint
- [ ] 6.10 Create `PUT /api/v1/providers/{id}/join-requests/{requestId}/reject` endpoint
- [ ] 6.11 Create `POST /api/v1/providers/{id}/convert-to-organization` endpoint
- [ ] 6.12 Update existing provider endpoints to return hierarchy info
- [ ] 6.13 Add API versioning for breaking changes
- [ ] 6.14 Update OpenAPI/Swagger documentation
- [ ] 6.15 Write API integration tests

## 7. API Layer - DTOs
- [ ] 7.1 Create `RegisterOrganizationRequest` and `Response` DTOs
- [ ] 7.2 Create `RegisterIndividualRequest` and `Response` DTOs
- [ ] 7.3 Create `InviteStaffMemberRequest` and `Response` DTOs
- [ ] 7.4 Create `JoinRequestDto` with status and timestamps
- [ ] 7.5 Create `ProviderHierarchyDto` for nested staff display
- [ ] 7.6 Update existing `ProviderDto` to include type and hierarchy info
- [ ] 7.7 Create AutoMapper profiles for all new DTOs

## 8. Frontend - Type Definitions
- [ ] 8.1 Create `ProviderType` enum in TypeScript
- [ ] 8.2 Create `OrganizationProvider` interface
- [ ] 8.3 Create `IndividualProvider` interface
- [ ] 8.4 Create `ProviderInvitation` interface
- [ ] 8.5 Create `JoinRequest` interface
- [ ] 8.6 Update existing `Provider` type to include hierarchy fields
- [ ] 8.7 Create `StaffMember` interface

## 9. Frontend - API Services
- [ ] 9.1 Create `registerOrganization()` service method
- [ ] 9.2 Create `registerIndividual()` service method
- [ ] 9.3 Create `inviteStaffMember()` service method
- [ ] 9.4 Create `acceptInvitation()` service method
- [ ] 9.5 Create `getOrganizationStaff()` service method
- [ ] 9.6 Create `removeStaffMember()` service method
- [ ] 9.7 Create `requestToJoinOrganization()` service method
- [ ] 9.8 Create `getJoinRequests()` service method
- [ ] 9.9 Create `approveJoinRequest()` service method
- [ ] 9.10 Create `convertToOrganization()` service method
- [ ] 9.11 Update existing provider services for hierarchy support

## 10. Frontend - Pinia Stores
- [ ] 10.1 Update `providerStore` to handle provider types
- [ ] 10.2 Create `staffStore` for managing organization staff
- [ ] 10.3 Create `invitationStore` for invitation workflow
- [ ] 10.4 Create `joinRequestStore` for join request workflow
- [ ] 10.5 Add actions for all new workflows
- [ ] 10.6 Add getters for hierarchy computations
- [ ] 10.7 Write store unit tests

## 11. Frontend - Registration Flows
- [ ] 11.1 Create `ProviderTypeSelection.vue` component (Organization vs Individual)
- [ ] 11.2 Create `OrganizationRegistrationFlow.vue` wizard
- [ ] 11.3 Create `IndividualRegistrationFlow.vue` wizard
- [ ] 11.4 Update existing registration to route to appropriate flow
- [ ] 11.5 Add smart recommendations based on business type questions
- [ ] 11.6 Create progress indicators for each wizard
- [ ] 11.7 Add form validation for each step
- [ ] 11.8 Add preview mode before final submission
- [ ] 11.9 Write component unit tests

## 12. Frontend - Staff Management UI
- [ ] 12.1 Create `StaffManagementDashboard.vue` for organizations
- [ ] 12.2 Create `StaffList.vue` component showing all staff members
- [ ] 12.3 Create `InviteStaffModal.vue` component
- [ ] 12.4 Create `StaffMemberCard.vue` component with actions
- [ ] 12.5 Create `PendingInvitations.vue` list component
- [ ] 12.6 Create `JoinRequestsList.vue` component for organization owners
- [ ] 12.7 Create notification system for new join requests
- [ ] 12.8 Add staff performance metrics (future phase)
- [ ] 12.9 Write component unit tests

## 13. Frontend - Invitation Flow UI
- [ ] 13.1 Create `AcceptInvitation.vue` page (from SMS link)
- [ ] 13.2 Create `CompleteStaffProfile.vue` wizard for invited individuals
- [ ] 13.3 Create SMS/email templates for invitation notifications
- [ ] 13.4 Add invitation expiry handling
- [ ] 13.5 Add resend invitation functionality
- [ ] 13.6 Write E2E tests for invitation flow

## 14. Frontend - Join Request Flow UI
- [ ] 14.1 Create `SearchOrganizations.vue` component
- [ ] 14.2 Create `RequestToJoinModal.vue` component
- [ ] 14.3 Create `MyJoinRequests.vue` dashboard for individuals
- [ ] 14.4 Create `ReviewJoinRequest.vue` component for org owners
- [ ] 14.5 Add notification for join request status changes
- [ ] 14.6 Write E2E tests for join request flow

## 15. Frontend - Hierarchy Display
- [ ] 15.1 Update `ProviderCard.vue` to show organization badge
- [ ] 15.2 Create `ProviderHierarchy.vue` component (org + staff tree)
- [ ] 15.3 Update search results to display nested staff
- [ ] 15.4 Update provider detail page with staff selection
- [ ] 15.5 Create `StaffSelector.vue` for booking flow
- [ ] 15.6 Add "About this organization" section
- [ ] 15.7 Write component unit tests

## 16. Frontend - Conversion Tool
- [ ] 16.1 Create `ConvertToOrganization.vue` wizard
- [ ] 16.2 Add impact preview before conversion
- [ ] 16.3 Add data migration progress indicator
- [ ] 16.4 Add rollback option (if conversion fails)
- [ ] 16.5 Create success confirmation with next steps
- [ ] 16.6 Write E2E tests for conversion flow

## 17. Booking Flow Updates
- [ ] 17.1 Update `CreateBookingModal.vue` to handle staff selection
- [ ] 17.2 Add staff availability check in booking logic
- [ ] 17.3 Update booking confirmation to show individual provider
- [ ] 17.4 Update booking notifications (include staff name)
- [ ] 17.5 Update booking history to display individual provider
- [ ] 17.6 Handle solo organization bookings (no staff selection)
- [ ] 17.7 Write E2E tests for updated booking flows

## 18. Search & Discovery
- [ ] 18.1 Update search indexing to include hierarchy
- [ ] 18.2 Update search filters to handle organization vs individual
- [ ] 18.3 Add "Professionals at this location" section in search
- [ ] 18.4 Update SEO metadata for hierarchical providers
- [ ] 18.5 Test search relevance with new structure

## 19. Admin Tools
- [ ] 19.1 Create admin dashboard for viewing provider hierarchy
- [ ] 19.2 Add admin tools for managing orphaned individuals
- [ ] 19.3 Add admin approval workflow for conversions (if needed)
- [ ] 19.4 Create reports on organization growth metrics
- [ ] 19.5 Add bulk operations for data cleanup

## 20. Testing & Quality Assurance
- [ ] 20.1 Write unit tests for all domain entities
- [ ] 20.2 Write integration tests for all commands and queries
- [ ] 20.3 Write API integration tests for all endpoints
- [ ] 20.4 Write frontend unit tests for all components
- [ ] 20.5 Write E2E tests for complete registration flows
- [ ] 20.6 Write E2E tests for invitation workflow
- [ ] 20.7 Write E2E tests for join request workflow
- [ ] 20.8 Write E2E tests for booking with staff selection
- [ ] 20.9 Performance testing for hierarchical queries
- [ ] 20.10 Load testing for invitation notifications
- [ ] 20.11 Security testing for authorization rules
- [ ] 20.12 Accessibility testing for all new UI components

## 21. Documentation
- [ ] 21.1 Update API documentation (OpenAPI/Swagger)
- [ ] 21.2 Create migration guide for existing providers
- [ ] 21.3 Create user guide for organization owners
- [ ] 21.4 Create user guide for individual professionals
- [ ] 21.5 Update architecture documentation with hierarchy diagrams
- [ ] 21.6 Create troubleshooting guide for common issues
- [ ] 21.7 Update README with new features

## 22. Deployment & Rollout
- [ ] 22.1 Create feature flag: `provider-hierarchy-enabled`
- [ ] 22.2 Deploy database migrations to staging
- [ ] 22.3 Deploy backend changes to staging
- [ ] 22.4 Deploy frontend changes to staging
- [ ] 22.5 Smoke test all flows on staging
- [ ] 22.6 Deploy to production with feature flag OFF
- [ ] 22.7 Enable feature flag for internal testing (10% users)
- [ ] 22.8 Monitor error rates and performance metrics
- [ ] 22.9 Gradually increase rollout to 50%, then 100%
- [ ] 22.10 Remove feature flag after stable rollout

## 23. Post-Launch Monitoring
- [ ] 23.1 Monitor registration completion rates by type
- [ ] 23.2 Monitor invitation acceptance rates
- [ ] 23.3 Monitor join request approval rates
- [ ] 23.4 Track conversion from individual to organization
- [ ] 23.5 Monitor booking flow performance
- [ ] 23.6 Collect user feedback via surveys
- [ ] 23.7 Address bug reports and issues
- [ ] 23.8 Plan iteration based on user feedback
