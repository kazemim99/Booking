# Autonomous Engineering Operating Model

Status: **adopted 2026-09-08.** The governing text is `AGENTS.md › Operating Model`; this document
is the design and rationale behind it. Approved with one adjustment to §3: clear bug fixes,
broken endpoints, incorrect behavior, architecture defects, legacy cleanup, and development
migrations are handled autonomously and recorded (tier 2). Tier 3 is reserved for genuinely
unresolved decisions about product/business behavior, money or payment semantics, security or
privacy semantics, irreversible production data loss, and genuinely ambiguous UX.

Implementation: `scripts/verify.{ps1,sh}`, `.claude/settings.json`, `.claude/hooks/stop-gate`,
`.claude/hooks/context-brief`, `.claude/commands/implement.md`,
`.hermes/skills/implementation-loop/`, `scripts/agent-run.ps1`. Rollout record:
`openspec/changes/_inline/autonomous-operating-model/tasks.md`.

## 1. The problem, precisely

Today every task is driven by hand: the assistant investigates, delivers an increment, summarizes,
and waits for "continue". The written policy already forbids that — the uncommitted
*Autonomous Execution* section in `AGENTS.md` says "keep going", names five stop conditions, and
declares `tasks.md` the loop state. It has not changed behavior because **nothing enforces it and
nothing makes "done" checkable**. Six concrete causes, all visible in the repository:

| # | Cause | Evidence |
|---|---|---|
| 1 | Stopping is free. No hook, script, or check runs when the assistant ends its turn. | `~/.claude/settings.json` has only `PreToolUse(Grep\|Glob)` and `SessionStart` hooks; `.claude/settings.local.json` has none. |
| 2 | "Done" is not a command. The gates (build, unit, architecture, composition, integration, `flutter analyze`, ESLint) are listed in prose; there is no single script that runs them and records the result. | `scripts/` holds SQL and one observability script; `Booksy.sln` has 13 test projects with no runner. |
| 3 | `tasks.md` has become a journal, not loop state. Single checkboxes carry 10-line paragraphs; `[~]` means "partly"; lines are found stale months later. | `openspec/changes/refactor-identity-and-membership/tasks.md` lines 4, 9, 10, 35; `.hermes/skills/openspec-change-lifecycle` §1 exists *because* task lists lag reality. |
| 4 | The decision policy defaults to asking. "Ambiguous business decisions are confirmed before coding" is right for product rules, but there is no tier for reversible engineering choices, so asking is always the safe move. | `AGENTS.md` stop condition 2 and *Investigation Before Implementation*. |
| 5 | Policy is duplicated in three places (global `~/.claude/CLAUDE.md`, `c:\Repos\CLAUDE.md`, `AGENTS.md`), and the Booking `CLAUDE.md` is ~300 lines of ops runbook. Long, repeated instructions dilute the ones that matter. | Compare *Test-First Development* in `~/.claude/CLAUDE.md` and `AGENTS.md` §Test-First. |
| 6 | Compaction and restarts drop the "what am I doing" context, and nothing re-injects it. | Only the codebase-memory reminder fires on `compact`/`resume`. |

Permissions are **not** a cause: `defaultMode` is `auto` and `Bash(*)`/`PowerShell(*)` are allowed.

## 2. Operating model in one page

```
Intake ──▶ Investigate ──▶ Plan ──▶ ┌─────────── Execute loop ───────────┐ ──▶ Finish ──▶ Report
                                    │ pick next [ ] task                  │
                                    │ write/extend the failing test       │
                                    │ implement the minimum               │
                                    │ verify FAST tier                    │
                                    │ fix until green (≤3 tries/failure)  │
                                    │ check the task off (own edit)       │
                                    │ record any decision taken           │
                                    └──── repeat while [ ] tasks remain ──┘
```

- **Intake.** Classify the request: bug, small change, or feature. Features get an OpenSpec change
  (`openspec/changes/<id>/`); bugs and small changes get an inline task list in the same format
  (§5) under `openspec/changes/_inline/<slug>/tasks.md` so the loop state is a file in every case.
