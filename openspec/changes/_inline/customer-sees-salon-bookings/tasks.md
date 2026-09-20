Status: ACTIVE
Verify: FAST

User request (2026-09-20):
1. Deploy the customer app at customer.nahalkmi.ir (DNS already points at the VPS).
2. When someone signs up, they must see every booking a salon already made for them — past,
   present and future — not just the ones they booked themselves.

## Findings (measured)
- A salon-entered booking's CustomerId is the salon owner; the customer it is really for is the
  ProviderCustomer row (added 2026-09-19), keyed by the mobile number.
- GET /Bookings/my-bookings filters on CustomerId alone, so those bookings are invisible to the
  person they belong to, before and after they sign up.
- The phone is a verified identity here (OTP sign-in), and IPersonDirectory already resolves a
  person by phone across contexts.
- booksy-customer-app is a Flutter app with web support; the provider app's web pipeline
  (cache-busting + nginx + self-hosted runner deploy) is the pattern to copy.

## Decisions (tier 1-2, recorded)
- Match by verified mobile, at query time — not a one-off claim at sign-up: a salon may book a
  number before OR after that person signs up, and both must show. Nothing is rewritten.
- Sign-in proves the number (OTP), so what the salon recorded against it is that person's.
- The bookings stay the salon's own record: the customer sees them, the salon keeps them if the
  customer deletes their account.

## Tasks
- [ ] 1.1 Backend: my-bookings also returns bookings the salon recorded for the caller's verified
      number (past/present/future), deduplicated; integration tests
- [ ] 1.2 Customer app: the bookings list shows them (whatever the app needs to display a booking
      it did not create); tests
- [ ] 1.3 Deploy: customer.nahalkmi.ir — nginx site + TLS, CI build/deploy job, API CORS
- [ ] 1.4 Verify, deploy, confirm live

## Log
