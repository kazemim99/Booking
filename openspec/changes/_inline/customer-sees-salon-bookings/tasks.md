Status: DONE
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
- [x] 1.1 Backend: my-bookings also returns bookings the salon recorded for the caller's verified
      number (past/present/future), deduplicated; integration tests
- [x] 1.2 Customer app: the bookings list shows them (whatever the app needs to display a booking
      it did not create); tests
- [x] 1.3 Deploy: customer.nahalkmi.ir — nginx site + TLS, CI build/deploy job, API CORS
- [x] 1.4 Verify, deploy, confirm live

## Log
- 2026-09-20 1.1 my-bookings now also returns bookings whose ProviderCustomer entry carries the
  caller's verified mobile (IPersonDirectory -> phone -> IdsByPhoneAsync across salons, one extra
  predicate in the history query, no date restriction so past and future both appear). 2 integration
  tests: the person who signed up afterwards sees the salon's booking; a stranger's number sees
  nothing.
- 1.2 No customer-app change was needed: its bookings list renders whatever my-bookings returns.
- 1.3 customer.nahalkmi.ir: CI job build-customer-web (analyze + test + web build with
  API_BASE_URL compiled in + content-hashed entry), published to /var/www/booksy-customer by the
  deploy job and health-checked; nginx vhost with its own Let's Encrypt certificate (issued
  2026-09-20, expires 2026-12-19, auto-renewing); CORS origin added (+1 composition test).
  The app's API base URL honours --dart-define like the provider app (1 test); generated
  Retrofit/json_serializable files are excluded from analysis, whose warnings failed the job.
- 2026-09-20 verify FULL PASS (17 steps, 428 s); pushed f4fcd856.
- 2026-09-20 Live: the salon booked a test number, that number signed in as a customer and its
  «my-bookings» returned exactly that appointment (سالن نهال, خط‌گیری ریش, 2026-09-25 17:30);
  test booking and test customer removed afterwards.
- 2026-09-20 customer.nahalkmi.ir serves the app (main.dart.01bdbffedd21d9ff.js, API URL compiled
  in). Two CI defects were fixed to get there: the app's generated code was gitignored so a clean
  checkout could not analyze (now tracked — retrofit_generator 8.2.1 does not compile on this SDK,
  so CI cannot regenerate it), and the site directory, created by hand as root, was not writable
  by the runner (the publish step now takes ownership once through docker, which this job already
  drives, and otherwise prints the exact chown to run). SSH from here could not reach port 22 for
  ~40 minutes, which is why the fix had to go through the pipeline.
