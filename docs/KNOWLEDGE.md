# Knowledge Contract

Where Booksy's knowledge lives, and which copy wins when two disagree.

This file is tool-neutral on purpose. It assumes nothing beyond a filesystem, git, and markdown.
Any agent — or any human — can be oriented by reading this page and the files it points at.

## Where each kind of knowledge belongs

| Kind | Lives in | Versioned |
|---|---|---|
| **Source code / tests** | `src/`, `tests/`, `booksy-*/` | git |
| **Project knowledge** — what the system *is*, and what it currently does | [`../openspec/project.md`](../openspec/project.md) (system shape) and `../openspec/specs/<capability>/spec.md` (per-capability behavior) | git |
| **Authoritative decisions** — what we chose and why | [`../ARCHITECTURAL_DECISIONS.md`](../ARCHITECTURAL_DECISIONS.md) | git |
| **Work in flight** — proposals, tasks, delta specs | `../openspec/changes/**`, archived under `../openspec/changes/archive/` | git |
| **Procedural knowledge** — how *we* perform a recurring task | [`../.hermes/skills/<name>/SKILL.md`](../.hermes/skills/) — vendor-neutral, loaded on demand | git |
| **Agent instructions** — the rules every AI assistant must follow | [`../AGENTS.md`](../AGENTS.md) (vendor-neutral; `CLAUDE.md` defers to it) | git |
| **Project documentation** — how-tos, references, guides | `docs/**`, root `*.md`, `docs-site/` | git |
| **Historical write-ups** — point-in-time, not maintained | [`archive/`](archive/) | git |
| **AI working memory** | `~/.claude/projects/c--Repos-Booking/memory/` | **no — a cache** |
| **Historical AI conversations** | `~/.claude/projects/c--Repos-Booking/*.jsonl` | **no** |
| **Global / personal knowledge** | `~/.claude/CLAUDE.md` | no |

## Which copy wins

1. **"What does the system do?"** → the **code**, then `openspec/specs/`.
   A document describing behavior that the code contradicts is wrong, by definition.
2. **"Why did we choose this?"** → **`ARCHITECTURAL_DECISIONS.md`**.
   A doc contradicting an accepted ADR is a bug worth filing.
3. **"What is this system?"** → **`openspec/project.md`**.
4. **"How do we do X?"** → the **skill** in `.hermes/skills/`. Skills carry *procedure only* —
   never facts about the system. A skill asserting architecture is out of its lane; that belongs
   in `project.md` or a spec.
5. **AI memory never wins.** If memory and git disagree, git is right and the memory is stale.

## Rules

- **Nothing may live only in AI memory.** Memory is a cache: deleting the whole directory must
  cost nothing but re-derivation time. If a fact matters, it belongs in git — in an ADR, a spec,
  or `project.md`.
- **Global knowledge stays out of the project, project knowledge stays out of global.**
  If it names a class, endpoint, table, migration, branch, or a decision, it is project knowledge
  and belongs in this repository.
- **Facts get promoted; assumptions do not.** A fact is verifiable in the code, tests, or git
  history today. A plan or an intention belongs in `openspec/changes/`, not in `project.md`.
- **Cite the file.** Claims in `project.md` and in ADRs should name what proves them, so the next
  reader can re-verify instead of re-trusting.
- **Stale beats absent is false here.** A confidently wrong document is worse than no document —
  it is the failure this contract exists to prevent.

## For AI agents

Before answering an architecture question, read [`../openspec/project.md`](../openspec/project.md)
and [`../ARCHITECTURAL_DECISIONS.md`](../ARCHITECTURAL_DECISIONS.md). Treat your own memory as a
cache and verify it against git before relying on it. When you learn something durable, propose
adding it to the right file above — do not leave it in memory alone.
