## Context

Authorization today is a mix of `[Authorize]` (authenticated) and `[Authorize(Policy="ProviderOrAdmin")]` (role) attributes, plus ad-hoc in-handler/controller ownership checks that exist on some read paths (`GetBooking`) but are missing on state-changing paths (`cancel`, `reschedule`, `refund`). There is no cross-cutting ownership enforcement and no global fallback policy. `ICurrentUser.GetUserId()` is available in the Application layer.

## Goals / Non-Goals

**Goals:** enforce resource ownership on every state-changing customer/provider command in one auditable place; derive the actor from the JWT only; make public endpoints explicit; instant rollback via flag.

**Non-Goals:** redesigning the permission/role system; changing read-path behavior that already checks ownership; provider-app authorization.

## Decisions

- **D1 — MediatR `AuthorizationBehavior` over per-handler checks.** A pipeline behavior runs before the handler for any command implementing a resource-ownership marker interface, resolves the target resource, and compares its owner to `ICurrentUser.GetUserId()`. *Rationale:* single enforcement point, impossible to forget on new commands, testable in isolation. *Alternative rejected:* per-handler `if` checks — the exact pattern that was inconsistently applied and caused the bug.
- **D2 — Marker interfaces + a resolver.** Commands implement e.g. `IRequireBookingOwnership { Guid BookingId; Guid ActingUserId; }`. A registered `IResourceOwnershipResolver<T>` loads the owner id for the resource type. The behavior throws `ForbiddenException` (mapped to 403) on mismatch, with a role bypass for provider/admin where the capability allows it (e.g., provider cancelling a booking on their own calendar).
- **D3 — Actor from JWT, injected in the controller.** Controllers set `ActingUserId = GetCurrentUserId()` when constructing the command; the property is never model-bound from the body. Body fields `CancelledBy`/`ByProvider` are removed from binding; provider-vs-customer initiation is derived from the resolved identity.
- **D4 — Global fallback policy.** `AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())`; audit Categories/Locations/Platform and annotate the genuinely public ones with `[AllowAnonymous]`.
- **D5 — Feature flag `Authorization:EnforceOwnership`** (default true). When false the behavior no-ops (logs would-be denials) for a safe production canary.

## Decisions discovered during implementation

- **D6 — Ownership is enforced among *authenticated* callers; system/background contexts pass through.** The build surfaced a system-initiated caller: `BookingCancelledRefundIntegrationEventHandler` raises a `RefundPaymentCommand` from a CAP integration event (no HTTP user). Rather than thread a fake identity, the `AuthorizationBehavior` treats "no authenticated principal" as a trusted system context and passes through — authentication itself is the controller's responsibility (`[Authorize]` today, the global fallback policy tomorrow). This keeps a single, clean rule: *the behavior decides ownership among logged-in users; the pipeline edge decides authentication.* The system refund passes `ActingUserId = Guid.Empty` as an explicit system-actor marker for audit. *Rationale:* avoids a synthetic system principal and keeps the behavior's contract crisp; safe because HTTP anonymous access cannot reach a marked handler once `[Authorize]`/fallback are in place.
- **D7 — Split delivery: ship ownership enforcement now, defer the global fallback policy.** Ownership enforcement (the confirmed P0-2 IDOR fix) is non-breaking — it only ever 403s a genuine non-owner. The global fallback policy (P1-6) IS potentially breaking (it would require auth on any endpoint lacking `[AllowAnonymous]`, e.g. public discovery), so it is deferred to a separate, explicitly-flagged step that includes the public-endpoint audit + `[AllowAnonymous]` annotations. *Rationale:* land the critical, safe fix immediately; treat the breaking change as its own reviewable unit.
- **D8 — `ByProvider` becomes server-derived (follow-up).** The spoofable client `ByProvider` flag (a latent cancellation-fee-bypass) will be derived server-side from whether `ActingUserId` matches the provider owner, and dropped from the command. Kept defaulted-false and controller-unset in the interim so it is inert. *Rationale:* the audit's "actor is server-derived" requirement; closes the fee-bypass vector with no behavior change today.

## Risks / Trade-offs

- [A legitimate cross-actor flow breaks] → Mitigation: role bypass in the resolver for provider/admin; positive-path integration tests for owner, provider, admin; canary via flag.
- [Resolver adds a DB read before the handler] → Mitigation: the handler already loads the resource; share via the tracked context / accept one extra lightweight lookup (owned columns only).
- [Fallback policy accidentally locks a needed public endpoint] → Mitigation: explicit endpoint audit before enabling; `[AllowAnonymous]` list reviewed.

## Migration Plan

Behavioral only. Deploy with the flag ON in dev/staging, canary in prod, then default ON. Rollback = set flag false.

## Open Questions

- Should provider/admin bypass be per-capability configurable, or a single global role check? (Lean: per-capability, since some actions are customer-only.)
