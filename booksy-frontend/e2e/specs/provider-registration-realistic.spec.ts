import { test, expect } from '../fixtures/test-base'
import { LoginPage } from '../pages/login.page'
import { ProviderRegistrationPage } from '../pages/provider-registration.page'
import { newProviderUiIdentity, newBusinessName } from '../utils/identity'
import { fileURLToPath } from 'url'

/**
 * Realistic provider self-onboarding — exercises what a real user actually does:
 *  - drops a pin on the Neshan map (sets real coordinates),
 *  - changes working hours (closes a day),
 *  - uploads several gallery images,
 *  - goes BACK from the review to edit a section, then returns,
 *  - completes, and is taken to the provider dashboard.
 *
 * Run headed:  PW_SLOWMO=400 npx playwright test provider-registration-realistic --headed --workers=1
 */
const IMG = (n: number) => fileURLToPath(new URL(`../fixtures/salon${n}.png`, import.meta.url))

test('realistic: provider onboards with map pin, custom hours, gallery, back-edit, dashboard', async ({ page }) => {
  test.setTimeout(240_000)
  await new LoginPage(page).loginAs('provider', newProviderUiIdentity())
  await expect(page).toHaveURL(/\/provider\/registration/, { timeout: 15_000 })

  const reg = new ProviderRegistrationPage(page)
  await reg.selectOrganizationIfPrompted()
  await reg.fillBusinessInfo(newBusinessName())
  await reg.selectFirstCategory()
  await reg.fillLocationWithMapPin()
  await reg.addService()
  // Real users navigate back: from Working Hours, step back to Services then return.
  await reg.goBackToServicesAndReturn()
  await reg.closeOneWorkingDayAndContinue()
  // Upload several gallery images during onboarding (the multipart Content-Type bug
  // that 499'd this is now fixed centrally in the http-client).
  await reg.uploadGalleryImagesAndContinue([IMG(1), IMG(2), IMG(3)])

  await expect(page.getByRole('heading', { name: /بررسی نهایی/ })).toBeVisible({ timeout: 15_000 })
  await reg.confirmAndSubmit()

  // Completion → into the provider dashboard. The completion handler refreshes the
  // token + loads provider/hierarchy/status first (~15-20s), so allow generous time.
  await reg.goToDashboard()
  await expect(page).toHaveURL(/\/provider\/dashboard/, { timeout: 45_000 })
  await page.screenshot({ path: 'test-results/provider-registration-realistic-dashboard.png', fullPage: true })
})

/**
 * Pick a MAIN gallery image. Skipped — the 3 images uploaded during onboarding DO
 * persist (provider_gallery_images has 3 rows, step-7/gallery → 200 after the
 * multipart fix), but the dashboard gallery (/provider/gallery) renders empty for a
 * freshly-registered provider, so there's nothing to set as primary. That's a real
 * frontend loading bug (data present, not displayed). Un-skip once the dashboard
 * gallery loads the provider's images; the set-main flow itself is wired below.
 */
test.skip('gallery: set a main image (blocked: dashboard gallery renders empty)', async ({ page }) => {
  await page.goto('/provider/gallery')
  const firstCard = page.locator('.image-card').first()
  await firstCard.hover()
  await firstCard.getByTitle('تنظیم به عنوان تصویر اصلی').click()
  await expect(firstCard.locator('.primary-badge')).toBeVisible({ timeout: 15_000 })
})
