## Context

`Review` already exists as an aggregate root in ServiceCatalog with a single `RatingValue`, an optional
comment, an unused `ProviderResponse` pair and two bare integer counters. It has an EF configuration, two
repositories, three endpoints and a seeder. It has no tests. Production holds real review rows.

Three things about the current state shape every decision below:

1. **`Provider.AverageRating` is written by nothing.** It is `internal set`, and the only assignment in the
   repository is inside `ReviewSeeder`. `SearchProvidersQueryHandler` sorts by it. Every rating on every
   card in production is therefore either seed data or zero.
2. **`HelpfulCount`/`NotHelpfulCount` have no provenance.** They were incremented by an `[AllowAnonymous]`
   endpoint with no per-user record, so there is no way to reconstruct who voted.
3. **`notification-system` is in flight in a parallel session** and owns `NotificationEventCode.ReviewRequest`
   and its 2h/3-day reminder pair (its task 7.6). This change must call into that surface, not rebuild it.

Constraints: one PostgreSQL database, schema-per-context; ServiceCatalog owns both `Provider` and `Review`,
so this is intra-context and needs no CAP integration event. Admin authorisation in this codebase is
`[Authorize(Roles = "Admin,SysAdmin")]` / `Policy = "ProviderOrAdmin"`, as `BookingsController` uses.

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

### D5. Legacy counters are frozen as a baseline, not converted into votes
Migration renames the existing `HelpfulCount`/`NotHelpfulCount` to `LegacyHelpfulCount`/`LegacyNotHelpfulCount`
and leaves them immutable forever. Displayed count = legacy + live vote count.

*Why:* those numbers have no users behind them. Inventing synthetic vote rows to back them would fabricate a
record of who voted, and dropping them would silently delete visible product state. Freezing is the only
option that neither lies nor destroys. It also makes "withdrawing a vote cannot take the count below the
baseline" fall out arithmetically instead of needing a floor check.

### D6. Aggregates are recomputed, not incremented
On publish / unpublish / hide / edit-approval / removal, a domain event handler runs a grouped query over that
provider's published reviews and overwrites the stored aggregates.

*Why:* incremental deltas drift, and there is no reconciliation job here to catch the drift. Hiding, unhiding
and editing all mutate the set, so every one of them needs a correct inverse — five chances to be subtly
wrong versus one query that is right by construction. Moderation actions are rare and per-provider; the cost
is one indexed `GROUP BY` on an action a human just took.

*Alternative:* incremental counters with a nightly reconciliation job — more moving parts, and it means the
number is knowingly wrong between runs.

### D7. Where aggregates live: overall on `Provider`, dimensions beside it
`Provider.AverageRating` (existing) and a new `Provider.PublishedReviewCount` stay on `Providers`, because
`SearchProvidersQueryHandler` sorts on them and search must not join. The four dimension averages and their
counts go in a companion `ProviderRatingSummary` table read only by the profile endpoint.

*Why:* the search table is the hot path; widening it with eight columns only the profile renders is the wrong
trade.

### D8. "No rating" is signalled by the count, not by a nullable average
`AverageRating` stays non-nullable at the database level; `PublishedReviewCount == 0` is the discriminator,
and the API returns `rating: null` in that case.

*Why:* making a non-nullable column on a live, hot table nullable is a heavier migration than this is worth,
and the count has to exist anyway. The API contract is what clients read, and that contract is honest.

*Trade-off accepted:* the stored column will read 0.0 for an unrated provider. Nothing may read it without
also reading the count. This is a real footgun and is why the sort in D9 is specified explicitly.

### D9. Rating sort orders rated providers; unrated are not zero
`ORDER BY` puts `PublishedReviewCount = 0` providers in their own band rather than interleaving them at 0.0.

*Why:* without this, D8's stored zero silently makes every new salon the worst-rated business on the platform.

### D10. Withdrawal fires on submission, via the existing raiser
The create-review handler calls
`INotificationRaiser.WithdrawPendingForSubjectAsync("Booking", bookingId)` — signature verified against the
interface, not assumed — inside the same unit of work as the review write.

*Why on submission rather than publication:* a review sitting in the moderation queue is a review the customer
has already written. Waiting for approval would send them "you haven't reviewed yet" while they wait.

*Known bluntness, per the notification-system owner:* that method withdraws **everything** unsent for the
booking, not just review reminders. Today the sets do not overlap — appointment reminders are already
withdrawn at completion, and a review request only exists after completion — so this is safe. It is safe by
circumstance rather than by design, so the wiring task must re-verify it rather than inherit this sentence.

### D11. Authorisation follows the existing shapes
Moderation: `[Authorize(Roles = "Admin,SysAdmin")]`. Reply: authenticated, plus a resource check that the
caller's provider owns the reviewed business — administrators explicitly may **not** reply, since a reply is
published speech attributed to the business. Vote and report: authenticated.

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
  triggered only by a human moderation action, never on read.
- **`AverageRating` reading 0.0 for unrated providers (D8).** → Every consumer must pair it with the count.
  This is the weakest point of the design and the one most likely to be got wrong later.
- **Two sessions editing ServiceCatalog concurrently.** → `notification-system` owns
  `Services/Notifications/` and the outbox; this change owns `Review*`. Coordinate before touching
  `NotificationEventCatalog`.

## Migration Plan

1. **Schema, additive only.** Add dimension columns, moderation columns, `ReviewVotes`, `ReviewReports`,
   `ProviderRatingSummary`, `Provider.PublishedReviewCount`. Rename the two counter columns to their `Legacy*`
   names. No drops.
2. **Backfill.** Every existing review → `ModerationStatus = Published`; every existing reply → published.
   Dimension columns stay NULL, which is correct: those customers were never asked.
3. **Recompute once.** Run the D6 aggregation across all providers so `AverageRating` becomes true for the
   first time, and `PublishedReviewCount` is populated.
4. **Deploy backend, then clients.** The read path is backward-compatible for a client that ignores the new
   fields. The vote endpoint is the only breaking call.
5. **Rollback.** Steps 1–3 are additive apart from the counter rename, so rollback is a redeploy of the prior
   image plus reversing that rename. No data is destroyed at any step, which is the point of D5.

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
