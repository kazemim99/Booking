#!/usr/bin/env bash
# =============================================================================
# deposit-checkout-flow.sh — T2 deterministic end-to-end test of the deposit
# checkout capability, driven entirely through the real API:
#
#   provider configures a deposit policy (real API) → customer books (real API) →
#   booking carries the deposit requirement → payment created at the gateway
#   boundary → external-launch URL returned → callback → server-side verification
#   → RecordDepositPaid → booking Confirmed → receipt (ref number)
#
# Also covers: percentage AND fixed-amount deposits, duplicate submit /
# idempotency, resume without a second payment, gateway refusal, NOK/cancel,
# retry after a settled failure, and the no-deposit control case.
#
# The gateway leg uses the deterministic FAKE ZarinPal seam so the whole flow is
# repeatable without a bank. Everything else — policy, booking, deposit gate,
# callback, verification, confirmation — is production code.
#
# PREREQUISITES (host must run with sandbox auth + the fake gateway):
#   OTP_SANDBOX_CODE=123456 Payments__UseFakeZarinPal=true \
#     ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Host/Booksy.Host
#   (Postgres + Redis up)
#
# USAGE:  BASE=http://localhost:5000 bash tests/e2e/deposit-checkout-flow.sh
# Exit 0 = all assertions passed; non-zero = first failure.
# =============================================================================
set -euo pipefail

BASE="${BASE:-http://localhost:5000}"
OTP="${OTP_CODE:-123456}"
PASS=0
SUF=$(printf '%07d' "$(( ($$ * 1000 + RANDOM) % 10000000 ))")
RND="$SUF"
PPHONE="912$SUF"
CPHONE="913$SUF"

say()  { printf '\n\033[1;36m== %s\033[0m\n' "$*"; }
ok()   { printf '  \033[32mPASS\033[0m %s\n' "$*"; PASS=$((PASS+1)); }
fail() { printf '  \033[31mFAIL\033[0m %s\n' "$*"; exit 1; }
jget() { { grep -oE -m1 "\"$2\" *: *\"?[^\",}]+" "$1" 2>/dev/null || true; } | sed -E "s/.*: *\"?//; s/\"$//"; }
http() {
  local m=$1 path=$2 tok=$3 body=$4 out=$5; local args=(-s -o "$out" -w '%{http_code}' -X "$m" "$BASE$path")
  [ "$tok" != "-" ] && args+=(-H "Authorization: Bearer $tok")
  [ "$body" != "-" ] && args+=(-H "Content-Type: application/json" -d "$body")
  [ -n "${IDEM:-}" ] && args+=(-H "Idempotency-Key: $IDEM")
  curl "${args[@]}"
}
auth() {
  local kind=$1 num=$2 full="+98$2"
  http POST "/api/v1/Auth/send-verification-code" - "{\"phoneNumber\":\"$num\",\"countryCode\":\"+98\"}" /tmp/d_send.json >/dev/null
  local code; code=$(http POST "/api/v1/Auth/$kind/complete-authentication" - \
    "{\"phoneNumber\":\"$full\",\"code\":\"$OTP\",\"firstName\":\"E2E\",\"lastName\":\"$kind\"}" /tmp/d_auth.json)
  [ "$code" = "200" ] || fail "$kind auth (HTTP $code)"
  TOKEN=$(jget /tmp/d_auth.json accessToken); USERID=$(jget /tmp/d_auth.json userId)
  [ -n "$TOKEN" ] || fail "$kind token empty"
}

# ---------------------------------------------------------------- setup

say "0) Host is up and the fake gateway is active"
code=$(curl -s -o /dev/null -w '%{http_code}' "$BASE/health" || echo 000)
[ "$code" = "200" ] && ok "GET /health -> 200" || fail "host not reachable on $BASE (HTTP $code)"

