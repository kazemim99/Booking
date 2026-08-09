## 1. Identity hardening (UserManagement)
- [x] 1.1 Canonical E.164 stored on `User.PhoneNumber` by every creation path (WS1 §9.4) — phone-first accounts always carry it.
- [ ] 1.2 Add partial unique index on `users(phone_number) WHERE phone_number IS NOT NULL AND Status <> Deleted` (idempotent migration)
- [ ] 1.3 Backfill `User.PhoneNumber` from `user_profiles` where null; emit a duplicate-phone report (no destructive merge)
- [x] 1.4 Fix inverted `ExistsByEmailAsync` (+ corrected `IsEmailAvailableAsync` callers); add `ExistsByPhoneNumberAsync` (repo + interface + cache decorator) — compile-verified
- [x] 1.5 All account creation funnels through `IPersonProvisioningService` (WS1 §9.2/9.3); email/password paths canonicalize + guard uniqueness (§9.4).
- [x] 1.6 Status-gate OTP completion (reject Banned/Suspended/Inactive before issuing tokens; Deleted excluded by query filter) — both provider + customer handlers, compile-verified
- [x] 1.7 Customer/provider unified onto one Person per phone via `User.EnsureCanActAs` → `UserType.Both` + additive role (WS1 §9.1/9.3).
- [ ] 1.8 Remove dead `PhoneNumber` VO copy and legacy `SendPhoneVerificationCodeCommandHandler`; guard/implement `SetStatus`/`UpdateRoles`
- [~] 1.9 **Status-gate regression test DONE** — `CompleteProviderAuthenticationStatusGateTests` (Theory: Banned/Suspended/Inactive all rejected before any JWT/persist; NSubstitute+FluentAssertions; full UM.Application suite green = 14). Still owed: phone canonicalization/dual-level uniqueness + concurrent double-create + no-cross-type-duplicate (need DB-backed integration).

## 2. Membership domain (ServiceCatalog.Domain)
- [x] 2.1 Add `OrganizationMembership` aggregate (Guid Id like sibling aggregates, `PersonId`, `OrganizationId`, roles set, status, InvitedAt/JoinedAt/LeftAt) — built, Domain compiles
- [x] 2.2 Add `MembershipRole` enum (Owner, Manager, StaffProvider, Receptionist, Custom) and `MembershipStatus` (Invited, Active, Suspended, Terminated)
- [~] 2.3 Add owned `StaffProfile` (providesServices, bioOverride) — schedule/service-assignments + within-org-hours validation deferred to a later task
- [x] 2.4 Behaviors: InviteExisting, CreateOwner, Accept, AssignRole/RemoveRole, EnableStaffProfile/DisableStaffProfile, Suspend, Terminate(reason), Reinstate
- [~] 2.5 Invariants: aggregate-local (roles non-empty, status transitions, StaffProvider↔StaffProfile) done; org-wide (≤1 live per person/org, keep ≥1 Owner) to be enforced in the application layer (task 4)
- [x] 2.6 Domain events: MembershipInvited, MembershipAccepted, MembershipActivated, MembershipRoleChanged, StaffProfileEnabled, MembershipTerminated, MembershipReinstated
- [ ] 2.7 Deprecate `Provider.RegisterStaffMember` and `ParentProviderId`-as-membership; retire orphan `Staff*Event` records/handlers and no-op seeders
- [x] 2.8 Domain unit tests — 15 tests green covering scenarios S1/S2/S3/S5/S6 + role/StaffProfile/terminated invariants

