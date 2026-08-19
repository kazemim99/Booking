# Provider Hierarchy Design Document

## Context

The current Booksy system models all service providers as single-entity businesses. This works for solo professionals but breaks down for multi-staff businesses like salons, clinics, and spas where:

- Multiple professionals work under one business brand
- Each professional needs individual scheduling and booking capabilities
- Customers want to book with specific professionals, not just "the salon"
- Business owners start solo but need to scale by hiring staff

**Stakeholders**:
- Solo business owners who may hire staff later
- Salon/clinic owners managing multiple staff
- Individual professionals (barbers, stylists, therapists) working at businesses
- Customers booking services with specific professionals
- Platform administrators managing provider approvals

**Constraints**:
- Must maintain backward compatibility with existing providers
- Must preserve existing bookings, reviews, and provider data
- Must support both solo and multi-staff businesses
- Must work within existing DDD bounded contexts (ServiceCatalog, UserManagement)

## Goals / Non-Goals

### Goals
1. **Support hierarchical provider relationships**
   - Organizations (salons, clinics) as parent entities
   - Individuals (barbers, stylists) as child entities or independent

2. **Enable seamless business growth**
   - Solo Organization → Organization with staff (no restructuring)
   - Clear conversion path from Individual to Organization

3. **Improve customer booking experience**
   - Select specific professional when booking
   - View professional's individual profile, schedule, reviews

4. **Maintain data integrity**
   - Preserve bookings when providers change type
   - Clear ownership of services, schedules, and bookings

5. **Support multiple business models**
   - Independent freelancers (mobile barbers)
   - Solo business owners (single stylist with shop)
   - Multi-staff businesses (salons with 5+ professionals)
   - Mixed models (salon owner who also provides services)

### Non-Goals
1. **Multi-location franchises** - Not addressing chains/franchises in this change (future enhancement)
2. **Commission/payment splits** - Financial arrangements between organization and staff (future enhancement)
3. **Complex scheduling conflicts** - Advanced resource management (future enhancement)
4. **Staff roles/permissions** - Detailed RBAC for staff (future enhancement)

## Decisions

### Decision 1: Two Provider Types (Organization vs Individual)

**Choice**: Introduce `ProviderType` enum with two values: `Organization` and `Individual`

**Rationale**:
- **Simplicity**: Two types cover all scenarios (business entity vs person)
- **Clarity**: Clear distinction between "business" and "professional"
- **Extensibility**: Can add types later (Franchise, TemporaryEvent) if needed
- **Domain alignment**: Matches real-world mental models

**Alternatives Considered**:
1. ❌ **Single type with flags** - Too confusing, unclear semantics
2. ❌ **Three types (Org, Solo, Group)** - Overcomplicates, Solo = Org with no staff
3. ❌ **Nested Staff entity** - Staff can't have full provider capabilities

### Decision 2: Hierarchical Relationship via ParentProviderId

**Choice**: Add nullable `ParentProviderId` to Provider aggregate

**Rationale**:
- **Simplicity**: Single foreign key relationship
- **Flexibility**: Individuals can be independent (null) or linked (non-null)
- **Query efficiency**: Easy to load organization with all staff
- **Integrity**: Database constraints ensure valid relationships

**Alternatives Considered**:
1. ❌ **Separate StaffMember table** - Duplicates provider data, complex joins
2. ❌ **Many-to-many relationship** - Overengineered, individuals shouldn't work at multiple orgs simultaneously
3. ❌ **Graph database** - Too complex for simple parent-child hierarchy

### Decision 3: Organization as Default for Business Owners

**Choice**: Recommend Organization type for anyone with physical location, even if solo

**Rationale**:
- **Future-proof**: No migration needed when hiring first staff member
- **Professional branding**: Separates business identity from personal identity
- **Customer expectations**: "Elite Hair Salon" sounds more established than "Ali's Cuts"
- **Booking flow consistency**: Same flow whether solo or multi-staff

**Alternatives Considered**:
1. ❌ **Individual as default** - Requires migration when hiring, confusing
2. ❌ **Force conversion** - Disruptive to existing solo businesses

### Decision 4: Invitation-First with Optional Request-to-Join

**Choice**: Primary flow is Organization invites Individual; secondary flow is Individual requests to join

**Rationale**:
- **Security**: Organization controls who represents their brand
- **Trust**: Prevents fraudulent claims of employment
- **Flexibility**: Request-to-join allows discovery and networking
- **Real-world match**: Employer typically initiates hiring, not employee

