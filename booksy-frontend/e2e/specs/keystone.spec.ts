import { test, expect } from '../fixtures/test-base'
import { LoginPage } from '../pages/login.page'
import { BookingFlowPage, MyBookingsPage } from '../pages/booking.page'
import { seedBookingWithToken } from '../utils/api-seed'
import { readSharedSeed } from '../utils/seed-fixture'
import { newCustomerIdentity } from '../utils/identity'

/**
 * Keystone UI E2E against the REAL backend, with sandbox auth
 * (OTP_SANDBOX_CODE + Sms:SandboxMode). See e2e/README.md to run.
 *
 * Uses the provider seeded once for the whole run by global-setup.ts (see
 * utils/seed-fixture.ts) rather than seeding its own.
 *
 * GREEN:
 *  - Customer OTP sign-in through the real Vue login flow.
 *  - The customer provider-detail screen rendering REAL backend data (this screen
 *    used to be hardcoded mock data; ProviderDetailView is now wired to
 *    providerService.getProviderById + serviceService.getServicesByProvider).
 *  - My Bookings list + cancel: was previously skipped ("renders empty despite a
 *    seeded booking") — root-caused to the `GET /Bookings/my-bookings` `from`/`to`
 *    query params binding as `DateTime` (silently reinterpreted as local server
 *    time instead of UTC) and its pagination binding to the wrong query-string
 *    names (`page`/`size` instead of the `pageNumber`/`pageSize` the frontend
 *    sends). Fixed in BookingsController.GetMyBookings.
 */
test.describe.configure({ mode: 'serial' })

test('customer can sign in with OTP (real backend)', async ({ page }) => {
  const login = new LoginPage(page)
  await login.loginAs('customer', newCustomerIdentity())
  await expect(page).not.toHaveURL(/login|phone-verification/)
  await expect(page.getByTestId('phone-input')).toHaveCount(0)
})

test.describe('Customer provider detail (real backend data)', () => {
  const seeded = readSharedSeed()
  const suffixName = 'E2E Salon'

  test('shows the seeded provider and its services (not mock data)', async ({ page }) => {
    test.setTimeout(90_000)
    await new LoginPage(page).loginAs('customer', newCustomerIdentity())

    await new BookingFlowPage(page).openProvider(seeded.providerId)
    // Real business name from register-full (was the hardcoded "آرایشگاه زیبای پارسی").
    // .first() — the name appears in both the cover img alt and the heading.
    await expect(page.getByText(new RegExp(suffixName, 'i')).first()).toBeVisible({ timeout: 20_000 })
    // Real service from register-full (name + description both say "Haircut").
    await expect(page.getByText(/Haircut/i).first()).toBeVisible()
    // The old hardcoded mock provider must be gone.
    await expect(page.getByText('آرایشگاه زیبای پارسی')).toHaveCount(0)
  })
})

test.describe('Customer My Bookings (UI)', () => {
  const seeded = readSharedSeed()

  test('customer sees a seeded booking in My Bookings and cancels it', async ({ page }) => {
    test.setTimeout(120_000)
    const login = new LoginPage(page)
    await login.loginAs('customer', newCustomerIdentity())

    // Seed the booking against this exact browser session's token/identity, not a
    // separately-generated one — sidesteps any phone-normalization mismatch.
    const token = await login.accessToken()
    await seedBookingWithToken(token, seeded)

    const myBookings = new MyBookingsPage(page)
    await myBookings.open()
    await myBookings.expectHasBooking()
    await myBookings.cancelFirst()
  })
})
