#!/usr/bin/env bash
# Give the web build's entry file a content-hashed name, so a deploy reaches every browser at once.
#
# Flutter names the whole app `main.dart.js` on every build. Served with a long cache lifetime (as
# production did until 2026-09-19: 30 days), a browser that had loaded the app once kept running the
# OLD code after each deploy — the user saw last week's services screen in an incognito window.
# A new name per build makes a stale copy impossible: flutter_bootstrap.js (served no-store) points at
# the new file, which no cache has seen.
#
# Usage: tool/cache_bust_web.sh [build/web]      Prints the new file name.
set -euo pipefail

dir="${1:-build/web}"
cd "$dir"

test -f main.dart.js || { echo "no main.dart.js in $dir — run flutter build web first" >&2; exit 1; }

hash=$(sha256sum main.dart.js | cut -c1-16)
new="main.dart.$hash.js"

mv main.dart.js "$new"
if [ -f main.dart.js.map ]; then mv main.dart.js.map "$new.map"; fi

# The loader reads the entry name from the build config it embeds ({"mainJsPath":"main.dart.js"})
# and only falls back to "main.dart.js" when that is absent. Rewrite that one entry.
grep -q '"mainJsPath":"main.dart.js"' flutter_bootstrap.js \
  || { echo "flutter_bootstrap.js no longer names mainJsPath — the Flutter loader changed; update this script" >&2; exit 1; }
sed -i "s/\"mainJsPath\":\"main\.dart\.js\"/\"mainJsPath\":\"$new\"/" flutter_bootstrap.js
grep -q "\"mainJsPath\":\"$new\"" flutter_bootstrap.js

echo "$new"
