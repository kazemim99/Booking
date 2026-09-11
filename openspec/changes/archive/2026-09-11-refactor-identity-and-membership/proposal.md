## Why

The platform cannot model real-world salons. Identity, business, and "staff" are collapsed into an overloaded `User` + `Provider` pair, with **no Person, Organization Membership, or Staff Profile as separate concepts**. Concretely:

- **Phone is not a unique identity.** `users.email` is unique but `users.phone_number` has **no index or unique constraint**; duplicate people on the same phone arise via email/password registration, soft-delete, and parallel customer/provider accounts. OTP login also never checks `Status`, so blocked users still receive tokens.
- **"Staff" is a second `Provider`.** The wired add-staff path mints a throwaway `UserId.CreateNew()` and copies the salon's own phone/email — an account-less duplicate. A person **cannot** belong to two salons (single scalar `ParentProviderId`), cannot be owner-and-staff, and has no leave/rejoin lifecycle. Invite-by-phone **rejects** existing accounts; self-invite is an unguarded `// TODO`.
- **Onboarding** never asks whether the owner provides services and never makes the owner a staff member, so every new salon starts with zero staff — a state the app itself flags as broken.

Full audit and the approved target design: [IDENTITY_AND_STAFF_ARCHITECTURE.md](../../../IDENTITY_AND_STAFF_ARCHITECTURE.md).

## What Changes

Introduce a **Person → OrganizationMembership → StaffProfile** model (approved option 3.3‑A: staff schedule/services live on a `StaffProfile` owned by a membership; bookings will reference `MembershipId`). This change delivers **Phase 1**:

- **Identity hardening** — canonical, globally-unique phone on `Person` (`User`), enforced at **both** DB (partial unique index) and application (guarded creation in every path); `Status`-gated OTP; fix inverted `ExistsByEmailAsync`; add `ExistsByPhoneNumberAsync`.
- **New `OrganizationMembership` aggregate** (+ owned `StaffProfile`, per-membership roles, `Invited/Active/Suspended/Terminated` lifecycle, join/leave history) with EF config, idempotent migration, repositories, and domain events.
- **Rewire invitation & join-request** to resolve a Person **by canonical phone** (reuse existing accounts, never duplicate), guard self-invite / already-member, and produce memberships instead of sub-Providers.
- **Onboarding "Do you personally provide services?" branch** — Yes ⇒ owner membership `{Owner, StaffProvider}` + StaffProfile (owner becomes first active staff, no invitation); No ⇒ `{Owner}`, zero staff, invite later.
- **Membership-centric APIs** (invite / accept / register-and-accept / list / change-roles / terminate / `GET /me/memberships`).
- **Flutter provider app** — onboarding branch, phone-first invite, accept-invitation (existing user) and register-and-accept (new user), complete-staff-profile, staff list showing roles/status, and `ProviderSession.memberships[]`.
- **BREAKING**: `POST/PUT/DELETE /Providers/{id}/staff` (Model A) and `/providers/{id}/hierarchy/*` invitation/staff endpoints (Model B) are deprecated behind membership endpoints (kept as thin shims through the transition).

**Supersedes** the identity/parent mechanism of `add-provider-hierarchy`: its `ProviderInvitation`/`ProviderJoinRequest` aggregates and per-staff booking attribution are retained and rewired; `ParentProviderId`-as-membership is retired.

**Deferred to later phases (not in this change):** switching bookings to `MembershipId` (dual-write only here), the multi-salon switcher polish, deduplication tooling for pre-existing duplicate accounts run at scale, and aligning the Vue `booksy-frontend` hierarchy UI.

## Impact

- **Affected specs:** `organization-membership` (new), `authentication`, `provider-registration`, `provider-staff-management`.
- **Superseded:** `add-provider-hierarchy` (identity/parent mechanism); `staff-management` (Vue) to be reconciled in a follow-up.
- **Affected code (backend):**
  - `src/UserManagement/...` — `User` aggregate, `UserConfiguration`, new migration, `UserRepository`, OTP completion handlers, `PhoneNumber` VO cleanup.
  - `src/BoundedContexts/ServiceCatalog/...Domain` — new `OrganizationMembershipAggregate` (+ `StaffProfile`, `MembershipRole`, `MembershipStatus`, events); deprecate `Provider.RegisterStaffMember`/`ParentProviderId`.
  - `...Application` — invitation/join/onboarding command handlers; new membership commands/queries + validators.
  - `...Infrastructure` — EF config, migration, repositories, DI; retire no-op `StaffSeeder`/`StaffDataSeeder`.
  - `...Api` — membership controller + DTOs; deprecate legacy staff endpoints.
- **Affected code (Flutter `booksy-provider-app`):** onboarding wizard + cubit, new invitation/accept/complete-profile screens, staff list, `ProviderSession`, auth models, DI, api services, and tests.
