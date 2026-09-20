Status: PLANNED
Verify: FAST

<!-- Set the first line to `Status: ACTIVE` to start the implementation loop. Note that doing so makes this
     change the repo's active change, which gates the Stop hook for EVERY session sharing this checkout —
     the hook picks the most recently modified ACTIVE tasks.md with no notion of ownership. Check
     ListAgents first. -->

Change: notification-system. Proposal, design and specs are complete and validated.

Three product decisions are deliberately unresolved and marked `[?] DECISION:` below. None of them blocks
the structural work — each is a row's timing, not a shape.

## 1. Outbox — the mechanism everything else rides on

- [ ] 1.1 `NotificationOutboxEntry` entity + EF configuration: event code, recipient, subject entity, copy
      parameters (JSON), scheduled-for, claim token, attempt count, state. Migration.
- [ ] 1.2 Claim-and-process semantics: claim is atomic, a claimed row is invisible to a second sweep, a
      failed row returns to pending, an exhausted row is dead-lettered. Unit tests for the state machine.
- [ ] 1.3 `INotificationRaiser.RaiseAsync(code, recipient, subject, parameters, scheduledFor?)` writing an
      outbox row on the caller's DbContext so it joins the caller's transaction. Test that a rolled-back
      business write leaves no intent.
- [ ] 1.4 Sweep job: claim due rows, resolve via catalogue, build `Notification`, dispatch, mark processed.
      Integration test proving exactly-once under two concurrent sweeps.
- [ ] 1.5 Wire the sweep into the host alongside `ProcessScheduledNotificationsJob`; confirm both run and do
      not contend for the same rows.

## 2. Catalogue

- [ ] 2.1 `NotificationEventCode` as a plain (non-flags) enum covering the 37 catalogued notifications.
      Source: `openspec/changes/notification-system/` discovery + design.md.
- [ ] 2.2 `NotificationEventCatalog` descriptor table: audience, channels, criticality, suppressibility,
      destination kind. One entry per code, no defaults.
- [ ] 2.3 Catalogue self-validation test: every code has an entry; no non-critical entry names SMS; the
      critical set matches the documented product decision exactly.
- [ ] 2.4 Wire `NotificationSuppressionPolicy` to read criticality from the catalogue instead of its own
      `HashSet`, keeping today's behaviour identical. Characterisation test first.

## 3. Copy — Persian, Jalali, salon-local

- [ ] 3.1 Extend `BookingSmsText` (landed by walk-in-customer-name-sms) into the general copy seam, or a
      sibling that shares its Jalali/wall-clock rendering. Do not duplicate that logic.
- [ ] 3.2 Move booking + payment copy out of the ten dead handlers into templates keyed by event code.
      Seed them. Render via the existing `ITemplateEngine`.
- [ ] 3.3 Unit tests per template: Persian text, Jalali date, salon wall-clock, parameter substitution,
      missing-parameter behaviour.
- [ ] 3.4 `[?] DECISION:` reminder offsets. T-24h and T-2h (customer) and T-30m (provider) are assumptions.
      The T-2h one is an SMS, so this sets recurring cost. Needs product sign-off before 5.1 ships.

## 4. Push — real delivery and a registry

- [ ] 4.1 `DeviceToken` entity + configuration + migration: user, token, platform, last-seen, revoked-at.
      Unique on token.
- [ ] 4.2 Register / refresh / revoke endpoints, scoped to the authenticated caller. Re-registering an
      existing token refreshes; a token held by another user is reassigned. Tests for all three.
- [ ] 4.3 Replace `FirebasePushNotificationService` with real FirebaseAdmin delivery. **Delete the fabricated
      success path** — an unimplemented or unconfigured channel reports skipped, never delivered.
- [ ] 4.4 Fan out to every live token; one device failing does not block the others; no-device is a skip that
      does not consume the retry budget. Gateway-rejected tokens are pruned. Tests with a faked gateway.
- [ ] 4.5 FCM credentials per environment, documented in the deployment runbook. Absent credentials must
      degrade to skipped, not to fake success.

## 5. Reminders

- [ ] 5.1 Schedule reminders on booking confirmation via the outbox with a future scheduled-for. Elapsed
      offsets are skipped, not fired immediately.
- [ ] 5.2 Reschedule on booking reschedule; cancel on cancel / complete / no-show. Integration tests for
      each transition.
- [ ] 5.3 Exactly-once under repeated sweeps and repeated confirmation. Integration test.

## 6. Inbox

- [ ] 6.1 Per-recipient read-state on the notification read model + migration. Mark-read is idempotent.
- [ ] 6.2 Endpoints: list (paged, newest first), unread count, mark-read, mark-all-read — all scoped to the
      caller.
- [ ] 6.3 **Ownership fix**: `GetDeliveryStatus` takes a notificationId with no ownership check today. Scope
      it to the caller and add a test proving another user's notification is refused.
- [ ] 6.4 Destination resolver: recompute tap targets for a page on read, batched; a deleted or
      no-longer-owned target returns non-actionable while the text stays as written. Tests for both.
- [ ] 6.5 Persist in-app notifications so an offline recipient sees them on next read; keep the SignalR push
      as a live hint, not the record.

## 7. Provider and customer coverage

- [ ] 7.1 Raise from the booking command handlers: requested (→ provider), confirmed, rescheduled, cancelled
      **distinguishing the actor**, completed, no-show. Replaces the dead handlers.
- [ ] 7.2 Raise from the payment/payout handlers: payment taken, payment failed, refund, payout completed,
      payout failed/on-hold.
- [ ] 7.3 Raise for membership and verification: invitation accepted, join request approved, staff assigned
      to a booking, provider verification status changed, provider activated/deactivated.
- [ ] 7.4 Delete the ten dead notification event handlers once their coverage is replaced, and record in
      FOLLOW-UPS that ServiceCatalog domain-event dispatch remains broken (see design.md) as its own change.
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
