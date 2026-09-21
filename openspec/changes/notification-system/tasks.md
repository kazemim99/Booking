Status: ACTIVE
Verify: FAST

<!-- ACTIVE since 2026-09-20. This is now the repo's gating change for the Stop hook in every session
     sharing this checkout; peers were told. -->

Change: notification-system. Proposal, design and specs are complete and validated.

Three product decisions are deliberately unresolved and marked `[?] DECISION:` below. None of them blocks
the structural work — each is a row's timing, not a shape.

## 1. Outbox — the mechanism everything else rides on

- [x] 1.1 `NotificationOutboxEntry` entity + EF configuration: event code, recipient, subject entity, copy
      parameters (JSON), scheduled-for, claim token, attempt count, state. Migration.
- [x] 1.2 Claim-and-process semantics: claim is atomic, a claimed row is invisible to a second sweep, a
      failed row returns to pending, an exhausted row is dead-lettered. Unit tests for the state machine.
- [x] 1.3 `INotificationRaiser.RaiseAsync(code, recipient, subject, parameters, scheduledFor?)` writing an
      outbox row on the caller's DbContext so it joins the caller's transaction. Test that a rolled-back
      business write leaves no intent.
- [x] 1.4 Sweep job: claim due rows, resolve via catalogue, build `Notification`, dispatch, mark processed.
      Integration test proving exactly-once under two concurrent sweeps.
- [x] 1.5 Wire the sweep into the host alongside `ProcessScheduledNotificationsJob`; confirm both run and do
      not contend for the same rows.

## 2. Catalogue

- [x] 2.1 `NotificationEventCode` as a plain (non-flags) enum covering the 37 catalogued notifications.
      Source: `openspec/changes/notification-system/` discovery + design.md.
- [x] 2.2 `NotificationEventCatalog` descriptor table: audience, channels, criticality, suppressibility,
      destination kind. One entry per code, no defaults.
- [x] 2.3 Catalogue self-validation test: every code has an entry; no non-critical entry names SMS; the
      critical set matches the documented product decision exactly.
- [x] 2.4 Wire `NotificationSuppressionPolicy` to read criticality from the catalogue instead of its own
      `HashSet`, keeping today's behaviour identical. Characterisation test first.

## 3. Copy — Persian, Jalali, salon-local

- [x] 3.1 Extend `BookingSmsText` (landed by walk-in-customer-name-sms) into the general copy seam, or a
      sibling that shares its Jalali/wall-clock rendering. Do not duplicate that logic.
- [~] 3.2 SUPERSEDED BY 3.1, except one handler. This task assumed the copy would move into seeded templates
      rendered by `ITemplateEngine`; 3.1 built `INotificationCopyWriter` instead, which needs no seeding and
      cannot silently fall back to an English template row. Every handler whose copy this task meant to move
      has since been DELETED (7.4), so there is nothing left to move — except
      `InvitationSentNotificationHandler`, which still holds its own copy and stays until 7.9 decides whether
      the outbox replaces it. Blocked on 7.9, not on this.
- [x] 3.3 Done as `PersianNotificationCopyWriterTests`: Persian text, Jalali date, salon wall-clock,
      parameter substitution and missing-parameter behaviour, plus one test asserting every catalogued code
      has non-blank wording — the per-template coverage this asked for, against the seam 3.1 chose.
- [x] 3.4 Reminder offsets DECIDED by the user 2026-09-20: T-24h and T-2h (customer), T-30m (provider),
      as proposed. The T-2h one carries the SMS, so it is one billed message per booking — accepted
      knowingly. Implemented in `BookingReminderScheduler.Offsets`; changing them is a one-line edit.

## 4. Push — real delivery and a registry

- [x] 4.1 `DeviceToken` entity + configuration + migration: user, token, platform, last-seen, revoked-at.
      Unique on token.
- [x] 4.2 Register / refresh / revoke endpoints, scoped to the authenticated caller. Re-registering an
      existing token refreshes; a token held by another user is reassigned. Tests for all three.
- [x] 4.3 Replace `FirebasePushNotificationService` with real FirebaseAdmin delivery. **Delete the fabricated
      success path** — an unimplemented or unconfigured channel reports skipped, never delivered.
- [x] 4.4 Fan out to every live token; one device failing does not block the others; no-device is a skip that
      does not consume the retry budget. Gateway-rejected tokens are pruned. Tests with a faked gateway.
- [x] 4.5 FCM credentials per environment, documented in the deployment runbook. Absent credentials must
      degrade to skipped, not to fake success.

## 5. Reminders

- [x] 5.1 Schedule reminders on booking confirmation via the outbox with a future scheduled-for. Elapsed
      offsets are skipped, not fired immediately.
- [x] 5.2 Reschedule on booking reschedule; cancel on cancel / complete / no-show. Integration tests for
      each transition.
- [x] 5.3 Exactly-once under repeated sweeps and repeated confirmation. Integration test.

## 6. Inbox

- [x] 6.1 Per-recipient read-state on the notification read model + migration. Mark-read is idempotent.
- [x] 6.2 Endpoints: list (paged, newest first), unread count, mark-read, mark-all-read — all scoped to the
      caller.
- [x] 6.3 **Ownership fix**: `GetDeliveryStatus` takes a notificationId with no ownership check today. Scope
      it to the caller and add a test proving another user's notification is refused.
- [x] 6.4 Destination resolver: recompute tap targets for a page on read, batched; a deleted or
      no-longer-owned target returns non-actionable while the text stays as written. Tests for both.
- [x] 6.5 Persist in-app notifications so an offline recipient sees them on next read; keep the SignalR push
      as a live hint, not the record.

## 7. Provider and customer coverage

- [x] 7.1 Create, cancel, complete and no-show all raise. An online booking raises BookingRequested to the
      customer and NewBookingRequest to the salon; a salon-entered one raises BookingConfirmed /
      NewBookingConfirmed and schedules reminders. A walk-in customer is deliberately NOT notified here —
      the aggregate's customer is then the salon owner, and that person is reached by the Persian SMS.
      Cancellation notifies the party who did NOT cancel, and **the actor is inferred from the
      authenticated caller, not from a request field**: the controller never set `ByProvider` anyway, and a
      client should not be able to claim it cancelled as the salon. Reschedule still to do (7.7).
- [x] 7.2 REFUND and CAPTURE done: `RefundProcessed` and `PaymentReceived` raised to the customer, each
      recorded before the commit so the money move and the notice of it are written by the same
      `CommitAsync`, and each with its legacy handler deleted in the SAME step.
      PAYOUT COMPLETED done the same way, with `PayoutCompletedNotificationHandler` deleted alongside.
      PAYMENT FAILED now done too, once 7.8 unblocked it, with `PaymentFailedNotificationHandler` deleted
      in the same step. Still to raise: payout failed/on-hold, which have no reachable flow the way
      ProviderDeactivated does not — a payout is only ever executed, never failed or held, by any code
      path that exists. Recorded with 7.10 rather than guessed at.
- [~] 7.3 STAFF ASSIGNED, INVITATION ACCEPTED and PROVIDER ACTIVATED done. Join request approved was
      REMOVED (no flow). Verification-changed and provider-deactivated are NOT BUILDABLE today — see 7.10.
- [x] 7.4 Done, and it was NOT bookkeeping — four handlers were live duplicates. Every superseded handler is
      now deleted, each in the commit that replaced it. `InvitationSentNotificationHandler` deliberately
      stays: nothing replaces it yet (7.9). FOLLOW-UPS #66 (dispatch ordering) stays open as its own change.
