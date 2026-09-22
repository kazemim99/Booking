Status: DONE
Verify: FULL

<!-- ACTIVE since 2026-09-22. Flipped only once the condition this comment used to state held:
     `notification-system` is archived and `add-notification-clients` is STOPPED(blocked), so no other change
     was ACTIVE and no peer loop is being displaced. The Stop hook gates the whole shared checkout on the most
     recently modified ACTIVE tasks.md. -->

Change: provider-reviews-and-ratings. Proposal, specs and design are complete and validated, then hardened
against a nine-lens adversarial review of the plan on 2026-09-21 (17 findings, each survived two independent
verifiers). Line-level findings are folded into the tasks below rather than kept as a separate list.

Test-first per `AGENTS.md`: within each slice the test task comes before the implementation task it
describes. Run `scripts/verify.ps1 -Tier fast` after each task; `-Tier full` before closing.

Two open product questions are marked `[?] DECISION:` below, and one item (7.2) is a blocking handoff to
the session that owns `notification-system`. None of the three blocks structural work.

## 1. Domain: rating dimensions

- [x] 1.1 Unit tests for the rating model: overall required; each of the four dimensions independently
      omittable; every rating within 1.0–5.0 on 0.5 increments; out-of-range and off-increment rejected by
      name; overall never recomputed from dimensions.
- [x] 1.2 Add `CleanlinessRating`, `SkillRating`, `PunctualityRating`, `ConductRating` as nullable decimals on
      `Review`; extend `Review.Create` and validation. Keep `RatingValue` as the customer's own overall.
- [x] 1.3 Unit tests for the edit window: author edits at day 2 succeed, day 8 rejected, non-author rejected,
      edit of a Rejected review refused, edit of a Hidden review refused, and an edit from Published returns
      the review to pending.
- [x] 1.4 `Review.EditByAuthor(...)` enforcing the 7-day window. Consider a maximum-edits-per-review
      invariant: an edit unpublishes and forces a full per-provider recompute, and the trigger is an
      ordinary customer, repeatable for the whole window.

## 2. Domain: moderation state

- [x] 2.1 Unit tests proving `ModerationStatus` and `IsVerified` are independent — hiding a verified review
      leaves it verified, and the verified count never counts a hidden review as unverified.
- [x] 2.2 `ModerationStatus` enum + `ModeratedAt`/`ModeratedBy`/`ModerationReason` on `Review`; transitions
      `Publish`/`Reject`/`Hide` with their invariants. Do not touch `IsVerified`, `Verify()` or `Unverify()`.
- [x] 2.3 Unit tests for the reply lifecycle: exactly one reply; edit and remove; reply moderation independent
      of the review's own status; a rejected reply leaves the review published.
- [x] 2.4 `ReplyModerationStatus` and reply transitions on `Review`, reusing the existing
      `AddProviderResponse`/`UpdateProviderResponse`/`RemoveProviderResponse` methods.
- [x] 2.5 Domain events `ReviewPublishedEvent`, `ReviewUnpublishedEvent` raised on the transitions that change
      public visibility, following the `RaiseDomainEvent` pattern in `BookingAggregate`. These are for
      notifications and auditing — they are NOT the recompute trigger (see 4.4).
- [x] 2.6 Guard `Review.EditByAuthor(...)` on moderation state: editable only from Pending or Published;
      Rejected and Hidden refuse the edit. Without this, the 7-day window is a route back to publication
      that moderation already refused, and a way to undo an administrator's hide.
- [x] 2.7 `Review.Restore()` — Hidden back to Published, votes and counts untouched. Rejected cannot be
      restored. Unit tests first.
- [x] 2.8 Editing a review that carries a published reply returns the reply to pending with it, so published
      provider words are never displayed under text the provider never saw. Unit test first.

## 3. Persistence and migration

- [x] 3.1 RED: standalone migration test. Does NOT use the shared host or its collections — that fixture
      migrates a fresh empty container, so a backfill there touches zero rows and proves nothing. Start its
      own Postgres container, build a `ServiceCatalogDbContext`, migrate to the current head via `IMigrator`,
      insert legacy `Review` rows over raw SQL at that schema version (one carrying a provider reply,
      counters of 7/2), migrate to the new head, then assert: reviews and replies are `Published`, counters
      survive as the legacy baseline, `ReviewVotes` is empty. This is the sole coverage for the three
      "WHEN the migration runs" scenarios.
- [x] 3.2 Extend `ReviewConfiguration` for the dimension, moderation and reply-moderation columns; index
      `(ProviderId, ModerationStatus)` for the aggregate query and the public listing.
- [x] 3.3 `ReviewVote` entity + configuration: own table, unique index `(ReviewId, UserId)`, FK to `Reviews`.
      Not an owned collection — see design D4 and the ValueGeneratedNever hazard in D1.
- [x] 3.4 `ReviewReport` entity + configuration: unique index `(ReviewId, ReportedByUserId)` so a user cannot
      report twice; carries the reason and the reporter.
- [x] 3.5 `ProviderRatingSummary` entity + configuration keyed by `ProviderId`, holding the four dimension
      averages and their counts. Add `PublishedReviewCount` to `Provider`, and a single domain method
      `Provider.SetRatingAggregates(decimal average, int publishedCount)` setting both together —
      `AverageRating` is `internal set` with no mutator and the Domain project declares no
      `InternalsVisibleTo`, so there is no writable path at all today. One method also makes 9.2's
      "never read one without the other" mechanical rather than a hand audit.
- [x] 3.6 Rename the CLR properties to `LegacyHelpfulCount`/`LegacyNotHelpfulCount` and pin them with
      `.HasColumnName("HelpfulCount")` / `.HasColumnName("NotHelpfulCount")` — the PHYSICAL columns do not
      change (design D5); precedent is the existing `Id` → `"ReviewId"` pin in the same file. Add
      `HelpfulVoteCount`/`NotHelpfulVoteCount`. The deletion of `MarkAsHelpful()`/`MarkAsNotHelpful()` moved to
      5.8 (their one caller is the command 5.8 replaces); until then they write the LIVE tally, never the legacy.
- [x] 3.7 Migration: additive only, no renames, no drops. Every new NOT NULL column on an existing table
      (`ModerationStatus`, `ReplyModerationStatus`, `HelpfulVoteCount`, `NotHelpfulVoteCount`,
      `Providers.PublishedReviewCount`) carries a database-level DEFAULT, so the previous image's INSERTs —
      which omit them — do not fail 23502. Backfill every existing review and every existing reply to
      `Published`. Idempotent. Turns 3.1 green.

