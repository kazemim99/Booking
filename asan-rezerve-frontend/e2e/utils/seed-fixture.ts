/**
 * Reads the shared provider seeded once per run by global-setup.ts, instead of
 * every spec file calling seedBookableProvider() independently.
 */
import { readFileSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import type { SeededProvider } from './api-seed'

const __dirname = dirname(fileURLToPath(import.meta.url))

/** gitignored — written by global-setup.ts at the start of each run. */
export const SEED_FIXTURE_PATH = join(__dirname, '..', '.seed-output.json')

export function readSharedSeed(): SeededProvider {
  try {
    return JSON.parse(readFileSync(SEED_FIXTURE_PATH, 'utf-8')) as SeededProvider
  } catch (err) {
    throw new Error(
      `Could not read the shared seed fixture at ${SEED_FIXTURE_PATH} — did global-setup run? ` +
        'Playwright only runs globalSetup for full `npm run e2e:pw` runs, not when a single ' +
        'spec is run directly against an already-running dev server without it.',
      { cause: err },
    )
  }
}
