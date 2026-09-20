Status: PLANNED
Verify: FAST

<!-- Deliberately NOT `Status: ACTIVE`. The Stop hook gates the whole shared checkout on the most recently
     modified ACTIVE tasks.md, and `notification-system` currently holds that role with its owner's knowledge.
     Flip this to ACTIVE only when starting implementation AND the other change is done or parked — otherwise
     this change silently steals the gate from a session mid-loop. -->

Change: provider-reviews-and-ratings. Proposal, specs and design are complete and validated, then hardened
against a nine-lens adversarial review of the plan on 2026-09-21 (17 findings, each survived two independent
verifiers). Line-level findings are folded into the tasks below rather than kept as a separate list.

Test-first per `AGENTS.md`: within each slice the test task comes before the implementation task it
describes. Run `scripts/verify.ps1 -Tier fast` after each task; `-Tier full` before closing.

Two open product questions are marked `[?] DECISION:` below, and one item (7.2) is a blocking handoff to
the session that owns `notification-system`. None of the three blocks structural work.

## 1. Domain: rating dimensions

- [ ] 1.1 Unit tests for the rating model: overall required; each of the four dimensions independently
      omittable; every rating within 1.0–5.0 on 0.5 increments; out-of-range and off-increment rejected by
      name; overall never recomputed from dimensions.
- [ ] 1.2 Add `CleanlinessRating`, `SkillRating`, `PunctualityRating`, `ConductRating` as nullable decimals on
      `Review`; extend `Review.Create` and validation. Keep `RatingValue` as the customer's own overall.
- [ ] 1.3 Unit tests for the edit window: author edits at day 2 succeed, day 8 rejected, non-author rejected,
      edit of a Rejected review refused, edit of a Hidden review refused, and an edit from Published returns
      the review to pending.
- [ ] 1.4 `Review.EditByAuthor(...)` enforcing the 7-day window. Consider a maximum-edits-per-review
      invariant: an edit unpublishes and forces a full per-provider recompute, and the trigger is an
      ordinary customer, repeatable for the whole window.

## 2. Domain: moderation state

- [ ] 2.1 Unit tests proving `ModerationStatus` and `IsVerified` are independent — hiding a verified review
      leaves it verified, and the verified count never counts a hidden review as unverified.
- [ ] 2.2 `ModerationStatus` enum + `ModeratedAt`/`ModeratedBy`/`ModerationReason` on `Review`; transitions
      `Publish`/`Reject`/`Hide` with their invariants. Do not touch `IsVerified`, `Verify()` or `Unverify()`.
- [ ] 2.3 Unit tests for the reply lifecycle: exactly one reply; edit and remove; reply moderation independent
      of the review's own status; a rejected reply leaves the review published.
- [ ] 2.4 `ReplyModerationStatus` and reply transitions on `Review`, reusing the existing
      `AddProviderResponse`/`UpdateProviderResponse`/`RemoveProviderResponse` methods.
- [ ] 2.5 Domain events `ReviewPublishedEvent`, `ReviewUnpublishedEvent` raised on the transitions that change
      public visibility, following the `RaiseDomainEvent` pattern in `BookingAggregate`. These are for
      notifications and auditing — they are NOT the recompute trigger (see 4.4).
- [ ] 2.6 Guard `Review.EditByAuthor(...)` on moderation state: editable only from Pending or Published;
      Rejected and Hidden refuse the edit. Without this, the 7-day window is a route back to publication
      that moderation already refused, and a way to undo an administrator's hide.
- [ ] 2.7 `Review.Restore()` — Hidden back to Published, votes and counts untouched. Rejected cannot be
      restored. Unit tests first.
- [ ] 2.8 Editing a review that carries a published reply returns the reply to pending with it, so published
      provider words are never displayed under text the provider never saw. Unit test first.

## 3. Persistence and migration

