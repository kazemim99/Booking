## Context

Implements `provider-clients`. The blocker was cross-context identity: bookings carry only `customerId`. The monolith shares one Postgres database (schema-per-context), and the join `user_management.user_profiles.user_id = users.id` with names/phones was verified live. Reviews' `CustomerName` is currently a placeholder for the same reason — this change builds the seam properly.

## Goals / Non-Goals

**Goals:** a real client book with names; Persian-normalized search; book-again flowing into the composer's walk-in fields; an explicit, reusable name-resolution seam.
**Non-Goals:** client CRUD (clients are derived, not managed); client notes/preferences (needs storage); pagination (a provider's distinct-client count is small; revisit if proven otherwise); enriching booking rows/reviews with names (same seam, separate change).

## Decisions

- **D1 — Read-only cross-schema SQL, as an explicit Infrastructure seam.** `IProviderClientsReadService` (Application abstraction) with one Infrastructure implementation running a single aggregate query: bookings grouped by customer joined to `user_management` for identity. DDD purity would demand an event-fed read model; in a schema-per-context monolith a *read-only* SQL seam is the honest, cheap version of the same idea — no domain coupling, one clearly-marked file to replace if contexts ever split databases.
- **D2 — Exclude the provider's own user.** Walk-ins are booked under it (composer MVP convention); a "client" row aggregating all walk-ins under the provider's own name would be noise. Identified walk-ins arrive when first-class walk-in identity ships.
- **D3 — Endpoint on ProvidersController** (`/Providers/{id}/clients`), guarded by the existing `CanManageProvider` (with its first-session ownership fallback).
- **D4 — App-side search is local** over the loaded list using the existing `PersianText.normalize` (built for exactly this); no server search param until list sizes demand it.
- **D5 — Book-again = composer prefill via query params** (`client`, `phone`) — consistent with the `?date=` pattern, deep-linkable, and it feeds the walk-in notes convention so the created booking carries the client's identity.
- **D6 — SQL uses stored status strings** (`'Completed'`, `'Requested'`, `'Confirmed'`, verified in live rows) and `IsDeleted = false`.

## Risks / Trade-offs

- **[Schema drift]** the read service references `user_management` columns by name → single file, covered by live E2E; breaks loudly (500) not silently.
- **[No unit test on the SQL]** documented: no test double captures a cross-schema join; verified live instead. A Reqnroll integration scenario is the right future home.
- **[Unbounded list]** accepted for MVP; ORDER BY recency means the top of the list is always useful.

## Migration Plan

1. Backend query + read service + endpoint; rebuild; live-verify (client with counts appears; owner excluded; 403 for stranger).
2. Flutter cubit/page/nav/prefill + tests.
3. Full suites.

## Open Questions

- None blocking. Future: reuse the seam for booking-row/review names; client notes; pagination.
