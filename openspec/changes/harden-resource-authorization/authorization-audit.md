# Endpoint Authorization Audit (C1)

Complete classification of every API endpoint, performed before enabling the **global fallback policy** (`FallbackPolicy = RequireAuthenticatedUser`, `PolicyAuthorizationExtensions.cs`). After this change, **any endpoint without explicit `[AllowAnonymous]` requires an authenticated user.** Public endpoints are explicitly annotated. Roles/policies: `ProviderOrAdmin`, `AdminOnly`, `SysAdminOnly`, `Admin,Finance`, per-action ownership (C1 `AuthorizationBehavior`).

Legend: 🟢 Public · 👤 Auth Customer · 🧑‍🔧 Auth Provider · 🏢 Org Owner/Staff · 🛡️ Admin · ⚙️ Internal/System

## 🟢 Public — explicit `[AllowAnonymous]`

| Endpoint | Controller | Note |
|---|---|---|
| `GET /availability/{slots,check,dates}` | Availability | already anonymous |
| `GET /providers/{id}/availability` | ProviderAvailability | already anonymous |
| `GET /categories`, `/categories/popular` | Categories | **added class-level `[AllowAnonymous]`** (was implicit) |
| `GET /locations/*` (provinces, cities, hierarchy, search, {id}, with-parent) | Locations | **added class-level `[AllowAnonymous]`** (was implicit) |
| `GET /platform/statistics` | Platform | **added `[AllowAnonymous]`** (doc-declared public) |
| `GET /services/{id}`, `/search`, `/provider/{id}`, `/popular`, `.../qualified-staff` | Services | already anonymous (browse) |
| `GET /reviews/*` (2 read endpoints) | Reviews | already anonymous |
| provider search/browse/detail (4) | Providers | already anonymous |
| `POST /payments/calculate-pricing` | Payments | already anonymous (pricing preview) |
| `GET /payments/callback` | Payments | already anonymous (ZarinPal redirect; validated by Authority) |
| invitation view + register-and-accept | Memberships, ProviderHierarchy | already anonymous (pre-registration invite flow) |
| `POST /auth/send-verification-code`, `/auth/customer|provider/complete-authentication` | Auth | already anonymous (OTP login) |
| `POST /authentication/{login,refresh,forgot-password}` | Authentication | already anonymous |
| `POST /customers/register`, `POST /users` | Customers, Users | already anonymous (registration) |
| `GET /health`, `/health/ready`, `/health/live` | (Program.cs) | **added `.AllowAnonymous()`** so probes aren't 401'd |
| `/swagger*` | (middleware) | exempt by middleware order (before `UseAuthorization`) |

## 👤 Authenticated Customer

Bookings: `POST /bookings` (create), `GET /bookings/{id}` (ownership-checked), `GET /bookings/my-bookings`, `GET /bookings/search`, `GET /bookings/available-slots`, `POST /bookings/{id}/notes`; **`POST /bookings/{id}/cancel` + `/reschedule` — now ownership-enforced (C1 behavior)**. Customers role: get/update/delete self, favorites, profile, upcoming/history bookings, preferences, recently-visited. NotificationPreferences (all). Payments: create/capture/get/history/customer. **`POST /payments/{id}/refund` — now ownership-enforced (C1)**. Users: activate/profile/change-password/phone-verify (self). Auth: `POST /authentication/logout`.

## 🧑‍🔧 Authenticated Provider (`ProviderOrAdmin`)

Bookings: confirm, complete, no-show, assign-staff, provider/{id}, statistics. Services: AddService, ActivateService, DeactivateService, ArchiveService, **UpdateService + DeleteService (SECURITY FIX — were unauthenticated; now `ProviderOrAdmin`)**. ProviderSettings (all). ProviderRegistration (class `[Authorize]`). Financial: provider earnings. Notifications: `bulk` (`Admin,Provider`).

## 🏢 Organization Owner / Staff

ProviderHierarchy: staff/members/invitations/join-requests management, convert-to-organization (class `[Authorize]`, ownership via provider context). Memberships: me, terminate, revoke, roles (class `[Authorize]`).

## 🛡️ Administrator / Finance

Providers: 2 `AdminOnly` actions (796, 823). Services: by-status `AdminOnly`. Users: search/deactivate `AdminOnly`, delete `SysAdminOnly`, by-status `AdminOnly`. Payments: `GET /payments/reconciliation` (`Admin,Finance`). Payouts: create/execute/pending (`Admin,Finance`).

## ⚙️ Internal / System (protected by fallback)

| Endpoint | Disposition |
|---|---|
| `POST /auth/generate-token` | **Was unauthenticated and mints a JWT for an arbitrary userId — a latent forgery hole.** No `[AllowAnonymous]`; the fallback now requires auth. Flagged for follow-up: confirm the sole caller and consider `AdminOnly`/removal. |
| `MapHub /hubs/notifications` (NotificationHub) | No `[Authorize]`; fallback now requires an authenticated user (correct for user-specific notifications). **Verify SignalR `access_token` negotiation still connects (E2E).** |
| System auto-refund (`BookingCancelledRefundIntegrationEventHandler`) | Not an HTTP endpoint; `AuthorizationBehavior` passes system context through (design D6). |

## Findings surfaced by the audit
1. **`Services.UpdateService` / `DeleteService` were unauthenticated** — any caller could modify/delete any provider's services. **Fixed** with `[Authorize(Policy="ProviderOrAdmin")]`.
2. **`Auth.generate-token` was unauthenticated** and issues a JWT for a supplied userId — an auth-bypass risk. Now protected by the fallback; flagged for caller review.
3. Health probes and the notification hub would have been caught by the fallback — health explicitly exempted; hub intentionally protected (verify SignalR auth).

## Validation
- Build: full `Booksy.Host` compiles green with the fallback enabled.
- Runtime: integration tests assert (a) each 🟢 endpoint returns non-401 anonymously, (b) representative protected endpoints return 401 anonymously, (c) the C1 IDOR case (A cancels B → 403). Run in the ServiceCatalog integration harness / CI.