- [ ] 3.1 RED: standalone migration test. Does NOT use the shared host or its collections — that fixture
      migrates a fresh empty container, so a backfill there touches zero rows and proves nothing. Start its
      own Postgres container, build a `ServiceCatalogDbContext`, migrate to the current head via `IMigrator`,
      insert legacy `Review` rows over raw SQL at that schema version (one carrying a provider reply,
      counters of 7/2), migrate to the new head, then assert: reviews and replies are `Published`, counters
      survive as the legacy baseline, `ReviewVotes` is empty. This is the sole coverage for the three
      "WHEN the migration runs" scenarios.
- [ ] 3.2 Extend `ReviewConfiguration` for the dimension, moderation and reply-moderation columns; index
      `(ProviderId, ModerationStatus)` for the aggregate query and the public listing.
- [ ] 3.3 `ReviewVote` entity + configuration: own table, unique index `(ReviewId, UserId)`, FK to `Reviews`.
      Not an owned collection — see design D4 and the ValueGeneratedNever hazard in D1.
- [ ] 3.4 `ReviewReport` entity + configuration: unique index `(ReviewId, ReportedByUserId)` so a user cannot
      report twice; carries the reason and the reporter.
- [ ] 3.5 `ProviderRatingSummary` entity + configuration keyed by `ProviderId`, holding the four dimension
      averages and their counts. Add `PublishedReviewCount` to `Provider`, and a single domain method
      `Provider.SetRatingAggregates(decimal average, int publishedCount)` setting both together —
      `AverageRating` is `internal set` with no mutator and the Domain project declares no
      `InternalsVisibleTo`, so there is no writable path at all today. One method also makes 9.2's
      "never read one without the other" mechanical rather than a hand audit.
- [ ] 3.6 Rename the CLR properties to `LegacyHelpfulCount`/`LegacyNotHelpfulCount` and pin them with
      `.HasColumnName("HelpfulCount")` / `.HasColumnName("NotHelpfulCount")` — the PHYSICAL columns do not
      change (design D5); precedent is the existing `Id` → `"ReviewId"` pin in the same file. Add
      `HelpfulVoteCount`/`NotHelpfulVoteCount`. Delete `Review.MarkAsHelpful()`/`MarkAsNotHelpful()`: with no
      physical rename to enforce it, "never written again" holds only because no writer remains.
- [ ] 3.7 Migration: additive only, no renames, no drops. Every new NOT NULL column on an existing table
      (`ModerationStatus`, `ReplyModerationStatus`, `HelpfulVoteCount`, `NotHelpfulVoteCount`,
      `Providers.PublishedReviewCount`) carries a database-level DEFAULT, so the previous image's INSERTs —
      which omit them — do not fail 23502. Backfill every existing review and every existing reply to
      `Published`. Idempotent. Turns 3.1 green.

## 4. Rating aggregates

- [ ] 4.1 RED: unit tests for the aggregation rules — dimension averaged only over reviews that rated it;
      pending, rejected and hidden reviews excluded; a provider with no published reviews reported as
      unrated, not zero.
- [ ] 4.2 Recompute service: one grouped query per provider calling `Provider.SetRatingAggregates` and
      writing `ProviderRatingSummary`. Recompute, never increment (design D6).
- [ ] 4.3 RED: integration test driving the real moderation endpoints end-to-end — publish → average moves;
      hide → recomputes without it; restore → recomputes with it; edit a published review → it leaves the
      aggregates until re-approved. Read `Provider.AverageRating`/`PublishedReviewCount` in a FRESH scope
      after the response, never by raising the event against an already-committed row.
- [ ] 4.4 Call the recompute from each moderation command handler, inline, after the state change and before
      commit, on the command's own `DbContext`. NOT from a domain event handler: `EfCoreUnitOfWork`
      dispatches domain events before it saves, and `SimpleDomainEventDispatcher` opens a fresh DI scope per
      event, so the handler would run on another connection outside the transaction against a row that has
      not been written — leaving every aggregate one action stale. See design D6.
