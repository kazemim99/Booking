#!/usr/bin/env bash
# =============================================================================
# media-url-smoke.sh — post-deploy assertion that salon photos can actually load
# (openspec/changes/_inline/salon-images-load, G3).
#
# On 2026-09-22 every health check was green while no salon photo loaded: the
# API sent them as relative paths ("uploads/providers/…"), which a Flutter web
# page resolves against ITS OWN host, where nginx answers index.html. A root
# page answering 200 can never see that. This asks the real read models:
#
#   GET /api/v1/Providers/search          every logoUrl / profileImageUrl is absolute
#   GET /api/v1/Providers/{id}            (a salon with an upload) every image URL is absolute
#   GET <that upload>  with Origin        200, image/*, and the CORS header for ORIGIN
#   GET <that upload>  without Origin     still "Vary: Origin" (a shared browser cache
#                                         must not reuse a no-CORS copy for a CORS fetch)
#
# Anonymous, read-only, writes nothing — safe against production.
# No uploaded photo anywhere in the catalogue skips the upload checks with a notice.
#
# USAGE:  BASE=https://back.nahalkmi.ir REQUIRE_HTTPS=1 ORIGIN=https://customer.nahalkmi.ir \
#           bash tests/e2e/media-url-smoke.sh
# Pure curl + grep, like review-read-smoke.sh. Exit 0 = pass.
# =============================================================================
set -euo pipefail

BASE="${BASE:-http://localhost:5050}"
ORIGIN="${ORIGIN:-}"
ABSOLUTE='^https?://'
[ "${REQUIRE_HTTPS:-0}" = "1" ] && ABSOLUTE='^https://'
OUT="$(mktemp)"; HDR="$(mktemp)"
trap 'rm -f "$OUT" "$HDR"' EXIT

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

urls_in() { # urls_in <field...> -> every non-null string value of those fields in $OUT
  local fields; fields=$(IFS='|'; echo "$*")
  grep -oE "\"($fields)\" *: *\"[^\"]*\"" "$OUT" | sed -E 's/^"[^"]*" *: *"(.*)"$/\1/' || true
}

all_absolute() { # all_absolute <what> <urls...>
  local what=$1; shift
  local bad
  bad=$(printf '%s\n' "$@" | grep -v '^$' | grep -vE "$ABSOLUTE" || true)
  [ -z "$bad" ] || fail "$what carries a URL a browser on another host cannot load: $(echo "$bad" | head -1)"
}

code=$(get "/api/v1/Providers/search?pageNumber=1&pageSize=100")
[ "$code" = "200" ] || fail "provider search answered HTTP $code"
mapfile -t search_urls < <(urls_in logoUrl profileImageUrl)
all_absolute "provider search" "${search_urls[@]:-}"
ok "provider search → ${#search_urls[@]} photo URL(s), all absolute"

upload=$(printf '%s\n' "${search_urls[@]:-}" | grep -m1 '/uploads/providers/' || true)
if [ -z "$upload" ]; then
  printf '  \033[33mSKIP\033[0m no uploaded photo in the catalogue — nothing to fetch\n'
  exit 0
fi
provider=$(echo "$upload" | grep -oE '/uploads/providers/[0-9a-fA-F-]{36}' | grep -oE '[0-9a-fA-F-]{36}')

code=$(get "/api/v1/Providers/$provider")
[ "$code" = "200" ] || fail "provider $provider answered HTTP $code"
mapfile -t detail_urls < <(urls_in logoUrl profileImageUrl thumbnailUrl mediumUrl originalUrl)
all_absolute "provider $provider" "${detail_urls[@]:-}"
ok "provider $provider → ${#detail_urls[@]} photo URL(s), all absolute"

code=$(curl -s -o /dev/null -D "$HDR" -w '%{http_code}' ${ORIGIN:+-H "Origin: $ORIGIN"} "$upload" || echo 000)
[ "$code" = "200" ] || fail "$upload answered HTTP $code"
grep -qiE '^content-type: *image/' "$HDR" || fail "$upload is not an image ($(grep -i '^content-type' "$HDR" | tr -d '\r'))"
if [ -n "$ORIGIN" ]; then
  grep -qiE "^access-control-allow-origin: *($ORIGIN|\*)" "$HDR" \
    || fail "$upload has no CORS header for $ORIGIN — the Flutter apps cannot read it"
fi
ok "upload → 200 image${ORIGIN:+, CORS for $ORIGIN}"

curl -s -o /dev/null -D "$HDR" "$upload" || true
grep -qiE '^vary:.*origin' "$HDR" \
  || fail "$upload fetched without an Origin has no 'Vary: Origin' — a cached copy would break CORS fetches"
ok "upload fetched without Origin → Vary: Origin"