## 4. Rating aggregates

- [x] 4.1 RED: unit tests for the aggregation rules — dimension averaged only over reviews that rated it;
      pending, rejected and hidden reviews excluded; a provider with no published reviews reported as
      unrated, not zero.
- [x] 4.2 Recompute service: one grouped query per provider calling `Provider.SetRatingAggregates` and
      writing `ProviderRatingSummary`. Recompute, never increment (design D6).
- [x] 4.3 RED: integration test driving the real moderation endpoints end-to-end — publish → average moves;
      hide → recomputes without it; restore → recomputes with it; edit a published review → it leaves the
      aggregates until re-approved. Read `Provider.AverageRating`/`PublishedReviewCount` in a FRESH scope
      after the response, never by raising the event against an already-committed row.
- [x] 4.4 Call the recompute from each moderation command handler, inline, after the state change and before
      commit, on the command's own `DbContext`. NOT from a domain event handler: `EfCoreUnitOfWork`
      dispatches domain events before it saves, and `SimpleDomainEventDispatcher` opens a fresh DI scope per
      event, so the handler would run on another connection outside the transaction against a row that has
      not been written — leaving every aggregate one action stale. See design D6.
- [x] 4.5 RED: integration tests over `SearchProvidersQueryHandler` — published-review average and count
      returned; an unrated provider comes back with `totalReviews` 0 (and `averageRating` 0 — numeric, not null;
      see D8's revision); unrated providers banded after all rated providers in BOTH sort directions. Named against the `customer-discovery-journey` scenarios.
- [x] 4.6 Project `PublishedReviewCount` through `ProviderSearchItem` — its property is commented out today,
      so `ProviderSearchResponse.TotalReviews` ships a permanent 0, which collapses the Flutter
      `ProviderRating.hasRating(rating, reviewCount)` guard to `rating > 0`, i.e. exactly D8's stored zero.
      Wire it through the controller mapping and the `ProviderLocationItem`/`ProviderSummaryItem` paths, and
      change both the `"rating"` branch and the no-coordinates `"distance"` fallback to band the zero-count
      set. This is server-side work: every client only passes `sortBy`.
- [x] 4.7 One-off recompute across all providers, runnable after the migration, so `AverageRating` becomes
      true for the first time. Re-runnable, with its own test.

## 5. Commands and queries

- [x] 5.1 RED: integration tests for create — eligibility (non-owner forbidden, non-completed conflict,
      duplicate conflict, unauthenticated rejected) and that a new review lands in `Pending`.
- [x] 5.2 Extend `CreateReviewCommand`/`Handler` for dimensions and for entering `Pending`, keeping the
      existing eligibility checks.
- [x] 5.3 RED: integration tests for edit — author inside the window, author outside it, non-author, edit of
      a Rejected review refused, edit of a Hidden review refused, and that an edit returns the review and
      any published reply to pending.
- [x] 5.4 `EditReviewCommand` + handler.
- [x] 5.5 RED: integration tests for reply authorisation — owning provider succeeds; a different provider
      forbidden; the review's author forbidden; an administrator forbidden (design D11); a second reply
      conflicts.
- [x] 5.6 `ReplyToReviewCommand`, `EditReplyCommand`, `RemoveReplyCommand` + handlers, with the ownership
      check shaped like `BookingsController.CanManageProvider`.
- [x] 5.7 RED: integration tests for voting — fifty repeats from one user leave one vote; changing a vote
      never double-counts; author cannot vote on their own review; voting on a pending review rejected;
      withdrawing never takes a count below the legacy baseline; concurrent votes from one user do not both
      insert; and the DERIVED ratio and considered-helpful flag reflect legacy + live, not the frozen baseline.
- [x] 5.8 `CastReviewVoteCommand` replacing `MarkReviewHelpfulCommand`, and delete `Review.MarkAsHelpful()`/
      `MarkAsNotHelpful()` with it (moved here from 3.6): insert/replace/toggle-off against
      `ReviewVotes`, with the denormalised counters updated in the same transaction. Record whether the
      counter write is an atomic `ExecuteUpdate` or a re-derivation — the hazard is a lost update, and
      `AggregateRoot.Version` does not protect it because it is bumped only inside `RaiseDomainEvent`.
- [x] 5.9 Redefine `GetHelpfulnessRatio()` and `IsConsideredHelpful()` over `(Legacy + Live)`, and change the
      `sortBy="helpful"` ORDER BY to the same sum (persisted computed column if it must stay indexable).
      Without this every review created after the migration is frozen at 0/0 and can never be "considered
      helpful" however many live votes it collects, and the helpful sort freezes at the deploy-day ordering.
      Dropping both fields from the response contract is an acceptable alternative; silence is not.
- [x] 5.10 RED: integration test that a duplicate report from the same user is rejected, and that a reported
      published review is returned to an administrator with its report count, its reasons and its reporters.
- [x] 5.11 `ReportReviewCommand` + handler, and the reported-review read side. Decided here rather than at
      implementation time: reported reviews are an explicit filter on the moderation queue query, since the
      queue already owns "hide an already-published review" as one of its actions.
- [x] 5.12 RED: integration tests for the moderation commands and the queue — approve, reject, hide, restore,
      reply approve/reject, queue ordered oldest first, hidden reviews findable on their own filter. The
      admin-only test must NOT use the shared `TestUser.Admin`, which carries all three role spellings and
      would therefore pass against any of them — that is exactly how the 2026-09-19 incident escaped. Follow
      `AdminRoleNameTests`: assert with a token carrying only `"Admin"` and again with only `"Administrator"`.
- [x] 5.13 Moderation commands (`ApproveReviewCommand`, `RejectReviewCommand`, `HideReviewCommand`,
      `RestoreReviewCommand`, `ApproveReplyCommand`, `RejectReplyCommand`) and `GetModerationQueueQuery`
      with its pending / reported / hidden filters.
- [x] 5.14 RED: integration tests for all three listings — the anonymous public listing returns published
      only (given a provider holding pending, rejected, hidden and published) AND returns nothing extra to
      the owning provider's own token; the author's own listing returns every state with reasons and is
      scoped to the caller; the owner-scoped provider listing returns every state and is forbidden to other
      providers.
- [x] 5.15 `GetMyReviewsQuery` — wrapping the existing `GetByCustomerIdAsync`, which is implemented with zero
      callers anywhere in `src/` or `tests/` — and the owner-scoped provider listing, either its own query or
      an audience parameter on `GetProviderReviewsQuery`. "Published only" is scoped to the public caller,
      never stated absolutely, or it removes the very rows these two surfaces exist to show.
- [x] 5.16 RED then green: `ReviewStatistics` computed over published reviews only. Today the read repository
      loads every review for the provider and builds the average, the whole star distribution and the
      verified count off that unfiltered set, and the block ships on the public listing — so filtering the
      list without filtering the statistics leaves the public payload reporting pending, rejected and hidden
      reviews, contradicting the moderation spec directly.
- [x] 5.17 Extend `GetProviderReviewsQuery`/`Handler`: published-only for the public caller, dimension
      statistics, and the calling user's own current vote per review.

## 6. API

- [x] 6.1 RED: controller tests in `Booksy.ServiceCatalog.Api.UnitTests` for every endpoint below — including
      that the identity is read from `ClaimTypes.NameIdentifier` first, since production tokens carry
      `nameidentifier` and not `sub`/`userId`, and that the vote endpoint rejects an anonymous caller.
- [x] 6.2 `ReviewsController`: dimensions on create; edit; reply add/edit/remove; report; `GET /reviews/me`
      for the author's own list; and the owner-scoped provider listing.
- [x] 6.3 Change `PUT /reviews/{id}/helpful` to authenticated and idempotent-per-user. **BREAKING** — remove
      `[AllowAnonymous]`.
- [x] 6.4 `GET /reviews/providers/{id}` returns published only to the public caller, with dimension
      statistics and the caller's own vote. **BREAKING** for any client relying on unpublished rows.
- [x] 6.5 Moderation endpoints under `[Authorize(Policy = "AdminOnly")]`, as `ProvidersController` uses —
      NOT a raw `Roles = "Admin,SysAdmin"` list. The seeded administrator carries only `"Administrator"`, and
      a narrow list reproduces the 2026-09-19 403 incident recorded in `PolicyAuthorizationExtensions`; the
      vocabulary drift itself is FOLLOW-UPS #46. Name the reported-review endpoint explicitly so 8.4 has a
      target to call.
- [x] 6.6 A named rate-limit policy for EACH new write path — vote, report, edit, reply add/edit/remove and
      each moderation action — with the tightest ceiling on edit, which is a customer-triggered unpublish
      plus a full per-provider GROUP BY. Each name must ALSO be added to `RateLimitingOptions.Defaults`:
      registration builds exactly that table, and an `[EnableRateLimiting]` naming an unregistered policy
      throws at request time — including in the test host, where `Enabled=false` still registers each name
      as a no-op limiter. The failure mode otherwise is a 500 on first call, not a missing limit.
- [x] 6.7 Update `API_ENDPOINTS.md` and `DTO_MAPPING.md`.

## 7. Notifications

- [x] 7.1 RED: integration test at the INBOX boundary — complete a booking, submit the review, assert
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
- [x] 7.3 Call `INotificationRaiser.WithdrawPendingForSubjectAsync("Booking", bookingId)` from the
      create-review handler in the same unit of work. The outbox is now the only store for review requests
      (7.2), so this is sufficient — re-verify that is still true at wiring time rather than inheriting it.
      Withdrawal fires on SUBMISSION, not publication, or a review held in the moderation queue still gets
      the 3-day "you have not reviewed yet" reminder.
- [x] 7.4 Agree the two new `NotificationEventCode` entries with the `notification-system` owner — that file
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
- [x] 7.6 Raise the provider notification when a review is published — distinguishing a re-publication after
      an edit from a first publication, or the provider cannot tell a changed review from a new one — and
      the customer notification when a provider reply is published. Tested through the publishing path.
- [x] 7.7 DECIDED 2026-09-22 (user): the author IS told their review was rejected, and the notification carries the
      administrator's reason. New `NotificationEventCode.ReviewRejected = 73` (Customer, PushInApp, Standard,
      destination Booking) raised inline by `ModerateReviewCommandHandler` on Reject only — hiding stays silent,
      because it is usually someone else's report and it is reversible. The admin reject form now says the reason
      goes to the customer.
