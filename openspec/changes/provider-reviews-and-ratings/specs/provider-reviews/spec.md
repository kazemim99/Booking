# provider-reviews

## ADDED Requirements

### Requirement: Only the customer of a completed booking may review it
A review SHALL be created only by the customer who owns the booking, only while that booking is in `Completed` status, and at most once per booking. Every review SHALL therefore reference exactly one booking, and SHALL be marked verified by construction.

#### Scenario: The booking's customer reviews a completed booking
- **WHEN** the owning customer submits a review for a booking in `Completed` status that has no review
- **THEN** the review is created, linked to that booking and its provider, and marked verified

#### Scenario: A different user attempts to review the booking
- **WHEN** an authenticated user who does not own the booking submits a review for it
- **THEN** the request is rejected as forbidden and no review is created

#### Scenario: The booking has not been completed
- **WHEN** the owning customer submits a review for a booking in any status other than `Completed`
- **THEN** the request is rejected as a conflict naming the booking's current status

#### Scenario: The booking already has a review
- **WHEN** the owning customer submits a second review for the same booking
- **THEN** the request is rejected as a conflict directing them to edit the existing review

#### Scenario: An unauthenticated caller attempts to review
- **WHEN** a caller with no valid token submits a review
- **THEN** the request is rejected as unauthorized

### Requirement: A review carries one required overall rating and four optional dimensions
A review SHALL carry a required overall rating, and MAY carry a rating for each of four dimensions: cleanliness and hygiene, skill and quality of work, punctuality, and conduct and manner. Every rating SHALL be between 1.0 and 5.0 inclusive, in increments of 0.5. The overall rating SHALL be stored as the customer gave it and SHALL NOT be computed from the dimensions, so that what is displayed as their overall verdict is always what they actually stated. Each dimension SHALL be independently omittable.

#### Scenario: Overall rating only
- **WHEN** a customer submits a review with an overall rating and no dimension ratings
- **THEN** the review is accepted and every dimension is recorded as absent

#### Scenario: Overall rating with a subset of dimensions
- **WHEN** a customer submits an overall rating plus ratings for cleanliness and punctuality only
- **THEN** the review is accepted, those two dimensions are recorded, and skill and conduct are recorded as absent

#### Scenario: Overall rating is missing
- **WHEN** a customer submits a review with dimension ratings but no overall rating
- **THEN** the request is rejected as a validation error identifying the overall rating

#### Scenario: A rating is out of range or off-increment
- **WHEN** any submitted rating is below 1.0, above 5.0, or not a multiple of 0.5
- **THEN** the request is rejected as a validation error identifying which rating was invalid

#### Scenario: Dimensions never override the customer's overall verdict
- **WHEN** a customer submits an overall rating of 5.0 alongside dimension ratings averaging 3.0
- **THEN** the stored and displayed overall rating remains 5.0

### Requirement: A review may carry a comment
A review MAY carry a comment. When present it SHALL be between 10 and 2000 characters after trimming. Persian text SHALL be stored and returned without alteration.

#### Scenario: Review with a Persian comment
- **WHEN** a customer submits a review with a 40-character Persian comment
- **THEN** the comment is stored and returned exactly as submitted

#### Scenario: Comment below the minimum length
- **WHEN** a customer submits a comment shorter than 10 characters after trimming
- **THEN** the request is rejected as a validation error

#### Scenario: Review without a comment
- **WHEN** a customer submits ratings with no comment
- **THEN** the review is accepted with no comment

### Requirement: The author may edit a review within a bounded window
The author SHALL be able to edit their own review's ratings and comment for 7 days after it was created. After that window the review SHALL be immutable to its author. Only the author SHALL be able to edit their review.

Only a review in the Pending or Published state SHALL be editable. A Rejected review SHALL NOT be editable, because it is permanently invisible to the public and an edit would otherwise be a route back to publication that moderation has already refused. A Hidden review SHALL NOT be editable, because an edit would otherwise let the author undo an administrator's decision.

