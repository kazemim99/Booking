# Provider Hierarchy - OpenSpec Change Proposal

**Status**: 📋 Proposal Ready for Review
**Created**: 2025-11-22
**Priority**: High
**Complexity**: Large (5-6 weeks)

---

## 📖 Quick Summary

This proposal introduces a **hierarchical provider model** to support real-world business scenarios where:
- **Organizations** (salons, clinics) can manage multiple **Individual** professionals as staff
- **Solo business owners** can start alone and seamlessly add staff later without restructuring
- **Independent professionals** (freelancers, mobile barbers) work autonomously
- **Customers** can book with specific professionals, not just "the business"

---

## 🎯 Problem Being Solved

### Current Limitation
The system treats all providers as single-entity businesses. This breaks down for:
- ✗ Salons with multiple barbers (can't give each barber their own profile/schedule)
- ✗ Solo owners who grow their business (no clear path to add staff)
- ✗ Customers wanting to book specific professionals (can't select preferred barber)

### Real-World Example
**Elite Hair Salon** has:
- Ali (owner/barber) - specializes in men's cuts
- Reza (barber) - specializes in beards
- Sara (stylist) - specializes in women's hair

**Current System**: All bookings go to "Elite Hair Salon" (unclear who performs service)
**Proposed System**: Customers select specific barber → Ali, Reza, or Sara

---

## 💡 Proposed Solution

### Two Provider Types

1. **Organization Provider**
   - Represents physical business (salon, clinic)
   - Has brand identity (logo, location, overall hours)
   - Can work solo initially (owner handles bookings)
   - Can invite/manage staff members
   - Appears in search as primary entity

2. **Individual Provider**
   - Represents a person (barber, stylist, therapist)
   - Can be **independent** (solo freelancer) OR **linked** to organization
   - Has own profile, avatar, bio, services, schedule
   - Receives direct bookings from customers
   - Manages own calendar within org constraints (if linked)

### Hierarchical Relationship
```
Organization: Elite Hair Salon
├── Individual: Ali (Owner/Barber)
├── Individual: Reza (Beard Specialist)
└── Individual: Sara (Women's Stylist)
```

---

## 🚀 Key Features

### 1. **Multiple Registration Paths**
- **Organization Registration**: For business owners with physical location
- **Independent Individual**: For freelancers/mobile professionals
- **Invitation Flow**: Organizations invite staff via phone number
- **Join Request Flow**: Individuals request to join existing organizations

### 2. **Solo → Multi-Staff Growth**
- Organization starts solo (owner handles all bookings)
- Owner adds first staff member → booking flow adapts
- Seamless transition, no data migration needed

### 3. **Smart Booking Flow**
- **Solo Organization**: Direct booking (no staff selection)
- **Multi-Staff Organization**: Customer selects specific professional
- **Independent Individual**: Direct booking (as before)

### 4. **Conversion Support**
- Independent Individual → Organization (when hiring staff)
- Preserves all bookings, reviews, SEO rankings
- Same provider ID (URLs don't break)

---

## 📁 Files in This Proposal

| File | Purpose |
|------|---------|
| **[proposal.md](./proposal.md)** | Why, what, impact summary |
| **[design.md](./design.md)** | Technical decisions, architecture, trade-offs |
| **[tasks.md](./tasks.md)** | 23 sections, 200+ implementation tasks |
| **specs/provider-management/spec.md** | Hierarchy requirements and scenarios |
| **specs/staff-management/spec.md** | Updated staff model (now Individual Providers) |
| **specs/provider-registration/spec.md** | Three registration flows |

---

## 🗓️ Implementation Timeline

| Phase | Duration | Focus |
|-------|----------|-------|
| **Phase 1** | Week 1-2 | Database schema, migrations, domain model |
| **Phase 2** | Week 2-3 | Registration flows (Organization, Individual, Invitation) |
| **Phase 3** | Week 3-4 | Staff management UI, hierarchy display |
| **Phase 4** | Week 4-5 | Booking flow updates, testing |
| **Phase 5** | Week 5-6 | Conversion tool, polish, gradual rollout |

**Total**: ~5-6 weeks with 2-3 developers

---

## 🔥 Breaking Changes

⚠️ **This is a breaking change** - requires careful migration:

1. **Database Schema**: New columns `type`, `parent_provider_id`, `is_independent`
2. **Booking Entity**: Needs `individual_provider_id` to track which staff handled service
3. **Search Logic**: Must display hierarchical results
4. **Registration Flow**: Splits into 3 distinct paths
5. **Existing Data**: All current providers become Organization type by default

**Migration Strategy**: Feature flag + gradual rollout + backward-compatible APIs

---

## 📊 Success Metrics

### Business Metrics
- % of organizations adding staff (target: >30% within 6 months)
- Solo → Multi-staff conversion rate
- Customer booking completion rate improvement
- Average staff count per organization

### Technical Metrics
- Hierarchy query performance <50ms (p95)
- Invitation acceptance rate >60% within 7 days
- Registration completion >70% for each flow
- Zero data integrity violations

---

## 🧪 Testing Strategy

- **Unit Tests**: All domain logic (Provider aggregate, validations)
- **Integration Tests**: Commands, queries, repositories
- **API Tests**: All new endpoints
- **E2E Tests**: Complete registration flows, booking flows
- **Performance Tests**: Hierarchical queries, search with staff
- **Migration Tests**: Existing provider data conversion

---

## 🚨 Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Complex database migration | Extensive staging testing, phase migrations |
| User confusion during registration | Clear UI copy, smart recommendations, preview mode |
| Performance impact of hierarchy queries | Indexes, eager loading, caching, denormalization |
| Booking flow breaking changes | Feature flag, backward-compatible APIs, gradual rollout |
| Invitation workflow abuse | Rate limiting, expiration, verification required |

---

## 🎯 Decision Points Needed

Before implementation, we need decisions on:

1. **Payment Distribution**: Who receives payment when customer books individual at organization?
   - Option A: Organization (100%) - handles staff payment offline
   - Option B: Platform splits commission based on organization settings
   - **Recommendation**: Option A for Phase 1, Option B future enhancement

2. **Organization Owner as Staff**: Should owner appear as explicit staff member?
   - Option A: Owner is separate Individual Provider under org
   - Option B: Organization itself bookable (owner implicit)
   - **Recommendation**: Option B for simplicity

3. **Service Catalog Control**: How much control does organization have over staff services?
   - Option A: Org has master catalog, staff select from it only
   - Option B: Org catalog + staff can add custom services
   - **Recommendation**: Option B (configurable per organization)

4. **Admin Approval**: Who approves org-invited individuals?
   - Option A: Organization approval only (faster)
   - Option B: Organization + Admin approval (safer)
   - **Recommendation**: Option A for invited, Option B for join requests

---

## 📚 Related Documentation

- **[Provider Management Spec](../../specs/provider-management/spec.md)** - Current provider model
- **[Staff Management Spec](../../specs/staff-management/spec.md)** - Current staff model
- **[Registration Spec](../../specs/provider-registration/spec.md)** - Current registration flow
- **[UX Design: Role-Based Navigation](../../../docs/UX_ROLE_BASED_NAVIGATION.md)** - Related UX patterns

---

## ✅ Next Steps

1. **Review & Approve** this proposal
2. **Clarify decision points** (listed above)
3. **Validate with stakeholders** (business team, UX team)
4. **Create project plan** with detailed sprint breakdown
5. **Begin Phase 1**: Database schema design and migrations

---

## 💬 Questions or Feedback?

This proposal is ready for:
- Technical review (architecture, database design)
- Business review (does it match real-world needs?)
- UX review (are the flows intuitive?)
- Security review (invitation workflow, data access)

Please provide feedback on any aspect - this is a foundational change that will affect many parts of the system!

---

**Last Updated**: 2025-11-22
**Authors**: AI Assistant + Product Team
**Reviewers**: [To be assigned]
