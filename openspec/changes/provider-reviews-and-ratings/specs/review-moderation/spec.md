# review-moderation

## ADDED Requirements

### Requirement: A review is not publicly visible until an administrator approves it
A submitted review SHALL enter a pending state and SHALL NOT appear in any public listing, in any provider rating aggregate, or to any user other than its author, the owning provider, and administrators. An administrator SHALL be able to approve it, at which point it becomes published, or reject it, at which point it stays permanently invisible to the public.

#### Scenario: Newly submitted review is not public
- **WHEN** a customer submits a review
- **THEN** it is recorded as pending and does not appear in the provider's public review listing

#### Scenario: Author sees their own pending review
- **WHEN** the author lists their own reviews while one is pending
- **THEN** that review is returned to them, marked as awaiting approval

#### Scenario: Owning provider sees a pending review about them
- **WHEN** the owning provider lists reviews of their business while one is pending
- **THEN** that review is returned to them, marked as awaiting approval

#### Scenario: Approved review becomes public
- **WHEN** an administrator approves a pending review
- **THEN** it appears in the provider's public review listing and is counted in the provider's aggregates

#### Scenario: Rejected review stays invisible
- **WHEN** an administrator rejects a pending review
- **THEN** it never appears in any public listing and is excluded from the provider's aggregates

#### Scenario: Public listing returns published reviews only
- **WHEN** any caller requests a provider's public reviews while that provider has pending, rejected, hidden and published reviews
- **THEN** only the published reviews are returned

### Requirement: Moderation state is independent of booking verification
A review SHALL carry its moderation state and its booking-verified flag as two independent values. Changing a moderation state SHALL NOT change whether the review is verified, and a review that came from a completed booking SHALL remain verified whether it is pending, published, rejected or hidden.

#### Scenario: Hiding a review does not unverify it
- **WHEN** an administrator hides a published review that came from a completed booking
- **THEN** the review remains marked verified and only its moderation state changes

#### Scenario: Verified review count counts published verified reviews
- **WHEN** a provider has published verified reviews and one hidden verified review
- **THEN** the reported verified review count includes only the published ones and the hidden one is not reported as unverified

### Requirement: Administrators can work a moderation queue
An administrator SHALL be able to list reviews awaiting moderation, oldest first, and for each one approve it, reject it with a reason, or hide an already-published review with a reason. Only users holding the platform administrator role SHALL be able to perform any moderation action or view the queue.

Administrator authorisation SHALL accept every role spelling the platform's admin policy accepts, rather than a narrower hand-written list. A production administrator holding only one of those spellings must not be refused.

#### Scenario: Queue lists pending items oldest first
- **WHEN** an administrator opens the moderation queue
- **THEN** reviews awaiting moderation are returned oldest first, with their ratings, comment, provider and author

#### Scenario: Non-administrator attempts to view the queue
- **WHEN** an authenticated user without the administrator role requests the moderation queue
- **THEN** the request is rejected as forbidden

#### Scenario: Provider attempts to moderate a review about their own business
- **WHEN** the owning provider attempts to approve, reject or hide a review of their business
- **THEN** the request is rejected as forbidden

#### Scenario: Hiding a published review
- **WHEN** an administrator hides a published review with a reason
- **THEN** the review leaves the public listing, the reason is recorded, and the provider's aggregates are recomputed

### Requirement: An edited review returns to the queue
When an author edits a review in the Pending or Published state, it SHALL be in the Pending state afterwards, SHALL NOT appear in the public listing, and SHALL be excluded from the provider's aggregates until it is approved again. A Rejected or Hidden review is not editable and this requirement does not apply to it.

If the edited review carries a published provider reply, that reply SHALL also return to pending, so that words the provider published against one review text are never displayed under a different one.

#### Scenario: Published review is edited
- **WHEN** the author edits a published review within the edit window
- **THEN** the review returns to pending, leaves the public listing, and the provider's aggregates are recomputed without it

#### Scenario: Edited review carries a published reply
- **WHEN** the author edits a published review that has a published provider reply
- **THEN** the reply returns to pending with it and is not displayed against the edited text

#### Scenario: Re-publication is distinguishable from first publication
- **WHEN** an administrator approves a review that had previously been published and was then edited
- **THEN** the provider is notified in a way that identifies it as a changed review rather than a new one

### Requirement: An administrator can restore a hidden review
An administrator SHALL be able to restore a Hidden review to Published. On restore it SHALL return to the public listing with its votes and counts intact, and the provider's aggregates SHALL be recomputed to include it. Hidden reviews SHALL be findable by an administrator; the moderation queue holds items awaiting moderation only, so hidden reviews SHALL be listed on their own surface.

Restoring a Rejected review is deliberately not supported: rejection is permanent.

#### Scenario: Administrator restores a hidden review
- **WHEN** an administrator restores a review that was hidden after a report
- **THEN** it reappears in the public listing, its helpful and not-helpful counts are unchanged, and the provider's aggregates are recomputed to include it

