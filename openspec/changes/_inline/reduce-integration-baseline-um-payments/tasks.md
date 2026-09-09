Status: ACTIVE
Verify: FULL

Split agreed with booking-21 on 2026-09-09: booking-21 owns every failure that flows through
`ServiceCatalogIntegrationTestBase` (availability, bookings, provider staff, provider management,
working hours, registration steps). This change owns the rest of `tests/known-failures.txt`.

## Acceptance scenarios
- S1 Every test removed from `tests/known-failures.txt` by this change passes for a root-cause reason (a defect fixed or a test corrected against the real contract), never by weakening an assertion
- S2 The known-failures list only shrinks; FULL verify stays green with no off-list failure

## Tasks
- [x] 1 UserRepositorySaveTests (3): tests built their context with a bare UseNpgsql while DI suppresses EF's pending-model warning; one shared UserManagementDbContextOptions now serves both (FOLLOW-UPS #37 closed)
- [x] 2 CustomersControllerTests (12): fixture created customers without a person; six lost-write handlers, an IDOR, a blanket catch(Exception), two stub queries, a favorites contract gap and a spec-contradicting validator behind the 12 — all fixed; suite 29/29
- [ ] 3 PayoutsControllerTests (12): diagnose and fix
- [ ] 4 PaymentsControllerTests (8): diagnose and fix (FOLLOW-UPS #31 says payment commands may reach the real gateway)
- [ ] 5 FinancialControllerTests (5): diagnose and fix
- [ ] 6 NotificationsControllerTests (10): diagnose and fix (booking-21 owns the ScheduleNotification_WithPastDate off-list case)
- [x] 8 Sweep of the lost-write class across UserManagement: seven more handlers (login, refresh, change-password, register, activate, deactivate, password reset) now commit the UM unit of work; PasswordAccountLifecycleTests (4) pin registration, login→refresh rotation, change-password and forgot-password through the real pipeline
- [x] 9 Defects found by that sweep and fixed: password double-hashed at registration (email accounts could never log in); login returned an empty refresh token; refresh scanned every user; forgot-password dereferenced null for unknown e-mails; the best-effort membership read could abort the request transaction (savepoint now confines it)
- [x] 7a 15 UserManagement lines removed from tests/known-failures.txt (68 remain, all ServiceCatalog) — reasons per line in the Log
- [ ] 7b FULL verify green for this change — blocked until booking-21's in-progress ServiceCatalogIntegrationTestBase.cs compiles again (CS0246 at 682-683 as of 10:20)

## Decisions
- 2026-09-09 Password hashing at registration: `HashedPassword.FromHash(hasher.HashPassword(pw))` replaces `HashedPassword.Create(hasher.HashPassword(pw))`, which double-hashed and made every e-mail-registered account unable to log in (RegisterUser and RegisterCustomer). Tier 2 — flag: any account registered through those two handlers before this fix has an unverifiable hash and needs a password reset.
- 2026-09-09 `User.Authenticate` now returns the refresh token it generated (was `""`), and `RefreshTokenCommandHandler` looks the user up by token through a new `IUserRepository.GetByRefreshTokenAsync` instead of loading every user. Tier 2.
- 2026-09-09 `MembershipInfoService` runs its raw read under a savepoint and enlists in the ambient transaction. In the UserManagement-only host (and any host where the pipeline's unit of work is UserManagement's) a failed read aborted the request transaction, so the login's writes were rolled back at COMMIT while the client got 200. Callers already catch and continue; now that is true at the database level as well. Tier 2 — flag.
- 2026-09-09 Which unit of work the pipeline commits is a composition fact, not a handler fact: in `Booksy.Host` it is ServiceCatalog's (registered last), in `Booksy.UserManagement.API` it is UserManagement's. UM handlers therefore commit explicitly and idempotently (flush inside an ambient transaction, commit otherwise), which is correct in both hosts. Tier 1.
- 2026-09-09 CustomersController no longer catches `Exception` (13 blocks removed; the `InvalidOperationException → 404` mapping stays). The blanket catch turned FluentValidation's `DomainValidationException` and the application exceptions into 500s; `ExceptionHandlingMiddleware` already maps them to 400/404/409. Tier 2.
- 2026-09-09 `POST /customers/{id}/favorites` returns 201 (was 200). Tests and REST semantics say Created; API_ENDPOINTS.md does not state a code; clients treat any 2xx as success. Tier 2 — flag.
- 2026-09-09 Favorites contract enforced in the handlers, not the aggregate: duplicate add → `ConflictException` (409), remove of a non-favorite → `NotFoundException` (404). The aggregate keeps its idempotent no-ops (safe for event replay); the API is the layer that promises those codes. Tier 1.
- 2026-09-09 GetUpcomingBookings and GetBookingHistory were TODO stubs returning empty lists in production. Implemented over the existing `CustomerBookingHistory` read model (fed by `BookingEventSubscribers`) through a new `ICustomerBookingHistoryReadRepository`. "Upcoming" = start ≥ now and status ≠ Cancelled, soonest first; history = all, most recent first, paged (size clamped 1..100). Tier 2 — flag: the status vocabulary is the read model's free-text status.
- 2026-09-09 Every customer command handler (profile, favorites, preferences, recently-visited, register) now commits `IUserManagementUnitOfWork` explicitly, the pattern the OTP handlers already use; before, `repository.UpdateAsync` only tracked and the ServiceCatalog-bound TransactionBehavior never flushed the UserManagement context, so those writes were lost. Tier 2 — flag: this was a silent production data-loss bug for customer profile edits, favorites and preferences.
- 2026-09-09 `CustomersController.CanAccessCustomerProfile` now enforces ownership via the `customerId` claim (Admin bypass). It was a TODO returning true for any authenticated user — an IDOR across customer profiles, favorites and preferences. Tier 2 — flag (security fix with an unambiguous intended rule; the failing 403 test already encoded it).

## Log
- 2026-09-09 10:31 Why each removed known-failures line now passes (none by weakening an assertion):
  - UserRepositorySaveTests ×3 — test-context options now identical to DI (shared configurator); the tests themselves are unchanged.
  - GetCustomerProfile_WithValidId — fixture now creates the person behind the customer (the handler loads User for name/email/phone).
  - UpdateCustomerProfile_WithValidData — handler now commits the UM unit of work; the test now asserts the persisted User.Profile (Customer.Profile is EF-ignored, see debt below) and reads with a cleared change tracker.
  - UpdateCustomerProfile_WithInvalidData, UpdateNotificationPreferences_WithInvalidTiming — controller no longer swallows DomainValidationException into a 500; middleware maps it to 400.
  - UpdateCustomerProfile_AccessingOtherCustomer — CanAccessCustomerProfile enforces the customerId claim (was `return true`).
  - AddFavoriteProvider_WithValidData — action returns 201; AddFavoriteProvider_WhenAlreadyExists — handler throws ConflictException (409).
  - RemoveFavoriteProvider_WithValidData — removal now committed; RemoveFavoriteProvider_WhenNotFavorite — handler throws NotFoundException (404).
  - UpdateNotificationPreferences_WithValidData — commit added; UpdateNotificationPreferences_DisablingBothChannels — validator rule contradicting customer-profile spec + SettingsModal removed.
  - GetUpcomingBookings — query implemented over the CustomerBookingHistory read model (was a stub).
  Also: GetBookingHistory implemented the same way (its tests only checked 200 and kept passing on the stub); a new test pins RecordProviderVisit persistence; PhoneNumberChangeTests briefly regressed when the customer test identity stopped carrying the user id as NameIdentifier — fixed by issuing both the NameIdentifier and the customerId claim.
  Debt found, not fixed: `Customer.Profile` is a constructor-time in-memory copy that CustomerConfiguration ignores; after any reload it is null, so nothing may read it (FOLLOW-UPS #43).
- 2026-09-09 Task 1 diagnosis: production DI suppresses EF's PendingModelChangesWarning; the two test files built their contexts with a bare `UseNpgsql(conn)` and so hit the warning as an exception on `Migrate()`. Fix: `UserManagementDbContextOptions.Configure` is now the single definition used by DI and both tests. Whether the model genuinely has pending changes is a separate question to settle with `dotnet ef migrations has-pending-model-changes` once the build lock is free. Task 2 diagnosis: `CreateTestCustomerAsync` minted a random UserId with no users row; the profile/preferences handlers load that User and throw → 404. Fixture now registers a real phone-first User (UserType.Customer) and hangs the Customer off it. Both await a test run — booking-21 holds the dotnet lock. Payments/Payouts/Financial/Notifications (tasks 3–6) all build their provider through the fixture booking-21 is rewriting, so they wait for that to land.
- 2026-09-09 Opened after committing the operating model and identity work (13fd58bc..1b422077). booking-21 holds the dotnet lock; diagnosis is static until it is released.