- [x] 8.0 DECIDE FIRST, because it determines where every other `booksy-frontend` task lands. There are
      already THREE review surfaces in that app and this change must end with one:
      (a) `components/profile/ProfileReviews.vue` — the mounted provider reviews tab, entirely fabricated;
      (b) `modules/customer/components/modals/{ReviewsModal,EditReviewModal,ReviewCard}.vue` — wired to
      UserManagement routes that do not exist;
      (c) `modules/reviews/` — an empty scaffold: `review.api.ts`, `review.store.ts` and `useReview.ts` are
      **0 lines**, the five components and `ReviewsView.vue` are 16-line stubs, `ReviewsView` is routed from
      nowhere — but `review.types.ts` is a real 261-line type set.
      Either make `modules/reviews/` the single home and point (a) and (b) at it, or delete it and keep the
      types. Not deciding means an implementer builds a fourth surface beside the other three.
- [x] 8.1 RED: Vue component tests for `booksy-frontend` — the dimension disclosure in the write/edit form,
      vote state rendered from the caller's own vote, provider reply rendering, and the "no reviews yet"
      treatment. FULL does not run Vue unit tests (see 9.4), so these need their own invocation.
- [x] 8.2 `booksy-frontend` reviews surface. FIRST delete the fabrication: `ProfileReviews.vue` — the reviews
      tab mounted on the provider page — holds six hardcoded Persian reviews, a hardcoded rating
      distribution, the constants 4.8 and 127, and a `console.log` stub for liking. It has never been wired
      to the API. This change's whole premise is that ratings become honest, so shipping it while that
      component invents them is not an option. Wire it to the real provider-reviews endpoint and replace the
      like stub with the real vote call.
- [x] 8.3 `booksy-frontend` repoint. `customer.service.ts` / `customer.store.ts` GET and PATCH
      `/api/v1/customers/{id}/reviews[/{id}]` on `userManagementClient` — routes `CustomersController` does
      not define at all. Move BOTH the list load and the edit onto the ServiceCatalog reviews routes. The one
      file that already targets those routes, `reviews.service.ts`, is imported by nothing: wire it in or
      delete it. Also fix `ProviderDetailView.vue`, which discards `rating`, `reviewCount` and `reviews`
      after the API call, and the landing-page `|| 5.0` fallback in `FeaturedProviders.vue`.
