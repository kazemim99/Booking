# Proposal: fix-reschedule-membership-staff

## Why

**Rescheduling is broken for every booking made against a membership-based staff member** — which, since `refactor-identity-and-membership`, is every normally-created booking. The API returns 404 and the customer's reschedule modal simply never closes.

`CreateBookingCommandHandler` was migrated to the Person→OrganizationMembership model and now resolves a *bookable resource* that may be any of three things, storing the resolved id on the booking (`CreateBookingCommandHandler.cs:121-156`, and its own comment at `:226` — "Member slots belong to the organization and carry StaffId=MembershipId; legacy sub-provider slots are keyed by the sub-provider itself"):

| Resource | `Booking.StaffId` holds | Availability slots keyed by |
|---|---|---|
| Organization member (current model) | `MembershipId` | organization id + `StaffId` |
| Organization itself (solo direct) | organization `ProviderId` | organization id |
| Legacy individual sub-provider | sub-provider `ProviderId` | sub-provider id |

`RescheduleBookingCommandHandler` was **not** migrated. It still assumes the legacy model exclusively:

```csharp
// RescheduleBookingCommandHandler.cs:81-86
var individualProvider = await _providerRepository.GetByIdAsync(
    ProviderId.From(newStaffId), cancellationToken);
if (individualProvider == null)
    throw new NotFoundException($"Individual provider with ID {newStaffId} not found");
```

A `MembershipId` is not a `ProviderId`, so this throws immediately → HTTP 404. Reproduced against the live stack:

```
[ERR] Transaction failed for RescheduleBookingCommand, rolling back
Booksy.Core.Application.Exceptions.NotFoundException:
  Individual provider with ID bf526b3d-… not found
POST /api/v1/Bookings/{id}/reschedule → 404
```

The failure is also load-bearing downstream: after the resource lookup the handler passes `individualProvider` to `IAvailabilityService.IsTimeSlotAvailableAsync(...)` and then releases/marks slots keyed only by `booking.ProviderId` — neither of which is correct for member-staff, whose slots hang off the organization and are discriminated by `StaffId`. So the bug is not a one-line null check; the whole resource-resolution and slot-keying path needs to match `CreateBooking`.

This is currently caught by `booksy-frontend/e2e/specs/booking-reschedule.spec.ts`, which is **deliberately left failing** (not skipped) by `add-playwright-e2e` so the defect stays visible.

### A second, independent defect (found while pinning the baseline in §1)

Resource resolution is not the only thing wrong. `Booking.Reschedule` hands the successor booking the *same owned-entity instances* as the original (`Booking.cs:337-338`):

```csharp
PaymentInfo = PaymentInfo, // Transfer payment info
Policy = Policy,
```

`BookingPolicy` is an EF **owned entity** whose key includes the identifying FK `BookingId`. Assigning the old booking's instance to a new booking asks EF to re-parent it, which it refuses:

```
The property 'Booking.Policy#BookingPolicy.BookingId' is part of a key and so cannot be
modified or marked as modified.
```

This fires on **every** resource kind, but only *after* resolution succeeds — so it was hidden behind the 404 for membership and organization-direct bookings, and only surfaced on the legacy sub-provider path once its scenario was made date-independent.

**Consequence: there is no working reschedule path at all**, and therefore no green baseline to guard the §2 refactor with. The proposal originally assumed legacy sub-provider worked; that assumption was wrong, and the plan below is adjusted accordingly.

## What Changes

- Extract the bookable-resource resolution in `CreateBookingCommandHandler` into a **single shared resolver** (membership | organization-direct | legacy sub-provider) so creation and reschedule cannot drift apart again.
- `RescheduleBookingCommandHandler` uses that resolver instead of assuming a sub-provider `Provider` row.
- Availability validation on the reschedule path matches creation: conflict-check by resolved **resource id**, rather than requiring a staff `Provider` aggregate.
- Slot release (old time) and slot marking (new time) are **keyed the same way creation keys them** — organization id + `StaffId` for members, resource id for legacy sub-providers.
- Reschedule preserves the resource: a reschedule that does not change staff SHALL keep the same `StaffId`, and an explicit `NewStaffId` SHALL be validated as bookable for that organization.

## Capabilities

### New Capabilities
- `booking-reschedule`: rescheduling an existing booking to a new time (and optionally a new staff member) SHALL work identically for every bookable-resource kind that booking creation accepts, moving the occupied slot atomically.

### Modified Capabilities
<!-- None. This restores intended behavior of an existing capability; it does not change
     what rescheduling is supposed to do, only makes it work under the membership model. -->

## Impact

- **Code**: `RescheduleBookingCommandHandler` (resource resolution, availability validation, slot release/mark); a shared resolver extracted from `CreateBookingCommandHandler`. No API contract change — same route, request, and response shapes.
- **Behavior**: `POST /api/v1/Bookings/{id}/reschedule` returns 200 instead of 404 for member-staff bookings. No change for legacy sub-provider bookings.
- **DB**: none — no schema or migration.
- **Tests**: Reqnroll acceptance scenarios for reschedule across all three resource kinds; `booking-reschedule.spec.ts` returns to green.
- **Sequencing**: overlaps `booking-slot-integrity`, which also edits `RescheduleBookingCommandHandler` (for fail-closed slots + 409 contention). Distinct defects; land this one first — `booking-slot-integrity` cannot meaningfully harden a reschedule path that 404s before reaching slot logic.
- **Risk**: touches a financial/booking-lifecycle path. Mitigated by covering all three resource kinds with integration tests before changing behavior, and by making slot keying a single shared decision rather than duplicated logic.
