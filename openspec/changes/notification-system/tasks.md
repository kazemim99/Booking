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
- [ ] 3.2 Move booking + payment copy out of the existing notification event handlers into templates keyed
      by event code. Seed them. Render via the existing `ITemplateEngine`.
- [ ] 3.3 Unit tests per template: Persian text, Jalali date, salon wall-clock, parameter substitution,
      missing-parameter behaviour.
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

- [ ] 7.1 Raise from the booking command handlers: requested (→ provider), confirmed, rescheduled, cancelled
      **distinguishing the actor**, completed, no-show. Supersedes the existing event handlers path by path.
- [ ] 7.2 Raise from the payment/payout handlers: payment taken, payment failed, refund, payout completed,
      payout failed/on-hold.
- [ ] 7.3 Raise for membership and verification: invitation accepted, join request approved, staff assigned
      to a booking, provider verification status changed, provider activated/deactivated.
- [ ] 7.4 Remove each superseded notification event handler only after its outbox coverage is in place and
      tested, so no notification has a window with neither. FOLLOW-UPS #66 (dispatch ordering) stays open as
      its own change.
- [ ] 7.5 `[?] DECISION:` daily provider digest — send it at all, and at what salon-local hour?
- [ ] 7.6 `[?] DECISION:` review requests — how long after completion, and how many times?

## 8. `NotificationType` repair (BREAKING)

- [ ] 8.1 Characterisation tests pinning today's preference behaviour, including the known false positives.
- [ ] 8.2 Split the type: `NotificationEventCode` (identity, already in 2.1) vs a small, genuinely-flags
      preference category enum. Remove the sequential members from the flags type.
- [ ] 8.3 Data migration rewriting persisted preference masks; tested in both directions on a seeded database.
- [ ] 8.4 Replace every `HasFlag`/bitwise membership test on the old type with exact membership.

## 9. Verification

- [ ] 9.1 `scripts/verify.ps1 -Tier fast` green.
- [ ] 9.2 `scripts/verify.ps1 -Tier full` green (Testcontainers; coordinate build time with peer sessions —
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
