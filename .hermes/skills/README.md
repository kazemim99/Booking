# Project Skills

Reusable knowledge for AI assistants working in this repository, in the
[agentskills.io](https://agentskills.io) layout: one directory per skill, containing a `SKILL.md`
with YAML frontmatter and optional `references/` files loaded on demand.

## Why this directory exists

The repository already says *what the system is* (`openspec/project.md`), *what it does*
(`openspec/specs/`), and *why* (`ARCHITECTURAL_DECISIONS.md`). What it had no home for was
**how we work** and **how to navigate a 465-file documentation corpus**. That is what lives here.

## The skills

| Skill | Answers |
|---|---|
| [`openspec-change-lifecycle/`](openspec-change-lifecycle/) | Booking's own discipline for verifying and archiving OpenSpec changes — the judgement the generic docs omit. |
| [`verify-before-claiming/`](verify-before-claiming/) | How to establish that a claim about this repo is true before writing it somewhere durable. |

## Rules these skills follow

Per [`docs/KNOWLEDGE.md`](../../docs/KNOWLEDGE.md):

- **Procedure and navigation only.** No skill asserts how the system behaves — that belongs in
  `project.md`, a spec, or an ADR. A skill that drifts into architecture is a bug.
- **Everything is re-derivable.** Every fact a skill states can be recomputed from the repository
  (a date from `git log`, a count from `ls`). Each skill ends with the commands to do so.
- **The source always wins.** Skills point at files; they never replace them. If a skill and the
  file it cites disagree, the file is right.

## Not tied to any one tool

These are plain markdown. Read them directly, or `cat` them into any assistant.

The directory is named `.hermes/skills/` because Hermes discovers project skills there
automatically (it also accepts `.agents/skills/`). Nothing about the *content* is
Hermes-specific — moving the directory is a `git mv` plus updating the handful of links in
`docs/KNOWLEDGE.md`, `openspec/AGENTS.md`, and this file.

Hermes requires an explicit trust step before loading project skills:

```bash
hermes skills trust      # run once, from the repository root
```

Hermes's autonomous skill curator **never modifies project-local skills** — these files change
only when a human or an agent edits them deliberately and commits the result.
