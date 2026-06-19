import type { Page } from '@playwright/test'
import { expect } from '../fixtures/test-base'

/**
 * Customer-side browse → book → my-bookings → cancel.
 *
 * Routes: /providers (list), /providers/:id (details), /bookings/new (flow),
 *         /customer or /bookings (my bookings).
 * data-testid contract:
 *   - provider-card (one per provider; first one is fine for the smoke)
 *   - book-service-button, service-option, slot-option, staff-option, booking-confirm
 *   - my-bookings-list, booking-row, booking-status, booking-cancel-button
 * Cancel uses a native window.confirm() dialog (not a modal) — accepted via page.once('dialog').
 */
export class BookingFlowPage {
  constructor(private readonly page: Page) {}

  /** Open a specific (seeded) provider's detail page (customer area). */
  async openProvider(providerId: string): Promise<void> {
    await this.page.goto(`/customer/provider/${providerId}`)
  }

  /** Open the first provider from the customer provider list. */
  async openFirstProvider(): Promise<void> {
    await this.page.goto('/customer/providers')
    const firstCard = this.page.getByTestId('provider-card').first()
    await expect(firstCard).toBeVisible()
    await firstCard.click()
  }

  private async clickIfVisible(testId: string): Promise<boolean> {
    const el = this.page.getByTestId(testId).first()
    if (await el.isVisible().catch(() => false)) {
      await el.click()
      return true
    }
    return false
  }

  /**
   * Drives the multi-step booking wizard. The exact step order (service → advance →
   * slot/staff → advance → confirm) is tolerated: each pick/advance is applied only
   * if its control is present. The final `booking-confirm` is required.
   */
  async bookFirstAvailable(): Promise<void> {
    await this.clickIfVisible('book-service-button')
    await this.clickIfVisible('service-option')
    await this.clickIfVisible('booking-advance')
    await this.clickIfVisible('slot-option')
    await this.clickIfVisible('staff-option')
    await this.clickIfVisible('booking-advance')
    await this.page.getByTestId('booking-confirm').click()
  }
}

export class MyBookingsPage {
  constructor(private readonly page: Page) {}

  async open(): Promise<void> {
    // A full page.goto('/customer/my-bookings') can 404 (auth-store hydration race
    // on hard reload). Navigate client-side via the sidebar link, like a real user.
    if (!/\/customer(\/|$)/.test(this.page.url())) {
      await this.page.goto('/customer/dashboard')
    }
    await this.page.getByRole('link', { name: /رزروهای من/ }).click()
    // NOTE: GET /Bookings/my-bookings is currently slow (~20s observed) — generous
    // timeout until that query is optimized.
    await expect(this.page.getByTestId('my-bookings-list')).toBeVisible({ timeout: 35_000 })
  }

  async expectHasBooking(): Promise<void> {
    await expect(this.page.getByTestId('booking-row').first()).toBeVisible({ timeout: 35_000 })
  }

  async cancelFirst(): Promise<void> {
    const row = this.page.getByTestId('booking-row').first()
    // Cancellation is confirmed via a native window.confirm() dialog — auto-accept it.
    this.page.once('dialog', (dialog) => dialog.accept())
    await row.getByTestId('booking-cancel-button').click()
    // Status label is localized (fa); match the Persian "لغو" or English "cancel".
    await expect(row.getByTestId('booking-status')).toContainText(/لغو|cancel/i)
  }
}