## 3. Membership persistence & migration (ServiceCatalog.Infrastructure)
- [x] 3.1 EF Core configuration: roles as converted CSV string, StaffProfile as optional separate-table owned entity (`staff_profiles`), audit columns — Infrastructure compiles, model valid
- [x] 3.2 Migration `20260721212656_AddOrganizationMembership` — creates `organization_memberships` + `staff_profiles` + `ix_membership_org`/`ix_membership_person`/`ux_membership_person_org_active` (partial unique: `status <> 'Terminated' AND person_id IS NOT NULL`); inspected, snapshot compiles
- [x] 3.3 `IOrganizationMembershipRepository` + `OrganizationMembershipRepository` (combined read+write) registered in DI
- [ ] 3.4 Backfill: Organization → `{Owner}` membership for OwnerId; existing sub-Providers / Model-A staff records → StaffProfile (resolve PersonId by phone, else null/claimable)
- [ ] 3.5 Repository integration tests (uniqueness, backfill, queries)
- NOTE: the UserManagement phone partial-unique-index migration (task 1.2) is deliberately deferred — it auto-applies via `Database.Migrate()` at host startup and would fail if the dev DB already holds duplicate phones; it must follow the dedupe report. App-level guard (`ExistsByPhoneNumberAsync`) is already in place.

## 4. Application layer (ServiceCatalog.Application)
- [x] 4.0 Cross-context Person-directory seam: `IPersonDirectory`/`PersonInfo` (Application.Abstractions.Identity) + `PersonDirectoryReadService` (in-process read over `user_management.users`, matches canonical value OR national number, excludes Deleted; mirrors `ProviderClientsReadService`) + DI — compile-verified
- [x] 4.1 Rewire `SendInvitationCommand`: normalize phone to canonical, dup-invitation guard on canonical value, **self-invite/already-member guard** via person directory + `HasActiveMembershipAsync` + `OwnerId` check (replaces the old `// TODO`) — implements S7, foundation for S8; compile-verified
- [x] 4.2 Accept → membership — **S4 fully closed (backend)**: existing-user `AcceptInvitationAsMemberCommand` + `ProviderInvitation.AcceptByMember()` + `POST /memberships/invitations/{id}/accept`; **new-user** `RegisterAndAcceptInvitationCommand` (OTP-verify + create-**or-reuse-by-phone** account via person directory + active membership + orphan-account compensation) + `POST /memberships/invitations/{id}/register-and-accept` [AllowAnonymous]. Both compile-verified. Phone taken from the invitation (OTP proves ownership); never a duplicate person.
- [~] 4.3 (self-invite guard portion done in 4.1)
- [ ] 4.3 Rewire `ApproveJoinRequestCommand` to create a membership (requester = PersonId)
- [ ] 4.4 `AddStaffToProvider` becomes a shim over the invitation/membership path (no synthetic UserId)
- [x] 4.5 Onboarding: `SetOwnerProvidesServicesCommand` + handler writing owner membership roles + StaffProfile; exposed as `POST /api/v1/registration/owner-provides-services` (ServiceCatalog.Api builds clean) — implements S1/S2 end-to-end
- [~] 4.6 Queries: `GetMyMemberships` DONE; **`TerminateMembershipCommand` DONE** (S5, ≥1-owner); **`GetOrganizationMembershipsQuery` DONE** — staff list from memberships (roles/status/provides-services) enriched with names via extended `IPersonDirectory.FindByIdsAsync` (batch `id = ANY(@ids)`); `GET /providers/{id}/hierarchy/members` (supersedes legacy `staff`). Compile-verified. **`ChangeMembershipRolesCommand` DONE** — domain `OrganizationMembership.ChangeRoles` (replaces set, syncs StaffProfile, non-empty invariant; +2 domain tests) + owner-auth + demote-last-owner guard + `PATCH /memberships/{id}/roles`; compile-verified, 17 membership domain tests green. Still owed: `GetPendingInvitations`.
- [ ] 4.7 FluentValidation for all new/changed commands
- [~] 4.8 **App-layer handler unit tests DONE** (NSubstitute/FluentAssertions): SendInvitation (fixed for new ctor + self-invite guard, already-member reject, reuse-by-phone allow) and TerminateMembership (owner-removes-staff, self-leave, **≥1-owner reject**, stranger-unauthorized, not-found). Full ServiceCatalog.Application suite green (62). Still owed: DB-backed **integration** tests (concurrent double-accept must not create two persons/memberships; register-and-accept reuse-by-phone at the DB boundary) via WebApplicationFactory + Reqnroll.
- NOTE: rewiring `SendInvitation`/`AcceptInvitation` to reuse a Person by phone (4.1/4.2) needs a cross-context Person-directory abstraction (ServiceCatalog → UserManagement in-process by-phone lookup, mirroring the existing `IProviderInfoService` seam in reverse); this is the next substantial backend piece.

