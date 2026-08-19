# Proposal: payment-consistency-and-idempotency

## Why

The audit confirmed the payment execution path is **not crash-consistent and not reliably idempotent** — unacceptable for real money.

- The external gateway call runs **inside** the command's DB transaction, and that transaction runs inside a **retried execution strategy** (`ProcessPaymentCommandHandler.cs:70`, `RefundPaymentCommandHandler.cs:61`, `EfCoreUnitOfWork.ExecuteInTransactionAsync`). If the commit fails after a successful charge → money moves with **no persisted record**. If a transient DB error triggers a retry → the gateway is charged **again**.
- `IdempotencyBehavior` is a **non-atomic** check-then-act with no in-flight lock and is **opt-in** (`ProcessPaymentCommand.IdempotencyKey` defaults null) — concurrent duplicates both execute.
- There is **no per-booking payment uniqueness** in the domain or DB (`PaymentConfiguration.cs:265` is a non-unique `BookingId` index).
- `BehpardakhtService` returns **mock success** without charging (`BehpardakhtService.cs:55,88`); if ever selected it records phantom-paid bookings.

(The ZarinPal *verify* path is already idempotent via a `Status==Paid` short-circuit — that is preserved.)

## What Changes

- Adopt a **payment-intent + outbox** flow: persist intent → call gateway **outside** any DB transaction → persist result idempotently → publish `PaymentProcessed` via the existing CAP outbox.
- Replace the cache check-then-act with an **atomic idempotency reservation** (insert-first / `SET NX`), and make an idempotency key **mandatory** for payment/refund commands.
- Add **DB-level payment uniqueness per booking** (partial unique index over non-failed statuses).
- Make `PaymentGatewayFactory` **fail closed** for non-functional gateways (Behpardakht/Parsian/Saman) unless an explicit stub flag is set (default off).

## Capabilities

### New Capabilities
- `payment-processing-integrity`: money-moving operations SHALL be crash-consistent — no charge without a recoverable record, no external call inside a retried DB transaction, and unrecorded charges SHALL be reconcilable.
- `payment-idempotency`: identical payment/refund operations SHALL be de-duplicated atomically (concurrent and sequential), keyed by a mandatory idempotency key, with at most one gateway side effect and one successful payment per booking.

### Modified Capabilities
- (none — new payment specs; existing feature specs describe flows, not the persistence mechanism.)

## Impact

- **Code**: new `PaymentIntent` lifecycle (or Payment `Pending` phase); refactor `ProcessPayment`/`CreateZarinPalPayment`/`RefundPayment` handlers to call the gateway outside the transaction; new atomic idempotency store; `PaymentGatewayFactory` guard.
- **DB**: `PaymentIntents` / idempotency-reservation tables; partial unique index on `Payments(BookingId)` (non-failed).
- **API**: payment/refund commands require an `Idempotency-Key` header; duplicates return the original result (no new charge).
- **Flutter**: send a stable idempotency key per checkout attempt (consumed by C4).
- **Security**: removes phantom-charge and double-charge vectors; stub gateway can no longer fake success in prod.
- **Depends on**: pairs with `financial-ledger-and-settlement` (C5) for the reconciliation sweep of charged-but-lost intents.