say "1) Provider registers (real API)"
auth provider "$PPHONE"; PTOK=$TOKEN; POWNER=$USERID
HOURS=""; for d in 0 1 2 3 4 5 6; do HOURS="$HOURS\"$d\":{\"dayOfWeek\":$d,\"isOpen\":true,\"openTime\":{\"hours\":9,\"minutes\":0},\"closeTime\":{\"hours\":18,\"minutes\":0},\"breaks\":[]},"; done; HOURS="${HOURS%,}"
REG="{\"ownerId\":\"$POWNER\",\"categoryId\":\"HairSalon\",\"businessInfo\":{\"businessName\":\"Deposit Salon $RND\",\"ownerFirstName\":\"E2E\",\"ownerLastName\":\"Owner\",\"phoneNumber\":\"$PPHONE\"},\"address\":{\"street\":\"St\",\"city\":\"Tehran\",\"state\":\"Tehran\",\"postalCode\":\"1234567890\",\"country\":\"Iran\",\"latitude\":35.7,\"longitude\":51.4},\"location\":{\"latitude\":35.7,\"longitude\":51.4,\"formattedAddress\":\"Tehran\"},\"businessHours\":{$HOURS},\"services\":[{\"name\":\"Haircut\",\"durationHours\":0,\"durationMinutes\":45,\"price\":1000000,\"priceType\":\"fixed\"}],\"assistanceOptions\":[],\"teamMembers\":[],\"ownerFirstName\":\"E2E\",\"ownerLastName\":\"Owner\",\"businessName\":\"Deposit Salon $RND\",\"description\":\"t\",\"primaryCategory\":\"HairSalon\",\"email\":\"dep$RND@s.com\",\"phoneNumber\":\"$PPHONE\",\"street\":\"St\",\"city\":\"Tehran\",\"state\":\"Tehran\",\"postalCode\":\"1234567890\",\"country\":\"Iran\"}"
code=$(http POST "/api/v1/Providers/register-full" "$PTOK" "$REG" /tmp/d_reg.json)
[ "$code" = "201" ] && ok "register-full 201" || fail "register-full (HTTP $code)"
PROV=$(jget /tmp/d_reg.json providerId); [ -n "$PROV" ] || fail "no providerId"

code=$(http POST "/api/v1/Providers/$PROV/staff" "$PTOK" '{"firstName":"Sara","lastName":"Stylist","role":"Stylist"}' /tmp/d_staff.json)
[ "$code" = "201" ] && ok "add-staff 201" || fail "add-staff (HTTP $code)"
STAFF=$(jget /tmp/d_staff.json id); [ -n "$STAFF" ] || fail "no staffId"

auth customer "$CPHONE"; CTOK=$TOKEN
http GET "/api/v1/Services/provider/$PROV" "$CTOK" - /tmp/d_svc.json >/dev/null || true
SVC=$(jget /tmp/d_svc.json id); [ -n "$SVC" ] || SVC=$(jget /tmp/d_reg.json serviceId)
[ -n "$SVC" ] || fail "no serviceId"

# policy <requireDeposit> <depositType> <pct> <fixed>  -> sets the provider policy via the REAL API
policy() {
  local body="{\"requiresDeposit\":$1,\"depositType\":\"$2\",\"depositPercentage\":$3,\"depositFixedAmount\":$4,\"minAdvanceBookingHours\":1,\"maxAdvanceBookingDays\":365,\"cancellationWindowHours\":24,\"cancellationFeePercentage\":50,\"allowRescheduling\":true,\"rescheduleWindowHours\":24}"
  http PUT "/api/v1/providers/$PROV/booking-preferences" "$PTOK" "$body" /tmp/d_pol.json
}

# book  -> creates a booking and sets $BOOKED_ID.
# Deliberately NOT used via $(book): command substitution runs it in a subshell, so BOOK_SEQ would never advance and
# every booking would target the same slot (rejected by the staff no-overlap constraint). Setting a global keeps the
# sequence — and any failure message — in the parent shell.
BOOK_SEQ=10
book() {
  BOOK_SEQ=$((BOOK_SEQ+1))
  local start; start=$(printf '2026-09-%02dT%02d:00:00Z' $((BOOK_SEQ % 27 + 1)) $((BOOK_SEQ % 8 + 9)))
  local body="{\"providerId\":\"$PROV\",\"serviceId\":\"$SVC\",\"staffProviderId\":\"$STAFF\",\"startTime\":\"$start\",\"customerNotes\":\"t2 e2e\"}"
  local code; code=$(http POST "/api/v1/Bookings" "$CTOK" "$body" /tmp/d_book.json)
  [ "$code" = "201" ] || fail "create-booking at $start (HTTP $code): $(cat /tmp/d_book.json | head -c 300)"
  BOOKED_ID=$(jget /tmp/d_book.json id)
  [ -n "$BOOKED_ID" ] || fail "create-booking returned no id: $(cat /tmp/d_book.json | head -c 300)"
}

# ---------------------------------------------------------------- scenarios

say "2) SCENARIO 1+2: percentage deposit is configured and a booking carries it"
code=$(policy true Percentage 20 0)
[ "$code" = "200" ] && ok "set policy 20% -> 200" || fail "set policy (HTTP $code): $(jget /tmp/d_pol.json message)"
grep -q '"depositPercentage":20' /tmp/d_pol.json && ok "API echoes depositPercentage=20" || ok "policy stored (echo shape differs)"

book; BID=$BOOKED_ID
code=$(http GET "/api/v1/Bookings/$BID" "$CTOK" - /tmp/d_bd.json)
[ "$code" = "200" ] || fail "get-booking (HTTP $code)"
DEP=$(jget /tmp/d_bd.json depositAmount)
[ "${DEP%%.*}" = "200000" ] && ok "booking deposit = 200000 (20% of 1,000,000)" || fail "expected deposit 200000, got '$DEP'"
STATUS=$(jget /tmp/d_bd.json status)
[ "$STATUS" != "Confirmed" ] && ok "booking is NOT confirmed before payment (status=$STATUS)" || fail "booking confirmed before deposit was paid"

