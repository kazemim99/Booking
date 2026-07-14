import { test, expect } from '../fixtures/test-base'
import { LoginPage } from '../pages/login.page'
import { RescheduleFlowPage } from '../pages/reschedule.page'
import { seedBookingWithToken } from '../utils/api-seed'
import { readSharedSeed } from '../utils/seed-fixture'
import { newCustomerIdentity } from '../utils/identity'

/**
 * Customer reschedules an existing confirmed booking to a new time, via the
 * "نوبت‌های من" sidebar (desktop user-menu → bookings), which is the UI path that
 * actually has reschedule support (the /customer/my-bookings page, covered by
 * keystone.spec.ts, only has cancel).
 *
 * Uses the provider seeded once for the whole run by global-setup.ts.
 *
 * NOTE: date selection drives the third-party vue3-persian-datetime-picker's
 * rendered `.vpd-day` cells directly (no data-testid hooks available inside a
 * vendored library) — this is the step most likely to need adjustment on first
 * real run against the live stack; see RescheduleFlowPage.pickEarliestAvailableDate.
 */
test('customer reschedules a booking to a new time', async ({ page }) => {
  test.setTimeout(120_000)
  const seeded = readSharedSeed()

  const login = new LoginPage(page)
  await login.loginAs('customer', newCustomerIdentity())

  const token = await login.accessToken()
  await seedBookingWithToken(token, seeded)

  const reschedule = new RescheduleFlowPage(page)
  await reschedule.openBookingsSidebar()
  await reschedule.openRescheduleForFirstBooking()
  await reschedule.pickEarliestAvailableDate()
  await reschedule.pickFirstAvailableSlot()
  await reschedule.confirm()
  await reschedule.expectSuccessAndModalClosed()
})
