Status: ACTIVE
Verify: FULL

FOLLOW-UPS #48, re-diagnosed. The entry says the `DateTime` columns are
`timestamp without time zone` and that fixing it "carries a **migration on a shared database**".
Both halves are wrong, and the correction turns this into a code-only change with no protected
operation and no data conversion.

**Measured, not inferred.** Every timestamp column in both contexts is already
`timestamp with time zone`: 118 occurrences of `type: "timestamp with time zone"` across the
ServiceCatalog migrations and 44 across UserManagement, and **zero** occurrences of
`without time zone` anywhere under `src` outside prose comments. `timestamptz` stores an absolute
instant, normalised to UTC by Postgres, so no stored value is wrong and nothing needs converting.
The schema was right the whole time.

**What is actually broken** is one line repeated four times:
`AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` in `Booksy.Host/Program.cs`,
`Booksy.UserManagement.API/Program.cs`, `ServiceCatalog.Api/Startup.cs`, and — the one #48 does not
mention — `tests/Booksy.Tests.Commons/PostgresTestContainerFixture.cs`. Under that switch Npgsql
reads a `timestamptz` back as a `DateTime` with **`Kind=Local`**: the right instant, rendered in the
machine's own wall clock (+03:30 here). Everything downstream compares those values against
`DateTime.UtcNow` or against instants computed in process, and `DateTime` comparison ignores `Kind`,
so every such comparison is off by the server's UTC offset.

The OTP diagnostic printed `last sent 2026-09-11T00:52:32+03:30` — an `:o` round-trip format only
ever emits an offset for `Kind=Local`. That is the proof it is a read-side defect, not a storage one.

## Acceptance scenarios
- S1 A `DateTime` round-tripped through either context comes back `Kind=Utc` and equal to the instant written
- S2 The two `AvailabilityControllerTests` pass: a booked 10:00 slot reads as booked, not free — the double-booking risk #48 calls the serious one
- S3 `RescheduleBooking_WithValidNewTime` and `ScheduleNotification_WithValidData` pass
- S4 `tests/known-failures.txt` holds no test names
- S5 The OTP resend cooldown enforces, its `sinceLastSend >= Zero` guard is gone, and a test pins it — closes task 1b of `otp-and-endpoint-abuse-protection`
- S6 FULL verify green with no off-list failure

## Tasks
- [x] 1 Switch deleted from `Booksy.Host/Program.cs`, `Booksy.UserManagement.API/Program.cs`, `ServiceCatalog.Api/Startup.cs` and `PostgresTestContainerFixture`. Measured with it gone: UserManagement 40/40; ServiceCatalog 488 passed, 13 failed — and the reschedule and scheduled-notification baseline tests already passed
- [x] 2 All 13 were one error: `Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone'`, from query-string dates (`startDate='2026-07-13T00:00:00'`) in the availability and provider-earnings queries. Fixed once, in `UtcDateTimeConverter` applied by convention to both contexts, rather than per controller — see Decisions. All 22 tests in those classes pass, including both availability baseline tests
- [x] 3 `AvailabilityService` filtered slots with `slotStartUtc > DateTime.Now`, discarding every free slot in the next 3h30 on this machine. Now `DateTime.UtcNow`. The other `DateTime.Now` uses were checked and left: the Behpardakht gateway wants Iran local time; `ShouldNotifyToday` and the business-hours handler ask genuinely local-clock questions; `IDateTimeProvider.Now` has no callers; the design-time DbContext factories never run in a request
- [x] 4 Settled as the persistence rule, with no separate API-layer converter — see Decisions
- [x] 5 Guard deleted; two tests added (`A_Second_Send_Inside_The_Cooldown_Is_Refused_With_Retry_After`, `The_Cooldown_Is_Per_Phone`), with the cap at 100 so a 429 can only come from the cooldown. UserManagement 42/42. Closes task 1b of `otp-and-endpoint-abuse-protection`
- [x] 6 The four lines removed from `tests/known-failures.txt`, with the corrected diagnosis beside the old one. FOLLOW-UPS #48 and #10 closed in place with the wrong text kept. `openspec/project.md`'s note that the switch "is set in `Program.cs`" replaced with the rule that replaces it. Four unit tests pin the converter
- [ ] 7 FULL verify green

## Decisions
- 2026-09-11 **An unmarked `DateTime` means UTC, and that rule lives at the persistence boundary.** Considered: (a) fix each call site that produces `Kind=Unspecified`; (b) a JSON converter plus a model binder at the API edge; (c) one EF convention on every `DateTime` property. Chose (c). The invariant — every `DateTime` here is a UTC instant — is already assumed everywhere (`AvailabilityService` did `SpecifyKind(Unspecified, Utc)` by hand, "assume it's already meant to be UTC"), so an unmarked value can only be a UTC instant that lost its marker. (a) would leave the next controller taking a `DateTime` query parameter to rediscover the crash; (b) misses values made in process, like `DateOnly.ToDateTime(...)` in the cancel/reschedule/calendar handlers. EF converts query parameters with the converter of the column they are compared against, so (c) covers writes and filters in one place. `Local` values convert without loss. Reversible: delete the convention and the Npgsql exception returns
- 2026-09-11 **No response-format change needs a client change.** Timestamps now serialise as `…Z` where they used to carry `…+03:30`; same instant, and every client parses with a real ISO-8601 parser. Checked by grep: the Vue apps' `.split('T')` calls all format client-built `Date`s into request parameters, never a server response; the Flutter apps slice no timestamp strings, and their booking datasources already send `.toUtc().toIso8601String()`. The keystone e2e script sends `…Z`
- 2026-09-11 **Found, not fixed: an `ArgumentException` from the data layer answers 400.** That is how the 13 failures presented — as client errors, when they were server defects. Out of scope here; worth its own look at `ExceptionHandlingMiddleware`'s mapping
## Log
- 2026-09-11 Opened. Established by counting migration column types that #48's premise is wrong and no migration is needed, so this does not need the approval the previous session said it would.
