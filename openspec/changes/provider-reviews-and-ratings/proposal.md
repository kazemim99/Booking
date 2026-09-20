## Why

Booksy ships a review feature that does not work. A half-built `Review` aggregate exists, it is seeded
with convincing Persian data, and every client renders a rating — but in production
`Provider.AverageRating` is never written by any code path, so every provider card, every "sort by
rating" result and every profile header shows a number that means nothing. Around it: the provider can
never reply (the domain method exists with no caller), anyone on the internet can inflate a review's
helpful count without logging in and without limit, the web app's edit-review dialog calls an endpoint
that returns 404, and not one test covers any of it.

At the same time the product needs more than one star. A salon is not good or bad as a single scalar:
customers choose on hygiene, on whether the result was skilled, on whether their time was respected and
on how they were treated — and a provider cannot act on "3.5 stars" the way they can act on "punctuality
is your weak dimension". Supply-side trust is the keystone of the booking funnel, and right now the
platform has no honest signal to show.

## What Changes

**Rating becomes multi-dimensional.** One required overall star rating, plus four optional dimensions —
cleanliness and hygiene, skill and quality of work, punctuality, conduct and manner. The overall score
stays the customer's own statement rather than a computed average, so it can never disagree with what
they thought they said. Dimensions are collapsed behind a disclosure so the required path stays a single
tap; anything past four to five dimensions measurably collapses completion.

**Reviews are moderated before they are public.** A submitted review enters a pending state and is
published only on admin approval. **BREAKING** for the current read path: `GET /reviews/providers/{id}`
today returns everything ever written, and will return only published reviews.

- Moderation status is a **separate axis from `IsVerified`**. `IsVerified` means "came from a real
  completed booking"; moderation means "an admin cleared it for display". Collapsing them would make
  hiding an abusive review also brand it unverified, and would silently corrupt the verified-review count.
- An admin queue, plus a report path so a provider or customer can escalate an already-published review.

**The provider can reply — for real.** A reply endpoint wired to the existing `AddProviderResponse`
domain method, authorised so that only the owning provider may reply, exactly one reply per review,
editable and removable by that provider. Replies are moderated on the same path as reviews.

**Like/dislike becomes a real vote.** One vote per authenticated user per review, changeable and
withdrawable, stored per user instead of as an unguarded counter. **BREAKING**: the endpoint stops being
`[AllowAnonymous]` and stops accepting unlimited repeat votes from the same caller. Existing
`HelpfulCount`/`NotHelpfulCount` totals have no per-user provenance and cannot be migrated into votes;
they are carried forward as an opaque legacy baseline rather than invented.

**Provider rating aggregates get written.** Publishing, editing, hiding or removing a review recomputes
the provider's overall average, per-dimension averages and review count. This is what makes discovery
honest for the first time.

**Review editing gets a backend.** A bounded edit window for the author; an edited review re-enters
moderation. This closes the 404 the web client already ships against.

**Eligibility is unchanged and deliberate**: only the customer of a `Completed` booking may review, one
review per booking. Kept as-is because it is what makes every review verified by construction and what
keeps competitor and bot reviews out.

## Capabilities

### New Capabilities

- `provider-reviews`: who may review and when, the overall-plus-dimensions rating model, the comment,
  the author's edit window, the provider's reply, and the provider rating aggregates derived from
  published reviews.
- `review-moderation`: the pending → published/rejected lifecycle, the admin queue and its actions, the
  report path for published reviews, and the separation of moderation state from booking verification.
- `review-engagement`: one authenticated vote per user per review, changing and withdrawing a vote, the
  resulting counts, and the treatment of pre-existing counter values.

### Modified Capabilities

- `customer-discovery-journey`: the requirement that result cards show a rating currently resolves to a
  value no code writes. It changes to require a rating derived from published reviews, with an explicit
  "no reviews yet" state rather than a rating of zero standing in for absence.

## Impact

**Backend** (`Booksy.ServiceCatalog`, schema `ServiceCatalog`): `Review` aggregate gains dimension
scores and moderation state; new entities for votes and reports; new commands for reply, edit, vote,
moderate and report; `GetProviderReviews` gains dimension statistics and a published-only filter;
provider rating aggregates written on publish/unpublish. EF migrations against the live database —
reviews already exist in production, so the migration must backfill existing rows into a defensible
moderation state rather than hide them all.

**API** — new: provider reply, edit review, report review, moderation queue and decisions. Changed:
`PUT /reviews/{id}/helpful` becomes authenticated and idempotent per user; `GET
/reviews/providers/{id}` returns published only and gains dimension breakdowns.

**Clients**: `booksy-frontend` (write/edit review with dimensions, vote state, provider reply display),
`booksy-admin` (the moderation queue — new surface, pulled in by the approval decision),
`booksy-customer-app` (`features/reviews/` — dialog and section), `booksy-provider-app` (**no reviews
feature exists at all** — reading and replying is net-new).

**Coordination with `notification-system` (in flight, owned by another session).**
`NotificationEventCode.ReviewRequest` already exists, and that change's task 7.6 sends a review request
2 hours after completion plus one reminder 3 days later *only if no review was left*. Submitting a
review must therefore withdraw the pending intent via the existing
`INotificationRaiser.WithdrawPendingForSubjectAsync("Booking", bookingId)`, or the platform nags
customers who already reviewed. Two further notifications are ours to raise: the provider on a new
published review, and the customer on a provider reply. Verified against `INotificationRaiser` and
`notification-system/tasks.md` on 2026-09-21.

**Testing**: there is currently no review test of any kind. Integration coverage for the eligibility,
moderation and authorisation rules; unit coverage for the rating aggregation and vote state machine.

**Risk**: moderation is a standing human cost and a latency between writing a review and seeing it — a
trade accepted deliberately over an unguarded public write path.
