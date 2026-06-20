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

  /**
   * Click next and wait for the next step's heading. Step transitions persist a
   * draft via a (slow, ~10s) backend save; without waiting, a second next() would
   * double-submit and the save hits a DB concurrency conflict.
   */
  private async nextAndWait(heading: RegExp): Promise<void> {
    await this.next()
    await expect(this.page.getByRole('heading', { name: heading }).first())
      .toBeVisible({ timeout: 30_000 })
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
    await this.nextAndWait(/دسته.?بندی/)
  }

  // Step 2 — Category
  async selectFirstCategory(): Promise<void> {
    await expect(this.page.locator('.category-card').first()).toBeVisible({ timeout: 15_000 })
    await this.page.locator('.category-card').first().click()
    await this.nextAndWait(/موقعیت مکانی/)
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
    await this.nextAndWait(/^خدمات$/)
  }

  // Step 4 — Services (add one via the modal)
  async addService(): Promise<void> {
    await this.page.getByRole('button', { name: /افزودن خدمت جدید/ }).first().click()
    await this.page.getByPlaceholder('مثال: اصلاح مو').fill('کوتاهی مو')
    await this.page.getByPlaceholder('500,000').fill('250000')
    // Duration is a <select>; pick the first real option.
    await this.page.locator('select').first().selectOption({ index: 1 })
    await this.page.getByRole('button', { name: /^افزودن$/ }).click()
    // Service now listed → continue.
    await expect(this.page.getByText('کوتاهی مو').first()).toBeVisible({ timeout: 10_000 })
    await this.nextAndWait(/ساعات کاری/)
  }

  // Step 5 — Working Hours (all days open by default)
  async confirmWorkingHours(): Promise<void> {
    await this.nextAndWait(/گالری تصاویر/)
  }

  // Step 6 — Gallery (optional)
  async skipGallery(): Promise<void> {
    await this.nextAndWait(/بررسی نهایی/)
  }

  // Step 7 — Preview & confirm → submit
  async confirmAndSubmit(): Promise<void> {
    // Tick the "I confirm the information is correct" checkbox, then complete.
    const confirm = this.page.getByRole('checkbox').first()
    if (await confirm.isVisible({ timeout: 5000 }).catch(() => false)) {
      await confirm.check()
    }
    await this.page.getByRole('button', { name: /تکمیل ثبت|ثبت نهایی|تکمیل ثبت‌نام/ }).first().click()
  }

  /** Run all steps to completion. Returns when the completion screen shows. */
  async completeOrganizationRegistration(businessName: string): Promise<void> {
    await this.selectOrganizationIfPrompted()
    await this.fillBusinessInfo(businessName)
    await this.selectFirstCategory()
    await this.fillLocation()
    await this.addService()
    await this.confirmWorkingHours()
    await this.skipGallery()
    await this.confirmAndSubmit()
  }
}
