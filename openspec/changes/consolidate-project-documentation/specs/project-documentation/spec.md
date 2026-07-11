## ADDED Requirements

### Requirement: Tiered documentation layout

The repository SHALL organize its markdown documentation into three tiers so a reader can tell, from a file's location alone, whether it describes current behavior: root-level living docs, `docs/` secondary reference docs, and `docs/archive/` historical/completed write-ups.

#### Scenario: Living doc at root
- **WHEN** a doc describes the current architecture, API surface, or an in-progress plan (e.g. `README.md`, `API_ENDPOINTS.md`, `COMPLETION_ROADMAP.md`)
- **THEN** it lives at the repository root

#### Scenario: Completed work moved to archive
- **WHEN** a doc is a point-in-time write-up of a feature that has since shipped (e.g. an implementation log or a build-verification snapshot)
- **THEN** it lives under `docs/archive/` with a one-line banner noting it is archived and why

#### Scenario: No duplicate docs on the same topic
- **WHEN** two or more docs cover the same topic (e.g. two geolocation troubleshooting guides)
- **THEN** they are merged into a single doc rather than kept as separate near-duplicates

### Requirement: CLAUDE.md reflects current testing setup

`CLAUDE.md` SHALL document the E2E and integration testing setup that exists in the repository.

#### Scenario: Testing section present
- **WHEN** a reader (human or AI assistant) opens `CLAUDE.md` to learn how to test the app
- **THEN** it finds a section covering the Playwright E2E suite (`booksy-frontend/e2e/`) and the Reqnroll BDD integration tests (`tests/Booksy.ServiceCatalog.IntegrationTests/`)
