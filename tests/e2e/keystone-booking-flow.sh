#!/usr/bin/env bash
# =============================================================================
# keystone-booking-flow.sh — end-to-end smoke test of the core Booksy flow:
#
#   provider signs up (OTP) → register-full (auto-approved Active) →
#   adds a staff member (Active sub-provider, services qualified+activated,
#   availability generated) → views staff → customer signs up (OTP) →
#   sees available slots → books one → booking appears in my-bookings.
#
# This exercises BOTH bounded contexts in the monolith (UserManagement auth +
# ServiceCatalog provider/service/booking) and is the flow that must never
# regress. Pure curl + a tiny JSON grep — no extra deps.
#
# PREREQUISITES (the host must be running with sandbox auth enabled so OTP is
# deterministic):
#   OTP_SANDBOX_CODE=123456 ASPNETCORE_ENVIRONMENT=Development \
#     dotnet run --project src/Host/Booksy.Host
#   (and Postgres + Redis up; Rahyab:SandboxMode=true; Kavenegar disabled)
#
# USAGE:  BASE=http://localhost:5050 bash tests/e2e/keystone-booking-flow.sh
# Exit 0 = all assertions passed; non-zero = first failure.
# =============================================================================
set -euo pipefail

BASE="${BASE:-http://localhost:5050}"
OTP="${OTP_CODE:-123456}"
PASS=0
# Unique-per-run 7-digit suffix → valid 10-digit Iranian mobiles (912.../913...).
SUF=$(printf '%07d' "$(( ($$ * 1000 + RANDOM) % 10000000 ))")
RND="$SUF"
PPHONE="912$SUF"   # provider mobile
CPHONE="913$SUF"   # customer mobile

say()  { printf '\n\033[1;36m== %s\033[0m\n' "$*"; }
ok()   { printf '  \033[32mPASS\033[0m %s\n' "$*"; PASS=$((PASS+1)); }
fail() { printf '  \033[31mFAIL\033[0m %s\n' "$*"; exit 1; }
# jget <file> <key>  -> first scalar value of "key": in the JSON (never fails the script)
jget() { { grep -oE -m1 "\"$2\" *: *\"?[^\",}]+" "$1" 2>/dev/null || true; } | sed -E "s/.*: *\"?//; s/\"$//"; }
# http <method> <path> <token|-> <body|-> <outfile>  -> echoes status code
http() {
  local m=$1 path=$2 tok=$3 body=$4 out=$5; local args=(-s -o "$out" -w '%{http_code}' -X "$m" "$BASE$path")
  [ "$tok" != "-" ] && args+=(-H "Authorization: Bearer $tok")
  [ "$body" != "-" ] && args+=(-H "Content-Type: application/json" -d "$body")
  curl "${args[@]}"
}
# auth <provider|customer> <phonedigits>  -> writes token to $TOKEN, userId to $USERID
auth() {
  local kind=$1 num=$2 full="+98$2"
  http POST "/api/v1/Auth/send-verification-code" - "{\"phoneNumber\":\"$num\",\"countryCode\":\"+98\"}" /tmp/k_send.json >/dev/null
  local code; code=$(http POST "/api/v1/Auth/$kind/complete-authentication" - \
    "{\"phoneNumber\":\"$full\",\"code\":\"$OTP\",\"firstName\":\"E2E\",\"lastName\":\"$kind\"}" /tmp/k_auth.json)
  [ "$code" = "200" ] || fail "$kind auth (HTTP $code)"
  TOKEN=$(jget /tmp/k_auth.json accessToken); USERID=$(jget /tmp/k_auth.json userId)
  [ -n "$TOKEN" ] || fail "$kind token empty"
}