## 5. API layer (ServiceCatalog.Api + UserManagement.API)
- [~] 5.1 `MembershipsController` created — `POST /memberships/invitations/{id}/accept` + `GET /memberships/me` live. Remaining: list-org-staff / register-and-accept / change-roles / terminate / cancel-invitation.
- [x] 5.2 `GET /api/v1/memberships/me` + `POST /api/v1/registration/owner-provides-services` — both live, compile-verified
- [ ] 5.3 Deprecate `/Providers/{id}/staff` and `/providers/{id}/hierarchy/*` as thin shims; replace in-process HTTP `GET /Providers/by-owner/{id}` with a direct query
- [ ] 5.4 JWT enrichment carries `memberships[]` + `activeMembershipId`
- [ ] 5.5 API integration tests (authorization, self-invite guard, dual-level phone uniqueness at the boundary)

## 6. Flutter provider app (booksy-provider-app)
- [x] 6.1 Onboarding "Do you personally provide services?" branch — `ownerProvidesServices` field (data→api service→repo→cubit setter→preview-step toggle) submitted best-effort at completion via `POST /registration/owner-provides-services`; strings added; **`flutter analyze` clean, 29 onboarding cubit tests green (4 new for S1/S2)**. (Implemented as a preview-step toggle rather than a new wizard step to avoid desyncing the server-driven `resumeStep`.)
- [~] 6.2 Phone-first invite UI — `InviteStaffSheet` (phone gated on `^09\d{9}$` + optional name) + `staff-invite` action on `StaffView`, wired through `StaffCubit.inviteStaff` → repo → `POST /v1/providers/{id}/hierarchy/invitations`; strings added; `flutter analyze` clean, invite-flow widget test green (42 more_test pass). URL version resolved: `'v'VVV`+SubstituteApiVersionInUrl ⇒ `v1`, Dio base already has `/api`. Remaining: pending-invitation list with resend/cancel (kept the record-only `StaffFormSheet` alongside for now — no backfill yet).
- [x] 6.3 Accept-invitation screen (existing user) — anonymous backend `GET /memberships/invitations/{id}` summary (masked phone) + Flutter `features/invitations/` (model, api, repo w/ 401→AuthFailure, `AcceptInvitationCubit`, page at `/invite/:invitationId`, DI factoryParam, route via login return-to-intent, strings). Load → Accept → reuses account; 401 → login CTA; domain refusal surfaced. `flutter analyze` clean, 5 cubit tests green.
- [ ] 6.4 Register-and-accept flow (new user: name → OTP → accept) — backend endpoint DONE; app UI still owed (needs a send-OTP-for-invitation endpoint + register/OTP screen)
- [ ] 6.5 Complete-staff-profile screen for missing fields post-accept
- [ ] 6.6 Staff list sources memberships; shows roles + status; owner marked
- [~] 6.7 Membership consumption in-app: `ProviderMembership` model + `getMyMemberships` api (unwraps `data.memberships`) + `fetchMyMemberships` repo (person-scoped, no `_withProviderId`) + `MembershipsCubit` + `MyMembershipsPage`/`View` at `/more/memberships` (More → «سالن‌های من») + DI + route + strings. `flutter analyze` clean; 45 more_test pass (3 new: cubit ready/failed, list-with-badges, empty). Consumes `GET /memberships/me`, makes S6 visible. Remaining: fold into `ProviderSession.memberships[]` + `activeMembershipId` with an active-salon switcher that re-scopes Home/Calendar/Clients (deferred — touches the central session that 300+ tests depend on).
- [ ] 6.8 Widget/bloc tests: onboarding branch cubit, invite/accept/complete flows, staff list states, session memberships

