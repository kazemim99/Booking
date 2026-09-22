## Context

`Review` already exists as an aggregate root in ServiceCatalog with a single `RatingValue`, an optional
comment, an unused `ProviderResponse` pair and two bare integer counters. It has an EF configuration, two
repositories, three endpoints and a seeder. It has no tests. Production holds real review rows.

Three things about the current state shape every decision below:

1. **`Provider.AverageRating` is written by nothing, anywhere.** It is `internal set`, the Domain project
   declares no `InternalsVisibleTo`, and the aggregate exposes no mutator. A repo-wide search for
   assignments finds only DTO mapping, a migration column, and `ReviewSeeder.cs:275` — which is a field of
   an anonymous `statistics` object inside `LogReviewStatistics` that is logged and discarded, never a
   `Provider` write. `ProviderStatisticsSeeder.cs:61` holds the intended write commented out behind a TODO.
   `SearchProvidersQueryHandler` sorts by it. Every provider row in every environment reads `0.0m` — not
   "seed data or zero", just zero.
2. **`HelpfulCount`/`NotHelpfulCount` have no provenance.** They were incremented by an `[AllowAnonymous]`
   endpoint with no per-user record, so there is no way to reconstruct who voted.
3. **`notification-system` is in flight in a parallel session** and owns `NotificationEventCode.ReviewRequest`
   and its 2h/3-day reminder pair (its task 7.6). This change must call into that surface, not rebuild it.

Constraints: one PostgreSQL database, schema-per-context; ServiceCatalog owns both `Provider` and `Review`,
so this is intra-context and needs no CAP integration event.

Admin authorisation is `[Authorize(Policy = "AdminOnly")]`, as `ProvidersController.cs:697` uses — **not**
a raw `Roles = "Admin,SysAdmin"` list. `AdminOnly` requires any of `Admin`, `Administrator` or `SysAdmin`
(`PolicyAuthorizationExtensions.cs:39-40`), and its comment records a real 2026-09-19 incident: the
production administrator carries only one of those names and got 403 on every admin endpoint, which the
tests missed because the test admin carries all three. The vocabulary drift is FOLLOW-UPS #46. A raw role
list here would reproduce that incident. `Policy = "ProviderOrAdmin"`, which `BookingsController` uses, is
a `user_type` claim check on a different axis and is not the moderation gate.

## Goals / Non-Goals

**Goals:**
- A rating signal that is true: derived from published reviews, recomputed on every state change, and
  visibly absent rather than zero when there is nothing to show.
- Multi-dimensional rating that stays cheap to submit and cheap to query.
- Moderation that is a real gate without becoming a second source of truth about verification.
- Voting that is one-person-one-vote and survives a hostile caller.
- A provider reply path that actually exists.

**Non-Goals:**
- **Standalone reviews** not tied to a booking. Eligibility stays completed-booking-only. This is load-bearing
  for `notification-system` task 7.6, which keys its reminder on the booking id.
- **Per-staff ratings.** `StaffProfile` exists and rating the individual stylist is the obvious next step, but
  it multiplies the aggregate surface and the moderation queue. Deliberately deferred.
- **Photos on reviews.** Wants storage, moderation of images, and a gallery surface. Out.
- **Automated abuse detection / profanity filtering.** The queue is human for now.
- **Rewriting search ranking.** Sorting by rating starts telling the truth; the ranking formula is untouched.

## Decisions

### D1. Four nullable columns for dimensions, not a child table and not JSON
`CleanlinessRating`, `SkillRating`, `PunctualityRating`, `ConductRating` — `decimal(3,1) NULL` on `Reviews`,
alongside the existing `RatingValue` which becomes the overall rating.

*Why:* the dimension set is fixed, small, and a product decision. Nullable columns let per-dimension averages
be a single grouped `AVG()` that naturally ignores unrated dimensions, with no join and no `COUNT` gymnastics.

*Alternatives:* a `ReviewDimensionScore` child table keyed by dimension name — rejected because it turns every
aggregate into a pivot, and because it lets a fifth dimension appear through a code path rather than through a
product decision. A `jsonb` column — rejected because averaging across it is opaque and unindexable here.

*Note:* these are plain scalar columns on the aggregate root, **not** an owned collection. Owned child
collections with GUID keys in this codebase must be `ValueGeneratedNever` or appending one throws
`DbUpdateConcurrencyException` on a phantom UPDATE. Scalar columns sidestep that class of defect entirely.

