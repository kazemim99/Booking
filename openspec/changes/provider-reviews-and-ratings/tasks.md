Status: PLANNED
Verify: FAST

<!-- Deliberately NOT `Status: ACTIVE`. The Stop hook gates the whole shared checkout on the most recently
     modified ACTIVE tasks.md, and `notification-system` currently holds that role with its owner's knowledge.
     Flip this to ACTIVE only when starting implementation AND the other change is done or parked — otherwise
     this change silently steals the gate from a session mid-loop. -->

Change: provider-reviews-and-ratings. Proposal, specs and design are complete and validated.

Test-first per `AGENTS.md`: within each slice the test task comes before the implementation task it
describes. Run `scripts/verify.ps1 -Tier fast` after each task; `-Tier full` before closing.

Two open product questions from `design.md` are marked `[?] DECISION:` below. Neither blocks structural
work — both are copy and a notification, not shape.

## 1. Domain: rating dimensions

- [ ] 1.1 Unit tests for the rating model: overall required; each of the four dimensions independently
      omittable; every rating within 1.0–5.0 on 0.5 increments; out-of-range and off-increment rejected by
      name; overall never recomputed from dimensions.
- [ ] 1.2 Add `CleanlinessRating`, `SkillRating`, `PunctualityRating`, `ConductRating` as nullable decimals on
      `Review`; extend `Review.Create` and validation. Keep `RatingValue` as the customer's own overall.
- [ ] 1.3 Unit tests for the edit window: author edits at day 2 succeed, day 8 rejected, non-author rejected,
      an edit returns the review to pending.
- [ ] 1.4 `Review.EditByAuthor(...)` enforcing the 7-day window and resetting moderation state.

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
      public visibility, following the `RaiseDomainEvent` pattern in `BookingAggregate`.

## 3. Persistence and migration

- [ ] 3.1 RED: integration test against Testcontainers for the migration backfill — pre-existing reviews are
      published and still returned by the public listing; pre-existing counter values survive as the baseline
      with no votes attributed to anyone.
- [ ] 3.2 Extend `ReviewConfiguration` for the dimension, moderation and reply-moderation columns; index
      `(ProviderId, ModerationStatus)` for the aggregate query and the public listing.
- [ ] 3.3 `ReviewVote` entity + configuration: own table, unique index `(ReviewId, UserId)`, FK to `Reviews`.
      Not an owned collection — see design D4 and the ValueGeneratedNever hazard in D1.
- [ ] 3.4 `ReviewReport` entity + configuration: unique index `(ReviewId, ReportedByUserId)` so a user cannot
      report twice.
- [ ] 3.5 `ProviderRatingSummary` entity + configuration keyed by `ProviderId`, holding the four dimension
      averages and their counts. Add `PublishedReviewCount` to `Provider`.
- [ ] 3.6 Rename `HelpfulCount`/`NotHelpfulCount` to `LegacyHelpfulCount`/`LegacyNotHelpfulCount`; add
      `HelpfulVoteCount`/`NotHelpfulVoteCount`. Legacy columns are never written again after the rename.
- [ ] 3.7 Migration: all of the above, additive apart from the rename. Backfill every existing review and
      every existing reply to `Published`. Idempotent. Turns 3.1 green.

## 4. Rating aggregates

- [ ] 4.1 RED: unit tests for the aggregation rules — dimension averaged only over reviews that rated it;
      pending and hidden reviews excluded; a provider with no published reviews reported as unrated, not zero.
- [ ] 4.2 Recompute service: one grouped query per provider writing `Provider.AverageRating`,
      `Provider.PublishedReviewCount` and `ProviderRatingSummary`. Recompute, never increment (design D6).
- [ ] 4.3 RED: integration test — publish → average moves; hide → average recomputes without it; edit a
      published review → it leaves the aggregates until re-approved.
- [ ] 4.4 Domain event handler invoking the recompute on publish/unpublish/hide/edit-approval/removal.
      Test through the handler, not by calling the recompute service directly — the wiring is the behaviour.
- [ ] 4.5 One-off recompute across all providers, runnable after the migration, so `AverageRating` becomes
      true for the first time. Must be re-runnable, and covered by its own test.

## 5. Commands and queries

- [ ] 5.1 RED: integration tests for create — eligibility (non-owner forbidden, non-completed conflict,
      duplicate conflict, unauthenticated rejected) and that a new review lands in `Pending`.
- [ ] 5.2 Extend `CreateReviewCommand`/`Handler` for dimensions and for entering `Pending`, keeping the
      existing eligibility checks.
- [ ] 5.3 RED: integration tests for edit — author inside the window, author outside it, non-author, and that
      an edit returns the review to pending.
- [ ] 5.4 `EditReviewCommand` + handler.
- [ ] 5.5 RED: integration tests for reply authorisation — owning provider succeeds; different provider
      forbidden; the review's author forbidden; an administrator forbidden (design D11); second reply conflicts.
- [ ] 5.6 `ReplyToReviewCommand`, `EditReplyCommand`, `RemoveReplyCommand` + handlers, with the resource check
      that the caller's provider owns the reviewed business.
- [ ] 5.7 RED: integration tests for voting — fifty repeats from one user leave one vote; changing a vote never
      double-counts; author cannot vote on their own review; voting on a pending review rejected; withdrawing
      never takes a count below the legacy baseline; concurrent votes from one user do not both insert.
- [ ] 5.8 Replace `MarkReviewHelpfulCommand` with `CastReviewVoteCommand`: insert/replace/toggle-off against
      `ReviewVotes`, updating the denormalised counts in the same transaction.