## 18. WS4″ — Synthetic-UserId path RETIRED (DONE) — decision (a) placeholder member
- [x] 18.1 **Decision taken (a):** a salon can add a bookable staff member who has no app account. Modelled as an **unclaimed membership** (`PersonId = null`) carrying a `StaffProfile.DisplayName` — never a fake Person and never a shadow provider.
- [x] 18.2 Domain: `OrganizationMembership.CreateUnclaimed(orgId, displayName)`, `ClaimBy(personId)` (attaches a real account later, **preserving the membership id** so existing bookings and history survive), `IsUnclaimed`; `StaffProfile.DisplayName`. Migration `AddStaffProfileDisplayName` (single additive column).
- [x] 18.3 **`AddStaffToProviderCommandHandler` rewritten** — the last `UserId.CreateNew()` is gone. Phone given and it resolves to a known person → membership linked to them (rejecting an existing member); otherwise an unclaimed membership. Both are made bookable via `IMemberBookabilityService` and audited as `MemberAdded`.
- [x] 18.4 Display-name fallback wired through `GetOrganizationMemberships`, the legacy `GetProviderStaff` read, and the booking resolver, so an unclaimed member shows their name in the roster, the composer, and on customer-facing slots.
- [x] 18.5 Tests: 5 domain tests (unclaimed is active+bookable, name required, claiming preserves the membership, double-claim rejected, terminated cannot be claimed). **Backend 440 green / Flutter 387 green.**
- **Net effect:** every bookable person in the system is now a membership. No synthetic identities remain anywhere in the write paths.

## 17. WS6a — Salon switcher (DONE)
- [x] 17.1 `AuthRepository.switchActiveOrganization(providerId)` + impl: persists the chosen organization via `saveProviderState`, drops the cached session, then re-derives status from the server. The choice survives a restart.
- [x] 17.2 `MembershipsCubit.switchTo(organizationId)`; My-salons rows are tappable and route to the dashboard so every provider-scoped screen reloads against the new salon.
- [x] 17.3 **Product/UX decision taken**: only **Owner** memberships are switchable. The management screens are owner-authorized server-side, so switching into a staff-only membership would 403. Staff-only memberships remain listed (visible, non-switchable) until a dedicated staff workspace exists — honest rather than a broken entry point.
- [x] 17.4 Re-scoping via the session's `providerId` reuses the app's existing single-provider scoping — no per-screen changes, no parallel "active membership" concept to keep in sync.
- [x] 17.5 Verified: `flutter analyze` clean, **Flutter 387 green**, backend unchanged (406 across the three membership-touching suites).
- [ ] 17.6 Staff workspace (role-scoped Home/Calendar for a non-owner member) — deferred; needs its own UX definition.

## 16. WS7 — Keystone E2E extended to the membership chain (AUTHORED, gate-run)
- [x] 16.1 `tests/e2e/keystone-booking-flow.sh` extended with steps 6–12: invite by phone → **self-invite refused (S7)** → invitee signs up and accepts with their OWN account → **asserts personId matches (no duplicate person, S4/S8)** → member appears on the roster → **member has bookable slots** (proves bookability provisioning) → **a customer books that specific member** → **last-owner termination refused**.
- [x] 16.2 Script syntax-verified (`bash -n`). NOT executed here: it needs a running host with sandbox OTP + a migrated DB, which is exactly what the CI `e2e-keystone` deploy gate provides. The legacy sub-provider path in steps 1–5 is retained so the run proves both models still work during the transition.
- [ ] 16.3 Reqnroll integration coverage (concurrent double-accept, reuse-by-phone at the DB boundary) — still owed; needs the two-schema test DB.

## 15. WS2c — Validation & security hardening (DONE)
- [x] 15.1 `MembershipCommandValidators` — FluentValidation for Revoke / Terminate / ChangeRoles / AcceptInvitationAsMember / RegisterAndAccept. Limits mirror the EF column widths so a request is never rejected by the DB instead of the API; role names validated against the enum; OTP must be 4–8 digits; email optional but well-formed (accounts are phone-first). Auto-discovered via `AddValidatorsFromAssemblies` + the MediatR `ValidationBehavior`.
- [x] 15.2 Validation deliberately does NOT duplicate domain invariants (≥1 owner, self-invite, status transitions) — those stay in the aggregate/handler where they can see the whole organization.
- [x] 15.3 Remaining audit hooks wired: **OwnerCreated** (onboarding), **StaffProfileEnabled/Disabled** (provides-services toggle), and **Accepted** on the register-and-accept path — every membership lifecycle event is now attributable.
- [x] 15.4 `MembershipCommandValidatorsTests` (9). **Backend 435 green / Flutter 387 green.**
- [x] 4.7 (superseded by 15.1 — FluentValidation for the membership commands is complete.)

