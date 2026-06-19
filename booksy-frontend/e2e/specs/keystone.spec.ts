import { test, expect } from '../fixtures/test-base'
import { LoginPage } from '../pages/login.page'
import { BookingFlowPage, MyBookingsPage } from '../pages/booking.page'
import { seedBookableProvider, type SeededProvider } from '../utils/api-seed'
import { newCustomerIdentity } from '../utils/identity'

/**
 * Keystone customer journey through the real UI — the browser-level twin of
 * tests/e2e/keystone-booking-flow.sh. Requires the stack running with sandbox
 * auth (see e2e/README.md).
 *
 * Supply side (provider + active staff + service) is seeded via the API — the
 * provider registration wizard and staff *invitation* flow are awkward to drive
 * through the browser and the UI invite produces a pending invitation, not a
 * bookable staff member (see e2e/utils/api-seed.ts and design.md). The customer
 * journey — browse → book → cancel — is exercised through the UI.
 *
 * Steps run serially: each depends on the previous.
 */
test.describe.configure({ mode: 'serial' })

test.describe('Keystone customer journey', () => {
  const customer = newCustomerIdentity()
  let seeded: SeededProvider

  test.beforeAll(async () => {
    seeded = await seedBookableProvider()
  })

  test('customer logs in, browses the provider, and books', async ({ page }) => {
    const login = new LoginPage(page)
    await login.loginAs('customer', customer)

    const booking = new BookingFlowPage(page)
    await booking.openProvider(seeded.providerId)
    await booking.bookFirstAvailable()

    const myBookings = new MyBookingsPage(page)
    await myBookings.open()
    await myBookings.expectHasBooking()
  })

  test('customer cancels the booking', async ({ page }) => {
    const login = new LoginPage(page)
    await login.loginAs('customer', customer)

    const myBookings = new MyBookingsPage(page)
    await myBookings.open()
    await myBookings.expectHasBooking()
    // cancelFirst() accepts the native confirm dialog and asserts the cancelled
    // status; the refund amount itself is covered by the backend unit tests.
    await myBookings.cancelFirst()
  })
})

/**
 * Optional: provider self-onboarding through the registration *wizard* (UI).
 * Skipped by default — the wizard has many required per-step fields and needs a
 * first real-run pass to finalize selectors (testids reg-business-name / reg-next
 * are wired). Enable once tuned against the running app.
 */
test.describe('Provider registration (UI)', () => {
  test.skip('provider can self-onboard through the wizard', async () => {
    // See e2e/pages/provider.page.ts (ProviderRegistrationPage / StaffPage).
    expect(true).toBe(true)
  })
})