### D2. The overall rating is stored, never computed
`RatingValue` remains the customer's own statement. Dimension averages are reported separately and never
feed back into it.

*Why:* a computed overall can contradict the customer. Someone who rates 5 overall while marking punctuality
3 is saying "they were late and I still loved it" — averaging that to 4.5 puts words in their mouth, and it is
the number the provider is judged on.

*Alternative:* derive overall from dimensions (the option the user was offered and declined). Would have made
dimensions mandatory, which costs completion rate.

### D3. Moderation is its own axis, next to `IsVerified`
New `ModerationStatus` enum column (`Pending`/`Published`/`Rejected`/`Hidden`) plus
`ModeratedAt`, `ModeratedBy`, `ModerationReason`. A second, independent `ReplyModerationStatus` for the
provider reply. `IsVerified` keeps its existing meaning — "came from a completed booking" — and is never
touched by a moderation action.

*Why:* these answer different questions. Folding them together means hiding an abusive review also brands it
unverified and silently decrements the verified-review count that `ReviewStatistics` reports.

*Alternative:* reuse `IsVerified` as the visibility flag and drop the new column — rejected for exactly the
corruption above; `Unverify()` already exists and is the trap.

### D4. Votes are a separate table, not an owned collection
`ReviewVotes(ReviewId, UserId, IsHelpful, CreatedAt)` with a unique index on `(ReviewId, UserId)` and a
foreign key to `Reviews`.

*Why:* the unique index is what actually enforces one-vote-per-user under concurrency — a check-then-insert in
the handler races. A separate table also means casting a vote does not load every existing vote into the
aggregate, and it avoids the owned-collection key hazard from D1.

Denormalised `HelpfulVoteCount`/`NotHelpfulVoteCount` live on `Reviews`, written in the same transaction as
the vote, so listing reviews stays one query.

*The concurrency hazard here is a lost update, not a version conflict.* `AggregateRoot.Version` is bumped
only inside `RaiseDomainEvent`, so a read-modify-write of the denormalised counters is not protected by the
concurrency token. The vote path must either issue an atomic SQL increment (`ExecuteUpdate`) or re-derive
the counts from `ReviewVotes` inside the same transaction. The implementation must record which.

### D5. Legacy counters are frozen as a baseline, not converted into votes
The existing `HelpfulCount`/`NotHelpfulCount` are frozen and never written again. Displayed count =
legacy + live vote count.

**The CLR properties are renamed to `LegacyHelpfulCount`/`LegacyNotHelpfulCount`; the database columns are
not.** Both are pinned with `.HasColumnName("HelpfulCount")` / `.HasColumnName("NotHelpfulCount")`, exactly
as `ReviewConfiguration` already pins `Id` to `"ReviewId"`. This produces no schema operation at all, which
is what makes a rolling deploy safe — see the Migration Plan. Freezing is then enforced by there being no
writer: `Review.MarkAsHelpful()` and `MarkAsNotHelpful()` are **deleted** when `CastReviewVoteCommand` lands.

**legacy + live is a domain rule, not a display rule.** `Review.GetHelpfulnessRatio()` and
`IsConsideredHelpful()` read the raw counters today and are surfaced per review in the API response, and
`ReviewReadRepository` orders `sortBy="helpful"` on the raw column. If only the display adds the two
together, every review created after the migration has a legacy baseline of 0/0 and can therefore never be
"considered helpful" however many live votes it collects, while the helpful sort freezes at the deploy-day
ordering. Both derivations and the ORDER BY move to `(Legacy + Live)`; if the sort must stay indexable, a
persisted computed column. Dropping the ratio fields from the response contract is an acceptable
alternative. Leaving them silently frozen is not.

*Why:* those numbers have no users behind them. Inventing synthetic vote rows to back them would fabricate a
record of who voted, and dropping them would silently delete visible product state. Freezing is the only
option that neither lies nor destroys. It also makes "withdrawing a vote cannot take the count below the
baseline" fall out arithmetically instead of needing a floor check.

### D6. Aggregates are recomputed, not incremented — inside the command's own transaction
On publish / unpublish / hide / restore / edit-approval / removal, a grouped query over that provider's
published reviews overwrites the stored aggregates.

