#!/usr/bin/env bash
# Write a .gz next to every compressible file of the web build, so nginx serves it as is (gzip_static).
#
# Measured on production 2026-09-23: main.dart.<hash>.js (3.9 MB) went out UNCOMPRESSED, because the vhost had no
# gzip_types and nginx only compresses text/html by default. Compressing at -9 once, in CI, beats nginx compressing
# at a low level on every request of a shared box — and the vhost's `gzip on` still covers anything missed here.
#
# Idempotent: re-running rewrites each .gz from its source (-f) and never compresses a .gz. Source maps are skipped
# (browsers fetch them only with dev tools open); PNG images are compressed already.
#
# Usage: tool/precompress_web.sh [build/web]      Run AFTER tool/cache_bust_web.sh (it rewrites flutter_bootstrap.js).
set -euo pipefail

dir="${1:-build/web}"
test -d "$dir" || { echo "no directory $dir — run flutter build web first" >&2; exit 1; }
cd "$dir"

before=0
after=0
count=0
while IFS= read -r -d '' f; do
  gzip -9 -k -f -n -- "$f"
  size=$(wc -c < "$f")
  gz=$(wc -c < "$f.gz")
  before=$((before + size))
  after=$((after + gz))
  count=$((count + 1))
  if [ "$size" -ge 102400 ]; then
    printf '%10d -> %9d bytes  %s.gz\n' "$size" "$gz" "${f#./}"
  fi
done < <(find . -type f \( -name '*.js' -o -name '*.mjs' -o -name '*.wasm' -o -name '*.json' -o -name '*.css' \
  -o -name '*.html' -o -name '*.otf' -o -name '*.ttf' -o -name '*.svg' \) -print0 | sort -z)

test "$count" -gt 0 || { echo "nothing to compress in $dir" >&2; exit 1; }
printf 'precompressed %d files: %d -> %d bytes (.gz total, %d%%)\n' "$count" "$before" "$after" $((after * 100 / before))
