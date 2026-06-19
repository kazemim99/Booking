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
 *   - my-bookings-list, booking-row, booking-status
 *   - booking-cancel-button, cancel-confirm, refund-message
 */
export class BookingFlowPage {
  constructor(private readonly page: Page) {}

  async openFirstProvider(): Promise<void> {
    await this.page.goto('/providers')
    const firstCard = this.page.getByTestId('provider-card').first()
    await expect(firstCard).toBeVisible()
    await firstCard.click()
  }

  /** Drives the booking flow happy path: service → slot → staff → confirm. */
  async bookFirstAvailable(): Promise<void> {
    await this.page.getByTestId('book-service-button').first().click()
    await this.page.getByTestId('service-option').first().click()
    await this.page.getByTestId('slot-option').first().click()
    const staff = this.page.getByTestId('staff-option').first()
    if (await staff.isVisible().catch(() => false)) await staff.click()
    await this.page.getByTestId('booking-confirm').click()
  }
}

export class MyBookingsPage {
  constructor(private readonly page: Page) {}

  async open(): Promise<void> {
    await this.page.goto('/bookings')
    await expect(this.page.getByTestId('my-bookings-list')).toBeVisible()
  }

  async expectHasBooking(): Promise<void> {
    await expect(this.page.getByTestId('booking-row').first()).toBeVisible()
  }

  async cancelFirst(): Promise<void> {
    const row = this.page.getByTestId('booking-row').first()
    await row.getByTestId('booking-cancel-button').click()
    await this.page.getByTestId('cancel-confirm').click()
    await expect(row.getByTestId('booking-status')).toContainText(/cancel/i)
  }
}