- [ ] 4.5 RED: integration tests over `SearchProvidersQueryHandler` — published-review average and count
      returned; rating null when the count is 0; unrated providers banded after all rated providers in BOTH
      sort directions. Named against the `customer-discovery-journey` scenarios.
- [ ] 4.6 Project `PublishedReviewCount` through `ProviderSearchItem` — its property is commented out today,
      so `ProviderSearchResponse.TotalReviews` ships a permanent 0, which collapses the Flutter
      `ProviderRating.hasRating(rating, reviewCount)` guard to `rating > 0`, i.e. exactly D8's stored zero.
      Wire it through the controller mapping and the `ProviderLocationItem`/`ProviderSummaryItem` paths, and
      change both the `"rating"` branch and the no-coordinates `"distance"` fallback to band the zero-count
      set. This is server-side work: every client only passes `sortBy`.
- [ ] 4.7 One-off recompute across all providers, runnable after the migration, so `AverageRating` becomes
      true for the first time. Re-runnable, with its own test.

## 5. Commands and queries

- [ ] 5.1 RED: integration tests for create — eligibility (non-owner forbidden, non-completed conflict,
      duplicate conflict, unauthenticated rejected) and that a new review lands in `Pending`.
- [ ] 5.2 Extend `CreateReviewCommand`/`Handler` for dimensions and for entering `Pending`, keeping the
      existing eligibility checks.
- [ ] 5.3 RED: integration tests for edit — author inside the window, author outside it, non-author, edit of
      a Rejected review refused, edit of a Hidden review refused, and that an edit returns the review and
      any published reply to pending.
- [ ] 5.4 `EditReviewCommand` + handler.
- [ ] 5.5 RED: integration tests for reply authorisation — owning provider succeeds; a different provider
      forbidden; the review's author forbidden; an administrator forbidden (design D11); a second reply
      conflicts.
- [ ] 5.6 `ReplyToReviewCommand`, `EditReplyCommand`, `RemoveReplyCommand` + handlers, with the ownership
      check shaped like `BookingsController.CanManageProvider`.
- [ ] 5.7 RED: integration tests for voting — fifty repeats from one user leave one vote; changing a vote
      never double-counts; author cannot vote on their own review; voting on a pending review rejected;
      withdrawing never takes a count below the legacy baseline; concurrent votes from one user do not both
      insert; and the DERIVED ratio and considered-helpful flag reflect legacy + live, not the frozen baseline.
- [ ] 5.8 `CastReviewVoteCommand` replacing `MarkReviewHelpfulCommand`: insert/replace/toggle-off against
      `ReviewVotes`, with the denormalised counters updated in the same transaction. Record whether the
      counter write is an atomic `ExecuteUpdate` or a re-derivation — the hazard is a lost update, and
      `AggregateRoot.Version` does not protect it because it is bumped only inside `RaiseDomainEvent`.
- [ ] 5.9 Redefine `GetHelpfulnessRatio()` and `IsConsideredHelpful()` over `(Legacy + Live)`, and change the
      `sortBy="helpful"` ORDER BY to the same sum (persisted computed column if it must stay indexable).
      Without this every review created after the migration is frozen at 0/0 and can never be "considered
      helpful" however many live votes it collects, and the helpful sort freezes at the deploy-day ordering.
      Dropping both fields from the response contract is an acceptable alternative; silence is not.
- [ ] 5.10 RED: integration test that a duplicate report from the same user is rejected, and that a reported
      published review is returned to an administrator with its report count, its reasons and its reporters.
- [ ] 5.11 `ReportReviewCommand` + handler, and the reported-review read side. Decided here rather than at
      implementation time: reported reviews are an explicit filter on the moderation queue query, since the
      queue already owns "hide an already-published review" as one of its actions.