## 14. WS2b — Invitation lifecycle completed (DONE)
- [x] 14.1 Audit trail extended to the **invitation stage**: `MembershipId` is now nullable and `InvitationId` added, so one trail covers invited → accepted → terminated even when no membership ever resulted. `RecordInvitation(...)` factory. Migration regenerated as `20260727141509_AddMembershipAuditTrail` (nullable membership_id + invitation_id).
- [x] 14.2 `RevokeInvitationCommand` + handler + `POST /memberships/invitations/{id}/revoke` — owner-only, pending-only, closes (never deletes) the invitation and writes an `InvitationRevoked` audit entry with actor + reason.
- [x] 14.3 **Expiry fixed**: `GetPendingInvitations` filtered by `invitation.IsValid()` — a lapsed invitation's status stays `Pending` until touched, so it previously lingered in the owner's list forever.
- [x] 14.4 `RevokeInvitationCommandHandlerTests` (4): owner revokes + audited (InvitationId set, MembershipId null), non-owner rejected, already-accepted rejected, not-found. **Backend 426 green.**
- NOTE: EF `migrations remove` needs a real Npgsql connection string in the temp `appsettings.json` (an empty `{}` falls back to a SQL-Server default and fails on `trusted_connection`).

## 13. WS2a — Membership audit trail (DONE)
- [x] 13.1 `MembershipAuditEntry` aggregate (append-only): membershipId, organizationId, subject person, **action**, **actor**, roles snapshot, status-after, reason, occurredAt. No update/delete — history is evidence, not state.
- [x] 13.2 `MembershipAuditAction` enum (Invited/Accepted/OwnerCreated/RolesChanged/StaffProfileEnabled|Disabled/Suspended/Terminated/Reinstated/InvitationRevoked/InvitationExpired), persisted **by name** so values can be appended safely.
- [x] 13.3 `IMembershipAuditRepository` + EF config (`membership_audit_entries`, indexes on membership and (org, occurredAt)) + DbSet + DI + migration `20260727140508_AddMembershipAuditTrail` (inspected: additive, single table + 2 indexes).
- [x] 13.4 Audit writes hooked into Terminate / ChangeRoles / AcceptInvitation — appended in the **same UnitOfWork** as the change, so the trail can never diverge from what happened.
- [x] 13.5 Test: termination is recorded with actor + reason + status-after. Backend **422 green**.
- [ ] 13.6 Remaining hooks: OwnerCreated (onboarding), StaffProfileEnabled/Disabled, Suspended/Reinstated, invitation Revoked/Expired (lands with WS2b).

## 11. WS4′ — One roster platform-wide (legacy read backed by memberships) (DONE)
- [x] 11.1 `GetProviderStaffQueryHandler` (legacy `GET /Providers/{id}/staff`) now reads the **membership roster** (name/phone via `IPersonDirectory`, owner flagged, ids = MembershipId) and appends any not-yet-migrated legacy sub-providers. Response DTO shape unchanged, so the Vue admin keeps working while seeing the same people the Flutter app sees.
- [x] 11.2 Flutter `hasStaff` completeness signal switched from `getProviderStaff` to `getOrganizationMembers` — the Home checklist, Team screen and composer now agree.
- [x] 11.3 Verified: backend **421 green**, Flutter **387 green**, analyze clean.
- [ ] 11.4 REMAINING (needs a product decision): `POST /Providers/{id}/staff` still runs `AddStaffToProviderCommandHandler` (synthetic `UserId.CreateNew()` sub-provider). Converging it needs a decision on **staff without an app account** (see §12).

