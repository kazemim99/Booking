# Implementation Tasks

## 1. Database Schema & Migrations
- [x] 1.1 Create migration: Add `type`, `parent_provider_id`, `is_independent` columns to providers table
- [x] 1.2 Create migration: Add indexes on `parent_provider_id` and `type`
- [x] 1.3 Create migration: Add foreign key constraint for parent-child relationship
- [x] 1.4 Create migration: Create `provider_invitations` table
- [x] 1.5 Create migration: Create `provider_join_requests` table
- [x] 1.6 Create migration: Add `individual_provider_id` to bookings table
- [x] 1.7 Data migration: Set existing providers to type=Organization, is_independent=false
- [x] 1.8 Test migrations on staging database clone

## 2. Domain Model Enhancements
- [x] 2.1 Add `ProviderHierarchyType` enum (Organization, Individual) to domain
- [x] 2.2 Update Provider aggregate: Add HierarchyType, ParentProviderId, IsIndependent properties
- [x] 2.3 Add domain validation: Prevent circular parent-child relationships
- [x] 2.4 Add domain validation: Individuals can't have staff members
- [x] 2.5 Add domain method: `ConvertToOrganization()`
- [x] 2.6 Add domain method: `LinkToOrganization(organizationId)` / `UnlinkFromOrganization(reason)`
- [x] 2.7 Add domain method: `CanAcceptDirectBookings()`
- [x] 2.8 Create `ProviderInvitation` aggregate
- [x] 2.9 Create `ProviderJoinRequest` aggregate
- [x] 2.10 Add domain events: `ProviderConvertedToOrganization`, `StaffMemberAdded`, `StaffMemberRemoved`
- [x] 2.11 Add domain events: `InvitationSent`, `InvitationAccepted`, `JoinRequestApproved`
- [x] 2.12 Write unit tests for all domain logic

## 3. Application Layer - Commands
- [x] 3.1 Create `RegisterOrganizationProviderCommand` and handler
- [x] 3.2 Create `RegisterIndependentIndividualCommand` and handler
- [x] 3.3 Create `SendInvitationCommand` and handler
- [x] 3.4 Create `AcceptInvitationCommand` and handler
- [x] 3.5 Create `CreateJoinRequestCommand` and handler
- [x] 3.6 Create `ApproveJoinRequestCommand` and handler
- [x] 3.7 Create `RejectJoinRequestCommand` and handler
- [x] 3.8 Create `ConvertToOrganizationCommand` and handler
- [x] 3.9 Create `RemoveStaffMemberCommand` and handler
- [x] 3.10 Update `UpdateProviderCommand` to handle hierarchy constraints
- [x] 3.11 Add FluentValidation for all commands
- [x] 3.12 Write integration tests for all command handlers

## 4. Application Layer - Queries
- [x] 4.1 Create `GetProviderWithStaffQuery` and handler
- [x] 4.2 Create `GetStaffMembersQuery` and handler
- [x] 4.3 Create `GetPendingInvitationsQuery` and handler
- [x] 4.4 Create `GetPendingJoinRequestsQuery` and handler
- [x] 4.5 Update `SearchProvidersQuery` to include hierarchy information
- [x] 4.6 Update `GetProviderDetailsQuery` to include parent/staff info
- [x] 4.7 Write integration tests for all query handlers

## 5. Infrastructure Layer
- [x] 5.1 Update `ProviderRepository`: Add methods for hierarchical queries
- [x] 5.2 Create `ProviderInvitationRepository` (Read + Write)
- [x] 5.3 Create `ProviderJoinRequestRepository` (Read + Write)
- [x] 5.4 Update EF Core configurations for new entities and relationships
- [x] 5.5 Add database indexes for performance optimization
- [x] 5.6 Register new repositories in DI container
- [x] 5.7 Write repository integration tests

## 6. API Layer - Controllers
- [x] 6.1 Create `POST /api/v1/providers/organizations` endpoint
- [x] 6.2 Create `POST /api/v1/providers/individuals` endpoint
- [x] 6.3 Create `POST /api/v1/providers/{id}/hierarchy/invitations` endpoint
- [x] 6.4 Create `POST /api/v1/providers/{id}/hierarchy/invitations/{invitationId}/accept` endpoint
- [x] 6.5 Create `GET /api/v1/providers/{id}/hierarchy/staff` - List staff members
- [x] 6.6 Create `DELETE /api/v1/providers/{id}/hierarchy/staff/{staffId}` - Remove staff
- [x] 6.7 Create `POST /api/v1/providers/{id}/hierarchy/join-requests` endpoint
- [x] 6.8 Create `GET /api/v1/providers/{id}/hierarchy/join-requests` endpoint
- [x] 6.9 Create `POST /api/v1/providers/{id}/hierarchy/join-requests/{requestId}/approve` endpoint
- [x] 6.10 Create `POST /api/v1/providers/{id}/hierarchy/join-requests/{requestId}/reject` endpoint
- [x] 6.11 Create `POST /api/v1/providers/{id}/hierarchy/convert-to-organization` endpoint
- [x] 6.12 Update existing provider endpoints to return hierarchy info
- [x] 6.13 Add API versioning for breaking changes
- [x] 6.14 Update OpenAPI/Swagger documentation
- [x] 6.15 Write API integration tests

## 7. API Layer - DTOs
- [x] 7.1 Create `RegisterOrganizationRequest` and `Response` DTOs
- [x] 7.2 Create `RegisterIndividualRequest` and `Response` DTOs
- [x] 7.3 Create `InviteStaffMemberRequest` and `Response` DTOs
- [x] 7.4 Create `JoinRequestDto` with status and timestamps
- [x] 7.5 Create `ProviderHierarchyDto` for nested staff display
- [x] 7.6 Update existing `ProviderDto` to include type and hierarchy info
- [x] 7.7 Create AutoMapper profiles for all new DTOs

