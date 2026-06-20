import type { Page } from '@playwright/test'
import { expect } from '../fixtures/test-base'

/**
 * Drives the 8-step organization provider-registration wizard
 * (ProviderRegistrationView → OrganizationRegistrationFlow). Locators prefer
 * stable Persian labels/placeholders + roles over per-field testids.
 */
export class ProviderRegistrationPage {
  constructor(private readonly page: Page) {}

  /** Advance the wizard. Button label varies by step: "ادامه →", "بعدی", "تکمیل…". */
  private async next(): Promise<void> {
    const byTestId = this.page.getByTestId('reg-next')
    if (await byTestId.isVisible().catch(() => false)) {
      await byTestId.click()
      return
    }
    await this.page.getByRole('button', { name: /ادامه|بعدی|تکمیل|ثبت/ }).first().click()
  }

  async selectOrganizationIfPrompted(): Promise<void> {
    // ProviderRegistrationView may show a type-selection (individual/organization) first.
    const org = this.page.getByRole('button', { name: /سازمان|کسب.?و.?کار|مجموعه/ }).first()
    if (await org.isVisible({ timeout: 3000 }).catch(() => false)) {
      await org.click()
    }
  }

  // Step 1 — Business Info
  async fillBusinessInfo(businessName: string): Promise<void> {
    await expect(this.page.getByTestId('reg-business-name')).toBeVisible({ timeout: 15_000 })
    await this.page.getByTestId('reg-business-name').fill(businessName)
    await this.page.getByPlaceholder('نام', { exact: true }).fill('E2E')
    await this.page.getByPlaceholder('نام خانوادگی').fill('Owner')
    await this.page.getByPlaceholder(/توضیحات کوتاهی/).fill('E2E provider created through the UI wizard.')
    await this.next()
  }

  // Step 2 — Category
  async selectFirstCategory(): Promise<void> {
    await expect(this.page.locator('.category-card').first()).toBeVisible({ timeout: 15_000 })
    await this.page.locator('.category-card').first().click()
    await this.next()
  }

  // Step 3 — Location (city + address; the Neshan map is optional for validity)
  async fillLocation(): Promise<void> {
    const city = this.page.getByPlaceholder('جستجوی شهر...')
    await expect(city).toBeVisible({ timeout: 15_000 })
    await city.click()
    await city.fill('تهران')
    const option = this.page.locator('.dropdown-item').first()
    await expect(option).toBeVisible({ timeout: 10_000 })
    await option.click()
    const addr = this.page.getByRole('textbox', { name: /آدرس دقیق/ })
    await addr.fill('خیابان ولیعصر، پلاک ۱')
    await expect(addr).toHaveValue('خیابان ولیعصر، پلاک ۱')
    await this.next()
  }

  /** Run all steps to completion. Returns when the completion screen shows. */
  async completeOrganizationRegistration(businessName: string): Promise<void> {
    await this.selectOrganizationIfPrompted()
    await this.fillBusinessInfo(businessName)
    await this.selectFirstCategory()
    await this.fillLocation()
    // Steps 4–8 wired incrementally as they're discovered.
  }
}