- [ ] 5.9 RED: integration test that a duplicate report from the same user is rejected.
- [ ] 5.10 `ReportReviewCommand` + handler.
- [ ] 5.11 RED: integration tests for the moderation commands and the queue — approve, reject, hide, reply
      approve/reject, admin-only authorisation, queue ordered oldest first.
- [ ] 5.12 Moderation commands (`ApproveReviewCommand`, `RejectReviewCommand`, `HideReviewCommand`,
      `ApproveReplyCommand`, `RejectReplyCommand`) and `GetModerationQueueQuery`.
- [ ] 5.13 RED: integration test that the public listing returns published only, given a provider holding
      pending, rejected, hidden and published reviews, and that it reports the caller's own vote.
- [ ] 5.14 Extend `GetProviderReviewsQuery`/`Handler`: published-only filter, dimension statistics, and the
      calling user's own current vote per review.

## 6. API

- [ ] 6.1 RED: controller tests in `Booksy.ServiceCatalog.Api.UnitTests` for every endpoint below — including
      that the identity is read from `ClaimTypes.NameIdentifier` first, since production tokens carry
      `nameidentifier` and not `sub`/`userId`, and that the vote endpoint rejects an anonymous caller.
- [ ] 6.2 `ReviewsController`: dimensions on create; new edit endpoint (closes the 404 the Vue client already
      ships against); reply add/edit/remove; report.
- [ ] 6.3 Change `PUT /reviews/{id}/helpful` to authenticated and idempotent-per-user. **BREAKING** — remove
      `[AllowAnonymous]`.
- [ ] 6.4 `GET /reviews/providers/{id}` returns published only, with dimension statistics and the caller's own
      vote. **BREAKING** for any client relying on unpublished rows being returned.
- [ ] 6.5 Moderation endpoints under `[Authorize(Roles = "Admin,SysAdmin")]`, matching `BookingsController`.
- [ ] 6.6 Rate-limiting policies for vote and report alongside the existing `create-review` /
      `mark-review-helpful` / `provider-reviews` policies.
- [ ] 6.7 Update `API_ENDPOINTS.md` and `DTO_MAPPING.md`.

## 7. Notifications

- [ ] 7.1 RED: integration test that submitting a review withdraws the pending review-request reminder, and
      does so while the review is still pending moderation. Test through the create-review handler, not by
      asserting on `INotificationRaiser` directly — the call site is what must be proven.
- [ ] 7.2 Call `INotificationRaiser.WithdrawPendingForSubjectAsync("Booking", bookingId)` from the create-review
      handler, in the same unit of work. Re-verify at this point that no other pending notification exists for
      a completed booking — design D10 is safe by circumstance, not by construction.
- [ ] 7.3 Agree the two new `NotificationEventCode` entries with the `notification-system` owner before raising
      either — `NotificationEventCatalog` throws rather than defaulting. Both are in-app/push, non-critical,
      suppressible; neither may name SMS (reserved for critical, fails the catalogue self-validation test).
- [ ] 7.4 RED then green: raise the provider notification on a review being published, and the customer
      notification on a provider reply being published, each tested through the publishing path.
- [ ] 7.5 [?] DECISION: is a rejected review's author told, and told why? The `notification-system` owner has
      settled 7.6 as *never re-ask a rejected author*, which is only defensible if the rejection is
      communicated here. If the answer is "say nothing", the platform silently swallows reviews. Needs a
      product call; implement nothing until it lands.

## 8. Clients

- [ ] 8.1 `booksy-frontend`: write/edit review with the overall star required and the four dimensions behind a
      disclosure; render dimension breakdowns, provider replies, and vote state from the caller's own vote.
- [ ] 8.2 `booksy-frontend`: show "no reviews yet" instead of a zero rating wherever a rating renders, and fix
      the rating sort to band unrated providers (design D9).
- [ ] 8.3 `booksy-admin`: the moderation queue — list, approve, reject with reason, hide with reason, and the
      reported-reviews view. New surface.
- [ ] 8.4 `booksy-customer-app`: extend `features/reviews/` — dimensions in `write_review_dialog.dart`,
      breakdowns and replies in `provider_reviews_section.dart`, vote control.
- [ ] 8.5 `booksy-customer-app`: "no reviews yet" treatment in `provider_rating.dart` and the provider cards.
- [ ] 8.6 `booksy-provider-app`: net-new reviews feature — read reviews of your business, see dimension
      breakdowns, reply. Build buttons in a `Row` against the real `AppTheme`; its themed buttons are
      infinite-width via `Size.fromHeight` and blank the page otherwise.
- [ ] 8.7 `flutter analyze` + `flutter test` clean in both apps; Vue unit tests for the changed components.
- [ ] 8.8 [?] DECISION: should an overall rating of 3 or below prompt for dimensions? Highest-value dimension
      data, at a completion-rate cost. UX call; default to not prompting until decided.

## 9. Close out

- [ ] 9.1 Audit all three clients for anonymous calls to the vote endpoint before deploy (6.2 is breaking).
- [ ] 9.2 Confirm no consumer reads `Provider.AverageRating` without also reading `PublishedReviewCount` —
      design D8 leaves 0.0 stored for unrated providers and this is the design's weakest seam.
- [ ] 9.3 Map every spec scenario across the four spec files to the test that covers it, and name any with no
      coverage. Verify being green is not the same as being covered.
- [ ] 9.4 `scripts/verify.ps1 -Tier full` green.
- [ ] 9.5 Update `openspec/specs/` via the sync/archive flow; record the migration steps in
      `docs/DEPLOYMENT_RUNBOOK.md`, including the one-off recompute (4.5).
