import { defineConfig, devices } from '@playwright/test'

/**
 * Playwright E2E configuration for booksy-frontend.
 *
 * Drives the real Vue app (port 3000) against a running monolith stack. The Vite
 * dev server proxies /api to the host on :5050 (see vite.config.ts). For OTP login
 * to be deterministic the host MUST run with OTP_SANDBOX_CODE=123456 and
 * Sms:SandboxMode=true — see e2e/README.md.
 *
 * Bootstrap (first run):
 *   npm install
 *   npx playwright install --with-deps chromium
 *   npm run e2e:pw
 */
const PORT = 3000
const BASE_URL = process.env.E2E_BASE_URL ?? `http://localhost:${PORT}`
const isCI = !!process.env.CI

export default defineConfig({
  testDir: './e2e/specs',
  outputDir: './test-results',
  fullyParallel: true,
  forbidOnly: isCI,
  retries: isCI ? 2 : 0,
  workers: isCI ? 1 : undefined,
  // Generous: the my-bookings query is currently slow (~20s); see e2e/pages/booking.page.ts.
  timeout: 120_000,
  expect: { timeout: 10_000 },
  reporter: isCI
    ? [['html', { open: 'never' }], ['list']]
    : [['html', { open: 'never' }], ['list']],
  use: {
    baseURL: BASE_URL,
    trace: 'on-first-retry',
    // Video needs ffmpeg (bundled by `playwright install --with-deps`); allow
    // disabling via PW_VIDEO=off in environments where that download is blocked.
    video: (process.env.PW_VIDEO as 'on' | 'off' | 'retain-on-failure') ?? 'retain-on-failure',
    screenshot: 'only-on-failure',
    actionTimeout: 15_000,
    // Fixed geolocation so location-aware screens (provider search, registration map)
    // don't prompt or flake. Tehran city center.
    permissions: ['geolocation'],
    geolocation: { latitude: 35.6892, longitude: 51.389 },
    locale: 'fa-IR',
  },
  projects: [
    {
      name: 'chromium',
      // Use the system-installed Google Chrome ('chrome' channel) so CI/dev don't
      // need Playwright's bundled Chromium download. Override with PW_CHANNEL.
      use: { ...devices['Desktop Chrome'], channel: process.env.PW_CHANNEL ?? 'chrome' },
    },
  ],
  // Only auto-start the frontend dev server. The backend stack (host + Postgres +
  // Redis) is assumed to be already running with the sandbox env (locally via
  // docker-compose / dotnet run; in CI started by the workflow before this runs).
  webServer: {
    command: 'npm run dev',
    url: BASE_URL,
    reuseExistingServer: !isCI,
    timeout: 120_000,
  },
})