- **Investigate.** Read `openspec/project.md`, the relevant `specs/<capability>/spec.md`, existing
  tests, and the code paths involved. Write down what must *not* change. Never implement from a
  description alone (existing rule, unchanged).
- **Plan.** Produce `tasks.md` in the §5 format: acceptance scenarios first, then tasks, each one
  small enough to verify alone. Plans are not approved by chat; a proposal is approved once, and
  the task list is then executed without check-ins.
- **Execute loop.** The block above. The FAST verify tier runs after every task; the FULL tier
  runs at Finish. A task is checked off only after its own verification is green.
- **Finish.** FULL verify tier green, `tasks.md` reflects reality, `Status:` line set, one report.
- **Stop conditions** are the five already drafted (done, business rule, protected area, blocked,
  budget) with one refinement: *blocked* and *business rule* stops park the task with a
  `BLOCKED:` or `DECISION:` marker and move on; the run stops only when no unblocked task remains.

## 3. Decision policy — the four tiers

The single rule: **if a git revert undoes it and it is inside the approved scope, decide it. If
not, ask.** Everything below is that rule made concrete.

| Tier | What | Action |
|---|---|---|
| **0 — Decide silently** | Naming, file placement, following the existing pattern of the module, test structure, refactors with no behavior change. | Just do it. |
| **1 — Decide and record** | Engineering trade-offs within the spec: which of two existing patterns, test level chosen, default values, error messages, additive DTO fields, internal API shape, where a guard lives (validator vs aggregate). | Do it; add one line under `## Decisions` in `tasks.md` with the alternative rejected and why. |
| **2 — Decide, record, flag** | Clear bug fixes and corrections of broken behavior (broken endpoints, incorrect results, architecture defects), legacy cleanup, development migrations (written, applied only to local/test databases), additive public API changes, performance trade-offs on hot paths, dependency additions, deviations from a documented pattern. | Do it; record it; list it under *Decisions needing a look* in the final report. |
| **3 — Park and ask** | Genuinely unresolved decisions about **product or business behavior** the spec does not define or that changes an approved scenario; **money or payment semantics**; **security or privacy semantics**; **irreversible production data loss** (destructive migrations against shared data, ledger rewrites, deleting or weakening tests); **genuinely ambiguous UX requirements**. | Park the task with `DECISION:` and the question, continue with unblocked tasks, ask in the final report. |

A behavior change is not tier 3 because it is visible or outward-facing; it is tier 3 because
nobody has decided it and the choice is the business's to make. Outward *operations* (push, PR,
deploy, shared environments) are not decisions at all — they are protected mechanically by the
`ask` permission list (§7.4). Tier 3 is the *only* tier that may end a run early, and only when
nothing unblocked remains. "Requires judgment" is not tier 3; "requires someone else's authority" is.

## 4. What changes in behavior

| Situation today | Under this model |
|---|---|
| Deliver an increment, summarize, wait for "continue". | Continue until a stop condition fires. Summaries are written to `tasks.md`, not to chat. |
| Ask "should I also add the test?" | Tests are part of the task by policy; the loop writes them first. |
| Ask which of two reasonable designs to use. | Tier 1: pick the one matching existing code, record the alternative. |
| Stop when a test fails twice. | Fix root cause up to three attempts on the *same* failure, then park with `BLOCKED:` and move on. |
| Stop when a product question appears. | Park that task with `DECISION:`, finish everything else, ask once at the end. |
| Report "complete" with unrun tests. | Not allowed; FULL tier must be green or the report says red. |

## 5. `tasks.md` contract (loop state)

`tasks.md` is read by the assistant after every compaction and by the stop hook on every turn end.
It must be scannable by both. Rules:

