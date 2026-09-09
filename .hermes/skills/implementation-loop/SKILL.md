---
name: implementation-loop
description: The autonomous implementation loop — how to take an approved change from its task list to a verified finish without check-ins. Use when implementing an OpenSpec change or an inline task list, or when asked to "continue" work that has a tasks.md.
version: 1.0.0
metadata:
  hermes:
    tags: [workflow, autonomy, verification, openspec]
    category: project-workflow
---

# Implementation Loop (Booking)

Vendor-neutral twin of the Claude Code `/implement` command. The policy itself is
**AGENTS.md › Operating Model**; this skill is the procedure.

## When to Use

An approved change exists (`openspec/changes/<id>/tasks.md`) or a bug/small change needs an
inline list (`openspec/changes/_inline/<slug>/tasks.md`), and the goal is a verified finish.

## Procedure

1. **Load state.** Read `tasks.md` (and `proposal.md`/`design.md` if present). If the file does
   not follow the contract — `Status:` first line, `Verify:` second, sections *Acceptance
   scenarios / Tasks / Decisions / Log*, one-line tasks, states `[ ] [x] [-] [?]` only —
   migrate it: narrative → `## Log` (dated), `[~]` → one `[x]` line + one `[ ]` line. Set
   `Status: ACTIVE`.
2. **Distrust the checkboxes.** Confirm each claimed deliverable against a file, test, migration,
   or command output (see `openspec-change-lifecycle` §1). Fix the list before working it.
3. **Loop** while `- [ ]` lines remain:
   - pick the next task;
   - write or extend the failing test (test-first policy in AGENTS.md);
   - implement the minimum;
   - `scripts/verify.sh fast` (PowerShell: `scripts/verify.ps1 -Tier fast`); when the task
     touched persistence/API/events, also run the affected integration class with
     `scripts/verify.sh full --filter "FullyQualifiedName~<Class>"`;
   - fix the root cause until green — three attempts at the *same* failure, then mark the task
     `[-] BLOCKED: <actual error>` and move on;
   - check the task off in its own edit;
   - record tier-1/2 decisions as one line under `## Decisions` (alternative rejected, why).
4. **Tier-3 questions** — product/business behavior the spec does not define, money or payment
   semantics, security/privacy semantics, irreversible production data loss, genuinely ambiguous
   UX — become `[?] DECISION: <question>`. Continue with everything unblocked.
5. **Finish** when nothing unblocked remains: `scripts/verify.sh full`; make `tasks.md` describe
   reality; set `Status: DONE` or `Status: STOPPED(decision|blocked|budget)`; write one report —
   stop condition, what changed, Testing Summary, decisions to review, parked items.

## Pitfalls

- Summarizing progress in chat between tasks is not progress; put it in `## Log`.
- A `[x]` with unrun tests is a lie the next session will pay for.
- Protected operations (push, PR, `ef database update`, prod/staging compose, ssh) are asked
  about mechanically; do not route around the prompt.
- `.verify/status.json` is only current for the exact tree it ran on. Any edit after a green
  FULL run means run it again before `Status: DONE`.
