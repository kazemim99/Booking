import { test as base, expect } from '@playwright/test'

/**
 * Shared fixtures.
 *
 * `stubMaps` neutralizes the Neshan map so external tiles / geocode calls can't
 * make runs flaky. Geolocation is granted with a fixed position via the project
 * config (playwright.config.ts). Every page gets the route stub installed.
 */
export const test = base.extend({
  page: async ({ page }, use) => {
    // Stub Neshan map traffic — tiles, static maps, geocode, reverse-geocode.
    await page.route(/(\.neshan\.org|api\.neshan\.org|static\.neshan)/i, (route) => {
      const url = route.request().url()
      if (/geocod|search|reverse/i.test(url)) {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ items: [], status: 'OK' }),
        })
      }
      // tiles / scripts / styles → tiny transparent PNG (or empty 200)
      return route.fulfill({
        status: 200,
        contentType: 'image/png',
        body: Buffer.from(
          'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==',
          'base64',
        ),
      })
    })
    await use(page)
  },
})

export { expect }