## 12. OPEN PRODUCT DECISION — staff who do not use the app
Vue's "add staff" creates a bookable person from a name alone (no phone/account). The membership model requires a Person. Options:
  (a) **Placeholder member** — `OrganizationMembership` with `PersonId = null` + a display name held on `StaffProfile`; bookable immediately, claimable later when they accept an invitation on that phone. Supports real salons where juniors don't use the app.
  (b) **Invite-only** — adding staff always requires a phone and issues an invitation; no unclaimed members exist. Cleanest identity story, but changes Vue's UX and blocks account-less staff.
Recommendation: **(a)** — it preserves the "one roster" model, keeps every bookable person a membership, and avoids the synthetic-provider anti-pattern. Needs the display-name field on StaffProfile.

## 10. WS5a — Members are bookable (DONE) — booking core is now membership-native
- [x] 10.1 `IMemberBookabilityService`/`MemberBookabilityService`: on activation, qualifies the member for the org's services (`AddQualifiedStaff(membershipId)`, activates Draft services) and generates availability from org hours. Slots are **org-owned with `StaffId = MembershipId`** — no shadow provider record. Idempotent (skips days already generated). DI-registered.
- [x] 10.2 Hooked into all three activation points: `SetOwnerProvidesServices`, `AcceptInvitationAsMember`, `RegisterAndAcceptInvitation` — a member is bookable the moment they join, inside the same transaction.
- [x] 10.3 **`AvailabilityService` refactored to bookable RESOURCES** (`BookableResource(Id,Name)`) instead of `Provider` records: members (name via `IPersonDirectory`) → legacy sub-providers (until migrated) → solo org-direct fallback. `IAvailabilityService.GetAvailableTimeSlotsAsync` now takes `Guid? staffId` (a resource id), removing the sub-provider assumption from the engine.
- [x] 10.4 `GetAvailableSlotsQueryHandler` simplified — passes the resource id straight through (no provider load / hierarchy validation).
- [x] 10.5 `CreateBookingCommandHandler` resolves membership → legacy sub-provider → org-direct; validates the member belongs to the org, is Active and provides services; books against `StaffId = resourceId`; marks the correct availability rows (org+staffId for members).
- [x] 10.6 Flutter composer roster now reads `/hierarchy/members` (same source as the Team screen) filtered to Active + providesServices, keyed by MembershipId — **closes the Team-vs-Composer incoherence**; solo business still falls back to "خودم".
- [x] 10.7 Verified: backend **416 green / 0 failed**; Flutter **387 green**, `flutter analyze` clean.
- [x] 10.8 `MemberBookabilityServiceTests` (5): non-service-providing member is not bookable; invited-but-not-accepted is not bookable; active member's slots are **org-owned with StaffId=MembershipId**; repeat sync generates nothing (idempotent); a business with no open hours generates nothing. **Backend now 421 green.**

## 9. WS1 — Identity foundation: ONE PERSON PER PHONE (DONE)
- [x] 9.1 `User.EnsureCanActAs(UserType)` + `CanActAs(UserType)` — a person appearing on the other side of the marketplace gains a capacity (Type→`Both` + role) instead of getting a second account. Idempotent; Admin/Support never inferred.
- [x] 9.2 `IPersonProvisioningService` (Domain/Services) + `PersonProvisioningService` (Infrastructure) — **the single guarded path** phone→Person: canonicalize → lookup → reuse+grant, else uniqueness-guard then create. DI-registered.
- [x] 9.3 Both OTP handlers (customer + provider) rewired onto it; their ad-hoc upserts and private `CreateNew*User` factories **deleted**. Cross-type sign-in no longer throws "registered as X" — it promotes to `Both`. Customer aggregate is provisioned when the customer capacity is newly granted.
- [x] 9.4 Email/password registration (`RegisterUser`, `RegisterCustomer`) now store the **canonical phone on the User** (not only the profile) and reject an already-registered phone — closing the audit hole where such accounts were invisible to phone lookup and a later OTP sign-in minted a duplicate person.
- [x] 9.5 Tests: `OnePersonPerPhoneTests` (7) — cross-capacity promotion both directions, idempotency, `Both` covers all, elevated capacities never inferred, canonical account phone. Status-gate test updated for the new collaborator. **UM suite 21 green; backend total 416 green / 0 failed.**