- [ ] 5.12 RED: integration tests for the moderation commands and the queue — approve, reject, hide, restore,
      reply approve/reject, queue ordered oldest first, hidden reviews findable on their own filter. The
      admin-only test must NOT use the shared `TestUser.Admin`, which carries all three role spellings and
      would therefore pass against any of them — that is exactly how the 2026-09-19 incident escaped. Follow
      `AdminRoleNameTests`: assert with a token carrying only `"Admin"` and again with only `"Administrator"`.
- [ ] 5.13 Moderation commands (`ApproveReviewCommand`, `RejectReviewCommand`, `HideReviewCommand`,
      `RestoreReviewCommand`, `ApproveReplyCommand`, `RejectReplyCommand`) and `GetModerationQueueQuery`
      with its pending / reported / hidden filters.
- [ ] 5.14 RED: integration tests for all three listings — the anonymous public listing returns published
      only (given a provider holding pending, rejected, hidden and published) AND returns nothing extra to
      the owning provider's own token; the author's own listing returns every state with reasons and is
      scoped to the caller; the owner-scoped provider listing returns every state and is forbidden to other
      providers.
- [ ] 5.15 `GetMyReviewsQuery` — wrapping the existing `GetByCustomerIdAsync`, which is implemented with zero
      callers anywhere in `src/` or `tests/` — and the owner-scoped provider listing, either its own query or
      an audience parameter on `GetProviderReviewsQuery`. "Published only" is scoped to the public caller,
      never stated absolutely, or it removes the very rows these two surfaces exist to show.
- [ ] 5.16 RED then green: `ReviewStatistics` computed over published reviews only. Today the read repository
      loads every review for the provider and builds the average, the whole star distribution and the
      verified count off that unfiltered set, and the block ships on the public listing — so filtering the
      list without filtering the statistics leaves the public payload reporting pending, rejected and hidden
      reviews, contradicting the moderation spec directly.
- [ ] 5.17 Extend `GetProviderReviewsQuery`/`Handler`: published-only for the public caller, dimension
      statistics, and the calling user's own current vote per review.

## 6. API

- [ ] 6.1 RED: controller tests in `Booksy.ServiceCatalog.Api.UnitTests` for every endpoint below — including
      that the identity is read from `ClaimTypes.NameIdentifier` first, since production tokens carry
      `nameidentifier` and not `sub`/`userId`, and that the vote endpoint rejects an anonymous caller.
- [ ] 6.2 `ReviewsController`: dimensions on create; edit; reply add/edit/remove; report; `GET /reviews/me`
      for the author's own list; and the owner-scoped provider listing.
- [ ] 6.3 Change `PUT /reviews/{id}/helpful` to authenticated and idempotent-per-user. **BREAKING** — remove
      `[AllowAnonymous]`.
- [ ] 6.4 `GET /reviews/providers/{id}` returns published only to the public caller, with dimension
      statistics and the caller's own vote. **BREAKING** for any client relying on unpublished rows.
- [ ] 6.5 Moderation endpoints under `[Authorize(Policy = "AdminOnly")]`, as `ProvidersController` uses —
      NOT a raw `Roles = "Admin,SysAdmin"` list. The seeded administrator carries only `"Administrator"`, and
      a narrow list reproduces the 2026-09-19 403 incident recorded in `PolicyAuthorizationExtensions`; the
      vocabulary drift itself is FOLLOW-UPS #46. Name the reported-review endpoint explicitly so 8.4 has a
      target to call.
- [ ] 6.6 A named rate-limit policy for EACH new write path — vote, report, edit, reply add/edit/remove and
      each moderation action — with the tightest ceiling on edit, which is a customer-triggered unpublish
      plus a full per-provider GROUP BY. Each name must ALSO be added to `RateLimitingOptions.Defaults`:
      registration builds exactly that table, and an `[EnableRateLimiting]` naming an unregistered policy
      throws at request time — including in the test host, where `Enabled=false` still registers each name
      as a no-op limiter. The failure mode otherwise is a 500 on first call, not a missing limit.
