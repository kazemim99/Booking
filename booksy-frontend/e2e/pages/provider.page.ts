import type { Page } from '@playwright/test'
import { expect } from '../fixtures/test-base'

/**
 * Provider registration wizard + staff management.
 *
 * Routes: /provider/registration (and /individual, /organization), /provider/staff.
 * data-testid contract (registration is a multi-step wizard — add per step):
 *   - reg-business-name, reg-category, reg-next, reg-submit
 *   - reg-step-{location,services,hours,…}-next   (one "next" per step)
 *   - staff-add-button, staff-first-name, staff-last-name, staff-role, staff-save
 *   - staff-list (container), staff-row (one per member)
 */
export class ProviderRegistrationPage {
  constructor(private readonly page: Page) {}

  /**
   * Completes the organization registration wizard with minimal valid data.
   * Each step exposes a `reg-next`; the final step exposes `reg-submit`.
   */
  async registerOrganization(businessName: string): Promise<void> {
    await this.page.goto('/provider/registration/organization')

    await this.page.getByTestId('reg-business-name').fill(businessName)

    // Walk the wizard: click whichever "next" is visible until submit appears.
    // Steps that need input expose their own testids; this drives the happy path.
    for (let i = 0; i < 10; i++) {
      const submit = this.page.getByTestId('reg-submit')
      if (await submit.isVisible().catch(() => false)) {
        await submit.click()
        break
      }
      const next = this.page.getByTestId('reg-next')
      await expect(next).toBeVisible()
      await next.click()
    }

    // Auto-approve → provider is Active; we land on the dashboard.
    await expect(this.page).toHaveURL(/\/provider\/(dashboard|profile)/)
  }
}

export class StaffPage {
  constructor(private readonly page: Page) {}

  async addStaff(firstName: string, lastName: string, role = 'Stylist'): Promise<void> {
    await this.page.goto('/provider/staff')
    await this.page.getByTestId('staff-add-button').click()
    await this.page.getByTestId('staff-first-name').fill(firstName)
    await this.page.getByTestId('staff-last-name').fill(lastName)
    const roleField = this.page.getByTestId('staff-role')
    if (await roleField.isVisible().catch(() => false)) await roleField.fill(role)
    await this.page.getByTestId('staff-save').click()
  }

  async expectStaffListed(name: string): Promise<void> {
    await expect(this.page.getByTestId('staff-list')).toContainText(name)
  }
}