```markdown
Status: ACTIVE            # ACTIVE | DONE | STOPPED(<condition>)   — one line, first line
Verify: FAST              # FAST | FULL  — the tier a task must pass to be checked off

## Acceptance scenarios
- S1 Owner who provides services gets a StaffProfile at onboarding
- S2 ...

## Tasks
- [ ] 1.1 Add partial unique index on users(phone_number) — idempotent migration, NOT applied
- [x] 1.4 Fix inverted ExistsByEmailAsync; add ExistsByPhoneNumberAsync
- [-] 1.9 BLOCKED: needs find-duplicate-phone-numbers.sql run against prod first (see Log)
- [?] 4.3 DECISION: is a B2B join request a Manager membership or a new concept? (see Log)

## Decisions
- 2026-09-06 4.3 Grant requester's owner a Manager membership additively; rejected redefining
  ProviderJoinRequest because RequesterId is a ProviderId, not a PersonId. Reversible.

## Log
- 2026-09-08 1.8 Found both phone-verification handlers commented out while their endpoints were
  live; rewrote onto PhoneVerification aggregate. 10 unit + 3 integration tests. FOLLOW-UPS #39.
```

- One task per line, **≤ 160 characters**, imperative, independently verifiable.
- States: `[ ]` open, `[x]` done, `[-]` blocked, `[?]` awaiting a tier-3 decision. **No `[~]`**:
  a partly-done task is split into a done line and an open line.
- Narrative goes under `## Log`, dated, one paragraph per entry. Checkbox lines never grow.
- Checking off a task is its own edit, immediately after its verification passes.
- `Status:` is set to `DONE` only by the Finish step, and to `STOPPED(...)` only when a stop
  condition fires with nothing unblocked left. The stop hook reads this line.

Existing task lists are migrated lazily: the next session that touches a change moves the
paragraphs into `## Log` and splits the `[~]` lines. `openspec validate` is unaffected.

## 6. Instruction layering — where each rule lives

The assistant reads four files every session. Each gets one job; nothing is repeated.

| File | Job | Size target |
|---|---|---|
| `~/.claude/CLAUDE.md` (personal, all repos) | Personal defaults only: RTK prefix, tone, the one-line autonomy default ("carry tasks to a verified finish; ask only for tier-3 decisions"). **Remove** the Test-First section; it lives in each repo's `AGENTS.md`. | ≤ 60 lines |
| `c:\Repos\CLAUDE.md` (workspace) | Project index and pointers. Unchanged except: drop the duplicated Test-First paragraph in favor of a pointer. | as is |
| `Booking/AGENTS.md` (repo policy) | **Reordered**: §Source of truth → §Operating Model (the loop, tiers, stop conditions, `tasks.md` contract, verify tiers) → §Test policy → §Mobile checklist moved to a skill. The operating model comes *before* the test policy because it governs every turn. | ≤ 260 lines |
| `Booking/CLAUDE.md` | Routing only: read `AGENTS.md`; run `scripts/verify`; where docs live. The deployment runbook moves to `docs/DEPLOYMENT_RUNBOOK.md` and is linked. | ≤ 80 lines |
| `Booking/.claude/settings.json` (**checked in**, new) | Hooks and the `ask` permission list (§7). Personal allow-lists stay in `settings.local.json`. | — |
| `.hermes/skills/` | Procedures loaded on demand (existing). Add `mobile-test-checklist` (moved from AGENTS.md) and `implementation-loop` (the loop prompt, vendor-neutral). | — |

### 6.1 Drop-in replacement for the *Autonomous Execution* section of AGENTS.md

This replaces the uncommitted draft (working tree, `AGENTS.md` lines 290–355). Differences from
the draft are: the decision tiers, the park-and-continue rule, the `tasks.md` contract, and the
named verify tiers.

```markdown
## Operating Model — own the task from intake to a verified finish

This section governs every turn. It grants autonomy over execution and over reversible
engineering decisions. It grants none over product behavior, security semantics, irreversible
data changes, money, or anything that leaves the working tree.

### The loop

1. **Intake.** Feature → OpenSpec change. Bug or small change → inline task list at
   `openspec/changes/_inline/<slug>/tasks.md`. Either way the loop state is a file.
2. **Investigate** per *Investigation Before Implementation*. Write down what must not change.
3. **Plan** `tasks.md` in the contract format below: acceptance scenarios, then small tasks.
4. **Execute.** For each open task: write or extend the failing test → implement the minimum →
   `scripts/verify -Tier fast` → fix root cause until green → check the task off in its own
   edit → record any decision taken. Repeat while open tasks remain.
5. **Finish.** `scripts/verify -Tier full` green → `tasks.md` reflects reality → `Status: DONE`
   → one report.

Do not stop between tasks to summarize, to ask whether to proceed, or to seek approval for an
item on an already-approved list. The proposal is the approval. A task being large, tedious, or
spanning many files is not a reason to stop.

### Decisions — four tiers

Rule: if a git revert undoes it and it is inside the approved scope, decide it; if not, ask.

- **Tier 0, decide silently:** naming, placement, following the module's existing pattern,
  behavior-preserving refactors.
- **Tier 1, decide and record:** engineering trade-offs within the spec (pattern choice, test
  level, defaults, internal shapes). One line under `## Decisions` in `tasks.md`.
