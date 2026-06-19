import { test, expect } from '../fixtures/test-base'
import { LoginPage } from '../pages/login.page'
import { ProviderRegistrationPage, StaffPage } from '../pages/provider.page'
import { BookingFlowPage, MyBookingsPage } from '../pages/booking.page'
import {
  newProviderIdentity,
  newCustomerIdentity,
  newBusinessName,
} from '../utils/identity'

/**
 * Keystone booking journey through the real UI — the browser-level twin of
 * tests/e2e/keystone-booking-flow.sh. Requires the stack running with sandbox
 * auth (see e2e/README.md).
 *
 * Steps run in order and share one page (serial) because each depends on the
 * previous (a booking can't exist before the provider+staff do).
 */
test.describe.configure({ mode: 'serial' })

test.describe('Keystone booking flow', () => {
  const provider = newProviderIdentity()
  const customer = newCustomerIdentity()
  const businessName = newBusinessName()

  test('provider onboards and adds a staff member', async ({ page }) => {
    const login = new LoginPage(page)
    await login.loginAs('provider', provider)

    const registration = new ProviderRegistrationPage(page)
    await registration.registerOrganization(businessName)

    const staff = new StaffPage(page)
    await staff.addStaff('Sara', 'Stylist')
    await staff.expectStaffListed('Sara')
  })

  test('customer books an appointment and sees it in My Bookings', async ({ page }) => {
    const login = new LoginPage(page)
    await login.loginAs('customer', customer)

    const booking = new BookingFlowPage(page)
    await booking.openFirstProvider()
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
    await myBookings.cancelFirst()

    // Refund messaging reflects the policy outcome (full in-window, else partial).
    await expect(page.getByTestId('refund-message')).toBeVisible()
  })
})
