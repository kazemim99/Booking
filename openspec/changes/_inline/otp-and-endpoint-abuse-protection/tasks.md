Status: DONE
Verify: FULL

FOLLOW-UPS #50, corrected. The picture is not "no rate limiting anywhere" — `UseClientRateLimiting`
(AspNetCoreRateLimit) is wired and running in `Booksy.Host` and `ServiceCatalog.Api`. Three real gaps
sit behind it, and they are what this change closes.

## Acceptance scenarios
- S1 Sending an OTP to the same phone twice inside the cooldown is refused, and the SMS gateway is called once — the aggregate's own `CanResend()` rules apply to the anonymous send path
- S2 The thirteen `[EnableRateLimiting("...")]` policy names resolve to real policies; a caller exceeding one gets 429 with `Retry-After`
- S3 Two different anonymous callers do not share a rate-limit bucket
- S4 Limits are configuration, and the test host's values are permissive enough that the suites measure behaviour rather than throttling
- S5 FULL verify stays green with no off-list failure

## Tasks
- [x] 1a OTP send enforces the per-phone cap that `#if !DEBUG` used to compile away, and answers 429 with `Retry-After` instead of 400
- [-] 1b BLOCKED on FOLLOW-UPS #48: the 60-second cooldown is written and guarded off. Stored timestamps read back AHEAD of `UtcNow` by the server's offset, so elapsed time is negative and the rule would refuse every send forever — measured, with the diagnostic log still in place. Delete the `sinceLastSend >= Zero` guard and add its test when #48 lands
- [x] 2 All thirteen policies registered from one table (`RateLimitingOptions.Defaults`), partitioned per caller, limits bindable from configuration, master switch for test hosts
- [x] 3 `UseRateLimiter()` in all three hosts, after authentication so the partition can prefer the user id, with `Retry-After` on rejection
- [x] 4 `ClientRateLimitResolver` keys per caller — `user:{id}` or `ip:{addr}` — and the blanket rule is 300/min per caller instead of 100/min shared by everyone
- [x] 5 Test hosts raise the OTP limits and disable the limiter explicitly; three tests pin the cap, the per-phone isolation, and the suite's own configuration
- [x] 6 FULL verify green — 49e3babe, 959s, all 17 steps pass. ServiceCatalog integration: 497 passed, 4 failed, and the 4 are exactly the `tests/known-failures.txt` baseline (FOLLOW-UPS #48), no off-list failure. UserManagement integration and Host composition fully green

## Notes
- The `Booksy.API` project gained `<FrameworkReference Include="Microsoft.AspNetCore.App" />`. It already held HttpContext-based middleware, so this only makes explicit what it depended on transitively — and `AddRateLimiter` lives in `Microsoft.AspNetCore.Builder`, not the `DependencyInjection` namespace its siblings use.
- `src/Host/Booksy.Host/appsettings.json` carries an unrelated local edit from another session (a connection-string host). Only the rate-limit hunk was staged.

## Decisions
- 2026-09-11 Thresholds, chosen as defaults and configurable per environment rather than asked about, because every one of them is a config value that can be tuned without a code change: OTP send 5 per 5 min per IP, OTP verify 10 per 5 min, authentication 10 per min, password reset 5 per 15 min, registration 10 per hour, read-only public endpoints 120 per min. The per-PHONE protection is the domain's, not the limiter's — a limiter partitioned by IP cannot see the phone number without buffering the body, and the aggregate already models the rule correctly.

## Log
- 2026-09-11 Opened after correcting FOLLOW-UPS #50. Verified by reading the code: `SendVerificationCodeCommandHandler` calls `PhoneVerification.Create(...)` unconditionally, so `CanResend()` is never consulted on the anonymous path; no `AddRateLimiter` exists anywhere in `src`; `ClientRateLimitResolver.ResolveClientId` returns "anonymous" for all unauthenticated callers.