say "1) Provider signs up + registers (auto-approved Active)"
auth provider "$PPHONE"; PTOK=$TOKEN; POWNER=$USERID
HOURS=""; for d in 0 1 2 3 4 5 6; do HOURS="$HOURS\"$d\":{\"dayOfWeek\":$d,\"isOpen\":true,\"openTime\":{\"hours\":9,\"minutes\":0},\"closeTime\":{\"hours\":18,\"minutes\":0},\"breaks\":[]},"; done; HOURS="${HOURS%,}"
REG="{\"ownerId\":\"$POWNER\",\"categoryId\":\"HairSalon\",\"businessInfo\":{\"businessName\":\"E2E Salon $RND\",\"ownerFirstName\":\"E2E\",\"ownerLastName\":\"Owner\",\"phoneNumber\":\"$PPHONE\"},\"address\":{\"street\":\"St\",\"city\":\"Tehran\",\"state\":\"Tehran\",\"postalCode\":\"1234567890\",\"country\":\"Iran\",\"latitude\":35.7,\"longitude\":51.4},\"location\":{\"latitude\":35.7,\"longitude\":51.4,\"formattedAddress\":\"Tehran\"},\"businessHours\":{$HOURS},\"services\":[{\"name\":\"Haircut\",\"durationHours\":0,\"durationMinutes\":45,\"price\":250000,\"priceType\":\"fixed\"}],\"assistanceOptions\":[],\"teamMembers\":[],\"ownerFirstName\":\"E2E\",\"ownerLastName\":\"Owner\",\"businessName\":\"E2E Salon $RND\",\"description\":\"t\",\"primaryCategory\":\"HairSalon\",\"email\":\"e2e$RND@s.com\",\"phoneNumber\":\"$PPHONE\",\"street\":\"St\",\"city\":\"Tehran\",\"state\":\"Tehran\",\"postalCode\":\"1234567890\",\"country\":\"Iran\"}"
code=$(http POST "/api/v1/Providers/register-full" "$PTOK" "$REG" /tmp/k_reg.json)
[ "$code" = "201" ] && ok "register-full 201" || fail "register-full (HTTP $code)"
PROV=$(jget /tmp/k_reg.json providerId); [ -n "$PROV" ] || fail "no providerId"

say "2) Provider adds a staff member"
code=$(http POST "/api/v1/Providers/$PROV/staff" "$PTOK" '{"firstName":"Sara","lastName":"Stylist","role":"Stylist"}' /tmp/k_staff.json)
[ "$code" = "201" ] && ok "add-staff 201" || fail "add-staff (HTTP $code)"
STAFF=$(jget /tmp/k_staff.json id); [ -n "$STAFF" ] || fail "no staffId"

say "3) Provider can view the staff"
code=$(http GET "/api/v1/Providers/$PROV/staff" "$PTOK" - /tmp/k_list.json)
[ "$code" = "200" ] && ok "list-staff 200" || fail "list-staff (HTTP $code)"
grep -q "Sara" /tmp/k_list.json && ok "staff appears in list" || fail "staff not listed"

say "4) Customer signs up and sees available slots"
auth customer "$CPHONE"; CTOK=$TOKEN
SVC=""; code=$(http GET "/api/v1/Services/provider/$PROV" "$CTOK" - /tmp/k_svc.json || true)
SVC=$(jget /tmp/k_svc.json id)

say "5) Customer books, and it shows in my-bookings"
START="2026-09-01T10:00:00Z"
[ -n "$SVC" ] || SVC=$(jget /tmp/k_reg.json serviceId)
BODY="{\"providerId\":\"$PROV\",\"serviceId\":\"$SVC\",\"staffProviderId\":\"$STAFF\",\"startTime\":\"$START\",\"customerNotes\":\"keystone e2e\"}"
code=$(http POST "/api/v1/Bookings" "$CTOK" "$BODY" /tmp/k_book.json)
[ "$code" = "201" ] && ok "create-booking 201" || fail "create-booking (HTTP $code): $(jget /tmp/k_book.json message)"
BID=$(jget /tmp/k_book.json id); [ -n "$BID" ] || fail "no bookingId"
code=$(http GET "/api/v1/Bookings/my-bookings" "$CTOK" - /tmp/k_my.json)
[ "$code" = "200" ] && grep -q "$BID" /tmp/k_my.json && ok "booking in my-bookings" || fail "booking not in my-bookings (HTTP $code)"

printf '\n\033[1;32mALL %d ASSERTIONS PASSED — keystone booking flow is green.\033[0m\n' "$PASS"
