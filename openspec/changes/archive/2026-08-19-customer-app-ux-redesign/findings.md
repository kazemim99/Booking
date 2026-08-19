# API-Parity Findings (task 6.1)

Verified against `API_ENDPOINTS.md` and the backend source
(`Booksy.ServiceCatalog.Api/Controllers/V1/*`).

## Confirmed — no gaps blocking the mobile booking flow

- **Availability slot granularity**: `GET /api/v1/Availability/slots?ProviderId&ServiceId&Date&StaffId` is day-granular and returns `slots[{startTime, endTime, durationMinutes, isAvailable, availableStaffId, availableStaffName}]` plus `validationMessages`. No client-side chunking needed (design.md open question resolved). `StaffId` is optional, which cleanly supports the «فرقی نمی‌کند» (any staff) choice — the app books using the slot's `availableStaffId`.
- **Provider detail**: `GET /api/v1/Providers/{id}?includeServices=true&includeStaff=true` (AllowAnonymous) returns profile + `services[]` (`basePrice`, `currency`, `duration`) + `staff[]` (`firstName`, `lastName`, `role`, `isActive`) in one call — exactly what the detail screen and booking flow need.
- **Create booking**: `POST /api/v1/Bookings` `{providerId, serviceId, staffProviderId, startTime}`. `staffProviderId` is a required Guid, hence the slot-assigned staff strategy above.
- **Reschedule**: `POST /api/v1/Bookings/{id}/reschedule` `{newStartTime, newStaffId?, reason?}` — matches the web `RescheduleBookingModal` semantics; the mobile slot picker reuses the same slots endpoint.
- **Cancel**: `POST /api/v1/Bookings/{id}/cancel` `{reason, cancelledBy}`.
- **Guest checkout** (design.md open question): booking creation requires auth; `POST /api/v1/Auth/customer/complete-authentication` performs verify-OTP + login/register in one step, so OTP-at-confirmation onboards a new customer without a separate registration call. Return-to-intent brings the user back to the confirmation step (BookingBloc is app-scoped, selections survive).

## Gaps (documented, not implemented here)

1. **Promotions endpoint does not exist.** `HomeRemoteDataSource.getPromotions()` is a stub returning `[]`; the home promotions section stays hidden until a backend endpoint ships.
2. **No staff-per-service filter.** `includeStaff` returns all provider staff, not staff qualified for a specific service. The mobile staff step therefore lists all active staff; slot availability (which is staff-aware) is the effective filter. A `GET /Providers/{id}/services/{serviceId}/staff` endpoint would let the step show only qualified staff.
3. **Slot-taken conflict status code.** The app treats `409`/`422` from create-booking as "slot no longer available" and returns the user to the slot step with refreshed availability. If the backend signals this differently (e.g. `400` with an error code), the mapping in `BookingRepositoryImpl.createBooking` needs adjusting during E2E verification (task 10.2).
4. **Gallery imagery**: provider detail exposes `profileImageUrl`/`logoUrl` only; the richer gallery endpoints (provider gallery admin work, see `GALLERY_BACKEND_REQUIREMENTS.md`) are not consumed by the mobile app yet — the detail header uses the single profile image.
5. **`build_runner` codegen is broken** in the app (retrofit_generator 8.2.1 incompatible with the current Dart SDK). All new code avoids codegen: manual JSON parsing and manual `get_it` registrations in `injection.dart`. Upgrading retrofit/retrofit_generator would restore codegen; tracked as tech debt, not part of this change.
