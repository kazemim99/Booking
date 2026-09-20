# review-engagement

## ADDED Requirements

### Requirement: Voting on a review requires authentication
Casting, changing or withdrawing a vote on a review SHALL require an authenticated user. Anonymous callers SHALL be able to read vote counts but SHALL NOT be able to affect them.

#### Scenario: Anonymous caller attempts to vote
- **WHEN** a caller with no valid token votes on a review
- **THEN** the request is rejected as unauthorized and no count changes

#### Scenario: Anonymous caller reads a review
- **WHEN** a caller with no valid token reads a provider's published reviews
- **THEN** the helpful and not-helpful counts are returned and no vote of their own is reported

### Requirement: One vote per user per review, changeable and withdrawable
A user SHALL hold at most one vote on a given review, either helpful or not helpful. Casting a vote where one already exists SHALL replace it rather than add to it. Casting the same vote that is already held SHALL withdraw it, so the control is a toggle. Counts SHALL always equal the number of distinct users currently holding each vote, plus any preserved legacy baseline.

#### Scenario: First vote
- **WHEN** a user marks a review helpful for the first time
- **THEN** the helpful count increases by one and their vote is recorded as helpful

#### Scenario: Repeating the same vote withdraws it
- **WHEN** a user marks a review helpful when they have already marked it helpful
- **THEN** their vote is removed and the helpful count decreases by one

#### Scenario: Changing a vote moves it
- **WHEN** a user marks a review not helpful when they had marked it helpful
- **THEN** the helpful count decreases by one, the not-helpful count increases by one, and the totals never double-count them

#### Scenario: Repeated requests cannot inflate a count
- **WHEN** the same user sends fifty helpful votes for the same review
- **THEN** the review ends with at most one vote from that user

#### Scenario: Two users voting
- **WHEN** two different users mark the same review helpful
- **THEN** the helpful count is two

### Requirement: A user cannot vote on their own review
The author of a review SHALL NOT be able to vote on it.

#### Scenario: Author votes on their own review
- **WHEN** the author marks their own review helpful
- **THEN** the request is rejected as forbidden and no count changes

### Requirement: Votes are only accepted on publicly visible reviews
A vote SHALL be accepted only on a published review. A review that is pending, rejected or hidden SHALL NOT accept votes.

#### Scenario: Voting on a pending review
- **WHEN** a user votes on a review that is awaiting moderation
- **THEN** the request is rejected and no vote is recorded

#### Scenario: A published review is later hidden
- **WHEN** a review holding votes is hidden by an administrator
- **THEN** the existing votes are retained and reappear unchanged if it is published again

### Requirement: A reader is told their own current vote
A review returned to an authenticated user SHALL report which vote that user currently holds on it, if any, so that the control can render its state without a second request.

#### Scenario: Reader who has voted
- **WHEN** an authenticated user who has marked a review helpful reads the provider's reviews
- **THEN** that review is returned with their current vote reported as helpful

#### Scenario: Reader who has not voted
- **WHEN** an authenticated user who has not voted reads the provider's reviews
- **THEN** each review is returned with no vote of their own reported

### Requirement: Pre-existing counter values are preserved as an opaque baseline
The helpful and not-helpful counters that exist before per-user voting was introduced have no record of who cast them and SHALL NOT be converted into votes. Each review SHALL retain its pre-existing counts as a separate baseline that is added to the live vote tallies for display. A user voting on such a review SHALL NOT be treated as having already voted.

#### Scenario: Migration preserves existing counts
- **WHEN** the voting migration runs against a review showing 7 helpful and 2 not helpful
- **THEN** the review still displays 7 helpful and 2 not helpful, held as a baseline, with no votes attributed to any user

#### Scenario: New vote adds to the baseline
- **WHEN** a user marks a review helpful that carries a baseline of 7 helpful
- **THEN** the review displays 8 helpful

#### Scenario: Withdrawing a vote cannot erode the baseline
- **WHEN** a user withdraws their helpful vote on a review carrying a baseline of 7 helpful
- **THEN** the review displays 7 helpful and never fewer
