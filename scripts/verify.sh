#!/usr/bin/env bash
# The checkable definition of done (POSIX twin of verify.ps1 — same tiers, same status file).
#
#   scripts/verify.sh [fast|full] [--filter <dotnet test filter>] [--all] [--skip-build]
#
# FAST  build AsanRezerve.sln + every unit/architecture test project. No Docker.
# FULL  FAST + Host composition + both integration suites (Testcontainers Postgres) +
#       type-check/lint in touched Vue apps + analyze/test in touched Flutter apps.
# Writes .verify/status.json; `tree` is a hash of the working tree so later edits make it stale.
set -u
TIER=fast; FILTER=""; ALL=0; SKIP_BUILD=0
while [ $# -gt 0 ]; do
  case "$1" in
    fast|full) TIER="$1" ;;
    --filter) FILTER="$2"; shift ;;
    --all) ALL=1 ;;
    --skip-build) SKIP_BUILD=1 ;;
    *) echo "unknown arg: $1" >&2; exit 2 ;;
  esac; shift
done

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT" || exit 2
VERIFY_DIR="$ROOT/.verify"; LOG_DIR="$VERIFY_DIR/logs"; mkdir -p "$LOG_DIR"
STARTED=$(date -u +%Y-%m-%dT%H:%M:%SZ); T0=$(date +%s)
STEPS_JSON=""; FAILED=""; BLOCKED=""; COUNT=0

step() { # name dir command...
  local name="$1" dir="$2"; shift 2
  local log="$LOG_DIR/$(printf '%s' "$name" | tr -c 'A-Za-z0-9._-' '_').log"
  local s=$(date +%s)
  echo; echo "== $name"; echo "   $*"
  ( cd "$dir" && "$@" ) >"$log" 2>&1; local code=$?
  local secs=$(( $(date +%s) - s )); local result=pass
  grep -E '^\s*Failed |Passed!|Failed!|error [A-Z]+[0-9]+|Build succeeded|Build FAILED|error TS|No issues found|All tests passed|Some tests failed' "$log" | sort -u | sed 's/^/   /'
  # tests/known-failures.txt: recorded baseline of integration tests that already failed before a
  # change (see its header). A db step whose failures are ALL listed passes with a note; any
  # failure off the list keeps it red. Never consulted for unit steps.
  if [ $code -ne 0 ] && [[ "$name" == db:* ]] && [ -f "$ROOT/tests/known-failures.txt" ] && grep -q '\[FAIL\]' "$log"; then
    local failing unexpected
    failing=$(grep -E '\[FAIL\] *$' "$log" | sed -E 's/^\[xUnit\.net [0-9:.]+\] +//; s/ *\[FAIL\] *$//' | sort -u)
    unexpected=$(printf '%s\n' "$failing" | grep -vxF -f <(grep -v '^#' "$ROOT/tests/known-failures.txt" | sed '/^$/d') || true)
    if [ -z "$unexpected" ]; then
      code=0; echo "   PASS with $(printf '%s\n' "$failing" | wc -l) known-baseline failure(s) (all listed in tests/known-failures.txt)"
    else
      echo "   failure(s) NOT on tests/known-failures.txt (regressions until proven otherwise):"; printf '%s\n' "$unexpected" | sed 's/^/     /'
    fi
  fi
  if [ $code -ne 0 ]; then result=fail; FAILED="$FAILED $name"; echo "   --- last 40 lines of $log ---"; tail -40 "$log" | sed 's/^/   /'; fi
  echo "   $(echo $result | tr a-z A-Z)  ${secs}s"
  STEPS_JSON="$STEPS_JSON$( [ $COUNT -gt 0 ] && echo , ){\"name\":\"$name\",\"result\":\"$result\",\"seconds\":$secs,\"log\":\"$log\"}"
  COUNT=$((COUNT+1))
}
blocked() { # name why
  echo; echo "== $1"; echo "   BLOCKED: $2"; BLOCKED="$BLOCKED $1"
  STEPS_JSON="$STEPS_JSON$( [ $COUNT -gt 0 ] && echo , ){\"name\":\"$1\",\"result\":\"blocked\",\"seconds\":0,\"reason\":\"$2\"}"
  COUNT=$((COUNT+1))
}
touched() { # prefix
  [ $ALL -eq 1 ] && return 0
  { git diff --name-only HEAD; git ls-files --others --exclude-standard
    base=$(git merge-base HEAD master 2>/dev/null) && git diff --name-only "$base" HEAD; } 2>/dev/null | grep -q "^$1/"
}
tree_hash() {
  # Per-run temp index: several sessions share this checkout, and a fixed path let two
  # overlapping verify runs race for one index.lock, leaving the loser with no hash at all.
  local idx="$VERIFY_DIR/index-$$"
  GIT_INDEX_FILE="$idx" git read-tree HEAD 2>/dev/null
  GIT_INDEX_FILE="$idx" git -c core.safecrlf=false add -A 2>/dev/null
  GIT_INDEX_FILE="$idx" git write-tree 2>/dev/null
  rm -f "$idx"
}

