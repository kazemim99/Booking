# Spec: aggregate-persistence-integrity

## ADDED Requirements

### Requirement: Appending an owned child to a materialized aggregate persists as an insert

Updating a materialized (tracked) aggregate that appends a new owned child entity SHALL persist the child as an
`INSERT` and SHALL NOT raise a false optimistic-concurrency failure. Owned child entities with domain-generated
(`Guid.NewGuid()`) keys SHALL be mapped `ValueGeneratedNever()` so EF Core does not mis-classify a newly-added
child as an existing row.

#### Scenario: Verifying a payment appends a transaction

- **WHEN** a `Pending` `Payment` is loaded and `VerifyPayment` is called (which appends a `Verification`
  `Transaction`) and the unit of work is committed
- **THEN** the commit succeeds, the payment status becomes `Paid`, and the new transaction row is inserted
- **AND** no `DbUpdateConcurrencyException` is raised

#### Scenario: Recording delivery attempts on a loaded notification

- **WHEN** a `Notification` is loaded and two delivery attempts are recorded and saved
- **THEN** both `NotificationDeliveryAttempts` rows are inserted with no optimistic-concurrency failure

#### Scenario: Adding a price tier to an existing service

- **WHEN** a persisted `Service` is reloaded, a new `PriceTier` is added, and the service is saved
- **THEN** the new `ServicePriceTiers` row is inserted with no optimistic-concurrency failure

### Requirement: Owned value-object instances are never shared across navigations

The domain SHALL NOT assign the same owned value-object CLR instance (e.g. `Money`) to more than one owned
navigation on the same or a child aggregate. A reference-distinct copy SHALL be used whenever the same value
flows into more than one owned slot.

#### Scenario: Capturing/verifying a payment copies the amount

- **WHEN** a domain method assigns the payment amount to a second owned slot (e.g. `PaidAmount = Amount`) or
  passes it to a child `Transaction`
- **THEN** a reference-distinct `Money` instance is stored in each slot
- **AND** EF Core does not raise `DuplicateDependentEntityTypeInstanceWarning` for that aggregate

### Requirement: Repository UpdateAsync is safe on tracked aggregates

`UpdateAsync` SHALL be a no-op for an entity already tracked by the current `DbContext` (change tracking has
already captured its mutations, including newly-added owned children as `Added`). It SHALL attach-and-mark only
genuinely detached entities.

#### Scenario: Refund flow calls UpdateAsync on a tracked payment

- **WHEN** a payment is loaded via the repository, refunded via the domain, passed to `UpdateAsync`, and
  committed
- **THEN** the commit succeeds, the payment status becomes `Refunded`, and the refund transaction is inserted
- **AND** `UpdateAsync` does not re-stamp the newly-added transaction as `Modified`