- [x] 7.5 Daily provider digest — DECIDED 2026-09-20: yes, 08:00 salon-local, push + in-app, no SMS.
      One intent per provider per day; the send is skipped when the day has no bookings rather than
      sending "you have 0 appointments". Salon-local means the provider's wall-clock (FOLLOW-UPS #63), not
      UTC 08:00 — and since no provider timezone exists anywhere in this system, that is 08:00 in the one
      frame bookings already live in. `DailyScheduleDigestJob` + `DailyScheduleDigestService`, 9 tests.
- [x] 7.6 Review request — DECIDED 2026-09-20: 2 hours after completion, plus ONE reminder 3 days later
      only if no review was left by then. Two scheduled intents; the 3-day one is withdrawn as soon as a
      review arrives, so it must be filed under a subject the review flow can withdraw by. Never more than
      two in total.
      **Withdraw on SUBMISSION, not on publication** (booking-d2, 2026-09-21): reviews are
      admin-moderated, so a review can exist while still invisible. Keying the withdrawal off anything
      publication-shaped would nag the one group who must never be nagged — people who did leave a review
      and are waiting on moderation. The hook is
      `WithdrawPendingForSubjectAsync("Booking", bookingId)` at the moment the review is accepted into the
      queue.
      **Consequence: a REJECTED review means the customer is never asked again.** The reminder was
      withdrawn at submission, and nothing re-raises it. Building it that way, because the alternative is
      worse: the notification system cannot see WHY a review was rejected, so re-asking would either invite
      the same refused content back or read as "we ignored you, try again". Flag if you want the opposite.
      Separate gap, not mine: nobody tells a customer their review was rejected. That belongs to the review
      flow, not to 7.6, but it is the reason the silence above is tolerable rather than rude.
      Eligibility is settled on the other side (booking-d2, 2026-09-21): completed-booking-only, one review
      per booking, written as a requirement rather than an implementation detail — so every review carries a
      booking id and this subject key cannot drift.

- [x] 7.7 Reschedule raising: the party who did not move it is told, actor inferred from the authenticated
      caller exactly as cancellation does. Filed against the NEW booking — rescheduling closes one booking
      and opens another, and everything attached to the closed one has just been withdrawn, so a notice
      left there would be cancelled before it could go out. 4 integration tests.

- [x] 7.8 `FakePaymentGateway` grew an opt-in decline, requested per payment through the request's own
      metadata (`fakeGateway: decline`) rather than by a flag on the shared fake — two collections run
      against one host in parallel, so a "fail the next call" switch would decline somebody else's payment.
      The sentinel rides the real stack (API request → command → gateway request), so a test provokes the
      failure the way a caller would instead of reaching past the seams it means to exercise.