- [x] 8.4 RED: `booksy-admin` tests for the moderation views — the queue renders pending items oldest first,
      each action posts the right call, reject and hide require a reason, and a non-admin is refused. The app
      already has a `src/**/__tests__` convention to follow.
- [x] 8.5 `booksy-admin`: the moderation surface — a new `src/views/reviews/` beside the existing
      `providers`/`services`/`users` views, plus its router entry and sidebar item: the pending queue,
      approve, reject with reason, hide with reason, restore, and the reported-reviews view, against the
      endpoints named in 6.5. There is no reviews surface in this app today. Add the Persian and English
      strings to `src/locales/fa.json` and `en.json` — this app is fully localised and an untranslated view
      is a visible regression, not a detail.
- [x] 8.6 `booksy-admin`: `views/providers/ProviderDetails.vue` already renders `provider.totalReviews`,
      which is a constant 0 until 4.6 lands. Once it is real, show the rating beside it and give the
      unrated case the same "no reviews yet" treatment as the other clients.
- [x] 8.7 RED: Flutter widget/bloc tests for `booksy-customer-app` — dimensions in the write dialog, reply and
      breakdown rendering, the vote control, the author's own-reviews list showing moderation state and
      rejection reason, and the "no reviews yet" treatment in `provider_rating.dart` and the provider cards.
- [x] 8.8 `booksy-customer-app`: extend `features/reviews/` — today it holds only a remote datasource, a
      repository, one entity and two widgets, and the datasource has no vote, edit or own-list call. Add
      those, plus the moderation-state display. Its `hasRating(rating, reviewCount)` guard currently
      collapses to `rating > 0` because the API's review count is a constant zero — 4.6 fixes that server
      side, and this task stops the client compensating for it.
- [x] 8.9 RED: Flutter widget tests for `booksy-provider-app`, including a themed button inside a `Row`
      against the real `AppTheme` — its buttons are infinite-width via `Size.fromHeight` and blank the page
      otherwise, which only a real-theme widget test catches.
- [x] 8.10 `booksy-provider-app`: net-new reviews feature. This app has four features today (auth, home,
      invitations, onboarding) and nothing review-shaped at all, so this is a whole feature module plus its
      DI registration, route and navigation entry — not a screen bolted onto an existing one. Read reviews of
      your business against the owner-scoped listing from 5.15 (not the public one, which by then returns
      published rows only), see dimension breakdowns, reply, and see a reply's own moderation state so a
      provider knows their answer is not live yet.
- [x] 8.11 `booksy-provider-app` home: surface the business's rating and published review count, and the
      count of reviews awaiting the provider's reply. The Home workspace is the app's designed entry point;
      a reviews feature that can only be reached from a menu will not be seen.
- [x] 8.12 DECIDED 2026-09-22 (user): no. The four dimensions stay optional and behind their disclosure at every
      rating, on every client. No code change — the shipped behaviour already matches the decision.

## 9. Close out

- [x] 9.1 Audit all four clients for anonymous calls to the vote endpoint before deploy (6.3 is breaking).
- [x] 9.2 Confirm no consumer reads `Provider.AverageRating` without also reading `PublishedReviewCount` —
      design D8 leaves 0.0 stored for unrated providers, and this is the design's weakest seam. 3.5's single
      `SetRatingAggregates` method makes the write side mechanical; this task covers the read side.
- [x] 9.3 Map every spec scenario across the five spec files to the test that covers it, and name any with no
      coverage. Verify being green is not the same as being covered.
- [x] 9.4 `scripts/verify.ps1 -Tier full` green. Note FULL runs `flutter analyze`/`flutter test` and the Vue
      type-check and lint, but NOT Vue unit tests — 8.1's tests need their own invocation or they never run.
- [x] 9.5 Update `openspec/specs/` via the sync/archive flow. Record the migration steps AND the rollback in
      `docs/DEPLOYMENT_RUNBOOK.md`, which has no rollback section today: production compose pins `:latest`,
      so rolling back means editing it to the `-api:${sha_short}` tag and re-running `up -d`. Include the
      one-off recompute (4.7) and add a post-deploy smoke assertion on
      `GET /api/v1/reviews/providers/{id}` to the deploy job, shaped like the existing
      `tests/e2e/keystone-booking-flow.sh` gate — the compose healthcheck is `curl /health` and can never
      see a broken review read.

## Decisions

- T1 (9.5) The spec sync into `openspec/specs/` happens at archive (`/opsx:archive`), and this repository archives
  a change only after it is verified on production (see `ae2183e5`, notification-system). Archiving now would claim
  a verification that has not happened, so 9.5 delivers the runbook, rollback and deploy smoke; the sync is the
  archive step after the deploy. The smoke script also runs in the `e2e-keystone` CI job so it is proven before it
  can block a production deploy.
- T1 (8.11) The Home "awaiting your reply" number is computed server-side (`awaitingReplyCount` on the owner
  inbox, counted over every review) rather than on the client from one page, so it cannot depend on paging.
  Awaiting = published and either unanswered or answered with a reply an administrator rejected; a reply awaiting
  approval is the business's part done. Additive nullable field: `GET /reviews/me` leaves it null.
- T1 (8.10/8.11) Provider app headline numbers (average, published count, dimension averages) come from the
  public listing's `statistics` (published only — the same numbers customers see); the rows come from the owner
  inbox (every state). The Home card is appended after the registry zones instead of becoming a new
  `HomeWidgetId`: its data is independent of `HomeContext`, and a registry zone would need resolver and visibility
  rules for a card that is simply always shown on a working Home. It hides itself on failure, like the bell's badge.
- T1 (8.7) Customer app `hasRating(rating, count)` is now count-first: a known count decides (`count > 0`), the
  rating only stands in where no count travels (the recently-visited mini cards, whose customer payload has none).
  `widgets_test.dart` pinned `hasRating(4.5, 0) → true`; that expectation existed only because the API's count was
  a constant zero (4.6 fixed it), so it was changed deliberately to `false`, not to make new code pass. A known
  zero now renders an explicit "هنوز نظری ندارد" part on `ProviderMetaLine`; an unknown count still renders
  nothing, which is why two meta-line tests moved from `reviewCount: 0` to no count.
- T1 (8.7) Voting on the customer app: the section takes a nullable `onVote`; the page supplies one that sends a
  guest to login (return-to-intent) and a signed-in reader to `ProviderDetailCubit.vote`, which adopts the
  server's tally rather than computing its own. Own reviews live on `/profile/reviews` (auth-required route),
  entered from the profile tab.
