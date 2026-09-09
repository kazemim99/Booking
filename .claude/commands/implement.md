---
description: Run the autonomous implementation loop on an OpenSpec change (or inline task list) until a stop condition fires.
argument-hint: <change-id | _inline/<slug>>
---
Run the implementation loop defined in **AGENTS.md › Operating Model** on `$ARGUMENTS`.

1. Open `openspec/changes/$ARGUMENTS/tasks.md` (plus `proposal.md` and `design.md` when they
   exist). If the file is not in the tasks.md contract (first line `Status:`, second `Verify:`,
   sections Acceptance scenarios / Tasks / Decisions / Log, one-line tasks, no `[~]`), migrate it
   first: paragraphs move under `## Log` dated, partial `[~]` lines split into a `[x]` and a `[ ]`.
   Set the first line to `Status: ACTIVE`.
2. Verify the task list against the repository before trusting it (`.hermes/skills/openspec-change-lifecycle` §1): a checked box is not evidence; an unchecked one may already be done.
3. Loop while `- [ ]` tasks remain: pick the next one → write or extend the failing test →
   implement the minimum → `scripts/verify.ps1 -Tier fast` (or `scripts/verify.sh fast`) → fix
   the root cause until green (three attempts on the same failure, then `[-] BLOCKED:`) → check
   the task off in its own edit → record any tier-1/2 decision under `## Decisions`.
4. Tier-3 questions (product/business behavior, money, security/privacy, irreversible production
   data, ambiguous UX) become `[?] DECISION: <question>`; keep going with everything else.
5. When nothing unblocked remains: `scripts/verify.ps1 -Tier full`, make `tasks.md` describe
   reality, set `Status: DONE` (or `STOPPED(decision|blocked|budget)`), and write the one final
   report: stop condition, what changed, Testing Summary, decisions to look at, parked items.

Do not stop between tasks to summarize or ask whether to continue. The Stop hook will send you
back until the list is closed and a green FULL verify exists for the current working tree.