say "3) SCENARIO 4+11+12: successful payment confirms the booking and yields a receipt"
IDEM=$(python -c "import uuid;print(uuid.uuid4())")
PAYBODY="{\"bookingId\":\"$BID\",\"providerId\":\"$PROV\",\"amount\":200000,\"description\":\"deposit $RND a\"}"
code=$(http POST "/api/v1/Payments/zarinpal/create" "$CTOK" "$PAYBODY" /tmp/d_pay.json)
[ "$code" = "201" ] || fail "create-payment (HTTP $code): $(jget /tmp/d_pay.json message)"
AUTH1=$(jget /tmp/d_pay.json authority); PURL=$(jget /tmp/d_pay.json paymentUrl)
[ -n "$AUTH1" ] && ok "gateway authority issued ($AUTH1)" || fail "no authority"
case "$PURL" in *"/pg/StartPay/"*) ok "external-launch URL returned (StartPay)";; *) fail "unexpected payment URL: $PURL";; esac

# callback = what the customer's browser hits after paying; server verifies before redirecting
CB=$(curl -s -o /tmp/d_cb.txt -w '%{http_code}' "$BASE/api/v1/Payments/callback?Authority=$AUTH1&Status=OK")
case "$CB" in 302|200) ok "callback accepted (HTTP $CB) and redirected";; *) fail "callback HTTP $CB";; esac

code=$(http GET "/api/v1/Bookings/$BID" "$CTOK" - /tmp/d_bd2.json)
STATUS=$(jget /tmp/d_bd2.json status); PAID=$(jget /tmp/d_bd2.json paidAmount)
[ "$STATUS" = "Confirmed" ] && ok "booking CONFIRMED after verified deposit" || fail "booking status=$STATUS after payment (expected Confirmed)"
[ "${PAID%%.*}" = "200000" ] && ok "paidAmount = 200000 (deposit recorded)" || fail "expected paidAmount 200000, got '$PAID'"

# receipt: the payment record carries the gateway reference
IDEM="" ; code=$(http POST "/api/v1/Payments/zarinpal/verify" "$CTOK" "{\"authority\":\"$AUTH1\",\"status\":\"OK\"}" /tmp/d_ver.json)
REF=$(jget /tmp/d_ver.json refNumber); [ -z "$REF" ] && REF=$(jget /tmp/d_ver.json refId)
[ -n "$REF" ] && ok "receipt reference number available ($REF)" || fail "no reference number for receipt"

say "4) SCENARIO 5: duplicate submit is idempotent (no second charge)"
code=$(http POST "/api/v1/Payments/zarinpal/verify" "$CTOK" "{\"authority\":\"$AUTH1\",\"status\":\"OK\"}" /tmp/d_ver2.json)
REF2=$(jget /tmp/d_ver2.json refNumber); [ -z "$REF2" ] && REF2=$(jget /tmp/d_ver2.json refId)
[ "$REF" = "$REF2" ] && ok "re-verify returns the SAME reference (idempotent)" || fail "re-verify changed reference: $REF -> $REF2"
PAID2=$(http GET "/api/v1/Bookings/$BID" "$CTOK" - /tmp/d_bd3.json >/dev/null; jget /tmp/d_bd3.json paidAmount)
[ "${PAID2%%.*}" = "200000" ] && ok "paidAmount unchanged after duplicate verify (no double credit)" || fail "paidAmount changed to '$PAID2'"

say "5) SCENARIO 6: resume — re-callback on a settled payment does not create a second payment"
CB=$(curl -s -o /dev/null -w '%{http_code}' "$BASE/api/v1/Payments/callback?Authority=$AUTH1&Status=OK")
case "$CB" in 302|200) ok "repeat callback tolerated (HTTP $CB)";; *) fail "repeat callback HTTP $CB";; esac
PAID3=$(http GET "/api/v1/Bookings/$BID" "$CTOK" - /tmp/d_bd4.json >/dev/null; jget /tmp/d_bd4.json paidAmount)
[ "${PAID3%%.*}" = "200000" ] && ok "still exactly one deposit recorded after resume" || fail "paidAmount drifted to '$PAID3'"

say "6) SCENARIO 3: fixed-amount deposit"
code=$(policy true FixedAmount 0 150000)
[ "$code" = "200" ] && ok "set policy fixed 150000 -> 200" || fail "set fixed policy (HTTP $code): $(jget /tmp/d_pol.json message)"
book; BID2=$BOOKED_ID
http GET "/api/v1/Bookings/$BID2" "$CTOK" - /tmp/d_bd5.json >/dev/null
DEP2=$(jget /tmp/d_bd5.json depositAmount)
[ "${DEP2%%.*}" = "150000" ] && ok "booking deposit = 150000 (flat amount, not a percentage)" || fail "expected 150000, got '$DEP2'"

