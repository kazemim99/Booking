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
When an author edits a published review, it SHALL leave the published state, return to pending, and be excluded from the provider's aggregates until it is approved again.

#### Scenario: Published review is edited
- **WHEN** the author edits a published review within the edit window
- **THEN** the review returns to pending, leaves the public listing, and the provider's aggregates are recomputed without it

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

### Requirement: Reviews that exist before moderation are published, not hidden
Reviews already stored when moderation is introduced SHALL be migrated to the published state. Retroactively hiding reviews that are already visible in production, and that providers have already seen, would silently remove standing content and destroy the only rating signal the platform has.

#### Scenario: Existing review after migration
- **WHEN** the moderation migration runs against a database holding existing reviews
- **THEN** every existing review is recorded as published and remains visible in the provider's public listing

#### Scenario: Existing provider replies after migration
- **WHEN** the migration runs against reviews that already carry provider replies
- **THEN** those replies are recorded as published and remain visible
