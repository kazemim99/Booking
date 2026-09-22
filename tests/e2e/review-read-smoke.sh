#!/usr/bin/env bash
# =============================================================================
# review-read-smoke.sh — post-deploy assertion that the public review read works
# (openspec/changes/provider-reviews-and-ratings, task 9.5).
#
# The compose healthcheck is `curl /health`: it can never see a broken review
# read — a missing column after a half-applied migration, say, answers 500 here
# while /health stays green. This asks the real endpoint:
#
#   GET /api/v1/Providers/search?pageSize=1          → any one provider id
#   GET /api/v1/reviews/providers/{id}?pageSize=1    → 200, with a statistics block
#                                                      whose totalReviews is a number
#
# Anonymous, read-only, writes nothing — safe against production.
# An empty catalogue skips the review read with a notice rather than failing.
#
# USAGE:  BASE=https://back.nahalkmi.ir bash tests/e2e/review-read-smoke.sh
# Pure curl + grep, like keystone-booking-flow.sh. Exit 0 = pass.
# =============================================================================
set -euo pipefail

BASE="${BASE:-http://localhost:5050}"
OUT="$(mktemp)"
trap 'rm -f "$OUT"' EXIT

fail() { printf '  \033[31mFAIL\033[0m %s\n' "$*"; [ -s "$OUT" ] && head -c 600 "$OUT" && echo; exit 1; }
ok()   { printf '  \033[32mPASS\033[0m %s\n' "$*"; }

get() { # get <path> -> status code, body in $OUT (retries while the host is still warming up)
  local code=000
  for _ in 1 2 3 4 5 6; do
    code=$(curl -s -o "$OUT" -w '%{http_code}' "$BASE$1" || echo 000)
    [ "$code" != "000" ] && [ "$code" != "502" ] && [ "$code" != "503" ] && break
    sleep 5
  done
  echo "$code"
}

code=$(get "/api/v1/Providers/search?pageNumber=1&pageSize=1")
[ "$code" = "200" ] || fail "provider search answered HTTP $code"
provider=$(grep -oE -m1 '"id" *: *"[0-9a-fA-F-]{36}"' "$OUT" | grep -oE '[0-9a-fA-F-]{36}' || true)
if [ -z "$provider" ]; then
  printf '  \033[33mSKIP\033[0m no provider in the catalogue — nothing to read reviews of\n'
  exit 0
fi
ok "provider search → $provider"

code=$(get "/api/v1/reviews/providers/$provider?pageNumber=1&pageSize=1")
[ "$code" = "200" ] || fail "review listing for $provider answered HTTP $code"
grep -q '"statistics"' "$OUT" || fail "review listing has no statistics block"
grep -qE '"totalReviews" *: *[0-9]+' "$OUT" || fail "statistics.totalReviews is not a number"
ok "review listing → 200 with statistics"
