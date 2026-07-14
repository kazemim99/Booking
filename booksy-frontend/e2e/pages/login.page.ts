import type { Page } from '@playwright/test'
import { expect } from '../fixtures/test-base'
import { OTP_CODE, type TestIdentity } from '../utils/identity'

/**
 * OTP login for either audience.
 *
 * Routes (see src/core/router/routes): customer → /customer/login, provider → /provider/login.
 * data-testid contract (wired in the auth components):
 *   - phone-input        (PhoneNumberInput tel field)
 *   - send-code-button   (request OTP)
 *   - otp-input          (OtpInput container; holds N single-digit cell <input>s)
 *   - verify-button      (optional — this flow auto-submits when the OTP completes)
 *   - first-name-input / last-name-input (shown for first-time signup; optional)
 */
export class LoginPage {
  constructor(private readonly page: Page) {}

  async loginAs(kind: 'customer' | 'provider', who: TestIdentity): Promise<void> {
    await this.page.goto(`/${kind}/login`)

    await this.page.getByTestId('phone-input').fill(who.phone)
    await this.page.getByTestId('send-code-button').click()

    // OtpInput renders one <input> per digit and auto-submits on completion.
    const otp = this.page.getByTestId('otp-input')
    await expect(otp).toBeVisible()
    const cells = otp.locator('input')
    for (let i = 0; i < OTP_CODE.length; i++) {
      await cells.nth(i).fill(OTP_CODE[i])
    }

    // First-time users may need to provide a name; fill if the prompt appears.
    const firstName = this.page.getByTestId('first-name-input')
    if (await firstName.isVisible().catch(() => false)) {
      await firstName.fill(who.firstName)
      await this.page.getByTestId('last-name-input').fill(who.lastName)
    }

    // Some flows show an explicit verify button instead of auto-submitting.
    const verify = this.page.getByTestId('verify-button')
    if (await verify.isVisible().catch(() => false)) await verify.click()

    // Login is a two-page flow (/login → /phone-verification). After a successful
    // OTP we land on an authenticated page — assert we left both auth routes.
    await expect(this.page).not.toHaveURL(/login|phone-verification/)
  }

  /**
   * The logged-in user's access token (auth.store.ts persists it to localStorage as
   * "access_token"). Used to seed API-side state that must belong to exactly this
   * browser session's identity, sidestepping any phone-normalization mismatch between
   * the UI login and a separately-seeded API identity.
   */
  async accessToken(): Promise<string> {
    const token = await this.page.evaluate(() => localStorage.getItem('access_token'))
    if (!token) throw new Error('No access_token in localStorage — is the user logged in?')
    return token
  }
}