- T1 (1.2) Dimensions travel into `Review.Create` as one `ReviewDimensionRatings` record (four nullable decimals)
  and land as four flat properties on the aggregate, per design D1. The record keeps `Create`/`EditByAuthor`
  signatures short and is the one place the fixed set of four is named. The pre-existing `Rating` value object
  was not reused: it carries `TotalReviews`/`LastUpdated` for an aggregate, and only a seeder uses it.
- T1 (1.2) Validation errors are keyed by request field name — `Rating` for the overall, `CleanlinessRating`
  etc. for dimensions — via `DomainValidationException(propertyName, message)`, so a client can say which star
  row is wrong. The overall's key changed from the whole message (the old single-string overload) to `Rating`.
- T1 (2.2) Each moderation transition is legal from exactly one state (Publish/Reject ← Pending, Hide ←
  Published); anything else throws `InvalidAggregateStateException`, the repo's existing illegal-transition
  type. A second approve is therefore refused rather than silently idempotent — two admins racing get one
  success and one clear refusal. Reasons are required for Reject/Hide, trimmed, ≤ 500 chars.
- T0 (3.2) `ReviewModerationStatus` persists by name (`HasConversion<string>()`), following the module's other
  enums, with a database default of `'Pending'` so any row written by pre-moderation code lands in the queue
  rather than going straight to the public. (First recorded as int/0; switched once the convention was checked.)
- T0 (order) 2.1–2.2 were done before 1.3–1.4: the edit-window tests refuse Rejected/Hidden edits, which
  need the moderation states to exist.
- T1 (1.4) Authorship is NOT checked inside the aggregate. `Review.IsAuthoredBy(userId)` answers the question
  and the command handler throws `ForbiddenException` (403), matching how `Booking` keeps ownership in its
  handlers. Throwing from the aggregate would have surfaced as a 400 for what is an authorisation failure.
- T1 (1.4) Considered and declined: a maximum-edits-per-review invariant. How many edits a customer gets is a
  product rule nobody has stated; the abuse it guards against (a customer-triggered unpublish + recompute) is
  bounded by the tight per-caller rate limit on the edit endpoint (6.6) instead.
- T1 (1.4) The window is inclusive (`utcNow <= CreatedAt + 7 days`) and takes `utcNow` as a parameter, like
  `NotificationOutboxPolicy`, so it is testable without a clock. A closed window throws
  `BusinessRuleViolationException("ReviewEditWindow", …, "REVIEW_EDIT_WINDOW_CLOSED")`. An edit may clear the
  comment. All validation runs before any field changes, so a refused edit leaves the review untouched.
- T1 (2.4) A reply's moderation reuses `ReviewModerationStatus` as a nullable `ReplyModerationStatus` (null = no
  reply). Rejected is NOT terminal for a reply — the provider may rewrite it and it re-enters the queue; the
  spec only makes rejection permanent for reviews. A reply may only be added to a Published review: it answers
  what the public sees, and replying to a pending review that may yet be rejected is wasted moderation work.
- T1 (2.5) Re-publication is detected by `FirstPublishedAt` (set once, on first publication), carried as
  `ReviewPublishedEvent.IsRepublication`. `Restore` also publishes with `IsRepublication = true`.
- T2 (2.4) `AddProviderResponse` used to overwrite an existing reply silently; it now refuses a second one.
  `RemoveProviderResponse` now refuses when there is nothing to remove. No production caller existed.
- T2 (1.4) Deleted `Review.UpdateComment` and `Review.UpdateRating`: no callers anywhere, and each would let a
  future handler change a review while skipping the edit window and the return to moderation.
- T2 (2.4) `ReviewSeeder` now publishes each seeded review and approves each seeded reply: seeds stand in for
  existing reviews, which the migration publishes, and a pending seed would refuse its own reply.
- T1 (3.3/3.4) `ReviewVote` and `ReviewReport` are small aggregate roots in their own tables (the
  `MembershipAuditEntry` shape), each with a unique index — (review, user) and (review, reporter) — that is the
  real one-per-user guarantee under concurrency, and a cascading FK to `Reviews`.
- T1 (3.5) `ProviderRatingSummary` is an Infrastructure persistence type, not a domain aggregate: derived data
  with no invariants, overwritten wholesale by the recompute (the `NotificationOutboxEntry` placement).
  Per-dimension averages are `numeric(4,2)`; the overall stays on `Providers` as before.
- T1 (3.5) `Provider.SetRatingAggregates` refuses an average with a zero count and an out-of-range average with a
  positive one, so the stored pair can never say "rated" and "unrated" at once. `AverageRating` went from
  `internal set` to `private set`: nothing assigned it.
- T1 (3.6) `Review.HelpfulCount`/`NotHelpfulCount` survive as COMPUTED totals (legacy + live), unmapped, so every
  existing reader shows the right displayed value unchanged. The helpful sort orders on the sum of the two mapped
  columns, since a computed property does not translate to SQL.
- T0 (order) 5.9's domain rule was pulled into 3.6 as `ReviewHelpfulnessPolicy` (test-first): renaming the
  counters forced the ratio question immediately, and deferring it would have shipped a frozen ratio in-tree.
- T1 (4.1/4.2) The rating rule is written once, as the pure `ProviderRatingCalculator` (unit-tested), and the
  recompute feeds it a lean projection of the provider's published reviews in one query — rather than restating
  the rule as a SQL `GROUP BY`. Same single round trip; the rule is not duplicated between C# and SQL. Averages
  round to 2 decimals, half away from zero.
- T1 (4.2) The recompute overlays the reviews the DbContext is tracking on the stored rows, so it sees the
  command's own unsaved approve/hide. Callers need not flush first; without the overlay every rating would sit
  one moderation action behind. `ProviderRatingRecomputerTests` fails without it.
- T1 (4.2) Known limitation, accepted: two recomputes racing on one provider are last-writer-wins, because
  `Provider.Version` only moves when a domain event is raised. The next moderation action for that provider
  recomputes from scratch and heals it, and 4.7 is re-runnable. Bumping `Version` on every recompute was
  rejected: it would hand a provider editing their own profile a false 409 whenever an admin moderated.
- T2 (4.6) The unrated signal on the wire is ADDITIVE: `averageRating` stays numeric (0 when unrated) and the
  real published count goes into `totalReviews`. Design D8 had said "return null"; the deployed Vue app calls
  `rating.toFixed(1)` unguarded in four places, so null would have thrown in production. D8 is revised in place.