- **Tier 2, decide, record, flag:** additive public API changes, additive migrations (written,
  never applied to a shared database), hot-path performance trade-offs, new dependencies,
  documented-pattern deviations. Recorded, and listed in the final report.
- **Tier 3, stop and ask:** product behavior the spec does not define or that alters an approved
  scenario; security semantics (authn/authz, exposure, token lifetimes, secrets); irreversible
  data changes (destructive migrations, ledger/payments/settlements/refunds/payouts, deleting or
  weakening tests); anything outward (push, PR, deploy, shared environments, external services).

Tier 3 parks the task as `[?] DECISION: <question>` and the loop continues with unblocked tasks.
The run ends only when nothing unblocked remains. "Needs judgment" is tier 1; "needs someone
else's authority" is tier 3.

### Stop conditions — name the one that fired

1. **Done.** Every task `[x]` or parked, `scripts/verify -Tier full` green.
2. **Decision pending** (tier 3) and no unblocked task remains.
3. **Blocked** on something the environment cannot produce (credential, external service,
   broken toolchain) and no unblocked task remains. Park as `[-] BLOCKED: <reason>`.
4. **Budget exhausted:** three failed attempts at the *same* failure. Park it, report the actual
   error. Repeated failure means the premise is wrong.

### `tasks.md` contract

First line `Status: ACTIVE|DONE|STOPPED(<condition>)`; second line `Verify: FAST|FULL`.
Sections: `## Acceptance scenarios`, `## Tasks`, `## Decisions`, `## Log`.
Tasks are one line each, ≤ 160 chars, states `[ ] [x] [-] [?]` — never `[~]`; split partial
work. Narrative goes in `## Log`, dated. Check a task off in its own edit, right after its
verification passes. The file must describe reality after a crash at any point.

### Verification tiers

- **FAST** (after each task): `dotnet build` + unit + architecture + composition tests, plus the
  affected integration test class(es) when the task touched persistence, API, or events.
- **FULL** (at finish): FAST + all integration suites against the test Postgres + frontend
  lint/type-check + `flutter analyze`/`flutter test` for any touched app.
- Unrun tests are not a finish. A red run is reported red.

### Reporting

