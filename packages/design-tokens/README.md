# @booksy/tokens

Single source of truth for Booksy's design tokens (the Coliride brand): colors, typography,
spacing, radii, shadows, z-index, transitions. Plain CSS custom properties in `:root` — framework
agnostic, consumed by both Vue apps.

## What's here
- `tokens.css` — all design tokens as CSS custom properties (`--color-*`, `--radius-*`, `--shadow-*`,
  `--font-family-*`, …). Edit the brand **here only**; both apps update automatically.

## How it's consumed today (relative import)
Until a workspace install is wired (see below), each app imports the file by relative path in its
entry `main.ts`, before its own styles:

```ts
// booksy-frontend/src/main.ts  and  booksy-admin/src/main.ts
import '../../packages/design-tokens/tokens.css'
```

Vite dev needs filesystem access to this sibling folder, enabled in each app's `vite.config.ts`:

```ts
server: { fs: { allow: ['..'] } }
```

The admin additionally maps these tokens onto Ant Design via `src/config/antd-theme.ts`
(`<a-config-provider :theme>`), since Ant components are themed through Ant's token system.

## Promoting to a real workspace dependency (next step)
1. Add npm/pnpm workspaces to a root `package.json`:
   ```json
   { "private": true, "workspaces": ["booksy-frontend", "booksy-admin", "packages/*"] }
   ```
2. Add `"@booksy/tokens": "*"` (npm) / `"workspace:*"` (pnpm) to each app's `dependencies`, then
   install at the repo root to link it.
3. Replace the relative imports with `import '@booksy/tokens/tokens.css'` and drop the
   `server.fs.allow` entry.

## Brand reference
Tokens mirror the Coliride app design language: navy ink `#4d5e80`, primary blue `#3777bf`,
chrome blue `#3777c0`, accent green `#0ac075`, map-pin green `#1fa96e`, error `#ff6171`, soft
10–16px radii, flat navy-tinted shadows. See `MONOLITH_MIGRATION_PLAN.md` siblings / project memory
for the full brand spec.
