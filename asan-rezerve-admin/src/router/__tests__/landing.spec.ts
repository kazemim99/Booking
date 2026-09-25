import { describe, expect, it, vi } from 'vitest'

// The router module reads the auth store for its guard; the landing redirect does not need it.
vi.mock('../../stores/auth.store', () => ({ useAuthStore: () => ({}) }))

import { routes } from '../index'

// After login the admin lands on the providers list. The dashboard it used to land on calls
// /analytics/* endpoints the backend does not have (verified with an admin token on production,
// 2026-09-19), so the first screen an admin saw was a page of errors.
describe('admin landing page', () => {
  it('opens on the providers list', () => {
    const shell = routes.find((r) => r.path === '/')
    const landing = shell?.children?.find((c) => c.path === '')
    expect(landing?.redirect).toBe('/providers')
  })
})
