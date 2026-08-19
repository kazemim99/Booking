# resource-authorization Specification

## Purpose
TBD - created by archiving change harden-resource-authorization. Update Purpose after archive.
## Requirements
### Requirement: Ownership enforced on state-changing resource commands
Every command that mutates a customer-owned or provider-owned resource (booking cancel/reschedule/notes, payment refund, and equivalents) SHALL verify that the authenticated caller owns the target resource or holds an authorized role for it, before the command's business logic executes. If the caller is neither owner nor an authorized role, the request SHALL be denied with HTTP 403 and no state change and no side effects (no refund, no notification).

#### Scenario: Non-owner is denied
- **WHEN** an authenticated user who is not the booking's customer (and not its provider/admin) calls cancel, reschedule, or refund for that resource
- **THEN** the response is 403, the resource is unchanged, and no payment/gateway or notification side effect occurs

#### Scenario: Owner is allowed
- **WHEN** the resource's owning customer performs an allowed action on their own resource
- **THEN** the action proceeds normally

#### Scenario: Authorized role is allowed
- **WHEN** the provider that owns the calendar (or an admin) performs a role-permitted action on the resource
- **THEN** the action proceeds normally

### Requirement: Actor identity is server-derived
The acting user identity used for authorization and audit SHALL be taken from the authenticated principal (JWT), never from request-body fields. Body fields that previously carried an actor (e.g. `CancelledBy`, `ByProvider`) SHALL be ignored for authorization and audit.

#### Scenario: Spoofed actor field is ignored
- **WHEN** a request body sets an actor field to another user
- **THEN** authorization and audit use the JWT identity, not the body value

### Requirement: Public endpoints are explicit and everything else requires authentication
The application SHALL apply a global fallback policy requiring an authenticated user, so any endpoint without an explicit `[AllowAnonymous]` requires authentication. Genuinely public endpoints SHALL be explicitly annotated.

#### Scenario: Unannotated endpoint requires auth
- **WHEN** an unauthenticated request hits an endpoint that is neither `[Authorize]` nor `[AllowAnonymous]`
- **THEN** the response is 401