- T2 (4.6) Beyond search, four more paths returned a real `AverageRating` beside a fake count and were fixed with
  the same one-liner: `GetProviderById` (the customer detail page), `GetProviderByOwnerId` (the provider app),
  `GetProvidersByStatus` (the admin list) all hardcoded `TotalReviews = 0`; by-location never assigned it, so
  its `dynamic` serialised as `null`. `GetProviderProfile` counted every review in every state — it now reads the
  same source as its average. Each has a test in `ProviderSearchRatingTests`.
- T1 (5.13) One `ModerateReviewCommand` with a `ReviewModerationAction` (Approve/Reject/Hide/Restore/ApproveReply/
  RejectReply) instead of six commands: each is load → one domain transition → recompute → save, and the legality
  of every transition lives in the aggregate. The recompute runs only when public visibility actually changed.
  Endpoints: `POST /api/v1/admin/reviews/{id}/{approve|reject|hide|restore|reply/approve|reply/reject}`, and
  `GET /api/v1/admin/reviews/queue?filter=pending|hidden|reported`. Illegal transitions are 400 (the middleware's
  existing mapping of `InvalidAggregateStateException`); an unknown review is 404.
- T1 (5.13) The pending queue holds reviews awaiting a decision AND reviews whose reply awaits one, ordered by how
  long each has waited (edited review: since its edit; reply: since it was written), with `reviewPending` /
  `replyPending` flags and `wasPublishedBefore` so a moderator can tell an edit from a first submission.
- T1 (8.5) `booksy-admin` has no mounted-view tests and mounting Ant Design in jsdom is brittle, so the page's
  behaviour lives in `useReviewModeration` (load a queue; act; drop an item only once the server accepted; a failed
  load is an error, never an empty queue) and is tested there, with the client in `reviews.api.spec.ts` (the client
  refuses to send reject/hide/reply-reject without a reason). `ReviewModeration.vue` is layout: tabs
  pending/reported/hidden, an "edited — was published before" tag, reports, a reason modal that says rejection is
  permanent. Route `/reviews`, sidebar item, fa+en strings added as JSON (pure additions). No lint gate exists in
  this app (no ESLint config or script) — pre-existing.
- T1 (8.0) DECIDED: `modules/reviews/` is the single home, rebuilt for real. Correction to the plan's own premise:
  its "real 261-line type set" was a pasted MOCK file (header `src/mocks/types/review.types.ts`) that also defined
  unrelated Notification/StaffMember/Payment types and a different dimension set (quality/value/professionalism)
  contradicting this change — so nothing was kept. Deleted: the 0-line api/store/composable, the 16-line stub
  components, `ReviewsView.vue`, the mock types, and the unused `customer/services/reviews.service.ts`. Built:
  `reviews.api.ts`, `reviews.types.ts`, `RatingStars`, `ReviewForm`, `ReviewCard`, `ReviewList`. Both old surfaces
  now sit on it: `ProfileReviews.vue` is a thin wrapper over `ReviewList`; the customer modals use `ReviewForm`
  and the new client. 27 + 5 Vue tests.
- T1 (8.2) Star input is whole stars (a valid subset of the half-star rule, and hittable on a phone); display renders
  halves. The web client shows the moderation note ("نظر شما پس از بررسی نمایش داده می‌شود") on every submit/edit.
- T2 (8.3) Beyond the listed repoint: `provider.service.mapProviderResponse` silently dropped `averageRating` /
  `totalReviews`, so the customer detail view could not have shown a rating even with its hardcoded 0 removed; the
  `Provider` type gained both fields. `FeaturedProviders` now shows "جدید" for an unrated salon instead of 5.0.
- T2 (8.3) Backend addition found by the client: "My reviews" must name the salon, its picture and the service
  (customer-profile spec), and `/reviews/me` returned ids only. Added `GetReviewContextsAsync` (batched, one query
  per table) and `providerName` / `providerLogoUrl` / `serviceName` on the item, test-first.
- T1 (6.6) Policies `edit-review` 10/h (tightest), `report-review` 20/h, `reply-review` 60/h, `moderate-review`
  600/h, registered in `RateLimitingOptions.Defaults`. `ReviewRateLimitingTests` asserts every `[EnableRateLimiting]`
  name in the ServiceCatalog API is registered (the failure mode is a 500 on first call), every review write path
  carries one, and edit's ceiling is the lowest.
- T1 (7.4) THREE codes, not two: `ReviewPublished` (70) and `ReviewRepublished` (71) to the salon's owner,
  `ReviewReplyPublished` (72) to the author — the spec needs a changed review distinguishable from a new one and
  the catalogue's rule is one code per notification. All four surfaces landed together (enum, catalogue
  descriptor, `NotificationTypeFor` → `NewReview`/`ReviewResponse`, Persian copy with a new `rating` parameter
  rendered in Persian digits). PushInApp, Standard, suppressible, no SMS; the destination is the booking (7.5). The
  `notification-system` session that owned these files had finished and had said adding them was fine.
- T1 (7.6) Raised inline in `ModerateReviewCommandHandler`, on its unit of work, only when something became public:
  first publication → `ReviewPublished` (dedup key = review id); edit re-approval or restore → `ReviewRepublished`;
  reply approval → `ReviewReplyPublished`. Submission and rejection raise nothing (7.7 is open). A restore that makes
  an old reply visible again does NOT re-announce the reply.
- T0 (7.1) The inbox-boundary test passed on its first run: the defect it guards (a legacy second store serving a
  review request into the inbox) was fixed at its source by the notification-system work before this slice. Kept as
  a regression guard at the user-visible boundary. Likewise the restore/reply test was written after the fix.