One report at the end: stop condition fired; what changed; the Testing Summary; decisions
needing a look (tier 2 and 3); anything parked and why.
```

## 7. Mechanics — making the policy cost something

Three hooks, one script, one permission list. All live in the repository so every clone and every
assistant gets them.

### 7.1 `scripts/verify.ps1` and `scripts/verify.sh`

```
scripts/verify.ps1 -Tier fast|full [-Filter <dotnet test filter>] [-All] [-SkipBuild]
scripts/verify.sh  fast|full [--filter ..] [--all] [--skip-build]
```

- FAST: `dotnet build Booksy.sln`, then `dotnet test` on the seven unit/architecture projects.
  No Docker. (As built: Host composition tests boot Testcontainers Postgres, so they are FULL.)
- FULL: FAST, then Host composition + both integration projects (Testcontainers starts its own
  Postgres; no compose file needed), `type-check`/`lint:check` in each touched Vue app,
  `flutter analyze`/`flutter test` in each touched Flutter app. Without Docker the DB steps are
  reported `blocked`, and the overall result is `blocked`, never `pass`. The Reqnroll Gherkin
  features are excluded by default (`-IncludeFeatures` to run them): the repository documents
  them as a specification backlog and credentials-blocked, so they cannot be a gate.
- `tests/known-failures.txt` is the recorded baseline of integration tests that were already
  red before a change (measured on the committed HEAD in a clean worktree). A db step passes
  when all of its failures are on the list and fails on any that is not, so the gate enforces
  "no new regressions" today while the debt stays visible. Lines are removed as tests are
  fixed and never added to make a run green.
- Writes `.verify/status.json` (git-ignored): `{ sha, tree, tier, result, failed[], blocked[],
  steps[] }`. `tree` is `git write-tree` over a temporary index (`read-tree HEAD` + `add -A`),
  so any later edit — tracked or untracked — makes the run stale. The stop hook computes the
  same hash.
- Exit code is the result. Output is failures and summaries only; full logs in `.verify/logs/`.

### 7.2 Stop hook — the implementation loop

`.claude/hooks/stop-gate` (bash, matches the existing hook style), registered under `Stop` in
`.claude/settings.json`. On every turn end:

1. Find the active change: the `tasks.md` under `openspec/changes/**` most recently modified,
   whose first line is `Status: ACTIVE`. None → allow the stop.
2. If the per-session counter in `.verify/stop-count` exceeds 20 blocks → allow (loop breaker)
   and log the reason to `.verify/stop-gate.log`.
3. Count `- [ ]` lines. If any remain → **block** with reason:
   `N open tasks in <change>: <first three>. Continue the loop, or set Status: STOPPED(<condition>) with the reason.`
4. If none remain but `.verify/status.json` is missing, not tier `full`, not `result: pass`, or
   its `sha`/`tree` differ from the current commit and working tree → **block** with reason:
   `All tasks checked but FULL verify is <missing|stale|failed>. Run scripts/verify -Tier full.`
5. Otherwise allow. If `Status:` is `DONE` or `STOPPED(...)`, allow without checks: the assistant
   has explicitly named its stop condition, which is what we want it to do.

The block reason is fed back to the assistant as the next instruction, so "continue" is now typed
by the hook instead of by a person. The explicit `Status:` escape means a legitimate tier-3 stop
is never fought by the hook.

### 7.3 Context re-injection

A `SessionStart` (`startup`, `resume`, `compact`) hook, `.claude/hooks/context-brief`,
that prints: active change id, `Status:` line, counts of `[ ] [-] [?]`, the first five open tasks,
the last `## Decisions` entry, and the last verify result. Twenty lines, so the assistant knows
where it is without re-reading the change.

### 7.4 Protected areas as permissions, not prose

In `.claude/settings.json` (checked in; the `rtk`-prefixed forms are listed too, since this
machine wraps every command in `rtk`):

```json
{
  "permissions": {
    "ask": [
      "Bash(git push*)", "Bash(rtk git push*)", "PowerShell(git push*)",
      "Bash(gh pr create*)", "Bash(gh pr merge*)",
      "Bash(dotnet ef database update*)", "PowerShell(dotnet ef database update*)",
      "Bash(docker compose -f docker-compose.prod.yml*)", "... staging ...",
      "Bash(ssh *)", "Bash(scp *)"
    ]
  },
  "hooks": {
    "Stop": [{ "hooks": [{ "type": "command", "command": ".claude/hooks/stop-gate", "timeout": 10 }] }],
    "SessionStart": [
      { "matcher": "resume", "hooks": [{ "type": "command", "command": ".claude/hooks/context-brief" }] },
      { "matcher": "compact", "hooks": [{ "type": "command", "command": ".claude/hooks/context-brief" }] }
    ]
  }
}
```

The global `defaultMode: auto` stays. The `ask` list is the mechanical form of "anything
outward-facing needs confirmation": it prompts even in auto mode, and it cannot be forgotten.
Migrations are covered too: writing one is free, applying one asks.

### 7.5 Driver commands

- `.claude/commands/implement.md` → `/implement <change-id>`: loads `proposal.md`, `design.md`,
  `tasks.md`, and runs the loop from §6.1. The vendor-neutral twin lives in
  `.hermes/skills/implementation-loop/SKILL.md` so Hermes and Cursor run the same loop.
- `/loop /implement <change-id>` for a self-paced long run inside one interactive session.
- `scripts/agent-run.ps1 <change-id>` for unattended runs: repeats
  `claude -p "/implement <change-id>" --permission-mode acceptEdits` until the first line of
  `tasks.md` is `DONE` or `STOPPED(...)`, capped at N iterations, appending each report to
  `openspec/changes/<id>/RUNS.md`. This is the overnight mode; it relies on the `ask` list and
  the tier-3 rule to stay inside the working tree.

## 8. Rollout

Executed 2026-09-08; the live record is `openspec/changes/_inline/autonomous-operating-model/tasks.md`.

| Step | Deliverable | Why first |
|---|---|---|
| 1 | Commit the §6.1 section into `AGENTS.md` replacing the draft; trim the duplicate policy from `~/.claude/CLAUDE.md` and `c:\Repos\CLAUDE.md`. | The rule text has to exist before hooks can point at it. |
| 2 | `scripts/verify.{ps1,sh}` + `docker-compose.test.yml` + `.verify/` in `.gitignore`. Prove FAST < 3 min and FULL green on `master`. | "Done" becomes a command. |
| 3 | `.claude/settings.json` with the `ask` list, `stop-gate`, `context-brief`. Run one real change through it. | The loop becomes enforced. |
| 4 | Migrate `refactor-identity-and-membership/tasks.md` to the contract (paragraphs → `## Log`, split `[~]`). | The first real loop state; proves the format on the hardest example. |
| 5 | `/implement` command, `implementation-loop` skill, `agent-run.ps1`. | Unattended mode. |
| 6 | After two changes: count "continue" prompts per change and tier-3 questions per change. Target: zero of the former; the latter should all be genuine product/security/data questions. | Measures the model instead of trusting it. |

Steps 1–2 are a day. Steps 3–5 are another. Step 4 is a mechanical edit of one file.

## 9. Risks and the guards for them

- **The stop hook loops forever.** Guard: the 20-block counter, and the explicit `Status:` escape.
  A run that hits the cap prints why, and the report lists it as a budget stop.
- **FULL verify is too slow to run per task.** It isn't run per task; FAST is. FAST excludes
  integration except the touched classes. If FAST creeps past 3 minutes, split the unit projects,
  not the tier.
- **"Decide and record" hides bad calls.** Tier 2 items are surfaced in every report, and every
  tier-1 line in `## Decisions` names the alternative. Review is one file, not a transcript.
- **Something outward slips through auto mode.** The `ask` list is pattern-based and lives in
  git; extend it whenever a new outward command appears. It is the last line, not the only one:
  tier 3 remains in the rules the assistant follows.
- **Integration tests need a live Postgres and the machine doesn't have one.** `verify` starts it
  via compose; if Docker is absent, FULL reports `blocked` (not `pass`) and the stop hook treats
  that as a failed finish, so the report is honest.
- **Instructions are still too long to be followed.** The size targets in §6 are the guard; the
  operating model goes first in `AGENTS.md` because position matters more than emphasis.

## 9a. Several sessions, one checkout (learned on the first run)

The first loop run collided with a second interactive session working the same change in the
same working tree. Two consequences shaped the rules:

- **Build output is a shared lock.** `bin/obj` is one set of files; concurrent `dotnet build`
  or `test` runs fail on `MSB3027 ... locked by testhost`. A long-running `testhost` is a peer's
  integration run, not a leftover. The rule (AGENTS.md › Protected operations › Shared machine):
  never kill a process you did not start; `ListAgents` + `SendMessage` to take turns.
- **`tasks.md` is shared loop state.** Two sessions can check off, split, and add lines. The
  contract holds up: one-line tasks merge cleanly, and the Stop hook picks the most recently
  modified ACTIVE list. Re-read before editing; use exact-match edits, never whole-file writes.
- **Ownership by task, not by file.** Each session names the task numbers it holds in the
  `## Log` and stays off the other's files; the final report names both.

## 10. What this does not change

- Test-first, test integrity, the test pyramid, and the source-of-truth ordering: unchanged.
- OpenSpec as the planning format: unchanged; the `tasks.md` contract is a stricter subset.
- Proposals are still approved by a person before implementation. Autonomy starts *after*
  approval and ends at the working tree.