**Each moderation command handler calls the recompute service directly, after the state change and before
commit, on the command's own `DbContext`.** A domain event handler cannot be used: `EfCoreUnitOfWork`
dispatches domain events *before* it saves, and `SimpleDomainEventDispatcher` opens a fresh DI scope per
event, so the handler would run on a different connection, outside the command's transaction, against a
row that has not been written yet. Approving a 4.0★ review would write an average that excludes it — every
aggregate permanently one action stale. Switching to `SaveAndPublishEventsAsync` does not fix it either:
under `TransactionBehavior` the ambient transaction is still uncommitted when the handler runs. Calling it
inline also means a rolled-back moderation rolls back the aggregate with it.

The `ReviewPublishedEvent`/`ReviewUnpublishedEvent` domain events still exist, for notifications and
auditing. They are simply not the recompute trigger.

*Why:* incremental deltas drift, and there is no reconciliation job here to catch the drift. Hiding, unhiding
and editing all mutate the set, so every one of them needs a correct inverse — five chances to be subtly
wrong versus one query that is right. Moderation actions are per-provider and rare; the cost is one indexed
`GROUP BY`.

*But the trigger is not always a moderator.* An author editing a published review unpublishes it, which
recomputes — and that is an ordinary customer, repeatable for the whole 7-day window. The edit endpoint
therefore needs a tight per-caller rate limit (D12), and a maximum-edits-per-review invariant is worth
considering in the domain.

*Alternative:* incremental counters with a nightly reconciliation job — more moving parts, and it means the
number is knowingly wrong between runs.

### D7. Where aggregates live: overall on `Provider`, dimensions beside it
`Provider.AverageRating` (existing) and a new `Provider.PublishedReviewCount` stay on `Providers`, because
`SearchProvidersQueryHandler` sorts on them and search must not join. The four dimension averages and their
counts go in a companion `ProviderRatingSummary` table read only by the profile endpoint.

*Why:* the search table is the hot path; widening it with eight columns only the profile renders is the wrong
trade.

### D8. "No rating" is signalled by the count, not by a nullable average
`AverageRating` stays non-nullable at the database level **and on the wire**; `PublishedReviewCount == 0` is
the discriminator, returned to clients as the real `totalReviews` count.

*Revised during implementation (2026-09-22).* This first said the API would return `rating: null` for an
unrated provider. Checking the shipped clients killed that: the deployed Vue app calls `rating.toFixed(1)`
unguarded in several places (`ProviderSelection.vue`, `FavoriteProviderCard.vue`, `favorites.types.ts`,
`platform.service.ts`), so a null would throw in production. Populating the count instead is additive — old
clients keep working, and the Flutter customer app's existing `hasRating(rating, reviewCount)` guard becomes
correct with no client change at all.

*Why:* making a non-nullable column on a live, hot table nullable is a heavier migration than this is worth,
and the count has to exist anyway. The API contract is what clients read, and that contract is honest.

*Trade-off accepted:* the stored column will read 0.0 for an unrated provider. Nothing may read it without
also reading the count. This is a real footgun and is why the sort in D9 is specified explicitly.

### D9. Rating sort orders rated providers; unrated are a band, in both directions
`ORDER BY` puts `PublishedReviewCount = 0` providers in their own band. **Unrated providers sort last
whichever direction is requested** — below the worst-rated on descending, and below the best-rated on
ascending too. "Not interleaved as zero" alone is not implementable; the direction has to be stated.

*Why:* without this, D8's stored zero silently makes every new salon the worst-rated business on the
platform — and a naive `ASC` would make them the *best*, which is worse.

**This is server-side work, not a client fix.** Ordering happens in `SearchProvidersQueryHandler`, in both
the `"rating"` branch and the no-coordinates `"distance"` fallback; every client only passes `sortBy`. The
review count is server-side too: `ProviderSearchResponse.TotalReviews` is declared but never assigned —
its source property on `ProviderSearchItem` is commented out — so it ships as a permanent `0`. That
collapses the Flutter `ProviderRating.hasRating(rating, reviewCount)` guard to `rating > 0`, which is
precisely D8's stored zero. `PublishedReviewCount` must be projected through it.

### D10. Withdrawal fires on submission, via the existing raiser
The create-review handler calls
`INotificationRaiser.WithdrawPendingForSubjectAsync("Booking", bookingId)` — signature verified against the
interface, not assumed — inside the same unit of work as the review write.

*Why on submission rather than publication:* a review sitting in the moderation queue is a review the customer
has already written. Waiting for approval would send them "you haven't reviewed yet" while they wait.

*Known bluntness, per the notification-system owner:* that method withdraws **everything** unsent for the
booking, not just review reminders. Among outbox rows the sets do not overlap — appointment reminders are
already withdrawn at completion, and a review request only exists after completion. That remains safe by
circumstance rather than by design, so the wiring task re-verifies it rather than inheriting this sentence.

