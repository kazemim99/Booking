/**
 * Playwright global setup: fail fast if the environment isn't up, then seed one
 * shared bookable provider for the whole run.
 *
 * Mirrors the Coliride e2e pattern (a fast preflight check + a global-setup seed
 * step, run once per suite instead of per spec) — see
 * openspec/changes/harden-e2e-test-coverage/design.md for the comparison.
 *
 * 1. Preflight: shells out to tests/e2e/keystone-booking-flow.sh in PREFLIGHT_ONLY
 *    mode (health check + one OTP round-trip, a few seconds) so a down/misconfigured
 *    stack fails immediately with a clear message instead of a 120s webServer
 *    timeout or confusing mid-suite failures. Reuses the bash script instead of
 *    duplicating host/OTP/health logic in a second, parallel implementation.
 * 2. Seed: creates one bookable provider (+staff+service) via the API and writes
 *    it to a JSON fixture file so specs share it instead of each seeding their own
 *    (see e2e/utils/seed-fixture.ts).
 */
import { execFileSync } from 'node:child_process'
import { writeFileSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { seedBookableProvider } from './utils/api-seed'
import { SEED_FIXTURE_PATH } from './utils/seed-fixture'

const __dirname = dirname(fileURLToPath(import.meta.url))
const REPO_ROOT = join(__dirname, '..', '..')

export default async function globalSetup(): Promise<void> {
  process.stdout.write('\n[global-setup] preflight — checking the stack is up …\n')
  try {
    execFileSync('bash', [join(REPO_ROOT, 'tests', 'e2e', 'keystone-booking-flow.sh')], {
      cwd: REPO_ROOT,
      stdio: 'inherit',
      env: { ...process.env, PREFLIGHT_ONLY: '1' },
    })
  } catch (err) {
    throw new Error(
      '[global-setup] preflight failed — the Booksy host is not reachable or not accepting ' +
        'sandbox OTP auth. Start it with OTP_SANDBOX_CODE=123456 Sms__SandboxMode=true ' +
        'ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Host/Booksy.Host ' +
        '(see e2e/README.md).',
      { cause: err },
    )
  }

  process.stdout.write('[global-setup] seeding a shared bookable provider …\n')
  const seeded = await seedBookableProvider()
  writeFileSync(SEED_FIXTURE_PATH, JSON.stringify(seeded, null, 2))
  process.stdout.write(`[global-setup] seeded provider ${seeded.providerId}\n`)
}
