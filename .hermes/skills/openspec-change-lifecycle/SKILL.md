---
name: openspec-change-lifecycle
description: Booking-specific discipline for creating, verifying, and archiving OpenSpec changes — the judgement calls the generic OpenSpec docs do not cover. Use when archiving a change, auditing whether a change is really done, or promoting delta specs.
version: 1.0.0
metadata:
  hermes:
    tags: [openspec, specs, archive, process]
    category: project-workflow
---

# OpenSpec Change Lifecycle (Booking)

## When to Use

Load this when you are about to **archive** a change, **judge whether a change is done**, or
**promote delta specs** into `openspec/specs/`.

This skill deliberately does **not** repeat the generic OpenSpec workflow, CLI flags, or delta
syntax. Those live in [`openspec/AGENTS.md`](../../../openspec/AGENTS.md) and
`openspec/reference/`. What follows is only the project-specific judgement that has repeatedly
been got wrong in this repository.

## Procedure

### 1. Never trust `tasks.md` — verify against the repository

A change's checkbox count is **not** evidence. In this repo, task lists have repeatedly lagged
the code by months: changes have sat at "7/17" and "15/22" while the work they describe was
fully shipped, tested, and recorded in an accepted ADR.

Before judging a change complete, confirm each claimed deliverable against a **file, migration,
test, or CLI output**. When the task list and the repository disagree, **the repository wins** —
then fix the task list so the next reader is not misled.

### 2. Archive in dependency order

When one change creates a base capability and another extends it, archive the **creator first**,
or the extension's delta lands against a capability that does not exist yet.

### 3. Use `--skip-specs` when deltas would corrupt main specs

`MODIFIED` and `REMOVED` deltas **replace or delete whole requirements** at archive time. A change
whose model has since been superseded will therefore *overwrite current specs with the retired
design* if archived normally.

Before archiving, check:

```bash
grep -rls "^## \(MODIFIED\|REMOVED\) Requirements" openspec/changes/<change-id>/specs/
```

If the change's model has been superseded, archive with `--skip-specs` and record why in the
commit message. Preserving history is not worth corrupting the specs that describe today.

### 4. Carry follow-ups forward — an archive is not where a defect goes to die

Changes routinely discover defects outside their own scope and note them as "tracked elsewhere."
If "elsewhere" does not exist, archiving **loses the finding**. Before archiving, move every
known-but-unowned defect into a real change or an explicit checklist entry.

### 5. Write the capability `Purpose`

The archiver seeds `## Purpose` with `TBD - created by archiving change <name>. Update Purpose
after archive.` — and it is almost never updated. Most capabilities in `openspec/specs/` still
carry that placeholder.

Write one real sentence **as part of archiving**, not as a later sweep. A capability whose Purpose
is a TODO cannot answer the "what does it do?" question that
[`docs/KNOWLEDGE.md`](../../../docs/KNOWLEDGE.md) assigns to it.

### 6. Validate, then commit the archive on its own

```bash
openspec validate --all --strict
```

Commit archive moves and their spec promotions **separately** from feature work, so they are not
lost inside a large commit.

## Pitfalls

- **A change that is 100% coded can still be blocked** on credentials, hardware, or a device.
  Those are release gates, not engineering work — track them separately rather than leaving the
  change looking 70% done.
- **`MODIFIED` against a capability that does not exist in main specs** is a no-op that silently
  drops content. It must be `ADDED`.
- **Superseded changes should be archived, not executed.** Check whether a newer change's proposal
  explicitly retires this one's mechanism before working its remaining tasks.

## Verification

```bash
openspec list                    # active changes and progress
openspec validate --all --strict # must be N passed / 0 failed
grep -rl "TBD - created by archiving" openspec/specs/ | wc -l   # should trend to 0
```