**There used to be a second store, and withdrawal could not reach it — now resolved.**
`BookingCompletedNotificationHandler` was auto-registered by assembly scan and scheduled a *second* review
request on every completion — a legacy `Notification` row with an English HTML "How was your experience?"
body — while `CompleteBookingCommandHandler` already raised the outbox `ReviewRequest`.
`CancelPendingForSubjectAsync` only ever touched `NotificationOutbox`, so withdrawal could not have reached
it.

The blast radius was narrower than it first looked: that row was **visible but never sent**.
`ScheduledNotificationService`, which would have dispatched it, is registered inside
`AddNotificationBackgroundServices`, and nothing calls that method — the code says so in its own doc
comment, and it is FOLLOW-UPS #67. So a customer could see an English review request in their inbox and
never receive one by SMS, email or push.

Both halves are fixed, by the `notification-system` session on 2026-09-21: the handler is deleted (its own
task 7.4, "remove each superseded handler once its outbox coverage is in place"), and the inbox now filters
to Sent/Delivered/Read with a test asserting the list and the unread badge agree. There is no second store
for review requests any more, so withdrawal against the outbox alone is sufficient.

The test for this still belongs at the inbox boundary rather than on the outbox table: complete a booking,
submit the review, assert `GET /notifications/inbox` returns no review-request item for that booking. An
outbox-table assertion would pass while a user-visible duplicate survived, which is exactly how this one
lived so long.

### D11. Authorisation follows the existing shapes
Moderation: `[Authorize(Policy = "AdminOnly")]` — never a raw `Roles = …` list, for the incident reason in
Context. Reply: authenticated, plus a resource check that the caller's provider owns the reviewed business,
gated the way `BookingsController.CanManageProvider` already does it — administrators explicitly may **not**
reply, since a reply is published speech attributed to the business. The owner-scoped review listing uses
that same check. Vote and report: authenticated.

The admin-only test must not use the shared `TestUser.Admin`: it carries all three role spellings and
would pass against any of them, which is exactly how the 2026-09-19 incident escaped. Follow
`AdminRoleNameTests` and assert reachability with a token carrying only `"Admin"` *and* one carrying only
`"Administrator"`.

### D12. Every new write path gets a named rate-limit policy
Vote, report, edit, reply add/edit/remove and each moderation action get their own policy name alongside
the existing `create-review` / `mark-review-helpful` / `provider-reviews`. Edit gets the tightest ceiling,
because D6 makes it a customer-triggered unpublish plus a full per-provider `GROUP BY`.

*Mechanism, easy to get wrong:* each new name must also be added to `RateLimitingOptions.Defaults` —
`RateLimitingRegistration` registers exactly that table, and `[EnableRateLimiting]` naming an unregistered
policy throws at request time. It throws in the test host too, where `Enabled=false` still registers each
name as a no-op limiter. Without this the failure is a 500 on first call, not a missing limit.

Claims are read as `ClaimTypes.NameIdentifier` first. Production tokens carry the identity as
`nameidentifier`, not `sub`/`userId`; `ReviewsController` already does this and the pattern must not regress.

## Risks / Trade-offs

- **Moderation is an ongoing human cost, and reviews are invisible until someone acts.** → No auto-hide;
  reports never remove content on their own. If the queue is unstaffed the failure mode is reviews not
  appearing, which is recoverable, rather than abuse appearing, which is not.
- **The read path becomes narrower — published only.** → The migration publishes every existing review, so no
  currently-visible content disappears at deploy.
- **The vote endpoint stops being anonymous.** Any shipped client calling it without a token starts getting
  401. → Audit the three clients before deploy; the Vue service already routes through the authenticated
  client, the Flutter datasource does not currently call it at all.
- **Recompute on a provider with thousands of published reviews.** → Indexed on `(ProviderId, ModerationStatus)`;
  never on read. Not only moderators trigger it, though — an author's edit does too, so the edit endpoint
  carries the tightest rate limit in D12.
- **`ReviewStatistics` currently averages every review regardless of state.** The read repository loads the
  provider's whole review set and builds the average, the star distribution and the verified count off it,
  and that block is returned on the public listing. Filtering the review *list* without filtering the
  statistics block would leave the public payload reporting an average that includes pending, rejected and
  hidden reviews — contradicting the moderation spec directly. → Filtered in the same task as the listing.