**Alternatives Considered**:
1. ❌ **Auto-approval** - Too risky, allows fraud
2. ❌ **Request-only** - Burdens solo owners who aren't tech-savvy
3. ❌ **Manual admin approval for all** - Doesn't scale

### Decision 5: Solo Organizations Accept Direct Bookings

**Choice**: Organizations with no staff accept bookings directly (no staff selection step)

**Rationale**:
- **User experience**: Solo owner shouldn't need extra click
- **Simplicity**: Booking flow adapts to organization's state
- **Gradual transition**: Adding first staff changes flow naturally
- **Performance**: No unnecessary staff lookup queries

**Alternatives Considered**:
1. ❌ **Always require staff selection** - Poor UX for solo owners
2. ❌ **Create default staff member for owner** - Confusing, duplicate entities

### Decision 6: Preserve Provider IDs During Conversion

**Choice**: When Individual converts to Organization, keep same ProviderId

**Rationale**:
- **URL preservation**: Public profile URLs remain valid
- **SEO benefits**: Search engine rankings preserved
- **Bookmark compatibility**: Customer bookmarks still work
- **Simpler migration**: No need to update references everywhere

**Alternatives Considered**:
1. ❌ **Create new ID** - Breaks URLs, loses SEO, complex data migration
2. ❌ **Use aliasing** - Added complexity, confusing queries

### Decision 7: Booking Belongs to Individual, Not Organization

**Choice**: Bookings always reference an IndividualProviderId, never just OrganizationProviderId

**Rationale**:
- **Accountability**: Clear who performed the service
- **Reviews**: Customers review the person, not just the business
- **Scheduling**: Avoid conflicts, each person has their own calendar
- **Analytics**: Track individual performance metrics

**Implementation for Solo Organizations**:
- Create implicit "owner" Individual Provider
- Or allow nullable IndividualProviderId (organization-level booking)

**Alternatives Considered**:
1. ❌ **Organization-only bookings** - Can't track who did the work
2. ❌ **Dual reference (Org + Individual)** - Redundant, complex queries

## Risks / Trade-offs

### Risk 1: Database Migration Complexity
**Mitigation**:
- Existing providers default to Organization type
- Add columns with defaults to avoid null issues
- Migrate in phases: schema first, then data, then application logic
- Extensive testing on staging with production data clone

### Risk 2: Breaking Changes to Booking Flow
**Mitigation**:
- Feature flag for gradual rollout
- Backward-compatible API endpoints (v1 vs v2)
- Clear documentation and migration guide
- Support period for old flow

### Risk 3: User Confusion During Registration
**Mitigation**:
- Clear UI copy explaining each type
- Smart recommendations based on business type questions
- Preview of how profile will appear
- Allow type change during onboarding (before going live)

### Risk 4: Performance Impact of Hierarchical Queries
**Mitigation**:
- Index on ParentProviderId
- Eager loading of staff members when needed
- Cache organization + staff structure
- Denormalize common queries if needed

### Risk 5: Invitation/Approval Workflow Complexity
**Mitigation**:
- Clear email/SMS notifications at each step
- Status tracking dashboard for organizations
- Timeout for expired invitations (7 days)
- Simple retry mechanism for failed invitations

## Migration Plan

### Phase 1: Database Schema (Week 1)
```sql
-- Add new columns
ALTER TABLE providers
  ADD COLUMN type INTEGER NOT NULL DEFAULT 0,  -- 0 = Organization
  ADD COLUMN parent_provider_id UUID NULL,
  ADD COLUMN is_independent BOOLEAN NOT NULL DEFAULT true,
  ADD COLUMN allow_independent_staff BOOLEAN NOT NULL DEFAULT true;

-- Add indexes
CREATE INDEX idx_providers_parent ON providers(parent_provider_id);
CREATE INDEX idx_providers_type ON providers(type);

-- Add constraints
ALTER TABLE providers
  ADD CONSTRAINT fk_providers_parent
    FOREIGN KEY (parent_provider_id)
    REFERENCES providers(id) ON DELETE CASCADE;

-- Create invitation tables
CREATE TABLE provider_invitations (
  id UUID PRIMARY KEY,
  organization_id UUID NOT NULL,
  phone_number VARCHAR(20) NOT NULL,
  invitee_name VARCHAR(100),
  status INTEGER NOT NULL,  -- 0=Pending, 1=Accepted, 2=Expired, 3=Rejected
  created_at TIMESTAMP NOT NULL,
  expires_at TIMESTAMP NOT NULL,
  FOREIGN KEY (organization_id) REFERENCES providers(id)
);

CREATE TABLE provider_join_requests (
  id UUID PRIMARY KEY,
  organization_id UUID NOT NULL,
  requester_id UUID NOT NULL,
  message TEXT,
  status INTEGER NOT NULL,  -- 0=Pending, 1=Approved, 2=Rejected
  created_at TIMESTAMP NOT NULL,
  reviewed_at TIMESTAMP NULL,
  FOREIGN KEY (organization_id) REFERENCES providers(id),
  FOREIGN KEY (requester_id) REFERENCES providers(id)
);
```

