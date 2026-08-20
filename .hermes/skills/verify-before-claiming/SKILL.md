---
name: verify-before-claiming
description: How to establish that a claim about this repository is actually true before writing it into a doc, spec, ADR, commit message, or answer. Use when documenting architecture, auditing a change, promoting remembered knowledge, or asserting that something works.
version: 1.0.0
metadata:
  hermes:
    tags: [verification, documentation, accuracy]
    category: project-workflow
---

# Verify Before Claiming (Booking)

## When to Use

Load this before writing any factual claim about this repository into a durable place — a doc,
`openspec/project.md`, a spec, an ADR, a commit message — or before repeating something you
recall from memory or a previous session.

The rule this encodes: **`openspec/project.md` once described a microservices system with
RabbitMQ, an API gateway, Autofac, and per-context databases. None of it was true.** It had been
inherited from documentation rather than read from source, and it survived for months because
every reader trusted the previous reader.

## Procedure

### 1. Derive from source, never from other documentation

Check the claim against `.csproj`, `Program.cs`, `appsettings*.json`, `docker-compose*.yml`,
migrations, workflows, or tests. Do **not** verify one document against another — that is how the
original errors propagated. Assistant-instruction files, `README.md`, and `docs-site/` are
*guidance*, not evidence.

### 2. "Referenced" is not "wired"

The most common false claim in this repo is a package that appears in a `.csproj` but is never
activated. Before asserting a capability is in use, find its **call site**:

| Claim | The check that settles it |
|---|---|
| A DI container is in use | is the factory actually swapped in `Program.cs`? |
| A naming convention applies | is the convention method actually called? |
| Architecture rules are enforced | does the test project contain a real rule, or an empty stub? |
| Telemetry is active | does any project *reference* the monitoring assembly? |
| A logging sink is configured | is there a matching config section, or only a package? |

A referenced-but-unwired dependency should be documented as **explicitly not present**, so the
false claim cannot quietly return.

### 3. Cite the file that proves it

Every durable claim names its evidence — file path, line, migration id, or command output — so
the next reader can re-verify instead of re-trusting.

### 4. Distinguish the working tree from the committed state

Counts taken from the working tree include uncommitted work. If you state "verified against
commit `<sha>`", make sure the numbers actually come from that commit:

```bash
git ls-files <path> | wc -l     # committed
ls <path> | wc -l               # working tree — may be ahead
```

Say which one you measured.

### 5. Distrust your own verification command

An audit script can produce a false alarm as easily as a doc can carry a false claim. Before
reporting a discrepancy, confirm the *command* is sound. Real examples from this repo:

- Counting `*.csproj` matched a **directory** named `Booksy.Tests.Common.csproj`.
- Grepping compose files for a broker matched **comments saying the broker was removed**.
- Counting migrations included `…DbContextModelSnapshot.cs`, which is not a migration.

When a check disagrees with a document, investigate the check first.

### 6. Treat recalled knowledge as a lead, not a fact

Anything from agent memory or a prior session is a **hypothesis to test against git today**. Most
of it turns out to be already recorded in the repository; some of it is stale; a little is true,
absent, and worth promoting. Verify before promoting, and re-verify before repeating.

## Pitfalls

- **A follow-up question is not evidence you were wrong.** Answer what was asked; do not re-audit
  a statement that was accurate.
- **Machine-specific facts are not project facts.** A network blockage or local toolchain break
  belongs in agent memory or a build comment — not in a spec or `project.md`.
- **Absence of a grep hit is not absence of the thing.** Confirm the pattern would have matched.

## Verification

State plainly what you checked and what the check returned. If something could not be verified,
say so explicitly rather than softening the claim into something unfalsifiable.