- **`AverageRating` reading 0.0 for unrated providers (D8).** → Every consumer must pair it with the count.
  This is the weakest point of the design and the one most likely to be got wrong later.
- **Two sessions editing ServiceCatalog concurrently.** → `notification-system` owns
  `Services/Notifications/` and the outbox; this change owns `Review*`. Coordinate before touching
  `NotificationEventCatalog`.

## Migration Plan

The host migrates forward-only at startup, the published image is runtime-only (no SDK, so no
`dotnet ef database update` in the container), and the compose healthcheck is `curl /health`, which never
touches `Reviews`. Anything that breaks the *previous* image's SQL therefore fails silently behind a green
container. That shapes every step below.

1. **Schema, additive only — no renames, no drops.** Add dimension columns, moderation columns,
   `ReviewVotes`, `ReviewReports`, `ProviderRatingSummary`, `Provider.PublishedReviewCount`. The counter
   columns keep their physical names `HelpfulCount`/`NotHelpfulCount`; only the CLR properties are renamed,
   pinned via `HasColumnName` (D5). A physical rename would make the prior image's mapping throw
   `42703 undefined_column` on every review read, while the healthcheck stayed green and rollback was
   impossible.
1b. **Every new NOT NULL column on an existing table carries a database-level DEFAULT** —
   `Reviews.ModerationStatus`, `ReplyModerationStatus`, `HelpfulVoteCount`, `NotHelpfulVoteCount`,
   `Providers.PublishedReviewCount`. The prior image's INSERTs omit them and would otherwise fail
   `23502 not_null_violation`.
2. **Backfill.** Every existing review → `ModerationStatus = Published`; every existing reply → published.
   Dimension columns stay NULL, which is correct: those customers were never asked.
3. **Recompute once.** Run the D6 aggregation across all providers so `AverageRating` becomes true for the
   first time, and `PublishedReviewCount` is populated.
4. **Deploy backend, then clients.** The read path is backward-compatible for a client that ignores the new
   fields. The vote endpoint is the only breaking call.
5. **Rollback.** With step 1 fully additive and step 1b's defaults in place, the prior image runs unchanged
   against the new schema — rollback is a redeploy, with no schema reversal at all. That is the entire
   reason the rename is logical rather than physical. No data is destroyed at any step, which is the point
   of D5.

   Mechanically, rollback is not yet a documented procedure: the production compose file pins `:latest`, so
   rolling back means editing it to the `-api:${sha_short}` tag and re-running `up -d`.
   `docs/DEPLOYMENT_RUNBOOK.md` has no rollback section today and gains one here.

6. **Post-deploy smoke.** Assert `GET /api/v1/reviews/providers/{id}` still returns published reviews, in
   the deploy job, shaped like the existing `tests/e2e/keystone-booking-flow.sh` gate. The healthcheck
   cannot see this and the backfill is the step most likely to be silently wrong.

Migrations are applied against shared databases through the repository's protected-operation prompt, never
silently.

## Open Questions

- **Is a rejected review's author told, and told why?** Silence is confusing; a reason is a support surface and
  an argument. Product decision, not mine. Note the consequence already in the spec: because withdrawal fires
  at submission (D10), a rejected author is never re-prompted.

  This question is load-bearing beyond this change. The `notification-system` owner has settled task 7.6 as
  *never re-ask a rejected author*, on the reasoning that the notification system cannot see why a review was
  rejected, so a blind re-ask either invites the refused content back or reads as "we ignored you, try again".
  That silence is defensible only if the rejection is communicated somewhere — and that somewhere is this
  flow, not theirs. If this question is answered "no, say nothing", the platform silently swallows customer
  reviews with no signal at all. Flagged jointly on 2026-09-21; unowned until a product decision lands.
- **Should a low overall rating prompt for dimensions?** A common pattern is to ask "what went wrong" when the
  overall is ≤ 3, which is where dimension data is most useful and least often volunteered. Deliberately not
  specified; it is a UX decision with a completion-rate cost.
- **Catalogue entries for the two new notifications.** A new published review (→ provider) and a provider reply
  (→ customer) both need audience, channels and criticality stated in `NotificationEventCatalog`, which throws
  rather than defaulting. Neither wants SMS — that is reserved for critical notifications and would fail the
  catalogue's self-validation test. Owner of that file is the `notification-system` session; agreed to settle
  it at wiring time.
- **Does an edited-and-re-approved review keep its votes?** The spec says votes survive hide/unhide. Editing
  changes what was voted on. Leaning keep-and-flag over silently discarding, but unresolved.