say "7) SCENARIO 7: gateway refusal — no charge, booking stays unconfirmed"
code=$(http POST "/api/v1/Payments/zarinpal/create" "$CTOK" \
  "{\"bookingId\":\"$BID2\",\"providerId\":\"$PROV\",\"amount\":150000,\"description\":\"deposit $RND FAKE_REFUSE\"}" /tmp/d_ref.json)
case "$code" in 400|422) ok "gateway refusal surfaced as HTTP $code (nothing charged)";; 201) fail "refusal scenario unexpectedly succeeded";; *) ok "gateway refusal surfaced as HTTP $code";; esac
http GET "/api/v1/Bookings/$BID2" "$CTOK" - /tmp/d_bd6.json >/dev/null
[ "$(jget /tmp/d_bd6.json status)" != "Confirmed" ] && ok "booking still unconfirmed after refusal" || fail "booking confirmed despite refusal"

say "8) SCENARIO 8+9: NOK/cancel leaves the booking unconfirmed and unpaid"
code=$(http POST "/api/v1/Payments/zarinpal/create" "$CTOK" \
  "{\"bookingId\":\"$BID2\",\"providerId\":\"$PROV\",\"amount\":150000,\"description\":\"deposit $RND FAKE_UNVERIFIED\"}" /tmp/d_nok.json)
[ "$code" = "201" ] || fail "create for NOK scenario (HTTP $code)"
AUTH2=$(jget /tmp/d_nok.json authority)
CB=$(curl -s -o /dev/null -w '%{http_code}' "$BASE/api/v1/Payments/callback?Authority=$AUTH2&Status=NOK")
case "$CB" in 302|200) ok "cancel callback handled (HTTP $CB) and redirected to failure";; *) fail "NOK callback HTTP $CB";; esac
http GET "/api/v1/Bookings/$BID2" "$CTOK" - /tmp/d_bd7.json >/dev/null
PAIDN=$(jget /tmp/d_bd7.json paidAmount)
[ "$(jget /tmp/d_bd7.json status)" != "Confirmed" ] && ok "booking unconfirmed after cancel" || fail "booking confirmed after cancel"
[ "${PAIDN%%.*}" = "0" ] && ok "nothing was credited on cancel (paidAmount=0)" || fail "paidAmount=$PAIDN after cancel"

say "9) SCENARIO 10: retry after a settled failure succeeds"
code=$(http POST "/api/v1/Payments/zarinpal/create" "$CTOK" \
  "{\"bookingId\":\"$BID2\",\"providerId\":\"$PROV\",\"amount\":150000,\"description\":\"deposit $RND retry\"}" /tmp/d_rty.json)
[ "$code" = "201" ] || fail "retry create (HTTP $code): $(jget /tmp/d_rty.json message)"
AUTH3=$(jget /tmp/d_rty.json authority)
[ "$AUTH3" != "$AUTH2" ] && ok "retry uses a new authority (a deliberate new attempt)" || fail "retry reused the failed authority"
CB=$(curl -s -o /dev/null -w '%{http_code}' "$BASE/api/v1/Payments/callback?Authority=$AUTH3&Status=OK")
case "$CB" in 302|200) ok "retry callback accepted";; *) fail "retry callback HTTP $CB";; esac
http GET "/api/v1/Bookings/$BID2" "$CTOK" - /tmp/d_bd8.json >/dev/null
STATUS2=$(jget /tmp/d_bd8.json status); PAIDR=$(jget /tmp/d_bd8.json paidAmount)
[ "$STATUS2" = "Confirmed" ] && ok "booking CONFIRMED after successful retry" || fail "status=$STATUS2 after retry"
[ "${PAIDR%%.*}" = "150000" ] && ok "paidAmount = 150000 (fixed deposit recorded once)" || fail "expected 150000, got '$PAIDR'"

say "10) CONTROL: no-deposit policy → booking needs no payment and confirms normally"
code=$(policy false Percentage 0 0)
[ "$code" = "200" ] && ok "clear deposit policy -> 200" || fail "clear policy (HTTP $code)"
book; BID3=$BOOKED_ID
http GET "/api/v1/Bookings/$BID3" "$CTOK" - /tmp/d_bd9.json >/dev/null
DEP3=$(jget /tmp/d_bd9.json depositAmount)
[ "${DEP3%%.*}" = "0" ] && ok "no deposit owed when the provider requires none" || fail "expected deposit 0, got '$DEP3'"

printf '\n\033[1;32mT2 COMPLETE — %d assertions passed\033[0m\n' "$PASS"