## 8. Phase 2 (post-validation)
- [x] 8.1 C1 fixed server-side: owner `{Owner}` membership created at registration-complete (`SaveStep9CompleteCommandHandler`, idempotent); client toggle refines to `+StaffProvider`.
- [x] 8.2 Flutter staff list on memberships (`/hierarchy/members`): invite + terminate + owner badge + pending marker; legacy `StaffFormSheet` removed; 43 more_test green.
- [~] 8.3 Retire legacy staff path — Flutter moved off it; **backend `AddStaffToProvider` synthetic-UserId + `/Providers/{id}/staff` BLOCKED** (Vue frontend still uses them; needs Vue migration first).
- [x] 8.4 Staged data migration for review (not auto-applied): `backfill_memberships.sql` (owner + real-person sub-provider staff → memberships + staff_profiles) + `phone_uniqueness.sql` (S8 dedupe report → partial-unique index).
- [x] 8.5 **Step 1 — solo / org-direct booking (fixes the 2a regression; DONE, verified).** An active Organization with no staff that accepts direct bookings is bookable AS THE BUSINESS: `AvailabilityService.GetQualifiedIndividualProvidersAsync` returns the org itself as the bookable entity; `GetAvailableSlots` + `CreateBooking` accept `StaffId == ProviderId` as org-direct (skip the sub-provider ParentProviderId check); the Flutter composer offers the business (`ComposerStaff(id=providerId)`) when the staff list is empty. **Every provider type can book again** (solo + multi-staff book against the business). Api 0 errors; SC 62+304 backend tests green; 39 home_repository_impl tests green (+2 new); flutter analyze clean. This also resolves the 8.5.4 solo-vs-member overlap: solo → org-direct; specific staff → staff selection.
- [ ] 8.5(Step 2) **ENHANCEMENT — staff-SPECIFIC member booking** (own focused change; not a correctness fix now that Step 1 makes all providers bookable). Design: `PHASE2_BOOKING_CONVERGENCE.md`.
  - [ ] 8.5.1 `MemberBookabilityService`: on StaffProfile activation, qualify member for org services + generate `ProviderAvailability` keyed by `StaffId=membershipId` from org hours. Hook into SetOwnerProvidesServices / Accept / RegisterAndAccept.
  - [ ] 8.5.2 Union membership members into booking staff resolvers (`AvailabilityService`, composer catalog) alongside sub-providers; members appear as selectable staff (staffId = MembershipId).
  - [ ] 8.5.3 `GetAvailableSlots` + `CreateBooking`: accept a `staffId` that is a MembershipId (validate via membership, not sub-provider).
  - [ ] 8.5.5 Regression tests: existing sub-provider + org-direct bookings unaffected; new member bookings work.
- [ ] 8.6 Active-salon switcher (1b): `ProviderSession.memberships[]` + `activeMembershipId` + re-scope app; **needs staff-member UX scope decision**.
- [ ] 8.7 Vue frontend → memberships (unblocks 8.3); then re-key booking to memberships + retire sub-providers (Option B end-state).
- [ ] 8.8 In-process cross-context calls (replace `InvitationRegistrationService` self-HTTP); integration-test harness (two-schema DB): concurrent double-accept, reuse-by-phone.

## 7. Verification
- [x] 7.1 Backend unit tests **409 passed / 0 failed** (SC domain 304, SC app 62, UM app 14, Core 21, Infra 7, Arch 1); all ServiceCatalog projects build 0 errors. (DB-backed integration tests still owed — 8.8.)
- [x] 7.2 Flutter: `flutter analyze` clean; `flutter test` **387 passed / 0 failed** — includes fixing the 10 long-standing composer failures (stale `serviceIds` mocks/verifies + a lazy-list viewport issue in the book-again prefill test). **Whole suite green.**
- [ ] 7.3 Extend keystone E2E to cover owner-provides-services, invite-and-accept, switch-salon (no account duplication); deploy gate green
- [ ] 7.4 Update `IDENTITY_AND_STAFF_ARCHITECTURE.md` status and mark `add-provider-hierarchy` superseded
