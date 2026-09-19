Status: DONE
Verify: FAST

User request (2026-09-19): a provider keeps its own list of customers — first name, last name, phone —
adds them by hand or imports them from the phone's contacts, can do so optionally during onboarding,
and books them quickly. Also: next to the booking screen's customer name/phone, a button that picks a
contact and fills both in.

## Product decisions (asked 2026-09-19)
- Contacts: ONLY the contacts the provider ticks in the phone's own picker reach the server. The app
  never reads or uploads the whole address book.
- Platforms: the provider app runs in the browser today; the Contact Picker API exists only in Chrome
  on Android. Ship it there now; everywhere else the button is hidden and the customer is typed in
  (always available). A future native build adds iPhone.
- Surfaces: all three — a Customers tab, the booking screen, and an optional skippable onboarding step.

## Today (measured)
- GET /providers/{id}/clients derives a client book from BOOKINGS (online customers only).
- A walk-in booking made by the provider stores «مشتری حضوری: name — phone» in the notes, and the
  booking's customer is the provider's own person (seen on production: customerId = the owner).

## Acceptance scenarios
- K1 A provider adds a customer (first name, last name, phone); it appears in their list, searchable
  by name or number, and never in another salon's list.
- K2 The same phone is one customer per salon: adding or importing it again updates, never duplicates.
  Phones are stored normalized (+98...) and shown in the local form (0912 ...).
- K3 Import: the provider ticks N contacts in the picker; N customers are added/updated in one
  request; invalid numbers are reported per contact, the rest still import.
- K4 Editing and removing a customer works; removing never deletes past bookings.
- K5 Booking screen: picking a saved customer (or a contact) fills name and phone; the booking is
  recorded for that customer, and shows in that customer's history.
- K6 Onboarding: an optional step offers "add your customers" (import or type); skipping leaves
  onboarding exactly as today.
- K7 Only the salon's own members can read or change its customers.

## Tasks
- [x] 1.1 Backend: ProviderCustomer aggregate (provider id, names, normalized phone, notes, source),
      table + migration, uniqueness (provider, phone)
- [x] 1.2 Backend: endpoints — list/search, add, update, remove, bulk import with per-row results;
      authorization to the salon's members; integration tests (K1-K4, K7)
- [x] 1.3 App: Customers tab — list, search, add/edit/remove (required-field rule applies)
- [x] 1.4 App: contact import via the browser Contact Picker API where supported (feature-detected;
      hidden elsewhere); tests with a fake picker
- [x] 1.5 Booking: pick a saved customer or a contact in the composer; the booking is linked to the
      customer (backend + app) (K5)
- [x] 1.6 Onboarding: optional customers step (K6)
- [x] 1.7 Verify, deploy, confirm live

## Log
- 2026-09-19 1.1 ProviderCustomer aggregate + provider_customers table (migration AddProviderCustomers:
  CreateTable + unique index (provider, phone) only). 1.2 ProviderCustomersController
  (/providers/{id}/customers: list/search, add 201/409, update, delete 204, import with per-row
  Added/AlreadySaved/Invalid; never overwrites a saved entry); access = admin, providerId claim, or
  ManageBookings member. 7 integration tests (K1-K4, K7) green.
- 2026-09-19 K5 backend: Booking.ProviderCustomerId (nullable, indexed, no FK) set only on a
  salon-created booking that names one of ITS customers (else 403/404); the customer list carries
  totalBookings/upcomingBookings/lastBookingAt. 2 more integration tests; 22 green with Bookings.
- 1.3 Clients tab = saved customers + online clients merged by phone (one row, both counts); add
  (FAB), edit/remove on saved rows, "save to book" on online rows; form: first name + mobile
  required (inline rule). 1.4 ContactPicker (browser Contact Picker API via js_interop, conditional
  import; stub elsewhere -> buttons hidden); only ticked contacts reach the app. 1.5 composer: pick
  from book / from contacts beside the name+phone fields; the booking carries providerCustomerId
  while the number is unchanged; a picked contact is saved to the book first. 1.6 optional
  customers section on the completion step (draft id; skipping unchanged). App: 548 tests pass
  (+ contact parsing, clients, composer, completion tests); analyze clean.
- 2026-09-19 verify FULL PASS (17 steps, 1283 s) on a39da124; pushed to master, auto-deployed.
  Live on back.nahalkmi.ir as «سالن نهال»: add 201 (+989350001122), same number again 409 with the
  saved name, import 1 added / 1 already saved / 1 invalid, a booking with providerCustomerId 201
  and the customer's history shows 1 upcoming; test booking cancelled and both test customers
  removed (book back to 0). provider.nahalkmi.ir serves the new bundle (main.dart.2f9074ed….js).
  The contact button appears only in Chrome on Android (Contact Picker API); not exercised on a
  real phone from here.