### Phase 2: Domain Model (Week 1-2)
- Enhance Provider aggregate with Type, ParentProviderId
- Add domain events: ProviderConvertedToOrganization, StaffMemberAdded
- Create InviteStaffMemberCommand
- Create ApproveJoinRequestCommand
- Update validation rules for hierarchical constraints

### Phase 3: Application Layer (Week 2-3)
- New registration commands for each type
- Invitation workflow handlers
- Join request workflow handlers
- Migration command (Individual → Organization)
- Updated query services for hierarchical display

### Phase 4: API Layer (Week 3)
- New endpoints: POST /v1/providers/organizations
- New endpoints: POST /v1/providers/individuals
- New endpoints: POST /v1/providers/{id}/staff/invite
- New endpoints: POST /v1/providers/{id}/join-requests
- Versioned endpoints to maintain backward compatibility

### Phase 5: Frontend (Week 3-5)
- Registration type selection flow
- Organization registration wizard
- Individual registration wizard
- Invitation management UI
- Staff management dashboard
- Hierarchical provider display in search

### Phase 6: Testing & Rollout (Week 5-6)
- Unit tests for domain logic
- Integration tests for workflows
- E2E tests for registration flows
- Staging deployment
- Gradual production rollout with feature flag
- Monitor metrics and error rates

## Rollback Plan

If critical issues arise:

1. **Immediate**: Disable new registration flows via feature flag
2. **Day 1-3**: Fix issues, deploy hotfix
3. **If unfixable**: Revert frontend to old registration, keep database schema
4. **Data cleanup**: Mark new hierarchy providers as inactive until fixed

No data loss - hierarchy columns nullable, can be ignored by old code.

## Open Questions

1. **Payment Distribution**: When customer books individual at organization, who receives payment?
   - **Option A**: Organization receives 100%, handles staff payment offline
   - **Option B**: Platform splits commission based on organization settings
   - **Decision**: Defer to Phase 2 (payment management feature)

2. **Multi-Organization Membership**: Can individual work at multiple organizations?
   - **Example**: Barber works at Salon A (Mon-Wed) and Salon B (Thu-Sat)
   - **Current Design**: No, ParentProviderId is singular
   - **Future**: Could support via many-to-many if needed

3. **Organization Owner as Individual**: Should owner appear as staff member?
   - **Option A**: Owner is separate Individual Provider under their org
   - **Option B**: Organization itself can be booked (owner implicit)
   - **Decision**: Option B for simplicity, can add owner as explicit staff if they want

4. **Verification/Approval**: Who approves new Individual Providers?
   - **Independent**: Platform admin approval (as before)
   - **Org-invited**: Organization approval only, or org + admin?
   - **Org-requested**: Organization approval, then admin?
   - **Recommendation**: Org-invited = org approval only; others = admin approval

5. **Service Catalog Inheritance**: Should individuals inherit org's service catalog?
   - **Pro**: Easier setup, consistency
   - **Con**: Less flexibility for specialists
   - **Recommendation**: Org has master catalog, individuals can add custom services

## Success Metrics

### Technical Metrics
- Provider hierarchy depth queries: <50ms (p95)
- Invitation acceptance rate: >60% within 7 days
- Registration completion rate: >70% for each flow
- Zero data integrity violations (orphaned relationships)

### Business Metrics
- Organizations adding staff: Track growth over time
- Solo → Multi-staff conversion rate
- Customer booking rate improvement for multi-staff orgs
- Average staff count per organization

### User Experience Metrics
- Registration drop-off rate by step
- Time to complete registration (each type)
- User confusion indicators (support tickets, FAQs)
- Booking flow satisfaction (customer surveys)
