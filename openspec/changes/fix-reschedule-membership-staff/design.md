# Design: fix-reschedule-membership-staff

## Context

`CreateBookingCommandHandler` and `RescheduleBookingCommandHandler` both need to answer the same question — *"what resource is this booking held against, and how are its availability slots keyed?"* — and today only creation knows the answer. Reschedule still encodes the pre-`refactor-identity-and-membership` assumption that staff are individual sub-`Provider` aggregates.

The duplication is the actual defect. Fixing reschedule by copy-pasting creation's resolution logic would leave the same drift hazard in place for the next handler (cancel, provider-side composer, block-time). So the fix extracts the decision once.

## Goals / Non-Goals

**Goals**
- Reschedule succeeds for membership, organization-direct, and legacy sub-provider bookings.
- Resource resolution and slot keying exist in exactly one place.
- No API contract change; no DB migration.

**Non-Goals**
- Fail-closed behavior when no availability rows cover a time, and 409-on-contention mapping — those are `booking-slot-integrity`.
- Migrating legacy sub-provider staff onto memberships — that is `refactor-identity-and-membership`.
- Any change to what rescheduling *means* (windows, policy, refunds).

## Decisions

### Decision 1: Extract a `BookableResource` resolver

Introduce a single application-layer resolver that both handlers call:

```
BookableResource {
    Guid          ResourceId       // what Booking.StaffId holds
    ResourceKind  Kind             // Member | Organization | LegacySubProvider
    ProviderId    SlotOwnerId      // provider whose availability rows carry the slot
    Guid?         SlotStaffKey     // discriminator within those rows (null unless Member)
}
```

`SlotOwnerId`/`SlotStaffKey` exist so slot keying stops being an inline ternary. Creation currently expresses it as:

```csharp
membership is not null ? provider.Id : ProviderId.From(resourceId),   // owner
membership is not null ? resourceId  : null                            // staff key
```

which is exactly the pair above, and exactly what reschedule gets wrong today.

**Alternative rejected — a `null` check in reschedule.** Smallest diff, but leaves creation and reschedule with independent copies of a three-way rule that has already drifted once, and does not fix the slot-keying half of the bug.

### Decision 2: Reschedule validates availability the way creation does

Creation checks `ValidateBookingConstraintsAsync` + `GetConflictingBookingsAsync(resourceId, …)`. Reschedule currently calls `IsTimeSlotAvailableAsync(provider, service, individualProvider, …)`, whose signature *requires* a staff `Provider` — unrepresentable for a member.

Reschedule moves to the conflict-query form, keyed by `ResourceId`. This makes the two paths agree on what "available" means, which they currently do not.

**Excluding the booking being moved:** the conflict query must ignore the booking under reschedule, otherwise moving a booking *within* its own buffer window conflicts with itself. Creation has no such case, so this is reschedule-specific and gets its own scenario.

### Decision 3: Keep release-then-occupy ordering, inside the existing transaction

Reschedule already runs in `TransactionBehavior`, and already releases the old slot before occupying the new one. That ordering is retained: releasing first means a reschedule *within the same slot window* cannot deadlock against itself.

The rollback path matters — a failure between release and occupy must not leave the old slot free and the new one unoccupied. The existing transaction covers this; the test suite pins it with an explicit failure scenario rather than assuming it.

## Test-infrastructure constraint (discovered while applying §1)

Reqnroll code generation is **switched off** in `tests/Booksy.ServiceCatalog.IntegrationTests` by its own `Directory.Build.props`/`.targets`, which no-op the generation targets — a documented workaround for a .NET 9 MSBuild task-host issue ([reqnroll#2043](https://github.com/reqnroll/Reqnroll/issues/2043)). The `.feature.cs` code-behind files are therefore **committed artifacts**, not build output.

Consequences for anyone adding scenarios here:

- A new `.feature` file does nothing on its own — without its `.feature.cs`, the scenarios are invisible to the runner (they do not fail; they simply never run).
- The code-behind must be generated deliberately and committed alongside the feature.

Generation does work on the SDK in use here (10.0.103) once the two override files are temporarily moved aside — that is how this change's `RescheduleBooking.feature.cs` was produced. Two cautions learned the hard way:

- A full build with generation enabled **rewrites every** `.feature.cs` in the project (~30 files), because the current generator version differs from whichever produced the committed ones. The diff renames generated test methods (e.g. `ProviderCreatedWalkInIsBornConfirmed` → `Provider_CreatedWalk_InIsBornConfirmed`), which changes test identities. Regenerate deliberately, then revert everything except the file you meant to add.
- Restore both override files afterwards. Leaving generation on is arguably the better end state, but retiring the workaround is a shared build-infrastructure change affecting anyone still on the .NET 9 SDK, and belongs in its own change rather than riding along here.

## Risks / Trade-offs

- **Touches a booking-lifecycle path.** Mitigated by writing the Reqnroll scenarios for all three resource kinds *before* changing handler behavior, so the legacy path is pinned green before it is refactored.
- **Refactoring creation while fixing reschedule** risks regressing the working path. Mitigated by extracting the resolver as a pure move (no behavior change) in its own step, with creation's existing tests green before reschedule is touched.
- **Merge overlap with `booking-slot-integrity`** on the same handler. Sequencing it first keeps that change's diff small, since it can then assume a working reschedule path.

## Migration Plan

None — no schema, contract, or data change. Ships as a normal deploy; behavior change is 404 → 200 on a path that is currently entirely broken, so there is no rollback compatibility concern.

## Open Questions

- Should `NewStaffId` accept the organization's own id (moving a member booking to "anyone at the salon")? Creation allows organization-direct booking, so the resolver supports it; whether the *reschedule* UI should offer it is a product call and is left out of scope until asked.
