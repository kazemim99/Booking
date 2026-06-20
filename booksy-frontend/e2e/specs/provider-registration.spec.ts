import { test, expect } from '../fixtures/test-base'
import { LoginPage } from '../pages/login.page'
import { newProviderUiIdentity } from '../utils/identity'

/**
 * Provider self-onboarding through the real UI.
 *
 * A brand-new provider signs in with OTP and is auto-redirected into the
 * multi-step registration wizard (ProviderRegistrationFlow): Business Info →
 * Category → Location → Services → Working Hours → … → complete. Each step
 * persists a draft via the backend (provider-registration.service).
 *
 * Run headed to watch it:  PW_SLOWMO=800 npm run e2e:pw -- --headed --workers=1
 */
test('new provider signs in and reaches the registration wizard (step 1)', async ({ page }) => {
  await new LoginPage(page).loginAs('provider', newProviderUiIdentity())

  // New provider → auto-redirected into the wizard.
  await expect(page).toHaveURL(/\/provider\/registration/, { timeout: 15_000 })

  // Step 1 — Business Info — is rendered (real wizard, not a stub).
  await expect(page.getByRole('heading', { name: 'اطلاعات کسب‌و‌کار' })).toBeVisible({ timeout: 15_000 })
  await expect(page.getByTestId('reg-business-name')).toBeVisible()
})
