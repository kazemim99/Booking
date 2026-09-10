Status: ACTIVE
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
- [ ] 2 Register ASP.NET Core's built-in rate limiter with all thirteen named policies, partitioned by caller (IP for anonymous, user id when authenticated), limits bound from configuration
- [ ] 3 `UseRateLimiter()` in all three hosts, before authorization, and a 429 that carries `Retry-After`
- [ ] 4 `ClientRateLimitResolver` keys anonymous callers by IP instead of the constant "anonymous"
- [ ] 5 Test hosts get permissive limits so existing suites are unaffected; integration tests pin S1 and S3
- [ ] 6 FULL verify green

## Decisions
- 2026-09-11 Thresholds, chosen as defaults and configurable per environment rather than asked about, because every one of them is a config value that can be tuned without a code change: OTP send 5 per 5 min per IP, OTP verify 10 per 5 min, authentication 10 per min, password reset 5 per 15 min, registration 10 per hour, read-only public endpoints 120 per min. The per-PHONE protection is the domain's, not the limiter's — a limiter partitioned by IP cannot see the phone number without buffering the body, and the aggregate already models the rule correctly.

## Log
- 2026-09-11 Opened after correcting FOLLOW-UPS #50. Verified by reading the code: `SendVerificationCodeCommandHandler` calls `PhoneVerification.Create(...)` unconditionally, so `CanResend()` is never consulted on the anonymous path; no `AddRateLimiter` exists anywhere in `src`; `ClientRateLimitResolver.ResolveClientId` returns "anonymous" for all unauthenticated callers.
