# Phase 2 · item 2c — Making membership members bookable (design note)

**Status:** design / decision needed — **not yet implemented.** Written after tracing
the booking stack so the change is made deliberately, not rushed into shared code.

## What the trace found

Booking resolves "staff" as **sub-provider `Individual` providers** everywhere:

- `AvailabilityService.cs:529`, `GetQualifiedStaffQueryHandler`, `GetProviderStaffQueryHandler`,
  `GetProviderWithStaffQueryHandler`, `GetStaffMembersQueryHandler`, `GetProviderByIdQueryHandler`
  all call `IProviderReadRepository.GetStaffByOrganizationIdAsync(...)` → providers with
  `HierarchyType=Individual` + `ParentProviderId = org`.
- Availability slots (`ProviderAvailability.StaffId`) and service qualification
  (`Service.QualifiedStaff`, `AddQualifiedStaff`/`IsStaffQualified`) key staff by a **plain
  `Guid staffId`** — today the sub-provider's `ProviderId.Value`. **Nothing constrains that
  Guid to be a ProviderId** — a `MembershipId` works equally well.
- Bookings carry the assigned `staffId` (Guid) the same way.

**Implication:** membership `StaffProfile` members are currently a *directory only* — they are
invisible to booking because no availability/qualification exists for them and the composer
catalog lists sub-providers.

## The coexistence constraint

The sub-provider staff model is still **load-bearing for the Vue frontend** (`staff.service.ts`
GET/POST `/Providers/{id}/staff`) and for all existing bookings. Until Vue migrates to
memberships, the two models **must coexist** — we cannot re-key booking onto memberships and
delete the sub-provider path in one step.

## Options

### Option A — `MembershipId`-as-`staffId` bridge (recommended, incremental)
Treat an active `StaffProfile` membership as a bookable resource keyed by its `MembershipId`:
1. On StaffProfile activation (owner-provides-services, invite/register-accept), **qualify** the
   member for the org's services (`Service.AddQualifiedStaff(membershipId)`) and **generate
   availability** from org hours keyed by `StaffId = membershipId` (reuse the logic in
   `AddStaffToProviderCommandHandler.GenerateStaffAvailabilityAsync`).
2. **Union** membership members into the staff resolution used by booking: the composer catalog,
   `AvailabilityService`, and `GetQualifiedStaff` return sub-providers **and** membership members
   (name via the person-directory seam). Slot generation/booking already accept any Guid staffId.
- **Pros:** members become bookable now; coexists cleanly (both Guid kinds valid); no Vue break.
- **Cons:** two staff sources unioned in the resolver until Vue migrates; some duplication.
- **Size:** ~6 handlers + `AvailabilityService` + activation hooks + tests. Real, but contained.

### Option B — Re-key booking onto memberships (end-state, larger)
Replace sub-provider staff with membership members throughout booking; `Booking` references
`MembershipId`. **Blocked** until the Vue frontend is migrated, and requires data backfill of
in-flight bookings. This is the clean final state, done after Option A + Vue migration.

## Recommended sequencing

1. **Vue frontend → memberships** (unblocks retiring the sub-provider path; also unblocks 2b).
   This is arguably the true next step: it removes the coexistence constraint that makes 2c messy.
2. **Option A bridge** (2c) — make membership members bookable while both models still exist.
3. **Option B** — re-key booking to memberships and retire sub-providers (+ backfill in-flight
   bookings), once Vue no longer needs the old model.

Doing Option A *before* the Vue migration is possible but means building the union logic that
Option B later tears out. If the goal is least total work, **migrate Vue first**, then jump closer
to Option B. If the goal is "Flutter members bookable ASAP," do Option A now.

## ⚠️ Regression found: 2a and 2c are sequenced backwards

Tracing 2c revealed that **2a shipped a booking regression for newly-onboarded providers**:
- The Flutter booking composer sources staff from `getProviderStaff` → `GET /Providers/{id}/staff`
  → legacy **sub-provider** records. 2a made the "add staff" action invite-only and removed the
  path that created bookable sub-providers.
- A provider onboarded after 2a has a **membership** but **no sub-provider**, so the composer shows
  the "no staff" notice and cannot generate slots. (Providers with pre-existing sub-providers are
  unaffected.)
- The old synthetic-`UserId` sub-provider was **not** just sloppiness: a real-`UserId` owner
  sub-provider would give the owner two providers and break the one-provider-per-owner assumption
  in `GetProviderByOwnerId`/session. So the "bridge" (membership → real sub-provider) is a
  collision minefield, and the "synthetic sub-provider" route re-introduces the very anti-pattern
  the redesign removed (H2/H3).

**Conclusion:** making members bookable cleanly = migrate `AvailabilityService` (562 lines,
sub-provider-typed throughout: `GetQualifiedIndividualProvidersAsync`, staff-name from
`individualProvider.OwnerFirstName`, `IsStaffQualified(providerId)`) + `GetAvailableSlots` staff
validation + composer catalog + `createBooking` onto membership-keyed staff — a deliberate,
regression-tested change to business-critical code. It should have preceded 2a. Interim options to
un-break onboarding booking are in the decision below.

## Decision needed
- **Sequence:** Vue-migration-first (less rework, but Vue is a large effort) **vs.** Option-A
  bridge now (Flutter members bookable sooner, some throwaway union code).
- Either way this is a multi-handler change to shared booking infrastructure with regression risk
  to existing bookings + Vue — it warrants its own focused, well-tested change, not a tail-end slice.