#### Scenario: Hidden reviews are findable
- **WHEN** an administrator looks for reviews that have been hidden
- **THEN** they are returned on a surface distinct from the pending queue, with the reason each was hidden

#### Scenario: A rejected review cannot be restored
- **WHEN** an administrator attempts to restore a rejected review
- **THEN** the request is rejected and the review stays permanently invisible

### Requirement: The author can list their own reviews across all states
The author SHALL be able to list the reviews they have written regardless of moderation state, on an authenticated surface distinct from the public provider listing. Each SHALL report its moderation state, and where the review was rejected or hidden, the reason recorded by the administrator.

#### Scenario: Author lists their reviews
- **WHEN** an authenticated customer lists their own reviews while holding one published, one pending and one rejected
- **THEN** all three are returned, each marked with its state

#### Scenario: Author sees why a review was rejected
- **WHEN** an author lists their own reviews and one was rejected with a reason
- **THEN** that reason is returned with it

#### Scenario: The list is scoped to the caller
- **WHEN** an authenticated customer lists their own reviews
- **THEN** no review written by any other customer is returned

### Requirement: The owning provider can list reviews of their business across all states
The owning provider SHALL be able to list reviews of their business regardless of moderation state, on an ownership-restricted surface distinct from the public listing, with each review's moderation state reported. The public listing SHALL NOT return unpublished reviews to any caller, including the owning provider, so that the two surfaces cannot be confused.

#### Scenario: Owning provider lists reviews of their business
- **WHEN** the owning provider lists reviews of their business while one is pending
- **THEN** the pending review is returned, marked as awaiting approval

#### Scenario: A different provider attempts the same listing
- **WHEN** a provider requests the review listing of a business they do not own
- **THEN** the request is rejected as forbidden

#### Scenario: The public listing stays public for the owner too
- **WHEN** the owning provider calls the public provider-reviews listing with their own token
- **THEN** only published reviews are returned, and the pending ones are reachable only on the owner-scoped surface

### Requirement: Public review statistics are computed over published reviews only
The statistics returned alongside a provider's public review listing — total, verified count, average rating, rating distribution, dimension averages and the counts behind them — SHALL be computed over published reviews only. A pending, rejected or hidden review SHALL NOT contribute to any of them.

#### Scenario: Statistics exclude unpublished reviews
- **WHEN** the public listing is requested for a provider holding four published reviews and three that are pending, rejected or hidden
- **THEN** the reported total is four and the average, distribution and verified count are computed over those four only

#### Scenario: Hiding a review moves the statistics
- **WHEN** an administrator hides a published review
- **THEN** the next public listing reports a total one lower and an average recomputed without it

### Requirement: A provider reply is moderated before it is public
A provider reply SHALL enter a pending state on being added or edited, SHALL NOT be publicly visible until approved, and SHALL be approvable or rejectable independently of the review it answers. A pending or rejected reply SHALL NOT prevent its review from being published.

#### Scenario: Reply is not public until approved
- **WHEN** the owning provider adds a reply to a published review
- **THEN** the review remains public without the reply, and the reply appears only after an administrator approves it

#### Scenario: Rejected reply leaves the review published
- **WHEN** an administrator rejects a provider reply
- **THEN** the reply never becomes public and the review it answers remains published

### Requirement: Anyone may report a published review
Any authenticated user SHALL be able to report a published review with a reason, and the owning provider SHALL be able to report a review of their business. A reported review SHALL remain public until an administrator acts on it. The same user SHALL NOT be able to report the same review more than once.

#### Scenario: Provider reports a review as abusive
- **WHEN** the owning provider reports a published review with a reason
- **THEN** the report is recorded, the review appears in the administrator's reported list, and the review remains public

#### Scenario: Reporting does not auto-hide
- **WHEN** a published review accumulates reports
- **THEN** it remains publicly visible until an administrator hides or rejects it

#### Scenario: Duplicate report by the same user
- **WHEN** a user reports a review they have already reported
- **THEN** the request is rejected and no second report is recorded

#### Scenario: Unauthenticated caller attempts to report
- **WHEN** a caller with no valid token reports a review
- **THEN** the request is rejected as unauthorized

#### Scenario: Administrator reviews what has been reported
- **WHEN** an administrator lists reported reviews
- **THEN** each reported review is returned with how many reports it has, the reasons given, and who reported it

### Requirement: Reviews that exist before moderation are published, not hidden
Reviews already stored when moderation is introduced SHALL be migrated to the published state. Retroactively hiding reviews that are already visible in production, and that providers have already seen, would silently remove standing content and destroy the only rating signal the platform has.

#### Scenario: Existing review after migration
- **WHEN** the moderation migration runs against a database holding existing reviews
- **THEN** every existing review is recorded as published and remains visible in the provider's public listing

#### Scenario: Existing provider replies after migration
- **WHEN** the migration runs against reviews that already carry provider replies
- **THEN** those replies are recorded as published and remain visible