## 8. Frontend - Type Definitions
- [x] 8.1 Create `ProviderType` enum in TypeScript
- [x] 8.2 Create `OrganizationProvider` interface
- [x] 8.3 Create `IndividualProvider` interface
- [x] 8.4 Create `ProviderInvitation` interface
- [x] 8.5 Create `JoinRequest` interface
- [x] 8.6 Update existing `Provider` type to include hierarchy fields
- [x] 8.7 Create `StaffMember` interface

## 9. Frontend - API Services
- [x] 9.1 Create `registerOrganization()` service method
- [x] 9.2 Create `registerIndividual()` service method
- [x] 9.3 Create `inviteStaffMember()` service method
- [x] 9.4 Create `acceptInvitation()` service method
- [x] 9.5 Create `getOrganizationStaff()` service method
- [x] 9.6 Create `removeStaffMember()` service method
- [x] 9.7 Create `requestToJoinOrganization()` service method
- [x] 9.8 Create `getJoinRequests()` service method
- [x] 9.9 Create `approveJoinRequest()` service method
- [x] 9.10 Create `convertToOrganization()` service method
- [x] 9.11 Update existing provider services for hierarchy support

## 10. Frontend - Pinia Stores
- [x] 10.1 Update `providerStore` to handle provider types
- [x] 10.2 Create `staffStore` for managing organization staff
- [x] 10.3 Create `invitationStore` for invitation workflow
- [x] 10.4 Create `joinRequestStore` for join request workflow
- [x] 10.5 Add actions for all new workflows
- [x] 10.6 Add getters for hierarchy computations
- [x] 10.7 Write store unit tests

## 11. Frontend - Registration Flows
- [x] 11.1 Create `ProviderTypeSelection.vue` component (Organization vs Individual)
- [x] 11.2 Create `OrganizationRegistrationFlow.vue` wizard
- [x] 11.3 Create `IndividualRegistrationFlow.vue` wizard
- [x] 11.4 Update existing registration to route to appropriate flow
- [x] 11.5 Add smart recommendations based on business type questions (integrated in ProviderTypeSelection)
- [x] 11.6 Create progress indicators for each wizard (RegistrationProgressIndicator.vue)
- [x] 11.7 Add form validation for each step (integrated in step components)
- [x] 11.8 Add preview mode before final submission (OrganizationPreviewStep, IndividualPreviewStep)
- [ ] 11.9 Write component unit tests

## 12. Frontend - Staff Management UI
- [x] 12.1 Create `StaffManagementDashboard.vue` for organizations
- [x] 12.2 Create `StaffList.vue` component showing all staff members
- [x] 12.3 Create `InviteStaffModal.vue` component
- [x] 12.4 Create `StaffMemberCard.vue` component with actions
- [x] 12.5 Create `PendingInvitations.vue` list component (InvitationCard)
- [x] 12.6 Create `JoinRequestsList.vue` component for organization owners (JoinRequestCard)
- [ ] 12.7 Create notification system for new join requests
- [ ] 12.8 Add staff performance metrics (future phase)
- [ ] 12.9 Write component unit tests

## 13. Frontend - Invitation Flow UI
- [x] 13.1 Create `AcceptInvitation.vue` page (from SMS link)
- [x] 13.2 Create `CompleteStaffProfile.vue` wizard for invited individuals
- [ ] 13.3 Create SMS/email templates for invitation notifications
- [x] 13.4 Add invitation expiry handling (integrated in AcceptInvitation.vue)
- [ ] 13.5 Add resend invitation functionality
- [ ] 13.6 Write E2E tests for invitation flow

## 14. Frontend - Join Request Flow UI
- [x] 14.1 Create `SearchOrganizations.vue` component
- [x] 14.2 Create `RequestToJoinModal.vue` component
- [x] 14.3 Create `MyJoinRequests.vue` dashboard for individuals
- [x] 14.4 Create `ReviewJoinRequest.vue` component for org owners (JoinRequestCard already exists)
- [ ] 14.5 Add notification for join request status changes
- [ ] 14.6 Write E2E tests for join request flow

## 15. Frontend - Hierarchy Display
- [x] 15.1 Update `ProviderCard.vue` to show organization badge
- [x] 15.2 Create `ProviderHierarchy.vue` component (org + staff tree)
- [x] 15.3 Update search results to display nested staff
- [x] 15.4 Update provider detail page with staff selection
- [x] 15.5 Create `StaffSelector.vue` for booking flow
- [x] 15.6 Add "About this organization" section
- [ ] 15.7 Write component unit tests

## 16. Frontend - Conversion Tool
- [x] 16.1 Create `ConvertToOrganization.vue` wizard
- [x] 16.2 Add impact preview before conversion
- [x] 16.3 Add data migration progress indicator (built into wizard)
- [ ] 16.4 Add rollback option (if conversion fails)
- [x] 16.5 Create success confirmation with next steps
- [ ] 16.6 Write E2E tests for conversion flow

## 17. Booking Flow Updates
- [x] 17.1 Update `CreateBookingModal.vue` to handle staff selection
- [x] 17.2 Add staff availability check in booking logic
- [x] 17.3 Update booking confirmation to show individual provider
- [ ] 17.4 Update booking notifications (include staff name)
- [ ] 17.5 Update booking history to display individual provider
- [x] 17.6 Handle solo organization bookings (no staff selection)
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