- T1 (5.17) `GetByProviderIdAsync` defaults to `publishedOnly: true` — fail-closed, so both public callers (the
  listing and the profile's recent reviews) got the fix without being touched, and only the inbox opts out. The
  unused `GetRecentReviewsAsync` feed is closed the same way. Statistics are published-only always and use the same
  `ProviderRatingCalculator` as the stored rating, so the two can never disagree; they now carry per-dimension
  `{average, count}`.
- T2 (5.17) Found while writing the listing: the public listing mapped `ProviderResponse` unconditionally, so a
  pending or REJECTED reply would have been public. It now shows a reply only when `IsReplyPubliclyVisible`, and
  `ReviewsWithProviderResponse` counts only those.
- T2 (5.15) SECURITY-ADJACENT, flagged with 5.6: the owner inbox (`GET /api/v1/reviews/providers/{id}/inbox`) is
  gated on `ManageOrganization` — the same people who may reply see what they would reply to, including pending
  reviews that may yet be rejected for their content. The author's list is `GET /api/v1/reviews/me`, every state,
  with the moderation reason and a computed `canEdit`.
- T1 (5.11) Report is `POST /api/v1/reviews/{id}/report {reason}`; published reviews only (400 otherwise); a
  repeat by the same user is 409 (pre-checked, and the unique index holds it under concurrency). The reported
  list is `GET /api/v1/admin/reviews/queue?filter=reported`, most-reported first, each item carrying
  `reportCount` and `reports[] {reason, reportedByUserId, createdAt}`; hiding takes a review off it.
- T1 (5.8) Recorded as D4 asked: the vote tallies move by an atomic `ExecuteUpdate` delta (`SET x = x + @d`) on
  the command's transaction — not a re-derivation. Its row lock serialises concurrent voters on one review; the
  unique (ReviewId, UserId) index makes a racing duplicate insert fail at commit, rolling its delta back with it.
  The toggle itself is the pure `ReviewVotePolicy` (unit-tested). The response adds `myVote`.
- T2 (5.8) `ExceptionHandlingMiddleware` now maps a PostgreSQL unique_violation (23505) anywhere in the exception
  chain to 409 `DUPLICATE_CONFLICT`, instead of 500. Repo-wide in effect: a duplicate row was never a server
  error. Found by the 50-concurrent-votes test, which produced 500s before it.
- T2 (5.8) Deleted `MarkReviewHelpfulCommand`/handler and `Review.MarkAsHelpful`/`MarkAsNotHelpful`; the live
  tallies now have no domain writer at all. `ReviewSeeder` no longer seeds helpful votes (it would have to invent
  voters). Two domain tests that faked live votes through `MarkAsHelpful` were moved to `ReviewVoteTests`, where
  the same two behaviours run through the real vote path — the suite got stricter, not smaller.
- T2 (5.6) SECURITY-ADJACENT, flagged: "the owning provider" is implemented as anyone who may act for the salon on
  profile-level matters — `CanManageOrganizationQuery(..., ManageOrganization)`, i.e. the owner or a manager, the
  same people who may edit the salon's public profile. Plain staff cannot reply (a reply is public speech for the
  business), and there is deliberately NO admin shortcut, unlike `CanManageProvider`. If the product wants only the
  legal owner to reply, that is a one-word change of permission.
- T1 (5.6) One `ManageReplyCommand` (Add/Edit/Remove) — `POST|PUT|DELETE /api/v1/reviews/{id}/reply`. A second
  reply is 409 (checked before the domain call, so it reads as a conflict rather than a bad request, as the spec
  says); replying to a review not yet published is 400.
- T1 (5.4) Edit is `PUT /api/v1/reviews/{id}`, reusing the create request (same fields, same validation). A
  closed window / rejected / hidden review is 400; a non-author is 403 (checked in the handler); an edit of a
  published review recomputes the rating inline, since it is an unpublish triggered by a customer.
- T0 (order) The moderation slice (4.3 in part, 4.4, 5.11–5.13, 6.5) was built before edit/reply/vote: those
  slices need a published review, and the only honest way to make one in a test is the approve endpoint.
- T1 (4.7) The backfill is an admin endpoint, `POST /api/v1/admin/reviews/recompute-ratings` (`AdminOnly`), run once
  after deploy and re-runnable. Rejected: restating the rating rule as SQL inside the migration (two copies of the
  rule), and a startup sweep (it would race the shared integration host, where tests set ratings directly). The
  cost is a manual deploy step — recorded for the runbook in 9.5. `ReviewModerationController` is the home the
  moderation endpoints (6.5) will join.
- T1 (4.6) Rating sort: `ORDER BY PublishedReviewCount = 0, AverageRating <dir>, BusinessName` — unrated last in
  both directions, and the same banding in the no-coordinates "distance" fallback. `ProviderSummaryItem` was left
  alone: it is referenced only from commented-out code.
- T2 (3.7) Migration `20260921213139_AddReviewModerationAndVoting`: generated, then a hand-written, guarded
  backfill appended (existing reviews → Published with `FirstPublishedAt = CreatedAt`; existing replies →
  Published). Written only — applied to no shared database.

## Log

- 2026-09-22 7.7 implemented after the user's decision: `ReviewRejected` event code + `NotificationType.ReviewRejected`
  (stored by name, no migration), catalogue entry, Persian copy carrying the reason, raised in
  `ModerateReviewCommandHandler` for Reject only, addressed to the author. The integration test that asserted
  "rejecting notifies nobody" was deliberately inverted (the decision changed the requirement) and a new test pins
  that hiding still notifies nobody. Spec delta and `coverage.md` updated. Clients: all three route notifications by
  `destinationKind` (Booking), which every client already handles, and suppression is per channel, so no preference
  surface changes; the admin moderation form gained `reasonAudience` + a line naming who reads the reason
  (customer for a review, salon for a reply, nobody for a hide). Customer-facing reason display already shipped in
  8.x on both the web app and the customer app. Admin: type-check clean, 77/77 vitest.
- 2026-09-22 8.12 closed as "no change": decision matches shipped behaviour.
- 2026-09-22 9.4 `scripts/verify.ps1 -Tier full` PASS — 20 steps, 665 s: build, 8 unit/architecture projects
  (ServiceCatalog domain 711, application 237, api 108), `Booksy.Host.IntegrationTests` 763/763, Vue type-check/lint/
  unit for both web apps, `flutter analyze` + `flutter test` for both apps. Separately: `booksy-admin` vitest 76/76;
  `booksy-frontend` vitest 162 pass / 36 fail, the same 36 pre-existing failures in the same 5 files as the earlier
  baseline (empty auth specs, `tests/integration/*` needing a live backend).
- 2026-09-22 9.5 `docs/DEPLOYMENT_RUNBOOK.md`: new "Rolling back the API" (`:latest` → `-api:<sha_short>`, `up -d`,
  lasts until the next master push) and "Deploying provider-reviews-and-ratings" (backup, additive migration,
  one-off `recompute-ratings`, smoke, why the image rollback is safe here and `Down` is not). New
  `tests/e2e/review-read-smoke.sh`, run by the deploy job after the health checks and by `e2e-keystone` in CI.
  `API_ENDPOINTS.md`/`DTO_MAPPING.md` carry `awaitingReplyCount`. Spec sync deferred to archive (Decisions).
