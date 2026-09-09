#!/usr/bin/env bash
# The checkable definition of done (POSIX twin of verify.ps1 — same tiers, same status file).
#
#   scripts/verify.sh [fast|full] [--filter <dotnet test filter>] [--all] [--skip-build]
#
# FAST  build Booksy.sln + every unit/architecture test project. No Docker.
# FULL  FAST + Host composition + both integration suites (Testcontainers Postgres) +
#       type-check/lint in touched Vue apps + analyze/test in touched Flutter apps.
# Writes .verify/status.json; `tree` is a hash of the working tree so later edits make it stale.
set -u
TIER=fast; FILTER=""; ALL=0; SKIP_BUILD=0; FEATURES=0
while [ $# -gt 0 ]; do
  case "$1" in
    fast|full) TIER="$1" ;;
    --filter) FILTER="$2"; shift ;;
    --all) ALL=1 ;;
    --skip-build) SKIP_BUILD=1 ;;
    --features) FEATURES=1 ;;
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
  local idx="$VERIFY_DIR/index"
  GIT_INDEX_FILE="$idx" git read-tree HEAD 2>/dev/null
  GIT_INDEX_FILE="$idx" git -c core.safecrlf=false add -A 2>/dev/null
  GIT_INDEX_FILE="$idx" git write-tree 2>/dev/null
  rm -f "$idx"
}

echo "verify  tier=$TIER  root=$ROOT"
[ $SKIP_BUILD -eq 0 ] && step build "$ROOT" dotnet build Booksy.sln --nologo -v q

for p in tests/Booksy.Core.Domain.UnitTests tests/Booksy.Infrastructure.Core.UnitTests \
         tests/Booksy.ServiceCatalog.Domain.UnitTests tests/Booksy.ServiceCatalog.Application.UnitTests \
         tests/Booksy.ServiceCatalog.UnitTests tests/Booksy.UserManagement.Application.UnitTests \
         tests/Booksy.ArchitectureTests; do
  ls "$p"/*.csproj >/dev/null 2>&1 || continue   # skip dirs without a project
  step "unit:$(basename "$p")" "$ROOT" dotnet test "$p" --nologo -v q
done

if [ "$TIER" = full ]; then
  if docker info >/dev/null 2>&1; then DOCKER=1; else DOCKER=0; fi
  for p in tests/Booksy.Host.CompositionTests tests/Booksy.ServiceCatalog.IntegrationTests tests/Booksy.UserManagement.IntegrationTests; do
    n="db:$(basename "$p")"
    if [ $DOCKER -eq 0 ]; then blocked "$n" "Docker is not running; Testcontainers cannot start Postgres"; continue; fi
    clauses=""
    # Reqnroll features = spec backlog (REQNROLL-COVERAGE-GAP.md, FOLLOW-UPS #31): excluded unless --features.
    if [[ "$p" == *ServiceCatalog.IntegrationTests ]] && [ $FEATURES -eq 0 ]; then clauses="FullyQualifiedName!~IntegrationTests.Features"; fi
    if [ -n "$FILTER" ] && [[ "$p" == *IntegrationTests ]]; then clauses="${clauses:+$clauses&}($FILTER)"; fi
    if [ -n "$clauses" ]; then step "$n" "$ROOT" dotnet test "$p" --nologo -v q --filter "$clauses"
    else step "$n" "$ROOT" dotnet test "$p" --nologo -v q; fi
  done
  for app in booksy-frontend booksy-admin; do
    touched "$app" || continue
    [ -d "$app/node_modules" ] || { blocked "vue:$app" "no node_modules; run 'npm ci' in $app"; continue; }
    step "vue:$app:type-check" "$ROOT/$app" npm run --silent type-check
    [ "$app" = booksy-frontend ] && step "vue:$app:lint" "$ROOT/$app" npm run --silent lint:check
  done
  for app in booksy-customer-app booksy-provider-app; do
    touched "$app" || continue
    command -v flutter >/dev/null || { blocked "flutter:$app" "flutter not on PATH"; continue; }
    step "flutter:$app:analyze" "$ROOT/$app" flutter analyze --no-pub --no-fatal-warnings --no-fatal-infos
    step "flutter:$app:test" "$ROOT/$app" flutter test --no-pub
  done
fi

if [ -n "$FAILED" ]; then RESULT=fail; elif [ -n "$BLOCKED" ]; then RESULT=blocked; else RESULT=pass; fi
to_json_list() { printf '%s' "$1" | awk '{for(i=1;i<=NF;i++) printf "%s\"%s\"", (i>1?",":""), $i}'; }
cat >"$VERIFY_DIR/status.json" <<EOF
{
  "sha": "$(git rev-parse HEAD)",
  "tree": "$(tree_hash)",
  "tier": "$TIER",
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
[ -n "$FAILED" ] && echo "  failed: $FAILED"
[ -n "$BLOCKED" ] && echo "  blocked:$BLOCKED"
echo "  status:  .verify/status.json"
[ "$RESULT" = pass ]
