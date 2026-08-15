# Agent Instructions

Vendor-neutral entry point for any AI assistant working in this repository.

## Start here: the knowledge contract

Read **[docs/KNOWLEDGE.md](docs/KNOWLEDGE.md)** first. It states where each kind of knowledge
lives and which copy wins when two sources disagree. In short:

- **What the system is** → [openspec/project.md](openspec/project.md) — verified against source, cites its evidence
- **What it does** → the code, then `openspec/specs/<capability>/spec.md`
- **Why we chose it** → [ARCHITECTURAL_DECISIONS.md](ARCHITECTURAL_DECISIONS.md)
- **What's in flight** → `openspec/changes/`

**Any AI memory you carry is a cache, not a source of truth.** Verify it against git before
relying on it; if it disagrees with the code, the code is right. Nothing durable may live only
in agent memory — if a fact matters, propose adding it to the right file above.

<!-- OPENSPEC:START -->
# OpenSpec Instructions

These instructions are for AI assistants working in this project.

Always open `@/openspec/AGENTS.md` when the request:
- Mentions planning or proposals (words like proposal, spec, change, plan)
- Introduces new capabilities, breaking changes, architecture shifts, or big performance/security work
- Sounds ambiguous and you need the authoritative spec before coding

Use `@/openspec/AGENTS.md` to learn:
- How to create and apply change proposals
- Spec format and conventions
- Project structure and guidelines

Keep this managed block so 'openspec update' can refresh the instructions.

<!-- OPENSPEC:END -->