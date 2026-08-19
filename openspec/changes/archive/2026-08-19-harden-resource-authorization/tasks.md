# Tasks: harden-resource-authorization

> Read-model/ownership-only. Ownership enforcement ships now (non-breaking); the global fallback policy is deferred (breaking — see design D7). Flag: `Authorization:EnforceOwnership` (default true).

## 1. Authorization mechanism
- [x] 1.1 `IResourceOwnership` marker interfaces (`IRequireResourceOwnership`/`IRequireBookingOwnership`/`IRequirePaymentOwnership`) in `Booksy.Core.Application/Authorization`
- [x] 1.2 `IResourceOwnershipResolver<TCommand>` + `ResourceOwners`; generic ServiceCatalog resolvers (`BookingOwnershipResolver`, `PaymentOwnershipResolver`)
- [x] 1.3 `AuthorizationBehavior` — resolve owners, compare to `ICurrentUserService`, Admin bypass, `ForbiddenException`(403) else; flag-gated; **system/background contexts pass through** (design D6)
- [x] 1.4 Registered in the MediatR pipeline after Validation, before Transaction (denials never open a tx or trigger a refund)

## 2. Wire commands to JWT identity
- [x] 2.1 Server-set `Guid ActingUserId` + ownership markers on `CancelBookingCommand`, `RescheduleBookingCommand`, `RefundPaymentCommand`
- [x] 2.2 Controllers set `ActingUserId = Guid.Parse(GetCurrentUserId()!)`; system auto-refund passes `Guid.Empty`
- [~] 2.2b `ByProvider` → server-derived + dropped from the command (design D8) — **follow-up**; kept inert (defaulted false, controller-unset) meanwhile
- [ ] 2.3 Add `AddBookingNotesCommand` to the ownership set (same pattern); verify provider actions keep `ProviderOrAdmin`

## 3. Global fallback + public-endpoint audit — APPROVED & DONE (design D7)
- [x] 3.0 **Full endpoint authorization audit** — every endpoint classified into 6 categories; documented in `authorization-audit.md`
- [x] 3.1 `FallbackPolicy = RequireAuthenticatedUser` enabled in `PolicyAuthorizationExtensions`; health probes exempted with `.AllowAnonymous()` in `Program.cs` (Swagger exempt by middleware order)
- [x] 3.2 Public endpoints explicitly annotated: class-level `[AllowAnonymous]` on Categories + Locations; action-level on Platform.statistics
- [x] 3.2b **Security holes fixed:** `Services.UpdateService`/`DeleteService` were unauthenticated → `[Authorize(Policy="ProviderOrAdmin")]`; `Auth.generate-token` (anonymous JWT minting) now fallback-protected + flagged
- [x] 3.3 ZarinPal callback confirmed intentionally `[AllowAnonymous]` (validated by Authority); NotificationHub now fallback-protected (verify SignalR access_token in E2E)

## 4. Tests
- [x] 4.1 Unit: `AuthorizationBehavior` — owner/provider/admin allow, non-owner 403 (handler never runs), missing-resource fail-closed, system pass-through, flag-off canary (7 tests green)
- [x] 4.2 Integration (Testcontainers): **authorization boundary validated** — 5 public endpoints reachable anonymously (not 401/403), 4 protected endpoints → 401 anonymously (9 tests green). `AuthorizationBoundaryTests.cs`
- [ ] 4.2b Integration: HTTP-level IDOR (A creates booking, B cancels → 403, no refund) — behavior proven by the 7 unit tests; HTTP-level e2e is a further nicety
- [ ] 4.3 Integration: spoofed body actor ignored; audit uses JWT identity
- [ ] 4.4 Regression: full booking lifecycle for legitimate actors still green

## 5. Verify
- [x] 5.1 `Booksy.ServiceCatalog.Api` builds green (whole coupled change compiles); behavior unit tests green
- [x] 5.2 Boundary integration tests green (Testcontainers); no business-rule test changed for behavior reasons

## 6. MANDATORY acceptance before C1 is closed (product-owner requirement)
> Status corrected 2026-08-19 (`OPENSPEC-AUDIT-2026.md`): this section was left unchecked although the work
> shipped and is recorded in ADR-003's close-out. Two real defects were fixed here: the hub had **no
> `[Authorize]`** (it accepted anonymous connections), and JWT `OnMessageReceived` read `access_token` from a
> **header** — which a browser cannot set on a WebSocket handshake, so the standard SignalR `?access_token=`
> negotiation never authenticated a real client. Extracted to `SignalRAccessTokenExtractor` (query string,
> header fallback, hub paths only).
- [x] 6.1 **SignalR NotificationHub E2E** — authenticated connection succeeds under the global fallback via `access_token` (query-string) negotiation. Proven by `NotificationHubAuthTests.Authenticated_client_connects_and_can_reconnect` (real `HubConnection` over the test server) plus `SignalRAccessTokenExtractorTests` (4): reads the query string on a hub path, falls back to the header for non-browser clients, ignores the query on non-hub paths, returns null when absent.
- [x] 6.2 **Reconnect behavior** — covered by the same `Authenticated_client_connects_and_can_reconnect` test, which asserts the client re-establishes after a transient disconnect.
- [~] 6.3 **Expired-token handling — PARTIAL.** Rejection of an unauthenticated connection is proven (`NotificationHubAuthTests.Anonymous_connection_is_rejected`), and the expiry signal exists in production code (`JwtAuthenticationExtensions.cs:52-56` appends a `Token-Expired` response header from `OnAuthenticationFailed`). **Not tested:** a connection presenting a genuinely *expired* JWT, and the behaviour of an already-established connection whose token expires mid-flight. Carried into `harden-test-suite-and-dependencies` rather than blocking this change — the authorization mechanism itself is proven, and the untested paths are token-lifetime behaviour shared with every other authenticated endpoint.