- 2026-09-22 9.3 scenario → test map in `coverage.md` (every scenario of the five spec files). Gaps closed test-first:
  comment rules (3 domain tests — RED on a real defect: the 10-character minimum was checked before trimming, and a
  whitespace comment was stored as `""`; fixed in `Review.NormalizeComment` for create and edit), helpful-sort on
  live votes (IT, green first run), signed-in non-voter `myVote` (IT assertion, green first run), Vue expired-review
  message and awaiting-approval edit toast (2 Vue specs, RED then fixed). Left uncovered with a reason: the web
  modal's "no reviews" empty state lacks the scenario's "جستجوی خدمات" button — pre-existing, not touched here.
- 2026-09-22 9.2 read-side audit of `AverageRating`: every provider query handler that projects it
  (`GetProviderById`, `GetProviderByOwnerId`, `GetProvidersByStatus`, `GetProvidersByLocation`, `GetProviderProfile`,
  `SearchProviders`) projects `PublishedReviewCount` beside it as `TotalReviews`, and the controller mappings pass
  both. The two specifications that use it alone are safe by construction: `FeaturedProvidersSpecification` orders
  descending (unrated 0 sinks below every real 1.0–5.0 average) and `SearchProvidersSpecification`'s `minRating`
  filter excludes unrated providers for any positive minimum. `ReviewSeeder`'s `AverageRating` is review statistics,
  not the provider column. Clients: Vue `FeaturedProviders`, admin `ProviderDetails`, customer app `hasRating` and the
  provider app all decide on the count; the customer app's recently-visited mini card has no count and falls back
  to `rating > 0`, which is correct because an unrated provider's average is exactly 0.
- 2026-09-22 9.1 vote-call audit: `booksy-admin` and `booksy-provider-app` make no vote call. `booksy-customer-app`
  checks `AuthBloc` before calling and sends a guest to login with return-to-intent — no anonymous request leaves
  the app. `booksy-frontend` `ReviewList.castVote` does call as a guest; the 401 goes to the auth interceptor, the
  refresh fails without a refresh token, and the guest is sent to `/customer/login?redirect=<current path>` (the
  component's own sign-in prompt is the fallback if that ever changes). No client depends on anonymous voting.
- 2026-09-22 8.10/8.11 `booksy-provider-app`: new `features/reviews/` (api service, repository over the owner inbox +
  public statistics, `BusinessReview`/`ReviewsOverview`, `ReviewsCubit`, `ReviewsPage` with reply dialog and reply
  moderation state, `ReviewsHomeCard`/`ReviewsHomeEntry`), DI (factory cubit), `/reviews` route, More-menu entry,
  Home `trailing` slot (tested: shown on a working Home, absent on error). Inline reply buttons carry a finite
  `minimumSize`. `flutter analyze` clean; `flutter test` 607 passed, 1 skipped (pre-existing).
- 2026-09-22 8.9 RED: `booksy-provider-app/test/features/reviews/` — `reviews_repository_test.dart` (inbox +
  public summary on the wire, POST/PUT/DELETE reply, no-session, refusal message) and `reviews_screens_test.dart`
  (cubit, page and Home card, all against the real `AppTheme.light` at 390×844 with `takeException() == null`)
  fail to compile on the missing feature. Server side for 8.11: `The_inbox_counts_the_published_reviews_still_
  awaiting_the_business_reply` added to `ReviewListingTests` and `awaitingReplyCount` implemented
  (`IReviewReadRepository.CountAwaitingReplyAsync`, inbox view model); written before the code but NOT observed
  red before implementing — recorded rather than claimed. 14/14 listing tests pass.
- 2026-09-22 8.8 `booksy-customer-app`: entities (`ReviewDimension`, `ReviewVote`, `MyReview`, `ReviewModerationStatus`,
  `DimensionAverage`, `ReviewVoteResult`), datasource/repository create+edit (dimensions only when rated), vote,
  `me`; dialog dimension disclosure + edit mode; section breakdown + vote row; `ProviderDetailCubit.vote` adopting
  the server tally; guest vote → login with return-to-intent; `/profile/reviews` (auth-required) from the profile
  tab. `provider_detail_page_test` "no data yet" now expects the "no reviews yet" label instead of no meta line —
  the spec delta asks for it. `flutter analyze` clean; `flutter test` 330/330.
- 2026-09-22 8.7 RED: `test/features/reviews/review_engagement_test.dart` (dialog dimensions + edit prefill,
  breakdown, vote control, cubit vote, my-reviews list/page) and `review_repository_test.dart` (wire bodies for
  create/edit/vote, listing and `me` parsing) fail to compile on the missing symbols; `widgets_test.dart`
  rating-gate expectations changed as recorded in Decisions.
- 2026-09-22 `booksy-frontend`: type-check 0 errors; `vitest run` 36 failures in 5 files, ALL pre-existing and
  untouched — `auth.api.spec.ts` and `LoginForm.spec.ts` contain no test suite at all, and `tests/integration/*`
  (favorites, financial, gallery) need a live backend. Every review/customer spec passes.
- 2026-09-22 4.5–4.6 green: `ProviderSearchRatingTests` 9/9, went red first for the two predicted reasons
  (`totalReviews` 0 instead of 12; ascending put the unrated salon FIRST). Noted for 5.17: the profile's
  `RecentReviews` still come from `GetByProviderIdAsync`, which returns every moderation state — the published-only
  filter there fixes the public listing and the profile together.
- 2026-09-22 Sections 1–3 done. FAST PASS (694 domain tests); `FullyQualifiedName~Review` integration 9/9,
  including both migration tests against real Postgres. The "do not run FULL before 3.7" caution below is lifted:
  the model and the snapshot agree again.
- 2026-09-22 3.1 went red first for the WRONG reason (`23502` on `IsDeleted` — the legacy insert missed the
  soft-delete column every entity carries); fixed the arrange, then red for the right one (`42703` on
  `ModerationStatus`), then green with the migration.
- 2026-09-22 1.1–1.2 green, FAST PASS. **Do not run FULL before 3.7**: EF maps the four new `Review`
  properties by convention, so the model now differs from the last migration snapshot and EF 9 treats pending
  model changes as an error in `Migrate()` — the composed host would fail at startup until the migration lands.
- 2026-09-22 Implementation started (`/opsx:apply`). Baseline: branch `feat/provider-auth-flutter` at `cdebbd87`,
  tree clean. Since planning, the `notification-system` session implemented the review-submission withdrawal in
  `CreateReviewCommandHandler` (commit `c71204fb`) with an `onlyCodes` filter, so 7.3 is verification, not new code.