slowest() { # trxdir outfile — the 20 slowest tests of this run; a class's first test carries its fixture (host boot)
  local dir="$1" out="$2" tmp; tmp=$(mktemp)
  for trx in "$dir"/*.trx; do
    [ -f "$trx" ] || continue
    awk '
      /<UnitTest / { id=""; name=""; m=$0
        if (match(m,/ id="[^"]*"/)) id=substr(m,RSTART+5,RLENGTH-6)
        if (match(m,/ name="[^"]*"/)) name=substr(m,RSTART+7,RLENGTH-8)
        want=id }
      want!="" && /<TestMethod / { c=$0
        if (match(c,/className="[^"]*"/)) { cls=substr(c,RSTART+11,RLENGTH-12); n=split(cls,a,"."); cls=a[n] }
        names[want]=cls "." name; want="" }
      /<UnitTestResult / { r=$0; tid=""; d=""; o=""
        if (match(r,/testId="[^"]*"/)) tid=substr(r,RSTART+8,RLENGTH-9)
        if (match(r,/duration="[^"]*"/)) d=substr(r,RSTART+10,RLENGTH-11)
        if (match(r,/outcome="[^"]*"/)) o=substr(r,RSTART+9,RLENGTH-10)
        if (d!="") { split(d,t,":"); s=t[1]*3600+t[2]*60+t[3]; printf "%10.2f  %s  [%s]\n", s, names[tid], o } }
    ' "$trx" >>"$tmp"
  done
  [ -s "$tmp" ] || { rm -f "$tmp"; return; }
  { echo "# slowest 20 of $(wc -l <"$tmp") tests, $(date -u +%Y-%m-%dT%H:%M:%SZ). First test of a class includes its fixture (host boot)."
    sort -rn "$tmp" | head -20
    echo "# total test time $(awk '{s+=$1} END {printf "%.1f", s}' "$tmp")s"; } >"$out"
  rm -f "$tmp"; echo "   slowest tests: $out"
}

echo "verify  tier=$TIER  root=$ROOT"
[ $SKIP_BUILD -eq 0 ] && step build "$ROOT" dotnet build AsanRezerve.sln --nologo -v q

# The solution was just built; without --no-build every `dotnet test` re-evaluates the project graph
# (5-16 s per unit project, ~30 s per integration project, ~135 s per FULL run). With --skip-build the
# caller vouched for the build, so each step keeps its own incremental build.
NOBUILD=(); [ $SKIP_BUILD -eq 0 ] && NOBUILD=(--no-build)

for p in tests/AsanRezerve.Core.Domain.UnitTests tests/AsanRezerve.Infrastructure.Core.UnitTests \
         tests/AsanRezerve.ServiceCatalog.Domain.UnitTests tests/AsanRezerve.ServiceCatalog.Application.UnitTests \
         tests/AsanRezerve.ServiceCatalog.Api.UnitTests tests/AsanRezerve.Infrastructure.External.UnitTests \
         tests/AsanRezerve.ServiceCatalog.Infrastructure.UnitTests \
         tests/AsanRezerve.UserManagement.Application.UnitTests \
         tests/AsanRezerve.ArchitectureTests; do
  step "unit:$(basename "$p")" "$ROOT" dotnet test "$p" ${NOBUILD[@]+"${NOBUILD[@]}"} --nologo -v q
done

if [ "$TIER" = full ]; then
  if docker info >/dev/null 2>&1; then DOCKER=1; else DOCKER=0; fi
  # Clear the previous run, including the GUID folders the blame collector leaves behind.
  TRX_DIR="$VERIFY_DIR/trx"; mkdir -p "$TRX_DIR"; rm -rf "${TRX_DIR:?}"/*
  # One project since docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 4 (was three: SC, UM and
  # Composition each booted their own host).
  for p in tests/AsanRezerve.Host.IntegrationTests; do
    n="db:$(basename "$p")"
    if [ $DOCKER -eq 0 ]; then blocked "$n" "Docker is not running; Testcontainers cannot start Postgres"; continue; fi
    clauses=""
    if [ -n "$FILTER" ] && [[ "$p" == *IntegrationTests ]]; then clauses="${clauses:+$clauses&}($FILTER)"; fi
    # trx per project for per-test timings (slowest.txt); --blame-hang-timeout turns a hung
    # concurrency test into a dump with a stack instead of a silent ten-minute wait.
    logger=(--logger "trx;LogFileName=$(basename "$p").trx" --results-directory "$TRX_DIR" --blame-hang-timeout 5m)
    if [ -n "$clauses" ]; then step "$n" "$ROOT" dotnet test "$p" ${NOBUILD[@]+"${NOBUILD[@]}"} --nologo -v q "${logger[@]}" --filter "$clauses"
    else step "$n" "$ROOT" dotnet test "$p" ${NOBUILD[@]+"${NOBUILD[@]}"} --nologo -v q "${logger[@]}"; fi
  done
  slowest "$TRX_DIR" "$VERIFY_DIR/slowest.txt"
  for app in asanrezerve-frontend asanrezerve-admin; do
    touched "$app" || continue
    [ -d "$app/node_modules" ] || { blocked "vue:$app" "no node_modules; run 'npm ci' in $app"; continue; }
    step "vue:$app:type-check" "$ROOT/$app" npm run --silent type-check
    # asanrezerve-admin's vitest suite was never run by any gate — see verify.ps1.
    [ "$app" = asanrezerve-admin ] && step "vue:$app:unit" "$ROOT/$app" npx vitest run
    [ "$app" = asanrezerve-frontend ] && step "vue:$app:lint" "$ROOT/$app" npm run --silent lint:check
    # Unit tests — see verify.ps1 for why, and for the two excluded EMPTY placeholder specs.
    [ "$app" = asanrezerve-frontend ] && step "vue:$app:unit" "$ROOT/$app" npx vitest run src --exclude src/modules/auth/__tests__/auth.api.spec.ts --exclude src/modules/auth/__tests__/LoginForm.spec.ts
  done
  for app in asanrezerve-customer-app asanrezerve-provider-app; do
    touched "$app" || continue
    command -v flutter >/dev/null || { blocked "flutter:$app" "flutter not on PATH"; continue; }
    step "flutter:$app:analyze" "$ROOT/$app" flutter analyze --no-pub --no-fatal-warnings --no-fatal-infos
    step "flutter:$app:test" "$ROOT/$app" flutter test --no-pub
  done
fi

if [ -n "$FAILED" ]; then RESULT=fail; elif [ -n "$BLOCKED" ]; then RESULT=blocked; else RESULT=pass; fi
to_json_list() { printf '%s' "$1" | awk '{for(i=1;i<=NF;i++) printf "%s\"%s\"", (i>1?",":""), $i}'; }
# The filter that narrowed the integration steps, or "" when the whole tier ran. Without it a
# filtered FULL run was indistinguishable from a real one, to the Stop hook and to peer sessions.
FILTER_JSON=$(printf '%s' "$FILTER" | sed 's/\\/\\\\/g; s/"/\\"/g')
cat >"$VERIFY_DIR/status.json" <<EOF
{
  "sha": "$(git rev-parse HEAD)",
  "tree": "$(tree_hash)",
  "tier": "$TIER",
  "filter": "$FILTER_JSON",
  "result": "$RESULT",
  "startedAt": "$STARTED",
  "finishedAt": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "seconds": $(( $(date +%s) - T0 )),
  "failed": [$(to_json_list "$FAILED")],
  "blocked": [$(to_json_list "$BLOCKED")],
  "steps": [$STEPS_JSON]
}
EOF
echo; echo "verify $(echo $TIER | tr a-z A-Z): $(echo $RESULT | tr a-z A-Z)  ($COUNT steps, $(( $(date +%s) - T0 ))s)"
[ -n "$FILTER" ] && echo "  FILTERED: $FILTER  (a filtered run is not a FULL verification; the Stop hook will not accept it)"
[ -n "$FAILED" ] && echo "  failed: $FAILED"
[ -n "$BLOCKED" ] && echo "  blocked:$BLOCKED"
echo "  status:  .verify/status.json"
[ "$RESULT" = pass ]