- [x] 7.9 Emitter audit follow-through — closed 2026-09-21, see the log. Every catalogued code now has an emitter. 18 catalogued codes currently have no raise site. They are NOT one
      problem and must not be pruned as a batch:
      * **Wiring pending, flow exists** — StaffAdded, StaffRemoved, InvoiceGenerated. Ordinary remaining
        work. PaymentFailed (7.2/7.8), DailyScheduleDigest (7.5), ReviewReminder (7.6) and
        ProviderActivated (7.3) are now wired; ProviderVerificationChanged, ProviderDeactivated,
        PayoutFailed and PayoutOnHold moved to 7.10, which is a different problem.
      * **Sent today, but NOT through the outbox** — PhoneVerification (the OTP SMS, sent directly from a
        UserManagement command), Welcome, PasswordReset, SecurityAlert. These reach people already; the
        question is whether to route them through the outbox at all, which is a separate decision from
        building them.
      * **Sent today by a LEGACY handler that is still live and correct** — InvitationSent. Do not delete
        that handler until something replaces it (7.4's rule).
      * **No flow exists at all** — BookingRejected, DepositRequired, PaymentDeadlineReminder.
        BookingRejected is the closest relative of the code removed below: nothing rejects a booking. It is
        kept for now only because `Provider.RequiresApproval` and `BookingStatus.Requested` DO exist, so the
        approval path is half-built and a reject completes it rather than inventing it. If that stays
        unbuilt, this code should go the same way.

- [x] 7.10 CLOSED — all four codes removed by user decision 2026-09-21. Original text follows:
      * `ProviderVerificationChanged` — `UpdateProviderVerificationCommand` exists and has NO caller: no
        endpoint, nothing. The command is as orphaned as the join-request event was.
      * `ProviderDeactivated` — there is no provider-deactivation endpoint at all. Only
        `DeactivateProviderStaff` exists, which is a different thing (a staff member, not the salon).
      * `PayoutFailed` / `PayoutOnHold` — found while finishing 7.2. `ExecutePayoutCommandHandler` is the
        only thing that touches a payout, and it only ever completes one: nothing marks a payout failed or
        puts one on hold, so neither notification has a moment to be raised at. Same shape as the two
        above — the aggregate HAS those states, and nothing can reach them.
      All four are in the same category as the removed JoinRequestApproved. They are NOT removed yet because,
      unlike a join request, the surrounding feature plainly exists — a provider HAS a verification status
      and an active/inactive status, both persisted and both read. What is missing is the way to change
      them. Decide: build the endpoints, or drop the two codes.

## 8. `NotificationType` repair (BREAKING)

- [x] 8.1 Characterisation tests pinning today's preference behaviour, including the known false positives.
      13 unit + 5 integration. They FOUND a live user-visible defect and bounded the blast radius — see the
      log, and read 8.3 again before starting it.
- [x] 8.2 Split the type — but NOT the way this line said. The sequential members were NOT removed, and
      removing them would have been a serious mistake: see the log. `NotificationPreferenceCategory` is the
      new genuinely-flags enum (24 distinct bits, names and values copied exactly from the bit-valued half,
      so no stored preference needed rewriting). `NotificationType` kept every name and number and simply
      stopped claiming to be `[Flags]`. `PreferenceCategoryFor` renamed to `NotificationTypeFor`, which is
      what it always did.
- [x] 8.3 NOT NEEDED, and now proven rather than predicted: no name or value changed, so nothing stored
      changed meaning. `NotificationPreferencePersistenceTests` passes unmodified except for the enum it
      names. One residual risk is recorded in the log.
      Original wording, kept for the record: data migration rewriting persisted preference masks.
      **LIKELY UNNECESSARY — verify before building.** 8.1 measured it: `EnabledTypes` is persisted with
      `HasConversion<string>()`, i.e. BY NAME ("All", "BookingReminder, BookingConfirmation,
      PaymentReceived"), not as a bitmask integer. Renumbering therefore cannot silently re-label existing
      rows. `NotificationPreferencePersistenceTests.The_column_holds_names_not_a_number` is the guard; if it
      ever goes red, this task comes back. What DOES need care is any name that 8.2 removes, because a
      stored string containing it would then fail to parse.
- [x] 8.4 Done by construction. Every remaining `HasFlag` in `src/` operates on `NotificationChannel`,
      `NotificationDays` or `NotificationPreferenceCategory` — all genuine flags enums. None is on
      `NotificationType`; suppressibility was already exact set membership and stays so.

## 10. Test coverage gaps (found 2026-09-20 by mapping spec scenarios to tests)

The user restated the standing rule: every feature is test-covered and the test comes BEFORE the code.
These are the places where this change broke it — code was written first and the gaps stayed invisible
until the scenarios were counted (51 spec scenarios, 21 integration tests). Everything below is written
test-first.

- [x] 10.1 `DeviceTokenRegistryTests` (integration, real Postgres): register, re-register refreshes rather
      than duplicating, a re-used handset moves to its new owner, revoke is scoped to the caller, a
      gateway-retired token stops being used. **This code is committed with zero tests** — worst gap.
- [x] 10.2 `DeviceTokensController` scoping: register/revoke act only on the caller's own devices.
- [x] 10.3 Reminder wiring THROUGH the real commands: cancel, complete and no-show all covered via the
      API (5 tests), including that withdrawal is scoped by subject so one cancellation cannot silence
      another booking's reminders. Reschedule is exercised by the existing RescheduleResourceResolution
      tests but has no reminder-specific assertion yet — noted in 10.7.
- [x] 10.4 Dispatcher treats a no-device / not-configured push as a SKIP, not a failure — an explicit spec
      claim with no test today.
- [x] 10.5 Delivery log records a rejected send as failed and an accepted one as delivered. Two halves:
      `NotificationDeliveryLogTests` (5, real Postgres) proves the table records what it is given, and three
      new dispatcher unit tests prove it is GIVEN the right thing — the log is only as truthful as its caller.
- [x] 10.7 Reschedule: reminders move to the new booking, and are re-timed from the new start. The old
      assertion was unscoped — it asked whether ANY pending reminder existed anywhere in the table.
- [x] 10.6 End-to-end: a real API booking through confirm → outbox → sweep → notification → delivery.

## 9. Verification

- [x] 9.1 `scripts/verify.ps1 -Tier fast` green.
- [x] 9.2 GREEN 2026-09-21: 18 steps, 1,337s — both integration suites on Testcontainers, Host
      composition, both Vue apps and both Flutter apps. (Testcontainers; coordinate build time with peers —
      see ListAgents).
- [ ] 9.3 Confirm on staging that a real booking produces a real notification end to end, and that the
      delivery log contains no delivery that did not happen.

## Log
- 2026-09-20 Order note: 2.1 (`NotificationEventCode`) is done before 1.1, because the outbox row is typed
  by the event code and cannot be written without it. The rest of section 2 stays in place.
- 2026-09-20 1.1 `NotificationOutboxEntry` + EF config + migration 20260920151053_AddNotificationOutbox.
  Table only — the generated migration touches nothing else, which confirms no incidental model drift.
  Unique index (DedupKey, EventCode, RecipientId) is the dedup gate: the database serialises concurrent
  raises instead of a read-then-write that cannot. Partial index on (State, ScheduledFor) for the sweep so
  Processed rows, which accumulate forever, stay off the hot path. EventCode is stored BY NAME, because a
  row can outlive a deployment that reorders the enum.
- 1.2 State machine as `NotificationOutboxPolicy` in Domain/Policies (beside NotificationSuppressionPolicy),
  not on the persistence row: no unit-test project reaches ServiceCatalog.Infrastructure, and the decisions
  are policy rather than storage. Attempt cap and backoff are TAKEN FROM `Notification.MaxRetryAttempts`
  and DeliveryAttempt (5 attempts; 5s/15s/45s/135s/405s) rather than chosen again — a test asserts the two
  stay equal. 16 unit tests. Claiming uses a LEASE (`ClaimedUntil`), so a sweep that dies mid-row needs no
  crash detection; the lease simply expires. `NotificationOutboxStore.ClaimDueAsync` is raw SQL with
  `FOR UPDATE SKIP LOCKED` because EF cannot express it and load-then-save would let two sweeps take the
  same row; its predicate mirrors `IsClaimable` and the two must change together. Atomicity is a database
  property and is proved in 1.4 against real Postgres, not here.
- 2.1 `NotificationEventCode` — 44 codes, plain enum, deliberately NOT `[Flags]` (see design.md on the
  NotificationType defect). Done before 1.1 because the outbox row is typed by it.
- 2026-09-20 verify FAST PASS (9 steps, 124s; 1002 unit tests).
- 1.3 `INotificationRaiser` in Application (takes the code + primitives, so Application never references the
  Infrastructure row); `NotificationRaiser` in Infrastructure Adds to the scoped DbContext and does NOT save
  — whatever commits the caller's work writes the intent, which is what puts them in one transaction.
  Raising twice is a no-op, not an error: callers are command handlers that may legitimately re-run. The
  local-then-database existence check is a courtesy; the unique index is the guarantee, and a cross-
  transaction race correctly fails the duplicate's whole transaction. The rolled-back-leaves-no-intent test
  is integration (needs a real transaction) and lands with 1.4.
- 2.2/2.3 `NotificationEventCatalog`: 44 entries, each stating audience, channels, criticality and tap
  destination; `Describe` throws rather than defaulting, because a notification nobody specified would
  otherwise go out on channels nobody chose. Suppressibility is DERIVED from criticality, not stored twice.
  12 self-validation tests, of which two matter most: no non-critical entry may name SMS (each one is
  billed), and the critical set is asserted as an explicit list so that widening what a person may not
  switch off has to be a deliberate edit rather than a side effect of adding a notification.
- 1.4 `ProcessNotificationOutboxJob`: claim a batch, resolve via catalogue, hand to the EXISTING
  `SendNotificationCommand` — so preference gating, de-duplication, retry and the delivery log are reused
  rather than reimplemented. One row failing cannot stop the batch (a recipient with no phone would
  otherwise hold up everyone behind them). Recipients resolved in ONE `IPersonDirectory.FindByIdsAsync`
  per batch, not per row. The outbox row's own id is the downstream dedup key, so a row swept twice after
  a lease expiry still notifies once per channel.
  8 integration tests against real Postgres, of which two are the point: a rolled-back transaction leaves
  no intent, and two concurrent sweeps process each intent exactly once. The rollback test needed
  `CreateExecutionStrategy` — Npgsql's retrying strategy refuses a hand-rolled transaction. That was the
  test's bug, not the code's.
- 3.1 Copy seam: `INotificationCopyWriter` + `PersianNotificationCopyWriter`, built ON TOP of
  `BookingSmsText.PersianDate` rather than beside it — two Jalali renderers would eventually disagree
  about what a given day is called. All 44 notifications have Persian wording; the body and the SMS text
  are the SAME string, so a customer cannot be told different things by different channels. A missing or
  unparseable time renders «در زمان تعیین‌شده» rather than a fallback date (DateTime.MinValue would
  announce an appointment in the year 622). 10 unit tests, including one asserting every catalogued code
  has non-blank Persian wording.
- FINDING (measured while wiring 1.4): the existing notification handlers never call `SetRecipientContact`,
  and `NotificationDispatcher.ResolveRecipient` reads `RecipientEmail`/`RecipientPhone`. So on today's
  handlers Email and SMS are ALWAYS skipped, Push hits the fabricated-success stub, and InApp evaporates
  if nobody is connected — those notification rows reach nobody. The sweep resolves contact via
  `IPersonDirectory`, which is what makes an outbox notification actually deliverable. `PersonInfo` has no
  email, so `InvoiceGenerated` (the only Email entry) stays unreachable until that is added — tracked in 4.x.
- 1.5 `NotificationOutboxService` hosted service, 15s interval (a booking confirmation that waits a minute
  reads as a broken app), 1min pause after a broad failure. Registered beside the existing notification
  hosted services. It does NOT contend with `ScheduledNotificationService`: that one sweeps queued
  `Notifications`, this one sweeps `NotificationOutbox` intents — this sweep's output is that one's input.
  Several hosts may run it at once, which is precisely why the claim is database-level SKIP LOCKED rather
  than an in-process lock. 28 host-composition tests pass, so the real (unfaked) DI graph resolves it.
- FINDING: `ProcessScheduledNotificationsJob` has NO callers — it is never scheduled by anything. The
  hosted `ScheduledNotificationService` does its own sweep via the repository instead. So the job class
  reads as working infrastructure that never runs. Not touched here; noted for the cleanup in 7.4.
- 2.4 DEVIATION from this task's own wording, deliberately. The task said "keeping today's behaviour
  identical", written before the two sets were compared. They differ: the catalogue marks booking
  confirmations, cancellations and the 2h reminder Critical; the legacy `NonSuppressible` HashSet does not.
  Making the catalogue drive `IsSuppressible(NotificationType)` would therefore have silently changed
  behaviour for EXISTING callers, which is worse than either option.
  What was built instead: `ShouldSend` takes an optional `NotificationEventCode`. When present (i.e. the
  notification came through the outbox) the catalogue decides; when absent the legacy type path is
  untouched, and a characterisation test pins that. This also fixes a real gap — without it, an
  outbox-raised Critical notification would still have been suppressible, contradicting the
  notification-event-catalog spec.
  The event code had to reach the dispatcher for this, so `Notification.EventCode` was added (migration
  AddNotificationEventCode, one nullable column, stored by name). That column is needed by the inbox
  (6.x) regardless — the spec requires the client to select presentation from the code.
  A test asserts the catalogue is AT LEAST AS protective as the legacy set, so this can never quietly let
  someone silence something that used to be guaranteed.
  NOTE FOR THE USER: the catalogue does WIDEN what cannot be switched off (booking confirmed/rejected/
  rescheduled/cancelled-by-salon, the 2h reminder, new-booking-request, invitation). That is a product
  decision implied by the SMS-critical scoping; say so if you want any of them suppressible.
- 2026-09-20 verify FAST PASS (9 steps, 223s; 1033 unit tests).
- 4.x Push. FirebaseAdmin 3.1.0 restored fine — NuGet works here even though Google Maven does not (that
  memory is about Gradle/Android, not .NET).
  4.1 `DeviceToken` + migration AddDeviceTokens. Unique index on Token is what makes "reassign, never
  duplicate" true even when two sign-ins race: the second MUST update, it cannot insert. Revoked rather
  than deleted, so a returning device is distinguishable from a new one.
  4.2 `DeviceTokensController` — register (idempotent refresh) and revoke, both scoped to the caller via
  `User.GetUserId()`, which is the helper booking-39 fixed for the nameidentifier claim defect; revoking
  by token alone would have let anyone silence anyone else's device. Revoke returns 204 whether or not the
  token existed, so it cannot be used to probe which tokens are registered.
  4.3 **The fabricated success is gone.** The stub returned (true, newGuid, null) without sending, so the
  delivery log answered "yes, delivered" for messages that never existed. Now: an unconfigured environment
  reports SKIPPED, never delivered, and a misconfigured one degrades the same way instead of failing
  startup. Introduced `IFirebaseMessagingGateway` as the boundary — the absence of such a seam is part of
  why a non-sending stub could sit there looking like a service.
  4.4 Fan-out over every live device; one handset failing does not stop the others and the notification
  counts as delivered if any device took it; a gateway-declared dead token is retired (the gateway is the
  only thing that can tell us an app was uninstalled). No-device and not-configured return sentinels the
  dispatcher treats as SKIPS — counting them as failures would burn the retry budget and eventually
  dead-letter a notification whose other channels were fine. The sentinels live on the Application contract
  (`PushUnavailable`) so sender and dispatcher cannot drift on the exact wording.
  4.5 `Notifications:Firebase:CredentialsPath|CredentialsJson` in appsettings, documented in
  docs/DEPLOYMENT_RUNBOOK.md under "Outbound services" with how to tell which state a running host is in.
- NEW TEST PROJECT: `tests/Booksy.ServiceCatalog.Infrastructure.UnitTests`, added to Booksy.sln and to BOTH
  verify.ps1 and verify.sh. Reason: this is the second time a genuinely unit-testable Infrastructure class
  (push fan-out; both its dependencies are interfaces) had nowhere to live, and the test-architecture rule
  says anything not needing Docker/WebApplicationFactory/DbContext belongs in a unit project. 8 tests.
  FAST is now 10 steps.
- 2026-09-20 verify FAST PASS (10 steps, 101s).
- 6.x Inbox. NO MIGRATION NEEDED for read-state: `Notification.ReadAt` already existed and a notification
  has exactly one recipient, so per-recipient read-state is that column. `MarkAsRead` was already
  idempotent via `??=`.
  One domain change was needed: `MarkAsRead` refused any status but Delivered, and an in-app notification
  sits at Sent — no gateway ever confirms it — so the inbox could not mark its own rows read. Widened to
  Sent/Delivered/Read, still refusing states where the notification reached nobody (queued/failed/
  cancelled/expired/dead-lettered), because marking one of those read asserts something untrue.
  6.2 `GET inbox`, `GET unread-count`, `POST {id}/read`, `POST read-all` — all scoped to `User.GetUserId()`.
  6.3 **SECURITY FIX**: `GetDeliveryStatus` took a notification id alone, so ANY signed-in user could read
  ANY notification's delivery record — including the recipient's phone number and the gateway's response.
  Now scoped to the caller, and a foreign id answers 404 rather than 403 so it cannot be used to probe
  which ids exist. Same pattern for mark-read.
  6.4 Destination recomputed per page in one batched lookup; a booking that is gone, or is no longer the
  reader's, comes back non-actionable while its text stays as written.
  Unread count deliberately counts Sent/Delivered only — badging a Queued or Failed notification sends the
  person looking for a message that does not exist.
  8 integration tests through HTTP (not the handlers), because the property that matters is authorization
  and testing it below the controller would not prove it.
  Two defects found by those tests, both real: the destination resolver compared `b.Id.Value` on a
  strongly-typed id, which has no SQL translation and 500'd the whole inbox; and the API wraps responses in
  a `data` envelope.
- 2026-09-20 verify FAST PASS (10 steps, 267s; 1041 unit tests).
- 5.x Reminders. `IBookingReminderScheduler` raises them through the outbox, so they commit with the
  booking change that caused them. Offsets live in ONE list (T-24h + T-2h customer, T-30m provider) —
  changing them is a one-line edit. Built on the assumed values; 3.4 stays a DECISION because the T-2h one
  carries the SMS and therefore a recurring per-booking cost.
  An offset whose moment has already passed is SKIPPED, not fired: a booking made an hour beforehand must
  not instantly send the day-before reminder.
  Wired into the real lifecycle: confirm schedules; cancel, complete and no-show withdraw; reschedule
  withdraws from the old booking and schedules against the new one (reschedule closes one booking and opens
  another, so the reminders have to move rather than be edited).
  The scheduler resolves salon and service names itself rather than making every caller fetch them, and
  captures them into the intent — the sweep must never re-read them, because by then the service may have
  been renamed or deleted.
  Dedup keyed on the booking id + code, which is what makes re-confirming idempotent. 5 integration tests.
- FINDING → FOLLOW-UPS #67: `AddNotificationBackgroundServices` is registered in a method NOTHING CALLS,
  so `NotificationProcessorService`, `ScheduledNotificationService` and `NotificationCleanupService` have
  never run in any environment — which is also why `ProcessScheduledNotificationsJob` has no caller. Found
  because a scoped service registered there failed to resolve at runtime. This change does not depend on
  any of them (its own sweep is registered in the Infrastructure extension, which the host does call).
  Deliberately NOT fixed by calling the method: that would start three never-executed services, one of
  which deletes notifications.
- 2026-09-20 verify FAST PASS (10 steps, 72s).
- 2026-09-20 TEST-FIRST correction. The user restated the standing rule — every feature test-covered, test
  BEFORE code — after I answered honestly that coverage was incomplete. Mapping the specs to tests found
  51 scenarios against 21 integration tests, and three gaps already committed.
  Closed so far, written test-first:
  10.1 `DeviceTokenRegistryTests` — 11 integration tests on real Postgres. All passed on the first run,
  which is luck rather than process: the code had been committed unproven. Covers register, refresh not
  duplicate, a handset changing hands moving to its new owner (the unique index makes a second row
  impossible), revoke scoped to the caller, revoke idempotent, gateway-retire, and a returning device.
  10.4 `NotificationDispatcherSkipTests` — 4 unit tests. Had to be unit, not integration: the integration
  push fake always succeeds, so the skip path is unreachable there. Pins that no-device and not-configured
  are SKIPS, that a genuine failure still burns the budget, and that a skipped push does not mark a
  notification failed when its SMS went out.
  10.3 `BookingReminderWiringTests` — 3 tests through the real cancel endpoint, because the previous
  reminder tests called the scheduler directly and proved nothing about the five handlers I changed.
  Still open: 10.2 (controller scoping), the rest of 10.3, 10.5, 10.6.
- 2026-09-20 verify FAST PASS (10 steps, 85s; 1045 unit tests).
- 2026-09-20 (cont.) 10.2 `DeviceTokensControllerTests` — 6 tests. Identity comes from the token, never the
  request: registering cannot attach a device to another account, revoke cannot silence another person's
  phone, and revoke answers 204 either way so it cannot be used to discover whether a token exists.
  10.3 finished. Complete and no-show needed real thought about booking state rather than a copied fixture:
  `Confirm()` demands two hours' notice, `Complete()` demands the start be within fifteen minutes, and
  `MarkAsNoShow()` demands the appointment be over — no single fixture satisfies them. Complete/no-show
  therefore use `CreateConfirmedByProvider` (the salon path, which is confirmed on creation) at +5min and
  -3h respectively, with the pending reminder seeded directly, because by then every reminder offset has
  correctly elapsed. The point under test is the handler's withdrawal call, not how the row arrived.
- 2026-09-20 verify FAST PASS (10 steps, 150s).
- 2026-09-20 DECISIONS from the user, all three answered together:
  * Daily digest: YES, 08:00 salon-local. (I had flagged doubt — the salon already sees this on its Today
    screen — and the user overrode that; building it.)
  * Review request: 2h after completion + one 3-day reminder if still no review. The reminder needs a
    withdrawal hook on the review flow, otherwise it nags people who already reviewed.
  * Push: NOT yet. Commits stay local until sections 7-9 are done, then push once.
- 2026-09-20 10.6 + 7.1, written test-first and it paid for itself immediately.
  `NotificationEndToEndTests` (6) starts at POST /api/v1/bookings and ends at the customer's inbox. It was
  RED when written — a real booking produced no notification at all, because nothing raised one. That is
  what made it the failing test for 7.1 rather than a gap discovered later.
  **BUG FOUND BY WRITING IT**: `BookingReminderScheduler` addressed the salon's reminder to
  `booking.ProviderId` — a ProviderId, where the inbox, preferences and device registry are all keyed by
  USER id. That reminder could never have been seen by anyone. Now resolved to `provider.OwnerId`, and
  skipped entirely when there is no owner to tell.
  That fix then correctly broke `BookingReminderTests`, which had been fabricating `ProviderId.New()`. The
  test was asserting against a fiction; it now creates a real provider. This is the test-first rule paying
  out twice in one task.
  Also of note: `Nothing_is_delivered_before_the_sweep_runs` proves the notification really does travel
  through the outbox rather than being sent inline by the request — the design claim, asserted.
  82 notification integration tests pass.
- 2026-09-20 verify FAST PASS (10 steps, 200s).
- 2026-09-21 Cross-session constraint from booking-d2 (provider-reviews-and-ratings): reviews are
  admin-moderated. Recorded on 7.6 — the 3-day reminder must be withdrawn on submission, not publication.
  Their side raises two notifications I do not: provider on a newly published review, customer on a
  provider reply. They will come to me before wiring either.
- 2026-09-21 7.1 finished, test-first. `BookingLifecycleNotificationTests` (7) written RED first; all seven
  failed, then the handlers were built to satisfy them.
  The addressing rule they hold: a notification goes to the party who did NOT act. A customer who cancels
  already knows; it is the salon whose day has a hole. One test asserts no notification is ever addressed
  to a ProviderId — that bug has already happened once, in the reminder scheduler.
  DESIGN CALL: the cancelling actor is derived from `ActingUserId == provider.OwnerId`, not from the
  request's `ByProvider` flag. The controller never populated that flag (it always defaulted false, so the
  domain has never recorded a provider cancellation), and a client-supplied "I am the salon" is not
  something to trust for addressing.
  Review request is raised on completion, SCHEDULED +2h rather than sent immediately — asking as somebody
  walks out of the salon is worse than not asking.
  REGRESSION CAUGHT BY THE FULL SUITE: the four `BookingReminderWiringTests` began failing, correctly. The
  handlers now raise a NEW notification after withdrawing the reminders, filed under the same booking
  subject and legitimately Pending — so "every row for this booking is Cancelled" was asserting something
  false. Assertions now name the reminder codes. This is the blunt-instrument property of
  `WithdrawPendingForSubjectAsync` that I warned booking-d2 about, showing up in my own tests.
- 2026-09-21 588 integration tests pass; verify FAST PASS (10 steps, 107s).
- 2026-09-21 7.7 done, test-first (4 red, then green).
  Two arrange lessons worth recording, both cost a cycle:
  * A reschedule needs a DIRECT booking (staffId == provider id). Using a bookable member id needs
    availability rows this fixture does not create, so the reschedule is refused with 400 long before any
    notification is reached — the test fails for a reason that has nothing to do with what it tests.
  * `Booksy.ServiceCatalog.Domain.Enums.DayOfWeek` shadows `System.DayOfWeek` in these files. Qualify it.
  Reused the `provider` already loaded and validated by the handler rather than fetching it again — the
  booking cannot change salon, so a second lookup would only be a second chance to disagree with itself.
- 2026-09-21 592 integration tests pass; verify FAST PASS (10 steps, 94s).
- 2026-09-21 DEFECT found by booking-d2, verified here, fixed test-first. Two separate bugs, one of them
  mine and worse than the one reported.
  (a) DUPLICATE REVIEW REQUEST: `BookingCompletedNotificationHandler` scheduled its OWN review request at
  CompletedAt+2h with hardcoded English HTML and an `href='#'` button, while `CompleteBookingCommandHandler`
  already raises the outbox ReviewRequest for the same booking. Handler DELETED — this is exactly 7.4's
  "remove each superseded handler once its outbox coverage is in place", and the coverage is in place.
  Verified no other references before deleting.
  (b) MINE, AND WORSE: the inbox showed notifications that had not been delivered. `GetInbox` used
  `GetUserNotificationHistoryAsync`, which filters on recipient ALONE — no status, no scheduled-for — so a
  Queued, future-dated legacy row appeared in the list as if it had arrived, while `GetUnreadCountAsync`
  correctly counted only Sent/Delivered. The list and the badge disagreed, and nobody could tell which was
  lying. Inbox now filters to Sent/Delivered/Read, matching the count.
  Note the outbox path was never affected: a scheduled INTENT does not become a Notification until it is
  due, so it cannot appear early. The bug was reachable only through the legacy schedule command — which
  is precisely the sort of thing that survives when two mechanisms coexist.
  3 new inbox tests, including one asserting the list and the badge agree.
- 2026-09-21 595 integration tests pass; verify FAST PASS (10 steps, 81s).
- 2026-09-21 7.2 (refund) — and a CORRECTION to something I had written down as fact twice.
  I recorded that payment domain events are never dispatched, reasoning that payment commands are
  `INonTransactionalCommand` (so TransactionBehavior skips them) and commit with `CommitAsync` (which my
  earlier mapping showed had no dispatch). **Both premises were right and the conclusion was wrong.**
  `CommitAsync` is a one-line delegate to `SaveChangesAsync`, which DOES dispatch — its own comment says
  "Delegate to SaveChangesAsync which dispatches events". My mapping had searched for direct
  `DispatchDomainEventsAsync` calls and never followed the `CommitAsync -> SaveChangesAsync` hop. Same
  class of error as the original dispatch misreading: tracing one level and stopping.
  The only reason I know is that I wrote the assertion as an EMPIRICAL test — "the legacy handlers produce
  nothing" — instead of asserting my reading. It failed, and the row it found had Subject "Refund
  Processed", the legacy handler's English text.
  Consequence had I trusted the trace: every refunded customer would have received TWO notices, one
  Persian from the outbox and one English from the legacy handler.
  `PaymentRefundedNotificationHandler` deleted. The other three (Processed, Failed, PayoutCompleted) are
  LEFT IN PLACE deliberately — 7.4's rule is to remove a handler only once its outbox coverage exists, and
  theirs does not yet. Whoever does the rest of 7.2 must delete each one in the same commit that replaces
  it, or ship a duplicate.
  Test rewritten around the real risk: a refund produces exactly ONE notification, and nothing outside the
  outbox notifies about refunds.
  Test-infra note: the outbox sweep hosted service runs every 15s INSIDE the test host, so a slow test sees
  its own swept rows. Distinguish by EventCode — outbox notifications carry one, legacy ones cannot.
  Arrange note: build a Payment through `Payment.CreateForBooking` + `ProcessCharge`, never raw SQL. A
  hand-written INSERT produced a row the refund path could not load and failed as an opaque 500.
- 2026-09-21 598 integration tests pass; verify FAST PASS (10 steps, 99s).
- 2026-09-21 7.2 (capture). `PaymentReceived` raised from `CapturePaymentCommandHandler`;
  `PaymentProcessedNotificationHandler` deleted in the same commit, which is the rule the previous entry
  established after the refund near-miss. 2 tests, one of them asserting exactly ONE notification and no
  non-outbox row — that assertion is the whole guard against the duplicate coming back.
  Arrange note: capture needs an AUTHORISED payment, not merely a pending one (`Authorize` then capture);
  a pending-only payment is refused with "has not been authorized".
  PaymentFailed deliberately NOT built: the fake gateway cannot fail, so the test cannot be written first.
  Raised as 7.8 rather than building it blind.
- 2026-09-21 600 integration tests pass; verify FAST PASS (10 steps, 70s).
- 2026-09-21 7.2 (payout). `PayoutCompleted` raised from `ExecutePayoutCommandHandler` before its commit;
  legacy handler deleted in the same step. 3 tests, one of which asserts the notice goes to the OWNER and
  that NOTHING is addressed to the provider id — the third place in this change where that mistake would
  have produced a notification nobody could ever see, so it is now asserted rather than remembered.
  Note this handler commits with `CommitAndPublishEventsAsync`, unlike capture and refund which use
  `CommitAsync`. Three different commit calls across the money paths, all of which dispatch; the outbox
  row rides whichever one the handler uses because it is written to the same context.
- 2026-09-21 603 integration tests pass; verify FAST PASS (10 steps, 73s).
- 2026-09-21 7.3 (staff assignment). `StaffAssignedToBooking` raised from `AssignStaffToBookingCommandHandler`.
  The recipient needed RESOLVING, unlike every other notification so far: the staff id on a booking is a
  MEMBERSHIP, not a person, so addressing it directly would have reached nobody — the fourth variant of the
  same mistake (provider id, membership id, both keyed differently from the inbox). An unresolvable
  membership is skipped rather than guessed at.
  A TEST OF MINE WAS WRONG, not the code: I asserted "the salon is not told about its own assignment", and
  it failed because in this fixture the bookable member IS the owner — a solo salon where the owner does
  the work. The owner legitimately receives it: that is the practitioner being told about their day, not
  the organisation being told about its own click. They are the same human. Rewritten to assert the actual
  rule — the recipient is the PERSON behind the membership, and never the membership id.
  Worth keeping in mind for 7.5: several fixtures collapse owner and staff into one person, so any test
  distinguishing "salon" from "practitioner" needs a second member or it is asserting nothing.
- 2026-09-21 606 integration tests pass; verify FAST PASS (10 steps, 59s).
- 2026-09-21 7.3 (invitation accepted). Raised from `RegisterAndAcceptInvitationCommandHandler` before its
  `SaveAndPublishEventsAsync`. Fills a real gap rather than replacing a handler: `InvitationSent` already
  told the invitee, but the OWNER had no way to learn their invitation was taken up except by looking.
  Addressed to the owner as a person; a second test asserts nothing is addressed to the organisation id.
  Touched an existing unit test — `RegisterAndAcceptInvitationConcurrencyTests` constructs the handler
  directly, so two substitutes were added. Left as substitutes on purpose: that class tests the CONCURRENCY
  of accepting, and a notification raised against a stubbed provider lookup would assert nothing. The
  behaviour is covered by the integration tests instead.
- 2026-09-21 608 integration tests pass; verify FAST PASS (10 steps, 73s).
- 2026-09-21 REMOVED `NotificationEventCode.JoinRequestApproved`. Nothing in the codebase creates or
  approves a join request — all that exists is an orphan `JoinRequestApprovedEvent`, a status enum, a
  response model and a table from a 2025 migration. A code with no possible emitter advertises a
  notification the product cannot send, which is the single thing this catalogue exists to prevent, and it
  came with a catalogue entry, a preference mapping and Persian copy that all read as working.
  Removed from all four places at once because the completeness tests enforce that they move together.
  Enum value 52 is left UNUSED rather than reassigned: the code is persisted by name in the outbox, but a
  reused number would still be the sort of thing that bites during a migration.
  Audit done while there: 18 codes have no emitter, recorded as 7.9 with the categories, because they are
  not one problem and pruning them as a batch would delete work that is merely unfinished.
- 2026-09-21 7.3 (provider activated). Raised from `ActivateProviderCommandHandler`. 2 tests.
  ARRANGE LESSON, and a real one: the first version created an active provider and demoted it with raw SQL.
  That does not work — provider reads are CACHED (which is what `ProviderCacheInvalidationEventHandler` is
  for), so the UPDATE landed in the table and the handler went on seeing an active provider. The error was
  "Provider is already active" from a row that said otherwise. Build the aggregate in the state you need
  (`ProviderBuilder().WithStatus(PendingVerification)`); do not mutate around the cache.
  Found while here: `UpdateProviderVerificationCommand` has NO caller and there is no provider-deactivation
  endpoint, so two more catalogued codes are unreachable. Recorded as 7.10 rather than removed — unlike the
  join request, the surrounding feature exists and only the way to change it is missing.
- 2026-09-21 610 integration tests pass; verify FAST PASS (10 steps, 74s).
- 2026-09-21 7.5 daily digest, test-first. 9 integration tests written before the job existed — which in a
  statically-typed language is the only "red" available, since the test cannot compile until the seam does.
  Because all nine passed on the first run, and the log above already records that passing first time is
  luck rather than proof, the job was MUTATED to check the tests discriminate: removing the morning window
  and counting cancelled/requested bookings turned 4 of the 9 red, each for its own reason. Restored.
  DESIGN CALL — this is the only notification nothing in the business causes, so it is a job rather than a
  handler, and the count is taken AT SEND TIME rather than captured at raise time. That looks like a
  violation of the raiser's "capture the parameters when it happened" rule and is not: for a digest the
  thing that happens IS the count being taken. Raising on the day's first booking would tell a salon with
  nine appointments that it has one.
  "08:00 salon-local" has only one honest reading today: there is NO provider timezone anywhere in this
  system, and booking times are salon wall-clock values in a single frame (FOLLOW-UPS #63). So the digest
  fires at 08:00 in that same frame. If #63 is ever resolved with a real per-provider timezone, the job and
  its tests are where the conversion belongs.
  Two judgment calls worth flagging rather than burying:
  * A day is SKIPPED, not announced late, after 12:00. A host restarting at lunchtime would otherwise tell a
    salon about a schedule it has already worked through, and that reads as current.
  * `Completed` counts toward the day, `Requested` does not. A request is not an appointment; counting it
    would tell the salon it has work booked that it has not agreed to do.
  De-duplication is per salon per DAY, via a derived key (SHA-256 of provider id + day number) — the outbox
  key is a Guid, so the day has to be folded into it, or tomorrow's digest would be swallowed as a duplicate
  of today's.
- 2026-09-21 7.6 review request + 3-day reminder, test-first. 5 integration tests, and again mutated rather
  than trusted: reverting the withdrawal to its blunt form failed 1, removing it entirely failed 2.
  TWO DESIGN CALLS, both of which changed the shape of the change.
  (1) The reminder is its OWN code, `ReviewReminder = 14`, not a second `ReviewRequest`. Two reasons, either
  sufficient: the outbox de-duplicates on (key, code, recipient), so a repeat under the same code would have
  been silently swallowed and the feature would have looked built while sending nothing; and the wording
  must differ, because a recipient who sees the same sentence twice reads it as a bug, not a reminder.
  (2) Withdrawal is now TARGETED. `WithdrawPendingForSubjectAsync` grew an optional event-code filter, and
  the review hook names the two review codes. The blunt form — cancel everything unsent about the booking —
  stays exactly right for cancellation, where the appointment is off and nothing about it should go; it is
  wrong here, because the booking still happened and a review says nothing about a refund notice queued
  against it. This is the blunt-instrument hazard I flagged to booking-d2 turning up in my own work, so it
  is now a test (`Leaving_a_review_does_not_silence_anything_else_about_the_booking`) rather than a caveat.
  An empty code list means "withdraw nothing", not "no filter" — the opposite reading is a silent disaster.
  The hook sits in `CreateReviewCommandHandler`, i.e. at SUBMISSION. There is no moderation state on the
  Review aggregate yet (booking-d2 is building it), so today submission is the only event there is — but the
  hook is already in the place that stays correct once moderation lands, which is the whole point of the
  cross-session agreement.
- 2026-09-21 7.8 then 7.2's last piece, in that order, which is the whole point of 7.8 existing.
  7.8: the fake gateway declines when THE PAYMENT asks it to, via metadata, not when a flag is set on the
  fake. Two test collections share one host in parallel, so a "fail the next call" switch would have
  declined another test's payment and the flake would have read as a defect in the payment code. The
  sentinel also rides the real stack — API request, command, gateway request — so the failure is provoked
  the way a caller would rather than by reaching past the seams under test.
  7.2 PaymentFailed: raised from the `else` branch that already existed in `ProcessPaymentCommandHandler`,
  before its CommitAsync, so the failed payment and the notice of it are written together.
  `PaymentFailedNotificationHandler` deleted in the same commit — the rule the refund near-miss established.
  Two mutations, both bit: restoring the legacy handler failed the "exactly one" test (so the duplicate
  would NOT have shipped silently), and making the fake always decline failed the two success tests.
  `ProcessPaymentCommandHandlerTests` needed two substitutes; left as substitutes deliberately, same
  reasoning as `RegisterAndAcceptInvitationConcurrencyTests` — that class tests unit-of-work commit, and a
  notification raised against a stubbed provider lookup asserts nothing.
  FOUND WHILE HERE, recorded on 7.10: `PayoutFailed` and `PayoutOnHold` have no reachable flow either.
  `ExecutePayoutCommandHandler` is the only code that touches a payout and it only ever completes one.
  Four codes now sit in that category, all the same shape: the aggregate has the state, nothing can reach it.
- 2026-09-21 10.7 and 10.5 done. Section 10 is closed.
  10.7 — AND A CORRECTION TO MYSELF. I replaced my own unscoped reminder assertion and wrote in the new
  test's comment that the old one "would have passed with the reschedule scheduling nothing at all". Then I
  measured it instead of leaving the claim standing: with `_reminders.ScheduleAsync` removed from the
  handler, the OLD unscoped assertion FAILED too. It was weaker, not vacuous, and the comment now says so.
  What the old one genuinely could not do is tell a reminder that moved to the successor from one left on
  the closed booking — opposite outcomes, same query result. The two new tests name the booking each
  reminder must be on, and check the 24h/2h rows are re-timed from the NEW start: moving the rows without
  moving the times would still remind somebody about an appointment they no longer have.
  10.5 — deliberately in two halves, because "the delivery log is correct" is two claims.
  `NotificationDeliveryLogTests` (5, real Postgres, not a substitute): the recording is a raw UPDATE whose
  null handling has already caused one silent failure, and only a real database catches that class.
  It also pins the state machine the de-duplication depends on — Failed stays re-claimable (else one
  transient gateway error becomes a message nobody ever receives), Delivered does not, and recording one
  recipient's outcome does not touch another's.
  Three dispatcher unit tests for the other half: a rejection is reported with success:false, an acceptance
  with success:true and the gateway's id, and a SKIP is never reported as a delivery — that last one is
  precisely the lie the fabricated push stub used to tell, and the delivery log is where it would have
  lived on after 4.3 removed the stub.
  Mutation: forcing the log to always write Delivered failed 2 of the 5, including the retry one.
- 2026-09-21 8.1 done — and it changed what section 8 is.
  13 unit characterisation tests + 5 integration. Written as characterisation, not specification: several
  assert outcomes that are DEFECTS, labelled so, so the repair reads as a diff.
  TWO FINDINGS, both measured rather than reasoned about.
  (1) THE BLAST RADIUS IS SMALLER THAN THE TASK ASSUMED. Nothing in the live send path consults
  `EnabledTypes`. `NotificationSuppressionPolicy.ShouldSend` asks only about channels; the one method that
  tests type membership, `UserNotificationPreferences.ShouldSendNotification`, HAS NO CALLERS, and neither
  do the aggregate's `EnableTypes`/`DisableTypes` (the controller's "enable types" endpoint replaces the
  whole mask instead of OR-ing into it). So today no notification is sent or withheld because of the flags
  defect. A test now pins that, because 8.2 must not start honouring types by accident.
  (2) BUT IT IS NOT PURELY LATENT — there is a live, user-visible defect I had not expected. The settings
  screen renders the mask with `ToString().Split(',')`, and .NET decomposes a flags value greedily, largest
  first. `AccountDeactivated` is bit24|bit4, and bit4 IS `BookingConfirmation` — so for a mask of
  `All | ReviewRequest` the rendering OMITS BookingConfirmation (which is enabled) and INVENTS
  AccountDeactivated (which was never set). Two tests pin exactly that.
  (3) Consequence for 8.3: the persisted column is a NAME string, not an integer — measured against the raw
  column in SQL, not through EF. So renumbering cannot rewrite what existing rows mean, and the data
  migration is probably unnecessary. Marked on 8.3 with the guard test that would prove otherwise.
  Note for 8.2: the 17 sequential members cannot simply be given distinct bits — 24 existing bit values plus
  17 more exceeds an int's 32. That is the arithmetic reason 8.2's split is the only available repair: those
  17 are notification IDENTITIES, which now live in NotificationEventCode, not preference categories.
- 2026-09-21 Section 8 done — and the plan in 8.2 was WRONG, which the user's go-ahead was given under.
  I asked whether I could remove the 17 sequential members, framing the risk as "probably zero, I will count
  the affected preference rows first". The user said yes. Counting rows was not the point: `Notification.Type`
  AND `NotificationTemplate.Type` are ALSO persisted with `HasConversion<string>()`, the outbox sweep writes
  that type on every notification it sends (15 of the catalogue's mappings return a sequential member), and
  the template seeder seeds 16 of the 17. Removing those names would have broken reading rows the system
  writes right now, plus every template and all notification history. Said so and took the other route.
  WHAT WAS ACTUALLY WRONG was never the members' existence — it was one enum doing two incompatible jobs:
  a persisted discriminator (wants one value per notification, grows forever) and a preference mask (wants
  distinct bits, capped at 32). Growing the first past 24 bits under a `[Flags]` attribute is the whole
  defect. So the two jobs were separated instead:
  * `NotificationPreferenceCategory` — new, real flags, 24 distinct bits. Names AND values copied exactly,
    which is what makes the split free: preferences are text, so every stored row parses to the set it
    always meant. `All` now genuinely contains everything, because nothing sits outside the bits any more.
  * `NotificationType` — every name and number untouched, `[Flags]` removed, `All` moved out (it was a mask
    idea; no notification or template row ever carried it).
  * `PreferenceCategoryFor` → `NotificationTypeFor`. The old name was the conflation made official: its
    result goes to `Notifications.Type` and was never consulted about anyone's preferences.
  NOTE, measured: removing `[Flags]` does NOT stop `HasFlag` working — that is bit arithmetic and ignores
  the attribute. `RefundIssued.HasFlag(RefundProcessed)` is still true, and a test says so plainly rather
  than implying a fix. What the attribute removal DOES fix is `ToString()`, which stops decomposing greedily
  — and that was the live, user-visible defect (the settings screen dropping a category the user had enabled
  while naming one they had not).
  RESIDUAL RISK, stated not hidden: a preference row whose `EnabledTypes` text holds one of the 17 names
  would now fail to parse. Only the `types/enable` endpoint could ever have written one, and no client calls
  it — checked across booksy-frontend, booksy-admin and both Flutter apps, zero references. Production
  cannot be queried from here (ssh is a protected operation), so this is argued, not counted.
  639 integration + 598 domain tests green; FAST green.
- 2026-09-21 7.4 finished — and the four remaining booking handlers were sending duplicates RIGHT NOW.
  `BookingLifecycleDuplicateTests` written first; all four RED. Cancelling, confirming, marking a no-show and
  rescheduling each produced a second notification outside the outbox — the cancel one reading "Booking
  Cancelled" in English, beside the Persian one the customer had already been sent. Third time this
  near-miss has surfaced in this change, and the first time it had actually shipped.
  The tests count notifications with NO event code, because only the outbox stamps one. Asserting "the
  outbox row exists" would have passed happily while the customer received two messages.
  Deleted: BookingCancelled / BookingConfirmed / BookingNoShow / BookingRescheduled NotificationHandler.
  `InvitationSentNotificationHandler` kept — nothing replaces it (7.9's rule).
  FOUND WHILE HERE → FOLLOW-UPS #64: `POST /bookings/{id}/confirm` answers 500 for ANY booking requiring no
  deposit, which is every salon on `BookingPolicy.Default`. PaymentMethodId is `[Required]`, the handler
  processes a deposit whenever one is supplied, and `WithDepositPaid` throws when the amount is zero. The
  test builds a deposit-requiring policy purely to get through the endpoint; deleting that workaround is the
  check that #64 is fixed. Not fixed here — which of the two one-line fixes is right is a booking decision.
  BOOKKEEPING CORRECTION, mine, worth recording because it nearly corrupted the follow-up log: I read the
  FOLLOW-UPS table, saw it ended at 63, and concluded my own earlier references to #66 and #67 were dangling.
  They were not. That file holds PROSE SECTIONS as well as table rows, and both entries were already there —
  my grep only matched rows. I had already added two duplicate rows and renumbered the live references
  before checking; all reverted. The new finding went in as a prose section like its neighbours.
  Genuinely inconsistent and NOT mine: #67 is used twice in that file — the background services and the
  Toman/Rial reconciliation.
- 2026-09-21 DECISIONS from the user, all four answered together:
  * 7.10 — REMOVE all four unreachable codes (ProviderVerificationChanged, ProviderDeactivated,
    PayoutFailed, PayoutOnHold). Each comes back in the change that builds its flow.
  * 7.9 account notifications — LEAVE THEM ALONE. PhoneVerification/Welcome/PasswordReset/SecurityAlert keep
    their current path; routing them through the outbox would add up to 15s of sweep latency to the OTP,
    which is the product's most latency-sensitive message, for no user-visible gain.
  * 7.9 flowless codes — BUILD the booking-reject flow (BookingRejected stays and gets wired; it completes
    the half-built approval path rather than inventing one, and "the salon never answered" is the worst
    customer experience available). REMOVE DepositRequired and PaymentDeadlineReminder; deposits are their
    own project and the codes return with it.
  * InvitationSent — MOVE IT to the outbox and delete the legacy handler in the same commit. It is the last
    English notification in a Persian product.
- 2026-09-21 InvitationSent NOT moved to the outbox, and the decision to move it was taken on my bad
  information. Three things, the third decisive:
  1. I told the user its copy was "English and hardcoded". It is hardcoded and it is PERSIAN. Wrong.
  2. It carries the INVITATION LINK (`/provider/invitations/{id}/accept?org=…`), which is the only way the
     invitee can act on it. The copy writer's `InvitationSent` wording has no link, so moving it as-is would
     have sent a message that cannot be acted on.
  3. THE OUTBOX CANNOT ADDRESS THE INVITEE. It is keyed by user id: the sweep resolves contact through
     `IPersonDirectory.FindByIdsAsync` and passes `person?.PhoneNumber`. An invitee is normally not a user
     yet — the flow is literally `RegisterAndAcceptInvitation`, i.e. they register by accepting — so there
     is no Person, the phone comes back null, and the dispatcher SKIPS the SMS. The invitation would simply
     never arrive, silently. This is the same "recipient must be a real user" constraint that has already
     produced four addressing bugs in this change, reached from the other side.
  So the legacy handler stays, and the catalogue comment that predicted exactly this ("the invitee is often
  not a user yet, so SMS is the only way to reach them") was right. Moving it needs the outbox to be able to
  address a phone number with no account behind it — a real feature, not a refactor, and its own change.
- 2026-09-21 The remaining items, done in one pass.
  BOOKING REJECTION — and there was no flow to build. The task list assumed a rejection needed inventing;
  the investigation says otherwise. A `Requested` booking is already cancellable, and the cancel endpoint
  already infers the actor from the authenticated caller, so a salon declining a request is a transition the
  domain performs today. The ONLY thing missing was that the customer was told the wrong thing about it.
  The distinction is the STARTING state, not the destination: both land on Cancelled, but a request the
  salon never accepted is a rejection (the customer never had an appointment) while a confirmed booking
  called off is a cancellation (they did, and it was taken away). Captured before `Cancel()` mutates it.
  4 tests; the two rejection ones were red, the two control ones green, which is what says the condition
  discriminates rather than the code merely changing.
  STAFF ADDED / REMOVED — wired, and re-pointed at the MEMBER. The catalogue had both as Provider audience
  with third-person copy ("X was added to Y"), which only makes sense read by the salon; but the owner
  performs the change and already knows, and for a removal this is the only notice the member gets that
  their access has ended. Audience is now StaffMember and the copy is second person — a message about you,
  written about you in the third person, reads as a leak from somebody else's inbox.
  A member with NO account is skipped, and cannot be anything else: the outbox is keyed by user id and a
  salon may add someone with no app account at all. Same constraint that keeps InvitationSent out.
  Self-removal is skipped too — they know.
  INVOICE GENERATED — REMOVED, and this corrects something I told the user. I listed it as "flow exists,
  ordinary wiring". It does not: there is NO invoice code anywhere in the product, only a seeded template
  and the code itself. Same category as the six removed earlier today, so the same treatment.
  Test-arrange notes, both cost a cycle: a GUID's hex is not a phone number (digits only, and `Random` is
  banned here so the digits come from a GUID's hash); and read the new membership's id from the database
  rather than the response body — the endpoint's field names are not what an addressing test is about.
  651 integration tests pass; FAST green.