- [ ] 6.7 Update `API_ENDPOINTS.md` and `DTO_MAPPING.md`.

## 7. Notifications

- [ ] 7.1 RED: integration test at the INBOX boundary — complete a booking, submit the review, assert
      `GET /notifications/inbox` returns no review-request item for that booking. Asserting on the outbox
      table alone passes while the user-visible duplicate survives.
- [x] 7.2 CLOSED 2026-09-21 by the `notification-system` session. `BookingCompletedNotificationHandler`
      scheduled a SECOND review request on every completion — a legacy `Notification` row with an English
      HTML body — outside the outbox that withdrawal can reach. It is now deleted (their task 7.4), with no
      remaining references, and the inbox filters to Sent/Delivered/Read with a test asserting the list and
      the unread badge agree. Recorded rather than dropped because it changes 7.3: there is no second store
      for review requests any more. Blast radius, for the record: that row was visible in the inbox but
      never sent — `ScheduledNotificationService` is registered in `AddNotificationBackgroundServices`,
      which nothing calls (FOLLOW-UPS #67).
- [ ] 7.3 Call `INotificationRaiser.WithdrawPendingForSubjectAsync("Booking", bookingId)` from the
      create-review handler in the same unit of work. The outbox is now the only store for review requests
      (7.2), so this is sufficient — re-verify that is still true at wiring time rather than inheriting it.
      Withdrawal fires on SUBMISSION, not publication, or a review held in the moderation queue still gets
      the 3-day "you have not reviewed yet" reminder.
- [ ] 7.4 Agree the two new `NotificationEventCode` entries with the `notification-system` owner — that file
      is theirs, and all the edits land together or FAST stays red. FOUR surfaces throw on an unhandled
      member, so a bare enum addition is red on its own: the catalogue test iterates every enum value, and
      `Describe`, `PreferenceCategoryFor` and `PersianNotificationCopyWriter` each carry their own
      `_ => throw`. Required per code: the catalogue descriptor (audience/channels/criticality), the
      preference-category mapping (`NotificationType.NewReview` and `ReviewResponse` already exist), and
      **Persian copy**. Both are in-app/push, non-critical, suppressible; neither may name SMS, which is
      reserved for critical notifications and fails the catalogue self-validation test.
- [x] 7.5 DECIDED 2026-09-21 with the `notification-system` owner: both notifications point at the
      **booking**, not at a review. The notification row already carries `BookingId` so no entity change is
      needed; `NotificationDestinationResolver` already checks booking ownership, so a tap cannot open
      someone else's; and a review is only ever reachable in the context of its booking anyway. A
      review-shaped destination would have meant an entity change plus a resolver case plus a new ownership
      rule, to land somewhere the booking screen already reaches.
- [ ] 7.6 Raise the provider notification when a review is published — distinguishing a re-publication after
      an edit from a first publication, or the provider cannot tell a changed review from a new one — and
      the customer notification when a provider reply is published. Tested through the publishing path.
- [ ] 7.7 [?] DECISION: is a rejected review's author told, and told why? The `notification-system` owner has
      settled their task 7.6 as *never re-ask a rejected author*, on the reasoning that the notification
      system cannot see WHY a review was rejected. That silence is defensible only if the rejection is
      communicated here. If the answer is "say nothing", the platform silently swallows customer reviews.
      Needs a product call; implement nothing until it lands. Note the author can already see the state and
      the reason on their own review list (5.15) — this decision is only about pushing a notification.

## 8. Clients

Per-client slices, each with its covering tests named before the implementation task.

- [ ] 8.1 RED: Vue component tests for `booksy-frontend` — the dimension disclosure in the write/edit form,
      vote state rendered from the caller's own vote, provider reply rendering, and the "no reviews yet"
      treatment. FULL does not run Vue unit tests (see 9.4), so these need their own invocation.
- [ ] 8.2 `booksy-frontend` reviews surface. FIRST delete the fabrication: `ProfileReviews.vue` — the reviews
      tab mounted on the provider page — holds six hardcoded Persian reviews, a hardcoded rating
      distribution, the constants 4.8 and 127, and a `console.log` stub for liking. It has never been wired
      to the API. This change's whole premise is that ratings become honest, so shipping it while that
      component invents them is not an option. Wire it to the real provider-reviews endpoint and replace the
      like stub with the real vote call.
- [ ] 8.3 `booksy-frontend` repoint. `customer.service.ts` / `customer.store.ts` GET and PATCH
      `/api/v1/customers/{id}/reviews[/{id}]` on `userManagementClient` — routes `CustomersController` does
      not define at all. Move BOTH the list load and the edit onto the ServiceCatalog reviews routes. The one
      file that already targets those routes, `reviews.service.ts`, is imported by nothing: wire it in or
      delete it. Also fix `ProviderDetailView.vue`, which discards `rating`, `reviewCount` and `reviews`
      after the API call, and the landing-page `|| 5.0` fallback in `FeaturedProviders.vue`.
- [ ] 8.4 `booksy-admin`: the moderation queue — list, approve, reject with reason, hide with reason, restore,
      and the reported-reviews view, against the endpoints named in 6.5. New surface.
- [ ] 8.5 RED: Flutter widget/bloc tests for `booksy-customer-app` — dimensions in the write dialog, reply and
      breakdown rendering, the vote control, and the "no reviews yet" treatment in `provider_rating.dart` and
      the provider cards.
- [ ] 8.6 `booksy-customer-app`: extend `features/reviews/` and the rating widgets accordingly. Its
      `hasRating(rating, reviewCount)` guard currently collapses to `rating > 0` because the API's review
      count is a constant zero — 4.6 fixes that server side, and this task stops the client compensating.
- [ ] 8.7 RED: Flutter widget tests for `booksy-provider-app`, including a themed button inside a `Row`
      against the real `AppTheme` — its buttons are infinite-width via `Size.fromHeight` and blank the page
      otherwise, which only a real-theme widget test catches.
- [ ] 8.8 `booksy-provider-app`: net-new reviews feature — read reviews of your business against the
      owner-scoped listing from 5.15 (not the public one, which by then returns published rows only), see
      dimension breakdowns, reply.
- [ ] 8.9 [?] DECISION: should an overall rating of 3 or below prompt for dimensions? That is where dimension
      data is most valuable and least often volunteered, at a completion-rate cost. UX call; default to not
      prompting until it lands.

## 9. Close out

- [ ] 9.1 Audit all four clients for anonymous calls to the vote endpoint before deploy (6.3 is breaking).
- [ ] 9.2 Confirm no consumer reads `Provider.AverageRating` without also reading `PublishedReviewCount` —
      design D8 leaves 0.0 stored for unrated providers, and this is the design's weakest seam. 3.5's single
      `SetRatingAggregates` method makes the write side mechanical; this task covers the read side.
- [ ] 9.3 Map every spec scenario across the five spec files to the test that covers it, and name any with no
      coverage. Verify being green is not the same as being covered.
- [ ] 9.4 `scripts/verify.ps1 -Tier full` green. Note FULL runs `flutter analyze`/`flutter test` and the Vue
      type-check and lint, but NOT Vue unit tests — 8.1's tests need their own invocation or they never run.
- [ ] 9.5 Update `openspec/specs/` via the sync/archive flow. Record the migration steps AND the rollback in
      `docs/DEPLOYMENT_RUNBOOK.md`, which has no rollback section today: production compose pins `:latest`,
      so rolling back means editing it to the `-api:${sha_short}` tag and re-running `up -d`. Include the
      one-off recompute (4.7) and add a post-deploy smoke assertion on
      `GET /api/v1/reviews/providers/{id}` to the deploy job, shaped like the existing
      `tests/e2e/keystone-booking-flow.sh` gate — the compose healthcheck is `curl /health` and can never
      see a broken review read.