An edit to a Published review SHALL return it to Pending. An edit to a Pending review SHALL leave it Pending.

#### Scenario: Author edits inside the window
- **WHEN** the author edits their published review 2 days after creating it
- **THEN** the ratings and comment are updated and the review returns to pending moderation

#### Scenario: Author edits after the window
- **WHEN** the author edits their review 8 days after creating it
- **THEN** the request is rejected and the review is unchanged

#### Scenario: A different user attempts to edit
- **WHEN** an authenticated user who is not the author edits the review
- **THEN** the request is rejected as forbidden

#### Scenario: Author attempts to edit a rejected review
- **WHEN** the author edits their review that an administrator rejected, inside the 7-day window
- **THEN** the request is rejected, the review stays Rejected, and it does not return to the moderation queue

#### Scenario: Author attempts to edit a hidden review
- **WHEN** the author edits their review that an administrator hid, inside the 7-day window
- **THEN** the request is rejected and the review stays Hidden

### Requirement: The provider may reply to a review about them
The owning provider SHALL be able to add exactly one reply to a review of their business, and SHALL be able to edit or remove that reply. A reply SHALL be between 1 and 1000 characters after trimming. No user other than the owning provider SHALL be able to reply, including platform administrators and the review's author. A reply SHALL be subject to moderation on the same path as a review.

#### Scenario: The owning provider replies
- **WHEN** the owning provider adds a reply to a published review of their business
- **THEN** the reply is recorded with its timestamp and enters pending moderation

#### Scenario: A second reply to the same review
- **WHEN** the owning provider adds a reply to a review that already has one
- **THEN** the request is rejected as a conflict directing them to edit the existing reply

#### Scenario: A different provider attempts to reply
- **WHEN** a provider who does not own the reviewed business attempts to reply
- **THEN** the request is rejected as forbidden

#### Scenario: The review's author attempts to reply
- **WHEN** the customer who wrote the review attempts to reply to it
- **THEN** the request is rejected as forbidden

#### Scenario: The provider removes their reply
- **WHEN** the owning provider removes their reply
- **THEN** the reply and its timestamp are cleared and the review is returned without a reply

### Requirement: Provider rating aggregates derive from published reviews only
A provider SHALL carry an overall average rating, a per-dimension average rating, and a published review count, all derived exclusively from that provider's published reviews. These SHALL be recomputed whenever a review is published, edited, hidden, or removed. A dimension's average SHALL be computed only over reviews that rated that dimension. A provider with no published reviews SHALL be recorded as having no rating, which SHALL be distinguishable from a rating of zero.

#### Scenario: First review is published
- **WHEN** a provider with no published reviews has a review published with an overall rating of 4.0
- **THEN** the provider's overall average becomes 4.0 and the published review count becomes 1

#### Scenario: A review is hidden by moderation
- **WHEN** a published review is hidden
- **THEN** the provider's averages and count are recomputed excluding it

#### Scenario: Dimension averaged only over those who rated it
- **WHEN** three reviews are published and only two rated punctuality, at 4.0 and 5.0
- **THEN** the provider's punctuality average is 4.5 and is based on 2 ratings, not 3

#### Scenario: Provider with no published reviews
- **WHEN** a provider has no published reviews
- **THEN** the provider is reported as having no rating rather than a rating of 0

#### Scenario: Pending reviews do not move the average
- **WHEN** a review is submitted but not yet approved
- **THEN** the provider's averages and published review count are unchanged

### Requirement: Submitting a review withdraws the pending review request
Submitting a review SHALL withdraw any not-yet-sent notification intent for that review's booking, at the moment of submission rather than at publication, so that a customer waiting on moderation is never asked again to review something they have already reviewed.

#### Scenario: Review submitted while a reminder is scheduled
- **WHEN** a customer submits a review for a booking that has a scheduled review-request reminder
- **THEN** the pending reminder for that booking is withdrawn

#### Scenario: Withdrawal happens before moderation
- **WHEN** a customer submits a review that is held pending moderation
- **THEN** the reminder is already withdrawn and is not sent while the review waits for approval
