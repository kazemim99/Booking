## Context

Full architecture review, problem catalog, DB/API detail, and scenario traceability live in the repo-root [IDENTITY_AND_STAFF_ARCHITECTURE.md](../../../IDENTITY_AND_STAFF_ARCHITECTURE.md). This file records only the **decisions approved for Phase 1** so implementers don't re-derive them.

Stakeholders: solo owners, salon owners with staff, professionals working at one or several salons, customers, platform admins. Constraints: modular monolith (ServiceCatalog + UserManagement, schema-per-context, in-process CAP events); preserve existing providers/bookings; migrations must be idempotent (repo rule).

## Goals / Non-Goals

- **Goals:** separate Person / Organization / OrganizationMembership / StaffProfile / per-org Roles; phone as the single global identity; invitation reuses accounts by phone; owner-as-staff and multi-salon membership supported; onboarding "provides services?" branch; comprehensive tests for all eight required scenarios.
- **Non-Goals (this change):** switch booking attribution fully to `MembershipId` (dual-write only); salon-switcher UX polish; large-scale dedupe of pre-existing duplicate accounts; Vue `booksy-frontend` alignment; commission/payment splits; multi-location franchises.

## Decisions

- **D1 — Approved model 3.3‑A.** A staff member's schedule/services live on a `StaffProfile` **owned by an `OrganizationMembership`**. No per-staff `Provider(Individual)` rows. Multi-salon = multiple memberships for one `PersonId`. (Rejected 3.3‑B: keeping Individual-Provider resources — retains the duplicate-Provider smell and complicates multi-org.)
- **D2 — `OrganizationMembership` is a new aggregate in ServiceCatalog** keyed by `MembershipId`, referencing `PersonId` (UserId) and `OrganizationId` (ProviderId). Invariants: at most **one non-terminated** membership per `(PersonId, OrganizationId)`; an org always has **≥1 Owner**; the last Owner cannot be terminated. Roles are a **set** so `{Owner, StaffProvider}` is expressible.
- **D3 — Person identity = `UserManagement.User`.** Phone becomes canonical E.164 and **globally unique** (partial unique index excluding soft-deleted). Enforcement is dual: DB index **and** an application guard (`ExistsByPhoneNumberAsync` + canonicalization) in every creation path. Unify customer/provider onto one Person per phone via `UserType.Both`.
- **D4 — Status-gated OTP.** OTP completion rejects `Banned/Suspended/Inactive/Deleted` before issuing tokens, routed through one guarded method (parity with `User.Authenticate()`).
- **D5 — Invitation resolves by phone.** `SendInvitation` normalizes/validates the phone, blocks self-invite and already-member, and (a) links an existing Person as `Invited` or (b) records an invitation with no Person yet. Acceptance activates a membership; the new-user path registers a Person first. Never creates a second Person, never a sub-Provider.
- **D6 — Onboarding branch.** A new step after Working Hours writes the owner's membership: Yes ⇒ `{Owner, StaffProvider}` + StaffProfile(`providesServices=true`); No ⇒ `{Owner}`, no StaffProfile.
- **D7 — Retain & rewire, don't rewrite.** Keep `ProviderInvitation`/`ProviderJoinRequest`; retire `Provider.RegisterStaffMember` and `ParentProviderId`-as-membership after backfill. Legacy staff endpoints become thin shims over membership commands during transition.
- **D8 — Soft-deleted phone is reclaimable (default).** Partial unique index excludes `Deleted`; a returning person on that phone is reactivated rather than duplicated. (Open to change to permanently-reserved if product prefers.)

## Risks / Trade-offs

- **Backfill surfaces real duplicate persons** → dry-run report first; repoint memberships to the surviving Person; manual review queue. This change ships the report + guards; bulk merge is a follow-up.
- **Booking attribution mid-transition** → dual-write `individual_provider_id` + `membership_id`; reads stay on the legacy path until a later phase.
- **DI/UoW collision** (documented in the audit — `TransactionBehavior` commits the ServiceCatalog UoW) → membership writes stay within ServiceCatalog's UoW; identity writes keep the established `SaveAndPublishEventsAsync` workaround.
- **Scope breadth** (touches auth, onboarding, invitations, Flutter) → feature flag `identity-membership-model`; each task group independently testable.

## Migration Plan

1. Identity: canonical phone + partial unique index; backfill `User.PhoneNumber` from profiles; fix `ExistsByEmailAsync`; add `ExistsByPhoneNumberAsync`; status-gated OTP. Emit a duplicate-phone report (no destructive merge here).
2. Membership schema (tables `organization_memberships`, `membership_roles`, `staff_profiles`) + aggregate + repositories + events (idempotent migration).
3. Backfill: each Organization → `{Owner}` membership for its `OwnerId`; each existing sub-Provider / Model-A staff record → StaffProfile (resolve `PersonId` by phone; else `PersonId=null` claimable).
4. Rewire writes (invitation/join/add-staff/onboarding) to memberships behind the flag.
5. Flutter surfaces on the new APIs.
6. Cleanup (later phase): drop `parent_provider_id`, delete dead `Staff.cs`/seeders/events, remove deprecated endpoints.

Rollback: flag gates reads; membership tables are additive; legacy columns retained until a later cleanup phase.

## Open Questions

- Soft-deleted phone reclaim vs permanent reservation (D8 default = reclaim).
- Whether owner-provides-services should be editable post-onboarding via the same membership role toggle (assumed yes; covered by change-roles API).
