import type { Page } from '@playwright/test'
import { expect } from '../fixtures/test-base'

/**
 * Drives the customer "نوبت‌های من" sidebar's reschedule flow (desktop user-menu
 * → bookings sidebar → reschedule button → RescheduleBookingModal).
 *
 * data-testid contract:
 *   - user-menu-toggle, user-menu-item-bookings (opens the sidebar)
 *   - sidebar-booking-row, booking-reschedule-button (per upcoming booking)
 *   - reschedule-slot-option, reschedule-confirm (inside the modal)
 * The calendar date picker is a vendored third-party component
 * (vue3-persian-datetime-picker) with no data-testid hooks; date selection uses
 * its rendered `.vpd-day` cells directly.
 */
export class RescheduleFlowPage {
  constructor(private readonly page: Page) {}

  async openBookingsSidebar(): Promise<void> {
    await this.page.getByTestId('user-menu-toggle').click()
    await this.page.getByTestId('user-menu-item-bookings').click()
    await expect(this.page.getByTestId('sidebar-bookings-list')).toBeVisible({ timeout: 35_000 })
  }

  async openRescheduleForFirstBooking(): Promise<void> {
    const row = this.page.getByTestId('sidebar-booking-row').first()
    await expect(row).toBeVisible()
    await row.getByTestId('booking-reschedule-button').click()
    await expect(this.page.getByTestId('reschedule-confirm')).toBeVisible()
  }

  /** Picks the earliest non-empty, non-disabled day in the inline calendar (the
   * modal enforces a minimum of "tomorrow"). */
  async pickEarliestAvailableDate(): Promise<void> {
    const day = this.page.locator('.vpd-day:not(.vpd-empty):not([disabled])').first()
    await expect(day).toBeVisible({ timeout: 15_000 })
    await day.click()
  }

  async pickFirstAvailableSlot(): Promise<void> {
    const slot = this.page.getByTestId('reschedule-slot-option').first()
    await expect(slot).toBeVisible({ timeout: 20_000 })
    await slot.click()
  }

  async confirm(): Promise<void> {
    const confirmButton = this.page.getByTestId('reschedule-confirm')
    await expect(confirmButton).toBeEnabled()
    await confirmButton.click()
  }

  /** The modal only closes after a successful reschedule (see
   * BookingsSidebar.confirmRescheduleBooking) — its disappearance is the signal. */
  async expectSuccessAndModalClosed(): Promise<void> {
    await expect(this.page.getByTestId('reschedule-confirm')).toHaveCount(0, { timeout: 20_000 })
  }
}
