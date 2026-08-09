## Context

`TransactionBehavior` wraps every command in `ExecuteInTransactionAsync`, which opens a transaction (READ COMMITTED), runs the whole handler — including any external gateway HTTP call — then `SaveChanges` + commit, all inside `DbContext.Database.CreateExecutionStrategy().ExecuteAsync(...)` (which retries the delegate on transient errors). Idempotency is a Redis `GetString`/`SetString` around `next()`. Payment has no DB uniqueness. ZarinPal verify already short-circuits on `Status==Paid`.

## Goals / Non-Goals

**Goals:** no charge without a recoverable record; no external call inside a retried DB transaction; exactly-one gateway side effect per idempotency key; one successful payment per booking; stub gateways cannot fake success.

**Non-Goals:** replacing the gateway integrations; building the ledger (C5 consumes the events); the customer checkout UI (C4).

## Decisions

- **D1 — Intent + outbox, gateway call outside the transaction.** Flow: (a) *tx1*: create/find a `Payment` in `Pending` with the idempotency key and `BookingId`; (b) **no transaction**: call the gateway; (c) *tx2*: transition `Pending → Paid/Failed` idempotently and enqueue `PaymentProcessed` in the CAP outbox. *Rationale:* the external side effect is never inside a DB transaction or a retried strategy, and every charge has a durable `Pending` record that reconciliation (C5) can resolve. *Alternative rejected:* keeping the call in-tx with a shorter timeout — still loses records on commit failure.
- **D2 — Exclude payment/refund commands from `TransactionBehavior`'s auto-transaction.** Introduce an `INonTransactionalCommand` (or `IManagesOwnTransaction`) marker so these handlers manage their own two explicit transactions and never run inside the retried strategy around the gateway call.
- **D3 — Atomic idempotency reservation.** Replace check-then-act with insert-first semantics: a unique `(RequestType, IdempotencyKey)` row (DB table, or Redis `SET key val NX`). First writer proceeds; a concurrent/subsequent duplicate reads the stored result (or waits for the in-flight one) and returns it — never a second `next()`. Idempotency key becomes **required** for `ICommand`s marked `IRequireIdempotency`.
- **D4 — DB payment uniqueness.** Partial unique index `Payments(BookingId) WHERE Status IN ('Pending','Paid')` so a second concurrent charge for a booking fails at the DB even if application checks race. Handler maps the unique violation → "payment already in progress/complete" (409).
- **D5 — Gateway fail-closed.** `PaymentGatewayFactory` throws for gateways whose implementation is a stub/`NotImplemented` (Behpardakht mock, Parsian, Saman) unless `Payments:AllowStubGateways=true` (default false). Behpardakht's mock success path is gated behind the same flag.
- **D6 — Preserve ZarinPal verify idempotency** (`Status==Paid` short-circuit) and extend it to be concurrency-safe via the `Authority` lookup + the D4 uniqueness.

## Failure-correctness invariants (source of truth = DB, never the gateway)

Three invariants must hold under **any** failure:
- **I1 — Never lost/untraceable:** no gateway charge exists without a durable DB record. Guaranteed because the `Pending` intent is committed (tx1) *before* the gateway is ever called; if anything fails afterward, the intent remains and reconciliation resolves it by querying the gateway.
- **I2 — Never duplicated:** at most one captured (`Paid`/`PartiallyPaid`) payment per booking, and exactly one gateway side-effect per idempotency key. Guaranteed by the DB partial-unique index (D4) + atomic idempotency reservation (D3) + idempotent verify (D6).
- **I3 — Convergence:** the DB always converges to the gateway's actual state via the reconciliation sweep, which treats the gateway as a **read-only oracle** (query via `IPaymentGateway.GetPaymentDetailsAsync` / ZarinPal `VerifyPaymentAsync`), never as the record.

### Failure scenario → mechanism → invariant
| Failure | What happens | Mechanism that saves it | Invariant |
|---|---|---|---|
| Gateway **timeout** (no response) | Intent stays `Pending` | Reconciliation queries the gateway later and settles `Paid`/`Failed` | I1, I3 |
| **Network interruption after charge** (charged, confirm lost) | Money moved at gateway, DB still `Pending` | Reconciliation sees "verified/paid" and marks `Paid` — no loss | I1, I3 |
| **Duplicate callback** / double verify | Second verify finds `Status==Paid` | Idempotent verify short-circuit (D6) + `Authority`/`BookingId` uniqueness (D4) | I2 |
| **Process crash between stages** | If before tx1: nothing charged (safe). If after charge, before tx2: `Pending` intent persists | Intent-first ordering (D1) + reconciliation | I1 |
| **Retries under concurrency** | N concurrent identical requests | Atomic reservation (D3) → one `next()`; DB unique (D4) → one captured row | I2 |
| **Partial persistence failure** (tx2 commit fails post-charge) | Gateway charged, transition not saved | Gateway call is **outside** the tx (D1); intent remains `Pending`; reconciliation settles it | I1, I3 |

Fault-injection tests inject a controllable fake `IPaymentGateway`/`IZarinPalService` (the test factory already supports `RemoveAll<IPaymentGateway>()` + fake) to simulate each row and assert the DB ends in the correct state with no lost/duplicated money.

## Refinement (implementation): ship the non-regressive dedup first
- **D4-refinement — uniqueness on *captured* payments, not "live".** A unique index on `(BookingId) WHERE Status IN ('Pending','Paid')` would block a legitimate re-payment after an *abandoned* `Pending` until reconciliation expires it — a usability regression if reconciliation isn't yet live. So the first, safe-to-ship guarantee is `UNIQUE (BookingId) WHERE Status IN ('Paid','PartiallyPaid')` — **at most one captured payment per booking** (prevents the actual double-charge harm, permits Pending retries). The stronger "one live payment" form lands together with reconciliation (which expires abandoned intents), so retries never break. Rationale: deliver I2's core immediately without a new failure mode; complete I1/I3 with reconciliation next.

## Risks / Trade-offs

- [Intent without a resolved result if the app dies between charge and tx2] → Mitigation: reconciliation sweep (C5) queries the gateway by intent and settles `Pending` intents; intents older than a threshold are alerted.
- [Two transactions instead of one] → Trade-off accepted: money-safety > single-ACID-unit; the `Pending` record is the durability guarantee.
- [Making idempotency key mandatory breaks existing callers] → Mitigation: server-generate a key when absent during a deprecation window, log it, and require it after C4 ships the client key.
- [Unique index rejects legitimate re-payment after a failed attempt] → Mitigation: index excludes `Failed`/`Refunded`; a failed payment can be retried.

## Migration Plan

Additive migrations (intent + reservation tables, partial unique index created online). Verify no existing booking has two non-failed payments before adding the unique index (data audit + cleanup migration if needed). Feature flag `Payments:UseIntentFlow` (default on in non-prod first). Old handler retained one release.

## Open Questions

- Reservation store: dedicated DB table vs Redis `SET NX`. Lean DB table for durability + auditability, with the same 24h TTL semantics. **(Resolvable at implementation; no product input needed.)**
